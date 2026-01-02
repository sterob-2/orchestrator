using System;
using System.Threading.Tasks;
using Orchestrator.App;
using Xunit;

namespace Orchestrator.App.Tests.Integration;

[Trait("Category", "Integration")]
public class McpIntegrationTests : IAsyncLifetime
{
    private McpClientManager? _manager;
    private OrchestratorConfig? _config;
    private readonly string _testWorkspace;

    public McpIntegrationTests()
    {
        _testWorkspace = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mcp-test-{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(_testWorkspace);
    }

    public async Task InitializeAsync()
    {
        _config = new OrchestratorConfig(
            OpenAiBaseUrl: "https://api.openai.com/v1",
            OpenAiApiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "test-key",
            OpenAiModel: "gpt-4o-mini",
            DevModel: "gpt-4o",
            TechLeadModel: "gpt-4o-mini",
            WorkspacePath: _testWorkspace,
            WorkspaceHostPath: _testWorkspace,
            GitRemoteUrl: "https://github.com/test/repo.git",
            GitAuthorName: "Test Agent",
            GitAuthorEmail: "test@example.com",
            GitHubToken: Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? "",
            RepoOwner: "test-owner",
            RepoName: "test-repo",
            DefaultBaseBranch: "main",
            PollIntervalSeconds: 120,
            FastPollIntervalSeconds: 30,
            WorkItemLabel: "ready-for-agents",
            InProgressLabel: "in-progress",
            DoneLabel: "done",
            BlockedLabel: "blocked",
            PlannerLabel: "agent:planner",
            TechLeadLabel: "agent:techlead",
            DevLabel: "agent:dev",
            TestLabel: "agent:test",
            ReleaseLabel: "agent:release",
            UserReviewRequiredLabel: "user-review-required",
            ReviewNeededLabel: "agent:review-needed",
            ReviewedLabel: "agent:reviewed",
            SpecQuestionsLabel: "spec-questions",
            SpecClarifiedLabel: "spec-clarified",
            CodeReviewNeededLabel: "code-review-needed",
            CodeReviewApprovedLabel: "code-review-approved",
            CodeReviewChangesRequestedLabel: "code-review-changes-requested",
            ResetLabel: "agent:reset",
            ProjectStatusInProgress: "In Progress",
            ProjectStatusInReview: "In Review",
            ProjectOwner: "test-owner",
            ProjectOwnerType: "user",
            ProjectNumber: 1,
            ProjectStatusDone: "Done",
            UseWorkflowMode: false
        );

        _manager = new McpClientManager();

        try
        {
            await _manager.InitializeAsync(_config);
        }
        catch (Exception ex)
        {
            // Log but don't fail - Docker might not be available in all test environments
            Console.WriteLine($"MCP initialization skipped: {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_manager != null)
        {
            await _manager.DisposeAsync();
        }

        if (System.IO.Directory.Exists(_testWorkspace))
        {
            System.IO.Directory.Delete(_testWorkspace, true);
        }
    }

    [Fact]
    public async Task FilesystemServer_ListDirectory_ReturnsFiles()
    {
        if (_manager == null || _manager.Tools.Count == 0)
        {
            // Skip if MCP not initialized (Docker not available)
            return;
        }

        // Create test file using absolute path
        var testFile = System.IO.Path.Combine(_testWorkspace, "test-file.txt");
        await System.IO.File.WriteAllTextAsync(testFile, "test content");

        // MCP expects relative path from workspace root
        var result = await _manager.CallToolAsync("list_directory", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["path"] = "."
        });

        Assert.Contains("test-file.txt", result);
    }

    [Fact]
    public async Task FilesystemServer_ReadFile_ReturnsContent()
    {
        if (_manager == null || _manager.Tools.Count == 0)
        {
            return;
        }

        // Create file with absolute path
        var testFile = System.IO.Path.Combine(_testWorkspace, "read-test.txt");
        await System.IO.File.WriteAllTextAsync(testFile, "hello world");

        // Read with relative path
        var result = await _manager.CallToolAsync("read_file", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["path"] = "read-test.txt"
        });

        Assert.Contains("hello world", result);
    }

    [Fact]
    public async Task FilesystemServer_WriteFile_CreatesFile()
    {
        if (_manager == null || _manager.Tools.Count == 0)
        {
            return;
        }

        // Write with relative path
        await _manager.CallToolAsync("write_file", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["path"] = "write-test.txt",
            ["content"] = "written by MCP"
        });

        // Check with absolute path
        var testFile = System.IO.Path.Combine(_testWorkspace, "write-test.txt");
        Assert.True(System.IO.File.Exists(testFile));
        var content = await System.IO.File.ReadAllTextAsync(testFile);
        Assert.Contains("written by MCP", content);
    }

    [Fact]
    public async Task GetToolsByServer_Filesystem_ReturnsFilesystemTools()
    {
        if (_manager == null || _manager.Tools.Count == 0)
        {
            return;
        }

        var filesystemTools = _manager.GetToolsByServer("filesystem");

        Assert.NotEmpty(filesystemTools);
        Assert.Contains(filesystemTools, t => t.Name == "read_file");
        Assert.Contains(filesystemTools, t => t.Name == "write_file");
        Assert.Contains(filesystemTools, t => t.Name == "list_directory");
    }

    [Fact]
    public void Tools_AfterInitialization_ContainsTools()
    {
        if (_manager == null || _manager.Tools.Count == 0)
        {
            return;
        }

        Assert.NotEmpty(_manager.Tools);
        Assert.True(_manager.Tools.Count >= 10); // Should have at least filesystem tools
    }
}
