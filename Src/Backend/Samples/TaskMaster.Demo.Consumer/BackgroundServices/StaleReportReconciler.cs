using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.Demo.Consumer.Configs;
using TaskMaster.Demo.Consumer.Reports;

namespace TaskMaster.Demo.Consumer.BackgroundServices;

public sealed class StaleReportReconciler : BackgroundService
{
    private const int MaxRowsPerSweep = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DemoConsumerOptions _options;
    private readonly ILogger<StaleReportReconciler> _logger;

    public StaleReportReconciler(
        IServiceScopeFactory scopeFactory,
        IOptions<DemoConsumerOptions> options,
        ILogger<StaleReportReconciler> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.StaleSweepMinutes));

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Never let one bad sweep kill the loop.
                _logger.LogError(ex, "Stale report sweep failed; retrying on the next tick");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-_options.StaleReportTimeoutMinutes);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReportRepository>();

        var stale = await repository.GetStaleRunningAsync(cutoff, MaxRowsPerSweep, cancellationToken);
        if (stale.Count == 0) return;

        var failed = 0;
        foreach (var row in stale)
        {
            try
            {
                await repository.MarkFailedAsync(
                    row.Id,
                    $"Abandoned: no progress for more than {_options.StaleReportTimeoutMinutes} minutes. The worker likely stopped mid-job.",
                    DateTimeOffset.UtcNow,
                    cancellationToken);

                failed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark stale report {ReportId}; will retry on the next sweep", row.Id);
            }
        }

        if (failed > 0)
        {
            _logger.LogWarning("Marked {Count} abandoned report(s) as failed (no progress since {Cutoff:o})", failed, cutoff);
        }
    }
}
