namespace Orchestrator.App.Core.Models;

internal sealed record ComplexityIndicators(
    IReadOnlyList<string> Signals,
    string? Summary
);
