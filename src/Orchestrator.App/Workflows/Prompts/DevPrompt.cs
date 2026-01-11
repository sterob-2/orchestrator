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
                     "CRITICAL INSTRUCTIONS:\n" +
                     "1. Read the Touch List Entry to understand what operation to perform\n" +
                     "2. Study the Interfaces section which shows the required changes (before/after examples)\n" +
                     "3. Apply those exact changes to the Current File Content\n" +
                     "4. For 'Modify' operations: update/remove code as specified in the notes\n" +
                     "5. When removing code: COMPLETELY OMIT it from your output - do NOT include it with comments\n" +
                     "6. Output ONLY the complete updated file content\n" +
                     "7. Do NOT include before/after comments or explanations\n" +
                     "8. Do NOT preserve code marked for removal\n" +
                     "9. VERIFY your output does not contain any code that should be removed\n" +
                     "Follow the spec strictly. Code removal means the code must be absent from your output.";

        var builder = new StringBuilder();
        builder.AppendLine($"Mode: {mode}");
        builder.AppendLine();
        builder.AppendLine("Spec Goal:");
        builder.AppendLine(spec.Goal);
        builder.AppendLine();
        builder.AppendLine("=== TOUCH LIST ENTRY ===");
        builder.AppendLine($"Operation: {entry.Operation}");
        builder.AppendLine($"File: {entry.Path}");
        builder.AppendLine($"Instructions: {entry.Notes}");
        builder.AppendLine();
        builder.AppendLine("=== REQUIRED CHANGES (Before/After Examples) ===");
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
        builder.AppendLine("=== CURRENT FILE CONTENT (TO BE MODIFIED) ===");
        builder.AppendLine(string.IsNullOrWhiteSpace(existingContent) ? "<empty>" : existingContent);
        builder.AppendLine();
        builder.AppendLine("=== EXAMPLE: HOW TO REMOVE CODE ===");
        builder.AppendLine("If the instruction says 'Remove CreateBranchAsync method':");
        builder.AppendLine();
        builder.AppendLine("BEFORE (current file content):");
        builder.AppendLine("  Task<bool> CreateBranchAsync(string branchName, string baseBranch);");
        builder.AppendLine("  Task DeleteFileAsync(string path);");
        builder.AppendLine();
        builder.AppendLine("AFTER (your output should NOT include CreateBranchAsync):");
        builder.AppendLine("  Task DeleteFileAsync(string path);");
        builder.AppendLine();
        builder.AppendLine("CRITICAL: When removing code:");
        builder.AppendLine("- DO NOT include removed methods/classes/properties in your output");
        builder.AppendLine("- DO NOT add comments like '// removed' or '// deleted'");
        builder.AppendLine("- Simply omit the code completely from your response");
        builder.AppendLine();
        builder.AppendLine("=== YOUR TASK ===");
        builder.AppendLine("Apply the changes shown in 'REQUIRED CHANGES' section to the 'CURRENT FILE CONTENT'.");
        builder.AppendLine($"Follow the instructions: {entry.Notes}");
        builder.AppendLine("Output the complete updated file content below:");

        return (system, builder.ToString());
    }
}
