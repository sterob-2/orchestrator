using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

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
        var dodJson = await ReadStateWithFallbackAsync(
            context,
            WorkflowStateKeys.DodGateResult,
            cancellationToken);

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
