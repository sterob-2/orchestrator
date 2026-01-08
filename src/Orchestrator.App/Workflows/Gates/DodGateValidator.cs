namespace Orchestrator.App.Workflows;

internal static class DodGateValidator
{
    public static GateResult Evaluate(DodGateInput input)
    {
        var failures = new List<string>();

        if (!input.CiWorkflowGreen)
        {
            failures.Add("DoD-01: CI workflow is not green.");
        }

        if (!input.RequiredChecksGreen)
        {
            failures.Add("DoD-02: Required checks are not green.");
        }

        if (!input.NoPendingChecks)
        {
            failures.Add("DoD-03: Pending checks still exist.");
        }

        if (!input.QualityGateOk)
        {
            failures.Add("DoD-10: SonarQube quality gate not OK.");
        }

        if (!input.NoNewBugs)
        {
            failures.Add("DoD-11: New bugs detected.");
        }

        if (!input.NoNewVulnerabilities)
        {
            failures.Add("DoD-12: New vulnerabilities detected.");
        }

        if (!input.NoCriticalCodeSmells)
        {
            failures.Add("DoD-13: Critical code smells detected.");
        }

        if (!input.Coverage.HasValue || input.Coverage.Value < input.CoverageThreshold)
        {
            failures.Add("DoD-14: Coverage below threshold.");
        }

        if (!input.Duplication.HasValue || input.Duplication.Value > input.DuplicationThreshold)
        {
            failures.Add("DoD-15: Duplication above threshold.");
        }

        if (!input.AcceptanceCriteriaComplete)
        {
            failures.Add("DoD-20: Acceptance criteria are not complete.");
        }

        if (!input.TouchListSatisfied)
        {
            failures.Add("DoD-21: Touch list requirements not satisfied.");
        }

        if (!input.ForbiddenFilesClean)
        {
            failures.Add("DoD-22: Forbidden files were modified.");
        }

        if (!input.PlannedFilesChanged)
        {
            failures.Add("DoD-23: Planned files were not fully changed.");
        }

        if (!input.CodeReviewPassed)
        {
            failures.Add("DoD-30: AI code review not passed.");
        }

        if (!input.NoBlockerFindings)
        {
            failures.Add("DoD-31: Blocker findings remain.");
        }

        if (!input.NoTodos)
        {
            failures.Add("DoD-40: TODO markers remain.");
        }

        if (!input.NoFixmes)
        {
            failures.Add("DoD-41: FIXME markers remain.");
        }

        if (!input.SpecComplete)
        {
            failures.Add("DoD-42: Spec status not complete.");
        }

        return new GateResult(
            Passed: failures.Count == 0,
            Summary: failures.Count == 0 ? "DoD gate passed." : "DoD gate failed.",
            Failures: failures);
    }
}
