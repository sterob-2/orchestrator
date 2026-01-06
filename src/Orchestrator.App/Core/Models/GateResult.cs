namespace Orchestrator.App.Core.Models;

internal sealed record GateResult(
    bool Passed,
    string Summary,
    IReadOnlyList<string> Failures
);
