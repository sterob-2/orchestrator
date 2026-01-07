using System;
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
STATUS: Draft
UPDATED: 2024-02-10

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

## Architektur-Referenzen
- ADR-001
- docs/architecture.md

## Risiken
| Risiko | Beschreibung |
|--------|--------------|
| R1 | Timebox |

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
        result.Status.Should().Be("Draft");
        result.Updated.Should().Be(new DateTime(2024, 2, 10));
        result.ArchitectureReferences.Should().HaveCount(2).And.Contain("ADR-001");
        result.Risks.Should().HaveCount(1);
        result.Components.Should().HaveCount(2).And.Contain("Core");
        result.TouchList.Should().HaveCount(1);
        result.TouchList[0].Path.Should().Be("src/A.cs");
        result.Interfaces.Should().HaveCount(1);
        result.Scenarios.Should().HaveCount(2);
        result.Sequence.Should().HaveCount(2);
        result.TestMatrix.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_SpecWithoutOptionalSections_ReturnsEmptyValues()
    {
        var parser = new SpecParser();
        var input = @"# Spec: Test

## Goal
Make it work.
";

        var result = parser.Parse(input);

        result.Status.Should().BeEmpty();
        result.Updated.Should().BeNull();
        result.ArchitectureReferences.Should().BeEmpty();
        result.Risks.Should().BeEmpty();
    }
}
