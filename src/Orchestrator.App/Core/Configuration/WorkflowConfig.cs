namespace Orchestrator.App.Core.Configuration;

public sealed record WorkflowConfig(
    string DefaultBaseBranch,
    int PollIntervalSeconds,
    int FastPollIntervalSeconds,
    int MaxRefinementIterations,
    int MaxTechLeadIterations,
    int MaxDevIterations,
    int MaxCodeReviewIterations,
    int MaxDodIterations
);
