using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using TaskMaster.API.Data;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Jobs;
using TaskMaster.Test.IntegrationTests.Abstracts;
using TaskMaster.Test.IntegrationTests.Data;

namespace TaskMaster.Test.IntegrationTests;

public class ConcurrencyTests : IntegrationTestBase
{
    [Test]
    public async Task GetNextWorkerJobsAsync_WhenOneJobUnderConcurrency_ShouldOnlyBeClaimedOnceByOneWorker()
    {
        // Arrange
        var workerIds = await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var workers = Enumerable.Range(1, 16) // Alligned to PCs logical cores X2
                .Select(i => TestData.Worker($"worker-{i}", [jobType]))
                .ToArray();

            db.Add(jobType);
            db.Add(TestData.Job(jobType));
            db.AddRange(workers);
            await db.SaveChangesAsync();

            return workers.Select(w => w.WorkerPublicId).ToArray();
        });

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pullTasks = workerIds.Select(async workerId =>
        {
            await using var scope = ServiceProvider.CreateAsyncScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();

            await start.Task; // Used to start all jobs at the same time

            return await jobService.GetNextWorkerJobsAsync(workerId);
        }).ToArray();

        // Act
        start.SetResult();
        var results = await Task.WhenAll(pullTasks);

        // Assert
        var claimed = results.Where(r => r is not null).ToArray();
        Assert.That(claimed, Has.Length.EqualTo(1));
        Assert.That(claimed[0]!.Status, Is.EqualTo(JobStatusEnum.InProgress));

        await ExecuteDbAsync(async db =>
        {
            var jobs = await db.Jobs.AsNoTracking().ToListAsync();
            Assert.That(jobs, Has.Count.EqualTo(1));
            Assert.That(jobs.Single().Status, Is.EqualTo(JobStatusEnum.InProgress));
            Assert.That(jobs.Single().AssignedWorkerId, Is.Not.Null);
        });
    }
}
