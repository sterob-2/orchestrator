using Orchestrator.App.Workflows;

namespace Orchestrator.App.Watcher;

internal sealed class GitHubIssueWatcher
{
    private readonly OrchestratorConfig _cfg;
    private readonly IGitHubClient _github;
    private readonly IRepoWorkspace _workspace;
    private readonly IRepoGit _repoGit;
    private readonly ILlmClient _llm;
    private readonly McpClientManager _mcpManager;
    private readonly WorkflowRunner _workflowRunner;

    public GitHubIssueWatcher(
        OrchestratorConfig cfg,
        IGitHubClient github,
        IRepoWorkspace workspace,
        IRepoGit repoGit,
        ILlmClient llm,
        McpClientManager mcpManager,
        WorkflowRunner workflowRunner)
    {
        _cfg = cfg;
        _github = github;
        _workspace = workspace;
        _repoGit = repoGit;
        _llm = llm;
        _mcpManager = mcpManager;
        _workflowRunner = workflowRunner;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Logger.WriteLine("GitHubIssueWatcher starting polling...");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Implement actual issue processing logic here
                // For now, we just wait to prevent a tight loop
                await Task.Delay(TimeSpan.FromSeconds(_cfg.Workflow.PollIntervalSeconds), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Error in GitHubIssueWatcher: {ex.Message}");
                // Wait a bit before retrying to avoid log flooding
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
        Logger.WriteLine("GitHubIssueWatcher stopped.");
    }
}
