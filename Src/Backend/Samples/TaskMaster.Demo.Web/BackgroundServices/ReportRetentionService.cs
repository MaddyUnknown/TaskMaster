using TaskMaster.Demo.Web.Configs;
using TaskMaster.Demo.Web.Reports;
using TaskMaster.Demo.Web.Storage;

namespace TaskMaster.Demo.Web.BackgroundServices;

/// <summary>
/// Enforces the demo's one-time result lifetime. The consumer writes the CSV to the
/// file store and marks the row Completed; this service removes both after
/// <c>Demo:ReportRetentionHours</c> (24 by default), so a report is a genuinely
/// short-lived artefact rather than accumulating in a sample app.
///
/// The file is deleted before the row, so a surviving row can never point at a missing
/// file. Failures on individual rows are logged and the sweep continues.
/// </summary>
public sealed class ReportRetentionService : BackgroundService
{
    private const int MaxRowsPerSweep = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DemoOptions _options;
    private readonly ILogger<ReportRetentionService> _logger;

    public ReportRetentionService(
        IServiceScopeFactory scopeFactory,
        DemoOptions options,
        ILogger<ReportRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.RetentionSweepMinutes));

        // Give the app time to finish starting before the first sweep.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

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
                _logger.LogError(ex, "Report retention sweep failed; retrying on the next tick");
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
        var cutoff = DateTimeOffset.UtcNow.AddHours(-Math.Max(1, _options.ReportRetentionHours));

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
        var fileStore = scope.ServiceProvider.GetRequiredService<IReportFileStore>();

        var expired = await repository.GetExpiredAsync(cutoff, MaxRowsPerSweep, cancellationToken);
        if (expired.Count == 0) return;

        var removed = 0;
        foreach (var row in expired)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(row.StorageKey))
                {
                    await fileStore.DeleteAsync(row.StorageKey, cancellationToken);
                }

                await repository.DeleteAsync(row.Id, cancellationToken);
                removed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove expired report {ReportId}; will retry on the next sweep", row.Id);
            }
        }

        if (removed > 0)
        {
            _logger.LogInformation("Retention sweep removed {Removed} report(s) submitted before {Cutoff:o}",
                removed, cutoff);
        }
    }
}
