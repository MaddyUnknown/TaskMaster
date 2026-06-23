using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.Library.Common.Interfaces.HttpClients;
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
            PollingWaitIntervalMs = 10
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

    private void SetupPullJob(JobDetails? job)
    {
        _httpClient
            .Setup(c => c.PullJob(It.IsAny<Guid>()))
            .ReturnsAsync(job);
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
    public void Initialise_WhenStandaloneMode_ShouldAllowWorkerFactory()
    {
        // Arrange
        using var consumer = TaskMasterConsumer.Initialise(options => options.ApiBaseUrl = "https://taskmaster.test/");

        // Act
        var factory = new TaskWorkerFactory();

        // Assert
        Assert.That(factory, Is.Not.Null);
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
            .Setup(c => c.PullJob(It.IsAny<Guid>()))
            .ReturnsAsync((JobDetails?)null);

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }
        await worker.DisposeAsync();

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

        var pullCount = 0;
        _httpClient
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync(() =>
            {
                pullCount++;
                if (pullCount == 1)
                    return new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "Queued" };
                return null;
            });

        _httpClient
            .Setup(c => c.CompleteJob(jobId, workerId))
            .ReturnsAsync(new JobDetails());

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }
        await worker.DisposeAsync();

        // Assert
        _httpClient.Verify(c => c.CompleteJob(jobId, workerId), Times.Once);
        _httpClient.Verify(c => c.FailJob(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_WhenHandlerSucceeds_ShouldPassDeserializedPayload()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "email", Version = 1 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);
        SetupPullJob(new JobDetails
        {
            JobId = jobId,
            JobType = jobType,
            Payload = """{"Email":"hello","Priority":42}""",
            Status = "Queued"
        });
        _httpClient
            .Setup(c => c.CompleteJob(jobId, workerId))
            .ReturnsAsync(new JobDetails());

        var handler = new EmailCapturingHandler();
        var factory = new TaskWorkerFactory(BuildServiceProviderWithHandler(handler));
        var worker = factory.CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailCapturingHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }
        await worker.DisposeAsync();

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

        var pullCount = 0;
        _httpClient
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync(() =>
            {
                pullCount++;
                if (pullCount == 1)
                    return new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "Queued" };
                return null;
            });

        _httpClient
            .Setup(c => c.FailJob(jobId, workerId))
            .ReturnsAsync(new JobDetails());

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailFailingHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }
        await worker.DisposeAsync();

        // Assert
        _httpClient.Verify(c => c.FailJob(jobId, workerId), Times.Once);
        _httpClient.Verify(c => c.CompleteJob(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
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
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? null
                    : new JobDetails { JobId = jobId, JobType = jobType, Payload = "{}", Status = "Queued" };
            });

        _httpClient
            .Setup(c => c.CompleteJob(jobId, workerId))
            .ReturnsAsync(new JobDetails());

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }
        await worker.DisposeAsync();

        // Assert
        Assert.That(callCount, Is.GreaterThanOrEqualTo(2));
        _httpClient.Verify(c => c.CompleteJob(jobId, workerId), Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsync_WhenPullReturnsNull_ShouldContinueLoop()
    {
        // Arrange
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        _httpClient
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync((JobDetails?)null);

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }
        await worker.DisposeAsync();

        // Assert
        _httpClient.Verify(c => c.PullJob(workerId), Times.AtLeast(2));
    }

    [Test]
    public async Task RunAsync_WhenJobTypeNotInHandlerMap_ShouldThrow()
    {
        // Arrange
        var jobType = new JobTypeRef { Name = "unknown-type", Version = 99 };
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        _httpClient
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync(new JobDetails { JobId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = "Queued" });

        // Act
        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Assert
        using var cts = new CancellationTokenSource();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await worker.RunAsync(cts.Token);
        });
        await worker.DisposeAsync();

        Assert.That(ex!.Message, Does.Contain("unknown-type"));
    }

    [Test]
    public async Task DisposeAsync_ShouldRemoveWorker()
    {
        // Arrange
        var workerId = Guid.NewGuid();

        SetupWorkerRegistration(workerId);

        _httpClient
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync((JobDetails?)null);

        var (_, worker) = CreateWorker("test-worker", cfg => cfg.Handle<EmailPayload, EmailHandler>());

        // Act
        using var cts = new CancellationTokenSource(200);
        try { await worker.RunAsync(cts.Token); } catch (OperationCanceledException) { }
        await worker.DisposeAsync();

        // Assert
        _httpClient.Verify(c => c.RemoveWorker(workerId), Times.Once);
    }
}
