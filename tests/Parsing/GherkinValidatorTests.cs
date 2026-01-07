using FluentAssertions;
using Orchestrator.App.Parsing;
using Xunit;

namespace Orchestrator.App.Tests.Parsing;

public class GherkinValidatorTests
{
    [Fact]
    public void IsValid_ValidScenario_ReturnsTrue()
    {
        var validator = new GherkinValidator();
        var input = @"
Scenario: Test
  Given context
  When action
  Then result
";
        validator.IsValid(input).Should().BeTrue();
    }

    [Fact]
    public void IsValid_MissingKeywords_ReturnsFalse()
    {
        var validator = new GherkinValidator();
        var input = @"
Scenario: Test
  And context
";
        validator.IsValid(input).Should().BeFalse();
    }

    [Fact]
    public void IsValid_CodeFenceScenario_ReturnsTrue()
    {
        var validator = new GherkinValidator();
        var input = @"
```gherkin
Scenario: Test
  Given context
  When action
  Then result
```
";
        validator.IsValid(input).Should().BeTrue();
    }

    [Fact]
    public void IsValid_OutlineScenario_ReturnsTrue()
    {
        var validator = new GherkinValidator();
        var input = @"
Scenario Outline: Example
  Given <state>
  When action
  Then result

Examples:
  | state |
  | ready |
";
        validator.IsValid(input).Should().BeTrue();
    }

    [Fact]
    public void IsValid_Empty_ReturnsFalse()
    {
        var validator = new GherkinValidator();
        validator.IsValid("").Should().BeFalse();
    }
}
