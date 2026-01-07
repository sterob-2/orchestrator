using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orchestrator.App.Agents;

namespace Orchestrator.App;

/// <summary>
/// Legacy orchestration logic from prototype.
/// This will be replaced by Workstream 3's Watcher + WorkflowRunner.
/// </summary>
internal class LegacyOrchestrator
{
    private readonly OrchestratorConfig _cfg;
    private readonly OctokitGitHubClient _github;
    private readonly RepoWorkspace _workspace;
    private readonly RepoGit _repoGit;
    private readonly LlmClient _llm;
    private readonly McpClientManager _mcpManager;

    public LegacyOrchestrator(
        OrchestratorConfig cfg,
        OctokitGitHubClient github,
        RepoWorkspace workspace,
        RepoGit repoGit,
        LlmClient llm,
        McpClientManager mcpManager)
    {
        _cfg = cfg;
        _github = github;
        _workspace = workspace;
        _repoGit = repoGit;
        _llm = llm;
        _mcpManager = mcpManager;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WorkItem? lastWorkItem = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await GetNextWorkItemAsync(_github, _cfg);
                lastWorkItem = workItem;

                if (workItem is null)
                {
                    Logger.WriteLine("No matching work items");
                }
                else
                {
                    var stage = GetStageName(workItem, _cfg);
                    var labels = FormatLabels(workItem);
                    Logger.WriteLine($"Picked work item #{workItem.Number}: {workItem.Title} (stage: {stage}, labels: {labels})");

                    await HandleWorkItemAsync(_github, _cfg, workItem, _workspace, _repoGit, _llm, _mcpManager);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.ToString());
            }

            try
            {
                var interval = ComputePollIntervalSeconds(_cfg, lastWorkItem);
                await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        Logger.WriteLine("Orchestrator stopped");

        // Dispose MCP client manager
        await _mcpManager.DisposeAsync();
    }

    private static async Task<WorkItem?> GetNextWorkItemAsync(OctokitGitHubClient github, OrchestratorConfig cfg)
    {
        var items = await github.GetOpenWorkItemsAsync();
        foreach (var item in items)
        {
            if (HasLabel(item, cfg.DoneLabel) || HasLabel(item, cfg.BlockedLabel))
                continue;

            if (HasAnyLabel(item, cfg.WorkItemLabel, cfg.PlannerLabel, cfg.TechLeadLabel, cfg.DevLabel, cfg.TestLabel, cfg.ReleaseLabel))
            {
                return item;
            }
        }

        return null;
    }

    private static int ComputePollIntervalSeconds(OrchestratorConfig cfg, WorkItem? workItem)
    {
        if (workItem == null)
        {
            return cfg.PollIntervalSeconds;
        }

        if (HasAnyLabel(workItem, cfg.PlannerLabel, cfg.TechLeadLabel, cfg.DevLabel, cfg.TestLabel, cfg.ReleaseLabel))
        {
            return cfg.FastPollIntervalSeconds;
        }

        return cfg.PollIntervalSeconds;
    }

    private static async Task HandleWorkItemAsync(
        OctokitGitHubClient github,
        OrchestratorConfig cfg,
        WorkItem item,
        RepoWorkspace workspace,
        RepoGit repoGit,
        LlmClient llm,
        McpClientManager mcpManager)
    {
        await ClearStaleSpecQuestionsAsync(github, cfg, item, workspace);
        if (HasLabel(item, cfg.ResetLabel))
        {
            await ResetWorkItemAsync(github, cfg, item, repoGit);
            return;
        }

        if (HasLabel(item, cfg.ReviewNeededLabel) && !HasLabel(item, cfg.ReviewedLabel))
        {
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInReview);
            Logger.WriteLine($"Work item #{item.Number} awaiting review.");
            await LogLabelsAsync(github, item.Number, "awaiting review", "system");
            return;
        }

        if (HasLabel(item, cfg.WorkItemLabel))
        {
            await github.AddLabelsAsync(item.Number, cfg.PlannerLabel);
            await github.RemoveLabelAsync(item.Number, cfg.WorkItemLabel);
            await github.AddLabelsAsync(item.Number, cfg.InProgressLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInProgress);
            await github.CommentOnWorkItemAsync(item.Number, "Assigned to PlannerAgent.");
            await LogLabelsAsync(github, item.Number, "moved to planner", "planner");
            return;
        }

        if (HasAnyLabel(item, cfg.PlannerLabel, cfg.TechLeadLabel, cfg.DevLabel, cfg.TestLabel, cfg.ReleaseLabel))
        {
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInProgress);
        }

        if (HasLabel(item, cfg.PlannerLabel))
        {
            // Check if we should use workflow mode (Agent Framework)
            if (cfg.UseWorkflowMode)
            {
                Logger.WriteLine($"[Workflow] PlannerExecutor handling work item #{item.Number}.");
                await HandlePlannerWorkflowAsync(github, cfg, item, workspace, repoGit, llm, mcpManager);
                return;
            }

            // Legacy mode: use old PlannerAgent
            Logger.WriteLine($"PlannerAgent handling work item #{item.Number}.");
            await HandleStageAsync(github, cfg, item, cfg.PlannerLabel, cfg.TechLeadLabel, new PlannerAgent(), workspace, repoGit, llm, mcpManager);
            return;
        }

        if (HasLabel(item, cfg.TechLeadLabel))
        {
            Logger.WriteLine($"TechLeadAgent handling work item #{item.Number}.");
            Logger.WriteLine($"Work item #{item.Number} state: stage={GetStageName(item, cfg)} labels={FormatLabels(item)}");
            await HandleStageAsync(github, cfg, item, cfg.TechLeadLabel, cfg.DevLabel, new TechLeadAgent(), workspace, repoGit, llm, mcpManager);
            return;
        }

        if (HasLabel(item, cfg.DevLabel))
        {
            Logger.WriteLine($"DevAgent handling work item #{item.Number}.");
            Logger.WriteLine($"Work item #{item.Number} state: stage={GetStageName(item, cfg)} labels={FormatLabels(item)}");
            await HandleDevStageAsync(github, cfg, item, new DevAgent(), workspace, repoGit, llm, mcpManager);
            return;
        }

        if (HasLabel(item, cfg.TestLabel))
        {
            Logger.WriteLine($"TestAgent handling work item #{item.Number}.");
            Logger.WriteLine($"Work item #{item.Number} state: stage={GetStageName(item, cfg)} labels={FormatLabels(item)}");
            await HandleStageAsync(github, cfg, item, cfg.TestLabel, cfg.ReleaseLabel, new TestAgent(), workspace, repoGit, llm, mcpManager);
            return;
        }

        if (HasLabel(item, cfg.ReleaseLabel))
        {
            Logger.WriteLine($"ReleaseAgent handling work item #{item.Number}.");
            Logger.WriteLine($"Work item #{item.Number} state: stage={GetStageName(item, cfg)} labels={FormatLabels(item)}");
            await HandleReleaseStageAsync(github, cfg, item, new ReleaseAgent(), workspace, repoGit, llm, mcpManager);
        }
    }

    private static async Task HandlePlannerWorkflowAsync(
        OctokitGitHubClient github,
        OrchestratorConfig cfg,
        WorkItem item,
        RepoWorkspace workspace,
        RepoGit repoGit,
        LlmClient llm,
        McpClientManager mcpManager)
    {
        try
        {
            // Create work context
            var ctx = new WorkContext(
                WorkItem: item,
                Config: cfg,
                GitHub: github,
                Workspace: workspace,
                Repo: repoGit,
                Llm: llm,
                Mcp: mcpManager
            );

            // Build workflow
            var workflow = SDLCWorkflow.BuildPlannerOnlyWorkflow(ctx);

            // Create workflow input from work item
            var input = new WorkflowInput(
                IssueNumber: item.Number,
                Title: item.Title,
                Body: item.Body ?? string.Empty,
                Labels: item.Labels.ToList()
            );

            // Execute workflow
            var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

            if (output is not null && output.Success)
            {
                // Post comment with result
                await github.CommentOnWorkItemAsync(item.Number, output.Notes);

                // Update labels: remove planner, add techlead
                await github.RemoveLabelAsync(item.Number, cfg.PlannerLabel);
                await github.AddLabelsAsync(item.Number, cfg.TechLeadLabel);

                Logger.WriteLine($"[Workflow] Completed successfully for issue #{item.Number}");
            }
            else
            {
                var errorMsg = output?.Notes ?? "Workflow failed with no output";
                Logger.WriteLine($"[Workflow] Failed for issue #{item.Number}: {errorMsg}");
                await github.CommentOnWorkItemAsync(item.Number, $"Workflow failed: {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"[Workflow] Exception for issue #{item.Number}: {ex.Message}");
            Logger.WriteLine($"   Stack trace: {ex.StackTrace}");
            await github.CommentOnWorkItemAsync(item.Number, $"Workflow error: {ex.Message}");
        }
    }

    private static async Task HandleStageAsync(
        OctokitGitHubClient github,
        OrchestratorConfig cfg,
        WorkItem item,
        string stageLabel,
        string nextStageLabel,
        IRoleAgent agent,
        RepoWorkspace workspace,
        RepoGit repoGit,
        LlmClient llm,
        McpClientManager mcpManager)
    {
        var requiresReview = HasLabel(item, cfg.UserReviewRequiredLabel) ||
            string.Equals(stageLabel, cfg.TestLabel, StringComparison.OrdinalIgnoreCase);
        if (requiresReview && HasLabel(item, cfg.ReviewNeededLabel) && !HasLabel(item, cfg.ReviewedLabel))
        {
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInReview);
            Logger.WriteLine($"Work item #{item.Number} awaiting review.");
            await LogLabelsAsync(github, item.Number, "awaiting review", "system");
            return;
        }

        if (requiresReview && HasLabel(item, cfg.ReviewedLabel))
        {
            await github.RemoveLabelsAsync(item.Number, stageLabel, cfg.ReviewedLabel, cfg.ReviewNeededLabel);
            await github.AddLabelsAsync(item.Number, nextStageLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInProgress);
            await github.CommentOnWorkItemAsync(item.Number, $"Handoff: {stageLabel} -> {nextStageLabel}");
            await LogLabelsAsync(github, item.Number, $"handoff {stageLabel} -> {nextStageLabel}", "system");
            return;
        }

        var ctx = new WorkContext(item, github, cfg, workspace, repoGit, llm, mcpManager);
        var result = await agent.RunAsync(ctx);

        if (!result.Success)
        {
            await github.CommentOnWorkItemAsync(item.Number, result.Notes);
            await github.AddLabelsAsync(item.Number, cfg.BlockedLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInProgress);
            await LogLabelsAsync(github, item.Number, "blocked", "system");
            return;
        }

        await github.CommentOnWorkItemAsync(item.Number, result.Notes);
        if (requiresReview)
        {
            await github.AddLabelsAsync(item.Number, cfg.ReviewNeededLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInReview);
            await LogLabelsAsync(github, item.Number, "review needed", "system");
            return;
        }

        if (result.AddLabels is { Count: > 0 })
        {
            await github.AddLabelsAsync(item.Number, result.AddLabels.ToArray());
            await LogLabelsAsync(github, item.Number, "labels added", "system");
        }

        if (result.RemoveLabels is { Count: > 0 })
        {
            await github.RemoveLabelsAsync(item.Number, result.RemoveLabels.ToArray());
            await LogLabelsAsync(github, item.Number, "labels removed", "system");
        }

        if (!string.IsNullOrWhiteSpace(result.NextStageLabel))
        {
            await github.RemoveLabelAsync(item.Number, stageLabel);
            await github.AddLabelsAsync(item.Number, result.NextStageLabel);
            await github.CommentOnWorkItemAsync(item.Number, $"Handoff: {stageLabel} -> {result.NextStageLabel}");
            await LogLabelsAsync(github, item.Number, $"handoff {stageLabel} -> {result.NextStageLabel}", "system");
            return;
        }

        await github.RemoveLabelAsync(item.Number, stageLabel);
        await github.AddLabelsAsync(item.Number, nextStageLabel);
        await github.CommentOnWorkItemAsync(item.Number, $"Handoff: {stageLabel} -> {nextStageLabel}");
        await LogLabelsAsync(github, item.Number, $"handoff {stageLabel} -> {nextStageLabel}", "system");
    }

    private static async Task HandleDevStageAsync(
        OctokitGitHubClient github,
        OrchestratorConfig cfg,
        WorkItem item,
        IRoleAgent agent,
        RepoWorkspace workspace,
        RepoGit repoGit,
        LlmClient llm,
        McpClientManager mcpManager)
    {
        if (HasLabel(item, cfg.CodeReviewNeededLabel) && !HasLabel(item, cfg.CodeReviewApprovedLabel))
        {
            var branch = WorkItemBranch.BuildBranchName(item);
            try
            {
                var hasCommits = await github.HasCommitsAsync(cfg.DefaultBaseBranch, branch);
                if (!hasCommits)
                {
                    Logger.WriteLine($"Work item #{item.Number} has no commits for review.");
                    await github.RemoveLabelAsync(item.Number, cfg.CodeReviewNeededLabel);
                    await LogLabelsAsync(github, item.Number, "removed code review needed", "techlead");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Work item #{item.Number} review commit check failed: {ex.Message}");
            }

            var reviewCtx = new WorkContext(item, github, cfg, workspace, repoGit, llm, mcpManager);
            var reviewResult = await new TechLeadReviewAgent().RunAsync(reviewCtx);
            if (!reviewResult.Success)
            {
                await github.CommentOnWorkItemAsync(item.Number, reviewResult.Notes);
                Logger.WriteLine($"Work item #{item.Number} awaiting code review.");
                await LogLabelsAsync(github, item.Number, "awaiting code review", "techlead");
                return;
            }

            await github.CommentOnWorkItemAsync(item.Number, reviewResult.Notes);
            if (reviewResult.AddLabels is { Count: > 0 })
            {
                await github.AddLabelsAsync(item.Number, reviewResult.AddLabels.ToArray());
                await LogLabelsAsync(github, item.Number, "labels added", "techlead");
            }

            if (reviewResult.RemoveLabels is { Count: > 0 })
            {
                await github.RemoveLabelsAsync(item.Number, reviewResult.RemoveLabels.ToArray());
                await LogLabelsAsync(github, item.Number, "labels removed", "techlead");
            }
        }

        if (HasLabel(item, cfg.CodeReviewChangesRequestedLabel))
        {
            Logger.WriteLine($"Work item #{item.Number} applying requested changes.");
            await github.RemoveLabelAsync(item.Number, cfg.CodeReviewChangesRequestedLabel);
            await LogLabelsAsync(github, item.Number, "applying requested changes", "dev");
        }

        if (HasLabel(item, cfg.CodeReviewApprovedLabel))
        {
            await github.RemoveLabelsAsync(item.Number, cfg.DevLabel, cfg.CodeReviewApprovedLabel, cfg.CodeReviewNeededLabel);
            await github.AddLabelsAsync(item.Number, cfg.TestLabel);
            await github.CommentOnWorkItemAsync(item.Number, $"Handoff: {cfg.DevLabel} -> {cfg.TestLabel}");
            await LogLabelsAsync(github, item.Number, $"handoff {cfg.DevLabel} -> {cfg.TestLabel}", "dev");
            return;
        }

        var ctx = new WorkContext(item, github, cfg, workspace, repoGit, llm, mcpManager);
        var result = await agent.RunAsync(ctx);

        if (!result.Success)
        {
            await github.CommentOnWorkItemAsync(item.Number, result.Notes);
            await github.AddLabelsAsync(item.Number, cfg.BlockedLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInProgress);
            await LogLabelsAsync(github, item.Number, "blocked", "dev");
            return;
        }

        await github.CommentOnWorkItemAsync(item.Number, result.Notes);

        if (result.AddLabels is { Count: > 0 })
        {
            await github.AddLabelsAsync(item.Number, result.AddLabels.ToArray());
            await LogLabelsAsync(github, item.Number, "labels added", "dev");
        }

        if (result.RemoveLabels is { Count: > 0 })
        {
            await github.RemoveLabelsAsync(item.Number, result.RemoveLabels.ToArray());
            await LogLabelsAsync(github, item.Number, "labels removed", "dev");
        }

        if (!string.IsNullOrWhiteSpace(result.NextStageLabel))
        {
            await github.RemoveLabelAsync(item.Number, cfg.DevLabel);
            await github.AddLabelsAsync(item.Number, result.NextStageLabel);
            await github.CommentOnWorkItemAsync(item.Number, $"Handoff: {cfg.DevLabel} -> {result.NextStageLabel}");
            await LogLabelsAsync(github, item.Number, $"handoff {cfg.DevLabel} -> {result.NextStageLabel}", "dev");
        }
    }

    private static async Task HandleReleaseStageAsync(
        OctokitGitHubClient github,
        OrchestratorConfig cfg,
        WorkItem item,
        IRoleAgent agent,
        RepoWorkspace workspace,
        RepoGit repoGit,
        LlmClient llm,
        McpClientManager mcpManager)
    {
        var requiresReview = HasLabel(item, cfg.UserReviewRequiredLabel);
        if (requiresReview && HasLabel(item, cfg.ReviewNeededLabel) && !HasLabel(item, cfg.ReviewedLabel))
        {
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInReview);
            Logger.WriteLine($"Work item #{item.Number} awaiting owner review.");
            await LogLabelsAsync(github, item.Number, "awaiting owner review", "release");
            return;
        }

        if (requiresReview && HasLabel(item, cfg.ReviewedLabel))
        {
            await github.RemoveLabelsAsync(item.Number, cfg.ReleaseLabel, cfg.ReviewedLabel, cfg.ReviewNeededLabel);
            await github.AddLabelsAsync(item.Number, cfg.DoneLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusDone);
            await github.CommentOnWorkItemAsync(item.Number, "Release review complete. Marking done.");
            await LogLabelsAsync(github, item.Number, "release approved", "release");
            return;
        }

        var ctx = new WorkContext(item, github, cfg, workspace, repoGit, llm, mcpManager);
        var result = await agent.RunAsync(ctx);

        if (!result.Success)
        {
            await github.CommentOnWorkItemAsync(item.Number, result.Notes);
            await github.AddLabelsAsync(item.Number, cfg.BlockedLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInProgress);
            await LogLabelsAsync(github, item.Number, "blocked", "release");
            return;
        }

        await github.CommentOnWorkItemAsync(item.Number, result.Notes);
        if (requiresReview)
        {
            await github.AddLabelsAsync(item.Number, cfg.ReviewNeededLabel);
            await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusInReview);
            await LogLabelsAsync(github, item.Number, "review needed", "release");
            return;
        }

        await github.RemoveLabelAsync(item.Number, cfg.ReleaseLabel);
        await github.AddLabelsAsync(item.Number, cfg.DoneLabel);
        await TryUpdateProjectStatusAsync(github, cfg, item, cfg.ProjectStatusDone);
        await github.CommentOnWorkItemAsync(item.Number, "Release complete. Marking done.");
        await LogLabelsAsync(github, item.Number, "release done", "release");
    }

    private static string GetStageName(WorkItem item, OrchestratorConfig cfg)
    {
        if (HasLabel(item, cfg.ResetLabel))
        {
            return "reset";
        }

        if (HasLabel(item, cfg.PlannerLabel))
        {
            return "planner";
        }

        if (HasLabel(item, cfg.TechLeadLabel))
        {
            return "techlead";
        }

        if (HasLabel(item, cfg.DevLabel))
        {
            return "dev";
        }

        if (HasLabel(item, cfg.TestLabel))
        {
            return "test";
        }

        if (HasLabel(item, cfg.ReleaseLabel))
        {
            return "release";
        }

        if (HasLabel(item, cfg.ReviewNeededLabel))
        {
            return "review";
        }

        if (HasLabel(item, cfg.WorkItemLabel))
        {
            return "ready";
        }

        return "unknown";
    }

    private static string FormatLabels(WorkItem item)
    {
        if (item.Labels.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", item.Labels.OrderBy(label => label, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasLabel(WorkItem item, string label) =>
        item.Labels.Any(l => string.Equals(l, label, StringComparison.OrdinalIgnoreCase));

    private static async Task ResetWorkItemAsync(OctokitGitHubClient github, OrchestratorConfig cfg, WorkItem item, RepoGit repoGit)
    {
        var branch = WorkItemBranch.BuildBranchName(item);
        var prNumber = await github.GetPullRequestNumberAsync(branch);
        if (prNumber is not null)
        {
            try
            {
                await github.ClosePullRequestAsync(prNumber.Value);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Reset: failed to close PR #{prNumber.Value}: {ex.Message}");
            }
        }

        try
        {
            await github.DeleteBranchAsync(branch);
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"Reset: failed to delete branch {branch}: {ex.Message}");
        }

        var remove = new[]
        {
            cfg.PlannerLabel,
            cfg.TechLeadLabel,
            cfg.DevLabel,
            cfg.TestLabel,
            cfg.ReleaseLabel,
            cfg.InProgressLabel,
            cfg.ReviewNeededLabel,
            cfg.ReviewedLabel,
            cfg.SpecQuestionsLabel,
            cfg.SpecClarifiedLabel,
            cfg.CodeReviewNeededLabel,
            cfg.CodeReviewApprovedLabel,
            cfg.CodeReviewChangesRequestedLabel,
            cfg.BlockedLabel,
            cfg.ResetLabel
        };

        await github.RemoveLabelsAsync(item.Number, remove);
        await github.AddLabelsAsync(item.Number, cfg.WorkItemLabel);
        await github.CommentOnWorkItemAsync(
            item.Number,
            $"Reset requested. Closed PR, deleted agent branch, and returned item to {cfg.WorkItemLabel}.");
        await LogLabelsAsync(github, item.Number, "reset", "system");
        repoGit.HardResetToRemote(cfg.DefaultBaseBranch);
    }

    private static async Task ClearStaleSpecQuestionsAsync(
        OctokitGitHubClient github,
        OrchestratorConfig cfg,
        WorkItem item,
        RepoWorkspace workspace)
    {
        if (!HasLabel(item, cfg.SpecQuestionsLabel))
        {
            return;
        }

        var questionsPath = $"orchestrator/questions/issue-{item.Number}.md";
        if (!workspace.Exists(questionsPath))
        {
            return;
        }

        var content = workspace.ReadAllText(questionsPath);
        if (!AgentTemplateUtil.IsStatus(content, "CLARIFIED"))
        {
            return;
        }

        await github.RemoveLabelAsync(item.Number, cfg.SpecQuestionsLabel);
        await LogLabelsAsync(github, item.Number, "spec questions cleared", "system");
    }

    private static bool HasAnyLabel(WorkItem item, params string[] labels)
    {
        foreach (var label in labels)
        {
            if (HasLabel(item, label)) return true;
        }

        return false;
    }

    private static async Task TryUpdateProjectStatusAsync(OctokitGitHubClient github, OrchestratorConfig cfg, WorkItem item, string status)
    {
        var projectRef = ResolveProjectReference(cfg, item);
        if (projectRef is null)
        {
            Logger.WriteLine($"Project status update skipped: no project reference for issue #{item.Number}.");
            return;
        }

        try
        {
            await github.UpdateProjectItemStatusAsync(projectRef.Owner, projectRef.Number, item.Number, status);
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"Project status update failed for issue #{item.Number} (status '{status}'): {ex}");
        }
    }

    private static ProjectReference? ResolveProjectReference(OrchestratorConfig cfg, WorkItem item)
    {
        if (!string.IsNullOrWhiteSpace(cfg.ProjectOwner) && cfg.ProjectNumber.HasValue)
        {
            var ownerType = ProjectOwnerType.User;
            if (string.Equals(cfg.ProjectOwnerType, "org", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cfg.ProjectOwnerType, "organization", StringComparison.OrdinalIgnoreCase))
            {
                ownerType = ProjectOwnerType.Organization;
            }
            else if (!string.Equals(cfg.ProjectOwnerType, "user", StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteLine($"Unknown PROJECT_OWNER_TYPE '{cfg.ProjectOwnerType}', defaulting to user.");
            }

            return new ProjectReference(cfg.ProjectOwner, cfg.ProjectNumber.Value, ownerType);
        }

        return WorkItemParsers.TryParseProjectReference(item.Body);
    }

    private static async Task LogLabelsAsync(OctokitGitHubClient github, int issueNumber, string context, string agentName)
    {
        try
        {
            var labels = await github.GetIssueLabelsAsync(issueNumber);
            Logger.WriteLine($"[{agentName}] Work item #{issueNumber} labels after {context}: {string.Join(", ", labels)}");
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"[{agentName}] Failed to read labels for issue #{issueNumber}: {ex.Message}");
        }
    }
}
