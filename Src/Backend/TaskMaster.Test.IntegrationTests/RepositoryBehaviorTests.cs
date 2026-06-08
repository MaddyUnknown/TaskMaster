using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.Test.IntegrationTests.Abstracts;
using TaskMaster.Test.IntegrationTests.Data;

namespace TaskMaster.Test.IntegrationTests;

public class RepositoryBehaviorTests : IntegrationTestBase
{
    [Test]
    public async Task CreateJob_WhenValidJob_ShouldPersistJob()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.JobTypes.Add(jobType);
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Job>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var persistedJobType = await db.JobTypes.SingleAsync();

        var job = TestData.Job(persistedJobType);

        // Act
        repository.Add(job);
        await unitOfWork.SaveAsync();

        // Assert
        var saved = await db.Jobs.Include(j => j.JobType).SingleAsync();
        Assert.That(saved.Status, Is.EqualTo(JobStatusEnum.Queued));
        Assert.That(saved.JobType.Name, Is.EqualTo("email"));
    }

    [Test]
    public async Task CreateWorker_WhenValidWorker_ShouldPersistWorkerWithCapabilities()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            db.JobTypes.Add(TestData.JobType());
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Worker>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var jobType = await db.JobTypes.SingleAsync();
        var worker = TestData.Worker("worker-a", [jobType]);

        // Act
        repository.Add(worker);
        await unitOfWork.SaveAsync();

        // Assert
        var saved = await db.Workers.Include(w => w.WorkerCapabilities).SingleAsync();
        Assert.That(saved.WorkerCapabilities, Has.Count.EqualTo(1));
        Assert.That(saved.Status, Is.EqualTo(WorkerStatusEnum.Active));
    }

    [Test]
    public async Task Heartbeat_WhenValidWorker_ShouldExtendWorkerExpiration()
    {
        // Arrange
        int workerExpiryInterval = 30;

        (var workerId, var workerExpiresAtTimestamp) = await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.Add(worker);
            await db.SaveChangesAsync();

            return (worker.WorkerPublicId, (DateTime?)db.Entry(worker).Property("WorkerExpiresAtTimestamp").CurrentValue);
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Act
        var rows = await repository.UpdateWorkerExpiryTimestampAsync(workerId, workerExpiryInterval);

        // Assert
        var newWorkerExpiresAtTimestamp = await ExecuteDbAsync(async db =>
        {
            var entity = await db.Workers.SingleAsync();
            return (DateTime?)db.Entry(entity).Property("WorkerExpiresAtTimestamp").CurrentValue;
        });

        Assert.That(rows, Is.EqualTo(1));
        Assert.That(workerExpiresAtTimestamp, Is.Not.Null);
        Assert.That(newWorkerExpiresAtTimestamp, Is.Not.Null);
        Assert.That(newWorkerExpiresAtTimestamp.Value, Is.GreaterThan(workerExpiresAtTimestamp!.Value));
    }
}
