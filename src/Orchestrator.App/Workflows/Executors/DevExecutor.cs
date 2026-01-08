using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

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
