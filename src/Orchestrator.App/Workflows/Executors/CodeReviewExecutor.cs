using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class CodeReviewExecutor : WorkflowStageExecutor
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
        var devJson = await ReadStateWithFallbackAsync(
            context,
            WorkflowStateKeys.DevResult,
            cancellationToken);

        if (!WorkflowJson.TryDeserialize(devJson, out DevResult? devResult) || devResult is null)
        {
            return (false, "Code review blocked: missing dev result.");
        }

        var diffSummary = BuildDiffSummary(devResult.ChangedFiles);
        var prompt = CodeReviewPrompt.Build(input.WorkItem, devResult.ChangedFiles, diffSummary);
        var response = await CallLlmAsync(
            WorkContext.Config.OpenAiModel,
            prompt.System,
            prompt.User,
            cancellationToken);

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

        await FileOperationHelper.WriteAllTextAsync(WorkContext, finalResult.ReviewPath, BuildReviewMarkdown(finalResult));
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

    private string BuildDiffSummary(IReadOnlyList<string> changedFiles)
    {
        var summaryLines = new List<string>();
        foreach (var file in changedFiles)
        {
            if (!WorkItemParsers.IsSafeRelativePath(file))
            {
                continue;
            }

            if (!WorkContext.Workspace.Exists(file))
            {
                summaryLines.Add($"{file}: deleted");
                continue;
            }

            var content = FileOperationHelper.ReadAllTextAsync(WorkContext, file).GetAwaiter().GetResult();
            summaryLines.Add($"{file}:\n{content}");
        }

        return string.Join("\n\n", summaryLines);
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
        // Try to load template, fallback to hardcoded if not found
        string content;
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
                    content = template;
                }
                else
                {
                    // Template file exists but is empty or ReadOrTemplate returned null - use fallback
                    content = ApplyTokensToTemplate(DefaultReviewTemplate);
                }
            }
            else
            {
                // Use hardcoded template and manually replace tokens
                content = ApplyTokensToTemplate(DefaultReviewTemplate);
            }
        }
        catch
        {
            // Any error reading template - use fallback
            content = ApplyTokensToTemplate(DefaultReviewTemplate);
        }

        // Replace decision status
        var decision = result.Approved ? "APPROVED" : "CHANGES_REQUESTED";
        content = content.Replace("APPROVED | CHANGES_REQUESTED", decision);
        content = content.Replace("STATUS: PENDING", $"STATUS: {decision}");

        // Build findings section
        var findingsSection = new System.Text.StringBuilder();
        findingsSection.AppendLine("## Findings");
        if (result.Findings.Count == 0)
        {
            findingsSection.AppendLine("- None");
        }
        else
        {
            foreach (var finding in result.Findings)
            {
                var location = string.IsNullOrWhiteSpace(finding.File)
                    ? ""
                    : $" ({finding.File}{(finding.Line.HasValue ? $":{finding.Line}" : "")})";
                findingsSection.AppendLine($"- [{finding.Severity}] {finding.Category}: {finding.Message}{location}");
            }
        }

        // Build summary section
        var summarySection = new System.Text.StringBuilder();
        summarySection.AppendLine("## Summary");
        summarySection.AppendLine(result.Summary);

        // Replace sections in template
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"## Findings\s*\n- None",
            findingsSection.ToString().TrimEnd(),
            System.Text.RegularExpressions.RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));

        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"## Summary\s*\n- Review notes here\.",
            summarySection.ToString().TrimEnd(),
            System.Text.RegularExpressions.RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));

        return content;
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
}
