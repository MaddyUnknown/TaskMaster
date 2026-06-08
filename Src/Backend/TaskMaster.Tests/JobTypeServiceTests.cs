using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Services;

namespace TaskMaster.Tests;

public class JobTypeServiceTests
{
    [Test]
    public async Task CreateJobType_ShouldPersistJobType()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IRepository<JobType>>(MockBehavior.Strict);
        JobType? persisted = null;

        repository.Setup(r => r.Add(It.IsAny<JobType>())).Callback<JobType>(j => persisted = j);

        var service = new JobTypeService(unitOfWork.Object, repository.Object);

        // Act
        var result = await service.CreateJobTypeAsync(new CreateJobType { Name = "email", Version = 1, Schema = "{}" });

        // Assert
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Name, Is.EqualTo("email"));
        Assert.That(persisted.Version, Is.EqualTo(1));
        Assert.That(result.Schema, Is.EqualTo("{}"));

        repository.Verify(r => r.Add(It.IsAny<JobType>()), Times.Once);
    }
}
