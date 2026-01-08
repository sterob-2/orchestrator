namespace Orchestrator.App.Workflows;

internal sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly LabelSyncHandler _labelSync;
    private readonly HumanInLoopHandler _humanInLoop;
    private readonly IWorkflowCheckpointStore _checkpointStore;

    public WorkflowRunner(
        LabelSyncHandler labelSync,
        HumanInLoopHandler humanInLoop,
        IWorkflowCheckpointStore checkpointStore)
    {
        _labelSync = labelSync;
        _humanInLoop = humanInLoop;
        _checkpointStore = checkpointStore;
    }

    public async Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken)
    {
        var attempt = _checkpointStore.IncrementStage(context.WorkItem.Number, stage);
        var limit = MaxIterationsForStage(context.Config.Workflow, stage);
        if (attempt > limit)
        {
            var blocked = new WorkflowOutput(
                Success: false,
                Notes: $"Iteration limit reached for {stage} ({attempt}/{limit}).",
                NextStage: null);
            await _labelSync.ApplyAsync(context.WorkItem, blocked);
            await _humanInLoop.ApplyAsync(context.WorkItem, blocked);
            return blocked;
        }

        var workflow = WorkflowFactory.BuildGraph(stage);
        var input = new WorkflowInput(
            context.WorkItem,
            BuildProjectContext(context.Config),
            Mode: null,
            Attempt: attempt);

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input, stage);
        if (output != null)
        {
            await _labelSync.ApplyAsync(context.WorkItem, output);
            await _humanInLoop.ApplyAsync(context.WorkItem, output);
        }

        return output;
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
}
