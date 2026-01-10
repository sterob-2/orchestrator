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
        IReadOnlyList<AnsweredQuestion>? answeredQuestions = null)
    {
        var system = "You are an SDLC refinement assistant following the MINIMAL FIRST principle. " +
                     "CORE PRINCIPLES:\n" +
                     "1. START MINIMAL: Always propose the simplest solution that satisfies the requirement\n" +
                     "2. NO FUTURE-PROOFING: Do not add features, config, or abstractions for hypothetical scenarios\n" +
                     "3. ONE FEATURE PER ISSUE: If the issue mixes multiple concerns, ask user to split it\n" +
                     "4. MAX 3-5 ACCEPTANCE CRITERIA: More criteria = issue too large, should be split\n\n" +
                     "Do not invent requirements. " +
                     "Clarify ambiguity and produce structured JSON only. " +
                     "CRITICAL: All acceptance criteria MUST be testable using BDD format (Given/When/Then) or keywords (should, must, verify, ensure). " +
                     "Questions that have been answered are shown with [x] checkbox and their answers. " +
                     "Do NOT re-ask answered questions. Only generate new questions or questions that remain unanswered.";

        var builder = new StringBuilder();

        builder.AppendLine("PRODUCT VISION:");
        builder.AppendLine("- Quality over speed: Slow and correct beats fast and broken");
        builder.AppendLine("- Minimal viable first: Start with simplest solution, extend only when reviews request");
        builder.AppendLine("- Small focused issues: One feature, max 3-5 acceptance criteria");
        builder.AppendLine("- No over-engineering: No abstractions for single use, no speculative features");
        builder.AppendLine();

        PromptBuilders.AppendIssueTitleAndBody(builder, item);

        // Show answered questions history
        if (answeredQuestions != null && answeredQuestions.Count > 0)
        {
            builder.AppendLine("Previously Answered Questions (DO NOT re-ask these):");
            builder.AppendLine();
            foreach (var aq in answeredQuestions)
            {
                builder.AppendLine($"- [x] **Question #{aq.QuestionNumber}:** {aq.Question}");
                builder.AppendLine($"  **Answer ({aq.AnsweredBy}):** {aq.Answer}");
                builder.AppendLine();
            }
        }

        // Include previous refinement to avoid re-asking same questions
        if (!string.IsNullOrWhiteSpace(previousRefinement))
        {
            builder.AppendLine("Previous Refinement:");
            builder.AppendLine(previousRefinement);
            builder.AppendLine();
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
        builder.AppendLine("SCOPE CHECK - Before finalizing, verify:");
        builder.AppendLine("- Does this issue implement ONE clear feature? If not, suggest splitting.");
        builder.AppendLine("- Are there 3-5 acceptance criteria? More than 5 = too large, suggest splitting.");
        builder.AppendLine("- Are you adding config/features for 'what if' scenarios? Remove them.");
        builder.AppendLine("- Can the solution be simpler? If yes, simplify.");
        builder.AppendLine();
        builder.AppendLine("Return JSON with fields:");
        builder.AppendLine("{");
        builder.AppendLine("  \"clarifiedStory\": string,");
        builder.AppendLine("  \"acceptanceCriteria\": [string],");
        builder.AppendLine("  \"openQuestions\": [string],  // IMPORTANT: Do NOT include 'Question #X:' prefix - just the question text");
        builder.AppendLine("  \"complexitySignals\": [string],");
        builder.AppendLine("  \"complexitySummary\": string,");
        builder.AppendLine("  \"answeredQuestions\": [{ \"questionNumber\": int, \"question\": string, \"answer\": string, \"answeredBy\": string }] (optional)");
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
            new List<OpenQuestion> { new OpenQuestion(1, "Refinement output was invalid; please clarify requirements.") },
            new ComplexityIndicators(new List<string>(), null));
    }

    private static string RenderPlaybook(Playbook playbook)
    {
        return PromptBuilders.RenderPlaybook(playbook);
    }
}
