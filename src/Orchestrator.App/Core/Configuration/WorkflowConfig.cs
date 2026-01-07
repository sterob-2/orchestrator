namespace Orchestrator.App.Core.Configuration;

public sealed record WorkflowConfig(
    string DefaultBaseBranch,
    int PollIntervalSeconds,
    int FastPollIntervalSeconds,
    bool UseWorkflowMode
);
