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
}
