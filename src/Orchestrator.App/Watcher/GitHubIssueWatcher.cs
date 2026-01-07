using Orchestrator.App.Workflows;

namespace Orchestrator.App.Watcher;

internal sealed class GitHubIssueWatcher
{
    private readonly OrchestratorConfig _cfg;
    private readonly OctokitGitHubClient _github;
    private readonly RepoWorkspace _workspace;
    private readonly RepoGit _repoGit;
    private readonly LlmClient _llm;
    private readonly McpClientManager _mcpManager;
    private readonly WorkflowRunner _workflowRunner;

    public GitHubIssueWatcher(
        OrchestratorConfig cfg,
        OctokitGitHubClient github,
        RepoWorkspace workspace,
        RepoGit repoGit,
        LlmClient llm,
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
        Logger.WriteLine("GitHubIssueWatcher stub: polling not implemented yet.");
        await Task.CompletedTask;
    }
}
