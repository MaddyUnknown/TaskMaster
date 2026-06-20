using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Common.Models.Workers;
using TaskMaster.Library.Consumer;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.Consumers;
using TaskMaster.Library.Consumer.DependencyInjection;
using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Test.UnitTests.ConsumerTests;

public class TaskWorkerTests
{
    private class TestPayload
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Test]
    public void AddTaskMasterConsumer_WhenCalled_ShouldRegisterCommonServices()
    {
        var services = new ServiceCollection();

        services.AddTaskMasterConsumer(options => options.ApiBaseUrl = "https://taskmaster.test/");
        using var serviceProvider = services.BuildServiceProvider();

        var httpClient = serviceProvider.GetService<IApiHttpClient>();
        Assert.That(httpClient, Is.Not.Null);
    }

    [Test]
    public void TaskWorker_WhenStandaloneServicesNotInitialised_ShouldThrow()
    {
        Assert.Throws<Exception>(() => _ = new TaskWorker<TestPayload>("test-worker"));
    }

    [Test]
    public void Initialise_WhenStandaloneMode_ShouldAllowParameterlessTaskWorker()
    {
        using var consumer = TaskMasterConsumer.Initialise(options => options.ApiBaseUrl = "https://taskmaster.test/");
        var worker = new TaskWorker<TestPayload>("test-worker");

        Assert.That(worker, Is.Not.Null);
    }

    [Test]
    public async Task ConsumeAsync_WhenJobAvailable_ShouldReturnPopulatedResult()
    {
        var jobId = Guid.NewGuid();
        var jobType = new JobTypeRef { Name = "test", Version = 1 };
        var workerId = Guid.NewGuid();

        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);
        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var options = Options.Create(new TaskMasterConsumerOptions());

        SetupWorkerRegistration(httpClient, workerId);
        SetupPullJob(httpClient, new JobDetails
        {
            JobId = jobId,
            JobType = jobType,
            Payload = """{"Name":"hello","Value":42}""",
            Status = "Queued"
        });

        var worker = new TaskWorker<TestPayload>("test-worker", schemaRegistry.Object, httpClient.Object, options);

        var result = await worker.ConsumeAsync();
        await worker.DisposeAsync();

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.JobId, Is.EqualTo(jobId));
            Assert.That(result.JobType.Name, Is.EqualTo("test"));
            Assert.That(result.JobType.Version, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo("Queued"));
            Assert.That(result.Data.Name, Is.EqualTo("hello"));
            Assert.That(result.Data.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public async Task ConsumeAsync_WhenNoJobThenJobAvailable_ShouldPollUntilJobFound()
    {
        var workerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);
        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var options = Options.Create(new TaskMasterConsumerOptions
        {
            PollingWaitIntervalMs = 1
        });

        SetupWorkerRegistration(httpClient, workerId);

        var callCount = 0;
        httpClient
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? null
                    : new JobDetails { JobId = jobId, JobType = new JobTypeRef(), Payload = "{}", Status = "Queued" };
            });

        var worker = new TaskWorker<TestPayload>("test-worker", schemaRegistry.Object, httpClient.Object, options);

        var result = await worker.ConsumeAsync();
        await worker.DisposeAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.JobId, Is.EqualTo(jobId));
        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ConsumeAsync_WhenNoJobAndTimeoutElapses_ShouldReturnNull()
    {
        var workerId = Guid.NewGuid();

        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);
        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var options = Options.Create(new TaskMasterConsumerOptions
        {
            PollingWaitIntervalMs = 10,
            ConsumerWaitTimeoutMs = 100
        });

        SetupWorkerRegistration(httpClient, workerId);

        httpClient
            .Setup(c => c.PullJob(workerId))
            .ReturnsAsync((JobDetails?)null);

        var worker = new TaskWorker<TestPayload>("test-worker", schemaRegistry.Object, httpClient.Object, options);

        var result = await worker.ConsumeAsync();
        await worker.DisposeAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CompleteAsync_ShouldCallHttpClientCompleteJob()
    {
        var workerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var jobResult = new JobConsumeResult<TestPayload>
        {
            JobId = jobId,
            Data = new TestPayload(),
            JobType = new JobTypeRef(),
            Status = "Queued"
        };

        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);
        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var options = Options.Create(new TaskMasterConsumerOptions());

        SetupWorkerRegistration(httpClient, workerId);
        SetupPullJob(httpClient, new JobDetails { JobId = jobId, JobType = new JobTypeRef(), Payload = "{}", Status = "Queued" });

        httpClient
            .Setup(c => c.CompleteJob(It.IsAny<Guid>(), workerId))
            .ReturnsAsync(new JobDetails());

        var worker = new TaskWorker<TestPayload>("test-worker", schemaRegistry.Object, httpClient.Object, options);

        await worker.ConsumeAsync();
        await worker.CompleteAsync(jobResult);
        await worker.DisposeAsync();

        httpClient.Verify(c => c.CompleteJob(It.IsAny<Guid>(), workerId), Times.Once);
    }

    [Test]
    public async Task FailAsync_ShouldCallHttpClientFailJob()
    {
        var workerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var jobResult = new JobConsumeResult<TestPayload>
        {
            JobId = jobId,
            Data = new TestPayload(),
            JobType = new JobTypeRef(),
            Status = "Queued"
        };

        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);
        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var options = Options.Create(new TaskMasterConsumerOptions());

        SetupWorkerRegistration(httpClient, workerId);
        SetupPullJob(httpClient, new JobDetails { JobId = jobId, JobType = new JobTypeRef(), Payload = "{}", Status = "Queued" });

        httpClient
            .Setup(c => c.FailJob(It.IsAny<Guid>(), workerId))
            .ReturnsAsync(new JobDetails());

        var worker = new TaskWorker<TestPayload>("test-worker", schemaRegistry.Object, httpClient.Object, options);

        await worker.ConsumeAsync();
        await worker.FailAsync(jobResult);
        await worker.DisposeAsync();

        httpClient.Verify(c => c.FailJob(It.IsAny<Guid>(), workerId), Times.Once);
    }

    [Test]
    public async Task DisposeAsync_ShouldRemoveWorker()
    {
        var workerId = Guid.NewGuid();

        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);
        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var options = Options.Create(new TaskMasterConsumerOptions());

        SetupWorkerRegistration(httpClient, workerId);
        SetupPullJob(httpClient, new JobDetails { JobId = Guid.NewGuid(), JobType = new JobTypeRef(), Payload = "{}", Status = "Queued" });

        httpClient
            .Setup(c => c.RemoveWorker(workerId))
            .ReturnsAsync(new WorkerDetails());

        var worker = new TaskWorker<TestPayload>("test-worker", schemaRegistry.Object, httpClient.Object, options);

        await worker.ConsumeAsync();
        await worker.DisposeAsync();

        httpClient.Verify(c => c.RemoveWorker(workerId), Times.Once);
    }

    private static void SetupWorkerRegistration(Mock<IApiHttpClient> httpClient, Guid workerId)
    {
        httpClient
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

        httpClient
            .Setup(c => c.RemoveWorker(workerId))
            .ReturnsAsync(new WorkerDetails());
    }

    private static void SetupPullJob(Mock<IApiHttpClient> httpClient, JobDetails? job)
    {
        httpClient
            .Setup(c => c.PullJob(It.IsAny<Guid>()))
            .ReturnsAsync(job);
    }
}
