using System.Text;

namespace Orchestrator.App.Workflows;

internal static class TechLeadPrompt
{
    public static (string System, string User) Build(WorkItem item, Playbook playbook, string template)
    {
        var system = "You are a senior tech lead. " +
                     "Follow the provided spec template and playbook constraints. " +
                     "Do not add requirements. Output markdown only.";

        var builder = new StringBuilder();
        builder.AppendLine("Issue Title:");
        builder.AppendLine(item.Title);
        builder.AppendLine();
        builder.AppendLine("Issue Body:");
        builder.AppendLine(item.Body);
        builder.AppendLine();
        builder.AppendLine("Playbook Constraints:");
        builder.AppendLine(RenderPlaybook(playbook));
        builder.AppendLine();
        builder.AppendLine("Spec Template:");
        builder.AppendLine(template);
        builder.AppendLine();
        builder.AppendLine("Write the spec in markdown using the template sections. " +
                           "Include at least 3 Gherkin scenarios and a Touch List table.");

        return (system, builder.ToString());
    }

    private static string RenderPlaybook(Playbook playbook)
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
}
