using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Services;
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

    private JobService CreateService() =>
        new(_unitOfWork.Object, _jobCrudRepository.Object, _jobTypeRepository.Object, _jobRepository.Object, _workerRepository.Object, _createJobValidator.Object);

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

        // Act
        var result = await CreateService().ChangeJobStatusAsync(job.JobPublicId, JobStatusEnum.Completed, new WorkerIdRef { WorkerId = worker.WorkerPublicId });

        // Assert
        Assert.That(result.Status, Is.EqualTo(JobStatusEnum.Completed));

        Assert.That(job.Status, Is.EqualTo(JobStatusEnum.Completed));

        _jobCrudRepository.Verify(r => r.Update(It.Is<Job>(j => j.Id == job.Id)), Times.Once);
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

        // Act
        var result = await CreateService().ChangeJobStatusAsync(job.JobPublicId, JobStatusEnum.Failed, new WorkerIdRef { WorkerId = worker.WorkerPublicId });

        // Assert
        Assert.That(result.Status, Is.EqualTo(JobStatusEnum.Failed));

        Assert.That(job.Status, Is.EqualTo(JobStatusEnum.Failed));

        _jobCrudRepository.Verify(r => r.Update(It.Is<Job>(j => j.Id == job.Id)), Times.Once);
    }

    [Test]
    public async Task ChangeJobStatusAsync_WhenJobNotExists_ShouldThrowException()
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
    public async Task GetNextWorkerJobsAsync_WhenQueuedJob_ShouldAssignJob()
    {
        // Arrange
        var jobType = ServiceTestData.EmailJobType();
        var worker = ServiceTestData.ActiveWorker(capabilities: [jobType]);
        var job = ServiceTestData.QueuedJob(jobType);

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);

        _jobRepository
            .Setup(r => r.GetNextJobForWorkerAsync(worker.Id))
            .Callback(() =>
            {
                job.Status = JobStatusEnum.InProgress;
                job.AssignedWorkerId = worker.Id;
            })
            .ReturnsAsync(job);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.JobId, Is.EqualTo(job.JobPublicId));
        Assert.That(result.Status, Is.EqualTo(JobStatusEnum.InProgress));
    }

    [Test]
    public async Task GetNextWorkerJobsAsync_WhenNoQueuedJob_ShouldReturnNull()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);

        _jobRepository
            .Setup(r => r.GetNextJobForWorkerAsync(worker.Id))
            .ReturnsAsync((Job?)null);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result, Is.Null);

        _jobRepository.Verify(r => r.GetNextJobForWorkerAsync(worker.Id), Times.Once);
    }

    [Test]
    public void GetNextWorkerJobsAsync_WhenInactiveWorker_ShouldThrowException()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();
        worker.Status = WorkerStatusEnum.InActive;

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);
        
        // Act
        var act = () => CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId);

        // Assert
        Assert.ThrowsAsync<WorkerInactiveException>(async () => await act());

        _jobRepository.Verify(r => r.GetNextJobForWorkerAsync(worker.Id), Times.Never);
    }

    [Test]
    public async Task GetNextWorkerJobsAsync_WhenWorkerWithoutCapability_ShouldNotReceiveJob()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);
        _jobRepository
            .Setup(r => r.GetNextJobForWorkerAsync(worker.Id))
            .ReturnsAsync((Job?)null);

        // Act
        var result = await CreateService().GetNextWorkerJobsAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result, Is.Null);

        _jobRepository.Verify(r => r.GetNextJobForWorkerAsync(worker.Id), Times.Once);
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
    public void GetNextWorkerJobsAsync_WhenWorkerNotFound_ShouldThrowException()
    {
        // Arrange
        var workerId = Guid.NewGuid();
        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(workerId))
            .ReturnsAsync((Worker?)null);

        // Act + Assert
        var act = () => CreateService().GetNextWorkerJobsAsync(workerId);
        Assert.ThrowsAsync<NotFoundException>(async () => await act());
        _jobRepository.Verify(r => r.GetNextJobForWorkerAsync(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task GetAllJobsAsync_ShouldReturnJobs()
    {
        // Arrange
        var jobs = new[] { ServiceTestData.QueuedJob(), ServiceTestData.QueuedJob() };
        _jobRepository.Setup(r => r.GetAllJobsAsync()).ReturnsAsync(jobs);

        // Act
        var result = await CreateService().GetAllJobsAsync();

        // Assert
        Assert.That(result, Has.Exactly(2).Items);
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
    public void GetJobByPublicIdAsync_WhenEmptyJobId_ShouldThrowException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetJobByPublicIdAsync(Guid.Empty));
    }
}
