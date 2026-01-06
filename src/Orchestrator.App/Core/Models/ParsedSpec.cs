namespace Orchestrator.App.Core.Models;

internal sealed record ParsedSpec(
    string Goal,
    string NonGoals,
    IReadOnlyList<string> Components,
    IReadOnlyList<TouchListEntry> TouchList,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> Scenarios,
    IReadOnlyList<string> Sequence,
    IReadOnlyList<string> TestMatrix,
    IReadOnlyDictionary<string, string> Sections
);
