using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchestrator.App.Agents;

internal sealed class TechLeadAgent : IRoleAgent
{
    public Task<AgentResult> RunAsync(WorkContext ctx)
    {
        return RunTechLeadAsync(ctx);
    }

    private static async Task<AgentResult> RunTechLeadAsync(WorkContext ctx)
    {
        var branch = WorkItemBranch.BuildBranchName(ctx.WorkItem);
        var specPath = $"specs/issue-{ctx.WorkItem.Number}.md";
        if (ctx.Workspace.Exists(specPath) &&
            !ctx.WorkItem.Labels.Contains(ctx.Config.Labels.SpecQuestionsLabel, StringComparer.OrdinalIgnoreCase))
        {
            var existing = ctx.Workspace.ReadAllText(specPath);
            if (AgentTemplateUtil.IsStatusComplete(existing))
            {
                return AgentResult.Ok($"Spec already complete at `{specPath}`. Skipping.");
            }
        }

        var specContent = await BuildSpecAsync(ctx);

        ctx.Repo.EnsureBranch(branch, ctx.Config.Workflow.DefaultBaseBranch);
        ctx.Workspace.WriteAllText(specPath, specContent);
        ctx.Repo.CommitAndPush(branch, $"docs: add spec for issue {ctx.WorkItem.Number}", new[] { specPath });

        var notes = $"TechLead updated `{specPath}`.";
        var add = new List<string> { ctx.Config.Labels.SpecClarifiedLabel };
        var remove = new List<string>();
        var next = ctx.Config.Labels.DevLabel;

        remove.Add(ctx.Config.Labels.CodeReviewApprovedLabel);
        remove.Add(ctx.Config.Labels.CodeReviewNeededLabel);
        remove.Add(ctx.Config.Labels.CodeReviewChangesRequestedLabel);

        if (ctx.WorkItem.Labels.Contains(ctx.Config.Labels.SpecQuestionsLabel, StringComparer.OrdinalIgnoreCase))
        {
            var answered = UpdateQuestionsWithAnswers(ctx, specContent);
            if (answered)
            {
                remove.Add(ctx.Config.Labels.SpecQuestionsLabel);
                notes += " Addressed spec questions.";
            }
            else
            {
                notes += " Spec questions remain open.";
                next = ctx.Config.Labels.TechLeadLabel;
            }
        }

        return new AgentResult(true, notes, next, add, remove);
    }

    private static async Task<string> BuildSpecAsync(WorkContext ctx)
    {
        var planPath = $"plans/issue-{ctx.WorkItem.Number}.md";
        var plan = ctx.Workspace.Exists(planPath) ? ctx.Workspace.ReadAllText(planPath) : "";
        var questionsPath = $"questions/issue-{ctx.WorkItem.Number}.md";
        var devQuestions = ctx.Workspace.Exists(questionsPath) ? ctx.Workspace.ReadAllText(questionsPath) : "None";
        var filesHint = "- Use existing C# files under src/Orchestrator.App and add tests under tests or Assets/Tests.";
        var appFiles = ctx.Workspace.ListFiles("src/Orchestrator.App", "*.cs", 200);
        var testFiles = ctx.Workspace.ListFiles("tests", "*.cs", 200);
        var appFileList = string.Join("\n", appFiles);
        var testFileList = string.Join("\n", testFiles);

        var architecture = ctx.Workspace.Exists("Assets/Docs/architecture.md")
            ? ctx.Workspace.ReadAllText("Assets/Docs/architecture.md")
            : "";
        var systemPrompt = "You are a tech lead. Produce a concise C# implementation spec for this repo. Enforce the architecture guidelines. Always include a Files section with concrete file paths (C#) and explicit test files. Do not invent other languages.";
        var userPrompt = $"Architecture Guidelines:\n{architecture}\n\nIssue title: {ctx.WorkItem.Title}\n\nIssue body:\n{ctx.WorkItem.Body}\n\nPlan:\n{plan}\n\nDev questions:\n{devQuestions}\n\nFiles guidance:\n{filesHint}\n\nExisting app files:\n{appFileList}\n\nExisting test files:\n{testFileList}\n\nReturn markdown spec with sections: Scope, Files, Risks, Implementation Plan, Acceptance Criteria.";

        var content = await ctx.Llm.GetUpdatedFileAsync(ctx.Config.OpenAiModel, systemPrompt, userPrompt);
        var spec = AgentHelpers.StripCodeFence(content);
        spec = EnsureFilesSection(spec, ctx, appFiles.ToList());
        spec = EnsureValidFilesSection(spec, ctx, appFiles.ToList(), testFiles.ToList());
        spec = AgentTemplateUtil.EnsureTemplateHeader(spec, ctx, "docs/templates/spec.md");
        spec = AgentTemplateUtil.UpdateStatus(spec, "COMPLETE");
        return spec;
    }

    private static string EnsureFilesSection(string spec, WorkContext ctx, List<string> appFiles)
    {
        var files = WorkItemParsers.TryParseSpecFiles(spec);
        if (files.Count > 0)
        {
            return spec;
        }

        var isOrchestrator = ctx.WorkItem.Title.Contains("orchestrator", StringComparison.OrdinalIgnoreCase) ||
            ctx.WorkItem.Body.Contains("orchestrator", StringComparison.OrdinalIgnoreCase);

        var appFallback = appFiles.FirstOrDefault() ??
            (isOrchestrator ? "src/Orchestrator.App/Program.cs" : "Assets/Scripts/Placeholder.cs");
        var testFallback = isOrchestrator
            ? $"tests/Issue{ctx.WorkItem.Number}Tests.cs"
            : $"Assets/Tests/Issue{ctx.WorkItem.Number}Tests.cs";

        var filesSection = $"\n\n## Files\n- {appFallback}\n- {testFallback}\n";
        return spec + filesSection;
    }

    private static string EnsureValidFilesSection(string spec, WorkContext ctx, List<string> appFiles, List<string> testFiles)
    {
        var files = WorkItemParsers.TryParseSpecFiles(spec);
        if (files.Count == 0)
        {
            return spec;
        }

        var invalid = files.Any(path =>
            path.StartsWith("path/to", StringComparison.OrdinalIgnoreCase) ||
            !WorkItemParsers.IsSafeRelativePath(path));
        if (!invalid)
        {
            return spec;
        }

        var appFallback = appFiles.FirstOrDefault() ?? "src/Orchestrator.App/Program.cs";
        var testFallback = testFiles.FirstOrDefault() ?? $"tests/Issue{ctx.WorkItem.Number}Tests.cs";
        var filesSection = $"## Files\n- {appFallback}\n- {testFallback}\n";
        return AgentTemplateUtil.ReplaceSection(spec, "## Files", filesSection);
    }

    private static bool UpdateQuestionsWithAnswers(WorkContext ctx, string specContent)
    {
        var questionsPath = $"questions/issue-{ctx.WorkItem.Number}.md";
        var templatePath = "docs/templates/questions.md";
        var tokens = AgentTemplateUtil.BuildTokens(ctx);
        var content = ctx.Workspace.ReadOrTemplate(questionsPath, templatePath, tokens);
        var answers = GenerateAnswers(ctx, specContent, content);
        if (string.IsNullOrWhiteSpace(answers))
        {
            return false;
        }

        var updated = AgentTemplateUtil.ReplaceSection(content, "## Answers", answers);
        updated = AgentTemplateUtil.UpdateStatus(updated, "CLARIFIED");
        ctx.Workspace.WriteAllText(questionsPath, updated);
        ctx.Repo.CommitAndPush(WorkItemBranch.BuildBranchName(ctx.WorkItem), $"docs: answer questions for issue {ctx.WorkItem.Number}", new[] { questionsPath });
        return true;
    }

    private static string GenerateAnswers(WorkContext ctx, string specContent, string questionsContent)
    {
        var systemPrompt = "You are a tech lead. Answer each question concisely. Return only bullet points.";
        var userPrompt = $"Spec:\n{specContent}\n\nQuestions:\n{questionsContent}\n\nProvide answers as list items.";
        var response = ctx.Llm.GetUpdatedFileAsync(ctx.Config.TechLeadModel, systemPrompt, userPrompt).GetAwaiter().GetResult();
        var cleaned = AgentHelpers.StripCodeFence(response).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "";
        }

        if (!cleaned.StartsWith("-", StringComparison.Ordinal))
        {
            cleaned = "- " + cleaned.Replace("\n", "\n- ");
        }

        return cleaned;
    }
}
