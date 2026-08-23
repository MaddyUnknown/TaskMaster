using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.Library.Common.Constants;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Models.Auth;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Common.Models.Workers;
using TaskMaster.Library.Consumer;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.DependencyInjection;
using TaskMaster.Library.Consumer.Factories;
using TaskMaster.Library.Consumer.Interfaces;
using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Test.UnitTests.ConsumerTests;

public class TaskWorkerTests
{
    private Mock<IApiHttpClient> _httpClient = null!;
    private IOptions<TaskMasterConsumerOptions> _options = null!;

    [SetUp]
    public void SetupMock()
    {
        _httpClient = new(MockBehavior.Strict);
        _options = Options.Create(new TaskMasterConsumerOptions
        {
            PollingWaitIntervalMs = 10,
            MaxConcurrentHandlers = 1,
            ResultFlushIntervalMs = 10,
            MaxResultRetries = 0
        });
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_httpClient.Object);
        services.AddSingleton(_options);
        return services.BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProviderWithHandler(object handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_httpClient.Object);
        services.AddSingleton(handler.GetType(), handler);
        services.AddSingleton(_options);
        return services.BuildServiceProvider();
    }

    private void SetupWorkerRegistration(Guid workerId)
    {
        _httpClient
            .Setup(c => c.RegisterWorker(It.IsAny<RegisterWorker>()))
            .ReturnsAsync(new RegisterWorkerResponse
            {
                WorkerDetails = new WorkerDetails
                {
                    WorkerId = workerId,
                    WorkerName = "test-worker",
                    Status = "Active"
                },
                HeartBeatIntervalSeconds = 3600
            });

        _httpClient
            .Setup(c => c.RemoveWorker(workerId))
            .ReturnsAsync(new WorkerDetails());
    }

    private void SetupPullJobsBatch(Guid workerId, JobDetails? singleJob, int remainingCount = 0)
    {
        IEnumerable<JobDetails> response = singleJob != null ? [singleJob] : Enumerable.Empty<JobDetails>();

        _httpClient
            .Setup(c => c.PullJobs(workerId, It.IsAny<int>()))
            .ReturnsAsync(response);
    }

    private void SetupSubmitBatchResult()
    {
        _httpClient
            .Setup(c => c.BulkUpdateJobStatus(It.IsAny<BulkUpdateJobStatusRequest>()))
            .ReturnsAsync(new BulkUpdateJobStatusResponse
            {
                UpdatedRecordCount = 1,
                Errors = Enumerable.Empty<UpdateJobStatusErrorResponse>()
            });
    }

    private (TaskWorkerFactory Factory, IWorker Worker) CreateWorker(
        string name, Action<WorkerConfiguration>? configure = null)
    {
        var factory = new TaskWorkerFactory(BuildServiceProvider());
        var worker = factory.CreateWorker(name, configure ?? (_ => { }));
        return (factory, worker);
    }

    [Test]
    public void AddTaskMasterConsumer_WhenCalled_ShouldRegisterWorkerFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        var apiHttpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);

        apiHttpClient.Setup(x => x.GetAuthConfig()).ReturnsAsync(new AuthConfigDetails
        {
            Mode = EnumConstants.AuthModeEnum.None
        });

        services.AddSingleton<IApiHttpClient>(apiHttpClient.Object);


        services.AddTaskMasterConsumer(options => options.ApiBaseUrl = "https://taskmaster.test/");
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory = serviceProvider.GetService<IWorkerFactory>();

        // Assert
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void WorkerFactory_WhenStandaloneServicesNotInitialised_ShouldThrow()
    {
        // Assert
        Assert.Throws<InvalidOperationException>(() => _ = new TaskWorkerFactory());
    }

    [Test]
    public async Task CreateWorker_WithHandlerConfig_ShouldDeriveCapabilitiesFromAttribute()
    {
        // Arrange
        var workerId = Guid.NewGuid();

        RegisterWorker? capturedRegistration = null;
        _httpClient
            .Setup(c => c.RegisterWorker(It.IsAny<RegisterWorker>()))
            .Returns((RegisterWorker rw) =>
            {
                capturedRegistration = rw;
                return Task.FromResult(new RegisterWorkerResponse
                {
                    WorkerDetails = new WorkerDetails { WorkerId = workerId, WorkerName = "test-worker", Status = "Active" },
                    HeartBeatIntervalSeconds = 3600
                });
            });

        _httpClient
            .Setup(c => c.RemoveWorker(workerId))
            .ReturnsAsync(new WorkerDetails());

        _httpClient
            .Setup(c => c.PullJobs(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<JobDetails>());

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        Assert.That(capturedRegistration, Is.Not.Null);
        Assert.That(capturedRegistration!.JobTypeCapabilities, Has.Exactly(1).Items);
        var capability = capturedRegistration.JobTypeCapabilities.Single();
        Assert.Multiple(() =>
        {
            Assert.That(capability.Name, Is.EqualTo("email"));
            Assert.That(capability.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void CreateWorker_WhenPayloadMissingAttribute_ShouldThrow()
    {
        // Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            var (_, _) = CreateWorker("test-worker", cfg =>
            {
                cfg.Handle<UnattributedPayload, UnattributedHandler>();
            });
        });
    }

    [Test]
    public async Task RunAsync_WhenHandlerSucceeds_ShouldCompleteJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "email", Version = 1 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);
        SetupPullJobsBatch(workerId, new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "queued" });
        SetupSubmitBatchResult();

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        _httpClient.Verify(c => c.BulkUpdateJobStatus(
            It.Is<BulkUpdateJobStatusRequest>(r =>
                r.WorkerId == workerId &&
                r.JobStatuses.Count == 1 &&
                r.JobStatuses[0].JobId == jobId &&
                r.JobStatuses[0].Status == "completed")),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsync_WhenHandlerSucceeds_ShouldPassDeserializedPayload()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "email", Version = 1 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);
        SetupPullJobsBatch(workerId, new JobDetails
        {
            JobId = jobId,
            JobType = jobType,
            Payload = """{"Email":"hello","Priority":42}""",
            Status = "Queued"
        });
        SetupSubmitBatchResult();

        var handler = new EmailCapturingHandler();
        var factory = new TaskWorkerFactory(BuildServiceProviderWithHandler(handler));
        var worker = factory.CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailCapturingHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        Assert.That(handler.ReceivedPayload, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(handler.ReceivedPayload!.Email, Is.EqualTo("hello"));
            Assert.That(handler.ReceivedPayload.Priority, Is.EqualTo(42));
        });
    }

    [Test]
    public async Task RunAsync_WhenHandlerThrows_ShouldFailJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "email", Version = 1 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);
        SetupPullJobsBatch(workerId, new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "Queued" });
        SetupSubmitBatchResult();

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailFailingHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        _httpClient.Verify(c => c.BulkUpdateJobStatus(
            It.Is<BulkUpdateJobStatusRequest>(r =>
                r.WorkerId == workerId &&
                r.JobStatuses.Count == 1 &&
                r.JobStatuses[0].JobId == jobId &&
                r.JobStatuses[0].Status == "failed")),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsync_WhenNoJobThenJobAvailable_ShouldPollUntilJobFound()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "email", Version = 1 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        var callCount = 0;
        _httpClient
            .Setup(c => c.PullJobs(workerId, It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? Enumerable.Empty<JobDetails>()
                    : [new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "Queued" }];
            });

        _httpClient
            .Setup(c => c.BulkUpdateJobStatus(It.IsAny<BulkUpdateJobStatusRequest>()))
            .ReturnsAsync(new BulkUpdateJobStatusResponse
            {
                UpdatedRecordCount = 1,
                Errors = Enumerable.Empty<UpdateJobStatusErrorResponse>()
            });

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        Assert.That(callCount, Is.GreaterThanOrEqualTo(2));
        _httpClient.Verify(c => c.BulkUpdateJobStatus(
            It.Is<BulkUpdateJobStatusRequest>(r => r.JobStatuses.Count == 1 && r.JobStatuses[0].Status == "completed")),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsync_WhenPullReturnsNull_ShouldContinueLoop()
    {
        // Arrange
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        _httpClient
            .Setup(c => c.PullJobs(workerId, It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<JobDetails>());

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        _httpClient.Verify(c => c.PullJobs(workerId, It.IsAny<int>()), Times.AtLeast(2));
    }

    [Test]
    public async Task RunAsync_WhenJobTypeNotInHandlerMap_ShouldFailJob()
    {
        // Arrange
        var jobType = new JobTypeRef { Name = "unknown-type", Version = 99 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        _httpClient
            .Setup(c => c.PullJobs(workerId, It.IsAny<int>()))
            .ReturnsAsync([new JobDetails { JobId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = "Queued" }]);

        _httpClient
            .Setup(c => c.BulkUpdateJobStatus(It.IsAny<BulkUpdateJobStatusRequest>()))
            .ReturnsAsync(new BulkUpdateJobStatusResponse
            {
                UpdatedRecordCount = 1,
                Errors = Enumerable.Empty<UpdateJobStatusErrorResponse>()
            });

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        _httpClient.Verify(c => c.BulkUpdateJobStatus(
            It.Is<BulkUpdateJobStatusRequest>(r =>
                r.JobStatuses.Count == 1 &&
                r.JobStatuses[0].Status == "failed" 
            )),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsync_WhenPullFails_ShouldRetryNextPollCycle()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "email", Version = 1 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        var callCount = 0;
        _httpClient
            .Setup(c => c.PullJobs(workerId, It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => throw new InvalidOperationException("API down"),
                    2 => throw new InvalidOperationException("API still down"),
                    _ => [new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "Queued" }]
                };
            });

        _httpClient
            .Setup(c => c.BulkUpdateJobStatus(It.IsAny<BulkUpdateJobStatusRequest>()))
            .ReturnsAsync(new BulkUpdateJobStatusResponse
            {
                UpdatedRecordCount = 1,
                Errors = Enumerable.Empty<UpdateJobStatusErrorResponse>()
            });

        var options = Options.Create(new TaskMasterConsumerOptions
        {
            PollingWaitIntervalMs = 10,
            MaxConcurrentHandlers = 1,
            ResultFlushIntervalMs = 10,
            MaxResultRetries = 0,
            ReporterBackoffBaseMs = 10
        });

        var services = new ServiceCollection();
        services.AddSingleton(_httpClient.Object);
        services.AddSingleton(options);
        using var sp = services.BuildServiceProvider();

        var factory = new TaskWorkerFactory(sp);
        var worker = factory.CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(500);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        Assert.That(callCount, Is.GreaterThanOrEqualTo(3));
        _httpClient.Verify(c => c.BulkUpdateJobStatus(
            It.Is<BulkUpdateJobStatusRequest>(r => r.JobStatuses[0].Status == "completed")),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsync_WhenBatchResultSyncFails_ShouldRetry()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "email", Version = 1 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        var pullCount = 0;
        _httpClient
            .Setup(c => c.PullJobs(workerId, It.IsAny<int>()))
            .ReturnsAsync(() =>
            {
                pullCount++;
                return pullCount == 1
                    ? [new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "Queued" }]
                    : Enumerable.Empty<JobDetails>();
            });

        var submitCallCount = 0;
        _httpClient
            .Setup(c => c.BulkUpdateJobStatus(It.IsAny<BulkUpdateJobStatusRequest>()))
            .ReturnsAsync(() =>
            {
                submitCallCount++;
                return submitCallCount switch
                {
                    1 => throw new InvalidOperationException("API down"),
                    _ => new BulkUpdateJobStatusResponse
                    {
                        UpdatedRecordCount = 1,
                        Errors = Enumerable.Empty<UpdateJobStatusErrorResponse>()
                    }
                };
            });

        var options = Options.Create(new TaskMasterConsumerOptions
        {
            PollingWaitIntervalMs = 10,
            MaxConcurrentHandlers = 1,
            ResultFlushIntervalMs = 10,
            MaxResultRetries = 2,
            ReporterBackoffBaseMs = 10
        });

        var services = new ServiceCollection();
        services.AddSingleton(_httpClient.Object);
        services.AddSingleton(options);
        using var sp = services.BuildServiceProvider();

        var factory = new TaskWorkerFactory(sp);
        var worker = factory.CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(500);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert - first call fails, second succeeds
        Assert.That(submitCallCount, Is.EqualTo(2));
    }

    [Test]
    public async Task DisposeAsync_ShouldRemoveWorker()
    {
        // Arrange
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        _httpClient
            .Setup(c => c.PullJobs(workerId, It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<JobDetails>());

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        _httpClient.Verify(c => c.RemoveWorker(workerId), Times.Once);
    }
}
