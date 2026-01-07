using System.IO;
using System.Reflection;
using Orchestrator.App;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests;

public class ProgramTests
{
    [Fact]
    public void ValidateConfig_ReturnsFalse_WhenRepoMissing()
    {
        var cfg = MockWorkContext.CreateConfig() with { RepoOwner = "", RepoName = "" };
        var method = typeof(Program).GetMethod("ValidateConfig", BindingFlags.NonPublic | BindingFlags.Static);

        var result = (bool)method!.Invoke(null, new object[] { cfg })!;

        Assert.False(result);
    }

    [Fact]
    public void ValidateConfig_ReturnsTrue_WhenRepoProvided()
    {
        var cfg = MockWorkContext.CreateConfig();
        var method = typeof(Program).GetMethod("ValidateConfig", BindingFlags.NonPublic | BindingFlags.Static);

        var result = (bool)method!.Invoke(null, new object[] { cfg })!;

        Assert.True(result);
    }

    [Fact]
    public void LogStartupInfo_WritesExpectedMessages()
    {
        var cfg = MockWorkContext.CreateConfig();
        var method = typeof(Program).GetMethod("LogStartupInfo", BindingFlags.NonPublic | BindingFlags.Static);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            method!.Invoke(null, new object[] { cfg });
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = writer.ToString();
        Assert.Contains($"Repo: {cfg.RepoOwner}/{cfg.RepoName}", output);
        Assert.Contains(cfg.Labels.WorkItemLabel, output);
    }

    [Fact]
    public void LoadEnvironmentFiles_DoesNotThrow()
    {
        var method = typeof(Program).GetMethod("LoadEnvironmentFiles", BindingFlags.NonPublic | BindingFlags.Static);

        var exception = Record.Exception(() => method!.Invoke(null, Array.Empty<object>()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Main_ReturnsZero_WhenInitOnly()
    {
        using var workspace = new TempWorkspace();
        var originalOwner = Environment.GetEnvironmentVariable("REPO_OWNER");
        var originalName = Environment.GetEnvironmentVariable("REPO_NAME");
        var originalWorkspace = Environment.GetEnvironmentVariable("WORKSPACE_PATH");
        var originalWorkspaceHost = Environment.GetEnvironmentVariable("WORKSPACE_HOST_PATH");
        var originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("REPO_OWNER", "test-owner");
            Environment.SetEnvironmentVariable("REPO_NAME", "test-repo");
            Environment.SetEnvironmentVariable("WORKSPACE_PATH", workspace.WorkspacePath);
            Environment.SetEnvironmentVariable("WORKSPACE_HOST_PATH", workspace.WorkspacePath);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");

            var result = await Program.Main(new[] { "--init-only" });

            Assert.Equal(0, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REPO_OWNER", originalOwner);
            Environment.SetEnvironmentVariable("REPO_NAME", originalName);
            Environment.SetEnvironmentVariable("WORKSPACE_PATH", originalWorkspace);
            Environment.SetEnvironmentVariable("WORKSPACE_HOST_PATH", originalWorkspaceHost);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);
        }
    }
}
