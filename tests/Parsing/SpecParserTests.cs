using FluentAssertions;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Xunit;

namespace Orchestrator.App.Tests.Parsing;

public class SpecParserTests
{
    [Fact]
    public void Parse_FullSpec_ReturnsParsedModel()
    {
        var parser = new SpecParser();
        var input = @"# Spec: Test

## Goal
Make it work.

## Non-Goals
Don't break it.

## Components
- Core
- UI

## Touch List
| Action | Path | Description |
|--------|------|-------------|
| modify | src/A.cs | ... |

## Interfaces
```csharp
interface ITest {}
```

## Scenarios
Scenario: A
  Given ...
  When ...
  Then ...

Scenario: B
  Given ...
  When ...
  Then ...

## Sequence
1. Step 1
2. Step 2

## Test Matrix
| AK | Type | File |
|----|------|------|
| AK-1 | Unit | Test.cs |
";

        var result = parser.Parse(input);

        result.Goal.Should().Be("Make it work.");
        result.NonGoals.Should().Be("Don't break it.");
        result.Components.Should().HaveCount(2).And.Contain("Core");
        result.TouchList.Should().HaveCount(1);
        result.TouchList[0].Path.Should().Be("src/A.cs");
        result.Interfaces.Should().HaveCount(1);
        result.Scenarios.Should().HaveCount(2);
        result.Sequence.Should().HaveCount(2);
        result.TestMatrix.Should().HaveCount(1);
    }
}
