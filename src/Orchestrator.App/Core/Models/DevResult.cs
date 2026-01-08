namespace Orchestrator.App.Core.Models;

public sealed record DevResult(
    bool Success,
    int IssueNumber,
    IReadOnlyList<string> ChangedFiles,
    string CommitSha
);
