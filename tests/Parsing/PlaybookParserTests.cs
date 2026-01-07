using FluentAssertions;
using Orchestrator.App.Parsing;
using Xunit;

namespace Orchestrator.App.Tests.Parsing;

public class PlaybookParserTests
{
    [Fact]
    public void Parse_ValidYaml_ReturnsModel()
    {
        var parser = new PlaybookParser();
        var yaml = @"
project: Orchestrator
version: 1.0
allowed_frameworks:
  - id: FW-01
    name: ASP.NET Core
    version: 8.x
forbidden_frameworks:
  - name: Newtonsoft.Json
    use_instead: System.Text.Json
";
        var result = parser.Parse(yaml);

        result.Project.Should().Be("Orchestrator");
        result.AllowedFrameworks.Should().HaveCount(1);
        result.AllowedFrameworks[0].Name.Should().Be("ASP.NET Core");
        result.ForbiddenFrameworks.Should().HaveCount(1);
    }
}
