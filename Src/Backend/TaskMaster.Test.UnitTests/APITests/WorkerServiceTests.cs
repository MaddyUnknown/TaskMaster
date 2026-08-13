using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.API.Configs;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Events;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Models.Enums;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Publisher;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Services;
using TaskMaster.API.Validation;
using TaskMaster.Test.UnitTests.Data;

namespace TaskMaster.Test.UnitTests.APITests;

public class WorkerServiceTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IRepository<Worker>> _workerCrudRepository = null!;
    private Mock<IWorkerRepository> _workerRepository = null!;
    private Mock<IJobRepository> _jobRepository = null!;
    private Mock<IJobTypeRepository> _jobTypeRepository = null!;
    private Mock<IValidator<RegisterWorker>> _registerWorkerValidator = null!;
    private Mock<IEventPublisher> _eventPublisher = null!;
    private IOptions<WorkerConfig> _workerOptions = null!;

    private WorkerService CreateService() =>
        new(_unitOfWork.Object, _workerCrudRepository.Object, _workerRepository.Object, _jobRepository.Object, _jobTypeRepository.Object, _workerOptions, _registerWorkerValidator.Object, _eventPublisher.Object);

    [SetUp]
    public void SetupMock()
    {
        _unitOfWork = new();
        _workerCrudRepository = new(MockBehavior.Strict);
        _workerRepository = new(MockBehavior.Strict);
        _jobRepository = new(MockBehavior.Strict);
        _jobTypeRepository = new(MockBehavior.Strict);
        _registerWorkerValidator = new(MockBehavior.Strict);
        _registerWorkerValidator
            .Setup(v => v.Validate(It.IsAny<RegisterWorker>()))
            .Returns(Array.Empty<string>());
        _eventPublisher = new(MockBehavior.Strict);
        _workerOptions = Options.Create(new WorkerConfig
        {
            HeartBeatIntervalSeconds = 15,
            WorkerExpiryIntervalSeconds = 40
        });
    }

    [Test]
    public async Task RegisterAsync_WhenValidWorker_ShouldCreateNewWorker()
    {
        // Arrange
        var emailJobType = ServiceTestData.EmailJobType();
        var videoJobType = ServiceTestData.VideoJobType();
        Worker? persisted = null;

        var request = new RegisterWorker
        {
            WorkerName = "worker-a",
            JobTypeCapabilities = [ServiceTestData.EmailJobTypeRef, ServiceTestData.VideoJobTypeRef]
        };

        _jobTypeRepository
            .Setup(r => r.GetByJobTypeNameAndVersionAsync(It.IsAny<IEnumerable<(string jobTypeName, long jobTypeVersion)>>()))
            .ReturnsAsync([emailJobType, videoJobType]);

        _workerRepository
            .Setup(r => r.GetByWorkerNameAsync(request.WorkerName, true))
            .ReturnsAsync((Worker?)null);

        _workerCrudRepository.Setup(r => r.Add(It.IsAny<Worker>()))
            .Callback<Worker>(w => persisted = w);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<WorkerRegisteredEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().RegisterAsync(request);

        // Assert
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.WorkerName, Is.EqualTo("worker-a"));
        Assert.That(persisted.Status, Is.EqualTo(WorkerStatusEnum.Active));
        Assert.That(persisted.WorkerCapabilities, Has.Count.EqualTo(2));
        Assert.That(persisted.WorkerCapabilities.Count(c => c.JobType == emailJobType), Is.EqualTo(1));
        Assert.That(persisted.WorkerCapabilities.Count(c => c.JobType == videoJobType), Is.EqualTo(1));

        Assert.That(result.WorkerDetails.WorkerId, Is.EqualTo(persisted.WorkerPublicId));
        Assert.That(result.WorkerDetails.Status, Is.EqualTo(WorkerStatusEnum.Active));
        Assert.That(result.HeartBeatIntervalSeconds, Is.EqualTo(15));

        _workerCrudRepository.Verify(r => r.Add(It.IsAny<Worker>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<WorkerRegisteredEvent>(e => e.WorkerId == persisted!.WorkerPublicId && e.WorkerName == persisted.WorkerName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void RegisterAsync_WhenInvalidCapabilities_ShouldRejectThrowException()
    {
        // Arrange
        var request = new RegisterWorker
        {
            WorkerName = "worker-a",
            JobTypeCapabilities = [ServiceTestData.EmailJobTypeRef, new() { Name = "missing", Version = 1 }]
        };

        _jobTypeRepository
            .Setup(r => r.GetByJobTypeNameAndVersionAsync(It.IsAny<IEnumerable<(string jobTypeName, long jobTypeVersion)>>()))
            .ReturnsAsync([ServiceTestData.EmailJobType()]);

        _workerRepository
            .Setup(r => r.GetByWorkerNameAsync(request.WorkerName, true))
            .ReturnsAsync((Worker?)null);

        _workerCrudRepository.Setup(r => r.Add(It.IsAny<Worker>()));

        // Act
        var act = () => CreateService().RegisterAsync(request);

        // Assert
        Assert.ThrowsAsync<ValidationException>(async () => await act());

        _workerCrudRepository.Verify(r => r.Add(It.IsAny<Worker>()), Times.Never);
        _workerCrudRepository.Verify(r => r.Update(It.IsAny<Worker>()), Times.Never);
    }

    [Test]
    public void RegisterAsync_WhenValidationFails_ShouldThrowException()
    {
        // Arrange
        _registerWorkerValidator
            .Setup(v => v.Validate(It.IsAny<RegisterWorker>()))
            .Returns(["Invalid registration"]);

        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().RegisterAsync(new RegisterWorker()));
        _workerCrudRepository.Verify(r => r.Add(It.IsAny<Worker>()), Times.Never);
        _workerCrudRepository.Verify(r => r.Update(It.IsAny<Worker>()), Times.Never);
    }

    [Test]
    public void RegisterAsync_WhenWorkerNameAlreadyActive_ShouldThrowException()
    {
        // Arrange
        var existingWorker = ServiceTestData.ActiveWorker();
        var request = new RegisterWorker
        {
            WorkerName = existingWorker.WorkerName,
            JobTypeCapabilities = [ServiceTestData.EmailJobTypeRef]
        };

        _workerRepository
            .Setup(r => r.GetByWorkerNameAsync(request.WorkerName, true))
            .ReturnsAsync(existingWorker);

        // Act
        var act = () => CreateService().RegisterAsync(request);

        // Assert
        Assert.ThrowsAsync<ValidationException>(async () => await act());

        _workerCrudRepository.Verify(r => r.Add(It.IsAny<Worker>()), Times.Never);
        _workerCrudRepository.Verify(r => r.Update(It.IsAny<Worker>()), Times.Never);
    }

    [Test]
    public async Task RegisterAsync_WhenInactiveWorkerReRegisters_ShouldReactivateWorker()
    {
        // Arrange
        var emailJobType = ServiceTestData.EmailJobType();
        var inactiveWorker = ServiceTestData.InactiveWorker(capabilities: [emailJobType]);
        Worker? updated = null;

        var request = new RegisterWorker
        {
            WorkerName = inactiveWorker.WorkerName,
            JobTypeCapabilities = [ServiceTestData.EmailJobTypeRef, ServiceTestData.VideoJobTypeRef]
        };

        _workerRepository
            .Setup(r => r.GetByWorkerNameAsync(request.WorkerName, true))
            .ReturnsAsync(inactiveWorker);

        _jobTypeRepository
            .Setup(r => r.GetByJobTypeNameAndVersionAsync(It.IsAny<IEnumerable<(string jobTypeName, long jobTypeVersion)>>()))
            .ReturnsAsync([emailJobType, ServiceTestData.VideoJobType()]);

        _jobRepository
            .Setup(r => r.UnassignJobForWorkerIdAsync(inactiveWorker.Id))
            .ReturnsAsync(0);

        _workerCrudRepository.Setup(r => r.Update(It.IsAny<Worker>()))
            .Callback<Worker>(w => updated = w);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<WorkerRegisteredEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().RegisterAsync(request);

        // Assert
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo(WorkerStatusEnum.Active));
        Assert.That(updated.WorkerCapabilities, Has.Count.EqualTo(2));

        Assert.That(result.WorkerDetails.Status, Is.EqualTo(WorkerStatusEnum.Active));

        _workerCrudRepository.Verify(r => r.Update(It.IsAny<Worker>()), Times.Once);
        _jobRepository.Verify(r => r.UnassignJobForWorkerIdAsync(inactiveWorker.Id), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<WorkerRegisteredEvent>(e => e.WorkerId == inactiveWorker.WorkerPublicId && e.WorkerName == inactiveWorker.WorkerName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveAsync_WhenValidWorker_ShouldDeactivateWorkerAndUnassignJobs()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker(capabilities: [ServiceTestData.EmailJobType()]);

        _workerRepository
            .Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId))
            .ReturnsAsync(worker);

        _workerCrudRepository.Setup(r => r.Update(It.IsAny<Worker>()));

        _jobRepository
            .Setup(r => r.UnassignJobForWorkerIdAsync(worker.Id))
            .ReturnsAsync(3);

        _eventPublisher
            .Setup(p => p.PublishAsync(It.IsAny<WorkerRemovedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().RemoveAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result.Status, Is.EqualTo(WorkerStatusEnum.InActive));
        Assert.That(worker.Status, Is.EqualTo(WorkerStatusEnum.InActive));

        _workerCrudRepository.Verify(r => r.Update(worker), Times.Once);
        _jobRepository.Verify(r => r.UnassignJobForWorkerIdAsync(worker.Id), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<WorkerRemovedEvent>(e => e.WorkerId == worker.WorkerPublicId && e.WorkerName == worker.WorkerName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void RemoveAsync_WhenUnknownWorker_ShouldThrowException()
    {
        // Arrange
        var workerId = Guid.NewGuid();

        _workerRepository.Setup(r => r.GetByPublicIdAsync(workerId))
            .ReturnsAsync((Worker?)null);

        _workerCrudRepository.Setup(r => r.Update(It.IsAny<Worker>()));

        _jobRepository.Setup(r => r.UnassignJobForWorkerIdAsync(It.IsAny<long>()));

        // Act
        var act = () => CreateService().RemoveAsync(workerId);

        // Assert
        Assert.ThrowsAsync<NotFoundException>(async () => await act());

        _workerCrudRepository.Verify(r => r.Update(It.IsAny<Worker>()), Times.Never);
        _jobRepository.Verify(r => r.UnassignJobForWorkerIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void RemoveAsync_WhenEmptyWorkerId_ShouldThrowException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().RemoveAsync(Guid.Empty));
        _workerRepository.Verify(r => r.GetByPublicIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task HeartBeatAsync_WhenValidWorker_ShouldExtendWorkerExpiration()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();

        _workerRepository.Setup(r => r.UpdateWorkerExpiryAndReturnAsync(worker.WorkerPublicId, _workerOptions.Value.WorkerExpiryIntervalSeconds))
            .ReturnsAsync(worker);

        // Act
        var result = await CreateService().HeartBeatAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result.ActionStatus, Is.EqualTo(ActionStatusEnum.Ok));

        _workerRepository.Verify(r => r.UpdateWorkerExpiryAndReturnAsync(worker.WorkerPublicId, _workerOptions.Value.WorkerExpiryIntervalSeconds), Times.Once);
    }

    [Test]
    public async Task HeartBeatAsync_WhenUnknownWorker_ShouldFailHeartBeat()
    {
        // Arrange
        var workerPublicId = Guid.NewGuid();
        _workerRepository.Setup(r => r.UpdateWorkerExpiryAndReturnAsync(workerPublicId, _workerOptions.Value.WorkerExpiryIntervalSeconds)).ReturnsAsync((Worker?)null);

        // Act
        var result = await CreateService().HeartBeatAsync(workerPublicId);

        // Assert
        Assert.That(result.ActionStatus, Is.EqualTo(ActionStatusEnum.Failed));

        _workerRepository.Verify(r => r.UpdateWorkerExpiryAndReturnAsync(workerPublicId, _workerOptions.Value.WorkerExpiryIntervalSeconds), Times.Once);
    }

    [Test]
    public void HeartBeatAsync_WhenEmptyWorkerId_ShouldThrowException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().HeartBeatAsync(Guid.Empty));
        _workerRepository.Verify(r => r.UpdateWorkerExpiryAndReturnAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetAllWorkersAsync_WhenWorkersExist_ShouldReturnPagedWorkers()
    {
        // Arrange
        var workers = new[] { ServiceTestData.ActiveWorker(), ServiceTestData.ActiveWorker() };
        var query = new WorkerQuery { Page = 1, PageSize = 20 };
        _workerRepository
            .Setup(r => r.GetAllWorkersAsync(query))
            .ReturnsAsync(PagedResult<Worker>.Create(workers, query.Page!.Value, query.PageSize!.Value, workers.Length));

        // Act
        var result = await CreateService().GetAllWorkersAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(2).Items);
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(1));

        _workerRepository.Verify(r => r.GetAllWorkersAsync(query), Times.Once);
    }

    [Test]
    public async Task GetAllWorkersAsync_WhenStatusFilterProvided_ShouldPassQueryToRepository()
    {
        // Arrange
        var query = new WorkerQuery { Page = 1, PageSize = 10, Status = WorkerStatusEnum.Active };
        _workerRepository
            .Setup(r => r.GetAllWorkersAsync(It.Is<WorkerQuery>(q => q.Status == WorkerStatusEnum.Active)))
            .ReturnsAsync(PagedResult<Worker>.Create(Array.Empty<Worker>(), query.Page!.Value, query.PageSize!.Value, 0));

        // Act
        var result = await CreateService().GetAllWorkersAsync(query);

        // Assert
        Assert.That(result.Items, Is.Empty);
        _workerRepository.Verify(r => r.GetAllWorkersAsync(It.Is<WorkerQuery>(q => q.Status == WorkerStatusEnum.Active)), Times.Once);
    }

    [Test]
    public async Task GetAllWorkersAsync_WhenInactiveFilterProvided_ShouldPassQueryToRepository()
    {
        // Arrange
        var query = new WorkerQuery { Page = 1, PageSize = 10, Status = WorkerStatusEnum.InActive };
        _workerRepository
            .Setup(r => r.GetAllWorkersAsync(It.Is<WorkerQuery>(q => q.Status == WorkerStatusEnum.InActive)))
            .ReturnsAsync(PagedResult<Worker>.Create(Array.Empty<Worker>(), query.Page!.Value, query.PageSize!.Value, 0));

        // Act
        var result = await CreateService().GetAllWorkersAsync(query);

        // Assert
        Assert.That(result.Items, Is.Empty);
        _workerRepository.Verify(r => r.GetAllWorkersAsync(It.Is<WorkerQuery>(q => q.Status == WorkerStatusEnum.InActive)), Times.Once);
    }

    [Test]
    public async Task GetAllWorkersAsync_WhenNoPagingParamsProvided_ShouldReturnAllWorkersUnpaged()
    {
        // Arrange
        var workers = new[] { ServiceTestData.ActiveWorker(), ServiceTestData.ActiveWorker() };
        var query = new WorkerQuery();
        _workerRepository
            .Setup(r => r.GetAllWorkersAsync(query))
            .ReturnsAsync(PagedResult<Worker>.Unpaged(workers));

        // Act
        var result = await CreateService().GetAllWorkersAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(2).Items);
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(2));
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(1));
        Assert.That(result.HasPreviousPage, Is.False);
        Assert.That(result.HasNextPage, Is.False);

        _workerRepository.Verify(r => r.GetAllWorkersAsync(query), Times.Once);
    }

    [Test]
    public void GetAllWorkersAsync_WhenPageBelowOne_ShouldThrowValidationException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetAllWorkersAsync(new WorkerQuery { Page = 0 }));
        _workerRepository.Verify(r => r.GetAllWorkersAsync(It.IsAny<WorkerQuery>()), Times.Never);
    }

    [Test]
    public void GetAllWorkersAsync_WhenPageSizeAboveMax_ShouldThrowValidationException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetAllWorkersAsync(new WorkerQuery { PageSize = PaginationValidator.MaxPageSize + 1 }));
        _workerRepository.Verify(r => r.GetAllWorkersAsync(It.IsAny<WorkerQuery>()), Times.Never);
    }

    [Test]
    public async Task GetWorkerByPublicIdAsync_WhenWorkerExists_ShouldReturnWorker()
    {
        // Arrange
        var worker = ServiceTestData.ActiveWorker();
        _workerRepository.Setup(r => r.GetByPublicIdAsync(worker.WorkerPublicId)).ReturnsAsync(worker);

        // Act
        var result = await CreateService().GetWorkerByPublicIdAsync(worker.WorkerPublicId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.WorkerId, Is.EqualTo(worker.WorkerPublicId));
    }

    [Test]
    public async Task GetWorkerByPublicIdAsync_WhenWorkerDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var workerId = Guid.NewGuid();
        _workerRepository.Setup(r => r.GetByPublicIdAsync(workerId)).ReturnsAsync((Worker?)null);

        // Act
        var result = await CreateService().GetWorkerByPublicIdAsync(workerId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetWorkerStatusCountsAsync_ShouldReturnCountsFromRepository()
    {
        // Arrange
        var counts = new WorkerStatusCounts { Active = 4, InActive = 1 };
        _workerRepository
            .Setup(r => r.CountWorkersByStatusAsync())
            .ReturnsAsync(counts);

        // Act
        var result = await CreateService().GetWorkerStatusCountsAsync();

        // Assert
        Assert.That(result, Is.SameAs(counts));
        Assert.That(result.Total, Is.EqualTo(5));

        _workerRepository.Verify(r => r.CountWorkersByStatusAsync(), Times.Once);
    }

    [Test]
    public void GetWorkerByPublicIdAsync_WhenEmptyWorkerId_ShouldThrowException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetWorkerByPublicIdAsync(Guid.Empty));
    }
}
