using Microsoft.Extensions.DependencyInjection;
using Moq;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Producer;
using TaskMaster.Library.Producer.Attributes;
using TaskMaster.Library.Producer.DependencyInjection;
using TaskMaster.Library.Producer.Interfaces;
using TaskMaster.Library.Producer.Producers;
using TaskMaster.Test.UnitTests.Data;

namespace TaskMaster.Test.UnitTests;

public class TaskProducerTests
{
    [Test]
    public void AddTaskMasterProducer_WhenCalled_ShouldRegisterProducerDependencies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTaskMasterProducer(options => options.ApiBaseUrl = "https://taskmaster.test/");
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var producer = serviceProvider.GetRequiredService<IProducer>();
        var taskProducer = serviceProvider.GetRequiredService<TaskProducer>();

        Assert.That(producer, Is.SameAs(taskProducer));
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
        var payload = new EmailPayload("person@example.com", 2);
        CreateJobRequest? createdJob = null;

        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);

        schemaRegistry
            .Setup(sr => sr.GetByJobTypeNameAndVersion(ServiceTestData.EmailJobTypeRef.Name, ServiceTestData.EmailJobTypeRef.Version))
            .ReturnsAsync(ServiceTestData.Emailv1Schema);

        httpClient
            .Setup(hc => hc.CreateJob(It.IsAny<CreateJobRequest>()))
            .Callback<CreateJobRequest>(c =>
            {
                createdJob = c;
            })
            .ReturnsAsync(new CreateJobResponse());
        
        var producer = new TaskProducer(schemaRegistry.Object, httpClient.Object);

        // Act
        await producer.ProduceAsync(new EmailPayload("person@example.com", 2));

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
        var payload = new EmailPayload("person@example.com", 2);

        var schemaRegistry = new Mock<IJobTypeSchemaRegistry>(MockBehavior.Strict);
        var httpClient = new Mock<IApiHttpClient>(MockBehavior.Strict);

        schemaRegistry
            .Setup(sr => sr.GetByJobTypeNameAndVersion(ServiceTestData.EmailJobTypeRef.Name, ServiceTestData.EmailJobTypeRef.Version))
            .ReturnsAsync(ServiceTestData.Emailv1Schema);

        httpClient
            .Setup(hc => hc.CreateJob(It.IsAny<CreateJobRequest>()));

        var producer = new TaskProducer(schemaRegistry.Object, httpClient.Object);

        // Act
        var act = () => producer.ProduceAsync(new EmailPayload("not-an-email", 0));

        // Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await act());
        httpClient.Verify(sr => sr.CreateJob(It.IsAny<CreateJobRequest>()), Times.Never);
    }


    [JobType("email", 1)]
    private sealed record EmailPayload(string Email, int Priority);
}
