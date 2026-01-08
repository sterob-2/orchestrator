using System.Text;

namespace Orchestrator.App.Workflows;

internal static class CodeReviewPrompt
{
    public static (string System, string User) Build(WorkItem item, IReadOnlyList<string> changedFiles, string diffSummary)
    {
        var system = "You are an AI code reviewer. " +
                     "Focus on correctness, spec compliance, security, and tests. " +
                     "Return JSON only.";

        var builder = new StringBuilder();
        builder.AppendLine($"Issue: {item.Title} (#{item.Number})");
        builder.AppendLine();
        builder.AppendLine("Changed Files:");
        foreach (var file in changedFiles)
        {
            builder.AppendLine($"- {file}");
        }
        builder.AppendLine();
        builder.AppendLine("Diff Summary:");
        builder.AppendLine(diffSummary);
        builder.AppendLine();
        builder.AppendLine("Return JSON with fields:");
        builder.AppendLine("{");
        builder.AppendLine("  \"approved\": boolean,");
        builder.AppendLine("  \"summary\": string,");
        builder.AppendLine("  \"findings\": [");
        builder.AppendLine("    { \"severity\": \"BLOCKER|MAJOR|MINOR\", \"category\": string, \"message\": string, \"file\": string?, \"line\": number? }");
        builder.AppendLine("  ]");
        builder.AppendLine("}");

        return (system, builder.ToString());
    }
}
