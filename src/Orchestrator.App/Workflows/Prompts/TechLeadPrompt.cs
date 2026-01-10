using System.Text;

namespace Orchestrator.App.Workflows;

internal static class TechLeadPrompt
{
    public static (string System, string User) Build(WorkItem item, Playbook playbook, string template)
    {
        var system = "You are a senior tech lead following MINIMAL FIRST architecture principles. " +
                     "CORE PRINCIPLES:\n" +
                     "1. SIMPLEST SOLUTION: Design the minimal implementation that satisfies requirements\n" +
                     "2. NO ABSTRACTIONS: Concrete implementations only. Add interfaces when 2nd implementation needed\n" +
                     "3. NO FUTURE-PROOFING: Hard-code sane defaults, no config for hypothetical scenarios\n" +
                     "4. FOLLOW EXISTING PATTERNS: Copy structure from similar code in codebase\n" +
                     "5. FILE SIZE LIMITS: Executors 200-400 LOC, Validators 100-200 LOC, Models 50-100 LOC\n\n" +
                     "Follow the provided spec template and playbook constraints. " +
                     "Do not add requirements beyond acceptance criteria. " +
                     "Output markdown only.";

        var builder = new StringBuilder();

        builder.AppendLine("ARCHITECTURE VISION:");
        builder.AppendLine("- Tech Stack: .NET 8, C# 12, xUnit, Moq (NO new frameworks without approval)");
        builder.AppendLine("- Design Patterns: Records for DTOs, Dependency Injection via constructor, Fail fast with exceptions");
        builder.AppendLine("- Anti-Patterns FORBIDDEN: God objects, premature abstraction, config overload, speculative features");
        builder.AppendLine("- Code Style: Minimal comments, no XML docs for internals, clear variable names over documentation");
        builder.AppendLine();
        builder.AppendLine("FOLDER STRUCTURE (Enforced):");
        builder.AppendLine("- Core/: Configuration, Models, Interfaces");
        builder.AppendLine("- Infrastructure/: Filesystem, Git, GitHub, Llm, Mcp");
        builder.AppendLine("- Workflows/: Executors, Gates, Prompts");
        builder.AppendLine("- Parsing/: Markdown parsers");
        builder.AppendLine("- Utilities/: Helpers (minimal)");
        builder.AppendLine();

        PromptBuilders.AppendIssueTitleAndBody(builder, item);
        builder.AppendLine("Playbook Constraints:");
        builder.AppendLine(RenderPlaybook(playbook));
        builder.AppendLine();
        builder.AppendLine("Spec Template:");
        builder.AppendLine(template);
        builder.AppendLine();
        builder.AppendLine("DESIGN CHECKLIST - Before finalizing spec:");
        builder.AppendLine("- Is this the SIMPLEST design that satisfies acceptance criteria?");
        builder.AppendLine("- Are you adding abstractions/interfaces? Remove unless 2+ implementations exist.");
        builder.AppendLine("- Are you adding config? Hard-code defaults unless environment-specific.");
        builder.AppendLine("- Did you copy patterns from existing similar code?");
        builder.AppendLine("- Will implementation fit file size limits? (Executors 200-400 LOC max)");
        builder.AppendLine("- IMPORTANT: If adding new files (Operation: Add in Touch List), reference at least one allowed framework ID (e.g., FW-01) and pattern ID (e.g., PAT-02) from the playbook in your spec.");
        builder.AppendLine();
        builder.AppendLine("Write the spec in markdown using the template sections. " +
                           "Include at least 3 Gherkin scenarios and a Touch List table with SPECIFIC file paths (not directories).");

        return (system, builder.ToString());
    }

    private static string RenderPlaybook(Playbook playbook)
    {
        return PromptBuilders.RenderPlaybook(playbook);
    }
}
