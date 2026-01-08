namespace Orchestrator.App.Core.Models;

public sealed record TechLeadResult(
    string SpecPath,
    ParsedSpec ParsedSpec,
    IReadOnlyList<string> UsedFrameworks,
    IReadOnlyList<string> UsedPatterns
);
