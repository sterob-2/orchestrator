namespace Orchestrator.App.Workflows;

internal static class DodGateValidator
{
    public static GateResult Evaluate(DodGateInput input)
    {
        var failures = new List<string>();

        AddFailureIfFalse(failures, input.CiWorkflowGreen, "DoD-01: CI workflow is not green.");
        AddFailureIfFalse(failures, input.RequiredChecksGreen, "DoD-02: Required checks are not green.");
        AddFailureIfFalse(failures, input.NoPendingChecks, "DoD-03: Pending checks still exist.");
        AddFailureIfFalse(failures, input.QualityGateOk, "DoD-10: SonarQube quality gate not OK.");
        AddFailureIfFalse(failures, input.NoNewBugs, "DoD-11: New bugs detected.");
        AddFailureIfFalse(failures, input.NoNewVulnerabilities, "DoD-12: New vulnerabilities detected.");
        AddFailureIfFalse(failures, input.NoCriticalCodeSmells, "DoD-13: Critical code smells detected.");
        AddCoverageFailure(failures, input.Coverage, input.CoverageThreshold);
        AddDuplicationFailure(failures, input.Duplication, input.DuplicationThreshold);
        AddFailureIfFalse(failures, input.AcceptanceCriteriaComplete, "DoD-20: Acceptance criteria are not complete.");
        AddFailureIfFalse(failures, input.TouchListSatisfied, "DoD-21: Touch list requirements not satisfied.");
        AddFailureIfFalse(failures, input.ForbiddenFilesClean, "DoD-22: Forbidden files were modified.");
        AddFailureIfFalse(failures, input.PlannedFilesChanged, "DoD-23: Planned files were not fully changed.");
        AddFailureIfFalse(failures, input.CodeReviewPassed, "DoD-30: AI code review not passed.");
        AddFailureIfFalse(failures, input.NoBlockerFindings, "DoD-31: Blocker findings remain.");
        AddFailureIfFalse(failures, input.NoTodos, "DoD-40: TODO markers remain.");
        AddFailureIfFalse(failures, input.NoFixmes, "DoD-41: FIXME markers remain.");
        AddFailureIfFalse(failures, input.SpecComplete, "DoD-42: Spec status not complete.");

        return new GateResult(
            Passed: failures.Count == 0,
            Summary: failures.Count == 0 ? "DoD gate passed." : "DoD gate failed.",
            Failures: failures);
    }

    private static void AddFailureIfFalse(List<string> failures, bool condition, string message)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }

    private static void AddCoverageFailure(List<string> failures, double? coverage, double threshold)
    {
        if (!coverage.HasValue || coverage.Value < threshold)
        {
            failures.Add("DoD-14: Coverage below threshold.");
        }
    }

    private static void AddDuplicationFailure(List<string> failures, double? duplication, double threshold)
    {
        if (!duplication.HasValue || duplication.Value > threshold)
        {
            failures.Add("DoD-15: Duplication above threshold.");
        }
    }
}
