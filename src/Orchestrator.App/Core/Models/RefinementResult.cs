namespace Orchestrator.App.Core.Models;

public sealed record RefinementResult(
    string ClarifiedStory,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> OpenQuestions,
    ComplexityIndicators Complexity
);
