namespace Orchestrator.App.Core.Models;

public sealed record ReleaseResult(
    int PrNumber,
    string PrUrl,
    bool Merged
);
