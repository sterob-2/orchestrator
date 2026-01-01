using System;
using System.Threading.Tasks;

namespace Orchestrator.App.Agents;

internal sealed class TechLeadReviewAgent : IRoleAgent
{
    public async Task<AgentResult> RunAsync(WorkContext ctx)
    {
        var branch = WorkItemBranch.BuildBranchName(ctx.WorkItem);
        var prNumber = await ctx.GitHub.GetPullRequestNumberAsync(branch);
        if (prNumber is null)
        {
            return AgentResult.Fail($"No open PR found for branch `{branch}`.");
        }

        var diff = await ctx.GitHub.GetPullRequestDiffAsync(prNumber.Value);
        var architecture = ctx.Workspace.Exists("Assets/Docs/architecture.md")
            ? ctx.Workspace.ReadAllText("Assets/Docs/architecture.md")
            : "";
        var specPath = $"orchestrator/specs/issue-{ctx.WorkItem.Number}.md";
        var spec = ctx.Workspace.Exists(specPath) ? ctx.Workspace.ReadAllText(specPath) : "";

        var systemPrompt = "You are a tech lead reviewer. Review the diff against the spec and architecture guidelines. Respond with either APPROVED or CHANGES_REQUESTED, then a bullet list of findings. If approved, no blocking issues.";
        var userPrompt = $"Architecture Guidelines:\\n{architecture}\\n\\nSpec:\\n{spec}\\n\\nPR Diff:\\n{diff}";
        var response = await ctx.Llm.GetUpdatedFileAsync(ctx.Config.TechLeadModel, systemPrompt, userPrompt);
        var reviewNotes = $"TechLead review for PR #{prNumber.Value}:\n\n{response}";
        var reviewPath = $"orchestrator/reviews/issue-{ctx.WorkItem.Number}.md";
        var templatePath = "orchestrator/docs/templates/review.md";
        var tokens = AgentTemplateUtil.BuildTokens(ctx);
        var reviewContent = AgentTemplateUtil.RenderTemplate(
            ctx.Workspace,
            templatePath,
            tokens,
            AgentTemplateUtil.ReviewTemplateFallback);
        var decision = response.Contains("CHANGES_REQUESTED", StringComparison.OrdinalIgnoreCase)
            ? "CHANGES_REQUESTED"
            : "APPROVED";
        reviewContent = AgentTemplateUtil.UpdateStatus(
            reviewContent,
            decision);
        reviewContent = UpdateReviewContent(reviewContent, reviewNotes, decision);

        ctx.Repo.EnsureBranch(branch, ctx.Config.DefaultBaseBranch);
        ctx.Workspace.WriteAllText(reviewPath, reviewContent);
        ctx.Repo.CommitAndPush(branch, $"docs: add review for issue {ctx.WorkItem.Number}", new[] { reviewPath });

        if (decision == "CHANGES_REQUESTED")
        {
            return new AgentResult(
                Success: true,
                Notes: reviewNotes,
                AddLabels: new[] { ctx.Config.CodeReviewChangesRequestedLabel },
                RemoveLabels: new[] { ctx.Config.CodeReviewNeededLabel }
            );
        }

        return new AgentResult(
            Success: true,
            Notes: reviewNotes,
            AddLabels: new[] { ctx.Config.CodeReviewApprovedLabel },
            RemoveLabels: new[] { ctx.Config.CodeReviewNeededLabel, ctx.Config.CodeReviewChangesRequestedLabel }
        );
    }

    private static string UpdateReviewContent(string content, string reviewNotes, string decision)
    {
        var updated = ReplaceSection(content, "## Notes", reviewNotes);
        updated = ReplaceSection(updated, "## Decision", decision);
        return updated;
    }

    private static string ReplaceSection(string content, string header, string body)
    {
        var start = content.IndexOf(header, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return content + $"\n\n{header}\n{body}\n";
        }

        var sectionStart = start + header.Length;
        var nextHeader = content.IndexOf("\n## ", sectionStart, StringComparison.OrdinalIgnoreCase);
        var end = nextHeader >= 0 ? nextHeader : content.Length;
        var before = content[..start];
        var after = content[end..];
        return before + header + "\n" + body.TrimEnd() + "\n" + after.TrimStart('\n');
    }
}
