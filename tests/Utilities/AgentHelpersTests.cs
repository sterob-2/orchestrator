using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class AgentHelpersTests
{
    [Fact]
    public void ValidateSpecFiles_WithValidPaths()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/Orchestrator.App/Valid.cs";
        temp.CreateFile(path, "// ok");

        var invalid = AgentHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Empty(invalid);
    }

    [Fact]
    public void ValidateSpecFiles_WithPathTraversalAttempts()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/../secrets.txt";

        var invalid = AgentHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Contains(path, invalid);
    }

    [Fact]
    public void ValidateSpecFiles_WithDisallowedExtensions()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/Orchestrator.App/Bad.exe";

        var invalid = AgentHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Contains(path, invalid);
    }

    [Fact]
    public void ValidateSpecFiles_WithNonExistentNewFiles()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/Orchestrator.App/NewFile.cs";

        var invalid = AgentHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Empty(invalid);
    }

    [Fact]
    public void IsAllowedPath_WithVariousPrefixes()
    {
        Assert.True(AgentHelpers.IsAllowedPath("orchestrator/src/Orchestrator.App/Program.cs"));
        Assert.True(AgentHelpers.IsAllowedPath("orchestrator/tests/Utilities/ExampleTests.cs"));
        Assert.True(AgentHelpers.IsAllowedPath("Assets/Scripts/Gameplay.cs"));
        Assert.True(AgentHelpers.IsAllowedPath("Assets/Tests/EditorTests.cs"));
        Assert.True(AgentHelpers.IsAllowedPath("orchestrator/README.md"));
        Assert.False(AgentHelpers.IsAllowedPath("orchestrator/docs/README.md"));
    }

    [Fact]
    public void IsAllowedExtension_ForSupportedTypes()
    {
        Assert.True(AgentHelpers.IsAllowedExtension(".cs"));
        Assert.True(AgentHelpers.IsAllowedExtension(".md"));
        Assert.True(AgentHelpers.IsAllowedExtension(".json"));
        Assert.True(AgentHelpers.IsAllowedExtension(".yml"));
        Assert.True(AgentHelpers.IsAllowedExtension(".yaml"));
        Assert.False(AgentHelpers.IsAllowedExtension(".txt"));
    }

    [Fact]
    public void IsTestFile_Detection()
    {
        Assert.True(AgentHelpers.IsTestFile("src/Tests/Example.cs"));
        Assert.True(AgentHelpers.IsTestFile("src\\Tests\\Example.cs"));
        Assert.True(AgentHelpers.IsTestFile("ExampleTests.cs"));
        Assert.False(AgentHelpers.IsTestFile("src/Example.cs"));
    }

    [Fact]
    public void StripCodeFence_WithVariousFormats()
    {
        var csharp = "```csharp\nvar x = 1;\n```";
        var json = "```json\n{ \"value\": 1 }\n```";
        var noLang = "```\nplain\n```";

        Assert.Equal("var x = 1;", AgentHelpers.StripCodeFence(csharp));
        Assert.Equal("{ \"value\": 1 }", AgentHelpers.StripCodeFence(json));
        Assert.Equal("plain", AgentHelpers.StripCodeFence(noLang));
    }

    [Fact]
    public void StripCodeFence_WithNoFence()
    {
        Assert.Equal("plain", AgentHelpers.StripCodeFence("plain"));
    }

    [Fact]
    public void StripCodeFence_WithIncompleteFence()
    {
        var content = "```\nstill open";

        Assert.Equal("```\nstill open", AgentHelpers.StripCodeFence(content));
    }

    [Fact]
    public void Truncate_WithVariousLengths()
    {
        Assert.Equal("short", AgentHelpers.Truncate("short", 10));
        Assert.Equal("abcde\n...truncated...", AgentHelpers.Truncate("abcdef", 5));
    }
}
