namespace Orchestrator.App.Core.Models;

public sealed record ParsedSpec(
    string Goal,
    string NonGoals,
    string Status,
    DateTime? Updated,
    IReadOnlyList<string> ArchitectureReferences,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Components,
    IReadOnlyList<TouchListEntry> TouchList,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> Scenarios,
    IReadOnlyList<string> Sequence,
    IReadOnlyList<string> TestMatrix,
    IReadOnlyDictionary<string, string> Sections
);
