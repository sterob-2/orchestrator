using System.Diagnostics.CodeAnalysis;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Core.Interfaces;

internal interface IGitHubClient
{
    [ExcludeFromCodeCoverage]
    Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50);
    [ExcludeFromCodeCoverage]
    Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber);
    [ExcludeFromCodeCoverage]
    Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body);
    [ExcludeFromCodeCoverage]
    Task<int?> GetPullRequestNumberAsync(string branchName);
    [ExcludeFromCodeCoverage]
    Task ClosePullRequestAsync(int prNumber);
    [ExcludeFromCodeCoverage]
    Task<IReadOnlyList<global::Orchestrator.App.IssueComment>> GetIssueCommentsAsync(int issueNumber);
    [ExcludeFromCodeCoverage]
    Task CommentOnWorkItemAsync(int issueNumber, string comment);
    [ExcludeFromCodeCoverage]
    Task AddLabelsAsync(int issueNumber, params string[] labels);
    [ExcludeFromCodeCoverage]
    Task RemoveLabelAsync(int issueNumber, string label);
    [ExcludeFromCodeCoverage]
    Task RemoveLabelsAsync(int issueNumber, params string[] labels);
    [ExcludeFromCodeCoverage]
    Task<string> GetPullRequestDiffAsync(int prNumber);
    [ExcludeFromCodeCoverage]
    Task CreateBranchAsync(string branchName);
    [ExcludeFromCodeCoverage]
    Task DeleteBranchAsync(string branchName);
    [ExcludeFromCodeCoverage]
    Task<bool> HasCommitsAsync(string baseBranch, string headBranch);
    [ExcludeFromCodeCoverage]
    Task<global::Orchestrator.App.RepoFile?> TryGetFileContentAsync(string branch, string path);
    [ExcludeFromCodeCoverage]
    Task CreateOrUpdateFileAsync(string branch, string path, string content, string message);
    [ExcludeFromCodeCoverage]
    Task<global::Orchestrator.App.ProjectSnapshot> GetProjectSnapshotAsync(
        string owner,
        int projectNumber,
        global::Orchestrator.App.ProjectOwnerType ownerType);
    [ExcludeFromCodeCoverage]
    Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName);
}
