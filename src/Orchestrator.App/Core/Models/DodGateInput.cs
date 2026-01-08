namespace Orchestrator.App.Core.Models;

public sealed record DodGateInput(
    bool CiWorkflowGreen,
    bool RequiredChecksGreen,
    bool NoPendingChecks,
    bool QualityGateOk,
    bool NoNewBugs,
    bool NoNewVulnerabilities,
    bool NoCriticalCodeSmells,
    double? Coverage,
    double CoverageThreshold,
    double? Duplication,
    double DuplicationThreshold,
    bool AcceptanceCriteriaComplete,
    bool TouchListSatisfied,
    bool ForbiddenFilesClean,
    bool PlannedFilesChanged,
    bool CodeReviewPassed,
    bool NoBlockerFindings,
    bool NoTodos,
    bool NoFixmes,
    bool SpecComplete
);
