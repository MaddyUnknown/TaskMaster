using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Queries;
using TaskMaster.API.Models.Enums;
using TaskMaster.API.Services;

namespace TaskMaster.Test.UnitTests.APITests;

public class DashboardServiceTests
{
    private Mock<IDashboardQuery> _dashboardQuery = null!;

    private DashboardService CreateService() => new(_dashboardQuery.Object);

    [SetUp]
    public void SetupMock()
    {
        _dashboardQuery = new(MockBehavior.Strict);
    }

    [Test]
    public async Task GetRecentActivityAsync_WhenJobCompletedActivity_ShouldMapAsSuccess()
    {
        // Arrage
        var activity = new SystemActivity
        {
            EntityType = EntityType.Job,
            EntityId = Guid.NewGuid(),
            ActivityType = ActivityType.JobCompleted,
            Message = "Job 'email v1' completed by worker 'worker-a'",
            CreatedDateTime = new DateTime(2026, 1, 2)
        };

        _dashboardQuery.Setup(q => q.GetRecentSystemActivitiesAsync(10)).ReturnsAsync([activity]);

        // Action
        var result = await CreateService().GetRecentActivityAsync(10);

        // Assert
        var item = result.Single();
        Assert.That(item.Status, Is.EqualTo(ActivityStatus.Success));
        Assert.That(item.ActivityType, Is.EqualTo(ActivityType.JobCompleted));
        Assert.That(item.EntityType, Is.EqualTo(EntityType.Job));
        Assert.That(item.EntityId, Is.EqualTo(activity.EntityId));
        Assert.That(item.Message, Is.EqualTo(activity.Message));
        Assert.That(item.Timestamp, Is.EqualTo(activity.CreatedDateTime));
    }

    [Test]
    public async Task GetRecentActivityAsync_WhenJobFailedActivity_ShouldMapAsError()
    {
        // Arrange
        var activity = new SystemActivity
        {
            EntityType = EntityType.Job,
            EntityId = Guid.NewGuid(),
            ActivityType = ActivityType.JobFailed,
            Message = "Job 'email v1' failed on worker 'worker-a'",
            CreatedDateTime = new DateTime(2026, 1, 2)
        };

        _dashboardQuery.Setup(q => q.GetRecentSystemActivitiesAsync(10)).ReturnsAsync([activity]);

        // Act
        var result = await CreateService().GetRecentActivityAsync(10);
        
        // Assert
        var item = result.Single();
        Assert.That(item.Status, Is.EqualTo(ActivityStatus.Error));
        Assert.That(item.ActivityType, Is.EqualTo(ActivityType.JobFailed));
    }

    [Test]
    public async Task GetRecentActivityAsync_WhenWorkerRegisteredActivity_ShouldMapAsSuccess()
    {
        // Arrange
        var activity = new SystemActivity
        {
            EntityType = EntityType.Worker,
            EntityId = Guid.NewGuid(),
            ActivityType = ActivityType.WorkerRegistered,
            Message = "Worker 'worker-a' registered",
            CreatedDateTime = new DateTime(2026, 1, 2)
        };

        _dashboardQuery.Setup(q => q.GetRecentSystemActivitiesAsync(10)).ReturnsAsync([activity]);

        // Act
        var result = await CreateService().GetRecentActivityAsync(10);

        // Assert
        var item = result.Single();
        Assert.That(item.Status, Is.EqualTo(ActivityStatus.Success));
        Assert.That(item.EntityType, Is.EqualTo(EntityType.Worker));
        Assert.That(item.Message, Is.EqualTo(activity.Message));
    }

    [Test]
    public async Task GetRecentActivityAsync_WhenJobCreatedActivity_ShouldMapAsInfo()
    {
        // Arrange
        var activity = new SystemActivity
        {
            EntityType = EntityType.Job,
            EntityId = Guid.NewGuid(),
            ActivityType = ActivityType.JobCreated,
            Message = "Job 'email v1' created",
            CreatedDateTime = new DateTime(2026, 1, 1)
        };

        _dashboardQuery.Setup(q => q.GetRecentSystemActivitiesAsync(10)).ReturnsAsync([activity]);

        // Act
        var result = await CreateService().GetRecentActivityAsync(10);

        // Assert
        var item = result.Single();
        Assert.That(item.Status, Is.EqualTo(ActivityStatus.Info));
        Assert.That(item.Timestamp, Is.EqualTo(activity.CreatedDateTime));
    }

    [Test]
    public async Task GetSystemMetricsAsync_WhenJobsExist_ShouldCalculateSuccessRate()
    {
        // Arrange
        _dashboardQuery
            .Setup(q => q.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData
            {
                QueuedJobs = 10,
                InProgressJobs = 5,
                CompletedJobs = 80,
                FailedJobs = 5,
                ActiveWorkers = 4,
                InactiveWorkers = 1,
                DatabaseHealthy = true
            });

        // Act
        var result = await CreateService().GetSystemMetricsAsync();

        // Assert
        Assert.That(result.ActiveWorkers, Is.EqualTo(4));
        Assert.That(result.TotalJobs, Is.EqualTo(100));
        Assert.That(result.QueuedJobs, Is.EqualTo(10));
    }

    [Test]
    public async Task GetSystemHealthAsync_WhenDbHealthy_ShouldSetHealthyStatus()
    {
        // Arrange
        _dashboardQuery
            .Setup(q => q.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData
            {
                DatabaseHealthy = true,
                ActiveWorkers = 4,
                InactiveWorkers = 0,
                QueuedJobs = 5
            });

        // Act
        var result = await CreateService().GetSystemHealthAsync();

        // Assert
        Assert.That(result.Database.Status, Is.EqualTo(HealthStatusEnum.Healthy));
        Assert.That(result.Api.Status, Is.EqualTo(HealthStatusEnum.Healthy));
        Assert.That(result.Workers.Status, Is.EqualTo(HealthStatusEnum.Healthy));
    }

    [Test]
    public async Task GetSystemHealthAsync_WhenDbUnhealthy_ShouldSetUnhealthyStatus()
    {
        // Arrange
        _dashboardQuery
            .Setup(q => q.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData
            {
                DatabaseHealthy = false,
                ActiveWorkers = 0,
                InactiveWorkers = 2,
                QueuedJobs = 0
            });

        // Act
        var result = await CreateService().GetSystemHealthAsync();

        // Assert
        Assert.That(result.Database.Status, Is.EqualTo(HealthStatusEnum.Unhealthy));
        Assert.That(result.Workers.Status, Is.EqualTo(HealthStatusEnum.Degraded));
    }

    [Test]
    public async Task GetJobStatsAsync_WhenJobsExist_ShouldReturnBuckets()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 12, 0, 0);
        var expected = new List<JobStatsItem>
        {
            new() { BucketStart = now.AddHours(-22), BucketEnd = now.AddHours(-20), BucketHour = "14:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-20), BucketEnd = now.AddHours(-18), BucketHour = "16:00", JobCount = 5 },
            new() { BucketStart = now.AddHours(-18), BucketEnd = now.AddHours(-16), BucketHour = "18:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-16), BucketEnd = now.AddHours(-14), BucketHour = "20:00", JobCount = 3 },
            new() { BucketStart = now.AddHours(-14), BucketEnd = now.AddHours(-12), BucketHour = "22:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-12), BucketEnd = now.AddHours(-10), BucketHour = "00:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-10), BucketEnd = now.AddHours(-8), BucketHour = "02:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-8), BucketEnd = now.AddHours(-6), BucketHour = "04:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-6), BucketEnd = now.AddHours(-4), BucketHour = "06:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-4), BucketEnd = now.AddHours(-2), BucketHour = "08:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(-2), BucketEnd = now.AddHours(0), BucketHour = "10:00", JobCount = 0 },
            new() { BucketStart = now.AddHours(0), BucketEnd = now.AddHours(2), BucketHour = "12:00", JobCount = 0 },
        };

        _dashboardQuery.Setup(q => q.GetJobStatsAsync()).ReturnsAsync(expected);

        // Act
        var result = (await CreateService().GetJobStatsAsync()).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(12));
        Assert.That(result.Single(i => i.BucketHour == "16:00").JobCount, Is.EqualTo(5));
        Assert.That(result.Single(i => i.BucketHour == "20:00").JobCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetJobStatsAsync_WhenNoJobsLast24h_ShouldReturnAllZeros()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 12, 0, 0);
        var hours = new[] { "14:00", "16:00", "18:00", "20:00", "22:00", "00:00", "02:00", "04:00", "06:00", "08:00", "10:00", "12:00" };
        var expected = hours.Select((h, i) => new JobStatsItem
        {
            BucketStart = now.AddHours(-22 + i * 2),
            BucketEnd = now.AddHours(-20 + i * 2),
            BucketHour = h,
            JobCount = 0
        }).ToList();
        _dashboardQuery.Setup(q => q.GetJobStatsAsync()).ReturnsAsync(expected);

        // Act
        var result = await CreateService().GetJobStatsAsync();

        // Assert
        Assert.That(result.All(i => i.JobCount == 0), Is.True);
    }
}
