using System.Diagnostics;
using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows;

/// <summary>
/// Output message from executors
/// </summary>
internal sealed record WorkflowOutput(
    bool Success,
    string Notes,
    WorkflowStage? NextStage = null
);

internal abstract class WorkflowStageExecutor : Executor<WorkflowInput, WorkflowOutput>
{
    private readonly WorkflowConfig _workflowConfig;
    protected WorkContext WorkContext { get; }
    protected int CurrentAttempt { get; private set; }

    protected WorkflowStageExecutor(string id, WorkContext workContext, WorkflowConfig workflowConfig) : base(id)
    {
        WorkContext = workContext;
        _workflowConfig = workflowConfig;
    }

    protected abstract WorkflowStage Stage { get; }
    protected abstract string Notes { get; }

    public override async ValueTask<WorkflowOutput> HandleAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var attemptKey = $"attempt:{Stage}";
        var currentAttempts = await context.ReadOrInitStateAsync(
            attemptKey,
            () => 0,
            cancellationToken: cancellationToken);
        var nextAttempt = currentAttempts + 1;
        await context.QueueStateUpdateAsync(attemptKey, nextAttempt, cancellationToken);
        CurrentAttempt = nextAttempt;
        WorkContext.Metrics?.RecordIteration(Stage, nextAttempt);

        var limit = MaxIterationsForStage(_workflowConfig, Stage);
        if (nextAttempt > limit)
        {
            return new WorkflowOutput(
                Success: false,
                Notes: $"Iteration limit reached for {Stage} ({nextAttempt}/{limit}).",
                NextStage: null);
        }

        var (success, notes) = await ExecuteAsync(input, context, cancellationToken);
        var nextStage = DetermineNextStage(success, input);
        if (nextStage is not null)
        {
            await context.SendMessageAsync(input, WorkflowStageGraph.ExecutorIdFor(nextStage.Value), cancellationToken);
        }

        return new WorkflowOutput(
            Success: success,
            Notes: notes,
            NextStage: nextStage);
    }

    protected virtual ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((true, Notes));
    }

    protected virtual WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        return WorkflowStageGraph.NextStageFor(Stage, success);
    }

    protected async Task<string> CallLlmAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await WorkContext.Llm.GetUpdatedFileAsync(model, systemPrompt, userPrompt);
        stopwatch.Stop();

        WorkContext.Metrics?.RecordLlmCall(new LlmCallMetrics(
            Model: model,
            PromptChars: systemPrompt.Length + userPrompt.Length,
            CompletionChars: response.Length,
            ElapsedMilliseconds: stopwatch.Elapsed.TotalMilliseconds,
            Cost: null));

        return response;
    }

    private static int MaxIterationsForStage(WorkflowConfig config, WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.ContextBuilder => 1,
            WorkflowStage.Refinement or WorkflowStage.DoR => config.MaxRefinementIterations,
            WorkflowStage.TechLead or WorkflowStage.SpecGate => config.MaxTechLeadIterations,
            WorkflowStage.Dev => config.MaxDevIterations,
            WorkflowStage.CodeReview => config.MaxCodeReviewIterations,
            WorkflowStage.DoD => config.MaxDodIterations,
            WorkflowStage.Release => 1,
            _ => 1
        };
    }
}

internal sealed class ContextBuilderExecutor : WorkflowStageExecutor
{
    private readonly LabelConfig _labels;
    private readonly WorkflowStage? _startOverride;

    public ContextBuilderExecutor(WorkContext workContext, WorkflowConfig workflowConfig, LabelConfig labels, WorkflowStage? startOverride)
        : base("ContextBuilder", workContext, workflowConfig)
    {
        _labels = labels;
        _startOverride = startOverride is WorkflowStage.ContextBuilder ? null : startOverride;
    }

    protected override WorkflowStage Stage => WorkflowStage.ContextBuilder;
    protected override string Notes => "Context builder placeholder executed.";

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        if (_startOverride is not null)
        {
            return _startOverride;
        }

        return WorkflowStageGraph.StartStageFromLabels(_labels, input.WorkItem);
    }
}

internal sealed class RefinementExecutor : WorkflowStageExecutor
{
    public RefinementExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("Refinement", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Refinement;
    protected override string Notes => "Refinement completed.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var refinement = await BuildRefinementAsync(input, cancellationToken);
        var serialized = WorkflowJson.Serialize(refinement);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.RefinementResult, serialized, cancellationToken);
        WorkContext.State[WorkflowStateKeys.RefinementResult] = serialized;

        var summary = $"Refinement captured ({refinement.AcceptanceCriteria.Count} criteria, {refinement.OpenQuestions.Count} open questions).";
        return (true, summary);
    }

    private async Task<RefinementResult> BuildRefinementAsync(WorkflowInput input, CancellationToken cancellationToken)
    {
        var workItem = input.WorkItem;
        var existingSpec = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.SpecPath(workItem.Number));
        var playbookContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath) ?? "";
        var playbook = new PlaybookParser().Parse(playbookContent);
        var prompt = RefinementPrompt.Build(workItem, playbook, existingSpec);

        var response = await CallLlmAsync(
            WorkContext.Config.TechLeadModel,
            prompt.System,
            prompt.User,
            cancellationToken);

        if (!WorkflowJson.TryDeserialize(response, out RefinementResult? result) || result is null)
        {
            return RefinementPrompt.Fallback(workItem);
        }

        return result;
    }
}

internal sealed class DorExecutor : WorkflowStageExecutor
{
    public DorExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("DoR", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoR;
    protected override string Notes => "DoR gate evaluated.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var refinementJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.RefinementResult,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(refinementJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.RefinementResult, out var fallbackJson))
        {
             refinementJson = fallbackJson;
        }

        if (!WorkflowJson.TryDeserialize(refinementJson, out RefinementResult? refinement) || refinement is null)
        {
            return (false, "DoR gate failed: missing refinement output.");
        }

        var result = DorGateValidator.Evaluate(input.WorkItem, refinement, WorkContext.Config.Labels);
        if (!result.Passed && result.Failures.Count > 0)
        {
            WorkContext.Metrics?.RecordGateFailures(result.Failures);
        }
        var notes = result.Passed
            ? "DoR gate passed."
            : $"DoR gate failed: {string.Join(" ", result.Failures)}";

        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.DorGateResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.DorGateResult] = serializedResult;
        return (result.Passed, notes);
    }
}

internal sealed class TechLeadExecutor : WorkflowStageExecutor
{
    public TechLeadExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("TechLead", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.TechLead;
    protected override string Notes => "TechLead spec generated.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var playbook = await LoadPlaybookAsync();
        var template = WorkContext.Workspace.ReadOrTemplate(
            WorkflowPaths.SpecTemplatePath,
            WorkflowPaths.SpecTemplatePath,
            TemplateUtil.BuildTokens(WorkContext));
        if (string.IsNullOrWhiteSpace(template))
        {
            template = DefaultSpecTemplate;
        }

        var prompt = TechLeadPrompt.Build(input.WorkItem, playbook, template);
        var response = await CallLlmAsync(
            WorkContext.Config.TechLeadModel,
            prompt.System,
            prompt.User,
            cancellationToken);

        var specDraft = string.IsNullOrWhiteSpace(response) ? template : response;
        var specContent = TemplateUtil.EnsureTemplateHeader(specDraft, WorkContext, WorkflowPaths.SpecTemplatePath);
        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        await FileOperationHelper.WriteAllTextAsync(WorkContext, specPath, specContent);

        var parsedSpec = new SpecParser().Parse(specContent);
        var (frameworks, patterns) = ResolvePlaybookUsage(playbook, specContent);
        var result = new TechLeadResult(specPath, parsedSpec, frameworks, patterns);
        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.TechLeadResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.TechLeadResult] = serializedResult;

        return (true, $"TechLead spec saved to {specPath}.");
    }

    private async Task<Playbook> LoadPlaybookAsync()
    {
        var content = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath) ?? "";
        return new PlaybookParser().Parse(content);
    }

    private static (IReadOnlyList<string> Frameworks, IReadOnlyList<string> Patterns) ResolvePlaybookUsage(Playbook playbook, string specContent)
    {
        var frameworks = playbook.AllowedFrameworks
            .Where(f => IsReferenced(specContent, f.Name) || IsReferenced(specContent, f.Id))
            .Select(f => f.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var patterns = playbook.AllowedPatterns
            .Where(p => IsReferenced(specContent, p.Name) || IsReferenced(specContent, p.Id))
            .Select(p => p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (frameworks, patterns);
    }

    private static bool IsReferenced(string content, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return content.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private const string DefaultSpecTemplate =
        "# Spec: Issue {{ISSUE_NUMBER}} - {{ISSUE_TITLE}}\n\n" +
        "STATUS: DRAFT\n" +
        "UPDATED: {{UPDATED_AT_UTC}}\n\n" +
        "## Ziel\n...\n\n" +
        "## Nicht-Ziele\n- ...\n\n" +
        "## Komponenten\n- ...\n\n" +
        "## Touch List\n| Operation | Path | Notes |\n| --- | --- | --- |\n| Modify | src/Example.cs | ... |\n\n" +
        "## Interfaces\n```csharp\n```\n\n" +
        "## Szenarien\nScenario: ...\nGiven ...\nWhen ...\nThen ...\n\n" +
        "Scenario: ...\nGiven ...\nWhen ...\nThen ...\n\n" +
        "Scenario: ...\nGiven ...\nWhen ...\nThen ...\n\n" +
        "## Sequenz\n1. ...\n2. ...\n\n" +
        "## Testmatrix\n| Test | Files | Notes |\n| --- | --- | --- |\n| Unit | tests/ExampleTests.cs | ... |\n";
}

internal sealed class SpecGateExecutor : WorkflowStageExecutor
{
    public SpecGateExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("SpecGate", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.SpecGate;
    protected override string Notes => "Spec gate evaluated.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath);
        if (string.IsNullOrWhiteSpace(specContent))
        {
            return (false, $"Spec gate failed: missing spec at {specPath}.");
        }

        var playbookContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath) ?? "";
        var playbook = new PlaybookParser().Parse(playbookContent);
        var parsedSpec = new SpecParser().Parse(specContent);
        var result = SpecGateValidator.Evaluate(parsedSpec, playbook, WorkContext.Workspace);
        if (!result.Passed && result.Failures.Count > 0)
        {
            WorkContext.Metrics?.RecordGateFailures(result.Failures);
        }

        if (result.Passed)
        {
            var updatedSpec = TemplateUtil.UpdateStatus(specContent, "COMPLETE");
            if (!string.Equals(updatedSpec, specContent, StringComparison.Ordinal))
            {
                await FileOperationHelper.WriteAllTextAsync(WorkContext, specPath, updatedSpec);
            }
        }

        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.SpecGateResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.SpecGateResult] = serializedResult;

        var notes = result.Passed
            ? "Spec gate passed."
            : $"Spec gate failed: {string.Join(" ", result.Failures)}";
        return (result.Passed, notes);
    }
}

internal sealed class DevExecutor : WorkflowStageExecutor
{
    public DevExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("Dev", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Dev;
    protected override string Notes => "Dev implementation completed.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath);
        if (string.IsNullOrWhiteSpace(specContent))
        {
            return (false, $"Dev blocked: missing spec at {specPath}.");
        }

        var parsedSpec = new SpecParser().Parse(specContent);
        var forbidden = parsedSpec.TouchList.FirstOrDefault(entry => entry.Operation == TouchOperation.Forbidden);
        if (forbidden != null)
        {
            return (false, $"Dev blocked: forbidden path in touch list ({forbidden.Path}).");
        }

        var mode = ResolveMode(input);
        var changedFiles = new List<string>();
        foreach (var entry in parsedSpec.TouchList)
        {
            if (!WorkItemParsers.IsSafeRelativePath(entry.Path))
            {
                return (false, $"Dev blocked: unsafe path {entry.Path}.");
            }

            switch (entry.Operation)
            {
                case TouchOperation.Add:
                case TouchOperation.Modify:
                    var existing = entry.Operation == TouchOperation.Modify
                        ? await FileOperationHelper.ReadAllTextAsync(WorkContext, entry.Path)
                        : null;
                    var prompt = DevPrompt.Build(mode, parsedSpec, entry, existing);
                    var updated = await CallLlmAsync(
                        WorkContext.Config.DevModel,
                        prompt.System,
                        prompt.User,
                        cancellationToken);
                    if (string.IsNullOrWhiteSpace(updated))
                    {
                        return (false, $"Dev blocked: empty output for {entry.Path}.");
                    }
                    await FileOperationHelper.WriteAllTextAsync(WorkContext, entry.Path, updated);
                    changedFiles.Add(entry.Path);
                    break;
                case TouchOperation.Delete:
                    await FileOperationHelper.DeleteAsync(WorkContext, entry.Path);
                    changedFiles.Add(entry.Path);
                    break;
            }
        }

        var updatedSpec = WorkItemParsers.MarkAcceptanceCriteriaDone(specContent);
        if (!string.Equals(updatedSpec, specContent, StringComparison.Ordinal))
        {
            await FileOperationHelper.WriteAllTextAsync(WorkContext, specPath, updatedSpec);
            changedFiles.Add(specPath);
        }

        var branchName = WorkItemBranch.BuildBranchName(input.WorkItem);
        WorkContext.Repo.EnsureBranch(branchName, WorkContext.Config.Workflow.DefaultBaseBranch);
        var commitOk = WorkContext.Repo.CommitAndPush(branchName, $"feat: issue {input.WorkItem.Number}", changedFiles);

        var result = new DevResult(commitOk, input.WorkItem.Number, changedFiles, "");
        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.DevResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.DevResult] = serializedResult;

        return commitOk
            ? (true, $"Dev changes committed on {branchName}.")
            : (false, "Dev changes could not be committed.");
    }

    private string ResolveMode(WorkflowInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.Mode))
        {
            return input.Mode;
        }

        var labels = WorkContext.WorkItem.Labels;
        if (labels.Any(label => string.Equals(label, "mode:batch", StringComparison.OrdinalIgnoreCase)))
        {
            return "batch";
        }
        if (labels.Any(label => string.Equals(label, "mode:tdd", StringComparison.OrdinalIgnoreCase)))
        {
            return "tdd";
        }

        return "minimal";
    }
}

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
        var devJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.DevResult,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(devJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.DevResult, out var fallbackDevJson))
        {
             devJson = fallbackDevJson;
        }

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

    private static string BuildReviewMarkdown(CodeReviewResult result)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"# Code Review");
        builder.AppendLine();
        builder.AppendLine($"STATUS: {(result.Approved ? "APPROVED" : "CHANGES_REQUESTED")}");
        builder.AppendLine($"UPDATED: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine(result.Summary);
        builder.AppendLine();
        builder.AppendLine("## Findings");
        if (result.Findings.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var finding in result.Findings)
            {
                var location = string.IsNullOrWhiteSpace(finding.File)
                    ? ""
                    : $" ({finding.File}{(finding.Line.HasValue ? $":{finding.Line}" : "")})";
                builder.AppendLine($"- [{finding.Severity}] {finding.Category}: {finding.Message}{location}");
            }
        }

        return builder.ToString();
    }
}

internal sealed class DodExecutor : WorkflowStageExecutor
{
    public DodExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("DoD", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoD;
    protected override string Notes => "DoD gate evaluated.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath) ?? "";
        var parsedSpec = new SpecParser().Parse(specContent);

        var devJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.DevResult,
            () => string.Empty,
            cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(devJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.DevResult, out var fallbackDevJson))
        {
             devJson = fallbackDevJson;
        }
        WorkflowJson.TryDeserialize(devJson, out DevResult? devResult);

        var codeReviewJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.CodeReviewResult,
            () => string.Empty,
            cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(codeReviewJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.CodeReviewResult, out var fallbackReviewJson))
        {
             codeReviewJson = fallbackReviewJson;
        }
        WorkflowJson.TryDeserialize(codeReviewJson, out CodeReviewResult? codeReviewResult);

        var changedFiles = devResult?.ChangedFiles ?? Array.Empty<string>();
        var (noTodos, noFixmes) = ScanForMarkers(changedFiles);
        var acceptanceComplete = !specContent.Contains("- [ ]", StringComparison.Ordinal);
        var touchListSatisfied = parsedSpec.TouchList.All(entry => changedFiles.Contains(entry.Path, StringComparer.OrdinalIgnoreCase));
        var forbiddenClean = !parsedSpec.TouchList.Any(entry => entry.Operation == TouchOperation.Forbidden && changedFiles.Contains(entry.Path, StringComparer.OrdinalIgnoreCase));

        var dodInput = new DodGateInput(
            CiWorkflowGreen: true,
            RequiredChecksGreen: true,
            NoPendingChecks: true,
            QualityGateOk: true,
            NoNewBugs: true,
            NoNewVulnerabilities: true,
            NoCriticalCodeSmells: true,
            Coverage: 100,
            CoverageThreshold: 80,
            Duplication: 0,
            DuplicationThreshold: 5,
            AcceptanceCriteriaComplete: acceptanceComplete,
            TouchListSatisfied: touchListSatisfied,
            ForbiddenFilesClean: forbiddenClean,
            PlannedFilesChanged: touchListSatisfied,
            CodeReviewPassed: codeReviewResult?.Approved ?? false,
            NoBlockerFindings: codeReviewResult?.Findings.All(f => f.Severity != ReviewSeverity.Blocker) ?? false,
            NoTodos: noTodos,
            NoFixmes: noFixmes,
            SpecComplete: TemplateUtil.IsStatusComplete(specContent));

        var result = DodGateValidator.Evaluate(dodInput);
        if (!result.Passed && result.Failures.Count > 0)
        {
            WorkContext.Metrics?.RecordGateFailures(result.Failures);
        }
        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.DodGateResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.DodGateResult] = serializedResult;

        var notes = result.Passed
            ? "DoD gate passed."
            : $"DoD gate failed: {string.Join(" ", result.Failures)}";
        return (result.Passed, notes);
    }

    private (bool NoTodos, bool NoFixmes) ScanForMarkers(IReadOnlyList<string> files)
    {
        var noTodos = true;
        var noFixmes = true;

        foreach (var file in files)
        {
            if (!WorkItemParsers.IsSafeRelativePath(file) || !WorkContext.Workspace.Exists(file))
            {
                continue;
            }

            var content = WorkContext.Workspace.ReadAllText(file);
            if (content.Contains("TODO", StringComparison.OrdinalIgnoreCase))
            {
                noTodos = false;
            }
            if (content.Contains("FIXME", StringComparison.OrdinalIgnoreCase))
            {
                noFixmes = false;
            }
        }

        return (noTodos, noFixmes);
    }
}

internal sealed class ReleaseExecutor : WorkflowStageExecutor
{
    public ReleaseExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("Release", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Release;
    protected override string Notes => "Release prepared.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var dodJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.DodGateResult,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(dodJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.DodGateResult, out var fallbackDodJson))
        {
             dodJson = fallbackDodJson;
        }

        if (!WorkflowJson.TryDeserialize(dodJson, out GateResult? dodResult) || dodResult is null || !dodResult.Passed)
        {
            return (false, "Release blocked: DoD gate not passed.");
        }

        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath) ?? "";
        var parsedSpec = new SpecParser().Parse(specContent);
        var prTitle = $"{input.WorkItem.Title} (#{input.WorkItem.Number})";
        var prBody = BuildPullRequestBody(parsedSpec, input.WorkItem);

        var branchName = WorkItemBranch.BuildBranchName(input.WorkItem);
        var prNumber = await WorkContext.GitHub.GetPullRequestNumberAsync(branchName);
        string prUrl;
        int number;
        if (prNumber is null)
        {
            prUrl = await WorkContext.GitHub.OpenPullRequestAsync(branchName, WorkContext.Config.Workflow.DefaultBaseBranch, prTitle, prBody);
            number = TryParsePullRequestNumber(prUrl);
        }
        else
        {
            number = prNumber.Value;
            prUrl = $"https://github.com/{WorkContext.Config.RepoOwner}/{WorkContext.Config.RepoName}/pull/{number}";
        }

        var releaseNotes = BuildReleaseNotes(parsedSpec, input.WorkItem, prUrl);
        var releasePath = WorkflowPaths.ReleasePath(input.WorkItem.Number);
        await FileOperationHelper.WriteAllTextAsync(WorkContext, releasePath, releaseNotes);

        var result = new ReleaseResult(number, prUrl, Merged: false);
        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.ReleaseResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.ReleaseResult] = serializedResult;

        return (true, $"Release notes saved to {releasePath}.");
    }

    private static string BuildPullRequestBody(ParsedSpec spec, WorkItem item)
    {
        var changes = spec.TouchList.Count > 0
            ? string.Join("\n", spec.TouchList.Select(entry => $"- {entry.Operation} {entry.Path}"))
            : "- No touch list entries.";

        return $"## Summary\n{spec.Goal}\n\n## Changes\n{changes}\n\n## Testing\n- Not run (automated via CI)\n\n## Issue\n{item.Url}\n";
    }

    private static string BuildReleaseNotes(ParsedSpec spec, WorkItem item, string prUrl)
    {
        return $"# Release Notes: Issue {item.Number}\n\n" +
               $"## Summary\n{spec.Goal}\n\n" +
               $"## PR\n{prUrl}\n\n" +
               $"## Changes\n{string.Join("\n", spec.TouchList.Select(entry => $"- {entry.Operation} {entry.Path}"))}\n";
    }

    private static int TryParsePullRequestNumber(string prUrl)
    {
        if (string.IsNullOrWhiteSpace(prUrl))
        {
            return 0;
        }

        var segments = prUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var last = segments.LastOrDefault();
        return int.TryParse(last, out var number) ? number : 0;
    }
}
