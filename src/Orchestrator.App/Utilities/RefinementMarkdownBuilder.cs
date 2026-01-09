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

    public static void AppendOpenQuestions(StringBuilder content, IReadOnlyList<OpenQuestion> questions)
    {
        foreach (var question in questions)
        {
            content.AppendLine($"- [ ] **Question #{question.QuestionNumber}:** {question.Question}");
        }
        content.AppendLine();
    }

    public static void AppendAnsweredQuestions(StringBuilder content, IReadOnlyList<AnsweredQuestion> answeredQuestions)
    {
        if (answeredQuestions.Count == 0)
        {
            return;
        }

        content.AppendLine($"## Answered Questions ({answeredQuestions.Count})");
        content.AppendLine();
        foreach (var aq in answeredQuestions)
        {
            content.AppendLine($"### Question #{aq.QuestionNumber}");
            content.AppendLine($"**Question:** {aq.Question}");
            content.AppendLine();
            content.AppendLine($"**Answer (from {aq.AnsweredBy}):**");
            content.AppendLine(aq.Answer);
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
