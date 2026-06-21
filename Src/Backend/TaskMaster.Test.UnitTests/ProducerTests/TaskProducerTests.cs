using Microsoft.Extensions.DependencyInjection;
using Moq;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Producer;
using TaskMaster.Library.Producer.DependencyInjection;
using TaskMaster.Library.Producer.Interfaces;
using TaskMaster.Library.Producer.Producers;
using TaskMaster.Test.UnitTests.Data;

namespace TaskMaster.Test.UnitTests.ProducerTests;

public class TaskProducerTests
{
    private Mock<IJobTypeSchemaRegistry> _schemaRegistry = null!;
    private Mock<IApiHttpClient> _httpClient = null!;

    private TaskProducer CreateProducer() =>
        new(_schemaRegistry.Object, _httpClient.Object);

    [SetUp]
    public void SetupMock()
    {
        _schemaRegistry = new(MockBehavior.Strict);
        _httpClient = new(MockBehavior.Strict);
    }

    [Test]
    public void AddTaskMasterProducer_WhenCalled_ShouldRegisterProducerDependencies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTaskMasterProducer(options => options.ApiBaseUrl = "https://taskmaster.test/");
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var producer = serviceProvider.GetService<IProducer>();
        Assert.That(producer, Is.Not.Null);
    }

    [Test]
    public void TaskProducer_WhenStandaloneServicesAreNotInitialised_ShouldThrow()
    {
        // Assert
        Assert.Throws<Exception>(() => new TaskProducer());
    }

    [Test]
    public void Initialise_WhenStandaloneMode_ShouldAllowParameterlessTaskProducer()
    {
        // Act
        using var taskMasterProducer = TaskMasterProducer.Initialise(options => options.ApiBaseUrl = "https://taskmaster.test/");
        var producer = new TaskProducer();

        // Assert
        Assert.That(producer, Is.Not.Null);
    }

    [Test]
    public async Task ProduceAsync_WhenPayloadMatchesSchema_ShouldCreateJob()
    {
        // Arrange
        CreateJobRequest? createdJob = null;

        _schemaRegistry
            .Setup(sr => sr.GetByJobTypeNameAndVersion(ServiceTestData.EmailJobTypeRef.Name, ServiceTestData.EmailJobTypeRef.Version))
            .ReturnsAsync(ServiceTestData.Emailv1Schema);

        _httpClient
            .Setup(hc => hc.CreateJob(It.IsAny<CreateJobRequest>()))
            .Callback<CreateJobRequest>(c =>
            {
                createdJob = c;
            })
            .ReturnsAsync(new JobDetails());

        // Act
        await CreateProducer().ProduceAsync(new EmailPayload("person@example.com", 2));

        // Assert
        Assert.That(createdJob, Is.Not.Null);
        Assert.That(createdJob!.JobType.Name, Is.EqualTo("email"));
        Assert.That(createdJob.JobType.Version, Is.EqualTo(1));
        Assert.That(createdJob.Payload, Does.Contain("person@example.com"));
    }

    [Test]
    public void ProduceAsync_WhenPayloadDoesNotMatchSchema_ShouldThrowAndNotCreateJob()
    {
        // Arrange
        _schemaRegistry
            .Setup(sr => sr.GetByJobTypeNameAndVersion(ServiceTestData.EmailJobTypeRef.Name, ServiceTestData.EmailJobTypeRef.Version))
            .ReturnsAsync(ServiceTestData.Emailv1Schema);

        _httpClient
            .Setup(hc => hc.CreateJob(It.IsAny<CreateJobRequest>()));

        // Act
        var act = () => CreateProducer().ProduceAsync(new EmailPayload("not-an-email", 0));

        // Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
        _httpClient.Verify(sr => sr.CreateJob(It.IsAny<CreateJobRequest>()), Times.Never);
    }
}
