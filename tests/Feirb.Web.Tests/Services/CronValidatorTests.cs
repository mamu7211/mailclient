using Feirb.Web.Services;
using FluentAssertions;

namespace Feirb.Web.Tests.Services;

public class CronValidatorTests
{
    [Theory]
    [InlineData("0 * * * * ?")]       // Every minute
    [InlineData("0 */5 * * * ?")]     // Every 5 minutes
    [InlineData("0 */15 * * * ?")]    // Every 15 minutes
    [InlineData("0 0 * * * ?")]       // Every hour
    [InlineData("0 0 12 * * ?")]      // Every day at noon
    [InlineData("0 0 0 ? * MON-FRI")] // Weekdays at midnight
    public void Validate_ValidExpression_ReturnsValid(string expression)
    {
        var (isValid, description) = CronValidator.Validate(expression);

        isValid.Should().BeTrue();
        description.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-cron")]
    [InlineData("* *")]
    [InlineData("0 0 0 0 0")]
    public void Validate_InvalidExpression_ReturnsInvalid(string? expression)
    {
        var (isValid, description) = CronValidator.Validate(expression);

        isValid.Should().BeFalse();
        description.Should().BeNull();
    }

    [Fact]
    public void Validate_EveryMinute_ReturnsHumanReadableDescription()
    {
        var (_, description) = CronValidator.Validate("0 * * * * ?");

        description.Should().NotBeNull();
        description!.ToLowerInvariant().Should().Contain("minute");
    }

    [Fact]
    public void Validate_SevenFieldExpression_ReturnsValid()
    {
        // 7-field Quartz format includes year
        var (isValid, description) = CronValidator.Validate("0 0 12 ? * MON-FRI 2026");

        isValid.Should().BeTrue();
        description.Should().NotBeNullOrWhiteSpace();
    }
}
