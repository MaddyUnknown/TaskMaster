using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces;
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
    private Mock<IValidator<CreateJobType>> _createJobTypeValidator = null!;
    private Mock<IValidator<JobTypeRef>> _jobTypeRefValidator = null!;

    private JobTypeService CreateService() =>
        new(_unitOfWork.Object, _repository.Object, _jobTypeRepository.Object, _createJobTypeValidator.Object, _jobTypeRefValidator.Object);

    [SetUp]
    public void SetupMock()
    {
        _unitOfWork = new();
        _repository = new(MockBehavior.Strict);
        _jobTypeRepository = new(MockBehavior.Strict);
        _createJobTypeValidator = new(MockBehavior.Strict);
        _createJobTypeValidator
            .Setup(v => v.Validate(It.IsAny<CreateJobType>()))
            .Returns(Array.Empty<string>());
        _jobTypeRefValidator = new(MockBehavior.Strict);
        _jobTypeRefValidator
            .Setup(v => v.Validate(It.IsAny<JobTypeRef>()))
            .Returns(Array.Empty<string>());
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
    public void CreateJobTypeAsync_WhenValidationFails_ShouldThrowException()
    {
        // Assert
        _createJobTypeValidator
            .Setup(v => v.Validate(It.IsAny<CreateJobType>()))
            .Returns(["Invalid job type"]);

        // Act + Arrange
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().CreateJobTypeAsync(new CreateJobType()));
        _repository.Verify(r => r.Add(It.IsAny<JobType>()), Times.Never);
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

    [Test]
    public async Task GetJobTypeAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        _jobTypeRepository.Setup(r => r.GetByJobTypeNameAndVersionAsync("missing", 1)).ReturnsAsync((JobType?)null);

        // Act
        var result = await CreateService().GetJobTypeAsync(new JobTypeRef { Name = "missing", Version = 1 });

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetJobTypeAsync_WhenValidationFails_ShouldThrowException()
    {
        // Arrange
        _jobTypeRefValidator
            .Setup(v => v.Validate(It.IsAny<JobTypeRef>()))
            .Returns(["Invalid job type ref"]);

        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetJobTypeAsync(new JobTypeRef { Name = "", Version = 0 }));
        _jobTypeRepository.Verify(r => r.GetByJobTypeNameAndVersionAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task GetAllJobTypesAsync_ShouldReturnJobTypes()
    {
        // Arrange
        var jobTypes = new[] { new JobType { Name = "email", Version = 1, Schema = "{}" }, new JobType { Name = "video", Version = 2, Schema = "{}" } };
        _jobTypeRepository.Setup(r => r.GetAllJobTypesAsync()).ReturnsAsync(jobTypes);

        // Act
        var result = await CreateService().GetAllJobTypesAsync();

        // Assert
        Assert.That(result, Has.Exactly(2).Items);
    }
}
