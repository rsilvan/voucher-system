using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoucherSystem.Application;
using VoucherSystem.Domain;

namespace VoucherSystem.Workers;

public class PromotionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PromotionWorker> _logger;

    public PromotionWorker(IServiceScopeFactory scopeFactory, ILogger<PromotionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PromotionWorker started (v2 — monitoring voucher batches)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PromotionWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("PromotionWorker stopped");
    }

    private async Task ProcessPendingBatchesAsync(CancellationToken ct)
    {
        // Placeholder: async batch processing will be implemented in a future iteration.
        // The voucher batch generation is currently synchronous.
        await Task.CompletedTask;
    }
}
