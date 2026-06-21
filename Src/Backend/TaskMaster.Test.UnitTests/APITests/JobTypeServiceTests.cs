using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Services;

namespace TaskMaster.Test.UnitTests.APITests;

public class JobTypeServiceTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IRepository<JobType>> _repository = null!;
    private Mock<IJobTypeRepository> _jobTypeRepository = null!;

    private JobTypeService CreateService() =>
        new(_unitOfWork.Object, _repository.Object, _jobTypeRepository.Object);

    [SetUp]
    public void SetupMock()
    {
        _unitOfWork = new();
        _repository = new(MockBehavior.Strict);
        _jobTypeRepository = new(MockBehavior.Strict);
    }

    [Test]
    public async Task CreateJobType_ShouldPersistJobType()
    {
        // Arrange
        JobType? persisted = null;

        _repository.Setup(r => r.Add(It.IsAny<JobType>())).Callback<JobType>(j => persisted = j);

        // Act
        var result = await CreateService().CreateJobTypeAsync(new CreateJobType { Name = "email", Version = 1, Schema = "{}" });

        // Assert
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Name, Is.EqualTo("email"));
        Assert.That(persisted.Version, Is.EqualTo(1));
        Assert.That(result.Schema, Is.EqualTo("{}"));

        _repository.Verify(r => r.Add(It.IsAny<JobType>()), Times.Once);
    }

    [Test]
    public async Task GetJobTypeAsync_WhenJobTypeExists_ShouldReturnDetails()
    {
        // Arrange
        var jobType = new JobType { Name = "email", Version = 1, Schema = "{}" };

        _jobTypeRepository
            .Setup(r => r.GetByJobTypeNameAndVersionAsync(jobType.Name, jobType.Version))
            .ReturnsAsync(jobType);

        // Act
        var result = await CreateService().GetJobTypeAsync(new JobTypeRef { Name = jobType.Name, Version = jobType.Version });

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo(jobType.Name));
        Assert.That(result.Version, Is.EqualTo(jobType.Version));
        Assert.That(result.Schema, Is.EqualTo(jobType.Schema));
    }
}
