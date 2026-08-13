using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Events;
using TaskMaster.API.Interfaces.Publisher;
using TaskMaster.API.Interfaces.Repositories;

namespace TaskMaster.API.BackgroundServices;

public class WorkerExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WorkerConfig> _workerConfig;
    private readonly ILogger<WorkerExpiryBackgroundService> _logger;

    public WorkerExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerConfig> workerConfig,
        ILogger<WorkerExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _workerConfig = workerConfig;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkIntervalSeconds = GetCheckIntervalSeconds();

        _logger.LogInformation("Worker expiry background service started. Checking every {Interval}s", checkIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredWorkersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired workers");
            }

            await Task.Delay(TimeSpan.FromSeconds(checkIntervalSeconds), stoppingToken);
        }
    }

    private int GetCheckIntervalSeconds()
    {
        var config = _workerConfig.Value;
        return (config.WorkerExpiryCheckIntervalSeconds > 0) ? config.WorkerExpiryCheckIntervalSeconds : Math.Max(1, config.WorkerExpiryIntervalSeconds / 2);
    }

    private async Task ProcessExpiredWorkersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var workerRepository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var expiredWorkers = await workerRepository.GetExpiredActiveWorkersAsync();

        var deactivatedCount = await workerRepository.DeactivateExpiredWorkersAsync();

        if (deactivatedCount > 0)
        {
            var unassignedCount = await jobRepository.UnassignJobsForInactiveWorkersAsync();
            _logger.LogInformation("Deactivated {DeactivatedCount} expired worker(s) and unassigned {UnassignedCount} job(s)", deactivatedCount, unassignedCount);

            foreach (var worker in expiredWorkers)
            {
                await eventPublisher.PublishAsync(new WorkerInactiveEvent
                {
                    WorkerId = worker.WorkerPublicId,
                    WorkerName = worker.WorkerName
                }, ct);
            }
        }
    }
}
