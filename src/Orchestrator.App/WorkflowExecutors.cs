using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Agents;

namespace Orchestrator.App;

/// <summary>
/// Input message for workflow executors - represents a GitHub work item
/// </summary>
internal sealed record WorkflowInput(
    int IssueNumber,
    string Title,
    string Body,
    List<string> Labels
);

/// <summary>
/// Output message from executors
/// </summary>
internal sealed record WorkflowOutput(
    bool Success,
    string Notes,
    string? NextStage = null
);

/// <summary>
/// Planner executor - creates initial plan from GitHub issue
/// </summary>
internal sealed class PlannerExecutor : Executor<WorkflowInput, WorkflowOutput>
{
    private readonly WorkContext _context;

    public PlannerExecutor(WorkContext context) : base("Planner")
    {
        _context = context;
    }

    public override async ValueTask<WorkflowOutput> HandleAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var planPath = $"plans/issue-{input.IssueNumber}.md";

        // Check if plan already exists and is complete (idempotent)
        if (_context.Workspace.Exists(planPath))
        {
            var existing = _context.Workspace.ReadAllText(planPath);
            if (AgentTemplateUtil.IsStatusComplete(existing))
            {
                return new WorkflowOutput(
                    Success: true,
                    Notes: $"Plan already complete at `{planPath}`. Skipping.",
                    NextStage: "TechLead"
                );
            }
        }

        // Create branch
        var branch = WorkItemBranch.BuildBranchName(_context.WorkItem);
        _context.Repo.EnsureBranch(branch, _context.Config.DefaultBaseBranch);

        // Generate plan content from template
        var templatePath = "docs/templates/plan.md";
        var tokens = AgentTemplateUtil.BuildTokens(_context);
        var planContent = _context.Workspace.ReadOrTemplate(planPath, templatePath, tokens);
        planContent = AgentTemplateUtil.UpdateStatus(planContent, "COMPLETE");
        planContent = AppendAcceptanceCriteria(planContent, _context);

        // Write plan file
        _context.Workspace.WriteAllText(planPath, planContent);

        // Commit and push
        var committed = _context.Repo.CommitAndPush(
            branch,
            $"docs: add plan for issue {input.IssueNumber}",
            new[] { planPath }
        );

        string notes;
        if (committed)
        {
            // Create draft PR
            var prTitle = $"Agent Plan: {input.Title}";
            var prBody = $"Work item #{input.IssueNumber}\n\nPlan: {planPath}";
            await _context.GitHub.OpenPullRequestAsync(
                branch,
                _context.Config.DefaultBaseBranch,
                prTitle,
                prBody
            );
            notes = $"Planner created branch `{branch}`, opened a draft PR, and wrote `{planPath}`.";
        }
        else
        {
            var existingPr = await _context.GitHub.GetPullRequestNumberAsync(branch);
            if (existingPr is null)
            {
                notes = $"Plan already present at `{planPath}`. No new commits; skipping PR creation.";
            }
            else
            {
                notes = $"Plan updated at `{planPath}`.";
            }
        }

        // Return result
        return new WorkflowOutput(
            Success: true,
            Notes: notes,
            NextStage: "TechLead"
        );
    }

    private static string AppendAcceptanceCriteria(string content, WorkContext ctx)
    {
        var criteria = WorkItemParsers.TryParseAcceptanceCriteria(ctx.WorkItem.Body);
        if (criteria.Count == 0)
        {
            return content;
        }

        var lines = new List<string>();
        foreach (var item in criteria)
        {
            lines.Add($"- [ ] {item}");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Join('\n', lines);
        }

        return content + "\n" + string.Join('\n', lines);
    }
}
