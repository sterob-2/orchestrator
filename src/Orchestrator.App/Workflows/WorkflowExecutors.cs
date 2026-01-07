using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Agents;

namespace Orchestrator.App.Workflows;

/// <summary>
/// Output message from executors
/// </summary>
internal sealed record WorkflowOutput(
    bool Success,
    string Notes,
    string? NextStage = null
);

/// <summary>
/// Planner executor - creates initial plan from GitHub issue
/// </summary>
internal sealed class PlannerExecutor : Executor<WorkflowInput, WorkflowOutput>
{
    private readonly WorkContext _context;

    public PlannerExecutor(WorkContext context) : base("Planner")
    {
        _context = context;
    }

    public override async ValueTask<WorkflowOutput> HandleAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var execContext = _context with { WorkItem = input.WorkItem };
        var notes = await PlannerPlanService.RunAsync(execContext);
        return new WorkflowOutput(
            Success: true,
            Notes: notes,
            NextStage: "TechLead"
        );
    }
}
