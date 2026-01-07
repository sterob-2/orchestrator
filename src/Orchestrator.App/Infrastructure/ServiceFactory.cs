namespace Orchestrator.App.Infrastructure;

internal sealed record AppServices(
    IGitHubClient GitHub,
    IRepoWorkspace Workspace,
    IRepoGit RepoGit,
    ILlmClient Llm,
    McpClientManager McpManager);

internal static class ServiceFactory
{
    public static AppServices Create(OrchestratorConfig cfg)
    {
        return new AppServices(
            GitHub: new GitHub.OctokitGitHubClient(cfg),
            Workspace: new Filesystem.RepoWorkspace(cfg.WorkspacePath),
            RepoGit: new Git.RepoGit(cfg, cfg.WorkspacePath),
            Llm: new Llm.LlmClient(cfg),
            McpManager: new Mcp.McpClientManager()
        );
    }
}
