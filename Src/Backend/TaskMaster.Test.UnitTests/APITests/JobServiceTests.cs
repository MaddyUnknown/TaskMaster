using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Events;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Publisher;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Services;
using TaskMaster.API.Validation;
using TaskMaster.Test.UnitTests.Data;

namespace TaskMaster.Test.UnitTests.APITests;

public class JobServiceTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IRepository<Job>> _jobCrudRepository = null!;
    private Mock<IJobTypeRepository> _jobTypeRepository = null!;
    private Mock<IJobRepository> _jobRepository = null!;
    private Mock<IWorkerRepository> _workerRepository = null!;
    private Mock<IValidator<CreateJob>> _createJobValidator = null!;
    private Mock<IEventPublisher> _eventPublisher = null!;

    private JobService CreateService() =>
        new(_unitOfWork.Object, _jobCrudRepository.Object, _jobTypeRepository.Object, _jobRepository.Object, _workerRepository.Object, _createJobValidator.Object, _eventPublisher.Object);

    [SetUp]
    public void SetupMock()
    {
        _unitOfWork = new();
        _jobCrudRepository = new(MockBehavior.Strict);
        _jobTypeRepository = new(MockBehavior.Strict);
        _jobRepository = new(MockBehavior.Strict);
        _workerRepository = new(MockBehavior.Strict);
        _createJobValidator = new(MockBehavior.Strict);
        _createJobValidator
            .Setup(v => v.Validate(It.IsAny<CreateJob>()))
            .Returns(Array.Empty<string>());
        _eventPublisher = new(MockBehavior.Strict);
    }

    [Test]
    public async Task CreateAsync_WhenValidJob_ShouldPersistQueuedJob()
    {
        // Arrange
        var jobType = ServiceTestData.EmailJobType();
        var request = new CreateJob { JobType = ServiceTestData.EmailJobTypeRef, Payload = "{\"hello\":\"world\"}" };
        Job? persisted = null;

        _jobTypeRepository
            .Setup(r => r.GetByJobTypeNameAndVersionAsync(jobType.Name, jobType.Version))
            .ReturnsAsync(jobType);

        _jobCrudRepository
            .Setup(r => r.Add(It.IsAny<Job>()))
            .Callback<Job>(j => persisted = j);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<JobCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().CreateAsync(request);

        // Assert
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Status, Is.EqualTo(JobStatusEnum.Queued));
        Assert.That(persisted.Payload, Is.EqualTo(request.Payload));
        Assert.That(persisted.JobType, Is.SameAs(jobType));

        Assert.That(result.JobId, Is.EqualTo(persisted.JobPublicId));
        Assert.That(result.Status, Is.EqualTo(JobStatusEnum.Queued));

        _jobCrudRepository.Verify(r => r.Add(It.IsAny<Job>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<JobCreatedEvent>(e => e.JobId == persisted!.JobPublicId && e.JobTypeName == jobType.Name && e.JobTypeVersion == jobType.Version),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateAsync_WhenUnknownJobType_ShouldThrowException()
    {
        // Arrange
        var jobType = new JobTypeRef { Name = "missing", Version = 1 };

        _jobTypeRepository
            .Setup(r => r.GetByJobTypeNameAndVersionAsync(jobType.Name, jobType.Version))
            .ReturnsAsync((JobType?)null);

        // Act + Assert
        var act = () => CreateService().CreateAsync(new CreateJob { JobType = jobType });

        Assert.ThrowsAsync<NotFoundException>(async () => await act());

        _jobCrudRepository.Verify(r => r.Add(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Test]
    public void CreateAsync_WhenValidationFails_ShouldThrowException()
    {
        // Arrange
        _createJobValidator
            .Setup(v => v.Validate(It.IsAny<CreateJob>()))
            .Returns(["Invalid payload"]);

        var request = new CreateJob { JobType = ServiceTestData.EmailJobTypeRef, Payload = "{}" };

        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().CreateAsync(request));
        _jobCrudRepository.Verify(r => r.Add(It.IsAny<Job>()), Times.Never);
    }

    [Test]
    public async Task ChangeJobStatusAsync_WhenCompleteJob_ShouldUpdateStatusAsComplete()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();
        var job = ServiceTestData.QueuedJob();
        job.AssignedWorkerId = worker.Id;

        _jobRepository
            .Setup(r => r.GetByJobPublicIdAndWorkerPublicIdAsync(job.JobPublicId, worker.WorkerPublicId))
            .ReturnsAsync(job);

        _jobCrudRepository
            .Setup(r => r.Update(It.Is<Job>(j => j.Id == job.Id)));

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<JobCompletedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().ChangeJobStatusAsync(job.JobPublicId, JobStatusEnum.Completed, new WorkerIdRef { WorkerId = worker.WorkerPublicId });

        // Assert
        Assert.That(result.Status, Is.EqualTo(JobStatusEnum.Completed));

        Assert.That(job.Status, Is.EqualTo(JobStatusEnum.Completed));

        _jobCrudRepository.Verify(r => r.Update(It.Is<Job>(j => j.Id == job.Id)), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<JobCompletedEvent>(e => e.JobId == job.JobPublicId && e.WorkerName == worker.WorkerName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeJobStatusAsync_WhenFailJob_ShouldUpdateStatusAsFailed()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();
        var job = ServiceTestData.QueuedJob();
        job.AssignedWorkerId = worker.Id;

        _jobRepository
            .Setup(r => r.GetByJobPublicIdAndWorkerPublicIdAsync(job.JobPublicId, worker.WorkerPublicId))
            .ReturnsAsync(job);

        _jobCrudRepository
            .Setup(r => r.Update(It.Is<Job>(j => j.Id == job.Id)));

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<JobFailedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().ChangeJobStatusAsync(job.JobPublicId, JobStatusEnum.Failed, new WorkerIdRef { WorkerId = worker.WorkerPublicId });

        // Assert
        Assert.That(result.Status, Is.EqualTo(JobStatusEnum.Failed));

        Assert.That(job.Status, Is.EqualTo(JobStatusEnum.Failed));

        _jobCrudRepository.Verify(r => r.Update(It.Is<Job>(j => j.Id == job.Id)), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<JobFailedEvent>(e => e.JobId == job.JobPublicId && e.WorkerName == worker.WorkerName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ChangeJobStatusAsync_WhenJobNotExists_ShouldThrowException()
    {
        // Arrange
        var workerId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        _jobRepository
            .Setup(r => r.GetByJobPublicIdAndWorkerPublicIdAsync(jobId, workerId))
            .ReturnsAsync((Job?)null);

        _jobCrudRepository
            .Setup(r => r.Update(It.Is<Job>(j => j.JobPublicId == jobId)));

        // Act + Assert
        var act = () => CreateService().ChangeJobStatusAsync(jobId, JobStatusEnum.Completed, new WorkerIdRef { WorkerId = workerId });

        Assert.ThrowsAsync<NotFoundException>(async () => await act());
        _jobCrudRepository.Verify(r => r.Update(It.Is<Job>(j => j.JobPublicId == jobId)), Times.Never);
    }

    [Test]
    public void ChangeJobStatusAsync_WhenEmptyJobId_ShouldThrowException()
    {
        // Act + Assert
        var act = () => CreateService().ChangeJobStatusAsync(Guid.Empty, JobStatusEnum.Completed, new WorkerIdRef { WorkerId = Guid.NewGuid() });
        Assert.ThrowsAsync<ValidationException>(async () => await act());
        _jobRepository.Verify(r => r.GetByJobPublicIdAndWorkerPublicIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public void ChangeJobStatusAsync_WhenEmptyWorkerId_ShouldThrowException()
    {
        // Act + Assert
        var act = () => CreateService().ChangeJobStatusAsync(Guid.NewGuid(), JobStatusEnum.Completed, new WorkerIdRef { WorkerId = Guid.Empty });
        Assert.ThrowsAsync<ValidationException>(async () => await act());
        _jobRepository.Verify(r => r.GetByJobPublicIdAndWorkerPublicIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task ChangeJobStatusAsync_Bulk_WhenValidRequest_ShouldReturnUpdatedCount()
    {
        // Arrange
        var workerId = Guid.NewGuid();
        var request = new BulkUpdateJobStatus
        {
            WorkerId = workerId,
            JobStatuses =
            [
                new UpdateJobStatus { JobId = Guid.NewGuid(), Status = JobStatusEnum.Completed },
                new UpdateJobStatus { JobId = Guid.NewGuid(), Status = JobStatusEnum.Failed }
            ]
        };

        var jobType = ServiceTestData.EmailJobType();

        _jobRepository
            .Setup(r => r.GetByJobPublicIdAndWorkerPublicIdAsync(It.Is<Guid>(j => request.JobStatuses.Any(d => d.JobId == j)), request.WorkerId))
            .ReturnsAsync((Guid jobPublicId, Guid workerPublicId) => {
                var job = ServiceTestData.QueuedJob(jobType, jobPublicId: jobPublicId);
                job.Status = JobStatusEnum.InProgress;
                return job;
            });

        _jobCrudRepository
            .Setup(r => r.Update(It.Is<Job>(j => request.JobStatuses.Any(d => d.JobId == j.JobPublicId))));

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(request.WorkerId))
            .ReturnsAsync((Worker?)null);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<IEnumerable<JobCompletedEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<IEnumerable<JobFailedEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().ChangeJobStatusAsync(request);

        // Assert
        Assert.That(result.UpdatedRecordCount, Is.EqualTo(2));
        _jobCrudRepository.Verify(r => r.Update(It.Is<Job>(j => request.JobStatuses.Any(d => d.JobId == j.JobPublicId))), Times.Exactly(request.JobStatuses.Count));
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<IEnumerable<JobCompletedEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny< IEnumerable<JobFailedEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ChangeJobStatusAsync_Bulk_WhenEmptyWorkerId_ShouldThrowException()
    {
        // Act + Assert
        var act = () => CreateService().ChangeJobStatusAsync(new BulkUpdateJobStatus { WorkerId = Guid.Empty });
        Assert.ThrowsAsync<ValidationException>(async () => await act());
        _jobCrudRepository.Verify(r => r.Update(It.IsAny<Job>()), Times.Never);
    }


    [Test]
    public async Task GetNextWorkerJobsAsync_WhenQueuedJob_ShouldAssignJob()
    {
        // Arrange
        var jobType = ServiceTestData.EmailJobType();
        var worker = ServiceTestData.ActiveWorker(capabilities: [jobType]);
        var job = ServiceTestData.QueuedJob(jobType);

        _jobRepository
            .Setup(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 1))
            .Callback(() =>
            {
                job.Status = JobStatusEnum.InProgress;
                job.AssignedWorkerId = worker.Id;
            })
            .ReturnsAsync([job]);

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<IEnumerable<JobAssignedEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.JobId, Is.EqualTo(job.JobPublicId));
        Assert.That(result.Status, Is.EqualTo(JobStatusEnum.InProgress));

        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<IEnumerable<JobAssignedEvent>>(e => e.Any(j => j.JobId == job.JobPublicId && j.WorkerName == worker.WorkerName)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetNextWorkerJobsAsync_WhenNoQueuedJob_ShouldReturnNull()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();

        _jobRepository
            .Setup(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 1))
            .ReturnsAsync([]);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result, Is.Null);

        _jobRepository.Verify(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 1), Times.Once);
    }

    [Test]
    public async Task GetNextWorkerJobsAsync_WhenWorkerWithoutCapability_ShouldNotReceiveJob()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();

        _jobRepository
            .Setup(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 1))
            .ReturnsAsync([]);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result, Is.Null);

        _jobRepository.Verify(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 1), Times.Once);
    }

    [Test]
    public void GetNextWorkerJobsAsync_WhenEmptyWorkerId_ShouldThrowException()
    {
        // Act + Assert
        var act = () => CreateService().GetNextWorkerJobsAsync(Guid.Empty);
        Assert.ThrowsAsync<ValidationException>(async () => await act());
        _workerRepository.Verify(r => r.GetByPublicIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task GetNextJobsAsync_WhenQueuedJobs_ShouldReturnJobs()
    {
        // Arrange
        var jobType = ServiceTestData.EmailJobType();
        var worker = ServiceTestData.ActiveWorker(capabilities: [jobType]);
        var jobs = new[] { ServiceTestData.QueuedJob(jobType), ServiceTestData.QueuedJob(jobType) };

        _jobRepository
            .Setup(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 3))
            .ReturnsAsync(jobs.ToList());

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny< IEnumerable<JobAssignedEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId, 3);

        // Assert
        Assert.That(result, Has.Exactly(2).Items);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<IEnumerable<JobAssignedEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetNextJobsAsync_WhenNoQueuedJobs_ShouldReturnEmptyList()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();

        _jobRepository
            .Setup(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 5))
            .ReturnsAsync([]);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId, 5);

        // Assert
        Assert.That(result, Is.Empty);

        _jobRepository.Verify(r => r.GetNextJobsForWorkerAsync(worker.WorkerPublicId, 5), Times.Once);
    }

    [Test]
    public void GetNextJobsAsync_WhenEmptyWorkerId_ShouldThrowException()
    {
        // Act + Assert
        var act = () => CreateService().GetNextWorkerJobsAsync(Guid.Empty, 5);
        Assert.ThrowsAsync<ValidationException>(async () => await act());
        _jobRepository.Verify(r => r.GetNextJobsForWorkerAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetAllJobsAsync_WhenJobsExist_ShouldReturnPagedJobs()
    {
        // Arrange
        var jobs = new[] { ServiceTestData.QueuedJob(), ServiceTestData.QueuedJob() };
        var query = new JobQuery { Page = 1, PageSize = 20 };
        _jobRepository
            .Setup(r => r.GetAllJobsAsync(query))
            .ReturnsAsync(PagedResult<Job>.Create(jobs, query.Page!.Value, query.PageSize!.Value, jobs.Length));

        // Act
        var result = await CreateService().GetAllJobsAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(2).Items);
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.TotalPages, Is.EqualTo(1));
        Assert.That(result.HasPreviousPage, Is.False);
        Assert.That(result.HasNextPage, Is.False);

        _jobRepository.Verify(r => r.GetAllJobsAsync(query), Times.Once);
    }

    [Test]
    public async Task GetAllJobsAsync_WhenStatusFilterProvided_ShouldPassQueryToRepository()
    {
        // Arrange
        var query = new JobQuery { Page = 1, PageSize = 10, Status = JobStatusEnum.Failed };
        _jobRepository
            .Setup(r => r.GetAllJobsAsync(It.Is<JobQuery>(q => q.Status == JobStatusEnum.Failed)))
            .ReturnsAsync(PagedResult<Job>.Create(Array.Empty<Job>(), query.Page!.Value, query.PageSize!.Value, 0));

        // Act
        var result = await CreateService().GetAllJobsAsync(query);

        // Assert
        Assert.That(result.Items, Is.Empty);
        _jobRepository.Verify(r => r.GetAllJobsAsync(It.Is<JobQuery>(q => q.Status == JobStatusEnum.Failed)), Times.Once);
    }

    [Test]
    public async Task GetAllJobsAsync_WhenPagingAcrossPages_ShouldReturnRemainingItems()
    {
        // Arrange
        var jobs = new[] { ServiceTestData.QueuedJob(), ServiceTestData.QueuedJob(), ServiceTestData.QueuedJob() };
        var query = new JobQuery { Page = 2, PageSize = 2 };
        _jobRepository
            .Setup(r => r.GetAllJobsAsync(query))
            .ReturnsAsync(PagedResult<Job>.Create(jobs.TakeLast(1), query.Page!.Value, query.PageSize!.Value, jobs.Length));

        // Act
        var result = await CreateService().GetAllJobsAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(1).Items);
        Assert.That(result.TotalCount, Is.EqualTo(3));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.HasPreviousPage, Is.True);
        Assert.That(result.HasNextPage, Is.False);
    }

    [Test]
    public async Task GetAllJobsAsync_WhenNoPagingParamsProvided_ShouldReturnAllJobsUnpaged()
    {
        // Arrange
        var jobs = new[] { ServiceTestData.QueuedJob(), ServiceTestData.QueuedJob() };
        var query = new JobQuery();
        _jobRepository
            .Setup(r => r.GetAllJobsAsync(query))
            .ReturnsAsync(PagedResult<Job>.Unpaged(jobs));

        // Act
        var result = await CreateService().GetAllJobsAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(2).Items);
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(2));
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(1));
        Assert.That(result.HasPreviousPage, Is.False);
        Assert.That(result.HasNextPage, Is.False);

        _jobRepository.Verify(r => r.GetAllJobsAsync(query), Times.Once);
    }

    [Test]
    public void GetAllJobsAsync_WhenPageBelowOne_ShouldThrowValidationException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetAllJobsAsync(new JobQuery { Page = 0 }));
        _jobRepository.Verify(r => r.GetAllJobsAsync(It.IsAny<JobQuery>()), Times.Never);
    }

    [Test]
    public void GetAllJobsAsync_WhenPageSizeBelowOne_ShouldThrowValidationException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetAllJobsAsync(new JobQuery { PageSize = 0 }));
        _jobRepository.Verify(r => r.GetAllJobsAsync(It.IsAny<JobQuery>()), Times.Never);
    }

    [Test]
    public void GetAllJobsAsync_WhenPageSizeAboveMax_ShouldThrowValidationException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetAllJobsAsync(new JobQuery { PageSize = PaginationValidator.MaxPageSize + 1 }));
        _jobRepository.Verify(r => r.GetAllJobsAsync(It.IsAny<JobQuery>()), Times.Never);
    }

    [Test]
    public async Task GetJobByPublicIdAsync_WhenJobExists_ShouldReturnJob()
    {
        // Arrange
        var job = ServiceTestData.QueuedJob();
        _jobRepository.Setup(r => r.GetByJobPublicIdAsync(job.JobPublicId)).ReturnsAsync(job);

        // Act
        var result = await CreateService().GetJobByPublicIdAsync(job.JobPublicId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.JobId, Is.EqualTo(job.JobPublicId));
    }

    [Test]
    public async Task GetJobByPublicIdAsync_WhenJobDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByJobPublicIdAsync(jobId)).ReturnsAsync((Job?)null);

        // Act
        var result = await CreateService().GetJobByPublicIdAsync(jobId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetJobStatusCountsAsync_ShouldReturnCountsFromRepository()
    {
        // Arrange
        var counts = new JobStatusCounts { Queued = 3, InProgress = 2, Completed = 1, Failed = 0 };
        _jobRepository
            .Setup(r => r.CountJobsByStatusAsync())
            .ReturnsAsync(counts);

        // Act
        var result = await CreateService().GetJobStatusCountsAsync();

        // Assert
        Assert.That(result, Is.SameAs(counts));
        Assert.That(result.Total, Is.EqualTo(6));

        _jobRepository.Verify(r => r.CountJobsByStatusAsync(), Times.Once);
    }

    [Test]
    public void GetJobByPublicIdAsync_WhenEmptyJobId_ShouldThrowException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetJobByPublicIdAsync(Guid.Empty));
    }
}
