namespace Orchestrator.App.Core.Models;

internal sealed record WorkflowInput(
    WorkItem WorkItem,
    ProjectContext ProjectContext,
    string? Mode,
    int Attempt
);
