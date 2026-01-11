using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed partial class CodeReviewExecutor : WorkflowStageExecutor
{
    private bool _forceHumanReview;

    public CodeReviewExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("CodeReview", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.CodeReview;
    protected override string Notes => "Code review completed.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Info($"[CodeReview] Starting code review for issue #{input.WorkItem.Number}");

        var branchName = WorkItemBranch.BuildBranchName(input.WorkItem);
        Logger.Info($"[CodeReview] Looking for PR for branch '{branchName}'");
        var prNumber = await WorkContext.GitHub.GetPullRequestNumberAsync(branchName);

        if (prNumber == null)
        {
            Logger.Warning($"[CodeReview] No PR found for branch '{branchName}'");
            return (false, "Code review blocked: Pull Request not found.");
        }

        Logger.Info($"[CodeReview] Found PR #{prNumber}. Fetching diff...");
        var diff = await WorkContext.GitHub.GetPullRequestDiffAsync(prNumber.Value);
        Logger.Info($"[CodeReview] Diff fetched. Length: {diff?.Length ?? 0} chars");

        var prompt = CodeReviewPrompt.Build(input.WorkItem, new List<string>(), diff ?? "");
        
        Logger.Info($"[CodeReview] Calling LLM with diff...");
        var response = await CallLlmAsync(
            WorkContext.Config.OpenAiModel,
            prompt.System,
            prompt.User,
            cancellationToken);

        Logger.Info($"[CodeReview] LLM Response received. Preview: {(response.Length > 200 ? response.Substring(0, 200) + "..." : response)}");

        if (!TryParseReview(response, out var reviewResult))
        {
            reviewResult = new CodeReviewResult(
                Approved: false,
                Findings: new List<ReviewFinding>
                {
                    new(ReviewSeverity.Major, "Parsing", "Code review output was invalid.")
                },
                Summary: "Review output invalid.",
                ReviewPath: WorkflowPaths.ReviewPath(input.WorkItem.Number));
        }

        var blockerCount = reviewResult.Findings.Count(f => f.Severity == ReviewSeverity.Blocker);
        var majorCount = reviewResult.Findings.Count(f => f.Severity == ReviewSeverity.Major);
        var approved = reviewResult.Approved && blockerCount == 0 && majorCount < 3;

        var requiresHuman = !approved && CurrentAttempt >= 3;
        if (requiresHuman)
        {
            _forceHumanReview = true;
        }

        var finalResult = reviewResult with
        {
            Approved = approved,
            RequiresHumanReview = requiresHuman,
            ReviewPath = WorkflowPaths.ReviewPath(input.WorkItem.Number)
        };

        WorkContext.Metrics?.RecordCodeReview(finalResult.Findings.Count, finalResult.Approved);

        var reviewMarkdown = BuildReviewMarkdown(finalResult);
        await FileOperationHelper.WriteAllTextAsync(WorkContext, finalResult.ReviewPath, reviewMarkdown);
        try
        {
            await WorkContext.GitHub.CommentOnWorkItemAsync(prNumber.Value, reviewMarkdown);
        }
        catch (Exception ex)
        {
            Logger.Warning($"[CodeReview] Failed to post comment on PR #{prNumber}: {ex.Message}");
        }

        var serializedResult = WorkflowJson.Serialize(finalResult);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.CodeReviewResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.CodeReviewResult] = serializedResult;

        var notes = approved
            ? "Code review approved."
            : requiresHuman
                ? "Code review requires human review."
                : "Code review changes requested.";
        return (approved, notes);
    }

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        if (_forceHumanReview)
        {
            return null;
        }

        return base.DetermineNextStage(success, input);
    }

    private static bool TryParseReview(string? json, out CodeReviewResult result)
    {
        result = null!;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var approved = root.TryGetProperty("approved", out var approvedProp) && approvedProp.GetBoolean();
            var summary = root.TryGetProperty("summary", out var summaryProp) ? summaryProp.GetString() ?? "" : "";
            var findings = new List<ReviewFinding>();

            if (root.TryGetProperty("findings", out var findingsProp) && findingsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var finding in findingsProp.EnumerateArray())
                {
                    var severityRaw = finding.TryGetProperty("severity", out var sevProp) ? sevProp.GetString() ?? "" : "";
                    var severity = severityRaw.ToUpperInvariant() switch
                    {
                        "BLOCKER" => ReviewSeverity.Blocker,
                        "MAJOR" => ReviewSeverity.Major,
                        _ => ReviewSeverity.Minor
                    };
                    var category = finding.TryGetProperty("category", out var catProp) ? catProp.GetString() ?? "" : "";
                    var message = finding.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "" : "";
                    var file = finding.TryGetProperty("file", out var fileProp) ? fileProp.GetString() : null;
                    var line = finding.TryGetProperty("line", out var lineProp) && lineProp.TryGetInt32(out var lineValue)
                        ? lineValue
                        : (int?)null;

                    findings.Add(new ReviewFinding(severity, category, message, file, line));
                }
            }

            result = new CodeReviewResult(approved, findings, summary, WorkflowPaths.ReviewPath(0));
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private string BuildReviewMarkdown(CodeReviewResult result)
    {
        var content = LoadReviewTemplate();
        content = ApplyDecisionStatus(content, result);

        var findingsSection = BuildFindingsSection(result);
        var summarySection = BuildSummarySection(result);

        content = FindingsSectionRegex().Replace(content, findingsSection.TrimEnd());
        content = SummarySectionRegex().Replace(content, summarySection.TrimEnd());

        return content;
    }

    private string LoadReviewTemplate()
    {
        try
        {
            if (WorkContext.Workspace.Exists(WorkflowPaths.ReviewTemplatePath))
            {
                var template = WorkContext.Workspace.ReadOrTemplate(
                    WorkflowPaths.ReviewTemplatePath,
                    WorkflowPaths.ReviewTemplatePath,
                    TemplateUtil.BuildTokens(WorkContext));

                if (!string.IsNullOrWhiteSpace(template))
                {
                    return template;
                }
            }
        }
        catch
        {
            // Fall through to default template
        }

        return ApplyTokensToTemplate(DefaultReviewTemplate);
    }

    private static string ApplyDecisionStatus(string content, CodeReviewResult result)
    {
        var decision = result.Approved ? "APPROVED" : "CHANGES_REQUESTED";
        content = content.Replace("APPROVED | CHANGES_REQUESTED", decision);
        content = content.Replace("STATUS: PENDING", $"STATUS: {decision}");
        return content;
    }

    private static string BuildFindingsSection(CodeReviewResult result)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("## Findings");

        if (result.Findings.Count == 0)
        {
            builder.AppendLine("- None");
            return builder.ToString();
        }

        foreach (var finding in result.Findings)
        {
            var location = FormatFindingLocation(finding);
            builder.AppendLine($"- [{finding.Severity}] {finding.Category}: {finding.Message}{location}");
        }

        return builder.ToString();
    }

    private static string FormatFindingLocation(ReviewFinding finding)
    {
        if (string.IsNullOrWhiteSpace(finding.File))
        {
            return "";
        }

        var lineInfo = finding.Line.HasValue ? $":{finding.Line}" : "";
        return $" ({finding.File}{lineInfo})";
    }

    private static string BuildSummarySection(CodeReviewResult result)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("## Summary");
        builder.AppendLine(result.Summary);
        return builder.ToString();
    }

    private string ApplyTokensToTemplate(string template)
    {
        var content = template;
        var tokens = TemplateUtil.BuildTokens(WorkContext);
        foreach (var pair in tokens)
        {
            content = content.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
        }
        return content;
    }

    private const string DefaultReviewTemplate =
        "# Code Review: Issue {{ISSUE_NUMBER}} - {{ISSUE_TITLE}}\n\n" +
        "STATUS: PENDING\n" +
        "UPDATED: {{UPDATED_AT_UTC}}\n\n" +
        "## Decision\n" +
        "APPROVED | CHANGES_REQUESTED\n\n" +
        "## Findings\n" +
        "- None\n\n" +
        "## Notes\n" +
        "- Review notes here.\n";

    [System.Text.RegularExpressions.GeneratedRegex(@"## Findings\s*\n- None", System.Text.RegularExpressions.RegexOptions.Multiline, 1000)]
    private static partial System.Text.RegularExpressions.Regex FindingsSectionRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"## Summary\s*\n- Review notes here\.", System.Text.RegularExpressions.RegexOptions.Multiline, 1000)]
    private static partial System.Text.RegularExpressions.Regex SummarySectionRegex();
}
