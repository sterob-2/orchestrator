using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Core.Interfaces;

internal interface IGitHubClient
{
    Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50);
    Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber);
    Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body);
    Task<int?> GetPullRequestNumberAsync(string branchName);
    Task ClosePullRequestAsync(int prNumber);
    Task<IReadOnlyList<global::Orchestrator.App.IssueComment>> GetIssueCommentsAsync(int issueNumber);
    Task CommentOnWorkItemAsync(int issueNumber, string comment);
    Task AddLabelsAsync(int issueNumber, params string[] labels);
    Task RemoveLabelAsync(int issueNumber, string label);
    Task RemoveLabelsAsync(int issueNumber, params string[] labels);
    Task<string> GetPullRequestDiffAsync(int prNumber);
    Task CreateBranchAsync(string branchName);
    Task DeleteBranchAsync(string branchName);
    Task<bool> HasCommitsAsync(string baseBranch, string headBranch);
    Task<global::Orchestrator.App.RepoFile?> TryGetFileContentAsync(string branch, string path);
    Task CreateOrUpdateFileAsync(string branch, string path, string content, string message);
    Task<global::Orchestrator.App.ProjectSnapshot> GetProjectSnapshotAsync(
        string owner,
        int projectNumber,
        global::Orchestrator.App.ProjectOwnerType ownerType);
    Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName);
}
