using System.Threading.Tasks;

namespace Orchestrator.App.Agents;

internal sealed class ReviewAgent : IRoleAgent
{
    public Task<AgentResult> RunAsync(WorkContext ctx)
    {
        return Task.FromResult(AgentResult.Ok("ReviewAgent placeholder"));
    }
}
