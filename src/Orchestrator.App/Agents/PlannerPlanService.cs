namespace Orchestrator.App.Agents;

internal static class PlannerPlanService
{
    public static async Task<string> RunAsync(WorkContext ctx)
    {
        var planPath = $"plans/issue-{ctx.WorkItem.Number}.md";
        if (ctx.Workspace.Exists(planPath))
        {
            var existing = ctx.Workspace.ReadAllText(planPath);
            if (AgentTemplateUtil.IsStatusComplete(existing))
            {
                return $"Plan already complete at `{planPath}`. Skipping.";
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
            return $"Planner created branch `{branch}`, opened a draft PR, and wrote `{planPath}`.";
        }

        var existingPr = await ctx.GitHub.GetPullRequestNumberAsync(branch);
        return existingPr is null
            ? $"Plan already present at `{planPath}`. No new commits; skipping PR creation."
            : $"Plan updated at `{planPath}`.";
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
