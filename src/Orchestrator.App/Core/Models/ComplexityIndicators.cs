namespace Orchestrator.App.Core.Models;

public sealed record ComplexityIndicators(
    IReadOnlyList<string> Signals,
    string? Summary
);
