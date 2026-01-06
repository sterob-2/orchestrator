namespace Orchestrator.App.Core.Models;

internal sealed record ProjectContext(
    string RepoOwner,
    string RepoName,
    string DefaultBaseBranch,
    string WorkspacePath,
    string WorkspaceHostPath,
    string ProjectOwner,
    string ProjectOwnerType,
    int? ProjectNumber
);
