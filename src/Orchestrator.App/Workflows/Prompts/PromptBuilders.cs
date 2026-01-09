using System.Text;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows;

/// <summary>
/// Shared prompt building utilities to reduce code duplication
/// </summary>
internal static class PromptBuilders
{
    /// <summary>
    /// Appends issue title and body in standard format
    /// </summary>
    public static void AppendIssueTitleAndBody(StringBuilder builder, WorkItem workItem)
    {
        builder.AppendLine("Issue Title:");
        builder.AppendLine(workItem.Title);
        builder.AppendLine();
        builder.AppendLine("Issue Body:");
        builder.AppendLine(workItem.Body);
        builder.AppendLine();
    }

    /// <summary>
    /// Appends standard issue context including title, body, clarified story, and acceptance criteria
    /// </summary>
    public static void AppendIssueContext(
        StringBuilder builder,
        WorkItem workItem,
        RefinementResult? refinement = null,
        bool includeBody = true)
    {
        builder.AppendLine("Issue Context:");
        builder.AppendLine($"Title: {workItem.Title}");

        if (includeBody)
        {
            builder.AppendLine($"Body: {workItem.Body}");
        }

        builder.AppendLine();

        if (refinement != null)
        {
            builder.AppendLine("Clarified Story:");
            builder.AppendLine(refinement.ClarifiedStory);
            builder.AppendLine();
            builder.AppendLine("Acceptance Criteria:");
            foreach (var criterion in refinement.AcceptanceCriteria)
            {
                builder.AppendLine($"- {criterion}");
            }
            builder.AppendLine();
        }
    }

    /// <summary>
    /// Renders playbook constraints (frameworks and patterns)
    /// </summary>
    public static string RenderPlaybook(Playbook playbook)
    {
        if (playbook.AllowedFrameworks.Count == 0 &&
            playbook.AllowedPatterns.Count == 0 &&
            playbook.ForbiddenFrameworks.Count == 0 &&
            playbook.ForbiddenPatterns.Count == 0)
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

        if (playbook.ForbiddenFrameworks.Count > 0)
        {
            builder.AppendLine("- Forbidden Frameworks:");
            foreach (var framework in playbook.ForbiddenFrameworks)
            {
                builder.AppendLine($"  - {framework.Name}");
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

        if (playbook.ForbiddenPatterns.Count > 0)
        {
            builder.AppendLine("- Forbidden Patterns:");
            foreach (var pattern in playbook.ForbiddenPatterns)
            {
                builder.AppendLine($"  - {pattern.Name} ({pattern.Id})");
            }
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Appends playbook constraints to the prompt builder
    /// </summary>
    public static void AppendPlaybookConstraints(StringBuilder builder, Playbook playbook)
    {
        builder.AppendLine("Playbook Constraints:");

        if (playbook.AllowedFrameworks.Count > 0)
        {
            builder.AppendLine("Allowed Frameworks:");
            foreach (var framework in playbook.AllowedFrameworks)
            {
                builder.AppendLine($"- {framework.Name} ({framework.Id}) version {framework.Version}");
            }
        }

        if (playbook.AllowedPatterns.Count > 0)
        {
            builder.AppendLine("Allowed Patterns:");
            foreach (var pattern in playbook.AllowedPatterns)
            {
                builder.AppendLine($"- {pattern.Name} ({pattern.Id}): {pattern.Reference}");
            }
        }

        if (playbook.ForbiddenPatterns.Count > 0)
        {
            builder.AppendLine("Forbidden Patterns:");
            foreach (var pattern in playbook.ForbiddenPatterns)
            {
                builder.AppendLine($"- {pattern.Name} ({pattern.Id})");
            }
        }

        builder.AppendLine();
    }

    /// <summary>
    /// Appends JSON schema instructions for question/answer/reasoning format
    /// Used by ProductOwner and TechnicalAdvisor executors
    /// </summary>
    public static void AppendQuestionAnswerSchema(StringBuilder builder)
    {
        builder.AppendLine("Return JSON:");
        builder.AppendLine("{");
        builder.AppendLine("  \"question\": string (repeat the question),");
        builder.AppendLine("  \"answer\": string (your answer to the question),");
        builder.AppendLine("  \"reasoning\": string (brief explanation or 'CANNOT_ANSWER' if unsure)");
        builder.AppendLine("}");
    }

    /// <summary>
    /// Appends JSON schema instructions for question classification format
    /// Used by QuestionClassifier executor
    /// </summary>
    public static void AppendQuestionClassificationSchema(StringBuilder builder)
    {
        builder.AppendLine("Return JSON:");
        builder.AppendLine("{");
        builder.AppendLine("  \"question\": string (the question being classified),");
        builder.AppendLine("  \"type\": \"Technical\" | \"Product\" | \"Ambiguous\",");
        builder.AppendLine("  \"reasoning\": string (brief explanation of classification)");
        builder.AppendLine("}");
    }

    /// <summary>
    /// Appends classification guidelines for distinguishing Technical vs Product vs Ambiguous questions
    /// Used by QuestionClassifier executor
    /// </summary>
    public static void AppendClassificationGuidelines(StringBuilder builder)
    {
        builder.AppendLine("Classification Guidelines:");
        builder.AppendLine();
        builder.AppendLine("TECHNICAL questions are about:");
        builder.AppendLine("- Implementation details (how to code something)");
        builder.AppendLine("- Architecture decisions (which pattern, structure)");
        builder.AppendLine("- Framework/library choices (which tool to use)");
        builder.AppendLine("- Code organization (where to put files)");
        builder.AppendLine("- Error handling strategies");
        builder.AppendLine("- Performance considerations");
        builder.AppendLine("Examples: 'Which framework?', 'How should errors be handled?', 'What's the API structure?'");
        builder.AppendLine();
        builder.AppendLine("PRODUCT questions are about:");
        builder.AppendLine("- User workflows (how users interact)");
        builder.AppendLine("- Business logic (what should happen)");
        builder.AppendLine("- Use cases (when/why feature is used)");
        builder.AppendLine("- Requirements clarification (what exactly is needed)");
        builder.AppendLine("- User expectations (what users see/experience)");
        builder.AppendLine("- Feature behavior (how feature should work)");
        builder.AppendLine("Examples: 'What happens when user clicks X?', 'What's the expected behavior?', 'Which users can access this?'");
        builder.AppendLine();
        builder.AppendLine("AMBIGUOUS questions:");
        builder.AppendLine("- Can't be clearly classified");
        builder.AppendLine("- Require human judgment");
        builder.AppendLine("- Mix technical and product concerns");
        builder.AppendLine();
    }
}
