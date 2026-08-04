using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Jobs;
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
            var worker = TestData.Worker("worker-a", [jobType], workerExpiryInterval);
            db.Add(worker);
            await db.SaveChangesAsync();

            return (worker.WorkerPublicId, (DateTime?)db.Entry(worker).Property("WorkerExpiresAtTimestamp").CurrentValue);
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Act
        var worker = await repository.UpdateWorkerExpiryAndReturnAsync(workerId, workerExpiryInterval);

        // Assert
        var newWorkerExpiresAtTimestamp = await ExecuteDbAsync(async db =>
        {
            var entity = await db.Workers.SingleAsync();
            return (DateTime?)db.Entry(entity).Property("WorkerExpiresAtTimestamp").CurrentValue;
        });

        Assert.That(worker, Is.Not.Null);
        Assert.That(workerExpiresAtTimestamp, Is.Not.Null);
        Assert.That(newWorkerExpiresAtTimestamp, Is.Not.Null);
        Assert.That(newWorkerExpiresAtTimestamp!.Value, Is.GreaterThan(workerExpiresAtTimestamp!.Value));
    }

    [Test]
    public async Task GetAllJobs_WhenMultipleJobs_ShouldReturnAll()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.Add(jobType);
            db.AddRange(TestData.Job(jobType), TestData.Job(jobType));
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Act
        var jobs = await repository.GetAllJobsAsync();

        // Assert
        Assert.That(jobs, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetJobByPublicId_WhenJobExists_ShouldReturnJob()
    {
        // Arrange
        var jobId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var job = TestData.Job(jobType);
            db.AddRange(jobType, job);
            await db.SaveChangesAsync();
            jobId = job.JobPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Act
        var job = await repository.GetByJobPublicIdAsync(jobId);

        // Assert
        Assert.That(job, Is.Not.Null);
        Assert.That(job!.JobPublicId, Is.EqualTo(jobId));
        Assert.That(job.JobType, Is.Not.Null);
    }

    [Test]
    public async Task GetJobByPublicIdAndWorkerPublicId_WhenBothExist_ShouldReturnJob()
    {
        // Arrange
        var jobId = Guid.Empty;
        var workerId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            var job = TestData.Job(jobType);
            db.AddRange(jobType, worker, job);
            await db.SaveChangesAsync();

            job.AssignedWorkerId = worker.Id;
            job.Status = JobStatusEnum.InProgress;
            await db.SaveChangesAsync();

            jobId = job.JobPublicId;
            workerId = worker.WorkerPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Act
        var job = await repository.GetByJobPublicIdAndWorkerPublicIdAsync(jobId, workerId);

        // Assert
        Assert.That(job, Is.Not.Null);
        Assert.That(job!.JobPublicId, Is.EqualTo(jobId));
    }

    [Test]
    public async Task UnassignJobForWorkerId_WhenWorkerHasJobs_ShouldUnassignAndQueue()
    {
        // Arrange
        var workerId = 0L;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
            workerId = worker.Id;

            var job = TestData.Job(jobType);
            job.AssignedWorkerId = workerId;
            job.Status = JobStatusEnum.InProgress;
            db.Add(job);
            await db.SaveChangesAsync();
        });
        
        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        
        // Act
        var rows = await repository.UnassignJobForWorkerIdAsync(workerId);

        // Assert
        Assert.That(rows, Is.EqualTo(1));

        await ExecuteDbAsync(async db =>
        {
            var job = await db.Jobs.SingleAsync();
            Assert.That(job.Status, Is.EqualTo(JobStatusEnum.Queued));
            Assert.That(job.AssignedWorkerId, Is.Null);
        });
    }

    [Test]
    public async Task GetJobTypeByNameAndVersion_WhenExists_ShouldReturnIt()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            db.Add(TestData.JobType("email", 1));
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobTypeRepository>();

        // Act
        var result = await repository.GetByJobTypeNameAndVersionAsync("email", 1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("email"));
        Assert.That(result.Version, Is.EqualTo(1));
    }

    [Test]
    public async Task GetJobTypeByMultipleNamesAndVersions_ShouldReturnAllMatching()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            db.AddRange(TestData.JobType("email", 1), TestData.JobType("video", 2), TestData.JobType("audio", 3));
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobTypeRepository>();

        // Act
        var results = await repository.GetByJobTypeNameAndVersionAsync([("email", (long)1), ("video", (long)2)]);

        // Assert
        Assert.That(results, Has.Exactly(2).Items);
    }

    [Test]
    public async Task GetWorkerByPublicId_WhenExists_ShouldReturnWithCapabilities()
    {
        // Arrange
        var workerId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
            workerId = worker.WorkerPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        // Act
        var worker = await repository.GetByPublicIdAsync(workerId);

        // Assert
        Assert.That(worker, Is.Not.Null);
        Assert.That(worker!.WorkerName, Is.EqualTo("worker-a"));
        Assert.That(worker.WorkerCapabilities, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetAllWorkers_WhenMultipleWorkers_ShouldReturnAll()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.Add(jobType);
            db.AddRange(TestData.Worker("worker-a", [jobType]), TestData.Worker("worker-b", [jobType]));
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        // Act
        var workers = await repository.GetAllWorkersAsync();

        // Assert
        Assert.That(workers, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByWorkerNameAsync_WhenExists_ShouldReturnWorker()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        // Act
        var worker = await repository.GetByWorkerNameAsync("worker-a");

        // Assert
        Assert.That(worker, Is.Not.Null);
        Assert.That(worker!.WorkerName, Is.EqualTo("worker-a"));
        Assert.That(worker.Status, Is.EqualTo(WorkerStatusEnum.Active));
        Assert.That(worker.WorkerCapabilities, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Repository_GetById_WhenExists_ShouldReturnEntity()
    {
        // Arrange
        var expectedId = 0L;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.Add(jobType);
            await db.SaveChangesAsync();
            expectedId = jobType.Id;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<JobType>>();

        // Act
        var result = await repository.GetByIdAsync(expectedId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(expectedId));
    }

    [Test]
    public async Task Repository_GetAll_WhenMultipleEntities_ShouldReturnAll()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            db.AddRange(TestData.JobType("email", 1), TestData.JobType("video", 2));
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<JobType>>();

        // Act
        var results = await repository.GetAllAsync();

        // Assert
        Assert.That(results, Has.Exactly(2).Items);
    }

    [Test]
    public async Task GetById_WhenEntityExists_ShouldReturnEntity()
    {
        // Arrange
        var jobTypeId = 0L;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.Add(jobType);
            await db.SaveChangesAsync();
            jobTypeId = jobType.Id;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<JobType>>();

        // Act
        var result = await repository.GetByIdAsync(jobTypeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("email"));
    }

    [Test]
    public async Task DeactivateExpiredWorkers_WhenExpiredWorkerExists_ShouldSetStatusToInactive()
    {
        // Arrange
        var workerPublicId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            worker.WorkerExpiresAtTimestamp = DateTime.Now.AddDays(-1);
            worker.LastHeartBeatTimestamp = DateTime.Now.AddDays(-2);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
            workerPublicId = worker.WorkerPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        // Act
        var rows = await repository.DeactivateExpiredWorkersAsync();

        // Assert
        Assert.That(rows, Is.EqualTo(1));


        var savedWorker = await repository.GetByPublicIdAsync(workerPublicId);
        Assert.That(savedWorker
            , Is.Not.Null);
        Assert.That(savedWorker!.Status, Is.EqualTo(WorkerStatusEnum.InActive));
        Assert.That(savedWorker!.WorkerPublicId, Is.EqualTo(workerPublicId));
    }

    [Test]
    public async Task DeactivateExpiredWorkers_WhenNoExpiredWorkers_ShouldNotModify()
    {
        // Arrange
        var workerPublicId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            worker.WorkerExpiresAtTimestamp = DateTime.Now.AddDays(1);
            worker.LastHeartBeatTimestamp = DateTime.Now;
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
            workerPublicId = worker.WorkerPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();

        // Act
        var rows = await repository.DeactivateExpiredWorkersAsync();

        // Assert
        Assert.That(rows, Is.EqualTo(0));

        var savedWorker = await repository.GetByPublicIdAsync(workerPublicId);
        Assert.That(savedWorker, Is.Not.Null);
        Assert.That(savedWorker!.Status, Is.EqualTo(WorkerStatusEnum.Active));
    }

    [Test]
    public async Task UnassignJobsForInactiveWorkers_WhenInactiveWorkerHasJobs_ShouldUnassignAndQueue()
    {
        // Arrange
        var jobPublicId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            worker.Status = WorkerStatusEnum.InActive;
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
            var workerId = worker.Id;

            var job = TestData.Job(jobType);
            job.AssignedWorkerId = workerId;
            job.Status = JobStatusEnum.InProgress;
            db.Add(job);
            await db.SaveChangesAsync();
            jobPublicId = job.JobPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var workerRepo = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Act
        var rows = await repository.UnassignJobsForInactiveWorkersAsync();

        // Assert
        Assert.That(rows, Is.EqualTo(1));

        var savedJob = await repository.GetByJobPublicIdAsync(jobPublicId);
        Assert.That(savedJob, Is.Not.Null);
        Assert.That(savedJob!.Status, Is.EqualTo(JobStatusEnum.Queued));
        Assert.That(savedJob!.AssignedWorkerId, Is.Null);
    }

    [Test]
    public async Task UnassignJobsForInactiveWorkers_WhenNoInactiveWorkers_ShouldNotModify()
    {
        // Arrange
        var jobPublicId = Guid.Empty;
        long workerId = 0;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
            workerId = worker.Id;

            var job = TestData.Job(jobType);
            job.AssignedWorkerId = workerId;
            job.Status = JobStatusEnum.InProgress;
            db.Add(job);
            await db.SaveChangesAsync();
            jobPublicId = job.JobPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Act
        var rows = await repository.UnassignJobsForInactiveWorkersAsync();

        // Assert
        Assert.That(rows, Is.EqualTo(0));

        var savedJob = await repository.GetByJobPublicIdAsync(jobPublicId);
        Assert.That(savedJob, Is.Not.Null);
        Assert.That(savedJob!.Status, Is.EqualTo(JobStatusEnum.InProgress));
        Assert.That(savedJob!.AssignedWorkerId, Is.EqualTo(workerId));
    }

    [Test]
    public async Task GetNextJobsForWorkerAsync_WhenMaxJobs3And5Jobs_ShouldReturn3()
    {
        // Arrange
        var workerPublicId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.Add(jobType);
            db.Add(worker);
            await db.SaveChangesAsync();
            workerPublicId = worker.WorkerPublicId;

            db.AddRange(
                TestData.Job(jobType), TestData.Job(jobType), TestData.Job(jobType),
                TestData.Job(jobType), TestData.Job(jobType));
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Act
        var jobs = await repository.GetNextJobsForWorkerAsync(workerPublicId, 3);

        // Assert
        Assert.That(jobs, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetNextJobsForWorkerAsync_WhenNoQueuedJobs_ShouldReturnEmptyList()
    {
        // Arrange
        var workerPublicId = Guid.Empty;
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            var worker = TestData.Worker("worker-a", [jobType]);
            db.AddRange(jobType, worker);
            await db.SaveChangesAsync();
            workerPublicId = worker.WorkerPublicId;
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Act
        var jobs = await repository.GetNextJobsForWorkerAsync(workerPublicId, 5);

        // Assert
        Assert.That(jobs, Is.Empty);
    }
}
