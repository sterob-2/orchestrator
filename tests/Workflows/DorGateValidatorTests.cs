using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class DorGateValidatorTests
{
    [Fact]
    public void Evaluate_ReturnsPass_WhenAllCriteriaMet()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(
            1,
            "Refine payment workflow",
            new string('a', 60),
            "https://example.com/issue/1",
            new List<string> { "estimate:3", config.Labels.WorkItemLabel });

        var refinement = new RefinementResult(
            "Clarified story that is sufficiently long to pass the Definition of Ready gate criteria and be valid.",
            new List<string>
            {
                "Given a user has an account, when they pay, then the balance updates.",
                "Given a valid card, when payment is submitted, then a receipt is shown.",
                "Given a failed payment, when retrying, then an error is surfaced."
            },
            new List<OpenQuestion>(),
            new ComplexityIndicators(new List<string>(), null));

        var result = DorGateValidator.Evaluate(workItem, refinement, config.Labels);

        Assert.True(result.Passed);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Evaluate_ReturnsFailures_WhenCriteriaMissing()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(
            2,
            "",
            "Too short",
            "https://example.com/issue/2",
            new List<string> { config.Labels.BlockedLabel });

        var refinement = new RefinementResult(
            "Clarified story",
            new List<string> { "Must handle errors." },
            new List<OpenQuestion> { new OpenQuestion(1, "Open question") },
            new ComplexityIndicators(new List<string>(), null));

        var result = DorGateValidator.Evaluate(workItem, refinement, config.Labels);

        Assert.False(result.Passed);
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoR-01", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoR-02", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoR-03", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoR-05", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoR-06", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoR-07", StringComparison.Ordinal));
    }
}
