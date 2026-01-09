namespace Orchestrator.App.Workflows;

internal static class WorkflowPaths
{
    public const string PlaybookPath = "docs/architecture-playbook.yaml";
    public const string SpecTemplatePath = "docs/templates/spec.md";
    public const string ReviewTemplatePath = "docs/templates/review.md";
    public const string MetricsPath = "orchestrator/metrics/workflow-metrics.jsonl";

    public static string SpecPath(int issueNumber) => $"orchestrator/specs/issue-{issueNumber}.md";
    public static string RefinementPath(int issueNumber) => $"orchestrator/refinement/issue-{issueNumber}.md";
    public static string DorResultPath(int issueNumber) => $"orchestrator/dor/issue-{issueNumber}.md";
    public static string QuestionsPath(int issueNumber) => $"orchestrator/questions/issue-{issueNumber}.md";
    public static string ReviewPath(int issueNumber) => $"orchestrator/reviews/issue-{issueNumber}.md";
    public static string ReleasePath(int issueNumber) => $"orchestrator/release/issue-{issueNumber}.md";
}
