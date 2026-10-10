using TaskMaster.API.Validation;

namespace TaskMaster.Test.UnitTests.APITests;

public class TimeZoneValidatorTests
{
    private readonly TimeZoneValidator _validator = new();

    [Test]
    public void Validate_WhenValidIanaTimezone_ShouldReturnNoErrors()
    {
        var errors = _validator.Validate("Europe/London");
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_WhenUtcAlias_ShouldReturnNoErrors()
    {
        var errors = _validator.Validate("UTC");
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_WhenUnknownTimezone_ShouldReturnError()
    {
        var errors = _validator.Validate("Not/AZone");
        Assert.That(errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Validate_WhenNullOrDefault_ShouldReturnError()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_validator.Validate(null!), Has.Count.EqualTo(1));
            Assert.That(_validator.Validate(""), Has.Count.EqualTo(1));
            Assert.That(_validator.Validate("   "), Has.Count.EqualTo(1));
        });
    }
}