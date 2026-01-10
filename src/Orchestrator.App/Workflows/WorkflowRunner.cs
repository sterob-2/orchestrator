using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Workflows;

internal sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly LabelSyncHandler _labelSync;
    private readonly HumanInLoopHandler _humanInLoop;
    private readonly IWorkflowMetricsStore? _metricsStore;
    private readonly IWorkflowCheckpointStore _checkpointStore;

    public WorkflowRunner(
        LabelSyncHandler labelSync,
        HumanInLoopHandler humanInLoop,
        IWorkflowMetricsStore? metricsStore,
        IWorkflowCheckpointStore checkpointStore)
    {
        _labelSync = labelSync;
        _humanInLoop = humanInLoop;
        _metricsStore = metricsStore;
        _checkpointStore = checkpointStore;
    }

    public async Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Try to begin workflow - if already in progress, skip
        if (!_checkpointStore.TryBeginWorkflow(context.WorkItem.Number))
        {
            Logger.WriteLine($"[WorkflowRunner] Issue #{context.WorkItem.Number} already has a workflow in progress, skipping");
            return null;
        }

        try
        {
            var mode = ResolveMode(context.WorkItem);
            var recorder = _metricsStore is not null ? new WorkflowMetricsRecorder(_metricsStore, context.Config) : null;
            var runContext = context with { Metrics = recorder };

            recorder?.BeginRun(context.WorkItem, stage, mode);

            var attempt = _checkpointStore.IncrementStage(context.WorkItem.Number, stage);
            var limit = MaxIterationsForStage(context.Config.Workflow, stage);
            if (attempt > limit)
            {
                var blocked = new WorkflowOutput(
                    Success: false,
                    Notes: $"Iteration limit reached for {stage} ({attempt}/{limit}).",
                    NextStage: null);

                await _labelSync.ApplyAsync(runContext.WorkItem, blocked);
                await _humanInLoop.ApplyAsync(runContext.WorkItem, blocked);

                if (recorder is not null)
                {
                    var iterations = new Dictionary<WorkflowStage, int> { [stage] = attempt };
                    await recorder.CompleteRunAsync(
                        runContext.WorkItem,
                        stage,
                        mode,
                        success: false,
                        nextStage: null,
                        iterations,
                        cancellationToken);
                }

                return blocked;
            }

            // Use single-stage workflow for direct stage execution
            // Only use BuildGraph for ContextBuilder (which might route to other stages)
            var workflow = stage == WorkflowStage.ContextBuilder
                ? WorkflowFactory.BuildGraph(runContext, null)
                : WorkflowFactory.Build(stage, runContext);
            var input = new WorkflowInput(
                runContext.WorkItem,
                BuildProjectContext(runContext.Config),
                Mode: mode,
                Attempt: 0);

            var output = await SDLCWorkflow.RunWorkflowAsync(
                workflow,
                input,
                async stageOutput =>
                {
                    await _labelSync.ApplyAsync(runContext.WorkItem, stageOutput);
                    if (!stageOutput.Success && stageOutput.NextStage is null)
                    {
                        await _humanInLoop.ApplyAsync(runContext.WorkItem, stageOutput);
                    }
                });

            if (recorder is not null)
            {
                var iterations = new Dictionary<WorkflowStage, int> { [stage] = attempt };
                await recorder.CompleteRunAsync(
                    runContext.WorkItem,
                    stage,
                    mode,
                    output?.Success ?? false,
                    output?.NextStage,
                    iterations,
                    cancellationToken);
            }

            return output;
        }
        finally
        {
            // Always mark workflow as complete, even if it fails or is cancelled
            _checkpointStore.CompleteWorkflow(context.WorkItem.Number);
        }
    }

    private static ProjectContext BuildProjectContext(OrchestratorConfig cfg)
    {
        return new ProjectContext(
            cfg.RepoOwner,
            cfg.RepoName,
            cfg.Workflow.DefaultBaseBranch,
            cfg.WorkspacePath,
            cfg.WorkspaceHostPath,
            cfg.ProjectOwner,
            cfg.ProjectOwnerType,
            cfg.ProjectNumber
        );
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

    private static string ResolveMode(WorkItem item)
    {
        if (item.Labels.Any(label => string.Equals(label, "mode:batch", StringComparison.OrdinalIgnoreCase)))
        {
            return "batch";
        }

        if (item.Labels.Any(label => string.Equals(label, "mode:tdd", StringComparison.OrdinalIgnoreCase)))
        {
            return "tdd";
        }

        return "minimal";
    }
}
