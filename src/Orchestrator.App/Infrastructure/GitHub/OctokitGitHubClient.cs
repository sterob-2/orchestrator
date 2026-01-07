using Octokit;
using Orchestrator.App.Core.Configuration;
using System.Net.Http;

namespace Orchestrator.App.Infrastructure.GitHub;

/// <summary>
/// GitHub client using Octokit.NET for REST API and GraphQL operations
/// </summary>
internal sealed class OctokitGitHubClient
{
    private readonly Octokit.GitHubClient _octokit;
    private readonly OrchestratorConfig _cfg;
    private readonly HttpClient _http;

    private string RepoOwner => _cfg.RepoOwner;
    private string RepoName => _cfg.RepoName;

    public OctokitGitHubClient(OrchestratorConfig cfg) : this(cfg, CreateDefaultGitHubClient(cfg), new HttpClient())
    {
    }

    internal OctokitGitHubClient(OrchestratorConfig cfg, Octokit.GitHubClient octokitClient, HttpClient httpClient)
    {
        _cfg = cfg;
        _octokit = octokitClient;
        _http = httpClient;

        // Configure HttpClient for GraphQL
        _http.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("conjunction-orchestrator", "0.3"));
        _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
        {
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.GitHubToken);
        }
    }

    private static Octokit.GitHubClient CreateDefaultGitHubClient(OrchestratorConfig cfg)
    {
        var client = new Octokit.GitHubClient(new ProductHeaderValue("conjunction-orchestrator", "0.3"));

        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
        {
            client.Credentials = new Credentials(cfg.GitHubToken);
        }

        return client;
    }

    // ========================================
    // PHASE 1: Core REST API (Octokit)
    // ========================================

    /// <summary>
    /// Get open issues for the repository
    /// </summary>
    public async Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50)
    {
        var issueRequest = new RepositoryIssueRequest
        {
            State = ItemStateFilter.Open
        };

        var apiOptions = new ApiOptions
        {
            PageSize = perPage,
            PageCount = 1
        };

        var issues = await _octokit.Issue.GetAllForRepository(
            RepoOwner,
            RepoName,
            issueRequest,
            apiOptions
        );

        // Convert Octokit Issues to WorkItems, excluding PRs
        return issues
            .Where(i => i.PullRequest == null)
            .Select(i => new WorkItem(
                Number: i.Number,
                Title: i.Title,
                Body: i.Body ?? string.Empty,
                Url: i.HtmlUrl,
                Labels: i.Labels.Select(l => l.Name).ToList()
            ))
            .ToList();
    }

    /// <summary>
    /// Get labels for a specific issue
    /// </summary>
    public async Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber)
    {
        var issue = await _octokit.Issue.Get(RepoOwner, RepoName, issueNumber);
        return issue.Labels.Select(l => l.Name).ToList();
    }

    /// <summary>
    /// Create a pull request
    /// </summary>
    public async Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body)
    {
        var newPr = new NewPullRequest(title, headBranch, baseBranch)
        {
            Body = body
        };

        var pr = await _octokit.PullRequest.Create(RepoOwner, RepoName, newPr);
        return pr.HtmlUrl;
    }

    /// <summary>
    /// Get PR number for a branch, or null if not found
    /// </summary>
    public async Task<int?> GetPullRequestNumberAsync(string branchName)
    {
        var prs = await _octokit.PullRequest.GetAllForRepository(
            RepoOwner,
            RepoName,
            new PullRequestRequest { State = ItemStateFilter.All }
        );

        var pr = prs.FirstOrDefault(p => p.Head.Ref == branchName);
        return pr?.Number;
    }

    /// <summary>
    /// Close a pull request
    /// </summary>
    public async Task ClosePullRequestAsync(int prNumber)
    {
        var update = new PullRequestUpdate
        {
            State = ItemState.Closed
        };

        await _octokit.PullRequest.Update(RepoOwner, RepoName, prNumber, update);
    }

    /// <summary>
    /// Get comments on an issue
    /// </summary>
    public async Task<IReadOnlyList<Core.Models.IssueComment>> GetIssueCommentsAsync(int issueNumber)
    {
        var comments = await _octokit.Issue.Comment.GetAllForIssue(
            RepoOwner,
            RepoName,
            issueNumber
        );

        return comments.Select(c => new Core.Models.IssueComment(
            Author: c.User.Login,
            Body: c.Body
        )).ToList();
    }

    /// <summary>
    /// Post a comment on an issue
    /// </summary>
    public async Task CommentOnWorkItemAsync(int issueNumber, string comment)
    {
        await _octokit.Issue.Comment.Create(
            RepoOwner,
            RepoName,
            issueNumber,
            comment
        );
    }

    /// <summary>
    /// Add labels to an issue
    /// </summary>
    public async Task AddLabelsAsync(int issueNumber, params string[] labels)
    {
        if (labels.Length == 0) return;

        await _octokit.Issue.Labels.AddToIssue(
            RepoOwner,
            RepoName,
            issueNumber,
            labels
        );
    }

    /// <summary>
    /// Remove a label from an issue
    /// </summary>
    public async Task RemoveLabelAsync(int issueNumber, string label)
    {
        try
        {
            await _octokit.Issue.Labels.RemoveFromIssue(
                RepoOwner,
                RepoName,
                issueNumber,
                label
            );
        }
        catch (NotFoundException)
        {
            // Label doesn't exist on issue - ignore
        }
    }

    /// <summary>
    /// Remove multiple labels from an issue
    /// </summary>
    public async Task RemoveLabelsAsync(int issueNumber, params string[] labels)
    {
        foreach (var label in labels)
        {
            await RemoveLabelAsync(issueNumber, label);
        }
    }

    /// <summary>
    /// Get PR diff
    /// </summary>
    public async Task<string> GetPullRequestDiffAsync(int prNumber)
    {
        // Get files changed in the PR
        var files = await _octokit.PullRequest.Files(RepoOwner, RepoName, prNumber);

        // Build a simplified diff from file changes
        var diff = new System.Text.StringBuilder();
        foreach (var file in files)
        {
            diff.AppendLine($"diff --git a/{file.FileName} b/{file.FileName}");
            diff.AppendLine($"--- a/{file.FileName}");
            diff.AppendLine($"+++ b/{file.FileName}");
            diff.AppendLine(file.Patch);
            diff.AppendLine();
        }

        return diff.ToString();
    }

    // ========================================
    // PHASE 3a: Git Operations (Octokit)
    // ========================================

    /// <summary>
    /// Create a branch
    /// </summary>
    public async Task CreateBranchAsync(string branchName)
    {
        var baseBranch = await _octokit.Git.Reference.Get(RepoOwner, RepoName, $"heads/{_cfg.Workflow.DefaultBaseBranch}");

        try
        {
            await _octokit.Git.Reference.Create(
                RepoOwner,
                RepoName,
                new NewReference($"refs/heads/{branchName}", baseBranch.Object.Sha)
            );
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            // Branch already exists - ignore
        }
    }

    /// <summary>
    /// Delete a branch
    /// </summary>
    public async Task DeleteBranchAsync(string branchName)
    {
        try
        {
            await _octokit.Git.Reference.Delete(RepoOwner, RepoName, $"heads/{branchName}");
        }
        catch (NotFoundException)
        {
            // Branch doesn't exist - ignore
        }
    }

    /// <summary>
    /// Check if there are commits between base and head branches
    /// </summary>
    public async Task<bool> HasCommitsAsync(string baseBranch, string headBranch)
    {
        var comparison = await _octokit.Repository.Commit.Compare(RepoOwner, RepoName, baseBranch, headBranch);
        return comparison.AheadBy > 0;
    }

    /// <summary>
    /// Get file content from a branch
    /// </summary>
    public async Task<RepoFile?> TryGetFileContentAsync(string branch, string path)
    {
        try
        {
            var contents = await _octokit.Repository.Content.GetAllContentsByRef(RepoOwner, RepoName, path, branch);
            if (contents.Count == 0)
            {
                return null;
            }

            var file = contents[0];
            return new RepoFile(path, file.Content, file.Sha);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Create or update a file on a branch
    /// </summary>
    public async Task CreateOrUpdateFileAsync(string branch, string path, string content, string message)
    {
        var existing = await TryGetFileContentAsync(branch, path);

        if (existing != null)
        {
            await _octokit.Repository.Content.UpdateFile(
                RepoOwner,
                RepoName,
                path,
                new UpdateFileRequest(message, content, existing.Sha, branch)
            );
        }
        else
        {
            await _octokit.Repository.Content.CreateFile(
                RepoOwner,
                RepoName,
                path,
                new CreateFileRequest(message, content, branch)
            );
        }
    }

    // ========================================
    // PHASE 3b: GraphQL Projects V2 (Raw GraphQL)
    // ========================================

    /// <summary>
    /// Get GitHub Projects V2 snapshot
    /// </summary>
    public async Task<ProjectSnapshot> GetProjectSnapshotAsync(string owner, int projectNumber, ProjectOwnerType ownerType)
    {
        var query = @"
query($login: String!, $number: Int!) {
  user(login: $login) {
    projectV2(number: $number) {
      ...ProjectFields
    }
  }
  organization(login: $login) {
    projectV2(number: $number) {
      ...ProjectFields
    }
  }
}

fragment ProjectFields on ProjectV2 {
  title
  items(first: 100) {
    nodes {
      content {
        ... on Issue { number title url }
        ... on DraftIssue { title }
      }
      fieldValues(first: 20) {
        nodes {
          ... on ProjectV2ItemFieldSingleSelectValue {
            name
            field { ... on ProjectV2SingleSelectField { name } }
          }
        }
      }
    }
  }
}";

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            query,
            variables = new { login = owner, number = projectNumber }
        });

        var res = await _http.PostAsync(
            "https://api.github.com/graphql",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        );
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
        }

        var doc = System.Text.Json.JsonDocument.Parse(txt);
        var project = ResolveProjectElement(doc);
        var title = project.GetProperty("title").GetString() ?? "";
        var items = new List<ProjectItem>();

        foreach (var node in project.GetProperty("items").GetProperty("nodes").EnumerateArray())
        {
            string? itemTitle = null;
            int? issueNumber = null;
            string? itemUrl = null;

            if (node.TryGetProperty("content", out var content) && content.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                if (content.TryGetProperty("title", out var ctTitle))
                    itemTitle = ctTitle.GetString();
                if (content.TryGetProperty("number", out var ctNumber))
                    issueNumber = ctNumber.GetInt32();
                if (content.TryGetProperty("url", out var ctUrl))
                    itemUrl = ctUrl.GetString();
            }

            var status = "Unknown";
            if (node.TryGetProperty("fieldValues", out var fields))
            {
                foreach (var fieldNode in fields.GetProperty("nodes").EnumerateArray())
                {
                    if (!fieldNode.TryGetProperty("field", out var field))
                        continue;

                    var fieldName = field.GetProperty("name").GetString();
                    if (!string.Equals(fieldName, "Status", StringComparison.OrdinalIgnoreCase))
                        continue;

                    status = fieldNode.GetProperty("name").GetString() ?? status;
                    break;
                }
            }

            items.Add(new ProjectItem(itemTitle ?? "(untitled)", issueNumber, itemUrl, status));
        }

        return new ProjectSnapshot(owner, projectNumber, ownerType, title, items);
    }

    /// <summary>
    /// Update project item status in GitHub Projects V2
    /// </summary>
    public async Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName)
    {
        var metadata = await GetProjectMetadataAsync(owner, projectNumber);
        var item = metadata.Items.FirstOrDefault(i => i.IssueNumber == issueNumber);
        if (item == null)
        {
            Logger.WriteLine($"Project item not found for issue #{issueNumber}.");
            return;
        }

        if (!metadata.StatusOptions.TryGetValue(statusName, out var optionId))
        {
            Logger.WriteLine($"Status option '{statusName}' not found.");
            return;
        }

        var mutation = @"
mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId
    itemId: $itemId
    fieldId: $fieldId
    value: { singleSelectOptionId: $optionId }
  }) {
    projectV2Item { id }
  }
}";

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            query = mutation,
            variables = new
            {
                projectId = metadata.ProjectId,
                itemId = item.ItemId,
                fieldId = metadata.StatusFieldId,
                optionId
            }
        });

        var res = await _http.PostAsync(
            "https://api.github.com/graphql",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        );
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Project status update failed: {res.StatusCode} {txt}");
        }
    }

    private async Task<ProjectMetadata> GetProjectMetadataAsync(string owner, int projectNumber)
    {
        var query = @"
query($login: String!, $number: Int!) {
  user(login: $login) {
    projectV2(number: $number) {
      id
      fields(first: 50) {
        nodes {
          ... on ProjectV2SingleSelectField {
            id
            name
            options { id name }
          }
        }
      }
      items(first: 100) {
        nodes {
          id
          content {
            ... on Issue { number }
          }
        }
      }
    }
  }
  organization(login: $login) {
    projectV2(number: $number) {
      id
      fields(first: 50) {
        nodes {
          ... on ProjectV2SingleSelectField {
            id
            name
            options { id name }
          }
        }
      }
      items(first: 100) {
        nodes {
          id
          content {
            ... on Issue { number }
          }
        }
      }
    }
  }
}";

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            query,
            variables = new { login = owner, number = projectNumber }
        });

        var res = await _http.PostAsync(
            "https://api.github.com/graphql",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        );
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
        }

        var doc = System.Text.Json.JsonDocument.Parse(txt);
        var project = ResolveProjectElement(doc);

        var projectId = project.GetProperty("id").GetString() ?? "";
        var statusFieldId = "";
        var statusOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!project.TryGetProperty("fields", out var fields) || fields.ValueKind == System.Text.Json.JsonValueKind.Null)
        {
            throw new InvalidOperationException("Project metadata missing 'fields'.");
        }

        if (!fields.TryGetProperty("nodes", out var fieldNodes) || fieldNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            throw new InvalidOperationException("Project metadata missing 'fields.nodes'.");
        }

        foreach (var field in fieldNodes.EnumerateArray())
        {
            if (!field.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            if (!string.Equals(name, "Status", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!field.TryGetProperty("id", out var idElement))
            {
                continue;
            }

            statusFieldId = idElement.GetString() ?? "";
            if (!field.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (!option.TryGetProperty("name", out var optionNameElement))
                {
                    continue;
                }

                if (!option.TryGetProperty("id", out var optionIdElement))
                {
                    continue;
                }

                var optionName = optionNameElement.GetString() ?? "";
                var optionId = optionIdElement.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(optionName) && !string.IsNullOrWhiteSpace(optionId))
                {
                    statusOptions[optionName] = optionId;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(statusFieldId))
        {
            throw new InvalidOperationException("Status field not found in project.");
        }

        if (!project.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind == System.Text.Json.JsonValueKind.Null)
        {
            throw new InvalidOperationException("Project metadata missing 'items'.");
        }

        if (!itemsElement.TryGetProperty("nodes", out var itemNodes) || itemNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            throw new InvalidOperationException("Project metadata missing 'items.nodes'.");
        }

        var items = new List<ProjectItemRef>();
        foreach (var node in itemNodes.EnumerateArray())
        {
            if (!node.TryGetProperty("content", out var content) || content.ValueKind == System.Text.Json.JsonValueKind.Null)
                continue;

            if (!content.TryGetProperty("number", out var numberProp))
                continue;

            var issueNumber = numberProp.GetInt32();
            var itemId = node.GetProperty("id").GetString() ?? "";
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                items.Add(new ProjectItemRef(itemId, issueNumber));
            }
        }

        return new ProjectMetadata(projectId, statusFieldId, statusOptions, items);
    }

    private static System.Text.Json.JsonElement ResolveProjectElement(System.Text.Json.JsonDocument doc)
    {
        var data = doc.RootElement.GetProperty("data");
        if (data.TryGetProperty("user", out var user) && user.ValueKind != System.Text.Json.JsonValueKind.Null &&
            user.TryGetProperty("projectV2", out var userProject) && userProject.ValueKind != System.Text.Json.JsonValueKind.Null)
        {
            return userProject;
        }

        if (data.TryGetProperty("organization", out var org) && org.ValueKind != System.Text.Json.JsonValueKind.Null &&
            org.TryGetProperty("projectV2", out var orgProject) && orgProject.ValueKind != System.Text.Json.JsonValueKind.Null)
        {
            return orgProject;
        }

        var errors = TryFormatGraphQlErrors(doc);
        if (!string.IsNullOrWhiteSpace(errors))
        {
            throw new InvalidOperationException($"Project not found. GraphQL errors: {errors}");
        }

        throw new InvalidOperationException("Project not found.");
    }

    private static string? TryFormatGraphQlErrors(System.Text.Json.JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("errors", out var errors) || errors.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return null;
        }

        var messages = new List<string>();
        foreach (var error in errors.EnumerateArray())
        {
            if (error.TryGetProperty("message", out var message))
            {
                var text = message.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    messages.Add(text);
                }
            }
        }

        return messages.Count > 0 ? string.Join(" | ", messages) : null;
    }
}
