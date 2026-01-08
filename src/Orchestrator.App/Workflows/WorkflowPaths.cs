namespace Orchestrator.App.Workflows;

internal static class WorkflowPaths
{
    public const string PlaybookPath = "docs/architecture-playbook.yaml";
    public const string SpecTemplatePath = "docs/templates/spec.md";
    public const string PlanTemplatePath = "docs/templates/plan.md";
    public const string ReviewTemplatePath = "docs/templates/review.md";
    public const string QuestionsTemplatePath = "docs/templates/questions.md";

    public static string SpecPath(int issueNumber) => $"orchestrator/specs/issue-{issueNumber}.md";
    public static string QuestionsPath(int issueNumber) => $"orchestrator/questions/issue-{issueNumber}.md";
    public static string ReviewPath(int issueNumber) => $"orchestrator/reviews/issue-{issueNumber}.md";
    public static string ReleasePath(int issueNumber) => $"orchestrator/release/issue-{issueNumber}.md";
}
