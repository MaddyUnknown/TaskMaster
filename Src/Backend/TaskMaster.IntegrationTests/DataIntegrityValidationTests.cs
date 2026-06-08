using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Enums;

namespace TaskMaster.IntegrationTests;

public class DataIntegrityValidationTests : IntegrationTestBase
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
                await db.Database.ExecuteSqlRawAsync(IntegrityQueries.DeleteWorkers);
            });
        };

        // Assert
        Assert.ThrowsAsync<SqlException>(async () => await act());
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
                await db.Database.ExecuteSqlRawAsync(IntegrityQueries.DeleteJobTypes);
            });
        };

        // Assert
        Assert.ThrowsAsync<SqlException>(async () => await act());
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
                await db.Database.ExecuteSqlRawAsync(IntegrityQueries.DeleteJobTypes);
            });
        };

        // Assert
        Assert.ThrowsAsync<SqlException>(async () => await act());
    }

    private static class IntegrityQueries
    {
        public const string DeleteWorkers = """DELETE FROM Workers""";
        public const string DeleteJobTypes = """DELETE FROM JobTypes""";

    }
}
