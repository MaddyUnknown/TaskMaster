using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Events;
using TaskMaster.API.Handlers;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;

namespace TaskMaster.Test.UnitTests.APITests;

public class SystemActivityEventHandlerTests
{
    private Mock<IRepository<SystemActivity>> _repository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    private SystemActivityEventHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [SetUp]
    public void Setup()
    {
        _repository = new(MockBehavior.Strict);
        _unitOfWork = new();
        _repository.Setup(r => r.Add(It.IsAny<SystemActivity>()));
        _unitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);
    }

    [Test]
    public async Task HandleJobCreated_ShouldAddJobCreatedActivity()
    {
        // Arrange
        SystemActivity? added = null;
        _repository.Setup(r => r.Add(It.IsAny<SystemActivity>())).Callback<SystemActivity>(a => added = a);

        var @event = new JobCreatedEvent { JobId = Guid.NewGuid(), JobTypeName = "email", JobTypeVersion = 1 };

        // Act
        await CreateHandler().HandleAsync(@event);

        // Assert
        Assert.That(added, Is.Not.Null);
        Assert.That(added!.EntityType, Is.EqualTo(EntityType.Job));
        Assert.That(added.EntityId, Is.EqualTo(@event.JobId));
        Assert.That(added.ActivityType, Is.EqualTo(ActivityType.JobCreated));
        Assert.That(added.Message, Is.EqualTo($"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' created"));
        _unitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Test]
    public async Task HandleJobCompleted_ShouldAddJobCompletedActivity()
    {
        // Arrange
        SystemActivity? added = null;
        _repository.Setup(r => r.Add(It.IsAny<SystemActivity>())).Callback<SystemActivity>(a => added = a);

        var @event = new JobCompletedEvent
        {
            JobId = Guid.NewGuid(),
            JobTypeName = "email",
            JobTypeVersion = 1,
            WorkerId = Guid.NewGuid(),
            WorkerName = "worker-a"
        };

        // Act
        await CreateHandler().HandleAsync(@event);

        // Assert
        Assert.That(added, Is.Not.Null);
        Assert.That(added!.ActivityType, Is.EqualTo(ActivityType.JobCompleted));
        Assert.That(added.Message, Is.EqualTo($"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' completed by worker '{@event.WorkerName}'"));
        _unitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Test]
    public async Task HandleJobFailed_ShouldAddJobFailedActivity()
    {
        // Arrange
        SystemActivity? added = null;
        _repository.Setup(r => r.Add(It.IsAny<SystemActivity>())).Callback<SystemActivity>(a => added = a);

        var @event = new JobFailedEvent
        {
            JobId = Guid.NewGuid(),
            JobTypeName = "email",
            JobTypeVersion = 1,
            WorkerId = Guid.NewGuid(),
            WorkerName = "worker-a"
        };

        // Act
        await CreateHandler().HandleAsync(@event);

        // Assert
        Assert.That(added, Is.Not.Null);
        Assert.That(added!.ActivityType, Is.EqualTo(ActivityType.JobFailed));
        Assert.That(added.Message, Is.EqualTo($"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' failed by worker '{@event.WorkerName}'"));
        _unitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Test]
    public async Task HandleWorkerRegistered_ShouldAddWorkerRegisteredActivity()
    {
        // Arrange
        SystemActivity? added = null;
        _repository.Setup(r => r.Add(It.IsAny<SystemActivity>())).Callback<SystemActivity>(a => added = a);

        var @event = new WorkerRegisteredEvent { WorkerId = Guid.NewGuid(), WorkerName = "worker-a" };

        // Act
        await CreateHandler().HandleAsync(@event);

        // Assert
        Assert.That(added, Is.Not.Null);
        Assert.That(added!.EntityType, Is.EqualTo(EntityType.Worker));
        Assert.That(added.EntityId, Is.EqualTo(@event.WorkerId));
        Assert.That(added.ActivityType, Is.EqualTo(ActivityType.WorkerRegistered));
        Assert.That(added.Message, Is.EqualTo($"Worker '{@event.WorkerName}' registered"));
        _unitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Test]
    public async Task HandleWorkerInactive_ShouldAddWorkerInactiveActivity()
    {
        // Arrange
        SystemActivity? added = null;
        _repository.Setup(r => r.Add(It.IsAny<SystemActivity>())).Callback<SystemActivity>(a => added = a);

        var @event = new WorkerInactiveEvent { WorkerId = Guid.NewGuid(), WorkerName = "worker-a" };

        // Act
        await CreateHandler().HandleAsync(@event);

        // Assert
        Assert.That(added, Is.Not.Null);
        Assert.That(added!.ActivityType, Is.EqualTo(ActivityType.WorkerInactive));
        Assert.That(added.Message, Is.EqualTo($"Worker '{@event.WorkerName}' deactivated (expired)"));
        _unitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Test]
    public async Task HandleWorkerRemoved_ShouldAddWorkerRemovedActivity()
    {
        // Arrange
        SystemActivity? added = null;
        _repository.Setup(r => r.Add(It.IsAny<SystemActivity>())).Callback<SystemActivity>(a => added = a);

        var @event = new WorkerRemovedEvent { WorkerId = Guid.NewGuid(), WorkerName = "worker-a" };

        // Act
        await CreateHandler().HandleAsync(@event);

        // Assert
        Assert.That(added, Is.Not.Null);
        Assert.That(added!.ActivityType, Is.EqualTo(ActivityType.WorkerRemoved));
        Assert.That(added.Message, Is.EqualTo($"Worker '{@event.WorkerName}' removed"));
        _unitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }
}
