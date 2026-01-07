using System;
using System.Threading;
using System.Threading.Tasks;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Watcher;
using Orchestrator.App.Workflows;
using Xunit;

namespace Orchestrator.App.Tests.Watcher;

public class GitHubIssueWatcherTests
{
    [Fact]
    public async Task RunAsync_CancelsDuringDelay_StopsGracefully()
    {
        using var temp = new TempWorkspace();
        var config = MockWorkContext.CreateConfig(temp.WorkspacePath);
        config = config with { Workflow = config.Workflow with { PollIntervalSeconds = 5 } };

        await using var mcpManager = new McpClientManager();
        var watcher = new GitHubIssueWatcher(
            config,
            new OctokitGitHubClient(config),
            temp.Workspace,
            new RepoGit(config, temp.WorkspacePath),
            new LlmClient(config),
            mcpManager,
            new WorkflowRunner(new WorkflowFactory()));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await watcher.RunAsync(cts.Token);
    }

    [Fact]
    public async Task RunAsync_InvalidInterval_HandlesErrorAndStops()
    {
        using var temp = new TempWorkspace();
        var config = MockWorkContext.CreateConfig(temp.WorkspacePath);
        config = config with { Workflow = config.Workflow with { PollIntervalSeconds = -5 } };

        await using var mcpManager = new McpClientManager();
        var watcher = new GitHubIssueWatcher(
            config,
            new OctokitGitHubClient(config),
            temp.Workspace,
            new RepoGit(config, temp.WorkspacePath),
            new LlmClient(config),
            mcpManager,
            new WorkflowRunner(new WorkflowFactory()));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await watcher.RunAsync(cts.Token);
    }
}
