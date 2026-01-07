using System.Text;

namespace Orchestrator.App.Workflows;

internal sealed record WorkflowRunResult(bool Success, string Notes);

internal sealed class WorkflowRunner
{
    private readonly WorkflowFactory _factory;

    public WorkflowRunner(WorkflowFactory factory)
    {
        _factory = factory;
    }

    public async Task<WorkflowRunResult> RunAsync(WorkContext context, CancellationToken cancellationToken)
    {
        var stages = _factory.BuildDefaultStages(context);
        var notes = new StringBuilder();

        foreach (var stage in stages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await stage.RunAsync(context, cancellationToken);
            notes.AppendLine($"{stage.Name}: {result.Notes}");

            if (!result.Success)
            {
                return new WorkflowRunResult(false, notes.ToString());
            }
        }

        return new WorkflowRunResult(true, notes.ToString());
    }
}
