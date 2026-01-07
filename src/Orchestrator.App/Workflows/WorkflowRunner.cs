namespace Orchestrator.App.Workflows;

internal sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly LabelSyncHandler _labelSync;
    private readonly HumanInLoopHandler _humanInLoop;

    public WorkflowRunner(LabelSyncHandler labelSync, HumanInLoopHandler humanInLoop)
    {
        _labelSync = labelSync;
        _humanInLoop = humanInLoop;
    }

    public async Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken)
    {
        var workflow = WorkflowFactory.Build(stage);
        var input = new WorkflowInput(
            context.WorkItem,
            BuildProjectContext(context.Config),
            Mode: null,
            Attempt: 0);

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);
        if (output != null)
        {
            await _labelSync.ApplyAsync(context.WorkItem, output);
            await _humanInLoop.ApplyAsync(context.WorkItem, output);
        }

        return output;
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
