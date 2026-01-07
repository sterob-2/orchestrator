using System;
using System.Threading;
using System.Threading.Tasks;
using dotenv.net;

namespace Orchestrator.App;

/// <summary>
/// Minimal entry point for Workstream 1.
/// Loads configuration and starts the orchestrator.
/// Will be replaced by Workstream 3's Watcher + WorkflowRunner.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Load .env file from default locations
        LoadEnvironmentFiles();

        // Load configuration from environment
        var cfg = OrchestratorConfig.FromEnvironment();

        // Validate required configuration
        if (!ValidateConfig(cfg))
        {
            return 2;
        }

        // Log startup information
        LogStartupInfo(cfg);

        // Create infrastructure services
        var github = new OctokitGitHubClient(cfg);
        var workspace = new RepoWorkspace(cfg.WorkspacePath);
        var repoGit = new RepoGit(cfg, cfg.WorkspacePath);
        var llm = new LlmClient(cfg);

        // Initialize git
        repoGit.EnsureConfigured();
        if (!repoGit.IsGitRepo())
        {
            Logger.WriteLine("Git operations disabled until workspace is a git repo.");
        }
        if (!System.IO.Directory.Exists(cfg.WorkspacePath))
        {
            Logger.WriteLine($"Workspace path not found: {cfg.WorkspacePath}");
        }

        // Initialize MCP client manager
        var mcpManager = new McpClientManager();
        try
        {
            await mcpManager.InitializeAsync(cfg);
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"Warning: MCP initialization failed: {ex.Message}");
            Logger.WriteLine("Continuing without MCP tools.");
        }

        // Setup cancellation
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        // Create and run orchestrator
        var orchestrator = new LegacyOrchestrator(cfg, github, workspace, repoGit, llm, mcpManager);
        await orchestrator.RunAsync(cts.Token);

        return 0;
    }

    private static void LoadEnvironmentFiles()
    {
        try
        {
            DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { ".env" }));
        }
        catch { /* Ignore if .env not found */ }

        try
        {
            DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { "orchestrator/.env" }));
        }
        catch { /* Ignore if not found */ }
    }

    private static bool ValidateConfig(OrchestratorConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.RepoOwner) || string.IsNullOrWhiteSpace(cfg.RepoName))
        {
            Logger.WriteLine("Missing REPO_OWNER or REPO_NAME. Fill orchestrator/.env");
            return false;
        }

        return true;
    }

    private static void LogStartupInfo(OrchestratorConfig cfg)
    {
        Logger.WriteLine("Orchestrator starting");
        Logger.WriteLine($"Repo: {cfg.RepoOwner}/{cfg.RepoName} base {cfg.DefaultBaseBranch}");
        Logger.WriteLine($"OpenAI base url: {cfg.OpenAiBaseUrl}");
        Logger.WriteLine($"Work item label: {cfg.WorkItemLabel}");
    }
}
