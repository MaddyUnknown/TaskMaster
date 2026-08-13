using Moq;
using TaskMaster.API.Entities;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Services;
using TaskMaster.API.Validation;

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
    public async Task GetAllJobTypesAsync_WhenJobTypesExist_ShouldReturnPagedJobTypes()
    {
        // Arrange
        var jobTypes = new[]
        {
            new JobType { Name = "email", Version = 1, Schema = "{}" },
            new JobType { Name = "video", Version = 2, Schema = "{}" }
        };
        var query = new PaginationQuery { Page = 1, PageSize = 20 };
        _jobTypeRepository
            .Setup(r => r.GetAllJobTypesAsync(query))
            .ReturnsAsync(PagedResult<JobType>.Create(jobTypes, query.Page!.Value, query.PageSize!.Value, jobTypes.Length));

        // Act
        var result = await CreateService().GetAllJobTypesAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(2).Items);
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(1));

        _jobTypeRepository.Verify(r => r.GetAllJobTypesAsync(query), Times.Once);
    }

    [Test]
    public async Task GetAllJobTypesAsync_WhenPagingAcrossPages_ShouldReturnRemainingItems()
    {
        // Arrange
        var jobTypes = new[]
        {
            new JobType { Name = "email", Version = 1, Schema = "{}" },
            new JobType { Name = "video", Version = 2, Schema = "{}" },
            new JobType { Name = "audio", Version = 3, Schema = "{}" }
        };
        var query = new PaginationQuery { Page = 2, PageSize = 2 };
        _jobTypeRepository
            .Setup(r => r.GetAllJobTypesAsync(query))
            .ReturnsAsync(PagedResult<JobType>.Create(jobTypes.TakeLast(1), query.Page!.Value, query.PageSize!.Value, jobTypes.Length));

        // Act
        var result = await CreateService().GetAllJobTypesAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(1).Items);
        Assert.That(result.TotalCount, Is.EqualTo(3));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.HasPreviousPage, Is.True);
        Assert.That(result.HasNextPage, Is.False);
    }

    [Test]
    public async Task GetAllJobTypesAsync_WhenNoPagingParamsProvided_ShouldReturnAllJobTypesUnpaged()
    {
        // Arrange
        var jobTypes = new[]
        {
            new JobType { Name = "email", Version = 1, Schema = "{}" },
            new JobType { Name = "video", Version = 2, Schema = "{}" }
        };
        var query = new PaginationQuery();
        _jobTypeRepository
            .Setup(r => r.GetAllJobTypesAsync(query))
            .ReturnsAsync(PagedResult<JobType>.Unpaged(jobTypes));

        // Act
        var result = await CreateService().GetAllJobTypesAsync(query);

        // Assert
        Assert.That(result.Items, Has.Exactly(2).Items);
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(2));
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(1));
        Assert.That(result.HasPreviousPage, Is.False);
        Assert.That(result.HasNextPage, Is.False);

        _jobTypeRepository.Verify(r => r.GetAllJobTypesAsync(query), Times.Once);
    }

    [Test]
    public void GetAllJobTypesAsync_WhenPageBelowOne_ShouldThrowValidationException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetAllJobTypesAsync(new PaginationQuery { Page = 0 }));
        _jobTypeRepository.Verify(r => r.GetAllJobTypesAsync(It.IsAny<PaginationQuery>()), Times.Never);
    }

    [Test]
    public void GetAllJobTypesAsync_WhenPageSizeAboveMax_ShouldThrowValidationException()
    {
        // Act + Assert
        Assert.ThrowsAsync<ValidationException>(async () => await CreateService().GetAllJobTypesAsync(new PaginationQuery { PageSize = PaginationValidator.MaxPageSize + 1 }));
        _jobTypeRepository.Verify(r => r.GetAllJobTypesAsync(It.IsAny<PaginationQuery>()), Times.Never);
    }
}
