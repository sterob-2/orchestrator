using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Infrastructure.Filesystem;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Integration;

[Trait("Category", "Integration")]
public class WorkflowIntegrationTests
{
    [Fact]
    public async Task RunAsync_HappyPath_ReachesRelease()
    {
        using var workspace = new TempWorkspace();
        var config = MockWorkContext.CreateConfig(workspace.WorkspacePath);
        SeedWorkspace(workspace);

        var workItem = new WorkItem(
            1,
            "Happy Path Issue",
            new string('a', 80),
            "https://example.com/issues/1",
            new List<string> { config.Labels.WorkItemLabel, "estimate:3" });

        var llm = new QueueLlmClient(new[]
        {
            BuildRefinementJson(),
            BuildValidSpec(workItem),
            "namespace Example; public sealed class Example { // Updated }",
            BuildCodeReviewJson()
        });
        var github = new FakeGitHubClient();
        var repo = new FakeRepoGit();

        var context = new WorkContext(workItem, github, config, workspace.Workspace, repo, llm, null, null, new System.Collections.Concurrent.ConcurrentDictionary<string, string>());
        var workflowContext = new InMemoryWorkflowContext();
        var input = BuildInput(config, workItem);

        var refinement = new RefinementExecutor(context, config.Workflow);
        var dor = new DorExecutor(context, config.Workflow);
        var techLead = new TechLeadExecutor(context, config.Workflow);
        var specGate = new SpecGateExecutor(context, config.Workflow);
        var dev = new DevExecutor(context, config.Workflow);
        var codeReview = new CodeReviewExecutor(context, config.Workflow);
        var dod = new DodExecutor(context, config.Workflow);

        var refinementOutput = await refinement.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var dorOutput = await dor.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var techLeadOutput = await techLead.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var specGateOutput = await specGate.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var devOutput = await dev.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var codeReviewOutput = await codeReview.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var dodOutput = await dod.HandleAsync(input, workflowContext.Context, CancellationToken.None);

        Assert.NotNull(dodOutput);
        Assert.True(dorOutput.Success);
        Assert.True(techLeadOutput.Success);
        Assert.True(specGateOutput.Success);
        Assert.True(devOutput.Success);
        Assert.True(codeReviewOutput.Success);
        Assert.True(dodOutput.Success);
        Assert.Null(dodOutput.NextStage);
        Assert.True(workspace.Workspace.Exists(WorkflowPaths.SpecPath(workItem.Number)));
        Assert.True(github.OpenPullRequestCalled);
    }

    [Fact]
    public async Task RunAsync_SpecGateFailure_TriggersTechLeadLoop()
    {
        using var workspace = new TempWorkspace();
        var config = MockWorkContext.CreateConfig(workspace.WorkspacePath);
        SeedWorkspace(workspace);

        var workItem = new WorkItem(
            2,
            "Spec Gate Loop Issue",
            new string('b', 80),
            "https://example.com/issues/2",
            new List<string> { config.Labels.SpecGateLabel, "estimate:3" });

        workspace.CreateFile(WorkflowPaths.SpecPath(workItem.Number), BuildInvalidSpec(workItem));

        var llm = new QueueLlmClient(new[]
        {
            BuildValidSpec(workItem),
            "namespace Example; public sealed class Example { // Updated }",
            BuildCodeReviewJson()
        });
        var github = new FakeGitHubClient();
        var repo = new FakeRepoGit();

        var context = new WorkContext(workItem, github, config, workspace.Workspace, repo, llm, null, null, new System.Collections.Concurrent.ConcurrentDictionary<string, string>());
        var workflowContext = new InMemoryWorkflowContext();
        var input = BuildInput(config, workItem);

        var specGate = new SpecGateExecutor(context, config.Workflow);
        var techLead = new TechLeadExecutor(context, config.Workflow);
        var dev = new DevExecutor(context, config.Workflow);
        var codeReview = new CodeReviewExecutor(context, config.Workflow);
        var dod = new DodExecutor(context, config.Workflow);

        var initialGate = await specGate.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var techLeadOutput = await techLead.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var finalGate = await specGate.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var devOutput = await dev.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var codeReviewOutput = await codeReview.HandleAsync(input, workflowContext.Context, CancellationToken.None);
        var dodOutput = await dod.HandleAsync(input, workflowContext.Context, CancellationToken.None);

        Assert.NotNull(dodOutput);
        Assert.True(dodOutput.Success);
        Assert.Null(dodOutput.NextStage);
        Assert.False(initialGate.Success);
        Assert.Equal(WorkflowStage.TechLead, initialGate.NextStage);
        Assert.True(techLeadOutput.Success);
        Assert.True(finalGate.Success);
        Assert.True(devOutput.Success);
        Assert.True(codeReviewOutput.Success);
        Assert.True(dodOutput.Success);
        Assert.Equal(3, llm.CallCount);
    }

    private static void SeedWorkspace(TempWorkspace workspace)
    {
        var repoRoot = GetRepoRoot();
        workspace.CreateFile(
            "docs/architecture-playbook.yaml",
            File.ReadAllText(Path.Combine(repoRoot, "docs", "architecture-playbook.yaml")));
        workspace.CreateFile(
            "docs/templates/spec.md",
            File.ReadAllText(Path.Combine(repoRoot, "docs", "templates", "spec.md")));
        workspace.CreateFile("src/Example.cs", "namespace Example; public sealed class Example { }");
    }

    private static string BuildRefinementJson()
    {
        var refinement = new RefinementResult(
            "Clarified story that is sufficiently long to pass the Definition of Ready gate criteria and be valid.",
            new List<string>
            {
                "Given a user, when they run, then the app responds.",
                "Given a valid request, when processed, then it succeeds.",
                "Given an invalid request, when processed, then it returns an error."
            },
            new List<OpenQuestion>(),
            new ComplexityIndicators(new List<string> { "API" }, null));

        return WorkflowJson.Serialize(refinement);
    }

    private static string BuildCodeReviewJson()
    {
        var payload = new
        {
            approved = true,
            summary = "Looks good.",
            findings = Array.Empty<object>()
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string BuildValidSpec(WorkItem item)
    {
        return $$"""
# Spec: Issue {item.Number} - {item.Title}

STATUS: COMPLETE
UPDATED: 2025-01-01 00:00:00 UTC

## Goal
Implement the change using .NET 8 and Clean Architecture.

## Non-Goals
- No database changes.

## Components
- Api

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Example.cs | Update example. |

## Interfaces
```csharp
public interface IExampleService
{
    void Run();
}
```

## Scenarios
Scenario: success
Given a user
When they run
Then it works

Scenario: failure
Given a user
When they fail
Then it shows an error

Scenario: retry
Given a user
When they retry
Then it works

## Sequence
1. Initialize request
2. Handle response

## Test Matrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/ExampleTests.cs | ok |
""";
    }

    private static string BuildInvalidSpec(WorkItem item)
    {
        return $"""
# Spec: Issue {item.Number} - {item.Title}

STATUS: COMPLETE
UPDATED: 2025-01-01 00:00:00 UTC

## Goal
Missing required sections.
""";
    }

    private static WorkflowInput BuildInput(OrchestratorConfig config, WorkItem item)
    {
        var project = new ProjectContext(
            config.RepoOwner,
            config.RepoName,
            config.Workflow.DefaultBaseBranch,
            config.WorkspacePath,
            config.WorkspaceHostPath,
            config.ProjectOwner,
            config.ProjectOwnerType,
            config.ProjectNumber);

        return new WorkflowInput(item, project, Mode: "minimal", Attempt: 0);
    }

    private static string GetRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && current != null; i++)
        {
            var candidate = Path.Combine(current.FullName, "docs", "architecture-playbook.yaml");
            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found for integration tests.");
    }

    private sealed class QueueLlmClient : ILlmClient
    {
        private readonly Queue<string> _responses;
        public int CallCount { get; private set; }

        public QueueLlmClient(IEnumerable<string> responses)
        {
            _responses = new Queue<string>(responses);
        }

        public Task<string> GetUpdatedFileAsync(string model, string systemPrompt, string userPrompt)
        {
            CallCount++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No LLM responses remaining.");
            }

            return Task.FromResult(_responses.Dequeue());
        }

        public Task<string> CompleteChatWithMcpToolsAsync(
            string model,
            string systemPrompt,
            string userPrompt,
            IEnumerable<ModelContextProtocol.Client.McpClientTool> mcpTools,
            McpClientManager mcpManager)
        {
            CallCount++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No LLM responses remaining.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class InMemoryWorkflowContext
    {
        private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);
        public IWorkflowContext Context { get; }
        public List<string> SentMessages { get; } = new();

        public InMemoryWorkflowContext()
        {
            var mock = new Mock<IWorkflowContext>();

            mock.Setup(c => c.ReadOrInitStateAsync<int>(
                    It.IsAny<string>(),
                    It.IsAny<Func<int>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, Func<int>, CancellationToken>((key, initializer, _) =>
                {
                    if (_state.TryGetValue(key, out var value) && value is int stored)
                    {
                        return new ValueTask<int>(stored);
                    }

                    var created = initializer();
                    _state[key] = created;
                    return new ValueTask<int>(created);
                });

            mock.Setup(c => c.ReadOrInitStateAsync<string>(
                    It.IsAny<string>(),
                    It.IsAny<Func<string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, Func<string>, CancellationToken>((key, initializer, _) =>
                {
                    if (_state.TryGetValue(key, out var value) && value is string stored)
                    {
                        return new ValueTask<string>(stored);
                    }

                    var created = initializer();
                    _state[key] = created;
                    return new ValueTask<string>(created);
                });

            mock.Setup(c => c.QueueStateUpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, int, CancellationToken>((key, value, _) =>
                {
                    _state[key] = value;
                    return ValueTask.CompletedTask;
                });

            mock.Setup(c => c.QueueStateUpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, string, CancellationToken>((key, value, _) =>
                {
                    _state[key] = value;
                    return ValueTask.CompletedTask;
                });

            mock.Setup(c => c.SendMessageAsync(
                    It.IsAny<WorkflowInput>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns<WorkflowInput, string, CancellationToken>((_, executorId, _) =>
                {
                    SentMessages.Add(executorId);
                    return ValueTask.CompletedTask;
                });

            Context = mock.Object;
        }
    }

    private sealed class FakeGitHubClient : IGitHubClient
    {
        public bool OpenPullRequestCalled { get; private set; }

        public Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50) =>
            Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());

        public Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body)
        {
            OpenPullRequestCalled = true;
            return Task.FromResult("https://github.com/test/repo/pull/1");
        }

        public Task<int?> GetPullRequestNumberAsync(string branchName) => Task.FromResult<int?>(1);
        public Task ClosePullRequestAsync(int prNumber) => Task.CompletedTask;
        public Task<IReadOnlyList<IssueComment>> GetIssueCommentsAsync(int issueNumber) =>
            Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>());
        public Task CommentOnWorkItemAsync(int issueNumber, string comment) => Task.CompletedTask;

        public Task AddLabelsAsync(int issueNumber, params string[] labels)
        {
            return Task.CompletedTask;
        }

        public Task RemoveLabelAsync(int issueNumber, string label) => Task.CompletedTask;
        public Task RemoveLabelsAsync(int issueNumber, params string[] labels) => Task.CompletedTask;
        public Task<string> GetPullRequestDiffAsync(int prNumber) => Task.FromResult(string.Empty);
        public Task CreateBranchAsync(string branchName) => Task.CompletedTask;
        public Task DeleteBranchAsync(string branchName) => Task.CompletedTask;
        public Task<bool> HasCommitsAsync(string baseBranch, string headBranch) => Task.FromResult(true);
        public Task<RepoFile?> TryGetFileContentAsync(string branch, string path) => Task.FromResult<RepoFile?>(null);
        public Task CreateOrUpdateFileAsync(string branch, string path, string content, string message) =>
            Task.CompletedTask;

        public Task<ProjectSnapshot> GetProjectSnapshotAsync(string owner, int projectNumber, ProjectOwnerType ownerType)
        {
            var snapshot = new ProjectSnapshot(owner, projectNumber, ownerType, "Test", Array.Empty<ProjectItem>());
            return Task.FromResult(snapshot);
        }

        public Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName) =>
            Task.CompletedTask;
    }

    private sealed class FakeRepoGit : IRepoGit
    {
        public void EnsureConfigured()
        {
        }

        public bool IsGitRepo() => true;

        public void CleanWorkingTree()
        {
        }

        public void EnsureBranch(string branchName, string baseBranch)
        {
        }

        public void HardResetToRemote(string branchName)
        {
        }

        public bool CommitAndPush(string branchName, string message, IEnumerable<string> paths) => true;
    }
}
