namespace Orchestrator.App.Workflows;

internal static class DorGateValidator
{
    private static readonly string[] TestableKeywords =
    [
        "given",
        "when",
        "then",
        "should",
        "must",
        "verify",
        "ensure"
    ];

    public static GateResult Evaluate(WorkItem item, RefinementResult refinement, LabelConfig labels)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            failures.Add("DoR-01: Title is required.");
        }

        var description = !string.IsNullOrWhiteSpace(refinement.ClarifiedStory) ? refinement.ClarifiedStory : item.Body;
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 50)
        {
            failures.Add("DoR-02: Description must be at least 50 characters.");
        }

        var acceptanceCriteria = refinement.AcceptanceCriteria ?? Array.Empty<string>();
        if (acceptanceCriteria.Count < 3)
        {
            failures.Add("DoR-03: At least 3 acceptance criteria are required.");
        }
        else
        {
            var untestable = acceptanceCriteria.Count(c => !IsTestableCriterion(c));
            if (untestable > 0)
            {
                failures.Add($"DoR-04: {untestable} acceptance criteria are not testable.");
            }
        }

        if (refinement.OpenQuestions.Count > 0)
        {
            failures.Add("DoR-05: Open questions must be resolved.");
        }

        if (!item.Labels.Any(label => label.StartsWith("estimate:", StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("DoR-06: Estimate label is missing.");
        }

        if (item.Labels.Any(label => string.Equals(label, labels.BlockedLabel, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add("DoR-07: Work item is blocked.");
        }

        return new GateResult(
            Passed: failures.Count == 0,
            Summary: failures.Count == 0 ? "DoR gate passed." : "DoR gate failed.",
            Failures: failures);
    }

    private static bool IsTestableCriterion(string criterion)
    {
        if (string.IsNullOrWhiteSpace(criterion))
        {
            return false;
        }

        foreach (var keyword in TestableKeywords)
        {
            if (criterion.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
