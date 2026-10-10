using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Queries;
using TaskMaster.API.Models.Dashboard;
using TaskMaster.Test.IntegrationTests.Abstracts;
using TaskMaster.Test.IntegrationTests.Data;

namespace TaskMaster.Test.IntegrationTests;

public class DashboardQueryTests : ProviderIntegrationTestBase
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
        var query = scope.ServiceProvider.GetRequiredService<IDashboardQuery>();

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
    public async Task GetJobStatsAsync_WhenJobsSeeded_ShouldBucketJobsIntoTwoHourWindows()
    {
        // Arrange
        var now = DateTime.UtcNow;

        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.Add(jobType);

            var jobs = new[]
            {
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.Completed },
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.Completed },
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.Completed },
                new Job { JobPublicId = Guid.NewGuid(), JobType = jobType, Payload = "{}", Status = JobStatusEnum.Completed }
            };
            db.AddRange(jobs);
            await db.SaveChangesAsync();

            jobs[0].CreatedDateTime = now;
            jobs[1].CreatedDateTime = now;
            jobs[2].CreatedDateTime = now.AddHours(-4);
            jobs[3].CreatedDateTime = now.AddHours(-30);
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IDashboardQuery>();
        var timeZoneToUser = TimeZoneInfo.Local. Id;

        // Act
        var response = await query.GetJobStatsAsync(timeZoneToUser);
        var buckets = response.Buckets;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Timezone, Is.EqualTo(timeZoneToUser));
            Assert.That(response.BucketSizeMinutes, Is.EqualTo(120));
            Assert.That(buckets, Has.Count.EqualTo(12), "The 24h window should be split into twelve 2-hour buckets");

            Assert.That(buckets.Sum(b => b.JobCount), Is.EqualTo(3), "Jobs older than the 24h window must be excluded");

            Assert.That(buckets, Is.Ordered.Ascending.By(nameof(JobStatsItem.BucketStart)));

            foreach (var bucket in buckets)
            {
                Assert.That(bucket.BucketEnd, Is.EqualTo(bucket.BucketStart.AddHours(2)));
                Assert.That(bucket.Label, Does.Match(@"^\d{2}:\d{2}$"), "Label must be an HH:mm string");
            }

            for (var i = 1; i < buckets.Count; i++)
            {
                Assert.That(buckets[i].BucketStart, Is.EqualTo(buckets[i - 1].BucketStart.AddHours(2)));
            }

            var populated = buckets.Where(b => b.JobCount > 0).ToList();
            Assert.That(populated, Has.Count.EqualTo(2), "Jobs created 4 hours apart must land in two distinct buckets");
            Assert.That(populated.Select(b => b.JobCount).OrderBy(c => c), Is.EqualTo(new[] { 1, 2 }));
        });
    }

    [Test]
    public async Task GetJobStatsAsync_WhenNoJobs_ShouldReturnEmptyTwoHourBuckets()
    {
        // Arrange
        await using var scope = ServiceProvider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IDashboardQuery>();

        // Act
        var response = await query.GetJobStatsAsync("Europe/London");
        var buckets = response.Buckets;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Timezone, Is.EqualTo("Europe/London"));
            Assert.That(buckets, Has.Count.EqualTo(12));
            Assert.That(buckets.All(b => b.JobCount == 0), Is.True);
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
                CreatedDateTime = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IDashboardQuery>();

        // Act
        var activities = await query.GetRecentSystemActivitiesAsync(10);

        // Assert
        Assert.That(activities, Has.Exactly(1).Items);
    }

}
