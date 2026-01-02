using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Agents;

internal sealed class DevAgent : IRoleAgent
{
    public async Task<AgentResult> RunAsync(WorkContext ctx)
    {
        var summaryResult = await TrySummarizeProjectAsync(ctx);
        if (summaryResult != null)
        {
            return summaryResult;
        }

        var shouldAddSpecClarifiedLabel = false;
        if (!ctx.WorkItem.Labels.Contains(ctx.Config.SpecClarifiedLabel, StringComparer.OrdinalIgnoreCase))
        {
            if (IsQuestionsClarified(ctx))
            {
                shouldAddSpecClarifiedLabel = true;
            }
            else
            {
                var notes = "Spec clarification needed. Please review the spec and address open questions.";
                return new AgentResult(
                    Success: true,
                    Notes: notes,
                    NextStageLabel: ctx.Config.TechLeadLabel,
                    AddLabels: new[] { ctx.Config.SpecQuestionsLabel },
                    RemoveLabels: new[] { ctx.Config.SpecClarifiedLabel }
                );
            }
        }

        var branch = WorkItemBranch.BuildBranchName(ctx.WorkItem);
        var specPath = $"specs/issue-{ctx.WorkItem.Number}.md";

        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, specPath);
        if (specContent == null)
        {
            return new AgentResult(
                Success: true,
                Notes: "Spec file missing. Please provide a spec.",
                NextStageLabel: ctx.Config.TechLeadLabel,
                AddLabels: new[] { ctx.Config.SpecQuestionsLabel }
            );
        }
        var reviewNotes = TryGetReviewNotes(ctx);
        var questionAnswers = await TryGetQuestionAnswersAsync(ctx);
        var reviewFiles = TryGetReviewFiles(ctx);
        var files = reviewFiles.Count > 0 ? reviewFiles : WorkItemParsers.TryParseSpecFiles(specContent);
        if (files.Count == 0)
        {
            return await CreateSpecQuestionAsync(
                ctx,
                "Spec lacks a Files section. Please list target files.",
                ctx.Config.TechLeadLabel
            );
        }

        var invalid = AgentHelpers.ValidateSpecFiles(files, ctx.Workspace);
        if (invalid.Count > 0)
        {
            var notes = $"Spec file list contains invalid paths: {string.Join(", ", invalid)}";
            return await CreateSpecQuestionAsync(ctx, notes, ctx.Config.TechLeadLabel);
        }

        if (!files.Any(AgentHelpers.IsTestFile))
        {
            return await CreateSpecQuestionAsync(
                ctx,
                "Spec does not include any test files. Please list unit tests to add or update.",
                ctx.Config.TechLeadLabel
            );
        }

        ctx.Repo.EnsureBranch(branch, ctx.Config.DefaultBaseBranch);
        var updatedFiles = new List<string>();
        foreach (var file in files)
        {
            if (string.Equals(file, specPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var currentContent = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, file) ?? "";

            var updated = await GetUpdatedFileAsync(ctx, specContent, reviewNotes, questionAnswers, file, currentContent);
            if (string.IsNullOrWhiteSpace(updated))
            {
                return await CreateSpecQuestionAsync(
                    ctx,
                    $"Generated empty content for `{file}`. Please clarify requirements.",
                    ctx.Config.TechLeadLabel
                );
            }

            await FileOperationHelper.WriteAllTextAsync(ctx, file, updated);
            updatedFiles.Add(file);
        }

        var reviewForCheck = reviewNotes == "None" ? "" : reviewNotes;
        var answersForCheck = questionAnswers == "None" ? "" : questionAnswers;
        var (checkOk, _, missing) = await RunSelfCheckAsync(ctx, specContent, reviewForCheck, answersForCheck, updatedFiles);
        if (!checkOk)
        {
            await RemediateFilesAsync(ctx, specContent, reviewForCheck, answersForCheck, updatedFiles, missing);
            var (retryOk, retryNotes, _) = await RunSelfCheckAsync(ctx, specContent, reviewForCheck, answersForCheck, updatedFiles);
            if (!retryOk)
            {
                return await CreateSpecQuestionAsync(
                    ctx,
                    $"Self-check failed against spec/review: {retryNotes}",
                    ctx.Config.TechLeadLabel
                );
            }
        }

        var updatedSpec = WorkItemParsers.MarkAcceptanceCriteriaDone(specContent);
        if (!string.Equals(updatedSpec, specContent, StringComparison.Ordinal))
        {
            await FileOperationHelper.WriteAllTextAsync(ctx, specPath, updatedSpec);
            updatedFiles.Add(specPath);
        }

        var committed = ctx.Repo.CommitAndPush(branch, $"feat: implement issue {ctx.WorkItem.Number}", updatedFiles);
        if (!committed)
        {
            if (shouldAddSpecClarifiedLabel)
            {
                return new AgentResult(
                    Success: true,
                    Notes: "No changes to commit. Spec/review may already be satisfied or requires clarification.",
                    AddLabels: new[] { ctx.Config.SpecClarifiedLabel }
                );
            }

            return new AgentResult(
                Success: true,
                Notes: "No changes to commit. Spec/review may already be satisfied or requires clarification."
            );
        }

        var addLabels = new List<string> { ctx.Config.CodeReviewNeededLabel };
        if (shouldAddSpecClarifiedLabel)
        {
            addLabels.Add(ctx.Config.SpecClarifiedLabel);
        }

        return new AgentResult(
            Success: true,
            Notes: "Implemented changes and updated acceptance criteria in the spec.",
            AddLabels: addLabels
        );
    }

    private static async Task<AgentResult?> TrySummarizeProjectAsync(WorkContext ctx)
    {
        var body = ctx.WorkItem.Body ?? string.Empty;
        var projectRef = WorkItemParsers.TryParseProjectReference(body);
        var targetIssueNumber = WorkItemParsers.TryParseIssueNumber(body);

        if (projectRef is null || targetIssueNumber is null)
        {
            return null;
        }

        ProjectSnapshot snapshot;
        try
        {
            snapshot = await ctx.GitHub.GetProjectSnapshotAsync(projectRef.Owner, projectRef.Number, projectRef.OwnerType);
        }
        catch (InvalidOperationException ex)
        {
            return AgentResult.Fail($"DevAgent failed to summarize project: {ex.Message}. Check the project URL and token permissions.");
        }
        var comment = ProjectSummaryFormatter.Format(snapshot);

        await ctx.GitHub.CommentOnWorkItemAsync(targetIssueNumber.Value, comment);

        var notes = $"Posted project summary to issue #{targetIssueNumber.Value}.\n\n{comment}";
        return AgentResult.Ok(notes);
    }

    private static async Task<string> GetUpdatedFileAsync(
        WorkContext ctx,
        string spec,
        string reviewNotes,
        string questionAnswers,
        string filePath,
        string currentContent)
    {
        var architecture = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, "Assets/Docs/architecture.md") ?? "";

        var systemPrompt = "You are a senior engineer. Update the file according to the spec, review notes, and architecture guidelines. Output ONLY the full file content, no markdown, no code fences.";
        var userPrompt = $"Architecture Guidelines:\\n{architecture}\\n\\nSpec:\\n{spec}\\n\\nTechLead Review Notes:\\n{reviewNotes}\\n\\nTechLead Answers:\\n{questionAnswers}\\n\\nFile: {filePath}\\n\\nCurrent content:\\n{currentContent}";
        var response = await ctx.Llm.GetUpdatedFileAsync(ctx.Config.DevModel, systemPrompt, userPrompt);
        return AgentHelpers.StripCodeFence(response);
    }

    private static async Task<(bool Ok, string Notes, List<string> Missing)> RunSelfCheckAsync(
        WorkContext ctx,
        string spec,
        string reviewNotes,
        string questionAnswers,
        List<string> updatedFiles)
    {
        if (updatedFiles.Count == 0)
        {
            return (false, "No files were updated.", new List<string>());
        }

        var fileBlocks = new List<string>();
        foreach (var file in updatedFiles)
        {
            var content = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, file);
            if (content == null)
            {
                continue;
            }

            fileBlocks.Add($"File: {file}\n{AgentHelpers.Truncate(content, 8000)}");
        }

        var systemPrompt = "You are a dev agent verifying work. Check the spec and review against the updated files. Identify any missing acceptance criteria or review action items. Return JSON: {\"ok\":true|false,\"missing\":[\"...\"]}.";
        var userPrompt =
            $"Spec:\n{spec}\n\nReview:\n{reviewNotes}\n\nAnswers:\n{questionAnswers}\n\nUpdated files:\n{string.Join("\n\n", fileBlocks)}";

        var response = await ctx.Llm.GetUpdatedFileAsync(ctx.Config.DevModel, systemPrompt, userPrompt);
        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
            var missing = new List<string>();
            if (root.TryGetProperty("missing", out var missingProp) && missingProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in missingProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        missing.Add(item.GetString() ?? "");
                    }
                }
            }

            if (ok)
            {
                return (true, "Self-check passed.", missing);
            }

            var notes = missing.Count > 0 ? string.Join("; ", missing) : response.Trim();
            return (false, notes, missing);
        }
        catch (JsonException)
        {
            return (false, $"Invalid self-check response: {response.Trim()}", new List<string>());
        }
    }

    private static async Task RemediateFilesAsync(
        WorkContext ctx,
        string spec,
        string reviewNotes,
        string questionAnswers,
        List<string> updatedFiles,
        List<string> missingItems)
    {
        if (missingItems.Count == 0)
        {
            return;
        }

        var remediationNotes = reviewNotes + "\n\nAnswers:\n" + questionAnswers + "\n\nMissing items to address:\n- " + string.Join("\n- ", missingItems);
        foreach (var file in updatedFiles)
        {
            var currentContent = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, file) ?? "";

            var updated = await GetUpdatedFileAsync(ctx, spec, remediationNotes, questionAnswers, file, currentContent);
            if (!string.IsNullOrWhiteSpace(updated))
            {
                await FileOperationHelper.WriteAllTextAsync(ctx, file, updated);
            }
        }
    }

    private static string TryGetReviewNotes(WorkContext ctx)
    {
        var reviewPath = $"orchestrator/reviews/issue-{ctx.WorkItem.Number}.md";
        if (!ctx.Workspace.Exists(reviewPath))
        {
            return "None";
        }

        return ctx.Workspace.ReadAllText(reviewPath);
    }

    private static List<string> TryGetReviewFiles(WorkContext ctx)
    {
        var reviewPath = $"orchestrator/reviews/issue-{ctx.WorkItem.Number}.md";
        if (!ctx.Workspace.Exists(reviewPath))
        {
            return new List<string>();
        }

        var content = ctx.Workspace.ReadAllText(reviewPath);
        if (!AgentTemplateUtil.IsStatus(content, "CHANGES_REQUESTED"))
        {
            return new List<string>();
        }

        return WorkItemParsers.TryParseSpecFiles(content);
    }

    private static async Task<string> TryGetQuestionAnswersAsync(WorkContext ctx)
    {
        var questionsPath = $"questions/issue-{ctx.WorkItem.Number}.md";

        var content = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, questionsPath);
        if (content == null)
        {
            return "None";
        }

        var answers = WorkItemParsers.TryParseSection(content, "## Answers");
        return string.IsNullOrWhiteSpace(answers) ? "None" : answers.Trim();
    }

    private static async Task<AgentResult> CreateSpecQuestionAsync(WorkContext ctx, string question, string nextStageLabel)
    {
        var branch = WorkItemBranch.BuildBranchName(ctx.WorkItem);
        var questionsPath = $"questions/issue-{ctx.WorkItem.Number}.md";
        var templatePath = "docs/templates/questions.md";
        var tokens = AgentTemplateUtil.BuildTokens(ctx);

        var existingContent = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, questionsPath);

        if (existingContent != null)
        {
            if (AgentTemplateUtil.IsStatus(existingContent, "CLARIFIED"))
            {
                return AgentResult.Fail($"Spec questions already clarified, but Dev still needs clarification: {question}");
            }

            if (existingContent.Contains(question, StringComparison.OrdinalIgnoreCase))
            {
                return new AgentResult(
                    Success: true,
                    Notes: "Spec question already asked. Waiting for TechLead clarification.",
                    NextStageLabel: nextStageLabel,
                    AddLabels: new[] { ctx.Config.SpecQuestionsLabel }
                );
            }
        }

        var content = ctx.Workspace.ReadOrTemplate(questionsPath, templatePath, tokens);
        var updated = AgentTemplateUtil.UpdateStatus(content, "NEEDS_CLARIFICATION");
        updated = AgentTemplateUtil.AppendQuestion(updated, question);

        ctx.Repo.EnsureBranch(branch, ctx.Config.DefaultBaseBranch);

        await FileOperationHelper.WriteAllTextAsync(ctx, questionsPath, updated);

        ctx.Repo.CommitAndPush(branch, $"docs: add spec questions for issue {ctx.WorkItem.Number}", new[] { questionsPath });

        return new AgentResult(
            Success: true,
            Notes: question,
            NextStageLabel: nextStageLabel,
            AddLabels: new[] { ctx.Config.SpecQuestionsLabel }
        );
    }

    private static bool IsQuestionsClarified(WorkContext ctx)
    {
        var questionsPath = $"questions/issue-{ctx.WorkItem.Number}.md";
        if (!ctx.Workspace.Exists(questionsPath))
        {
            return false;
        }

        var content = ctx.Workspace.ReadAllText(questionsPath);
        return AgentTemplateUtil.IsStatus(content, "CLARIFIED");
    }
}
