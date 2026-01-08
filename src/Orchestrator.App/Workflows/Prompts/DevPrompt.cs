using System.Text;

namespace Orchestrator.App.Workflows;

internal static class DevPrompt
{
    public static (string System, string User) Build(
        string mode,
        ParsedSpec spec,
        TouchListEntry entry,
        string? existingContent)
    {
        var system = "You are a software engineer implementing a spec. " +
                     "Follow the spec and touch list strictly. " +
                     "Output the full file content only.";

        var builder = new StringBuilder();
        builder.AppendLine($"Mode: {mode}");
        builder.AppendLine();
        builder.AppendLine("Spec Goal:");
        builder.AppendLine(spec.Goal);
        builder.AppendLine();
        builder.AppendLine("Touch List Entry:");
        builder.AppendLine($"{entry.Operation} {entry.Path} {entry.Notes}");
        builder.AppendLine();
        builder.AppendLine("Interfaces:");
        builder.AppendLine(string.Join("\n", spec.Interfaces));
        builder.AppendLine();
        builder.AppendLine("Scenarios:");
        builder.AppendLine(string.Join("\n\n", spec.Scenarios));
        builder.AppendLine();
        builder.AppendLine("Sequence:");
        builder.AppendLine(string.Join("\n", spec.Sequence));
        builder.AppendLine();
        builder.AppendLine("Test Matrix:");
        builder.AppendLine(string.Join("\n", spec.TestMatrix));
        builder.AppendLine();
        builder.AppendLine("Current File Content:");
        builder.AppendLine(string.IsNullOrWhiteSpace(existingContent) ? "<empty>" : existingContent);

        return (system, builder.ToString());
    }
}
