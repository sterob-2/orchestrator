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
        cancellationToken.ThrowIfCancellationRequested();

        var workflow = WorkflowFactory.BuildGraph(context, stage);
        var input = new WorkflowInput(
            context.WorkItem,
            BuildProjectContext(context.Config),
            Mode: null,
            Attempt: 0);

        return await SDLCWorkflow.RunWorkflowAsync(
            workflow,
            input,
            async stageOutput =>
            {
                await _labelSync.ApplyAsync(context.WorkItem, stageOutput);
                if (!stageOutput.Success && stageOutput.NextStage is null)
                {
                    await _humanInLoop.ApplyAsync(context.WorkItem, stageOutput);
                }
            });
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
