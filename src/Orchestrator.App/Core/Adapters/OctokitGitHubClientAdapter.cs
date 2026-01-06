using System.Linq;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Core.Adapters;

internal sealed class OctokitGitHubClientAdapter : IGitHubClient
{
    private readonly ILegacyGitHubClient _client;

    public OctokitGitHubClientAdapter(ILegacyGitHubClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50)
    {
        var items = await _client.GetOpenWorkItemsAsync(perPage);
        return items.Select(MapWorkItem).ToList();
    }

    public Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber)
    {
        return _client.GetIssueLabelsAsync(issueNumber);
    }

    public Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body)
    {
        return _client.OpenPullRequestAsync(headBranch, baseBranch, title, body);
    }

    public Task<int?> GetPullRequestNumberAsync(string branchName)
    {
        return _client.GetPullRequestNumberAsync(branchName);
    }

    public Task ClosePullRequestAsync(int prNumber)
    {
        return _client.ClosePullRequestAsync(prNumber);
    }

    public Task<IReadOnlyList<global::Orchestrator.App.IssueComment>> GetIssueCommentsAsync(int issueNumber)
    {
        return _client.GetIssueCommentsAsync(issueNumber);
    }

    public Task CommentOnWorkItemAsync(int issueNumber, string comment)
    {
        return _client.CommentOnWorkItemAsync(issueNumber, comment);
    }

    public Task AddLabelsAsync(int issueNumber, params string[] labels)
    {
        return _client.AddLabelsAsync(issueNumber, labels);
    }

    public Task RemoveLabelAsync(int issueNumber, string label)
    {
        return _client.RemoveLabelAsync(issueNumber, label);
    }

    public Task RemoveLabelsAsync(int issueNumber, params string[] labels)
    {
        return _client.RemoveLabelsAsync(issueNumber, labels);
    }

    public Task<string> GetPullRequestDiffAsync(int prNumber)
    {
        return _client.GetPullRequestDiffAsync(prNumber);
    }

    public Task CreateBranchAsync(string branchName)
    {
        return _client.CreateBranchAsync(branchName);
    }

    public Task DeleteBranchAsync(string branchName)
    {
        return _client.DeleteBranchAsync(branchName);
    }

    public Task<bool> HasCommitsAsync(string baseBranch, string headBranch)
    {
        return _client.HasCommitsAsync(baseBranch, headBranch);
    }

    public Task<global::Orchestrator.App.RepoFile?> TryGetFileContentAsync(string branch, string path)
    {
        return _client.TryGetFileContentAsync(branch, path);
    }

    public Task CreateOrUpdateFileAsync(string branch, string path, string content, string message)
    {
        return _client.CreateOrUpdateFileAsync(branch, path, content, message);
    }

    public Task<global::Orchestrator.App.ProjectSnapshot> GetProjectSnapshotAsync(
        string owner,
        int projectNumber,
        global::Orchestrator.App.ProjectOwnerType ownerType)
    {
        return _client.GetProjectSnapshotAsync(owner, projectNumber, ownerType);
    }

    public Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName)
    {
        return _client.UpdateProjectItemStatusAsync(owner, projectNumber, issueNumber, statusName);
    }

    private static WorkItem MapWorkItem(global::Orchestrator.App.WorkItem item)
    {
        return new WorkItem(item.Number, item.Title, item.Body, item.Url, item.Labels);
    }
}
