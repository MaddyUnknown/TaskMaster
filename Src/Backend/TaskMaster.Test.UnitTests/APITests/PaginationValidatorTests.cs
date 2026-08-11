using TaskMaster.API.Models.Common;
using TaskMaster.API.Validation;

namespace TaskMaster.Test.UnitTests.APITests;

public class PaginationValidatorTests
{
    [Test]
    public void Validate_WhenNoPagingParamsProvided_ShouldReturnNoErrors()
    {
        // Act
        var errors = PaginationValidator.Validate(new PaginationQuery());

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_WhenValidPagingParamsProvided_ShouldReturnNoErrors()
    {
        // Act
        var errors = PaginationValidator.Validate(new PaginationQuery { Page = 2, PageSize = 20 });

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_WhenPageBelowOne_ShouldReturnError()
    {
        // Act
        var errors = PaginationValidator.Validate(new PaginationQuery { Page = 0 });

        // Assert
        Assert.That(errors, Is.Not.Empty);
    }

    [Test]
    public void Validate_WhenPageSizeProvidedWithoutPage_ShouldReturnErrors()
    {
        // Act
        var errors = PaginationValidator.Validate(new PaginationQuery { PageSize = 50 });

        // Assert
        Assert.That(errors, Is.Not.Empty);
    }

    [Test]
    public void Validate_WhenPageProvidedWithoutPageSize_ShouldReturnErrors()
    {
        // Act
        var errors = PaginationValidator.Validate(new PaginationQuery { Page = 3 });

        // Assert
        Assert.That(errors, Is.Not.Empty);
    }

    [Test]
    public void Validate_WhenPageSizeBelowOne_ShouldReturnError()
    {
        // Act
        var errors = PaginationValidator.Validate(new PaginationQuery { PageSize = 0 });

        // Assert
        Assert.That(errors, Is.Not.Empty);
    }

    [Test]
    public void Validate_WhenPageSizeAboveMax_ShouldReturnError()
    {
        // Act
        var errors = PaginationValidator.Validate(new PaginationQuery { PageSize = PaginationValidator.MaxPageSize + 1 });

        // Assert
        Assert.That(errors, Is.Not.Empty);
    }
}
