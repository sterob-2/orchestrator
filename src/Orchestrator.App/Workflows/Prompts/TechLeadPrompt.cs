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
        PromptBuilders.AppendIssueTitleAndBody(builder, item);
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
        return PromptBuilders.RenderPlaybook(playbook);
    }
}
