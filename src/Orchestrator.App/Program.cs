using System;
using System.Threading;
using System.Threading.Tasks;
using dotenv.net;
using Orchestrator.App.Watcher;
using Orchestrator.App.Workflows;

namespace Orchestrator.App;

/// <summary>
/// Minimal entry point for Workstream 1.
/// Loads configuration and starts the watcher + workflow runner.
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
        var services = Infrastructure.ServiceFactory.Create(cfg);

        // Initialize git
        services.RepoGit.EnsureConfigured();
        if (!services.RepoGit.IsGitRepo())
        {
            Logger.WriteLine("Git operations disabled until workspace is a git repo.");
        }
        if (!System.IO.Directory.Exists(cfg.WorkspacePath))
        {
            Logger.WriteLine($"Workspace path not found: {cfg.WorkspacePath}");
        }

        // Initialize MCP client manager
        var mcpManager = services.McpManager;
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
        if (args.Contains("--init-only", StringComparer.OrdinalIgnoreCase))
        {
            cts.Cancel();
        }

        if (!cfg.Workflow.UseWorkflowMode)
        {
            Logger.WriteLine("Legacy Orchestrator mode is no longer supported. Please set USE_WORKFLOW_MODE=true.");
            return 1;
        }

        if (!cfg.Workflow.UseWorkflowMode)
        {
            Logger.WriteLine("Legacy Orchestrator mode is no longer supported. Please set USE_WORKFLOW_MODE=true.");
            return 1;
        }

        // Create and run watcher
        var workflowFactory = new WorkflowFactory();
        var workflowRunner = new WorkflowRunner(workflowFactory);
        var watcher = new GitHubIssueWatcher(
            cfg,
            services.GitHub,
            services.Workspace,
            services.RepoGit,
            services.Llm,
            mcpManager,
            workflowRunner);
        await watcher.RunAsync(cts.Token);

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
        Logger.WriteLine($"Repo: {cfg.RepoOwner}/{cfg.RepoName} base {cfg.Workflow.DefaultBaseBranch}");
        Logger.WriteLine($"OpenAI base url: {cfg.OpenAiBaseUrl}");
        Logger.WriteLine($"Work item label: {cfg.Labels.WorkItemLabel}");
    }
}
