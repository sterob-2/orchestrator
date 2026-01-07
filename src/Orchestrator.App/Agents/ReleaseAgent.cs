using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.App.Agents;

internal sealed class ReleaseAgent : IRoleAgent
{
    public Task<AgentResult> RunAsync(WorkContext ctx)
    {
        var branch = WorkItemBranch.BuildBranchName(ctx.WorkItem);
        var releasePath = $"orchestrator/release/issue-{ctx.WorkItem.Number}.md";
        var releaseContent = BuildReleaseNotes(ctx);

        ctx.Repo.EnsureBranch(branch, ctx.Config.Workflow.DefaultBaseBranch);
        ctx.Workspace.WriteAllText(releasePath, releaseContent);
        ctx.Repo.CommitAndPush(branch, $"docs: add release notes for issue {ctx.WorkItem.Number}", new[] { releasePath });

        return Task.FromResult(AgentResult.Ok($"Release notes written to `{releasePath}`."));
    }

    private static string BuildReleaseNotes(WorkContext ctx)
    {
        var lines = new List<string>
        {
            $"# Release Notes: Issue {ctx.WorkItem.Number} - {ctx.WorkItem.Title}",
            "",
            "## Summary",
            "- Describe user-facing changes.",
            "",
            "## Android Emulator Check",
            "- [ ] Verified on Android emulator (device + OS version)",
            "",
            "## Notes",
            "-"
        };

        return string.Join('\n', lines);
    }
}
