using System;
using System.Threading.Tasks;
using Orchestrator.App;
using Orchestrator.App.Core.Configuration;
using Xunit;

namespace Orchestrator.App.Tests.Integration;

/// <summary>
/// Shared fixture for MCP integration tests - initializes Docker once for all tests
/// </summary>
public class McpTestFixture : IAsyncLifetime
{
    private McpClientManager? _manager;
    public string TestWorkspace { get; private set; }
    public bool IsInitialized => _manager != null && _manager.Tools.Count > 0;

    public McpTestFixture()
    {
        TestWorkspace = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mcp-test-{Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(TestWorkspace);
    }

    public async Task InitializeAsync()
    {
        var labels = new LabelConfig(
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
            ResetLabel: "agent:reset"
        );

        var workflow = new WorkflowConfig(
            DefaultBaseBranch: "main",
            PollIntervalSeconds: 120,
            FastPollIntervalSeconds: 30,
            UseWorkflowMode: false
        );
        var config = new OrchestratorConfig(
            OpenAiBaseUrl: "https://api.openai.com/v1",
            OpenAiApiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "test-key",
            OpenAiModel: "gpt-4o-mini",
            DevModel: "gpt-4o",
            TechLeadModel: "gpt-4o-mini",
            WorkspacePath: TestWorkspace,
            WorkspaceHostPath: TestWorkspace,
            GitRemoteUrl: "https://github.com/test/repo.git",
            GitAuthorName: "Test Agent",
            GitAuthorEmail: "test@example.com",
            GitHubToken: Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? "",
            RepoOwner: "test-owner",
            RepoName: "test-repo",
            Workflow: workflow,
            Labels: labels,
            ProjectStatusInProgress: "In Progress",
            ProjectStatusInReview: "In Review",
            ProjectOwner: "test-owner",
            ProjectOwnerType: "user",
            ProjectNumber: 1,
            ProjectStatusDone: "Done"
        );

        _manager = new McpClientManager();

        try
        {
            // Initialize MCP servers (Git server will fail gracefully if workspace isn't a git repo)
            await _manager.InitializeAsync(config);
        }
        catch (Exception ex)
        {
            // Log but don't fail - Docker might not be available in all test environments
            Console.WriteLine($"MCP initialization skipped: {ex.Message}");
        }
    }

    public async Task<string> CallToolAsync(string toolName, System.Collections.Generic.Dictionary<string, object?> arguments)
    {
        if (_manager == null)
        {
            throw new InvalidOperationException("MCP manager not initialized");
        }
        return await _manager.CallToolAsync(toolName, arguments);
    }

    public System.Collections.Generic.IEnumerable<ModelContextProtocol.Client.McpClientTool> GetToolsByServer(string serverName)
    {
        if (_manager == null)
        {
            return Enumerable.Empty<ModelContextProtocol.Client.McpClientTool>();
        }
        return _manager.GetToolsByServer(serverName);
    }

    public int GetToolCount()
    {
        return _manager?.Tools.Count ?? 0;
    }

    public async Task DisposeAsync()
    {
        if (_manager != null)
        {
            await _manager.DisposeAsync();
        }

        if (System.IO.Directory.Exists(TestWorkspace))
        {
            try
            {
                System.IO.Directory.Delete(TestWorkspace, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}

[Trait("Category", "Integration")]
public class McpIntegrationTests : IClassFixture<McpTestFixture>
{
    private readonly McpTestFixture _fixture;

    public McpIntegrationTests(McpTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FilesystemServer_ListDirectory_ReturnsFiles()
    {
        if (!_fixture.IsInitialized)
        {
            // Skip if MCP not initialized (Docker not available)
            return;
        }

        // Create test file using absolute path with unique name per test
        var testFile = System.IO.Path.Combine(_fixture.TestWorkspace, $"list-test-{Guid.NewGuid()}.txt");
        await System.IO.File.WriteAllTextAsync(testFile, "test content");
        var fileName = System.IO.Path.GetFileName(testFile);

        // MCP expects relative path from workspace root
        var result = await _fixture.CallToolAsync("list_directory", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["path"] = "."
        });

        Assert.Contains(fileName, result);
    }

    [Fact]
    public async Task FilesystemServer_ReadFile_ReturnsContent()
    {
        if (!_fixture.IsInitialized)
        {
            return;
        }

        // Create file with absolute path and unique name
        var fileName = $"read-test-{Guid.NewGuid()}.txt";
        var testFile = System.IO.Path.Combine(_fixture.TestWorkspace, fileName);
        await System.IO.File.WriteAllTextAsync(testFile, "hello world");

        // Read with relative path
        var result = await _fixture.CallToolAsync("read_file", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["path"] = fileName
        });

        Assert.Contains("hello world", result);
    }

    [Fact]
    public async Task FilesystemServer_WriteFile_CreatesFile()
    {
        if (!_fixture.IsInitialized)
        {
            return;
        }

        // Write with relative path and unique name
        var fileName = $"write-test-{Guid.NewGuid()}.txt";
        await _fixture.CallToolAsync("write_file", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["path"] = fileName,
            ["content"] = "written by MCP"
        });

        // Check with absolute path
        var testFile = System.IO.Path.Combine(_fixture.TestWorkspace, fileName);
        Assert.True(System.IO.File.Exists(testFile));
        var content = await System.IO.File.ReadAllTextAsync(testFile);
        Assert.Contains("written by MCP", content);
    }

    [Fact]
    public void GetToolsByServer_Filesystem_ReturnsFilesystemTools()
    {
        if (!_fixture.IsInitialized)
        {
            return;
        }

        var filesystemTools = _fixture.GetToolsByServer("filesystem");

        Assert.NotEmpty(filesystemTools);
        Assert.Contains(filesystemTools, t => t.Name == "read_file");
        Assert.Contains(filesystemTools, t => t.Name == "write_file");
        Assert.Contains(filesystemTools, t => t.Name == "list_directory");
    }

    [Fact]
    public void Tools_AfterInitialization_ContainsTools()
    {
        if (!_fixture.IsInitialized)
        {
            return;
        }

        Assert.True(_fixture.GetToolCount() >= 3); // Filesystem server has multiple tools
    }
}
