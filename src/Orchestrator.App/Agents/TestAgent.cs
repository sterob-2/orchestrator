using System.Threading.Tasks;

namespace Orchestrator.App.Agents;

internal sealed class TestAgent : IRoleAgent
{
    public Task<AgentResult> RunAsync(WorkContext ctx)
    {
        var branch = WorkItemBranch.BuildBranchName(ctx.WorkItem);
        var specPath = $"specs/issue-{ctx.WorkItem.Number}.md";
        var updatedSpec = TryMarkAcceptanceCriteriaAsync(ctx, specPath);

        var notes = updatedSpec
            ? "TestAgent updated acceptance criteria in the spec."
            : "TestAgent could not find the spec file to update acceptance criteria.";

        if (updatedSpec)
        {
            ctx.Repo.EnsureBranch(branch, ctx.Config.Workflow.DefaultBaseBranch);
            ctx.Repo.CommitAndPush(branch, $"test: update acceptance criteria for issue {ctx.WorkItem.Number}", new[] { specPath });
        }

        return Task.FromResult(AgentResult.Ok(notes));
    }

    private static bool TryMarkAcceptanceCriteriaAsync(WorkContext ctx, string specPath)
    {
        if (!ctx.Workspace.Exists(specPath))
        {
            return false;
        }

        var content = ctx.Workspace.ReadAllText(specPath);
        var updated = WorkItemParsers.MarkAcceptanceCriteriaDone(content);
        if (updated == content)
        {
            return true;
        }

        ctx.Workspace.WriteAllText(specPath, updated);

        return true;
    }
}
