using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MtgMullagain.Core.Entities;
using MtgMullagain.Infrastructure;

namespace MtgMullagain.Infrastructure.Services;

public class ScryfallImportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MtgMullagainDbContext _context;
    private readonly ILogger<ScryfallImportService> _logger;
    private const string BULK_DATA_API = "https://api.scryfall.com/bulk-data";
    private const string ORACLE_CARDS_TYPE = "oracle_cards";
    private const string CACHE_DIR = "/app/cache";

    public ScryfallImportService(
        IHttpClientFactory httpClientFactory,
        MtgMullagainDbContext context,
        ILogger<ScryfallImportService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;

        // Ensure cache directory exists
        Directory.CreateDirectory(CACHE_DIR);
    }

    /// <summary>
    /// Main import method - checks if update is needed and performs import
    /// </summary>
    public async Task<ImportResult> ImportCardsAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting Scryfall card import process...");

        try
        {
            // Get bulk data info from Scryfall
            var bulkDataInfo = await GetBulkDataInfoAsync(cancellationToken);
            if (bulkDataInfo == null)
            {
                _logger.LogError("Failed to retrieve bulk data information from Scryfall");
                return new ImportResult 
                { 
                    Success = false, 
                    Message = "Failed to retrieve bulk data from Scryfall" 
                };
            }

            // Check if we need to update
            var lastImport = await _context.ImportMetadata
                .Where(m => m.Type == "scryfall_oracle_cards")
                .OrderByDescending(m => m.ImportedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var isFirstImport = lastImport == null;
            var needsUpdate = isFirstImport || 
                             lastImport!.UpdatedAt < bulkDataInfo.UpdatedAt;

            if (!needsUpdate)
            {
                _logger.LogInformation("Cards are up to date. Last import: {LastImport}", 
                    lastImport!.ImportedAt);
                return new ImportResult 
                { 
                    Success = true, 
                    Message = "Cards are already up to date",
                    CardsProcessed = 0,
                    IsUpdate = false
                };
            }

            _logger.LogInformation(
                isFirstImport 
                    ? "No existing cards found. Performing initial import..." 
                    : "New cards available. Performing update...");

            // Download and process cards
            var cardsProcessed = await DownloadAndImportCardsAsync(
                bulkDataInfo, 
                isFirstImport,
                cancellationToken);

            // Save import metadata
            var metadata = new ImportMetadata
            {
                Type = "scryfall_oracle_cards",
                UpdatedAt = bulkDataInfo.UpdatedAt,
                ImportedAt = DateTime.UtcNow,
                DownloadUri = bulkDataInfo.DownloadUri
            };
            _context.ImportMetadata.Add(metadata);
            await _context.SaveChangesAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Import completed successfully. Processed {Count} cards in {Duration:F2} seconds",
                cardsProcessed, duration.TotalSeconds);

            return new ImportResult
            {
                Success = true,
                Message = $"Successfully imported {cardsProcessed} cards",
                CardsProcessed = cardsProcessed,
                Duration = duration,
                IsUpdate = !isFirstImport
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Scryfall import");
            return new ImportResult 
            { 
                Success = false, 
                Message = $"Import failed: {ex.Message}" 
            };
        }
    }

    private async Task<ScryfallBulkData?> GetBulkDataInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching bulk data info from: {Url}", BULK_DATA_API);
            
            var client = _httpClientFactory.CreateClient();
            
            // Add required User-Agent header (Scryfall API requirement)
            client.DefaultRequestHeaders.Add("User-Agent", "MTGMullagain/1.0 (Card Import Service)");
            
            // First get the raw response to debug
            var response = await client.GetAsync(BULK_DATA_API, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Raw Scryfall response (first 500 chars): {Response}", 
                responseString.Length > 500 ? responseString.Substring(0, 500) : responseString);
            
            // Now try to deserialize
            var bulkDataResponse = JsonSerializer.Deserialize<ScryfallBulkDataResponse>(
                responseString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (bulkDataResponse?.Data == null || !bulkDataResponse.Data.Any())
            {
                _logger.LogWarning("No bulk data found in Scryfall response");
                return null;
            }

            _logger.LogInformation("Found {Count} bulk data types", bulkDataResponse.Data.Count);
            
            var oracleCards = bulkDataResponse.Data.FirstOrDefault(d => d.Type == ORACLE_CARDS_TYPE);
            
            if (oracleCards == null)
            {
                _logger.LogWarning("Oracle cards not found. Available types: {Types}", 
                    string.Join(", ", bulkDataResponse.Data.Select(d => d.Type)));
            }
            else
            {
                _logger.LogInformation("Found oracle cards: {Name}, size: {Size} MB, updated: {Updated}",
                    oracleCards.Name, oracleCards.Size / 1024.0 / 1024.0, oracleCards.UpdatedAt);
            }

            return oracleCards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bulk data info from Scryfall. Exception type: {Type}, Message: {Message}", 
                ex.GetType().Name, ex.Message);
            return null;
        }
    }

    private async Task<int> DownloadAndImportCardsAsync(
        ScryfallBulkData bulkData,
        bool isFirstImport,
        CancellationToken cancellationToken)
    {
        var cacheFile = Path.Combine(CACHE_DIR, $"oracle_cards_{bulkData.UpdatedAt:yyyyMMdd_HHmmss}.json");
        
        // Check if we have a cached version
        if (!File.Exists(cacheFile))
        {
            _logger.LogInformation("Downloading bulk data ({Size:F2} MB)...", 
                bulkData.Size / 1024.0 / 1024.0);
            
            await DownloadBulkDataAsync(bulkData.DownloadUri, cacheFile, cancellationToken);
            
            // Clean up old cache files
            CleanupOldCacheFiles(bulkData.UpdatedAt);
        }
        else
        {
            _logger.LogInformation("Using cached bulk data file: {File}", cacheFile);
        }

        // Process the cards
        return await ProcessCardsFromFileAsync(cacheFile, isFirstImport, cancellationToken);
    }

    private async Task DownloadBulkDataAsync(
        string uri, 
        string destinationPath, 
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "MTGMullagain/1.0 (Card Import Service)");
        
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        
        await contentStream.CopyToAsync(fileStream, cancellationToken);
        
        _logger.LogInformation("Download completed: {Path}", destinationPath);
    }

    private async Task<int> ProcessCardsFromFileAsync(
        string filePath,
        bool isFirstImport,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing cards from file...");
        
        var cardsProcessed = 0;
        var batchSize = 500; // Process in batches to manage memory
        var batch = new List<Card>();

        await using var fileStream = File.OpenRead(filePath);
        var cards = JsonSerializer.DeserializeAsyncEnumerable<ScryfallCard>(
            fileStream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        await foreach (var scryfallCard in cards.WithCancellation(cancellationToken))
        {
            if (scryfallCard == null) continue;

            try
            {
                var card = MapScryfallCardToEntity(scryfallCard);
                batch.Add(card);

                if (batch.Count >= batchSize)
                {
                    await SaveBatchAsync(batch, isFirstImport, cancellationToken);
                    cardsProcessed += batch.Count;
                    batch.Clear();
                    
                    _logger.LogInformation("Processed {Count} cards...", cardsProcessed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing card: {CardName}", scryfallCard.Name);
            }
        }

        // Save remaining cards
        if (batch.Count > 0)
        {
            await SaveBatchAsync(batch, isFirstImport, cancellationToken);
            cardsProcessed += batch.Count;
        }

        return cardsProcessed;
    }

    private async Task SaveBatchAsync(
        List<Card> cards, 
        bool isFirstImport, 
        CancellationToken cancellationToken)
    {
        if (isFirstImport)
        {
            // For initial import, just add all cards
            await _context.Cards.AddRangeAsync(cards, cancellationToken);
        }
        else
        {
            // For updates, check which cards are new
            var existingIds = await _context.Cards
                .Where(c => cards.Select(card => card.Id).Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var newCards = cards.Where(c => !existingIds.Contains(c.Id)).ToList();
            
            if (newCards.Any())
            {
                await _context.Cards.AddRangeAsync(newCards, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private Card MapScryfallCardToEntity(ScryfallCard scryfallCard)
    {
        return new Card
        {
            Id = Guid.Parse(scryfallCard.OracleId),
            Name = scryfallCard.Name,
            ManaCost = scryfallCard.ManaCost ?? string.Empty,
            Cmc = (int)(scryfallCard.Cmc ?? 0),
            Type = scryfallCard.TypeLine ?? string.Empty,
            OracleText = scryfallCard.OracleText ?? string.Empty,
            PowerToughness = scryfallCard.Power != null && scryfallCard.Toughness != null
                ? $"{scryfallCard.Power}/{scryfallCard.Toughness}"
                : null,
            Colors = scryfallCard.Colors?.ToArray() ?? Array.Empty<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void CleanupOldCacheFiles(DateTime currentUpdate)
    {
        try
        {
            var cacheFiles = Directory.GetFiles(CACHE_DIR, "oracle_cards_*.json");
            foreach (var file in cacheFiles)
            {
                var fileInfo = new FileInfo(file);
                // Keep files from the last 7 days
                if (fileInfo.CreationTimeUtc < currentUpdate.AddDays(-7))
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted old cache file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up cache files");
        }
    }
}

// DTOs
public record ScryfallBulkDataResponse(
    [property: JsonPropertyName("data")] List<ScryfallBulkData> Data
);

public record ScryfallBulkData(
    [property: JsonPropertyName("object")] string Object,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("download_uri")] string DownloadUri,
    [property: JsonPropertyName("content_type")] string ContentType,
    [property: JsonPropertyName("content_encoding")] string ContentEncoding
);

public record ScryfallCard(
    [property: JsonPropertyName("oracle_id")] string OracleId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mana_cost")] string? ManaCost,
    [property: JsonPropertyName("cmc")] double? Cmc,
    [property: JsonPropertyName("type_line")] string? TypeLine,
    [property: JsonPropertyName("oracle_text")] string? OracleText,
    [property: JsonPropertyName("power")] string? Power,
    [property: JsonPropertyName("toughness")] string? Toughness,
    [property: JsonPropertyName("colors")] List<string>? Colors
);

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CardsProcessed { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsUpdate { get; set; }
}

