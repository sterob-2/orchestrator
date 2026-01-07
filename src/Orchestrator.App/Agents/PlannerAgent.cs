namespace Orchestrator.App.Agents;

internal sealed class PlannerAgent : IRoleAgent
{
    public async Task<AgentResult> RunAsync(WorkContext ctx)
    {
        var notes = await PlannerPlanService.RunAsync(ctx);
        return AgentResult.Ok(notes);
    }
}
