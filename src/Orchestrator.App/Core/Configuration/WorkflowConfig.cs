namespace Orchestrator.App.Core.Configuration;

internal sealed record WorkflowConfig(
    string DefaultBaseBranch,
    int PollIntervalSeconds,
    int FastPollIntervalSeconds,
    bool UseWorkflowMode
);
