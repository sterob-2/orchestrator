using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.App.Agents;

internal interface IRoleAgent
{
    Task<AgentResult> RunAsync(WorkContext ctx);
}

internal sealed record AgentResult(
    bool Success,
    string Notes,
    string? NextStageLabel = null,
    IReadOnlyList<string>? AddLabels = null,
    IReadOnlyList<string>? RemoveLabels = null)
{
    public static AgentResult Ok(string notes) => new(true, notes);
    public static AgentResult Fail(string notes) => new(false, notes);
}
