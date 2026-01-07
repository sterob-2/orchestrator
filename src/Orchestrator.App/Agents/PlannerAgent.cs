using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.App.Agents;

internal sealed class PlannerAgent : IRoleAgent
{
    public Task<AgentResult> RunAsync(WorkContext ctx)
    {
        return RunPlannerAsync(ctx);
    }

    private static async Task<AgentResult> RunPlannerAsync(WorkContext ctx)
    {
        var planPath = $"plans/issue-{ctx.WorkItem.Number}.md";
        if (ctx.Workspace.Exists(planPath))
        {
            var existing = ctx.Workspace.ReadAllText(planPath);
            if (AgentTemplateUtil.IsStatusComplete(existing))
            {
                return AgentResult.Ok($"Plan already complete at `{planPath}`. Skipping.");
            }
        }

        var branch = WorkItemBranch.BuildBranchName(ctx.WorkItem);
        ctx.Repo.EnsureBranch(branch, ctx.Config.Workflow.DefaultBaseBranch);

        var templatePath = "docs/templates/plan.md";
        var tokens = AgentTemplateUtil.BuildTokens(ctx);
        var planContent = ctx.Workspace.ReadOrTemplate(planPath, templatePath, tokens);
        planContent = AgentTemplateUtil.UpdateStatus(planContent, "COMPLETE");
        planContent = AppendAcceptanceCriteria(planContent, ctx);
        ctx.Workspace.WriteAllText(planPath, planContent);
        var committed = ctx.Repo.CommitAndPush(branch, $"docs: add plan for issue {ctx.WorkItem.Number}", new[] { planPath });

        if (committed)
        {
            var prTitle = $"Agent Plan: {ctx.WorkItem.Title}";
            var prBody = $"Work item #{ctx.WorkItem.Number}\n\nPlan: {planPath}";
            await ctx.GitHub.OpenPullRequestAsync(branch, ctx.Config.Workflow.DefaultBaseBranch, prTitle, prBody);
        }
        else
        {
            var existingPr = await ctx.GitHub.GetPullRequestNumberAsync(branch);
            if (existingPr is null)
            {
                var skipNotes = $"Plan already present at `{planPath}`. No new commits; skipping PR creation.";
                return AgentResult.Ok(skipNotes);
            }
        }

        var summaryNotes = $"Planner created branch `{branch}`, opened a draft PR, and wrote `{planPath}`.";
        return AgentResult.Ok(summaryNotes);
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
