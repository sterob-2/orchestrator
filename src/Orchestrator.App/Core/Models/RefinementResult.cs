namespace Orchestrator.App.Core.Models;

public sealed record AnsweredQuestion(
    int QuestionNumber,
    string Question,
    string Answer,
    string AnsweredBy
);

public sealed record RefinementResult(
    string ClarifiedStory,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> OpenQuestions,
    ComplexityIndicators Complexity,
    IReadOnlyList<AnsweredQuestion>? AnsweredQuestions = null
);
