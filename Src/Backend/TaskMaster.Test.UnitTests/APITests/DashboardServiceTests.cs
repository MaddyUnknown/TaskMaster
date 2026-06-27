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
    public async Task GetRecentActivityAsync_WhenCompletedJob_ShouldMapAsSuccess()
    {
        var job = new Job
        {
            JobPublicId = Guid.NewGuid(),
            JobType = new JobType { Name = "email", Version = 1 },
            Status = JobStatusEnum.Completed,
            CreatedDateTime = new DateTime(2026, 1, 1),
            ModifyDateTime = new DateTime(2026, 1, 2)
        };
        _dashboardQuery.Setup(q => q.GetRecentJobsAsync(10)).ReturnsAsync([job]);

        var result = await CreateService().GetRecentActivityAsync();

        var item = result.Single();
        Assert.That(item.Status, Is.EqualTo(ActivityStatus.Success));
        Assert.That(item.Type, Is.EqualTo(JobStatusEnum.Completed));
        Assert.That(item.Message, Does.Contain(job.Status.ToString()));
        Assert.That(item.Timestamp, Is.EqualTo(job.ModifyDateTime));
    }

    [Test]
    public async Task GetRecentActivityAsync_WhenFailedJob_ShouldMapAsError()
    {
        var job = new Job
        {
            JobPublicId = Guid.NewGuid(),
            JobType = new JobType { Name = "email", Version = 1 },
            Status = JobStatusEnum.Failed,
            CreatedDateTime = DateTime.Now
        };
        _dashboardQuery.Setup(q => q.GetRecentJobsAsync(10)).ReturnsAsync([job]);

        var result = await CreateService().GetRecentActivityAsync();

        var item = result.Single();
        Assert.That(item.Status, Is.EqualTo(ActivityStatus.Error));
        Assert.That(item.Message, Does.Contain(job.Status.ToString()));
    }

    [Test]
    public async Task GetRecentActivityAsync_WhenQueuedJob_ShouldMapAsInfo()
    {
        var job = new Job
        {
            JobPublicId = Guid.NewGuid(),
            JobType = new JobType { Name = "email", Version = 1 },
            Status = JobStatusEnum.Queued,
            CreatedDateTime = DateTime.Now
        };
        _dashboardQuery.Setup(q => q.GetRecentJobsAsync(10)).ReturnsAsync([job]);

        var result = await CreateService().GetRecentActivityAsync();

        var item = result.Single();
        Assert.That(item.Status, Is.EqualTo(ActivityStatus.Info));
        Assert.That(item.Message, Does.Contain(job.Status.ToString()));
    }

    [Test]
    public async Task GetRecentActivityAsync_WhenNoModifyDate_ShouldFallbackToCreatedDate()
    {
        var job = new Job
        {
            JobPublicId = Guid.NewGuid(),
            JobType = new JobType { Name = "email", Version = 1 },
            Status = JobStatusEnum.InProgress,
            CreatedDateTime = new DateTime(2026, 6, 1, 12, 0, 0)
        };
        _dashboardQuery.Setup(q => q.GetRecentJobsAsync(10)).ReturnsAsync([job]);

        var result = await CreateService().GetRecentActivityAsync();

        var item = result.Single();
        Assert.That(item.Timestamp, Is.EqualTo(job.CreatedDateTime));
    }

    [Test]
    public async Task GetSystemMetricsAsync_WhenJobsExist_ShouldCalculateSuccessRate()
    {
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

        var result = await CreateService().GetSystemMetricsAsync();

        Assert.That(result.SuccessRate, Is.EqualTo(80.0));
        Assert.That(result.ActiveWorkers, Is.EqualTo(4));
        Assert.That(result.QueueDepth, Is.EqualTo(10));
    }

    [Test]
    public async Task GetSystemMetricsAsync_WhenNoJobs_ShouldReturnDefaultSuccessRate()
    {
        _dashboardQuery
            .Setup(q => q.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData
            {
                QueuedJobs = 0,
                InProgressJobs = 0,
                CompletedJobs = 0,
                FailedJobs = 0,
                ActiveWorkers = 0,
                InactiveWorkers = 0,
                DatabaseHealthy = true
            });

        var result = await CreateService().GetSystemMetricsAsync();

        Assert.That(result.SuccessRate, Is.EqualTo(100.0));
    }

    [Test]
    public async Task GetSystemHealthAsync_WhenDbHealthy_ShouldSetHealthyStatus()
    {
        _dashboardQuery
            .Setup(q => q.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData
            {
                DatabaseHealthy = true,
                ActiveWorkers = 3,
                InactiveWorkers = 1,
                QueuedJobs = 5
            });

        var result = await CreateService().GetSystemHealthAsync();

        Assert.That(result.Database.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Api.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Queue.Status, Is.EqualTo(HealthStatus.Healthy));
        Assert.That(result.Workers.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task GetSystemHealthAsync_WhenDbUnhealthy_ShouldSetUnhealthyStatus()
    {
        _dashboardQuery
            .Setup(q => q.GetDashboardDataAsync())
            .ReturnsAsync(new DashboardData
            {
                DatabaseHealthy = false,
                ActiveWorkers = 0,
                InactiveWorkers = 2,
                QueuedJobs = 0
            });

        var result = await CreateService().GetSystemHealthAsync();

        Assert.That(result.Database.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Workers.Status, Is.EqualTo(HealthStatus.Degraded));
    }
}
