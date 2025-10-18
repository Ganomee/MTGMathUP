using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MtgMullagain.Infrastructure.Services;

namespace MtgMullagain.Api.Services;

/// <summary>
/// Background service that runs card imports on startup and daily
/// </summary>
public class CardImportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CardImportBackgroundService> _logger;
    private readonly TimeSpan _dailyInterval = TimeSpan.FromHours(24);

    public CardImportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CardImportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Card Import Background Service starting...");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        // Run initial import on startup
        await RunImportAsync(stoppingToken);

        // Schedule daily imports
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_dailyInterval, stoppingToken);
                await RunImportAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Card Import Background Service is stopping.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Card Import Background Service loop");
                // Continue running even if one iteration fails
            }
        }
    }

    private async Task RunImportAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting scheduled card import...");

            using var scope = _serviceProvider.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<ScryfallImportService>();

            var result = await importService.ImportCardsAsync(cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Card import completed: {Message} ({Duration:F2}s)",
                    result.Message,
                    result.Duration.TotalSeconds);
            }
            else
            {
                _logger.LogWarning("Card import failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running card import");
        }
    }
}

