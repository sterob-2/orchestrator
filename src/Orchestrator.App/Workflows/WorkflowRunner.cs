namespace Orchestrator.App.Workflows;

internal sealed class WorkflowRunner
{
    public async Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken)
    {
        var workflow = WorkflowFactory.Build(stage);
        var input = new WorkflowInput(
            context.WorkItem,
            BuildProjectContext(context.Config),
            Mode: null,
            Attempt: 0);

        return await SDLCWorkflow.RunWorkflowAsync(workflow, input);
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
