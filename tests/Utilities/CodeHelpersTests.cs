using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Utilities;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class CodeHelpersTests
{
    [Fact]
    public void ValidateSpecFiles_WithValidPaths()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/Orchestrator.App/Valid.cs";
        temp.CreateFile(path, "// ok");

        var invalid = CodeHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Empty(invalid);
    }

    [Fact]
    public void ValidateSpecFiles_WithPathTraversalAttempts()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/../secrets.txt";

        var invalid = CodeHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Contains(path, invalid);
    }

    [Fact]
    public void ValidateSpecFiles_WithDisallowedExtensions()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/Orchestrator.App/Bad.exe";

        var invalid = CodeHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Contains(path, invalid);
    }

    [Fact]
    public void ValidateSpecFiles_WithNonExistentNewFiles()
    {
        using var temp = new TempWorkspace();
        var path = "orchestrator/src/Orchestrator.App/NewFile.cs";

        var invalid = CodeHelpers.ValidateSpecFiles(new[] { path }, temp.Workspace);

        Assert.Empty(invalid);
    }

    [Fact]
    public void IsAllowedPath_WithVariousPrefixes()
    {
        Assert.True(CodeHelpers.IsAllowedPath("orchestrator/src/Orchestrator.App/Program.cs"));
        Assert.True(CodeHelpers.IsAllowedPath("orchestrator/tests/Utilities/ExampleTests.cs"));
        Assert.True(CodeHelpers.IsAllowedPath("Assets/Scripts/Gameplay.cs"));
        Assert.True(CodeHelpers.IsAllowedPath("Assets/Tests/EditorTests.cs"));
        Assert.True(CodeHelpers.IsAllowedPath("orchestrator/README.md"));
        Assert.False(CodeHelpers.IsAllowedPath("orchestrator/docs/README.md"));
    }

    [Fact]
    public void IsAllowedExtension_ForSupportedTypes()
    {
        Assert.True(CodeHelpers.IsAllowedExtension(".cs"));
        Assert.True(CodeHelpers.IsAllowedExtension(".md"));
        Assert.True(CodeHelpers.IsAllowedExtension(".json"));
        Assert.True(CodeHelpers.IsAllowedExtension(".yml"));
        Assert.True(CodeHelpers.IsAllowedExtension(".yaml"));
        Assert.False(CodeHelpers.IsAllowedExtension(".txt"));
    }

    [Fact]
    public void IsTestFile_Detection()
    {
        Assert.True(CodeHelpers.IsTestFile("src/Tests/Example.cs"));
        Assert.True(CodeHelpers.IsTestFile("src\\Tests\\Example.cs"));
        Assert.True(CodeHelpers.IsTestFile("ExampleTests.cs"));
        Assert.False(CodeHelpers.IsTestFile("src/Example.cs"));
    }

    [Fact]
    public void StripCodeFence_WithVariousFormats()
    {
        var csharp = "```csharp\nvar x = 1;\n```";
        var json = "```json\n{ \"value\": 1 }\n```";
        var noLang = "```\nplain\n```";

        Assert.Equal("var x = 1;", CodeHelpers.StripCodeFence(csharp));
        Assert.Equal("{ \"value\": 1 }", CodeHelpers.StripCodeFence(json));
        Assert.Equal("plain", CodeHelpers.StripCodeFence(noLang));
    }

    [Fact]
    public void StripCodeFence_WithNoFence()
    {
        Assert.Equal("plain", CodeHelpers.StripCodeFence("plain"));
    }

    [Fact]
    public void StripCodeFence_WithIncompleteFence()
    {
        var content = "```\nstill open";

        Assert.Equal("```\nstill open", CodeHelpers.StripCodeFence(content));
    }

    [Fact]
    public void Truncate_WithVariousLengths()
    {
        Assert.Equal("short", CodeHelpers.Truncate("short", 10));
        Assert.Equal("abcde\n...truncated...", CodeHelpers.Truncate("abcdef", 5));
    }
}
