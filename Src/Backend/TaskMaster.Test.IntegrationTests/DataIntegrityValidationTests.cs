using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using TaskMaster.API.Enums;
using TaskMaster.Test.IntegrationTests.Abstracts;
using TaskMaster.Test.IntegrationTests.Data;
using TaskMaster.Test.IntegrationTests.Providers;

namespace TaskMaster.Test.IntegrationTests;

public class DataIntegrityValidationTests : ProviderIntegrationTestBase
{

    [Test]
    public async Task HealthyDatabase_WhenDeletingWorkerWithAssignedJob_ShouldThrowException()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            var completed = TestData.Job(jobType);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();

            completed.AssignedWorkerId = worker.Id;
            completed.Status = JobStatusEnum.Completed;
            db.Add(completed);
            await db.SaveChangesAsync();
        });

        // Act
        var act = async () =>
        {
            await ExecuteDbAsync(async db =>
            {
                await db.Database.ExecuteSqlRawAsync(TestProvider.DeleteWorkersScript);
            });
        };

        // Assert
        Assert.CatchAsync<DbException>(async () => await act());
    }

    [Test]
    public async Task HealthyDatabase_WhenDeletingJobTypeWithWorker_ShouldThrowException()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
        });

        // Act
        var act = async () =>
        {
            await ExecuteDbAsync(async db =>
            {
                await db.Database.ExecuteSqlRawAsync(TestProvider.DeleteJobTypesScript);
            });
        };

        // Assert
        Assert.CatchAsync<DbException>(async () => await act());
    }

    [Test]
    public async Task HealthyDatabase_WhenDeletingJobTypeWithJob_ShouldThrowException()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var completed = TestData.Job(jobType);
            db.AddRange(jobType, completed);
            await db.SaveChangesAsync();
        });

        // Act
        var act = async () =>
        {
            await ExecuteDbAsync(async db =>
            {
                await db.Database.ExecuteSqlRawAsync(TestProvider.DeleteJobTypesScript);
            });
        };

        // Assert
        Assert.CatchAsync<DbException>(async () => await act());
    }
}
