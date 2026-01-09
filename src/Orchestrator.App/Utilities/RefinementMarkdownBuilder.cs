using System.Text;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Utilities;

/// <summary>
/// Helper for building markdown content from refinement results
/// </summary>
internal static class RefinementMarkdownBuilder
{
    public static void AppendClarifiedStory(StringBuilder content, string? clarifiedStory)
    {
        if (string.IsNullOrWhiteSpace(clarifiedStory))
        {
            return;
        }

        content.AppendLine("## Clarified Story");
        content.AppendLine();
        content.AppendLine(clarifiedStory);
        content.AppendLine();
    }

    public static void AppendAcceptanceCriteria(StringBuilder content, IReadOnlyList<string> criteria)
    {
        if (criteria.Count == 0)
        {
            return;
        }

        content.AppendLine($"## Acceptance Criteria ({criteria.Count})");
        content.AppendLine();
        foreach (var criterion in criteria)
        {
            content.AppendLine($"- {criterion}");
        }
        content.AppendLine();
    }

    public static void AppendQuestions(StringBuilder content,
        IReadOnlyList<OpenQuestion> openQuestions,
        IReadOnlyList<AnsweredQuestion>? answeredQuestions)
    {
        var allQuestions = new Dictionary<int, (string Question, string? Answer, string? AnsweredBy, bool IsAnswered)>();

        // Add open questions
        foreach (var q in openQuestions)
        {
            allQuestions[q.QuestionNumber] = (q.Question, null, null, false);
        }

        // Add answered questions
        if (answeredQuestions != null)
        {
            foreach (var aq in answeredQuestions)
            {
                allQuestions[aq.QuestionNumber] = (aq.Question, aq.Answer, aq.AnsweredBy, true);
            }
        }

        // Sort by question number
        var sortedQuestions = allQuestions.OrderBy(kvp => kvp.Key);

        foreach (var (number, (question, answer, answeredBy, isAnswered)) in sortedQuestions)
        {
            var checkbox = isAnswered ? "[x]" : "[ ]";
            content.AppendLine($"- {checkbox} **Question #{number}:** {question}");

            if (isAnswered && !string.IsNullOrEmpty(answer))
            {
                content.AppendLine($"  **Answer ({answeredBy}):** {answer}");
            }
            else
            {
                content.AppendLine($"  **Answer:** _[Pending]_");
            }

            content.AppendLine();
        }
    }

    public static void AppendAmbiguousQuestions(StringBuilder content, IReadOnlyList<OpenQuestion> ambiguousQuestions)
    {
        if (ambiguousQuestions.Count == 0)
        {
            return;
        }

        content.AppendLine($"## Ambiguous Questions ({ambiguousQuestions.Count})");
        content.AppendLine();
        content.AppendLine("**These questions require human clarification:**");
        content.AppendLine("They mix product and technical concerns and need stakeholder input to determine the correct approach.");
        content.AppendLine();
        content.AppendLine("**How to clarify:**");
        content.AppendLine("1. Add a comment to the GitHub issue with clarifications");
        content.AppendLine("2. Remove `blocked` and `user-review-required` labels");
        content.AppendLine("3. Add the `dor` label to re-trigger refinement");
        content.AppendLine();
        foreach (var question in ambiguousQuestions)
        {
            content.AppendLine($"- **Question #{question.QuestionNumber}:** {question.Question}");
        }
        content.AppendLine();
    }
}
