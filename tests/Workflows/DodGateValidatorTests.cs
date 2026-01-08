namespace Orchestrator.App.Tests.Workflows;

public class DodGateValidatorTests
{
    [Fact]
    public void Evaluate_ReturnsPass_WhenAllCriteriaMet()
    {
        var input = new DodGateInput(
            CiWorkflowGreen: true,
            RequiredChecksGreen: true,
            NoPendingChecks: true,
            QualityGateOk: true,
            NoNewBugs: true,
            NoNewVulnerabilities: true,
            NoCriticalCodeSmells: true,
            Coverage: 90,
            CoverageThreshold: 80,
            Duplication: 1,
            DuplicationThreshold: 5,
            AcceptanceCriteriaComplete: true,
            TouchListSatisfied: true,
            ForbiddenFilesClean: true,
            PlannedFilesChanged: true,
            CodeReviewPassed: true,
            NoBlockerFindings: true,
            NoTodos: true,
            NoFixmes: true,
            SpecComplete: true);

        var result = DodGateValidator.Evaluate(input);

        Assert.True(result.Passed);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Evaluate_ReturnsFailures_WhenThresholdsMissing()
    {
        var input = new DodGateInput(
            CiWorkflowGreen: true,
            RequiredChecksGreen: true,
            NoPendingChecks: false,
            QualityGateOk: true,
            NoNewBugs: true,
            NoNewVulnerabilities: true,
            NoCriticalCodeSmells: true,
            Coverage: null,
            CoverageThreshold: 80,
            Duplication: 6,
            DuplicationThreshold: 5,
            AcceptanceCriteriaComplete: true,
            TouchListSatisfied: true,
            ForbiddenFilesClean: true,
            PlannedFilesChanged: true,
            CodeReviewPassed: true,
            NoBlockerFindings: true,
            NoTodos: true,
            NoFixmes: true,
            SpecComplete: true);

        var result = DodGateValidator.Evaluate(input);

        Assert.False(result.Passed);
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoD-03", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoD-14", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("DoD-15", StringComparison.Ordinal));
    }
}
