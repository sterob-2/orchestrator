using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

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
    protected override string Notes => "Branch created and ready for work.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var workItem = input.WorkItem;
        var branchName = WorkItemBranch.BuildBranchName(workItem);

        Logger.Info($"[ContextBuilder] Creating branch '{branchName}' for issue #{workItem.Number}");

        try
        {
            // Clean working tree before starting workflow (discard any uncommitted changes)
            Logger.Debug($"[ContextBuilder] Cleaning working tree");
            WorkContext.Repo.CleanWorkingTree();

            // Ensure branch exists and is checked out
            WorkContext.Repo.EnsureBranch(branchName, WorkContext.Config.Workflow.DefaultBaseBranch);
            Logger.Info($"[ContextBuilder] Branch '{branchName}' created and checked out");

            return (true, $"Branch '{branchName}' ready for work.");
        }
        catch (LibGit2Sharp.LibGit2SharpException ex)
        {
            Logger.Error($"[ContextBuilder] Git error creating branch: {ex.Message}");
            return (false, $"Failed to create branch: {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            Logger.Error($"[ContextBuilder] IO error creating branch: {ex.Message}");
            return (false, $"Failed to create branch: {ex.Message}");
        }
    }

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        if (!success)
        {
            return null; // Stop workflow if branch creation failed
        }

        if (_startOverride is not null)
        {
            return _startOverride;
        }

        return WorkflowStageGraph.StartStageFromLabels(_labels, input.WorkItem);
    }
}
