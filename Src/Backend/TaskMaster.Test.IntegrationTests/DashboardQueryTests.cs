using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Queries;
using TaskMaster.Test.IntegrationTests.Abstracts;
using TaskMaster.Test.IntegrationTests.Data;

namespace TaskMaster.Test.IntegrationTests;

public class DashboardQueryTests : IntegrationTestBase
{
    [Test]
    public async Task GetDashboardDataAsync_WhenDataSeeded_ShouldReturnCorrectCounts()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var activeWorker = TestData.Worker("active", [jobType]);
            var inactiveWorker = new Worker
            {
                WorkerPublicId = Guid.NewGuid(),
                WorkerName = "inactive",
                Status = WorkerStatusEnum.InActive,
                WorkerCapabilities = []
            };
            db.Add(jobType);
            db.AddRange(
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.Queued },
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.InProgress },
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.Completed },
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.Failed },
                activeWorker,
                inactiveWorker
            );
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var query = new DashboardQuery(db);

        // Act
        var result = await query.GetDashboardDataAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.QueuedJobs, Is.EqualTo(1));
            Assert.That(result.InProgressJobs, Is.EqualTo(1));
            Assert.That(result.CompletedJobs, Is.EqualTo(1));
            Assert.That(result.FailedJobs, Is.EqualTo(1));
            Assert.That(result.ActiveWorkers, Is.EqualTo(1));
            Assert.That(result.InactiveWorkers, Is.EqualTo(1));
            Assert.That(result.DatabaseHealthy, Is.True);
        });
    }

    [Test]
    public async Task GetRecentSystemActivitiesAsync_ShouldReturnRecentActivities()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            db.Add(new SystemActivity
            {
                EntityId = Guid.NewGuid(),
                EntityType = EntityType.Job,
                ActivityType = ActivityType.JobCreated,
                Message = "Job 'email v1' created",
                CreatedDateTime = DateTime.Now
            });
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var query = new DashboardQuery(db);

        // Act
        var activities = await query.GetRecentSystemActivitiesAsync(10);

        // Assert
        Assert.That(activities, Has.Exactly(1).Items);
    }

}
