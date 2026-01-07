namespace Orchestrator.App.Core.Configuration;

internal sealed record WorkflowConfig(
    string DefaultBaseBranch,
    int PollIntervalSeconds,
    int FastPollIntervalSeconds,
    int MaxRefinementIterations,
    int MaxTechLeadIterations,
    int MaxDevIterations,
    int MaxCodeReviewIterations,
    int MaxDodIterations
);
