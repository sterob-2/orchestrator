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
                     "CRITICAL: All acceptance criteria MUST be testable using BDD format (Given/When/Then) or keywords (should, must, verify, ensure). " +
                     "Write at least 3 specific, verifiable acceptance criteria. " +
                     "If previous refinement questions exist and issue comments contain answers, incorporate those answers and do NOT re-ask those questions. " +
                     "Only ask new questions or questions that remain unanswered.";

        var builder = new StringBuilder();
        PromptBuilders.AppendIssueTitleAndBody(builder, item);

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
        builder.AppendLine("IMPORTANT - Acceptance Criteria Requirements:");
        builder.AppendLine("- You MUST write at least 3 testable acceptance criteria");
        builder.AppendLine("- Each criterion MUST use BDD format or testable keywords");
        builder.AppendLine("- BDD format: 'Given [context], when [action], then [outcome]'");
        builder.AppendLine("- Testable keywords: 'should', 'must', 'verify', 'ensure', 'given', 'when', 'then'");
        builder.AppendLine("- Each criterion must be specific, verifiable, and testable");
        builder.AppendLine();
        builder.AppendLine("Examples of VALID acceptance criteria:");
        builder.AppendLine("  ✓ 'Given a user is logged in, when they click logout, then they should be redirected to the login page'");
        builder.AppendLine("  ✓ 'The system must validate email format before saving'");
        builder.AppendLine("  ✓ 'Should display error message when required fields are empty'");
        builder.AppendLine("  ✓ 'Given invalid credentials, when user attempts login, then access must be denied'");
        builder.AppendLine("  ✓ 'The API must return 401 status code for unauthorized requests'");
        builder.AppendLine();
        builder.AppendLine("Examples of INVALID acceptance criteria (will be rejected):");
        builder.AppendLine("  ✗ 'User can log out' (not testable - no verification criteria)");
        builder.AppendLine("  ✗ 'Good error handling' (vague, not verifiable)");
        builder.AppendLine("  ✗ 'Works correctly' (not specific)");
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
            new ComplexityIndicators(new List<string>(), null),
            new List<string>());
    }

    private static string RenderPlaybook(Playbook playbook)
    {
        return PromptBuilders.RenderPlaybook(playbook);
    }
}
