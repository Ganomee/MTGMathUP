using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MtgMullagain.Infrastructure.Services;
using MtgMullagain.Infrastructure;

namespace MtgMullagain.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly ScryfallImportService _importService;
    private readonly MtgMullagainDbContext _context;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        ScryfallImportService importService,
        MtgMullagainDbContext context,
        ILogger<ImportController> logger)
    {
        _importService = importService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger a card import from Scryfall
    /// </summary>
    [HttpPost("scryfall")]
    public async Task<ActionResult<ImportResult>> TriggerImport(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual import triggered via API");
            var result = await _importService.ImportCardsAsync(cancellationToken);
            
            return Ok(new
            {
                result.Success,
                result.Message,
                result.CardsProcessed,
                Duration = result.Duration.TotalSeconds,
                result.IsUpdate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual import");
            return StatusCode(500, new { error = "Import failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Get import status and history
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetImportStatus()
    {
        try
        {
            var cardCount = await _context.Cards.CountAsync();
            var lastImport = await _context.ImportMetadata
                .Where(m => m.Type == "scryfall_oracle_cards")
                .OrderByDescending(m => m.ImportedAt)
                .FirstOrDefaultAsync();

            var importHistory = await _context.ImportMetadata
                .Where(m => m.Type == "scryfall_oracle_cards")
                .OrderByDescending(m => m.ImportedAt)
                .Take(10)
                .Select(m => new
                {
                    m.Id,
                    m.ImportedAt,
                    m.UpdatedAt,
                    m.DownloadUri
                })
                .ToListAsync();

            return Ok(new
            {
                cardCount,
                lastImport = lastImport != null ? new
                {
                    lastImport.ImportedAt,
                    lastImport.UpdatedAt,
                    lastImport.DownloadUri
                } : null,
                importHistory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching import status");
            return StatusCode(500, new { error = "Failed to fetch import status" });
        }
    }
}

