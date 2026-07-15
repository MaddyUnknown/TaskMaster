using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.API.BackgroundServices;
using TaskMaster.API.Configs;
using TaskMaster.API.Interfaces.Repositories;

namespace TaskMaster.Test.UnitTests.APITests;

public class WorkerExpiryBackgroundServiceTests
{
    private Mock<IServiceScopeFactory> _scopeFactory = null!;
    private Mock<IServiceScope> _scope = null!;
    private Mock<IServiceProvider> _serviceProvider = null!;
    private Mock<IWorkerRepository> _workerRepository = null!;
    private Mock<IJobRepository> _jobRepository = null!;
    private IOptions<WorkerConfig> _workerConfig = null!;
    private Mock<ILogger<WorkerExpiryBackgroundService>> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _workerRepository = new Mock<IWorkerRepository>(MockBehavior.Strict);
        _jobRepository = new Mock<IJobRepository>(MockBehavior.Strict);
        _serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        _serviceProvider
            .Setup(sp => sp.GetService(typeof(IWorkerRepository)))
            .Returns(_workerRepository.Object);
        _serviceProvider
            .Setup(sp => sp.GetService(typeof(IJobRepository)))
            .Returns(_jobRepository.Object);

        _scope = new Mock<IServiceScope>(MockBehavior.Strict);
        _scope.Setup(s => s.ServiceProvider).Returns(_serviceProvider.Object);
        _scope.Setup(s => s.Dispose());

        _scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        _scopeFactory
            .Setup(f => f.CreateScope())
            .Returns(_scope.Object);

        _workerConfig = Options.Create(new WorkerConfig
        {
            HeartBeatIntervalSeconds = 10,
            WorkerExpiryIntervalSeconds = 30,
            WorkerExpiryCheckIntervalSeconds = 1
        });

        _logger = new Mock<ILogger<WorkerExpiryBackgroundService>>();
    }

    [Test]
    public async Task ExecuteAsync_WhenExpiredWorkersExist_ShouldDeactivateAndUnassign()
    {
        var processed = new TaskCompletionSource<object>();
        _workerRepository
            .Setup(r => r.DeactivateExpiredWorkersAsync())
            .ReturnsAsync(3)
            .Callback(() => processed.TrySetResult(null!));
        _jobRepository
            .Setup(r => r.UnassignJobsForInactiveWorkersAsync())
            .ReturnsAsync(5);

        var service = new WorkerExpiryBackgroundService(_scopeFactory.Object, _workerConfig, _logger.Object);

        await service.StartAsync(CancellationToken.None);
        await processed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        _workerRepository.Verify(r => r.DeactivateExpiredWorkersAsync(), Times.AtLeastOnce());
        _jobRepository.Verify(r => r.UnassignJobsForInactiveWorkersAsync(), Times.AtLeastOnce());
    }

    [Test]
    public async Task ExecuteAsync_WhenNoExpiredWorkers_ShouldSkipUnassign()
    {
        var processed = new TaskCompletionSource<object>();
        _workerRepository
            .Setup(r => r.DeactivateExpiredWorkersAsync())
            .ReturnsAsync(0)
            .Callback(() => processed.TrySetResult(null!));

        var service = new WorkerExpiryBackgroundService(_scopeFactory.Object, _workerConfig, _logger.Object);

        await service.StartAsync(CancellationToken.None);
        await processed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync(CancellationToken.None);

        _workerRepository.Verify(r => r.DeactivateExpiredWorkersAsync(), Times.AtLeastOnce());
        _jobRepository.Verify(r => r.UnassignJobsForInactiveWorkersAsync(), Times.Never);
    }
}
