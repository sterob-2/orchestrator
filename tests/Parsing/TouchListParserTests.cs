using FluentAssertions;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Xunit;

namespace Orchestrator.App.Tests.Parsing;

public class TouchListParserTests
{
    [Fact]
    public void Parse_ValidTable_ReturnsEntries()
    {
        var input = @"
| Action | Path | Description |
|--------|------|-------------|
| modify | src/A.cs | Change logic |
| add    | src/B.cs | New file |
";
        var result = TouchListParser.Parse(input);

        result.Should().HaveCount(2);
        result[0].Operation.Should().Be(TouchOperation.Modify);
        result[0].Path.Should().Be("src/A.cs");
        result[0].Notes.Should().Be("Change logic");

        result[1].Operation.Should().Be(TouchOperation.Add);
        result[1].Path.Should().Be("src/B.cs");
    }

    [Fact]
    public void Parse_GermanHeaders_ReturnsEntries()
    {
        var input = @"
| Aktion | Pfad | Beschreibung |
|--------|------|--------------|
| modify | src/A.cs | ... |
";
        var result = TouchListParser.Parse(input);
        result.Should().HaveCount(1);
        result[0].Operation.Should().Be(TouchOperation.Modify);
    }

    [Fact]
    public void Parse_InvalidOperation_Skipped()
    {
        var input = @"
| Action | Path |
|--------|------|
| unknown| src/A.cs |
";
        var result = TouchListParser.Parse(input);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        var result = TouchListParser.Parse("");
        result.Should().BeEmpty();
    }
}
