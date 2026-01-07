using FluentAssertions;
using Orchestrator.App.Parsing;
using Xunit;

namespace Orchestrator.App.Tests.Parsing;

public class GherkinValidatorTests
{
    [Fact]
    public void IsValid_ValidScenario_ReturnsTrue()
    {
        var input = @"
Scenario: Test
  Given context
  When action
  Then result
";
        GherkinValidator.IsValid(input).Should().BeTrue();
    }

    [Fact]
    public void IsValid_MissingKeywords_ReturnsFalse()
    {
        var input = @"
Scenario: Test
  Given context
";
        GherkinValidator.IsValid(input).Should().BeFalse();
    }

    [Fact]
    public void IsValid_Empty_ReturnsFalse()
    {
        GherkinValidator.IsValid("").Should().BeFalse();
    }
}
