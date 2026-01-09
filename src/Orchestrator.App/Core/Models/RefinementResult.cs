namespace Orchestrator.App.Core.Models;

public sealed record AnsweredQuestion(
    int QuestionNumber,
    string Question,
    string Answer,
    string AnsweredBy
);

public sealed record OpenQuestion(
    int QuestionNumber,
    string Question
);

// Internal result from LLM parsing (questions as strings)
internal sealed record RefinementLlmResult(
    string ClarifiedStory,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> OpenQuestions,
    ComplexityIndicators Complexity
);

// Final result with stable question numbers
public sealed record RefinementResult(
    string ClarifiedStory,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<OpenQuestion> OpenQuestions,
    ComplexityIndicators Complexity,
    IReadOnlyList<AnsweredQuestion>? AnsweredQuestions = null
);
