using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Services;

namespace TaskMaster.Test.UnitTests.APITests;

public class JobTypeServiceTests
{
    [Test]
    public async Task CreateJobType_ShouldPersistJobType()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IRepository<JobType>>(MockBehavior.Strict);
        var jobTypeRepository = new Mock<IJobTypeRepository>(MockBehavior.Strict);
        JobType? persisted = null;

        repository.Setup(r => r.Add(It.IsAny<JobType>())).Callback<JobType>(j => persisted = j);

        var service = new JobTypeService(unitOfWork.Object, repository.Object, jobTypeRepository.Object);

        // Act
        var result = await service.CreateJobTypeAsync(new CreateJobType { Name = "email", Version = 1, Schema = "{}" });

        // Assert
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Name, Is.EqualTo("email"));
        Assert.That(persisted.Version, Is.EqualTo(1));
        Assert.That(result.Schema, Is.EqualTo("{}"));

        repository.Verify(r => r.Add(It.IsAny<JobType>()), Times.Once);
    }

    [Test]
    public async Task GetJobTypeAsync_WhenJobTypeExists_ShouldReturnDetails()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var repository = new Mock<IRepository<JobType>>(MockBehavior.Strict);
        var jobTypeRepository = new Mock<IJobTypeRepository>(MockBehavior.Strict);
        var jobType = new JobType { Name = "email", Version = 1, Schema = "{}" };

        jobTypeRepository
            .Setup(r => r.GetByJobTypeNameAndVersionAsync(jobType.Name, jobType.Version))
            .ReturnsAsync(jobType);

        var service = new JobTypeService(unitOfWork.Object, repository.Object, jobTypeRepository.Object);

        // Act
        var result = await service.GetJobTypeAsync(new JobTypeRef { Name = jobType.Name, Version = jobType.Version });

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo(jobType.Name));
        Assert.That(result.Version, Is.EqualTo(jobType.Version));
        Assert.That(result.Schema, Is.EqualTo(jobType.Schema));
    }
}
