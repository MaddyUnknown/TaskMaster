using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Data;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Models.Workers;
using TaskMaster.Test.IntegrationTests.Abstracts;
using TaskMaster.Test.IntegrationTests.Data;

namespace TaskMaster.Test.IntegrationTests;

public class SystemActivityTests : IntegrationTestBase
{
    [Test]
    public async Task CreateJob_WhenPersisted_ShouldCreateJobCreatedActivity()
    {
        // Arrange
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();

        var jobType = TestData.JobType();
        db.Add(jobType);
        await db.SaveChangesAsync();

        // Act
        var job = await jobService.CreateAsync(new CreateJob
        {
            JobType = new JobTypeRef { Name = jobType.Name, Version = jobType.Version },
            Payload = "{}"
        });

        // Assert
        var activities = db.SystemActivities.Where(a => a.ActivityType == ActivityType.JobCreated).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(activities, Has.Exactly(1).Items);
            Assert.That(activities[0].EntityType, Is.EqualTo(EntityType.Job));
            Assert.That(activities[0].Message, Is.EqualTo($"Job '{job.JobId}' of type '{jobType.Name} (v{jobType.Version})' created"));
        });
    }

    [Test]
    public async Task RegisterWorker_WhenPersisted_ShouldCreateWorkerRegisteredActivity()
    {
        // Arrange
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workerService = scope.ServiceProvider.GetRequiredService<IWorkerService>();

        var jobType = TestData.JobType();
        db.Add(jobType);
        await db.SaveChangesAsync();

        // Act
        await workerService.RegisterAsync(new RegisterWorker
        {
            WorkerName = "worker-a",
            JobTypeCapabilities = [new JobTypeRef { Name = jobType.Name, Version = jobType.Version }]
        });

        // Assert
        var activities = db.SystemActivities.Where(a => a.ActivityType == ActivityType.WorkerRegistered).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(activities, Has.Exactly(1).Items);
            Assert.That(activities[0].EntityType, Is.EqualTo(EntityType.Worker));
            Assert.That(activities[0].Message, Is.EqualTo("Worker 'worker-a' registered"));
        });
    }
}
