namespace Orchestrator.App.Core.Configuration;

public sealed record WorkflowConfig(
    string DefaultBaseBranch,
    int MaxRefinementIterations,
    int MaxTechLeadIterations,
    int MaxDevIterations,
    int MaxCodeReviewIterations,
    int MaxDodIterations,
    int PollIntervalSeconds,
    int FastPollIntervalSeconds
);
