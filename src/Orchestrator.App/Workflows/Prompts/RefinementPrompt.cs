using System.Text;
using Orchestrator.App.Parsing;

namespace Orchestrator.App.Workflows;

internal static class RefinementPrompt
{
    public static (string System, string User) Build(
        WorkItem item,
        Playbook playbook,
        string? existingSpec,
        string? previousRefinement = null,
        IReadOnlyList<IssueComment>? comments = null)
    {
        var system = "You are an SDLC refinement assistant. " +
                     "Do not invent requirements. " +
                     "Clarify ambiguity and produce structured JSON only. " +
                     "If previous refinement questions exist and issue comments contain answers, incorporate those answers and do NOT re-ask those questions. " +
                     "Only ask new questions or questions that remain unanswered.";

        var builder = new StringBuilder();
        builder.AppendLine("Issue Title:");
        builder.AppendLine(item.Title);
        builder.AppendLine();
        builder.AppendLine("Issue Body:");
        builder.AppendLine(item.Body);
        builder.AppendLine();

        // Include previous refinement to avoid re-asking same questions
        if (!string.IsNullOrWhiteSpace(previousRefinement))
        {
            builder.AppendLine("Previous Refinement (contains questions previously asked):");
            builder.AppendLine(previousRefinement);
            builder.AppendLine();
        }

        // Include issue comments where answers might be
        if (comments != null && comments.Count > 0)
        {
            builder.AppendLine("Issue Comments (may contain answers to questions):");
            foreach (var comment in comments)
            {
                builder.AppendLine($"--- Comment by {comment.Author} ---");
                builder.AppendLine(comment.Body);
                builder.AppendLine();
            }
        }

        builder.AppendLine("Playbook Constraints:");
        builder.AppendLine(RenderPlaybook(playbook));
        builder.AppendLine();
        builder.AppendLine("Existing Spec (if any):");
        builder.AppendLine(string.IsNullOrWhiteSpace(existingSpec) ? "None" : existingSpec);
        builder.AppendLine();
        builder.AppendLine("Return JSON with fields:");
        builder.AppendLine("{");
        builder.AppendLine("  \"clarifiedStory\": string,");
        builder.AppendLine("  \"acceptanceCriteria\": [string],");
        builder.AppendLine("  \"openQuestions\": [string],");
        builder.AppendLine("  \"complexitySignals\": [string],");
        builder.AppendLine("  \"complexitySummary\": string");
        builder.AppendLine("}");

        return (system, builder.ToString());
    }

    public static RefinementResult Fallback(WorkItem item)
    {
        var acceptanceCriteria = WorkItemParsers.TryParseAcceptanceCriteria(item.Body);
        var clarifiedStory = string.IsNullOrWhiteSpace(item.Body) ? item.Title : item.Body;
        return new RefinementResult(
            clarifiedStory,
            acceptanceCriteria,
            new List<string> { "Refinement output was invalid; please clarify requirements." },
            new ComplexityIndicators(new List<string>(), null));
    }

    private static string RenderPlaybook(Playbook playbook)
    {
        if (playbook.AllowedFrameworks.Count == 0 && playbook.AllowedPatterns.Count == 0)
        {
            return "None";
        }

        var builder = new StringBuilder();
        if (playbook.AllowedFrameworks.Count > 0)
        {
            builder.AppendLine("- Allowed Frameworks:");
            foreach (var framework in playbook.AllowedFrameworks)
            {
                builder.AppendLine($"  - {framework.Name} ({framework.Id})");
            }
        }
        if (playbook.AllowedPatterns.Count > 0)
        {
            builder.AppendLine("- Allowed Patterns:");
            foreach (var pattern in playbook.AllowedPatterns)
            {
                builder.AppendLine($"  - {pattern.Name} ({pattern.Id})");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
