# System Prompt

You are an AI code reviewer. Focus on correctness, spec compliance, security, and tests. Return JSON only.

# User Prompt

Issue: Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient (#43)

Changed Files:

Diff Summary:
diff --git a/orchestrator/prompts/issue-43-CodeReview-attempt-1-response.md b/orchestrator/prompts/issue-43-CodeReview-attempt-1-response.md
--- a/orchestrator/prompts/issue-43-CodeReview-attempt-1-response.md
+++ b/orchestrator/prompts/issue-43-CodeReview-attempt-1-response.md
@@ -0,0 +1,62 @@
+{
+  "approved": false,
+  "summary": "The PR removes CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and deletes their implementations from OctokitGitHubClient. The changes are appropriate for eliminating dead code, and because the interface is internal the risk of breaking external consumers is reduced. However, I cannot approve merging yet because there is no evidence of CI/build/test verification, no repo-wide search/report showing zero remaining references, and no note about release/compatibility considerations. Please run a full build and test, perform a repo-wide search for the removed symbols, and document the change (or deprecate first if this is a published API).",
+  "findings": [
+    {
+      "severity": "MAJOR",
+      "category": "BUILD_AND_TEST_VERIFICATION",
+      "message": "No CI/build/test output is attached to the PR. Removing interface members and implementations can introduce compile or test failures if callers, mocks, or tests reference them. Run 'dotnet build' and the full test suite (dotnet test / xUnit) and attach the results before merging.",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MAJOR",
+      "category": "COMPILATION_IMPACT",
+      "message": "Removal of interface members requires verifying that all implementations, test doubles, and any reflection-based code do not reference the deleted members. Ensure there are no remaining compile-time references (calls, explicit interface implementations, or mock setups).",
+      "file": "src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs",
+      "line": 244
+    },
+    {
+      "severity": "MAJOR",
+      "category": "POTENTIAL_BREAKING_CHANGE",
+      "message": "IGitHubClient is declared internal in the repo, which reduces the risk of breaking external consumers. However, confirm that the assembly is not packaged/published or referenced externally (e.g., via InternalsVisibleTo, internal test projects, or a NuGet package). If the interface is part of a public package, follow a deprecation plan or schedule a major-version bump.",
+      "file": "src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs",
+      "line": 1
+    },
+    {
+      "severity": "MINOR",
+      "category": "TESTS",
+      "message": "The diff doesn't show updates to tests beyond the code changes. Search the tests/ tree for 'CreateBranchAsync' and 'DeleteBranchAsync' and remove or update any references. Also ensure mock setups (Moq) and test fixtures were updated accordingly.",
+      "file": "tests/",
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "DOCUMENTATION",
+      "message": "Update any developer docs, README, or changelog entries to reflect the removal of these methods. If consumers exist, add migration guidance (e.g., use RepoGit.EnsureBranch and CommitAndPush instead).",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "CLEANUP",
+      "message": "Run a repository-wide text search to ensure there are no stray references to the removed methods and remove any now-orphaned helper code or unused using directives introduced by the change.",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "REPO_CONTENT_REVIEW",
+      "message": "This PR adds a large number of 'orchestrator/prompts' files (various attempts/responses). Confirm those files are intended to be part of this change and not accidentally committed generated prompt artifacts.",
+      "file": "orchestrator/prompts/",
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "SECURITY",
+      "message": "No direct security issue is apparent from removing these methods. Verify any audit/authorization expectations previously handled by the deleted API methods are preserved in the remaining flows so callers don't implement insecure workarounds.",
+      "file": null,
+      "line": null
+    }
+  ]
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-CodeReview-attempt-1.md b/orchestrator/prompts/issue-43-CodeReview-attempt-1.md
--- a/orchestrator/prompts/issue-43-CodeReview-attempt-1.md
+++ b/orchestrator/prompts/issue-43-CodeReview-attempt-1.md


diff --git a/orchestrator/prompts/issue-43-CodeReview-attempt-2-response.md b/orchestrator/prompts/issue-43-CodeReview-attempt-2-response.md
--- a/orchestrator/prompts/issue-43-CodeReview-attempt-2-response.md
+++ b/orchestrator/prompts/issue-43-CodeReview-attempt-2-response.md
@@ -0,0 +1,55 @@
+{
+  "approved": false,
+  "summary": "The PR removes CreateBranchAsync and DeleteBranchAsync from the public IGitHubClient interface. This is a breaking public API change that must be handled carefully: ensure all implementations and callers are updated, tests and docs adjusted, and the package versioning/migration guidance updated before merging.",
+  "findings": [
+    {
+      "severity": "MAJOR",
+      "category": "API breaking change",
+      "message": "Removing public methods from IGitHubClient is a breaking change for consumers. If this library is published, this must be surfaced via a major version bump and a migration guide. Consider deprecating first (Obsolete attribute) before removal to give consumers time to adapt.",
+      "file": "src/.../IGitHubClient.cs",
+      "line": null
+    },
+    {
+      "severity": "MAJOR",
+      "category": "Compatibility / Build",
+      "message": "All types that implement IGitHubClient will need to be updated (or they will fail to compile). Ensure every implementation in the repo and any internal/partner implementations are changed prior to merging.",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MAJOR",
+      "category": "Usage",
+      "message": "Any consumer code that calls CreateBranchAsync/DeleteBranchAsync must be found and updated. Run a full repository/global search to confirm there are no remaining callers. If these methods provided functionality consumers rely on, supply a clear alternative or instructions.",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "Tests",
+      "message": "Unit/integration tests that referenced these methods need to be updated or removed. Ensure CI runs and test coverage remains adequate for branch-related behaviors (if applicable).",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "Documentation",
+      "message": "Update public documentation (README, API docs, changelog) to reflect that branch creation/deletion are no longer part of IGitHubClient and document the recommended alternative.",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "Security",
+      "message": "Removing branch-create/delete APIs may change the attack surface and the authorization model. Verify that any authorization checks previously performed by those methods are either moved to the new location or are no longer required, and ensure logging/audit behavior is preserved if necessary.",
+      "file": null,
+      "line": null
+    },
+    {
+      "severity": "MINOR",
+      "category": "Release process",
+      "message": "Ensure the change is recorded in the changelog and consumers are informed (breaking change). If the project maintains semantic versioning, prepare a major version release for this PR.",
+      "file": null,
+      "line": null
+    }
+  ]
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-CodeReview-attempt-2.md b/orchestrator/prompts/issue-43-CodeReview-attempt-2.md
--- a/orchestrator/prompts/issue-43-CodeReview-attempt-2.md
+++ b/orchestrator/prompts/issue-43-CodeReview-attempt-2.md
@@ -0,0 +1,21 @@
+# System Prompt
+
+You are an AI code reviewer. Focus on correctness, spec compliance, security, and tests. Return JSON only.
+
+# User Prompt
+
+Issue: Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient (#43)
+
+Changed Files:
+
+Diff Summary:
+
+
+Return JSON with fields:
+{
+  "approved": boolean,
+  "summary": string,
+  "findings": [
+    { "severity": "BLOCKER|MAJOR|MINOR", "category": string, "message": string, "file": string?, "line": number? }
+  ]
+}

diff --git a/orchestrator/prompts/issue-43-Dev-attempt-1-response.md b/orchestrator/prompts/issue-43-Dev-attempt-1-response.md
--- a/orchestrator/prompts/issue-43-Dev-attempt-1-response.md
+++ b/orchestrator/prompts/issue-43-Dev-attempt-1-response.md
@@ -0,0 +1,669 @@
+using System.Net.Http;
+using Octokit;
+using Orchestrator.App.Core.Configuration;
+using Orchestrator.App.Core.Models;
+using CoreIssueComment = Orchestrator.App.Core.Models.IssueComment;
+
+namespace Orchestrator.App.Infrastructure.GitHub;
+
+/// <summary>
+/// GitHub client using Octokit.NET for REST API and GraphQL operations
+/// </summary>
+internal sealed class OctokitGitHubClient : Orchestrator.App.Core.Interfaces.IGitHubClient
+{
+    private readonly Octokit.GitHubClient _octokit;
+    private readonly OrchestratorConfig _cfg;
+    private readonly HttpClient _http;
+
+    private string RepoOwner => _cfg.RepoOwner;
+    private string RepoName => _cfg.RepoName;
+
+    public OctokitGitHubClient(OrchestratorConfig cfg) : this(cfg, CreateDefaultGitHubClient(cfg), new HttpClient())
+    {
+    }
+
+    internal OctokitGitHubClient(OrchestratorConfig cfg, Octokit.GitHubClient octokitClient, HttpClient httpClient)
+    {
+        _cfg = cfg;
+        _octokit = octokitClient;
+        _http = httpClient;
+
+        // Configure HttpClient for GraphQL
+        _http.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("conjunction-orchestrator", "0.3"));
+        _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.GitHubToken);
+        }
+    }
+
+    private static Octokit.GitHubClient CreateDefaultGitHubClient(OrchestratorConfig cfg)
+    {
+        var client = new Octokit.GitHubClient(new ProductHeaderValue("conjunction-orchestrator", "0.3"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            client.Credentials = new Credentials(cfg.GitHubToken);
+        }
+
+        return client;
+    }
+
+    // ========================================
+    // PHASE 1: Core REST API (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Get open issues for the repository
+    /// </summary>
+    public async Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50)
+    {
+        var issueRequest = new RepositoryIssueRequest
+        {
+            State = ItemStateFilter.Open
+        };
+
+        var apiOptions = new ApiOptions
+        {
+            PageSize = perPage,
+            PageCount = 1
+        };
+
+        var issues = await _octokit.Issue.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            issueRequest,
+            apiOptions
+        );
+
+        // Convert Octokit Issues to WorkItems, excluding PRs
+        return issues
+            .Where(i => i.PullRequest == null)
+            .Select(i => new WorkItem(
+                Number: i.Number,
+                Title: i.Title,
+                Body: i.Body ?? string.Empty,
+                Url: i.HtmlUrl,
+                Labels: i.Labels.Select(l => l.Name).ToList()
+            ))
+            .ToList();
+    }
+
+    /// <summary>
+    /// Get labels for a specific issue
+    /// </summary>
+    public async Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber)
+    {
+        var issue = await _octokit.Issue.Get(RepoOwner, RepoName, issueNumber);
+        return issue.Labels.Select(l => l.Name).ToList();
+    }
+
+    /// <summary>
+    /// Create a pull request
+    /// </summary>
+    public async Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body)
+    {
+        var newPr = new NewPullRequest(title, headBranch, baseBranch)
+        {
+            Body = body
+        };
+
+        var pr = await _octokit.PullRequest.Create(RepoOwner, RepoName, newPr);
+        return pr.HtmlUrl;
+    }
+
+    /// <summary>
+    /// Get PR number for a branch, or null if not found
+    /// </summary>
+    public async Task<int?> GetPullRequestNumberAsync(string branchName)
+    {
+        var prs = await _octokit.PullRequest.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            new PullRequestRequest { State = ItemStateFilter.All }
+        );
+
+        var pr = prs.FirstOrDefault(p => p.Head.Ref == branchName);
+        return pr?.Number;
+    }
+
+    /// <summary>
+    /// Close a pull request
+    /// </summary>
+    public async Task ClosePullRequestAsync(int prNumber)
+    {
+        var update = new PullRequestUpdate
+        {
+            State = ItemState.Closed
+        };
+
+        await _octokit.PullRequest.Update(RepoOwner, RepoName, prNumber, update);
+    }
+
+    /// <summary>
+    /// Get comments on an issue
+    /// </summary>
+    public async Task<IReadOnlyList<CoreIssueComment>> GetIssueCommentsAsync(int issueNumber)
+    {
+        var comments = await _octokit.Issue.Comment.GetAllForIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber
+        );
+
+        return comments.Select(c => new CoreIssueComment(
+            Author: c.User.Login,
+            Body: c.Body
+        )).ToList();
+    }
+
+    /// <summary>
+    /// Post a comment on an issue
+    /// </summary>
+    public async Task CommentOnWorkItemAsync(int issueNumber, string comment)
+    {
+        await _octokit.Issue.Comment.Create(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            comment
+        );
+    }
+
+    /// <summary>
+    /// Add labels to an issue
+    /// </summary>
+    public async Task AddLabelsAsync(int issueNumber, params string[] labels)
+    {
+        if (labels.Length == 0)
+            return;
+
+        await _octokit.Issue.Labels.AddToIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            labels
+        );
+    }
+
+    /// <summary>
+    /// Remove a label from an issue
+    /// </summary>
+    public async Task RemoveLabelAsync(int issueNumber, string label)
+    {
+        try
+        {
+            await _octokit.Issue.Labels.RemoveFromIssue(
+                RepoOwner,
+                RepoName,
+                issueNumber,
+                label
+            );
+        }
+        catch (NotFoundException)
+        {
+            // Label doesn't exist on issue - ignore
+        }
+    }
+
+    /// <summary>
+    /// Remove multiple labels from an issue
+    /// </summary>
+    public async Task RemoveLabelsAsync(int issueNumber, params string[] labels)
+    {
+        foreach (var label in labels)
+        {
+            await RemoveLabelAsync(issueNumber, label);
+        }
+    }
+
+    /// <summary>
+    /// Get PR diff
+    /// </summary>
+    public async Task<string> GetPullRequestDiffAsync(int prNumber)
+    {
+        // Get files changed in the PR
+        var files = await _octokit.PullRequest.Files(RepoOwner, RepoName, prNumber);
+
+        // Build a simplified diff from file changes
+        var diff = new System.Text.StringBuilder();
+        foreach (var file in files)
+        {
+            diff.AppendLine($"diff --git a/{file.FileName} b/{file.FileName}");
+            diff.AppendLine($"--- a/{file.FileName}");
+            diff.AppendLine($"+++ b/{file.FileName}");
+            diff.AppendLine(file.Patch);
+            diff.AppendLine();
+        }
+
+        return diff.ToString();
+    }
+
+    // ========================================
+    // PHASE 3a: Git Operations (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Check if there are commits between base and head branches
+    /// </summary>
+    public async Task<bool> HasCommitsAsync(string baseBranch, string headBranch)
+    {
+        var comparison = await _octokit.Repository.Commit.Compare(RepoOwner, RepoName, baseBranch, headBranch);
+        return comparison.AheadBy > 0;
+    }
+
+    /// <summary>
+    /// Get file content from a branch
+    /// </summary>
+    public async Task<RepoFile?> TryGetFileContentAsync(string branch, string path)
+    {
+        try
+        {
+            var contents = await _octokit.Repository.Content.GetAllContentsByRef(RepoOwner, RepoName, path, branch);
+            if (contents.Count == 0)
+            {
+                return null;
+            }
+
+            var file = contents[0];
+            return new RepoFile(path, file.Content, file.Sha);
+        }
+        catch (NotFoundException)
+        {
+            return null;
+        }
+    }
+
+    /// <summary>
+    /// Create or update a file on a branch
+    /// </summary>
+    public async Task CreateOrUpdateFileAsync(string branch, string path, string content, string message)
+    {
+        var existing = await TryGetFileContentAsync(branch, path);
+
+        if (existing != null)
+        {
+            await _octokit.Repository.Content.UpdateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new UpdateFileRequest(message, content, existing.Sha, branch)
+            );
+        }
+        else
+        {
+            await _octokit.Repository.Content.CreateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new CreateFileRequest(message, content, branch)
+            );
+        }
+    }
+
+    // ========================================
+    // PHASE 3b: GraphQL Projects V2 (Raw GraphQL)
+    // ========================================
+
+    /// <summary>
+    /// Get GitHub Projects V2 snapshot
+    /// </summary>
+    public async Task<ProjectSnapshot> GetProjectSnapshotAsync(string owner, int projectNumber, ProjectOwnerType ownerType)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+}
+
+fragment ProjectFields on ProjectV2 {
+  title
+  items(first: 100) {
+    nodes {
+      content {
+        ... on Issue { number title url }
+        ... on DraftIssue { title }
+      }
+      fieldValues(first: 20) {
+        nodes {
+          ... on ProjectV2ItemFieldSingleSelectValue {
+            name
+            field { ... on ProjectV2SingleSelectField { name } }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+        var title = project.GetProperty("title").GetString() ?? "";
+        var items = new List<ProjectItem>();
+
+        foreach (var node in project.GetProperty("items").GetProperty("nodes").EnumerateArray())
+        {
+            string? itemTitle = null;
+            int? issueNumber = null;
+            string? itemUrl = null;
+
+            if (node.TryGetProperty("content", out var content) && content.ValueKind != System.Text.Json.JsonValueKind.Null)
+            {
+                if (content.TryGetProperty("title", out var ctTitle))
+                    itemTitle = ctTitle.GetString();
+                if (content.TryGetProperty("number", out var ctNumber))
+                    issueNumber = ctNumber.GetInt32();
+                if (content.TryGetProperty("url", out var ctUrl))
+                    itemUrl = ctUrl.GetString();
+            }
+
+            var status = "Unknown";
+            if (node.TryGetProperty("fieldValues", out var fields))
+            {
+                foreach (var fieldNode in fields.GetProperty("nodes").EnumerateArray())
+                {
+                    if (!fieldNode.TryGetProperty("field", out var field))
+                        continue;
+
+                    var fieldName = field.GetProperty("name").GetString();
+                    if (!string.Equals(fieldName, "Status", StringComparison.OrdinalIgnoreCase))
+                        continue;
+
+                    status = fieldNode.GetProperty("name").GetString() ?? status;
+                    break;
+                }
+            }
+
+            items.Add(new ProjectItem(itemTitle ?? "(untitled)", issueNumber, itemUrl, status));
+        }
+
+        return new ProjectSnapshot(owner, projectNumber, ownerType, title, items);
+    }
+
+    /// <summary>
+    /// Update project item status in GitHub Projects V2
+    /// </summary>
+    public async Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName)
+    {
+        var metadata = await GetProjectMetadataAsync(owner, projectNumber);
+        var item = metadata.Items.FirstOrDefault(i => i.IssueNumber == issueNumber);
+        if (item == null)
+        {
+            Logger.WriteLine($"Project item not found for issue #{issueNumber}.");
+            return;
+        }
+
+        if (!metadata.StatusOptions.TryGetValue(statusName, out var optionId))
+        {
+            Logger.WriteLine($"Status option '{statusName}' not found.");
+            return;
+        }
+
+        var mutation = @"
+mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
+  updateProjectV2ItemFieldValue(input: {
+    projectId: $projectId
+    itemId: $itemId
+    fieldId: $fieldId
+    value: { singleSelectOptionId: $optionId }
+  }) {
+    projectV2Item { id }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query = mutation,
+            variables = new
+            {
+                projectId = metadata.ProjectId,
+                itemId = item.ItemId,
+                fieldId = metadata.StatusFieldId,
+                optionId
+            }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project status update failed: {res.StatusCode} {txt}");
+        }
+    }
+
+    private async Task<ProjectMetadata> GetProjectMetadataAsync(string owner, int projectNumber)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+
+        var projectId = project.GetProperty("id").GetString() ?? "";
+        var statusFieldId = "";
+        var statusOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
+
+        if (!project.TryGetProperty("fields", out var fields) || fields.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields'.");
+        }
+
+        if (!fields.TryGetProperty("nodes", out var fieldNodes) || fieldNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields.nodes'.");
+        }
+
+        foreach (var field in fieldNodes.EnumerateArray())
+        {
+            if (!field.TryGetProperty("name", out var nameElement))
+            {
+                continue;
+            }
+
+            var name = nameElement.GetString();
+            if (!string.Equals(name, "Status", StringComparison.OrdinalIgnoreCase))
+                continue;
+
+            if (!field.TryGetProperty("id", out var idElement))
+            {
+                continue;
+            }
+
+            statusFieldId = idElement.GetString() ?? "";
+            if (!field.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
+            {
+                continue;
+            }
+
+            foreach (var option in optionsElement.EnumerateArray())
+            {
+                if (!option.TryGetProperty("name", out var optionNameElement))
+                {
+                    continue;
+                }
+
+                if (!option.TryGetProperty("id", out var optionIdElement))
+                {
+                    continue;
+                }
+
+                var optionName = optionNameElement.GetString() ?? "";
+                var optionId = optionIdElement.GetString() ?? "";
+                if (!string.IsNullOrWhiteSpace(optionName) && !string.IsNullOrWhiteSpace(optionId))
+                {
+                    statusOptions[optionName] = optionId;
+                }
+            }
+        }
+
+        if (string.IsNullOrWhiteSpace(statusFieldId))
+        {
+            throw new InvalidOperationException("Status field not found in project.");
+        }
+
+        if (!project.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items'.");
+        }
+
+        if (!itemsElement.TryGetProperty("nodes", out var itemNodes) || itemNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items.nodes'.");
+        }
+
+        var items = new List<ProjectItemRef>();
+        foreach (var node in itemNodes.EnumerateArray())
+        {
+            if (!node.TryGetProperty("content", out var content) || content.ValueKind == System.Text.Json.JsonValueKind.Null)
+                continue;
+
+            if (!content.TryGetProperty("number", out var numberProp))
+                continue;
+
+            var issueNumber = numberProp.GetInt32();
+            var itemId = node.GetProperty("id").GetString() ?? "";
+            if (!string.IsNullOrWhiteSpace(itemId))
+            {
+                items.Add(new ProjectItemRef(itemId, issueNumber));
+            }
+        }
+
+        return new ProjectMetadata(projectId, statusFieldId, statusOptions, items);
+    }
+
+    private static System.Text.Json.JsonElement ResolveProjectElement(System.Text.Json.JsonDocument doc)
+    {
+        var data = doc.RootElement.GetProperty("data");
+        if (data.TryGetProperty("user", out var user) && user.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            user.TryGetProperty("projectV2", out var userProject) && userProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return userProject;
+        }
+
+        if (data.TryGetProperty("organization", out var org) && org.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            org.TryGetProperty("projectV2", out var orgProject) && orgProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return orgProject;
+        }
+
+        var errors = TryFormatGraphQlErrors(doc);
+        if (!string.IsNullOrWhiteSpace(errors))
+        {
+            throw new InvalidOperationException($"Project not found. GraphQL errors: {errors}");
+        }
+
+        throw new InvalidOperationException("Project not found.");
+    }
+
+    private static string? TryFormatGraphQlErrors(System.Text.Json.JsonDocument doc)
+    {
+        if (!doc.RootElement.TryGetProperty("errors", out var errors) || errors.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            return null;
+        }
+
+        var messages = new List<string>();
+        foreach (var error in errors.EnumerateArray())
+        {
+            if (error.TryGetProperty("message", out var message))
+            {
+                var text = message.GetString();
+                if (!string.IsNullOrWhiteSpace(text))
+                {
+                    messages.Add(text);
+                }
+            }
+        }
+
+        return messages.Count > 0 ? string.Join(" | ", messages) : null;
+    }
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-Dev-attempt-1.md b/orchestrator/prompts/issue-43-Dev-attempt-1.md
--- a/orchestrator/prompts/issue-43-Dev-attempt-1.md
+++ b/orchestrator/prompts/issue-43-Dev-attempt-1.md
@@ -0,0 +1,812 @@
+# System Prompt
+
+You are a software engineer implementing a spec. CRITICAL INSTRUCTIONS:
+1. Read the Touch List Entry to understand what operation to perform
+2. Study the Interfaces section which shows the required changes (before/after examples)
+3. Apply those exact changes to the Current File Content
+4. For 'Modify' operations: update/remove code as specified in the notes
+5. When removing code: COMPLETELY OMIT it from your output - do NOT include it with comments
+6. Output ONLY the complete updated file content
+7. Do NOT include before/after comments or explanations
+8. Do NOT preserve code marked for removal
+9. VERIFY your output does not contain any code that should be removed
+Follow the spec strictly. Code removal means the code must be absent from your output.
+
+# User Prompt
+
+Mode: minimal
+
+Spec Goal:
+Remove dead methods CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and delete their implementations in OctokitGitHubClient so the public surface no longer exposes unused GitHub branch API calls. Ensure tests are updated so the test suite continues to pass.
+
+=== TOUCH LIST ENTRY ===
+Operation: Modify
+File: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+Instructions: Remove CreateBranchAsync and DeleteBranchAsync implementations (approx lines 250-280)
+
+=== REQUIRED CHANGES (Before/After Examples) ===
+// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+public interface IGitHubClient
+{
+    Task<string> GetDefaultBranchAsync(string owner, string repo);
+    Task<bool> RepositoryExistsAsync(string owner, string repo);
+    Task CreateBranchAsync(string branchName);
+    Task DeleteBranchAsync(string branchName);
+}
+
+// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+public interface IGitHubClient
+{
+    Task<string> GetDefaultBranchAsync(string owner, string repo);
+    Task<bool> RepositoryExistsAsync(string owner, string repo);
+    // CreateBranchAsync and DeleteBranchAsync removed
+
+// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+public class OctokitGitHubClient : IGitHubClient
+{
+    // ... other members ...
+
+    public async Task CreateBranchAsync(string branchName)
+    {
+        // Implementation using Octokit REST API to create a reference
+        var owner = _options.Owner;
+        var repo = _options.Repository;
+        var masterRef = await _client.Git.Reference.Get(owner, repo, "heads/main");
+        var newRef = new NewReference($"refs/heads/{branchName}", masterRef.Object.Sha);
+        await _client.Git.Reference.Create(owner, repo, newRef);
+    }
+
+    public async Task DeleteBranchAsync(string branchName)
+    {
+        var owner = _options.Owner;
+        var repo = _options.Repository;
+        await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");
+    }
+
+    // ... other members ...
+}
+
+// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+public class OctokitGitHubClient : IGitHubClient
+{
+    // ... other members ...
+    // CreateBranchAsync and DeleteBranchAsync implementations removed
+    // Remaining IGitHubClient methods (GetDefaultBranchAsync, RepositoryExistsAsync, etc.) remain unchanged.
+}
+
+Note: the BEFORE snippets above show the exact method signatures that will be removed. The AFTER snippets show those signatures absent from the files.
+
+For tests:
+ // BEFORE: tests/
+ // Example: tests/Infrastructure/GitHub/OctokitGitHubClientTests.cs
+ // [Fact]
+ // public async Task CreateBranchAsync_CreatesReferenceOnGitHub() { ... calls CreateBranchAsync ... }
+ //
+ // [Fact]
+ // public async Task DeleteBranchAsync_DeletesReferenceOnGitHub() { ... calls DeleteBranchAsync ... }
+ //
+ // AFTER: tests/
+ // Any tests that referenced CreateBranchAsync/DeleteBranchAsync have been removed or updated so no tests call these methods.
+
+Scenarios:
+Scenario: Interface no longer exposes branch API methods  
+Given the IGitHubClient interface in the codebase  
+When CreateBranchAsync and DeleteBranchAsync are removed from IGitHubClient.cs  
+Then the IGitHubClient interface does not contain CreateBranchAsync or DeleteBranchAsync
+
+Scenario: Octokit implementation no longer contains implementations  
+Given the OctokitGitHubClient class that previously implemented CreateBranchAsync and DeleteBranchAsync  
+When the implementations are deleted from OctokitGitHubClient.cs  
+Then OctokitGitHubClient compiles and no longer contains those methods
+
+Scenario: Test suite remains green after removal  
+Given the full test suite runs after code removal  
+When any tests referencing the removed methods are deleted/updated and tests executed  
+Then all tests pass and no test depends on the removed methods
+
+Scenario: No runtime behavior change for branch operations  
+Given branch creation/deletion is performed locally via RepoGit.EnsureBranch and CommitAndPush  
+When IGitHubClient removal is deployed  
+Then existing behavior remains unchanged because no code path called the removed methods
+
+Sequence:
+Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs — remove CreateBranchAsync and DeleteBranchAsync method declarations.
+Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs — delete the CreateBranchAsync and DeleteBranchAsync method implementations (preserve the rest of the class).
+Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync. Remove those test methods or update them to not call removed methods. Prefer deletion unless the test verifies other behavior.
+Build solution; fix any compilation issues (none expected if methods were truly unused).
+Run unit tests: dotnet test (xUnit). Ensure all tests pass.
+Commit changes with a short message: "Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient and OctokitGitHubClient".
+
+Test Matrix:
+| Unit | tests/ | Remove or update tests that reference CreateBranchAsync/DeleteBranchAsync. All other tests run unchanged (FW-02). |
+
+=== CURRENT FILE CONTENT (TO BE MODIFIED) ===
+using System.Net.Http;
+using Octokit;
+using Orchestrator.App.Core.Configuration;
+using Orchestrator.App.Core.Models;
+using CoreIssueComment = Orchestrator.App.Core.Models.IssueComment;
+
+namespace Orchestrator.App.Infrastructure.GitHub;
+
+/// <summary>
+/// GitHub client using Octokit.NET for REST API and GraphQL operations
+/// </summary>
+internal sealed class OctokitGitHubClient : Orchestrator.App.Core.Interfaces.IGitHubClient
+{
+    private readonly Octokit.GitHubClient _octokit;
+    private readonly OrchestratorConfig _cfg;
+    private readonly HttpClient _http;
+
+    private string RepoOwner => _cfg.RepoOwner;
+    private string RepoName => _cfg.RepoName;
+
+    public OctokitGitHubClient(OrchestratorConfig cfg) : this(cfg, CreateDefaultGitHubClient(cfg), new HttpClient())
+    {
+    }
+
+    internal OctokitGitHubClient(OrchestratorConfig cfg, Octokit.GitHubClient octokitClient, HttpClient httpClient)
+    {
+        _cfg = cfg;
+        _octokit = octokitClient;
+        _http = httpClient;
+
+        // Configure HttpClient for GraphQL
+        _http.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("conjunction-orchestrator", "0.3"));
+        _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.GitHubToken);
+        }
+    }
+
+    private static Octokit.GitHubClient CreateDefaultGitHubClient(OrchestratorConfig cfg)
+    {
+        var client = new Octokit.GitHubClient(new ProductHeaderValue("conjunction-orchestrator", "0.3"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            client.Credentials = new Credentials(cfg.GitHubToken);
+        }
+
+        return client;
+    }
+
+    // ========================================
+    // PHASE 1: Core REST API (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Get open issues for the repository
+    /// </summary>
+    public async Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50)
+    {
+        var issueRequest = new RepositoryIssueRequest
+        {
+            State = ItemStateFilter.Open
+        };
+
+        var apiOptions = new ApiOptions
+        {
+            PageSize = perPage,
+            PageCount = 1
+        };
+
+        var issues = await _octokit.Issue.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            issueRequest,
+            apiOptions
+        );
+
+        // Convert Octokit Issues to WorkItems, excluding PRs
+        return issues
+            .Where(i => i.PullRequest == null)
+            .Select(i => new WorkItem(
+                Number: i.Number,
+                Title: i.Title,
+                Body: i.Body ?? string.Empty,
+                Url: i.HtmlUrl,
+                Labels: i.Labels.Select(l => l.Name).ToList()
+            ))
+            .ToList();
+    }
+
+    /// <summary>
+    /// Get labels for a specific issue
+    /// </summary>
+    public async Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber)
+    {
+        var issue = await _octokit.Issue.Get(RepoOwner, RepoName, issueNumber);
+        return issue.Labels.Select(l => l.Name).ToList();
+    }
+
+    /// <summary>
+    /// Create a pull request
+    /// </summary>
+    public async Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body)
+    {
+        var newPr = new NewPullRequest(title, headBranch, baseBranch)
+        {
+            Body = body
+        };
+
+        var pr = await _octokit.PullRequest.Create(RepoOwner, RepoName, newPr);
+        return pr.HtmlUrl;
+    }
+
+    /// <summary>
+    /// Get PR number for a branch, or null if not found
+    /// </summary>
+    public async Task<int?> GetPullRequestNumberAsync(string branchName)
+    {
+        var prs = await _octokit.PullRequest.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            new PullRequestRequest { State = ItemStateFilter.All }
+        );
+
+        var pr = prs.FirstOrDefault(p => p.Head.Ref == branchName);
+        return pr?.Number;
+    }
+
+    /// <summary>
+    /// Close a pull request
+    /// </summary>
+    public async Task ClosePullRequestAsync(int prNumber)
+    {
+        var update = new PullRequestUpdate
+        {
+            State = ItemState.Closed
+        };
+
+        await _octokit.PullRequest.Update(RepoOwner, RepoName, prNumber, update);
+    }
+
+    /// <summary>
+    /// Get comments on an issue
+    /// </summary>
+    public async Task<IReadOnlyList<CoreIssueComment>> GetIssueCommentsAsync(int issueNumber)
+    {
+        var comments = await _octokit.Issue.Comment.GetAllForIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber
+        );
+
+        return comments.Select(c => new CoreIssueComment(
+            Author: c.User.Login,
+            Body: c.Body
+        )).ToList();
+    }
+
+    /// <summary>
+    /// Post a comment on an issue
+    /// </summary>
+    public async Task CommentOnWorkItemAsync(int issueNumber, string comment)
+    {
+        await _octokit.Issue.Comment.Create(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            comment
+        );
+    }
+
+    /// <summary>
+    /// Add labels to an issue
+    /// </summary>
+    public async Task AddLabelsAsync(int issueNumber, params string[] labels)
+    {
+        if (labels.Length == 0)
+            return;
+
+        await _octokit.Issue.Labels.AddToIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            labels
+        );
+    }
+
+    /// <summary>
+    /// Remove a label from an issue
+    /// </summary>
+    public async Task RemoveLabelAsync(int issueNumber, string label)
+    {
+        try
+        {
+            await _octokit.Issue.Labels.RemoveFromIssue(
+                RepoOwner,
+                RepoName,
+                issueNumber,
+                label
+            );
+        }
+        catch (NotFoundException)
+        {
+            // Label doesn't exist on issue - ignore
+        }
+    }
+
+    /// <summary>
+    /// Remove multiple labels from an issue
+    /// </summary>
+    public async Task RemoveLabelsAsync(int issueNumber, params string[] labels)
+    {
+        foreach (var label in labels)
+        {
+            await RemoveLabelAsync(issueNumber, label);
+        }
+    }
+
+    /// <summary>
+    /// Get PR diff
+    /// </summary>
+    public async Task<string> GetPullRequestDiffAsync(int prNumber)
+    {
+        // Get files changed in the PR
+        var files = await _octokit.PullRequest.Files(RepoOwner, RepoName, prNumber);
+
+        // Build a simplified diff from file changes
+        var diff = new System.Text.StringBuilder();
+        foreach (var file in files)
+        {
+            diff.AppendLine($"diff --git a/{file.FileName} b/{file.FileName}");
+            diff.AppendLine($"--- a/{file.FileName}");
+            diff.AppendLine($"+++ b/{file.FileName}");
+            diff.AppendLine(file.Patch);
+            diff.AppendLine();
+        }
+
+        return diff.ToString();
+    }
+
+    // ========================================
+    // PHASE 3a: Git Operations (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Check if there are commits between base and head branches
+    /// </summary>
+    public async Task<bool> HasCommitsAsync(string baseBranch, string headBranch)
+    {
+        var comparison = await _octokit.Repository.Commit.Compare(RepoOwner, RepoName, baseBranch, headBranch);
+        return comparison.AheadBy > 0;
+    }
+
+    /// <summary>
+    /// Get file content from a branch
+    /// </summary>
+    public async Task<RepoFile?> TryGetFileContentAsync(string branch, string path)
+    {
+        try
+        {
+            var contents = await _octokit.Repository.Content.GetAllContentsByRef(RepoOwner, RepoName, path, branch);
+            if (contents.Count == 0)
+            {
+                return null;
+            }
+
+            var file = contents[0];
+            return new RepoFile(path, file.Content, file.Sha);
+        }
+        catch (NotFoundException)
+        {
+            return null;
+        }
+    }
+
+    /// <summary>
+    /// Create or update a file on a branch
+    /// </summary>
+    public async Task CreateOrUpdateFileAsync(string branch, string path, string content, string message)
+    {
+        var existing = await TryGetFileContentAsync(branch, path);
+
+        if (existing != null)
+        {
+            await _octokit.Repository.Content.UpdateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new UpdateFileRequest(message, content, existing.Sha, branch)
+            );
+        }
+        else
+        {
+            await _octokit.Repository.Content.CreateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new CreateFileRequest(message, content, branch)
+            );
+        }
+    }
+
+    // ========================================
+    // PHASE 3b: GraphQL Projects V2 (Raw GraphQL)
+    // ========================================
+
+    /// <summary>
+    /// Get GitHub Projects V2 snapshot
+    /// </summary>
+    public async Task<ProjectSnapshot> GetProjectSnapshotAsync(string owner, int projectNumber, ProjectOwnerType ownerType)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+}
+
+fragment ProjectFields on ProjectV2 {
+  title
+  items(first: 100) {
+    nodes {
+      content {
+        ... on Issue { number title url }
+        ... on DraftIssue { title }
+      }
+      fieldValues(first: 20) {
+        nodes {
+          ... on ProjectV2ItemFieldSingleSelectValue {
+            name
+            field { ... on ProjectV2SingleSelectField { name } }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+        var title = project.GetProperty("title").GetString() ?? "";
+        var items = new List<ProjectItem>();
+
+        foreach (var node in project.GetProperty("items").GetProperty("nodes").EnumerateArray())
+        {
+            string? itemTitle = null;
+            int? issueNumber = null;
+            string? itemUrl = null;
+
+            if (node.TryGetProperty("content", out var content) && content.ValueKind != System.Text.Json.JsonValueKind.Null)
+            {
+                if (content.TryGetProperty("title", out var ctTitle))
+                    itemTitle = ctTitle.GetString();
+                if (content.TryGetProperty("number", out var ctNumber))
+                    issueNumber = ctNumber.GetInt32();
+                if (content.TryGetProperty("url", out var ctUrl))
+                    itemUrl = ctUrl.GetString();
+            }
+
+            var status = "Unknown";
+            if (node.TryGetProperty("fieldValues", out var fields))
+            {
+                foreach (var fieldNode in fields.GetProperty("nodes").EnumerateArray())
+                {
+                    if (!fieldNode.TryGetProperty("field", out var field))
+                        continue;
+
+                    var fieldName = field.GetProperty("name").GetString();
+                    if (!string.Equals(fieldName, "Status", StringComparison.OrdinalIgnoreCase))
+                        continue;
+
+                    status = fieldNode.GetProperty("name").GetString() ?? status;
+                    break;
+                }
+            }
+
+            items.Add(new ProjectItem(itemTitle ?? "(untitled)", issueNumber, itemUrl, status));
+        }
+
+        return new ProjectSnapshot(owner, projectNumber, ownerType, title, items);
+    }
+
+    /// <summary>
+    /// Update project item status in GitHub Projects V2
+    /// </summary>
+    public async Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName)
+    {
+        var metadata = await GetProjectMetadataAsync(owner, projectNumber);
+        var item = metadata.Items.FirstOrDefault(i => i.IssueNumber == issueNumber);
+        if (item == null)
+        {
+            Logger.WriteLine($"Project item not found for issue #{issueNumber}.");
+            return;
+        }
+
+        if (!metadata.StatusOptions.TryGetValue(statusName, out var optionId))
+        {
+            Logger.WriteLine($"Status option '{statusName}' not found.");
+            return;
+        }
+
+        var mutation = @"
+mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
+  updateProjectV2ItemFieldValue(input: {
+    projectId: $projectId
+    itemId: $itemId
+    fieldId: $fieldId
+    value: { singleSelectOptionId: $optionId }
+  }) {
+    projectV2Item { id }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query = mutation,
+            variables = new
+            {
+                projectId = metadata.ProjectId,
+                itemId = item.ItemId,
+                fieldId = metadata.StatusFieldId,
+                optionId
+            }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project status update failed: {res.StatusCode} {txt}");
+        }
+    }
+
+    private async Task<ProjectMetadata> GetProjectMetadataAsync(string owner, int projectNumber)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+
+        var projectId = project.GetProperty("id").GetString() ?? "";
+        var statusFieldId = "";
+        var statusOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
+
+        if (!project.TryGetProperty("fields", out var fields) || fields.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields'.");
+        }
+
+        if (!fields.TryGetProperty("nodes", out var fieldNodes) || fieldNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields.nodes'.");
+        }
+
+        foreach (var field in fieldNodes.EnumerateArray())
+        {
+            if (!field.TryGetProperty("name", out var nameElement))
+            {
+                continue;
+            }
+
+            var name = nameElement.GetString();
+            if (!string.Equals(name, "Status", StringComparison.OrdinalIgnoreCase))
+                continue;
+
+            if (!field.TryGetProperty("id", out var idElement))
+            {
+                continue;
+            }
+
+            statusFieldId = idElement.GetString() ?? "";
+            if (!field.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
+            {
+                continue;
+            }
+
+            foreach (var option in optionsElement.EnumerateArray())
+            {
+                if (!option.TryGetProperty("name", out var optionNameElement))
+                {
+                    continue;
+                }
+
+                if (!option.TryGetProperty("id", out var optionIdElement))
+                {
+                    continue;
+                }
+
+                var optionName = optionNameElement.GetString() ?? "";
+                var optionId = optionIdElement.GetString() ?? "";
+                if (!string.IsNullOrWhiteSpace(optionName) && !string.IsNullOrWhiteSpace(optionId))
+                {
+                    statusOptions[optionName] = optionId;
+                }
+            }
+        }
+
+        if (string.IsNullOrWhiteSpace(statusFieldId))
+        {
+            throw new InvalidOperationException("Status field not found in project.");
+        }
+
+        if (!project.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items'.");
+        }
+
+        if (!itemsElement.TryGetProperty("nodes", out var itemNodes) || itemNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items.nodes'.");
+        }
+
+        var items = new List<ProjectItemRef>();
+        foreach (var node in itemNodes.EnumerateArray())
+        {
+            if (!node.TryGetProperty("content", out var content) || content.ValueKind == System.Text.Json.JsonValueKind.Null)
+                continue;
+
+            if (!content.TryGetProperty("number", out var numberProp))
+                continue;
+
+            var issueNumber = numberProp.GetInt32();
+            var itemId = node.GetProperty("id").GetString() ?? "";
+            if (!string.IsNullOrWhiteSpace(itemId))
+            {
+                items.Add(new ProjectItemRef(itemId, issueNumber));
+            }
+        }
+
+        return new ProjectMetadata(projectId, statusFieldId, statusOptions, items);
+    }
+
+    private static System.Text.Json.JsonElement ResolveProjectElement(System.Text.Json.JsonDocument doc)
+    {
+        var data = doc.RootElement.GetProperty("data");
+        if (data.TryGetProperty("user", out var user) && user.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            user.TryGetProperty("projectV2", out var userProject) && userProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return userProject;
+        }
+
+        if (data.TryGetProperty("organization", out var org) && org.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            org.TryGetProperty("projectV2", out var orgProject) && orgProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return orgProject;
+        }
+
+        var errors = TryFormatGraphQlErrors(doc);
+        if (!string.IsNullOrWhiteSpace(errors))
+        {
+            throw new InvalidOperationException($"Project not found. GraphQL errors: {errors}");
+        }
+
+        throw new InvalidOperationException("Project not found.");
+    }
+
+    private static string? TryFormatGraphQlErrors(System.Text.Json.JsonDocument doc)
+    {
+        if (!doc.RootElement.TryGetProperty("errors", out var errors) || errors.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            return null;
+        }
+
+        var messages = new List<string>();
+        foreach (var error in errors.EnumerateArray())
+        {
+            if (error.TryGetProperty("message", out var message))
+            {
+                var text = message.GetString();
+                if (!string.IsNullOrWhiteSpace(text))
+                {
+                    messages.Add(text);
+                }
+            }
+        }
+
+        return messages.Count > 0 ? string.Join(" | ", messages) : null;
+    }
+}
+
+=== EXAMPLE: HOW TO REMOVE CODE ===
+If the instruction says 'Remove CreateBranchAsync method':
+
+BEFORE (current file content):
+  Task<bool> CreateBranchAsync(string branchName, string baseBranch);
+  Task DeleteFileAsync(string path);
+
+AFTER (your output should NOT include CreateBranchAsync):
+  Task DeleteFileAsync(string path);
+
+CRITICAL: When removing code:
+- DO NOT include removed methods/classes/properties in your output
+- DO NOT add comments like '// removed' or '// deleted'
+- Simply omit the code completely from your response
+
+=== YOUR TASK ===
+Apply the changes shown in 'REQUIRED CHANGES' section to the 'CURRENT FILE CONTENT'.
+Follow the instructions: Remove CreateBranchAsync and DeleteBranchAsync implementations (approx lines 250-280)
+Output the complete updated file content below:

diff --git a/orchestrator/prompts/issue-43-Dev-attempt-2-response.md b/orchestrator/prompts/issue-43-Dev-attempt-2-response.md
--- a/orchestrator/prompts/issue-43-Dev-attempt-2-response.md
+++ b/orchestrator/prompts/issue-43-Dev-attempt-2-response.md
@@ -0,0 +1,669 @@
+using System.Net.Http;
+using Octokit;
+using Orchestrator.App.Core.Configuration;
+using Orchestrator.App.Core.Models;
+using CoreIssueComment = Orchestrator.App.Core.Models.IssueComment;
+
+namespace Orchestrator.App.Infrastructure.GitHub;
+
+/// <summary>
+/// GitHub client using Octokit.NET for REST API and GraphQL operations
+/// </summary>
+internal sealed class OctokitGitHubClient : Orchestrator.App.Core.Interfaces.IGitHubClient
+{
+    private readonly Octokit.GitHubClient _octokit;
+    private readonly OrchestratorConfig _cfg;
+    private readonly HttpClient _http;
+
+    private string RepoOwner => _cfg.RepoOwner;
+    private string RepoName => _cfg.RepoName;
+
+    public OctokitGitHubClient(OrchestratorConfig cfg) : this(cfg, CreateDefaultGitHubClient(cfg), new HttpClient())
+    {
+    }
+
+    internal OctokitGitHubClient(OrchestratorConfig cfg, Octokit.GitHubClient octokitClient, HttpClient httpClient)
+    {
+        _cfg = cfg;
+        _octokit = octokitClient;
+        _http = httpClient;
+
+        // Configure HttpClient for GraphQL
+        _http.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("conjunction-orchestrator", "0.3"));
+        _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.GitHubToken);
+        }
+    }
+
+    private static Octokit.GitHubClient CreateDefaultGitHubClient(OrchestratorConfig cfg)
+    {
+        var client = new Octokit.GitHubClient(new ProductHeaderValue("conjunction-orchestrator", "0.3"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            client.Credentials = new Credentials(cfg.GitHubToken);
+        }
+
+        return client;
+    }
+
+    // ========================================
+    // PHASE 1: Core REST API (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Get open issues for the repository
+    /// </summary>
+    public async Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50)
+    {
+        var issueRequest = new RepositoryIssueRequest
+        {
+            State = ItemStateFilter.Open
+        };
+
+        var apiOptions = new ApiOptions
+        {
+            PageSize = perPage,
+            PageCount = 1
+        };
+
+        var issues = await _octokit.Issue.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            issueRequest,
+            apiOptions
+        );
+
+        // Convert Octokit Issues to WorkItems, excluding PRs
+        return issues
+            .Where(i => i.PullRequest == null)
+            .Select(i => new WorkItem(
+                Number: i.Number,
+                Title: i.Title,
+                Body: i.Body ?? string.Empty,
+                Url: i.HtmlUrl,
+                Labels: i.Labels.Select(l => l.Name).ToList()
+            ))
+            .ToList();
+    }
+
+    /// <summary>
+    /// Get labels for a specific issue
+    /// </summary>
+    public async Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber)
+    {
+        var issue = await _octokit.Issue.Get(RepoOwner, RepoName, issueNumber);
+        return issue.Labels.Select(l => l.Name).ToList();
+    }
+
+    /// <summary>
+    /// Create a pull request
+    /// </summary>
+    public async Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body)
+    {
+        var newPr = new NewPullRequest(title, headBranch, baseBranch)
+        {
+            Body = body
+        };
+
+        var pr = await _octokit.PullRequest.Create(RepoOwner, RepoName, newPr);
+        return pr.HtmlUrl;
+    }
+
+    /// <summary>
+    /// Get PR number for a branch, or null if not found
+    /// </summary>
+    public async Task<int?> GetPullRequestNumberAsync(string branchName)
+    {
+        var prs = await _octokit.PullRequest.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            new PullRequestRequest { State = ItemStateFilter.All }
+        );
+
+        var pr = prs.FirstOrDefault(p => p.Head.Ref == branchName);
+        return pr?.Number;
+    }
+
+    /// <summary>
+    /// Close a pull request
+    /// </summary>
+    public async Task ClosePullRequestAsync(int prNumber)
+    {
+        var update = new PullRequestUpdate
+        {
+            State = ItemState.Closed
+        };
+
+        await _octokit.PullRequest.Update(RepoOwner, RepoName, prNumber, update);
+    }
+
+    /// <summary>
+    /// Get comments on an issue
+    /// </summary>
+    public async Task<IReadOnlyList<CoreIssueComment>> GetIssueCommentsAsync(int issueNumber)
+    {
+        var comments = await _octokit.Issue.Comment.GetAllForIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber
+        );
+
+        return comments.Select(c => new CoreIssueComment(
+            Author: c.User.Login,
+            Body: c.Body
+        )).ToList();
+    }
+
+    /// <summary>
+    /// Post a comment on an issue
+    /// </summary>
+    public async Task CommentOnWorkItemAsync(int issueNumber, string comment)
+    {
+        await _octokit.Issue.Comment.Create(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            comment
+        );
+    }
+
+    /// <summary>
+    /// Add labels to an issue
+    /// </summary>
+    public async Task AddLabelsAsync(int issueNumber, params string[] labels)
+    {
+        if (labels.Length == 0)
+            return;
+
+        await _octokit.Issue.Labels.AddToIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            labels
+        );
+    }
+
+    /// <summary>
+    /// Remove a label from an issue
+    /// </summary>
+    public async Task RemoveLabelAsync(int issueNumber, string label)
+    {
+        try
+        {
+            await _octokit.Issue.Labels.RemoveFromIssue(
+                RepoOwner,
+                RepoName,
+                issueNumber,
+                label
+            );
+        }
+        catch (NotFoundException)
+        {
+            // Label doesn't exist on issue - ignore
+        }
+    }
+
+    /// <summary>
+    /// Remove multiple labels from an issue
+    /// </summary>
+    public async Task RemoveLabelsAsync(int issueNumber, params string[] labels)
+    {
+        foreach (var label in labels)
+        {
+            await RemoveLabelAsync(issueNumber, label);
+        }
+    }
+
+    /// <summary>
+    /// Get PR diff
+    /// </summary>
+    public async Task<string> GetPullRequestDiffAsync(int prNumber)
+    {
+        // Get files changed in the PR
+        var files = await _octokit.PullRequest.Files(RepoOwner, RepoName, prNumber);
+
+        // Build a simplified diff from file changes
+        var diff = new System.Text.StringBuilder();
+        foreach (var file in files)
+        {
+            diff.AppendLine($"diff --git a/{file.FileName} b/{file.FileName}");
+            diff.AppendLine($"--- a/{file.FileName}");
+            diff.AppendLine($"+++ b/{file.FileName}");
+            diff.AppendLine(file.Patch);
+            diff.AppendLine();
+        }
+
+        return diff.ToString();
+    }
+
+    // ========================================
+    // PHASE 3a: Git Operations (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Check if there are commits between base and head branches
+    /// </summary>
+    public async Task<bool> HasCommitsAsync(string baseBranch, string headBranch)
+    {
+        var comparison = await _octokit.Repository.Commit.Compare(RepoOwner, RepoName, baseBranch, headBranch);
+        return comparison.AheadBy > 0;
+    }
+
+    /// <summary>
+    /// Get file content from a branch
+    /// </summary>
+    public async Task<RepoFile?> TryGetFileContentAsync(string branch, string path)
+    {
+        try
+        {
+            var contents = await _octokit.Repository.Content.GetAllContentsByRef(RepoOwner, RepoName, path, branch);
+            if (contents.Count == 0)
+            {
+                return null;
+            }
+
+            var file = contents[0];
+            return new RepoFile(path, file.Content, file.Sha);
+        }
+        catch (NotFoundException)
+        {
+            return null;
+        }
+    }
+
+    /// <summary>
+    /// Create or update a file on a branch
+    /// </summary>
+    public async Task CreateOrUpdateFileAsync(string branch, string path, string content, string message)
+    {
+        var existing = await TryGetFileContentAsync(branch, path);
+
+        if (existing != null)
+        {
+            await _octokit.Repository.Content.UpdateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new UpdateFileRequest(message, content, existing.Sha, branch)
+            );
+        }
+        else
+        {
+            await _octokit.Repository.Content.CreateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new CreateFileRequest(message, content, branch)
+            );
+        }
+    }
+
+    // ========================================
+    // PHASE 3b: GraphQL Projects V2 (Raw GraphQL)
+    // ========================================
+
+    /// <summary>
+    /// Get GitHub Projects V2 snapshot
+    /// </summary>
+    public async Task<ProjectSnapshot> GetProjectSnapshotAsync(string owner, int projectNumber, ProjectOwnerType ownerType)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+}
+
+fragment ProjectFields on ProjectV2 {
+  title
+  items(first: 100) {
+    nodes {
+      content {
+        ... on Issue { number title url }
+        ... on DraftIssue { title }
+      }
+      fieldValues(first: 20) {
+        nodes {
+          ... on ProjectV2ItemFieldSingleSelectValue {
+            name
+            field { ... on ProjectV2SingleSelectField { name } }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+        var title = project.GetProperty("title").GetString() ?? "";
+        var items = new List<ProjectItem>();
+
+        foreach (var node in project.GetProperty("items").GetProperty("nodes").EnumerateArray())
+        {
+            string? itemTitle = null;
+            int? issueNumber = null;
+            string? itemUrl = null;
+
+            if (node.TryGetProperty("content", out var content) && content.ValueKind != System.Text.Json.JsonValueKind.Null)
+            {
+                if (content.TryGetProperty("title", out var ctTitle))
+                    itemTitle = ctTitle.GetString();
+                if (content.TryGetProperty("number", out var ctNumber))
+                    issueNumber = ctNumber.GetInt32();
+                if (content.TryGetProperty("url", out var ctUrl))
+                    itemUrl = ctUrl.GetString();
+            }
+
+            var status = "Unknown";
+            if (node.TryGetProperty("fieldValues", out var fields))
+            {
+                foreach (var fieldNode in fields.GetProperty("nodes").EnumerateArray())
+                {
+                    if (!fieldNode.TryGetProperty("field", out var field))
+                        continue;
+
+                    var fieldName = field.GetProperty("name").GetString();
+                    if (!string.Equals(fieldName, "Status", StringComparison.OrdinalIgnoreCase))
+                        continue;
+
+                    status = fieldNode.GetProperty("name").GetString() ?? status;
+                    break;
+                }
+            }
+
+            items.Add(new ProjectItem(itemTitle ?? "(untitled)", issueNumber, itemUrl, status));
+        }
+
+        return new ProjectSnapshot(owner, projectNumber, ownerType, title, items);
+    }
+
+    /// <summary>
+    /// Update project item status in GitHub Projects V2
+    /// </summary>
+    public async Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName)
+    {
+        var metadata = await GetProjectMetadataAsync(owner, projectNumber);
+        var item = metadata.Items.FirstOrDefault(i => i.IssueNumber == issueNumber);
+        if (item == null)
+        {
+            Logger.WriteLine($"Project item not found for issue #{issueNumber}.");
+            return;
+        }
+
+        if (!metadata.StatusOptions.TryGetValue(statusName, out var optionId))
+        {
+            Logger.WriteLine($"Status option '{statusName}' not found.");
+            return;
+        }
+
+        var mutation = @"
+mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
+  updateProjectV2ItemFieldValue(input: {
+    projectId: $projectId
+    itemId: $itemId
+    fieldId: $fieldId
+    value: { singleSelectOptionId: $optionId }
+  }) {
+    projectV2Item { id }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query = mutation,
+            variables = new
+            {
+                projectId = metadata.ProjectId,
+                itemId = item.ItemId,
+                fieldId = metadata.StatusFieldId,
+                optionId
+            }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project status update failed: {res.StatusCode} {txt}");
+        }
+    }
+
+    private async Task<ProjectMetadata> GetProjectMetadataAsync(string owner, int projectNumber)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+
+        var projectId = project.GetProperty("id").GetString() ?? "";
+        var statusFieldId = "";
+        var statusOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
+
+        if (!project.TryGetProperty("fields", out var fields) || fields.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields'.");
+        }
+
+        if (!fields.TryGetProperty("nodes", out var fieldNodes) || fieldNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields.nodes'.");
+        }
+
+        foreach (var field in fieldNodes.EnumerateArray())
+        {
+            if (!field.TryGetProperty("name", out var nameElement))
+            {
+                continue;
+            }
+
+            var name = nameElement.GetString();
+            if (!string.Equals(name, "Status", StringComparison.OrdinalIgnoreCase))
+                continue;
+
+            if (!field.TryGetProperty("id", out var idElement))
+            {
+                continue;
+            }
+
+            statusFieldId = idElement.GetString() ?? "";
+            if (!field.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
+            {
+                continue;
+            }
+
+            foreach (var option in optionsElement.EnumerateArray())
+            {
+                if (!option.TryGetProperty("name", out var optionNameElement))
+                {
+                    continue;
+                }
+
+                if (!option.TryGetProperty("id", out var optionIdElement))
+                {
+                    continue;
+                }
+
+                var optionName = optionNameElement.GetString() ?? "";
+                var optionId = optionIdElement.GetString() ?? "";
+                if (!string.IsNullOrWhiteSpace(optionName) && !string.IsNullOrWhiteSpace(optionId))
+                {
+                    statusOptions[optionName] = optionId;
+                }
+            }
+        }
+
+        if (string.IsNullOrWhiteSpace(statusFieldId))
+        {
+            throw new InvalidOperationException("Status field not found in project.");
+        }
+
+        if (!project.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items'.");
+        }
+
+        if (!itemsElement.TryGetProperty("nodes", out var itemNodes) || itemNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items.nodes'.");
+        }
+
+        var items = new List<ProjectItemRef>();
+        foreach (var node in itemNodes.EnumerateArray())
+        {
+            if (!node.TryGetProperty("content", out var content) || content.ValueKind == System.Text.Json.JsonValueKind.Null)
+                continue;
+
+            if (!content.TryGetProperty("number", out var numberProp))
+                continue;
+
+            var issueNumber = numberProp.GetInt32();
+            var itemId = node.GetProperty("id").GetString() ?? "";
+            if (!string.IsNullOrWhiteSpace(itemId))
+            {
+                items.Add(new ProjectItemRef(itemId, issueNumber));
+            }
+        }
+
+        return new ProjectMetadata(projectId, statusFieldId, statusOptions, items);
+    }
+
+    private static System.Text.Json.JsonElement ResolveProjectElement(System.Text.Json.JsonDocument doc)
+    {
+        var data = doc.RootElement.GetProperty("data");
+        if (data.TryGetProperty("user", out var user) && user.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            user.TryGetProperty("projectV2", out var userProject) && userProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return userProject;
+        }
+
+        if (data.TryGetProperty("organization", out var org) && org.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            org.TryGetProperty("projectV2", out var orgProject) && orgProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return orgProject;
+        }
+
+        var errors = TryFormatGraphQlErrors(doc);
+        if (!string.IsNullOrWhiteSpace(errors))
+        {
+            throw new InvalidOperationException($"Project not found. GraphQL errors: {errors}");
+        }
+
+        throw new InvalidOperationException("Project not found.");
+    }
+
+    private static string? TryFormatGraphQlErrors(System.Text.Json.JsonDocument doc)
+    {
+        if (!doc.RootElement.TryGetProperty("errors", out var errors) || errors.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            return null;
+        }
+
+        var messages = new List<string>();
+        foreach (var error in errors.EnumerateArray())
+        {
+            if (error.TryGetProperty("message", out var message))
+            {
+                var text = message.GetString();
+                if (!string.IsNullOrWhiteSpace(text))
+                {
+                    messages.Add(text);
+                }
+            }
+        }
+
+        return messages.Count > 0 ? string.Join(" | ", messages) : null;
+    }
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-Dev-attempt-2.md b/orchestrator/prompts/issue-43-Dev-attempt-2.md
--- a/orchestrator/prompts/issue-43-Dev-attempt-2.md
+++ b/orchestrator/prompts/issue-43-Dev-attempt-2.md
@@ -0,0 +1,846 @@
+# System Prompt
+
+You are a software engineer implementing a spec. CRITICAL INSTRUCTIONS:
+1. Read the Touch List Entry to understand what operation to perform
+2. Study the Interfaces section which shows the required changes (before/after examples)
+3. Apply those exact changes to the Current File Content
+4. For 'Modify' operations: update/remove code as specified in the notes
+5. When removing code: COMPLETELY OMIT it from your output - do NOT include it with comments
+6. Output ONLY the complete updated file content
+7. Do NOT include before/after comments or explanations
+8. Do NOT preserve code marked for removal
+9. VERIFY your output does not contain any code that should be removed
+Follow the spec strictly. Code removal means the code must be absent from your output.
+
+# User Prompt
+
+Mode: minimal
+
+Spec Goal:
+Remove two dead methods from the GitHub client surface: CreateBranchAsync and DeleteBranchAsync. This reduces unused code, simplifies the interface, and does not change runtime behavior because branch operations are performed locally via LibGit2Sharp.
+
+=== TOUCH LIST ENTRY ===
+Operation: Modify
+File: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+Instructions: Remove corresponding implementations (around lines ~250-280)
+
+=== REQUIRED CHANGES (Before/After Examples) ===
+using System.Threading.Tasks;
+
+namespace Orchestrator.App.Core.Interfaces
+{
+    public interface IGitHubClient
+    {
+        Task<string> GetDefaultBranchNameAsync(string owner, string repo);
+        Task<RepositoryInfo> GetRepositoryAsync(string owner, string repo);
+
+        // DEAD: never used in codebase
+        Task CreateBranchAsync(string branchName);
+        Task DeleteBranchAsync(string branchName);
+
+        Task<IEnumerable<IssueInfo>> GetOpenIssuesAsync(string owner, string repo);
+    }
+}
+using System.Threading.Tasks;
+
+namespace Orchestrator.App.Core.Interfaces
+{
+    public interface IGitHubClient
+    {
+        Task<string> GetDefaultBranchNameAsync(string owner, string repo);
+        Task<RepositoryInfo> GetRepositoryAsync(string owner, string repo);
+        Task<IEnumerable<IssueInfo>> GetOpenIssuesAsync(string owner, string repo);
+    }
+}
+using System.Threading.Tasks;
+using Orchestrator.App.Core.Interfaces;
+
+namespace Orchestrator.App.Infrastructure.GitHub
+{
+    public class OctokitGitHubClient : IGitHubClient
+    {
+        // ... other members ...
+
+        // DEAD: not referenced anywhere
+        public async Task CreateBranchAsync(string branchName)
+        {
+            // Implementation using Octokit REST API to create a branch
+            var @ref = new NewReference($"refs/heads/{branchName}", baseSha);
+            await _client.Git.Reference.Create(owner, repo, @ref);
+        }
+
+        public async Task DeleteBranchAsync(string branchName)
+        {
+            // Implementation using Octokit REST API to delete a branch ref
+            await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");
+        }
+
+        // ... other members ...
+    }
+}
+using System.Threading.Tasks;
+using Orchestrator.App.Core.Interfaces;
+
+namespace Orchestrator.App.Infrastructure.GitHub
+{
+    public class OctokitGitHubClient : IGitHubClient
+    {
+        // ... other members ...
+
+        // CreateBranchAsync and DeleteBranchAsync removed as they were unused.
+
+        // ... other members ...
+    }
+}
+using System.Threading.Tasks;
+using Xunit;
+using Moq;
+using Orchestrator.App.Core.Interfaces;
+
+public class GitHubClientTests
+{
+    [Fact]
+    public async Task CreateBranchAsync_CallsApi()
+    {
+        // test body
+    }
+
+    [Fact]
+    public async Task DeleteBranchAsync_CallsApi()
+    {
+        // test body
+    }
+}
+using System.Threading.Tasks;
+using Xunit;
+using Moq;
+using Orchestrator.App.Core.Interfaces;
+
+public class GitHubClientTests
+{
+    // Tests for CreateBranchAsync/DeleteBranchAsync removed because methods no longer exist.
+    // Other tests (GetRepositoryAsync, GetDefaultBranchNameAsync, etc.) remain unchanged.
+}
+
+Scenarios:
+Scenario: Interface no longer exposes CreateBranchAsync/DeleteBranchAsync
+Given the codebase compiles,
+When IGitHubClient is inspected,
+Then CreateBranchAsync and DeleteBranchAsync are not present on IGitHubClient.
+
+Scenario: Implementation methods removed
+Given the OctokitGitHubClient implementation class,
+When the file is compiled after changes,
+Then there are no CreateBranchAsync or DeleteBranchAsync method implementations in OctokitGitHubClient.
+
+Scenario: Tests continue to pass
+Given the test suite is executed,
+When the dead methods and any tests referencing them are removed,
+Then all tests pass (no compilation or runtime failures due to removed methods).
+
+Scenario: No behavioral change for branch operations
+Given branch operations are performed via RepoGit.EnsureBranch and CommitAndPush,
+When creating and pushing a branch in existing workflows,
+Then behavior is unchanged because the removed API methods were never invoked.
+
+Sequence:
+Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs and remove the two method signatures.
+Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs and delete the corresponding method implementations.
+Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync and remove those test methods or files.
+Build solution and run test suite (xUnit) and fix any incidental compile errors (e.g., using directives only).
+Commit changes.
+
+Test Matrix:
+| Unit | tests/ | Run full xUnit suite after removal; ensure no test refers to removed methods |
+| Compile | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs<br>src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Ensure project compiles with removed signatures/implementations |
+
+=== CURRENT FILE CONTENT (TO BE MODIFIED) ===
+using System.Net.Http;
+using Octokit;
+using Orchestrator.App.Core.Configuration;
+using Orchestrator.App.Core.Models;
+using CoreIssueComment = Orchestrator.App.Core.Models.IssueComment;
+
+namespace Orchestrator.App.Infrastructure.GitHub;
+
+/// <summary>
+/// GitHub client using Octokit.NET for REST API and GraphQL operations
+/// </summary>
+internal sealed class OctokitGitHubClient : Orchestrator.App.Core.Interfaces.IGitHubClient
+{
+    private readonly Octokit.GitHubClient _octokit;
+    private readonly OrchestratorConfig _cfg;
+    private readonly HttpClient _http;
+
+    private string RepoOwner => _cfg.RepoOwner;
+    private string RepoName => _cfg.RepoName;
+
+    public OctokitGitHubClient(OrchestratorConfig cfg) : this(cfg, CreateDefaultGitHubClient(cfg), new HttpClient())
+    {
+    }
+
+    internal OctokitGitHubClient(OrchestratorConfig cfg, Octokit.GitHubClient octokitClient, HttpClient httpClient)
+    {
+        _cfg = cfg;
+        _octokit = octokitClient;
+        _http = httpClient;
+
+        // Configure HttpClient for GraphQL
+        _http.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("conjunction-orchestrator", "0.3"));
+        _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg.GitHubToken);
+        }
+    }
+
+    private static Octokit.GitHubClient CreateDefaultGitHubClient(OrchestratorConfig cfg)
+    {
+        var client = new Octokit.GitHubClient(new ProductHeaderValue("conjunction-orchestrator", "0.3"));
+
+        if (!string.IsNullOrWhiteSpace(cfg.GitHubToken))
+        {
+            client.Credentials = new Credentials(cfg.GitHubToken);
+        }
+
+        return client;
+    }
+
+    // ========================================
+    // PHASE 1: Core REST API (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Get open issues for the repository
+    /// </summary>
+    public async Task<IReadOnlyList<WorkItem>> GetOpenWorkItemsAsync(int perPage = 50)
+    {
+        var issueRequest = new RepositoryIssueRequest
+        {
+            State = ItemStateFilter.Open
+        };
+
+        var apiOptions = new ApiOptions
+        {
+            PageSize = perPage,
+            PageCount = 1
+        };
+
+        var issues = await _octokit.Issue.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            issueRequest,
+            apiOptions
+        );
+
+        // Convert Octokit Issues to WorkItems, excluding PRs
+        return issues
+            .Where(i => i.PullRequest == null)
+            .Select(i => new WorkItem(
+                Number: i.Number,
+                Title: i.Title,
+                Body: i.Body ?? string.Empty,
+                Url: i.HtmlUrl,
+                Labels: i.Labels.Select(l => l.Name).ToList()
+            ))
+            .ToList();
+    }
+
+    /// <summary>
+    /// Get labels for a specific issue
+    /// </summary>
+    public async Task<IReadOnlyList<string>> GetIssueLabelsAsync(int issueNumber)
+    {
+        var issue = await _octokit.Issue.Get(RepoOwner, RepoName, issueNumber);
+        return issue.Labels.Select(l => l.Name).ToList();
+    }
+
+    /// <summary>
+    /// Create a pull request
+    /// </summary>
+    public async Task<string> OpenPullRequestAsync(string headBranch, string baseBranch, string title, string body)
+    {
+        var newPr = new NewPullRequest(title, headBranch, baseBranch)
+        {
+            Body = body
+        };
+
+        var pr = await _octokit.PullRequest.Create(RepoOwner, RepoName, newPr);
+        return pr.HtmlUrl;
+    }
+
+    /// <summary>
+    /// Get PR number for a branch, or null if not found
+    /// </summary>
+    public async Task<int?> GetPullRequestNumberAsync(string branchName)
+    {
+        var prs = await _octokit.PullRequest.GetAllForRepository(
+            RepoOwner,
+            RepoName,
+            new PullRequestRequest { State = ItemStateFilter.All }
+        );
+
+        var pr = prs.FirstOrDefault(p => p.Head.Ref == branchName);
+        return pr?.Number;
+    }
+
+    /// <summary>
+    /// Close a pull request
+    /// </summary>
+    public async Task ClosePullRequestAsync(int prNumber)
+    {
+        var update = new PullRequestUpdate
+        {
+            State = ItemState.Closed
+        };
+
+        await _octokit.PullRequest.Update(RepoOwner, RepoName, prNumber, update);
+    }
+
+    /// <summary>
+    /// Get comments on an issue
+    /// </summary>
+    public async Task<IReadOnlyList<CoreIssueComment>> GetIssueCommentsAsync(int issueNumber)
+    {
+        var comments = await _octokit.Issue.Comment.GetAllForIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber
+        );
+
+        return comments.Select(c => new CoreIssueComment(
+            Author: c.User.Login,
+            Body: c.Body
+        )).ToList();
+    }
+
+    /// <summary>
+    /// Post a comment on an issue
+    /// </summary>
+    public async Task CommentOnWorkItemAsync(int issueNumber, string comment)
+    {
+        await _octokit.Issue.Comment.Create(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            comment
+        );
+    }
+
+    /// <summary>
+    /// Add labels to an issue
+    /// </summary>
+    public async Task AddLabelsAsync(int issueNumber, params string[] labels)
+    {
+        if (labels.Length == 0)
+            return;
+
+        await _octokit.Issue.Labels.AddToIssue(
+            RepoOwner,
+            RepoName,
+            issueNumber,
+            labels
+        );
+    }
+
+    /// <summary>
+    /// Remove a label from an issue
+    /// </summary>
+    public async Task RemoveLabelAsync(int issueNumber, string label)
+    {
+        try
+        {
+            await _octokit.Issue.Labels.RemoveFromIssue(
+                RepoOwner,
+                RepoName,
+                issueNumber,
+                label
+            );
+        }
+        catch (NotFoundException)
+        {
+            // Label doesn't exist on issue - ignore
+        }
+    }
+
+    /// <summary>
+    /// Remove multiple labels from an issue
+    /// </summary>
+    public async Task RemoveLabelsAsync(int issueNumber, params string[] labels)
+    {
+        foreach (var label in labels)
+        {
+            await RemoveLabelAsync(issueNumber, label);
+        }
+    }
+
+    /// <summary>
+    /// Get PR diff
+    /// </summary>
+    public async Task<string> GetPullRequestDiffAsync(int prNumber)
+    {
+        // Get files changed in the PR
+        var files = await _octokit.PullRequest.Files(RepoOwner, RepoName, prNumber);
+
+        // Build a simplified diff from file changes
+        var diff = new System.Text.StringBuilder();
+        foreach (var file in files)
+        {
+            diff.AppendLine($"diff --git a/{file.FileName} b/{file.FileName}");
+            diff.AppendLine($"--- a/{file.FileName}");
+            diff.AppendLine($"+++ b/{file.FileName}");
+            diff.AppendLine(file.Patch);
+            diff.AppendLine();
+        }
+
+        return diff.ToString();
+    }
+
+    // ========================================
+    // PHASE 3a: Git Operations (Octokit)
+    // ========================================
+
+    /// <summary>
+    /// Check if there are commits between base and head branches
+    /// </summary>
+    public async Task<bool> HasCommitsAsync(string baseBranch, string headBranch)
+    {
+        var comparison = await _octokit.Repository.Commit.Compare(RepoOwner, RepoName, baseBranch, headBranch);
+        return comparison.AheadBy > 0;
+    }
+
+    /// <summary>
+    /// Get file content from a branch
+    /// </summary>
+    public async Task<RepoFile?> TryGetFileContentAsync(string branch, string path)
+    {
+        try
+        {
+            var contents = await _octokit.Repository.Content.GetAllContentsByRef(RepoOwner, RepoName, path, branch);
+            if (contents.Count == 0)
+            {
+                return null;
+            }
+
+            var file = contents[0];
+            return new RepoFile(path, file.Content, file.Sha);
+        }
+        catch (NotFoundException)
+        {
+            return null;
+        }
+    }
+
+    /// <summary>
+    /// Create or update a file on a branch
+    /// </summary>
+    public async Task CreateOrUpdateFileAsync(string branch, string path, string content, string message)
+    {
+        var existing = await TryGetFileContentAsync(branch, path);
+
+        if (existing != null)
+        {
+            await _octokit.Repository.Content.UpdateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new UpdateFileRequest(message, content, existing.Sha, branch)
+            );
+        }
+        else
+        {
+            await _octokit.Repository.Content.CreateFile(
+                RepoOwner,
+                RepoName,
+                path,
+                new CreateFileRequest(message, content, branch)
+            );
+        }
+    }
+
+    // ========================================
+    // PHASE 3b: GraphQL Projects V2 (Raw GraphQL)
+    // ========================================
+
+    /// <summary>
+    /// Get GitHub Projects V2 snapshot
+    /// </summary>
+    public async Task<ProjectSnapshot> GetProjectSnapshotAsync(string owner, int projectNumber, ProjectOwnerType ownerType)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      ...ProjectFields
+    }
+  }
+}
+
+fragment ProjectFields on ProjectV2 {
+  title
+  items(first: 100) {
+    nodes {
+      content {
+        ... on Issue { number title url }
+        ... on DraftIssue { title }
+      }
+      fieldValues(first: 20) {
+        nodes {
+          ... on ProjectV2ItemFieldSingleSelectValue {
+            name
+            field { ... on ProjectV2SingleSelectField { name } }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+        var title = project.GetProperty("title").GetString() ?? "";
+        var items = new List<ProjectItem>();
+
+        foreach (var node in project.GetProperty("items").GetProperty("nodes").EnumerateArray())
+        {
+            string? itemTitle = null;
+            int? issueNumber = null;
+            string? itemUrl = null;
+
+            if (node.TryGetProperty("content", out var content) && content.ValueKind != System.Text.Json.JsonValueKind.Null)
+            {
+                if (content.TryGetProperty("title", out var ctTitle))
+                    itemTitle = ctTitle.GetString();
+                if (content.TryGetProperty("number", out var ctNumber))
+                    issueNumber = ctNumber.GetInt32();
+                if (content.TryGetProperty("url", out var ctUrl))
+                    itemUrl = ctUrl.GetString();
+            }
+
+            var status = "Unknown";
+            if (node.TryGetProperty("fieldValues", out var fields))
+            {
+                foreach (var fieldNode in fields.GetProperty("nodes").EnumerateArray())
+                {
+                    if (!fieldNode.TryGetProperty("field", out var field))
+                        continue;
+
+                    var fieldName = field.GetProperty("name").GetString();
+                    if (!string.Equals(fieldName, "Status", StringComparison.OrdinalIgnoreCase))
+                        continue;
+
+                    status = fieldNode.GetProperty("name").GetString() ?? status;
+                    break;
+                }
+            }
+
+            items.Add(new ProjectItem(itemTitle ?? "(untitled)", issueNumber, itemUrl, status));
+        }
+
+        return new ProjectSnapshot(owner, projectNumber, ownerType, title, items);
+    }
+
+    /// <summary>
+    /// Update project item status in GitHub Projects V2
+    /// </summary>
+    public async Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName)
+    {
+        var metadata = await GetProjectMetadataAsync(owner, projectNumber);
+        var item = metadata.Items.FirstOrDefault(i => i.IssueNumber == issueNumber);
+        if (item == null)
+        {
+            Logger.WriteLine($"Project item not found for issue #{issueNumber}.");
+            return;
+        }
+
+        if (!metadata.StatusOptions.TryGetValue(statusName, out var optionId))
+        {
+            Logger.WriteLine($"Status option '{statusName}' not found.");
+            return;
+        }
+
+        var mutation = @"
+mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
+  updateProjectV2ItemFieldValue(input: {
+    projectId: $projectId
+    itemId: $itemId
+    fieldId: $fieldId
+    value: { singleSelectOptionId: $optionId }
+  }) {
+    projectV2Item { id }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query = mutation,
+            variables = new
+            {
+                projectId = metadata.ProjectId,
+                itemId = item.ItemId,
+                fieldId = metadata.StatusFieldId,
+                optionId
+            }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project status update failed: {res.StatusCode} {txt}");
+        }
+    }
+
+    private async Task<ProjectMetadata> GetProjectMetadataAsync(string owner, int projectNumber)
+    {
+        var query = @"
+query($login: String!, $number: Int!) {
+  user(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+  organization(login: $login) {
+    projectV2(number: $number) {
+      id
+      fields(first: 50) {
+        nodes {
+          ... on ProjectV2SingleSelectField {
+            id
+            name
+            options { id name }
+          }
+        }
+      }
+      items(first: 100) {
+        nodes {
+          id
+          content {
+            ... on Issue { number }
+          }
+        }
+      }
+    }
+  }
+}";
+
+        var payload = System.Text.Json.JsonSerializer.Serialize(new
+        {
+            query,
+            variables = new { login = owner, number = projectNumber }
+        });
+
+        var res = await _http.PostAsync(
+            "https://api.github.com/graphql",
+            new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
+        );
+        var txt = await res.Content.ReadAsStringAsync();
+        if (!res.IsSuccessStatusCode)
+        {
+            throw new InvalidOperationException($"Project query failed: {res.StatusCode} {txt}");
+        }
+
+        var doc = System.Text.Json.JsonDocument.Parse(txt);
+        var project = ResolveProjectElement(doc);
+
+        var projectId = project.GetProperty("id").GetString() ?? "";
+        var statusFieldId = "";
+        var statusOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
+
+        if (!project.TryGetProperty("fields", out var fields) || fields.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields'.");
+        }
+
+        if (!fields.TryGetProperty("nodes", out var fieldNodes) || fieldNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'fields.nodes'.");
+        }
+
+        foreach (var field in fieldNodes.EnumerateArray())
+        {
+            if (!field.TryGetProperty("name", out var nameElement))
+            {
+                continue;
+            }
+
+            var name = nameElement.GetString();
+            if (!string.Equals(name, "Status", StringComparison.OrdinalIgnoreCase))
+                continue;
+
+            if (!field.TryGetProperty("id", out var idElement))
+            {
+                continue;
+            }
+
+            statusFieldId = idElement.GetString() ?? "";
+            if (!field.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != System.Text.Json.JsonValueKind.Array)
+            {
+                continue;
+            }
+
+            foreach (var option in optionsElement.EnumerateArray())
+            {
+                if (!option.TryGetProperty("name", out var optionNameElement))
+                {
+                    continue;
+                }
+
+                if (!option.TryGetProperty("id", out var optionIdElement))
+                {
+                    continue;
+                }
+
+                var optionName = optionNameElement.GetString() ?? "";
+                var optionId = optionIdElement.GetString() ?? "";
+                if (!string.IsNullOrWhiteSpace(optionName) && !string.IsNullOrWhiteSpace(optionId))
+                {
+                    statusOptions[optionName] = optionId;
+                }
+            }
+        }
+
+        if (string.IsNullOrWhiteSpace(statusFieldId))
+        {
+            throw new InvalidOperationException("Status field not found in project.");
+        }
+
+        if (!project.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind == System.Text.Json.JsonValueKind.Null)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items'.");
+        }
+
+        if (!itemsElement.TryGetProperty("nodes", out var itemNodes) || itemNodes.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            throw new InvalidOperationException("Project metadata missing 'items.nodes'.");
+        }
+
+        var items = new List<ProjectItemRef>();
+        foreach (var node in itemNodes.EnumerateArray())
+        {
+            if (!node.TryGetProperty("content", out var content) || content.ValueKind == System.Text.Json.JsonValueKind.Null)
+                continue;
+
+            if (!content.TryGetProperty("number", out var numberProp))
+                continue;
+
+            var issueNumber = numberProp.GetInt32();
+            var itemId = node.GetProperty("id").GetString() ?? "";
+            if (!string.IsNullOrWhiteSpace(itemId))
+            {
+                items.Add(new ProjectItemRef(itemId, issueNumber));
+            }
+        }
+
+        return new ProjectMetadata(projectId, statusFieldId, statusOptions, items);
+    }
+
+    private static System.Text.Json.JsonElement ResolveProjectElement(System.Text.Json.JsonDocument doc)
+    {
+        var data = doc.RootElement.GetProperty("data");
+        if (data.TryGetProperty("user", out var user) && user.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            user.TryGetProperty("projectV2", out var userProject) && userProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return userProject;
+        }
+
+        if (data.TryGetProperty("organization", out var org) && org.ValueKind != System.Text.Json.JsonValueKind.Null &&
+            org.TryGetProperty("projectV2", out var orgProject) && orgProject.ValueKind != System.Text.Json.JsonValueKind.Null)
+        {
+            return orgProject;
+        }
+
+        var errors = TryFormatGraphQlErrors(doc);
+        if (!string.IsNullOrWhiteSpace(errors))
+        {
+            throw new InvalidOperationException($"Project not found. GraphQL errors: {errors}");
+        }
+
+        throw new InvalidOperationException("Project not found.");
+    }
+
+    private static string? TryFormatGraphQlErrors(System.Text.Json.JsonDocument doc)
+    {
+        if (!doc.RootElement.TryGetProperty("errors", out var errors) || errors.ValueKind != System.Text.Json.JsonValueKind.Array)
+        {
+            return null;
+        }
+
+        var messages = new List<string>();
+        foreach (var error in errors.EnumerateArray())
+        {
+            if (error.TryGetProperty("message", out var message))
+            {
+                var text = message.GetString();
+                if (!string.IsNullOrWhiteSpace(text))
+                {
+                    messages.Add(text);
+                }
+            }
+        }
+
+        return messages.Count > 0 ? string.Join(" | ", messages) : null;
+    }
+}
+
+=== EXAMPLE: HOW TO REMOVE CODE ===
+If the instruction says 'Remove CreateBranchAsync method':
+
+BEFORE (current file content):
+  Task<bool> CreateBranchAsync(string branchName, string baseBranch);
+  Task DeleteFileAsync(string path);
+
+AFTER (your output should NOT include CreateBranchAsync):
+  Task DeleteFileAsync(string path);
+
+CRITICAL: When removing code:
+- DO NOT include removed methods/classes/properties in your output
+- DO NOT add comments like '// removed' or '// deleted'
+- Simply omit the code completely from your response
+
+=== YOUR TASK ===
+Apply the changes shown in 'REQUIRED CHANGES' section to the 'CURRENT FILE CONTENT'.
+Follow the instructions: Remove corresponding implementations (around lines ~250-280)
+Output the complete updated file content below:

diff --git a/orchestrator/prompts/issue-43-ProductOwner-attempt-1-response.md b/orchestrator/prompts/issue-43-ProductOwner-attempt-1-response.md
--- a/orchestrator/prompts/issue-43-ProductOwner-attempt-1-response.md
+++ b/orchestrator/prompts/issue-43-ProductOwner-attempt-1-response.md
@@ -0,0 +1,5 @@
+{
+  "question": "Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?",
+  "answer": "I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.",
+  "reasoning": "CANNOT_ANSWER: the story and files listed show IGitHubClient lives under src/Orchestrator.App and that the two methods are unused internally, but they do not state whether the assembly is packaged/published or consumed by external projects. To be certain, check the repository for: 1) any NuGet packaging config (nuspec, PackageReference with Version/publish steps, dotnet pack in CI), 2) CI/CD pipeline steps that publish a package, 3) other repositories or solutions that reference this package (internal package feed or public), 4) public API docs or README stating consumers, and 5) the assembly's access modifiers (is the interface public). If any external consumers exist, treat this as a breaking change and follow a deprecation policy (mark obsolete first or bump major version). If no external consumers, proceed with the minimal removal as specified."
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-ProductOwner-attempt-1.md b/orchestrator/prompts/issue-43-ProductOwner-attempt-1.md
--- a/orchestrator/prompts/issue-43-ProductOwner-attempt-1.md
+++ b/orchestrator/prompts/issue-43-ProductOwner-attempt-1.md
@@ -0,0 +1,85 @@
+# System Prompt
+
+You are a Product Owner in an SDLC workflow. Answer product/business questions based on requirements, use cases, and user workflows. Base your answers on the provided context. If you cannot answer confidently, say so explicitly. Return JSON only.
+
+# User Prompt
+
+Issue Context:
+Title: Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+Clarified Story:
+Remove two dead methods (CreateBranchAsync and DeleteBranchAsync) from the IGitHubClient interface and their implementations in OctokitGitHubClient, and remove any associated tests. These methods are unused in the repo because branch creation/deletion is handled locally via LibGit2Sharp and pushed via CommitAndPush. The change should be minimal: delete the two interface method signatures, delete their implementations in OctokitGitHubClient, remove any tests that reference them, and ensure the solution builds and tests pass.
+
+Acceptance Criteria:
+- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
+- Given the OctokitGitHubClient implementation, when the change is applied, then the CreateBranchAsync and DeleteBranchAsync method implementations must be removed and the project must compile successfully.
+- Given the test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
+- Given the repository codebase, when performing a text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references (aside from removed tests) to these method names.
+
+Original Requirements:
+## Problem
+
+`IGitHubClient` contains `CreateBranchAsync` and `DeleteBranchAsync` methods that are **never used** in the codebase:
+
+**Dead code:**
+- `IGitHubClient.CreateBranchAsync(string branchName)` 
+- `IGitHubClient.DeleteBranchAsync(string branchName)`
+- `OctokitGitHubClient.CreateBranchAsync(...)` (implementation)
+- `OctokitGitHubClient.DeleteBranchAsync(...)` (implementation)
+
+**Why they exist:**
+These methods create/delete branches via GitHub REST API.
+
+**Why they're unused:**
+Branch operations are done **locally via LibGit2Sharp** in `RepoGit.EnsureBranch()`:
+```csharp
+// ContextBuilderExecutor.cs:35
+WorkContext.Repo.EnsureBranch(branchName, baseBranch);
+
+// RepoGit.cs:82 - Fetches from remote
+Commands.Fetch(repo, "origin", ...);
+
+// RepoGit.cs:107-128 - Creates branch locally
+localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
+```
+
+Branches are pushed to remote during `CommitAndPush()`, not via API calls.
+
+## Proposal
+
+Remove dead code for YAGNI compliance and reduced surface area.
+
+## Acceptance Criteria
+
+1. Given `IGitHubClient` interface, when unused methods are removed, then `CreateBranchAsync` and `DeleteBranchAsync` no longer exist
+2. Given `OctokitGitHubClient` implementation, when unused methods are removed, then implementations are deleted
+3. Given the test suite, when dead code is removed, then all tests pass (no code depends on these methods)
+
+## Files to Update
+
+- `src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs` (remove interface methods)
+- `src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs` (remove implementations around lines 250-280)
+- `tests/` (remove any tests for these methods, if they exist)
+
+## Impact
+
+- Cleaner interface
+- Less dead code
+- YAGNI compliance
+- No functional change (methods were never called)
+
+Product Question:
+Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
+
+Guidelines:
+- Answer based on the requirements, acceptance criteria, and existing spec
+- Focus on user workflows, business logic, and expected behavior
+- Be specific and actionable
+- If the answer is not clear from context, state 'CANNOT_ANSWER' in reasoning
+
+Return JSON:
+{
+  "question": string (repeat the question),
+  "answer": string (your answer to the question),
+  "reasoning": string (brief explanation or 'CANNOT_ANSWER' if unsure)
+}

diff --git a/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1-response.md b/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1-response.md
--- a/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1-response.md
+++ b/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1-response.md
@@ -0,0 +1,5 @@
+{
+  "question": "Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?",
+  "type": "Product",
+  "reasoning": "The question is about the API surface and release/compatibility impact on external consumers — i.e., whether this is a public contract and thus a breaking-change decision. This concerns project scope, stakeholder impact and requirements, not an implementation detail."
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1.md b/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1.md
--- a/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1.md
+++ b/orchestrator/prompts/issue-43-QuestionClassifier-attempt-1.md
@@ -0,0 +1,102 @@
+# System Prompt
+
+You are a question classifier for an SDLC workflow. Classify questions as Technical, Product, or Ambiguous. Return JSON only.
+
+# User Prompt
+
+Issue Context:
+Title: Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+Body: ## Problem
+
+`IGitHubClient` contains `CreateBranchAsync` and `DeleteBranchAsync` methods that are **never used** in the codebase:
+
+**Dead code:**
+- `IGitHubClient.CreateBranchAsync(string branchName)` 
+- `IGitHubClient.DeleteBranchAsync(string branchName)`
+- `OctokitGitHubClient.CreateBranchAsync(...)` (implementation)
+- `OctokitGitHubClient.DeleteBranchAsync(...)` (implementation)
+
+**Why they exist:**
+These methods create/delete branches via GitHub REST API.
+
+**Why they're unused:**
+Branch operations are done **locally via LibGit2Sharp** in `RepoGit.EnsureBranch()`:
+```csharp
+// ContextBuilderExecutor.cs:35
+WorkContext.Repo.EnsureBranch(branchName, baseBranch);
+
+// RepoGit.cs:82 - Fetches from remote
+Commands.Fetch(repo, "origin", ...);
+
+// RepoGit.cs:107-128 - Creates branch locally
+localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
+```
+
+Branches are pushed to remote during `CommitAndPush()`, not via API calls.
+
+## Proposal
+
+Remove dead code for YAGNI compliance and reduced surface area.
+
+## Acceptance Criteria
+
+1. Given `IGitHubClient` interface, when unused methods are removed, then `CreateBranchAsync` and `DeleteBranchAsync` no longer exist
+2. Given `OctokitGitHubClient` implementation, when unused methods are removed, then implementations are deleted
+3. Given the test suite, when dead code is removed, then all tests pass (no code depends on these methods)
+
+## Files to Update
+
+- `src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs` (remove interface methods)
+- `src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs` (remove implementations around lines 250-280)
+- `tests/` (remove any tests for these methods, if they exist)
+
+## Impact
+
+- Cleaner interface
+- Less dead code
+- YAGNI compliance
+- No functional change (methods were never called)
+
+Clarified Story:
+Remove two dead methods (CreateBranchAsync and DeleteBranchAsync) from the IGitHubClient interface and their implementations in OctokitGitHubClient, and remove any associated tests. These methods are unused in the repo because branch creation/deletion is handled locally via LibGit2Sharp and pushed via CommitAndPush. The change should be minimal: delete the two interface method signatures, delete their implementations in OctokitGitHubClient, remove any tests that reference them, and ensure the solution builds and tests pass.
+
+Acceptance Criteria:
+- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
+- Given the OctokitGitHubClient implementation, when the change is applied, then the CreateBranchAsync and DeleteBranchAsync method implementations must be removed and the project must compile successfully.
+- Given the test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
+- Given the repository codebase, when performing a text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references (aside from removed tests) to these method names.
+
+Question to Classify:
+Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
+
+Classification Guidelines:
+
+TECHNICAL questions are about:
+- Implementation details (how to code something)
+- Architecture decisions (which pattern, structure)
+- Framework/library choices (which tool to use)
+- Code organization (where to put files)
+- Error handling strategies
+- Performance considerations
+Examples: 'Which framework?', 'How should errors be handled?', 'What's the API structure?'
+
+PRODUCT questions are about:
+- User workflows (how users interact)
+- Business logic (what should happen)
+- Use cases (when/why feature is used)
+- Requirements clarification (what exactly is needed)
+- User expectations (what users see/experience)
+- Feature behavior (how feature should work)
+Examples: 'What happens when user clicks X?', 'What's the expected behavior?', 'Which users can access this?'
+
+AMBIGUOUS questions:
+- Can't be clearly classified
+- Require human judgment
+- Mix technical and product concerns
+
+Return JSON:
+{
+  "question": string (the question being classified),
+  "type": "Technical" | "Product" | "Ambiguous",
+  "reasoning": string (brief explanation of classification)
+}

diff --git a/orchestrator/prompts/issue-43-Refinement-attempt-1-response.md b/orchestrator/prompts/issue-43-Refinement-attempt-1-response.md
--- a/orchestrator/prompts/issue-43-Refinement-attempt-1-response.md
+++ b/orchestrator/prompts/issue-43-Refinement-attempt-1-response.md
@@ -0,0 +1,25 @@
+{
+  "clarifiedStory": "Remove two dead methods from the internal GitHub client surface: delete CreateBranchAsync(string) and DeleteBranchAsync(string) from IGitHubClient, remove their implementations from OctokitGitHubClient, and remove any unit tests that reference them. This is a minimal refactor: edit the interface file to remove the two signatures, delete the corresponding method bodies from the OctokitGitHubClient class, remove any tests that reference these methods, then build and run tests to verify no compilation or test failures. If IGitHubClient is part of a public/distributed API consumed by external projects, do NOT merge without coordinating a breaking-change plan; the ProductOwner could not determine whether it is published externally, so that check must be performed before merging.",
+  "acceptanceCriteria": [
+    "Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).",
+    "Given the OctokitGitHubClient implementation, when CreateBranchAsync and DeleteBranchAsync implementations are removed and the solution is built, then dotnet build must complete successfully with no missing-member compile errors.",
+    "Given the repository test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by the removal of CreateBranchAsync/DeleteBranchAsync.",
+    "Given the repository codebase after removal, when performing a code/text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references to these method names (interface, implementation, or tests)."
+  ],
+  "openQuestions": [],
+  "complexitySignals": [
+    "Small code change: removing two method signatures and two implementations",
+    "Requires full solution build to catch missing-member compile errors",
+    "Requires running full test suite to ensure no tests reference the removed methods",
+    "Potential breaking change if IGitHubClient is part of a public/distributed API consumed externally (unknown from ProductOwner)"
+  ],
+  "complexitySummary": "Low technical complexity: change is limited to removing two interface members, deleting two corresponding implementations, and removing any tests that reference them. Risk is low for internal-only code, but medium if IGitHubClient is a published public API because removal would be breaking; confirm API/publication status before merging. Run a full build and the xUnit test suite to validate.",
+  "answeredQuestions": [
+    {
+      "questionNumber": 1,
+      "question": "Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?",
+      "answer": "I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.",
+      "answeredBy": "ProductOwner"
+    }
+  ]
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-Refinement-attempt-1.md b/orchestrator/prompts/issue-43-Refinement-attempt-1.md
--- a/orchestrator/prompts/issue-43-Refinement-attempt-1.md
+++ b/orchestrator/prompts/issue-43-Refinement-attempt-1.md
@@ -0,0 +1,369 @@
+# System Prompt
+
+You are an SDLC refinement assistant following the MINIMAL FIRST principle. CORE PRINCIPLES:
+1. START MINIMAL: Always propose the simplest solution that satisfies the requirement
+2. NO FUTURE-PROOFING: Do not add features, config, or abstractions for hypothetical scenarios
+3. ONE FEATURE PER ISSUE: If the issue mixes multiple concerns, ask user to split it
+4. MAX 3-5 ACCEPTANCE CRITERIA: More criteria = issue too large, should be split
+5. AFTER INCORPORATING ANSWERS: Return ZERO open questions. Do not generate new questions.
+
+Do not invent requirements. Clarify ambiguity and produce structured JSON only. CRITICAL: All acceptance criteria MUST be testable using BDD format (Given/When/Then) or keywords (should, must, verify, ensure).
+
+# User Prompt
+
+PRODUCT VISION:
+- Quality over speed: Slow and correct beats fast and broken
+- Minimal viable first: Start with simplest solution, extend only when reviews request
+- Small focused issues: One feature, max 3-5 acceptance criteria
+- No over-engineering: No abstractions for single use, no speculative features
+
+Issue Title:
+Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+Issue Body:
+## Problem
+
+`IGitHubClient` contains `CreateBranchAsync` and `DeleteBranchAsync` methods that are **never used** in the codebase:
+
+**Dead code:**
+- `IGitHubClient.CreateBranchAsync(string branchName)` 
+- `IGitHubClient.DeleteBranchAsync(string branchName)`
+- `OctokitGitHubClient.CreateBranchAsync(...)` (implementation)
+- `OctokitGitHubClient.DeleteBranchAsync(...)` (implementation)
+
+**Why they exist:**
+These methods create/delete branches via GitHub REST API.
+
+**Why they're unused:**
+Branch operations are done **locally via LibGit2Sharp** in `RepoGit.EnsureBranch()`:
+```csharp
+// ContextBuilderExecutor.cs:35
+WorkContext.Repo.EnsureBranch(branchName, baseBranch);
+
+// RepoGit.cs:82 - Fetches from remote
+Commands.Fetch(repo, "origin", ...);
+
+// RepoGit.cs:107-128 - Creates branch locally
+localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
+```
+
+Branches are pushed to remote during `CommitAndPush()`, not via API calls.
+
+## Proposal
+
+Remove dead code for YAGNI compliance and reduced surface area.
+
+## Acceptance Criteria
+
+1. Given `IGitHubClient` interface, when unused methods are removed, then `CreateBranchAsync` and `DeleteBranchAsync` no longer exist
+2. Given `OctokitGitHubClient` implementation, when unused methods are removed, then implementations are deleted
+3. Given the test suite, when dead code is removed, then all tests pass (no code depends on these methods)
+
+## Files to Update
+
+- `src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs` (remove interface methods)
+- `src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs` (remove implementations around lines 250-280)
+- `tests/` (remove any tests for these methods, if they exist)
+
+## Impact
+
+- Cleaner interface
+- Less dead code
+- YAGNI compliance
+- No functional change (methods were never called)
+
+Answered Questions (1 total):
+
+- [x] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
+  **Answer (ProductOwner):** I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.
+
+IMPORTANT: Incorporate these answers into your refinement.
+Return openQuestions: [] (empty array) in your JSON response.
+
+Previous Refinement:
+# Refinement: Issue #43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+**Status**: Refinement Complete
+**Generated**: 2026-01-11 14:31:56 UTC
+
+## Clarified Story
+
+Remove two unused methods from the internal GitHub client surface: delete CreateBranchAsync(string) and DeleteBranchAsync(string) from IGitHubClient, remove their implementations from OctokitGitHubClient, and remove any unit tests that reference them. The change is minimal: only remove the interface signatures, the corresponding method bodies, and any direct tests; then ensure the solution builds and the test suite passes. Note: the ProductOwner could not determine whether IGitHubClient is published or consumed externally; if it is part of a public API or distributed NuGet package this change would be breaking and must not be merged without coordinating a major-version change or alternative compatibility plan.
+
+## Acceptance Criteria (4)
+
+- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
+- Given the OctokitGitHubClient implementation file, when the CreateBranchAsync and DeleteBranchAsync methods are removed, then dotnet build must complete successfully for the solution (no missing-member compile errors).
+- Given the repository test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
+- Given the repository codebase after removing the methods and any tests referencing them, when performing a code/text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references to these method names.
+
+## Questions
+
+**How to answer:**
+1. Edit this file and add your answer after the question
+2. Mark the checkbox with [x] when answered
+3. Commit and push changes
+4. Remove `blocked` label and add `dor` label to re-trigger
+
+- [x] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
+  **Answer (ProductOwner):** I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.
+
+
+
+Playbook Constraints:
+- Core Principles:
+  - Minimal First: Always start with simplest solution that satisfies requirements
+  - No Future-Proofing: Do not add features, config, or abstractions for hypothetical scenarios
+  - Small Focused Issues: One feature per issue, max 3-5 acceptance criteria
+  - Follow Existing Patterns: Copy structure from similar code in codebase before inventing new patterns
+  - Quality Over Speed: Slow and correct beats fast and broken
+- Allowed Frameworks:
+  - .NET 8 (FW-01)
+  - xUnit (FW-02)
+  - Moq (FW-03)
+- Forbidden Frameworks:
+  - Newtonsoft.Json
+- Allowed Patterns:
+  - Clean Architecture (PAT-01)
+  - Repository Pattern (PAT-02)
+  - Records for DTOs (PAT-03)
+  - Dependency Injection (PAT-04)
+- Forbidden Patterns:
+  - Singleton (ANTI-01)
+  - God Objects (ANTI-02)
+  - Premature Abstraction (ANTI-03)
+  - Config Overload (ANTI-04)
+  - Speculative Features (ANTI-05)
+
+Existing Spec (if any):
+# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+STATUS: DRAFT  
+UPDATED: 2026-01-11 14:31:57 UTC
+
+## Goal
+Remove two dead methods from the GitHub client surface: CreateBranchAsync and DeleteBranchAsync. This reduces unused code, simplifies the interface, and does not change runtime behavior because branch operations are performed locally via LibGit2Sharp.
+
+## Non-Goals
+- Replacing local branch operations with API calls
+- Adding alternate implementations or abstractions
+- Introducing configuration or feature flags
+
+## Components
+- Core/Interfaces/IGitHubClient.cs (interface)
+- Infrastructure/GitHub/OctokitGitHubClient.cs (concrete implementation)
+- tests/ (remove any tests that reference the removed methods)
+
+## Touch List
+| Operation | Path | Notes |
+| --- | --- | --- |
+| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync/DeleteBranchAsync signatures |
+| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove corresponding implementations (around lines ~250-280) |
+| Modify | tests/ | Remove any unit tests that assert/Create/Delete branch via IGitHubClient (if present) |
+
+## Interfaces
+// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+
+namespace Orchestrator.App.Core.Interfaces
+{
+    public interface IGitHubClient
+    {
+        Task<string> GetDefaultBranchNameAsync(string owner, string repo);
+        Task<RepositoryInfo> GetRepositoryAsync(string owner, string repo);
+
+        // DEAD: never used in codebase
+        Task CreateBranchAsync(string branchName);
+        Task DeleteBranchAsync(string branchName);
+
+        Task<IEnumerable<IssueInfo>> GetOpenIssuesAsync(string owner, string repo);
+    }
+}
+```
+
+// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+
+namespace Orchestrator.App.Core.Interfaces
+{
+    public interface IGitHubClient
+    {
+        Task<string> GetDefaultBranchNameAsync(string owner, string repo);
+        Task<RepositoryInfo> GetRepositoryAsync(string owner, string repo);
+        Task<IEnumerable<IssueInfo>> GetOpenIssuesAsync(string owner, string repo);
+    }
+}
+```
+
+// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+using Orchestrator.App.Core.Interfaces;
+
+namespace Orchestrator.App.Infrastructure.GitHub
+{
+    public class OctokitGitHubClient : IGitHubClient
+    {
+        // ... other members ...
+
+        // DEAD: not referenced anywhere
+        public async Task CreateBranchAsync(string branchName)
+        {
+            // Implementation using Octokit REST API to create a branch
+            var @ref = new NewReference($"refs/heads/{branchName}", baseSha);
+            await _client.Git.Reference.Create(owner, repo, @ref);
+        }
+
+        public async Task DeleteBranchAsync(string branchName)
+        {
+            // Implementation using Octokit REST API to delete a branch ref
+            await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");
+        }
+
+        // ... other members ...
+    }
+}
+```
+
+// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+using Orchestrator.App.Core.Interfaces;
+
+namespace Orchestrator.App.Infrastructure.GitHub
+{
+    public class OctokitGitHubClient : IGitHubClient
+    {
+        // ... other members ...
+
+        // CreateBranchAsync and DeleteBranchAsync removed as they were unused.
+
+        // ... other members ...
+    }
+}
+```
+
+// BEFORE: tests/ (example test methods that may exist)
+```csharp
+using System.Threading.Tasks;
+using Xunit;
+using Moq;
+using Orchestrator.App.Core.Interfaces;
+
+public class GitHubClientTests
+{
+    [Fact]
+    public async Task CreateBranchAsync_CallsApi()
+    {
+        // test body
+    }
+
+    [Fact]
+    public async Task DeleteBranchAsync_CallsApi()
+    {
+        // test body
+    }
+}
+```
+
+// AFTER: tests/ (those tests removed)
+```csharp
+using System.Threading.Tasks;
+using Xunit;
+using Moq;
+using Orchestrator.App.Core.Interfaces;
+
+public class GitHubClientTests
+{
+    // Tests for CreateBranchAsync/DeleteBranchAsync removed because methods no longer exist.
+    // Other tests (GetRepositoryAsync, GetDefaultBranchNameAsync, etc.) remain unchanged.
+}
+```
+
+## Scenarios
+
+Scenario: Interface no longer exposes CreateBranchAsync/DeleteBranchAsync
+Given the codebase compiles,
+When IGitHubClient is inspected,
+Then CreateBranchAsync and DeleteBranchAsync are not present on IGitHubClient.
+
+Scenario: Implementation methods removed
+Given the OctokitGitHubClient implementation class,
+When the file is compiled after changes,
+Then there are no CreateBranchAsync or DeleteBranchAsync method implementations in OctokitGitHubClient.
+
+Scenario: Tests continue to pass
+Given the test suite is executed,
+When the dead methods and any tests referencing them are removed,
+Then all tests pass (no compilation or runtime failures due to removed methods).
+
+Scenario: No behavioral change for branch operations
+Given branch operations are performed via RepoGit.EnsureBranch and CommitAndPush,
+When creating and pushing a branch in existing workflows,
+Then behavior is unchanged because the removed API methods were never invoked.
+
+## Sequence
+1. Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs and remove the two method signatures.
+2. Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs and delete the corresponding method implementations.
+3. Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync and remove those test methods or files.
+4. Build solution and run test suite (xUnit) and fix any incidental compile errors (e.g., using directives only).
+5. Commit changes.
+
+## Test Matrix
+| Test | Files | Notes |
+| --- | --- | --- |
+| Unit | tests/ | Run full xUnit suite after removal; ensure no test refers to removed methods |
+| Compile | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs<br>src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Ensure project compiles with removed signatures/implementations |
+
+DESIGN CHECKLIST - Before finalizing spec:
+- Is this the SIMPLEST design that satisfies acceptance criteria?
+  - Yes. We perform minimal removals only.
+- Are you adding abstractions/interfaces?
+  - No. We remove unused members; no new abstractions added.
+- Are you adding config?
+  - No.
+- Did you copy patterns from existing similar code?
+  - Yes. Follow existing class/interface layout; no new patterns introduced.
+- Will implementation fit file size limits?
+  - Yes — only small deletions required.
+
+Notes:
+- This change follows MINIMAL FIRST: remove dead API surface without introducing behavior changes.  
+- Testing: use existing xUnit test runner (FW-02).
+
+IMPORTANT - Acceptance Criteria Requirements:
+- You MUST write at least 3 testable acceptance criteria
+- Each criterion MUST use BDD format or testable keywords
+- BDD format: 'Given [context], when [action], then [outcome]'
+- Testable keywords: 'should', 'must', 'verify', 'ensure', 'given', 'when', 'then'
+- Each criterion must be specific, verifiable, and testable
+
+Examples of VALID acceptance criteria:
+  ✓ 'Given a user is logged in, when they click logout, then they should be redirected to the login page'
+  ✓ 'The system must validate email format before saving'
+  ✓ 'Should display error message when required fields are empty'
+  ✓ 'Given invalid credentials, when user attempts login, then access must be denied'
+  ✓ 'The API must return 401 status code for unauthorized requests'
+
+Examples of INVALID acceptance criteria (will be rejected):
+  ✗ 'User can log out' (not testable - no verification criteria)
+  ✗ 'Good error handling' (vague, not verifiable)
+  ✗ 'Works correctly' (not specific)
+
+SCOPE CHECK - Before finalizing, verify:
+- Does this issue implement ONE clear feature? If not, suggest splitting.
+- Are there 3-5 acceptance criteria? More than 5 = too large, suggest splitting.
+- Are you adding config/features for 'what if' scenarios? Remove them.
+- Can the solution be simpler? If yes, simplify.
+
+Return JSON with fields:
+{
+  "clarifiedStory": string,
+  "acceptanceCriteria": [string],
+  "openQuestions": [string],  // IMPORTANT: Do NOT include 'Question #X:' prefix - just the question text
+  "complexitySignals": [string],
+  "complexitySummary": string,
+  "answeredQuestions": [{ "questionNumber": int, "question": string, "answer": string, "answeredBy": string }] (optional)
+}

diff --git a/orchestrator/prompts/issue-43-Refinement-attempt-2-response.md b/orchestrator/prompts/issue-43-Refinement-attempt-2-response.md
--- a/orchestrator/prompts/issue-43-Refinement-attempt-2-response.md
+++ b/orchestrator/prompts/issue-43-Refinement-attempt-2-response.md
@@ -0,0 +1,26 @@
+{
+  "clarifiedStory": "Remove two dead methods from the internal GitHub client surface: delete CreateBranchAsync(string) and DeleteBranchAsync(string) from IGitHubClient, remove their implementations from OctokitGitHubClient, and remove any tests that reference them. These methods are unused because branch creation/deletion is performed locally via LibGit2Sharp and pushed via CommitAndPush. The change must be minimal: only remove the interface signatures, the corresponding method bodies, and any direct tests; then ensure the solution builds and the test suite passes.",
+  "acceptanceCriteria": [
+    "Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).",
+    "Given the OctokitGitHubClient implementation file, when the change is applied, then the CreateBranchAsync and DeleteBranchAsync method implementations must be removed and the project must compile successfully (dotnet build returns success).",
+    "Given the repository test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.",
+    "Given the repository codebase after removing the methods and any tests referencing them, when performing a text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references to these method names."
+  ],
+  "openQuestions": [],
+  "complexitySignals": [
+    "Code-level usage: The methods are unused within this repository (low internal risk).",
+    "Files to edit are small and known: IGitHubClient interface and OctokitGitHubClient implementation (low touch).",
+    "Tests: Any tests referencing these methods must be removed or updated; running the full test suite is required to validate no hidden dependencies.",
+    "Build verification: A successful 'dotnet build' and full test run are required to confirm no compilation or test regressions.",
+    "Release risk: It is unknown whether IGitHubClient is part of a public/published API or NuGet package; removing methods would be breaking for external consumers (moderate external compatibility risk)."
+  ],
+  "complexitySummary": "This is a small, low-complexity refactor limited to removing two unused interface methods and their implementations, plus any tests that reference them. Internal risk is low because the methods are unused in the codebase; however there is a moderate external risk if IGitHubClient is part of a published public API consumed outside the repository. Validation steps: remove the two method signatures from IGitHubClient, remove the two implementations from OctokitGitHubClient, delete or update any tests referencing them, run 'dotnet build' and the full test suite, and perform a repo-wide search to ensure no remaining references.",
+  "answeredQuestions": [
+    {
+      "questionNumber": 1,
+      "question": "Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?",
+      "answer": "I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.",
+      "answeredBy": "ProductOwner"
+    }
+  ]
+}
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-Refinement-attempt-2.md b/orchestrator/prompts/issue-43-Refinement-attempt-2.md
--- a/orchestrator/prompts/issue-43-Refinement-attempt-2.md
+++ b/orchestrator/prompts/issue-43-Refinement-attempt-2.md
@@ -0,0 +1,177 @@
+# System Prompt
+
+You are an SDLC refinement assistant following the MINIMAL FIRST principle. CORE PRINCIPLES:
+1. START MINIMAL: Always propose the simplest solution that satisfies the requirement
+2. NO FUTURE-PROOFING: Do not add features, config, or abstractions for hypothetical scenarios
+3. ONE FEATURE PER ISSUE: If the issue mixes multiple concerns, ask user to split it
+4. MAX 3-5 ACCEPTANCE CRITERIA: More criteria = issue too large, should be split
+5. AFTER INCORPORATING ANSWERS: Return ZERO open questions. Do not generate new questions.
+
+Do not invent requirements. Clarify ambiguity and produce structured JSON only. CRITICAL: All acceptance criteria MUST be testable using BDD format (Given/When/Then) or keywords (should, must, verify, ensure).
+
+# User Prompt
+
+PRODUCT VISION:
+- Quality over speed: Slow and correct beats fast and broken
+- Minimal viable first: Start with simplest solution, extend only when reviews request
+- Small focused issues: One feature, max 3-5 acceptance criteria
+- No over-engineering: No abstractions for single use, no speculative features
+
+Issue Title:
+Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+Issue Body:
+## Problem
+
+`IGitHubClient` contains `CreateBranchAsync` and `DeleteBranchAsync` methods that are **never used** in the codebase:
+
+**Dead code:**
+- `IGitHubClient.CreateBranchAsync(string branchName)` 
+- `IGitHubClient.DeleteBranchAsync(string branchName)`
+- `OctokitGitHubClient.CreateBranchAsync(...)` (implementation)
+- `OctokitGitHubClient.DeleteBranchAsync(...)` (implementation)
+
+**Why they exist:**
+These methods create/delete branches via GitHub REST API.
+
+**Why they're unused:**
+Branch operations are done **locally via LibGit2Sharp** in `RepoGit.EnsureBranch()`:
+```csharp
+// ContextBuilderExecutor.cs:35
+WorkContext.Repo.EnsureBranch(branchName, baseBranch);
+
+// RepoGit.cs:82 - Fetches from remote
+Commands.Fetch(repo, "origin", ...);
+
+// RepoGit.cs:107-128 - Creates branch locally
+localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
+```
+
+Branches are pushed to remote during `CommitAndPush()`, not via API calls.
+
+## Proposal
+
+Remove dead code for YAGNI compliance and reduced surface area.
+
+## Acceptance Criteria
+
+1. Given `IGitHubClient` interface, when unused methods are removed, then `CreateBranchAsync` and `DeleteBranchAsync` no longer exist
+2. Given `OctokitGitHubClient` implementation, when unused methods are removed, then implementations are deleted
+3. Given the test suite, when dead code is removed, then all tests pass (no code depends on these methods)
+
+## Files to Update
+
+- `src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs` (remove interface methods)
+- `src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs` (remove implementations around lines 250-280)
+- `tests/` (remove any tests for these methods, if they exist)
+
+## Impact
+
+- Cleaner interface
+- Less dead code
+- YAGNI compliance
+- No functional change (methods were never called)
+
+Answered Questions (1 total):
+
+- [x] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
+  **Answer (ProductOwner):** I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.
+
+IMPORTANT: Incorporate these answers into your refinement.
+Return openQuestions: [] (empty array) in your JSON response.
+
+Previous Refinement:
+# Refinement: Issue #43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+**Status**: Refinement Complete
+**Generated**: 2026-01-11 13:21:46 UTC
+
+## Clarified Story
+
+Remove two dead methods (CreateBranchAsync and DeleteBranchAsync) from the IGitHubClient interface and their implementations in OctokitGitHubClient, and remove any associated tests. These methods are unused in the repo because branch creation/deletion is handled locally via LibGit2Sharp and pushed via CommitAndPush. The change should be minimal: delete the two interface method signatures, delete their implementations in OctokitGitHubClient, remove any tests that reference them, and ensure the solution builds and tests pass.
+
+## Acceptance Criteria (4)
+
+- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
+- Given the OctokitGitHubClient implementation, when the change is applied, then the CreateBranchAsync and DeleteBranchAsync method implementations must be removed and the project must compile successfully.
+- Given the test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
+- Given the repository codebase, when performing a text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references (aside from removed tests) to these method names.
+
+## Questions
+
+**How to answer:**
+1. Edit this file and add your answer after the question
+2. Mark the checkbox with [x] when answered
+3. Commit and push changes
+4. Remove `blocked` label and add `dor` label to re-trigger
+
+- [ ] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
+  **Answer:** _[Pending]_
+
+- [ ] **Question #2:** Should I remove any unit/integration tests that reference these methods, or do you prefer converting them to assert that branch operations occur via LibGit2Sharp instead?
+  **Answer:** _[Pending]_
+
+
+
+Playbook Constraints:
+- Core Principles:
+  - Minimal First: Always start with simplest solution that satisfies requirements
+  - No Future-Proofing: Do not add features, config, or abstractions for hypothetical scenarios
+  - Small Focused Issues: One feature per issue, max 3-5 acceptance criteria
+  - Follow Existing Patterns: Copy structure from similar code in codebase before inventing new patterns
+  - Quality Over Speed: Slow and correct beats fast and broken
+- Allowed Frameworks:
+  - .NET 8 (FW-01)
+  - xUnit (FW-02)
+  - Moq (FW-03)
+- Forbidden Frameworks:
+  - Newtonsoft.Json
+- Allowed Patterns:
+  - Clean Architecture (PAT-01)
+  - Repository Pattern (PAT-02)
+  - Records for DTOs (PAT-03)
+  - Dependency Injection (PAT-04)
+- Forbidden Patterns:
+  - Singleton (ANTI-01)
+  - God Objects (ANTI-02)
+  - Premature Abstraction (ANTI-03)
+  - Config Overload (ANTI-04)
+  - Speculative Features (ANTI-05)
+
+Existing Spec (if any):
+None
+
+IMPORTANT - Acceptance Criteria Requirements:
+- You MUST write at least 3 testable acceptance criteria
+- Each criterion MUST use BDD format or testable keywords
+- BDD format: 'Given [context], when [action], then [outcome]'
+- Testable keywords: 'should', 'must', 'verify', 'ensure', 'given', 'when', 'then'
+- Each criterion must be specific, verifiable, and testable
+
+Examples of VALID acceptance criteria:
+  ✓ 'Given a user is logged in, when they click logout, then they should be redirected to the login page'
+  ✓ 'The system must validate email format before saving'
+  ✓ 'Should display error message when required fields are empty'
+  ✓ 'Given invalid credentials, when user attempts login, then access must be denied'
+  ✓ 'The API must return 401 status code for unauthorized requests'
+
+Examples of INVALID acceptance criteria (will be rejected):
+  ✗ 'User can log out' (not testable - no verification criteria)
+  ✗ 'Good error handling' (vague, not verifiable)
+  ✗ 'Works correctly' (not specific)
+
+SCOPE CHECK - Before finalizing, verify:
+- Does this issue implement ONE clear feature? If not, suggest splitting.
+- Are there 3-5 acceptance criteria? More than 5 = too large, suggest splitting.
+- Are you adding config/features for 'what if' scenarios? Remove them.
+- Can the solution be simpler? If yes, simplify.
+
+Return JSON with fields:
+{
+  "clarifiedStory": string,
+  "acceptanceCriteria": [string],
+  "openQuestions": [string],  // IMPORTANT: Do NOT include 'Question #X:' prefix - just the question text
+  "complexitySignals": [string],
+  "complexitySummary": string,
+  "answeredQuestions": [{ "questionNumber": int, "question": string, "answer": string, "answeredBy": string }] (optional)
+}

diff --git a/orchestrator/prompts/issue-43-TechLead-attempt-1-response.md b/orchestrator/prompts/issue-43-TechLead-attempt-1-response.md
--- a/orchestrator/prompts/issue-43-TechLead-attempt-1-response.md
+++ b/orchestrator/prompts/issue-43-TechLead-attempt-1-response.md
@@ -0,0 +1,218 @@
+# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+STATUS: DRAFT
+UPDATED: 2026-01-11 14:43:08 UTC
+
+## Goal
+Remove two unused GitHub branch API methods from the codebase to reduce dead surface area: IGitHubClient.CreateBranchAsync and IGitHubClient.DeleteBranchAsync, and the corresponding implementations in OctokitGitHubClient. Ensure tests are updated if any referenced these methods so the test suite remains green.
+
+## Non-Goals
+- Replacing branch operations with a new mechanism
+- Adding new abstractions or alternative branch APIs
+- Changing any behavior of RepoGit or CommitAndPush flow
+
+## Components
+- Core/Interfaces: IGitHubClient (interface removal)
+- Infrastructure/GitHub: OctokitGitHubClient (remove implementations)
+- tests/: Remove any unit tests that target the removed methods
+
+## Touch List
+| Operation | Path | Notes |
+| --- | --- | --- |
+| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync method declarations |
+| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove corresponding method implementations (around lines ~250-280) |
+| Modify | tests/GitHubClientTests.cs | Remove tests that reference CreateBranchAsync/DeleteBranchAsync, if present; keep unrelated tests |
+
+## Interfaces
+// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+
+namespace Orchestrator.App.Core.Interfaces
+{
+    public interface IGitHubClient
+    {
+        // existing methods kept
+        Task<string> GetRepositoryDefaultBranchAsync(string owner, string repo);
+        Task<bool> RepositoryExistsAsync(string owner, string repo);
+
+        // Dead code - unused in codebase
+        Task CreateBranchAsync(string branchName);
+        Task DeleteBranchAsync(string branchName);
+    }
+}
+```
+
+// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+
+namespace Orchestrator.App.Core.Interfaces
+{
+    public interface IGitHubClient
+    {
+        // existing methods kept
+        Task<string> GetRepositoryDefaultBranchAsync(string owner, string repo);
+        Task<bool> RepositoryExistsAsync(string owner, string repo);
+
+        // CreateBranchAsync and DeleteBranchAsync removed - no longer part of interface
+    }
+}
+```
+
+// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+using Octokit;
+using Orchestrator.App.Core.Interfaces;
+
+namespace Orchestrator.App.Infrastructure.GitHub
+{
+    public class OctokitGitHubClient : IGitHubClient
+    {
+        // other existing members and constructor omitted for brevity
+
+        public async Task CreateBranchAsync(string branchName)
+        {
+            // Implementation that used GitHub REST API to create a branch (unused)
+            var reference = new ReferenceUpdate("refs/heads/" + branchName);
+            // ... implementation details
+            await Task.CompletedTask;
+        }
+
+        public async Task DeleteBranchAsync(string branchName)
+        {
+            // Implementation that used GitHub REST API to delete a branch (unused)
+            await Client.Git.Reference.Delete(owner, repo, "heads/" + branchName);
+        }
+
+        // other IGitHubClient members implemented below
+    }
+}
+```
+
+// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+```csharp
+using System.Threading.Tasks;
+using Octokit;
+using Orchestrator.App.Core.Interfaces;
+
+namespace Orchestrator.App.Infrastructure.GitHub
+{
+    public class OctokitGitHubClient : IGitHubClient
+    {
+        // other existing members and constructor omitted for brevity
+
+        // CreateBranchAsync and DeleteBranchAsync implementations removed
+        // Remaining IGitHubClient methods continue to be implemented below
+    }
+}
+```
+
+// BEFORE: tests/GitHubClientTests.cs
+```csharp
+using System.Threading.Tasks;
+using Xunit;
+using Moq;
+using Orchestrator.App.Core.Interfaces;
+
+public class GitHubClientTests
+{
+    [Fact]
+    public async Task CreateBranchAsync_CreatesBranch()
+    {
+        // Test that wired up the Octokit implementation or IGitHubClient.CreateBranchAsync
+        // (This test will be removed because the method is removed)
+    }
+
+    [Fact]
+    public async Task DeleteBranchAsync_DeletesBranch()
+    {
+        // Test that wired up the Octokit implementation or IGitHubClient.DeleteBranchAsync
+        // (This test will be removed because the method is removed)
+    }
+
+    [Fact]
+    public async Task RepositoryExistsAsync_ReturnsTrue()
+    {
+        // Unrelated test retained
+    }
+}
+```
+
+// AFTER: tests/GitHubClientTests.cs
+```csharp
+using System.Threading.Tasks;
+using Xunit;
+using Moq;
+using Orchestrator.App.Core.Interfaces;
+
+public class GitHubClientTests
+{
+    // Tests that referenced CreateBranchAsync/DeleteBranchAsync removed.
+
+    [Fact]
+    public async Task RepositoryExistsAsync_ReturnsTrue()
+    {
+        // Unrelated test retained
+    }
+}
+```
+
+## Scenarios
+
+Scenario: Remove interface methods
+Given the IGitHubClient interface contains CreateBranchAsync and DeleteBranchAsync
+When the repository is updated per this spec
+Then IGitHubClient no longer contains CreateBranchAsync or DeleteBranchAsync declarations
+
+Scenario: Remove Octokit implementations
+Given OctokitGitHubClient implemented CreateBranchAsync and DeleteBranchAsync
+When the repository is updated per this spec
+Then OctokitGitHubClient no longer contains those method implementations and still compiles
+
+Scenario: Tests remain passing
+Given the test suite (tests/) may include tests for the removed methods
+When tests referencing CreateBranchAsync/DeleteBranchAsync are removed
+Then the full test suite passes with no references to the removed methods
+
+Scenario: No runtime behavioral change
+Given branch creation/deletion is already performed locally via RepoGit.EnsureBranch and CommitAndPush
+When the API methods are removed
+Then runtime behavior of branch creation/push remains unchanged
+
+## Sequence
+1. Modify src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs to remove CreateBranchAsync and DeleteBranchAsync declarations (exact lines removed shown above).
+2. Modify src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs to remove corresponding method implementations (around the noted region).
+3. Run test suite and remove any tests that reference the removed methods (update tests/GitHubClientTests.cs).
+4. Build project; run tests; commit changes.
+
+## Test Matrix
+| Test | Files | Notes |
+| --- | --- | --- |
+| Unit | tests/GitHubClientTests.cs | Remove tests that directly assert CreateBranchAsync/DeleteBranchAsync behavior; keep unrelated tests |
+| Integration/Unit | tests/ (full suite) | Run entire test suite to ensure no remaining references or compile errors |
+
+DESIGN CHECKLIST - Before finalizing spec:
+- Is this the SIMPLEST design that satisfies acceptance criteria?
+  - Yes. Directly remove two unused methods and their implementations; no new abstractions or features.
+- Are you adding abstractions/interfaces?
+  - No. We only remove methods; no new interfaces added.
+- Are you adding config?
+  - No.
+- Did you copy patterns from existing similar code?
+  - Yes. This follows the existing pattern of removing dead code without changing callers.
+- Will implementation fit file size limits?
+  - Yes. Changes are small edits to existing files.
+- IMPORTANT: If adding new files (Operation: Add in Touch List), reference at least one allowed framework ID and pattern ID from the playbook
+  - No new files added.
+
+Acceptance Criteria mapping
+1. IGitHubClient no longer contains CreateBranchAsync/DeleteBranchAsync — covered in Interfaces BEFORE/AFTER.
+2. OctokitGitHubClient implementations removed — covered in Interfaces BEFORE/AFTER.
+3. Test suite passes after removing any tests targeting these methods — covered by Scenarios and Test Matrix.
+
+Implementation notes (minimal)
+- Do not alter any other IGitHubClient methods.
+- Do not change RepoGit behavior; this change is orthogonal.
+- Ensure no compile-time references remain to the removed methods. Run 'dotnet build' and 'dotnet test' to verify.
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-TechLead-attempt-1.md b/orchestrator/prompts/issue-43-TechLead-attempt-1.md
--- a/orchestrator/prompts/issue-43-TechLead-attempt-1.md
+++ b/orchestrator/prompts/issue-43-TechLead-attempt-1.md
@@ -0,0 +1,187 @@
+# System Prompt
+
+You are a senior tech lead following MINIMAL FIRST architecture principles. CORE PRINCIPLES:
+1. SIMPLEST SOLUTION: Design the minimal implementation that satisfies requirements
+2. NO ABSTRACTIONS: Concrete implementations only. Add interfaces when 2nd implementation needed
+3. NO FUTURE-PROOFING: Hard-code sane defaults, no config for hypothetical scenarios
+4. FOLLOW EXISTING PATTERNS: Copy structure from similar code in codebase
+5. FILE SIZE LIMITS: Executors 200-400 LOC, Validators 100-200 LOC, Models 50-100 LOC
+
+Follow the provided spec template and playbook constraints. Do not add requirements beyond acceptance criteria. Output markdown only.
+
+# User Prompt
+
+ARCHITECTURE VISION:
+- Tech Stack: .NET 8, C# 12, xUnit, Moq (NO new frameworks without approval)
+- Design Patterns: Records for DTOs, Dependency Injection via constructor, Fail fast with exceptions
+- Anti-Patterns FORBIDDEN: God objects, premature abstraction, config overload, speculative features
+- Code Style: Minimal comments, no XML docs for internals, clear variable names over documentation
+
+FOLDER STRUCTURE (Enforced):
+- Core/: Configuration, Models, Interfaces
+- Infrastructure/: Filesystem, Git, GitHub, Llm, Mcp
+- Workflows/: Executors, Gates, Prompts
+- Parsing/: Markdown parsers
+- Utilities/: Helpers (minimal)
+
+Issue Title:
+Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+Issue Body:
+## Problem
+
+`IGitHubClient` contains `CreateBranchAsync` and `DeleteBranchAsync` methods that are **never used** in the codebase:
+
+**Dead code:**
+- `IGitHubClient.CreateBranchAsync(string branchName)` 
+- `IGitHubClient.DeleteBranchAsync(string branchName)`
+- `OctokitGitHubClient.CreateBranchAsync(...)` (implementation)
+- `OctokitGitHubClient.DeleteBranchAsync(...)` (implementation)
+
+**Why they exist:**
+These methods create/delete branches via GitHub REST API.
+
+**Why they're unused:**
+Branch operations are done **locally via LibGit2Sharp** in `RepoGit.EnsureBranch()`:
+```csharp
+// ContextBuilderExecutor.cs:35
+WorkContext.Repo.EnsureBranch(branchName, baseBranch);
+
+// RepoGit.cs:82 - Fetches from remote
+Commands.Fetch(repo, "origin", ...);
+
+// RepoGit.cs:107-128 - Creates branch locally
+localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
+```
+
+Branches are pushed to remote during `CommitAndPush()`, not via API calls.
+
+## Proposal
+
+Remove dead code for YAGNI compliance and reduced surface area.
+
+## Acceptance Criteria
+
+1. Given `IGitHubClient` interface, when unused methods are removed, then `CreateBranchAsync` and `DeleteBranchAsync` no longer exist
+2. Given `OctokitGitHubClient` implementation, when unused methods are removed, then implementations are deleted
+3. Given the test suite, when dead code is removed, then all tests pass (no code depends on these methods)
+
+## Files to Update
+
+- `src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs` (remove interface methods)
+- `src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs` (remove implementations around lines 250-280)
+- `tests/` (remove any tests for these methods, if they exist)
+
+## Impact
+
+- Cleaner interface
+- Less dead code
+- YAGNI compliance
+- No functional change (methods were never called)
+
+Playbook Constraints:
+- Core Principles:
+  - Minimal First: Always start with simplest solution that satisfies requirements
+  - No Future-Proofing: Do not add features, config, or abstractions for hypothetical scenarios
+  - Small Focused Issues: One feature per issue, max 3-5 acceptance criteria
+  - Follow Existing Patterns: Copy structure from similar code in codebase before inventing new patterns
+  - Quality Over Speed: Slow and correct beats fast and broken
+- Allowed Frameworks:
+  - .NET 8 (FW-01)
+  - xUnit (FW-02)
+  - Moq (FW-03)
+- Forbidden Frameworks:
+  - Newtonsoft.Json
+- Allowed Patterns:
+  - Clean Architecture (PAT-01)
+  - Repository Pattern (PAT-02)
+  - Records for DTOs (PAT-03)
+  - Dependency Injection (PAT-04)
+- Forbidden Patterns:
+  - Singleton (ANTI-01)
+  - God Objects (ANTI-02)
+  - Premature Abstraction (ANTI-03)
+  - Config Overload (ANTI-04)
+  - Speculative Features (ANTI-05)
+
+Spec Template:
+# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+STATUS: DRAFT
+UPDATED: 2026-01-11 14:43:08 UTC
+
+## Goal
+Describe the goal in 2-3 sentences.
+
+## Non-Goals
+- ...
+
+## Components
+- ...
+
+## Touch List
+| Operation | Path | Notes |
+| --- | --- | --- |
+| Modify | src/Example.cs | ... |
+
+## Interfaces
+```csharp
+// Interface stubs or signatures
+```
+
+## Scenarios
+Scenario: Example
+Given ...
+When ...
+Then ...
+
+Scenario: ...
+Given ...
+When ...
+Then ...
+
+Scenario: ...
+Given ...
+When ...
+Then ...
+
+## Sequence
+1. ...
+2. ...
+
+## Test Matrix
+| Test | Files | Notes |
+| --- | --- | --- |
+| Unit | tests/ExampleTests.cs | ... |
+
+
+DESIGN CHECKLIST - Before finalizing spec:
+- Is this the SIMPLEST design that satisfies acceptance criteria?
+- Are you adding abstractions/interfaces? Remove unless 2+ implementations exist.
+- Are you adding config? Hard-code defaults unless environment-specific.
+- Did you copy patterns from existing similar code?
+- Will implementation fit file size limits? (Executors 200-400 LOC max)
+- IMPORTANT: If adding new files (Operation: Add in Touch List), reference at least one allowed framework ID (e.g., FW-01) and pattern ID (e.g., PAT-02) from the playbook in your spec.
+
+TOUCH LIST REQUIREMENTS:
+- Use ACTUAL file paths (e.g., 'src/App.cs') or directory paths (e.g., 'tests/')
+- DO NOT use glob patterns like 'tests/**' or 'src/**/*.cs' - these will fail validation
+- For test files, use 'tests/' to indicate the entire test directory
+- Paths must exist in the repository or validation will fail
+
+INTERFACES SECTION REQUIREMENTS (CRITICAL FOR CODE REMOVAL):
+- Show CONCRETE BEFORE/AFTER examples for EACH file in the Touch List
+- For MODIFY operations removing code: Show exact methods/classes BEFORE and their absence AFTER
+- Use ACTUAL code from the repository, not simplified examples
+- When removing methods: Show the complete method signature in BEFORE, completely absent in AFTER
+- Format: '// BEFORE: path/to/file.cs' then '// AFTER: path/to/file.cs'
+- Example removing a method:
+  // BEFORE: src/IExample.cs
+  Task MethodToKeep();
+  Task MethodToRemove();  // ← Will be removed
+  
+  // AFTER: src/IExample.cs
+  Task MethodToKeep();
+  // MethodToRemove is completely absent
+
+Write the spec in markdown using the template sections with at least 3 Gherkin scenarios.

diff --git a/orchestrator/prompts/issue-43-TechLead-attempt-2-response.md b/orchestrator/prompts/issue-43-TechLead-attempt-2-response.md
--- a/orchestrator/prompts/issue-43-TechLead-attempt-2-response.md
+++ b/orchestrator/prompts/issue-43-TechLead-attempt-2-response.md
@@ -0,0 +1,148 @@
+# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+STATUS: DRAFT  
+UPDATED: 2026-01-11 14:43:39 UTC
+
+## Goal
+Remove dead methods CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and delete their implementations in OctokitGitHubClient so the public surface no longer exposes unused GitHub branch API calls. Ensure tests are updated so the test suite continues to pass.
+
+## Non-Goals
+- Add any new GitHub branch functionality.
+- Replace branch operations with new abstractions or APIs.
+- Change how branches are created/pushed (branches remain managed locally via LibGit2Sharp as-is).
+
+## Components
+- Core/Interfaces/IGitHubClient.cs — remove unused method declarations
+- Infrastructure/GitHub/OctokitGitHubClient.cs — remove unused method implementations
+- tests/ — remove any tests that reference these methods (if present)
+
+## Touch List
+| Operation | Path | Notes |
+| --- | --- | --- |
+| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync method declarations |
+| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync implementations (approx lines 250-280) |
+| Modify | tests/ | Remove tests that call the removed methods (if any exist) |
+
+## Interfaces
+
+// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+public interface IGitHubClient
+{
+    Task<string> GetDefaultBranchAsync(string owner, string repo);
+    Task<bool> RepositoryExistsAsync(string owner, string repo);
+    Task CreateBranchAsync(string branchName);
+    Task DeleteBranchAsync(string branchName);
+}
+
+// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+public interface IGitHubClient
+{
+    Task<string> GetDefaultBranchAsync(string owner, string repo);
+    Task<bool> RepositoryExistsAsync(string owner, string repo);
+    // CreateBranchAsync and DeleteBranchAsync removed
+
+// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+public class OctokitGitHubClient : IGitHubClient
+{
+    // ... other members ...
+
+    public async Task CreateBranchAsync(string branchName)
+    {
+        // Implementation using Octokit REST API to create a reference
+        var owner = _options.Owner;
+        var repo = _options.Repository;
+        var masterRef = await _client.Git.Reference.Get(owner, repo, "heads/main");
+        var newRef = new NewReference($"refs/heads/{branchName}", masterRef.Object.Sha);
+        await _client.Git.Reference.Create(owner, repo, newRef);
+    }
+
+    public async Task DeleteBranchAsync(string branchName)
+    {
+        var owner = _options.Owner;
+        var repo = _options.Repository;
+        await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");
+    }
+
+    // ... other members ...
+}
+
+// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+public class OctokitGitHubClient : IGitHubClient
+{
+    // ... other members ...
+    // CreateBranchAsync and DeleteBranchAsync implementations removed
+    // Remaining IGitHubClient methods (GetDefaultBranchAsync, RepositoryExistsAsync, etc.) remain unchanged.
+}
+
+Note: the BEFORE snippets above show the exact method signatures that will be removed. The AFTER snippets show those signatures absent from the files.
+
+For tests:
+ // BEFORE: tests/
+ // Example: tests/Infrastructure/GitHub/OctokitGitHubClientTests.cs
+ // [Fact]
+ // public async Task CreateBranchAsync_CreatesReferenceOnGitHub() { ... calls CreateBranchAsync ... }
+ //
+ // [Fact]
+ // public async Task DeleteBranchAsync_DeletesReferenceOnGitHub() { ... calls DeleteBranchAsync ... }
+ //
+ // AFTER: tests/
+ // Any tests that referenced CreateBranchAsync/DeleteBranchAsync have been removed or updated so no tests call these methods.
+
+## Scenarios
+
+Scenario: Interface no longer exposes branch API methods  
+Given the IGitHubClient interface in the codebase  
+When CreateBranchAsync and DeleteBranchAsync are removed from IGitHubClient.cs  
+Then the IGitHubClient interface does not contain CreateBranchAsync or DeleteBranchAsync
+
+Scenario: Octokit implementation no longer contains implementations  
+Given the OctokitGitHubClient class that previously implemented CreateBranchAsync and DeleteBranchAsync  
+When the implementations are deleted from OctokitGitHubClient.cs  
+Then OctokitGitHubClient compiles and no longer contains those methods
+
+Scenario: Test suite remains green after removal  
+Given the full test suite runs after code removal  
+When any tests referencing the removed methods are deleted/updated and tests executed  
+Then all tests pass and no test depends on the removed methods
+
+Scenario: No runtime behavior change for branch operations  
+Given branch creation/deletion is performed locally via RepoGit.EnsureBranch and CommitAndPush  
+When IGitHubClient removal is deployed  
+Then existing behavior remains unchanged because no code path called the removed methods
+
+## Sequence
+1. Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs — remove CreateBranchAsync and DeleteBranchAsync method declarations.
+2. Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs — delete the CreateBranchAsync and DeleteBranchAsync method implementations (preserve the rest of the class).
+3. Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync. Remove those test methods or update them to not call removed methods. Prefer deletion unless the test verifies other behavior.
+4. Build solution; fix any compilation issues (none expected if methods were truly unused).
+5. Run unit tests: dotnet test (xUnit). Ensure all tests pass.
+6. Commit changes with a short message: "Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient and OctokitGitHubClient".
+
+## Test Matrix
+| Test | Files | Notes |
+| --- | --- | --- |
+| Unit | tests/ | Remove or update tests that reference CreateBranchAsync/DeleteBranchAsync. All other tests run unchanged (FW-02). |
+
+DESIGN CHECKLIST
+- Is this the SIMPLEST design that satisfies acceptance criteria?  
+  Yes — direct deletion of unused methods and their implementations without adding abstractions.
+
+- Are you adding abstractions/interfaces?  
+  No — we are removing two methods. No new interfaces added (follows NO ABSTRACTIONS).
+
+- Are you adding config?  
+  No.
+
+- Did you copy patterns from existing similar code?  
+  Yes — changes are confined to matching existing file/class patterns used in the repo.
+
+- Will implementation fit file size limits?  
+  Yes — changes are small removals.
+
+- Framework / Pattern references:  
+  Uses .NET 8 and xUnit as-is (FW-01, FW-02). This is a refactor rather than a feature; no new framework or pattern is added.
+
+Notes
+- Minimalist change: only remove declarations and their bodies. Do not change other members.
+- If any code unexpectedly references the removed methods (compilation error), update that code to call existing RepoGit methods instead — but per repository analysis these calls do not exist.
+- Keep commit atomic and small so reviewers can easily validate removal.
\ No newline at end of file

diff --git a/orchestrator/prompts/issue-43-TechLead-attempt-2.md b/orchestrator/prompts/issue-43-TechLead-attempt-2.md
--- a/orchestrator/prompts/issue-43-TechLead-attempt-2.md
+++ b/orchestrator/prompts/issue-43-TechLead-attempt-2.md
@@ -0,0 +1,187 @@
+# System Prompt
+
+You are a senior tech lead following MINIMAL FIRST architecture principles. CORE PRINCIPLES:
+1. SIMPLEST SOLUTION: Design the minimal implementation that satisfies requirements
+2. NO ABSTRACTIONS: Concrete implementations only. Add interfaces when 2nd implementation needed
+3. NO FUTURE-PROOFING: Hard-code sane defaults, no config for hypothetical scenarios
+4. FOLLOW EXISTING PATTERNS: Copy structure from similar code in codebase
+5. FILE SIZE LIMITS: Executors 200-400 LOC, Validators 100-200 LOC, Models 50-100 LOC
+
+Follow the provided spec template and playbook constraints. Do not add requirements beyond acceptance criteria. Output markdown only.
+
+# User Prompt
+
+ARCHITECTURE VISION:
+- Tech Stack: .NET 8, C# 12, xUnit, Moq (NO new frameworks without approval)
+- Design Patterns: Records for DTOs, Dependency Injection via constructor, Fail fast with exceptions
+- Anti-Patterns FORBIDDEN: God objects, premature abstraction, config overload, speculative features
+- Code Style: Minimal comments, no XML docs for internals, clear variable names over documentation
+
+FOLDER STRUCTURE (Enforced):
+- Core/: Configuration, Models, Interfaces
+- Infrastructure/: Filesystem, Git, GitHub, Llm, Mcp
+- Workflows/: Executors, Gates, Prompts
+- Parsing/: Markdown parsers
+- Utilities/: Helpers (minimal)
+
+Issue Title:
+Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+Issue Body:
+## Problem
+
+`IGitHubClient` contains `CreateBranchAsync` and `DeleteBranchAsync` methods that are **never used** in the codebase:
+
+**Dead code:**
+- `IGitHubClient.CreateBranchAsync(string branchName)` 
+- `IGitHubClient.DeleteBranchAsync(string branchName)`
+- `OctokitGitHubClient.CreateBranchAsync(...)` (implementation)
+- `OctokitGitHubClient.DeleteBranchAsync(...)` (implementation)
+
+**Why they exist:**
+These methods create/delete branches via GitHub REST API.
+
+**Why they're unused:**
+Branch operations are done **locally via LibGit2Sharp** in `RepoGit.EnsureBranch()`:
+```csharp
+// ContextBuilderExecutor.cs:35
+WorkContext.Repo.EnsureBranch(branchName, baseBranch);
+
+// RepoGit.cs:82 - Fetches from remote
+Commands.Fetch(repo, "origin", ...);
+
+// RepoGit.cs:107-128 - Creates branch locally
+localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
+```
+
+Branches are pushed to remote during `CommitAndPush()`, not via API calls.
+
+## Proposal
+
+Remove dead code for YAGNI compliance and reduced surface area.
+
+## Acceptance Criteria
+
+1. Given `IGitHubClient` interface, when unused methods are removed, then `CreateBranchAsync` and `DeleteBranchAsync` no longer exist
+2. Given `OctokitGitHubClient` implementation, when unused methods are removed, then implementations are deleted
+3. Given the test suite, when dead code is removed, then all tests pass (no code depends on these methods)
+
+## Files to Update
+
+- `src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs` (remove interface methods)
+- `src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs` (remove implementations around lines 250-280)
+- `tests/` (remove any tests for these methods, if they exist)
+
+## Impact
+
+- Cleaner interface
+- Less dead code
+- YAGNI compliance
+- No functional change (methods were never called)
+
+Playbook Constraints:
+- Core Principles:
+  - Minimal First: Always start with simplest solution that satisfies requirements
+  - No Future-Proofing: Do not add features, config, or abstractions for hypothetical scenarios
+  - Small Focused Issues: One feature per issue, max 3-5 acceptance criteria
+  - Follow Existing Patterns: Copy structure from similar code in codebase before inventing new patterns
+  - Quality Over Speed: Slow and correct beats fast and broken
+- Allowed Frameworks:
+  - .NET 8 (FW-01)
+  - xUnit (FW-02)
+  - Moq (FW-03)
+- Forbidden Frameworks:
+  - Newtonsoft.Json
+- Allowed Patterns:
+  - Clean Architecture (PAT-01)
+  - Repository Pattern (PAT-02)
+  - Records for DTOs (PAT-03)
+  - Dependency Injection (PAT-04)
+- Forbidden Patterns:
+  - Singleton (ANTI-01)
+  - God Objects (ANTI-02)
+  - Premature Abstraction (ANTI-03)
+  - Config Overload (ANTI-04)
+  - Speculative Features (ANTI-05)
+
+Spec Template:
+# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+STATUS: DRAFT
+UPDATED: 2026-01-11 14:43:39 UTC
+
+## Goal
+Describe the goal in 2-3 sentences.
+
+## Non-Goals
+- ...
+
+## Components
+- ...
+
+## Touch List
+| Operation | Path | Notes |
+| --- | --- | --- |
+| Modify | src/Example.cs | ... |
+
+## Interfaces
+```csharp
+// Interface stubs or signatures
+```
+
+## Scenarios
+Scenario: Example
+Given ...
+When ...
+Then ...
+
+Scenario: ...
+Given ...
+When ...
+Then ...
+
+Scenario: ...
+Given ...
+When ...
+Then ...
+
+## Sequence
+1. ...
+2. ...
+
+## Test Matrix
+| Test | Files | Notes |
+| --- | --- | --- |
+| Unit | tests/ExampleTests.cs | ... |
+
+
+DESIGN CHECKLIST - Before finalizing spec:
+- Is this the SIMPLEST design that satisfies acceptance criteria?
+- Are you adding abstractions/interfaces? Remove unless 2+ implementations exist.
+- Are you adding config? Hard-code defaults unless environment-specific.
+- Did you copy patterns from existing similar code?
+- Will implementation fit file size limits? (Executors 200-400 LOC max)
+- IMPORTANT: If adding new files (Operation: Add in Touch List), reference at least one allowed framework ID (e.g., FW-01) and pattern ID (e.g., PAT-02) from the playbook in your spec.
+
+TOUCH LIST REQUIREMENTS:
+- Use ACTUAL file paths (e.g., 'src/App.cs') or directory paths (e.g., 'tests/')
+- DO NOT use glob patterns like 'tests/**' or 'src/**/*.cs' - these will fail validation
+- For test files, use 'tests/' to indicate the entire test directory
+- Paths must exist in the repository or validation will fail
+
+INTERFACES SECTION REQUIREMENTS (CRITICAL FOR CODE REMOVAL):
+- Show CONCRETE BEFORE/AFTER examples for EACH file in the Touch List
+- For MODIFY operations removing code: Show exact methods/classes BEFORE and their absence AFTER
+- Use ACTUAL code from the repository, not simplified examples
+- When removing methods: Show the complete method signature in BEFORE, completely absent in AFTER
+- Format: '// BEFORE: path/to/file.cs' then '// AFTER: path/to/file.cs'
+- Example removing a method:
+  // BEFORE: src/IExample.cs
+  Task MethodToKeep();
+  Task MethodToRemove();  // ← Will be removed
+  
+  // AFTER: src/IExample.cs
+  Task MethodToKeep();
+  // MethodToRemove is completely absent
+
+Write the spec in markdown using the template sections with at least 3 Gherkin scenarios.

diff --git a/orchestrator/refinement/issue-43.md b/orchestrator/refinement/issue-43.md
--- a/orchestrator/refinement/issue-43.md
+++ b/orchestrator/refinement/issue-43.md
@@ -0,0 +1,27 @@
+# Refinement: Issue #43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+**Status**: Refinement Complete
+**Generated**: 2026-01-11 14:43:07 UTC
+
+## Clarified Story
+
+Remove two dead methods from the internal GitHub client surface: delete CreateBranchAsync(string) and DeleteBranchAsync(string) from IGitHubClient, remove their implementations from OctokitGitHubClient, and remove any unit tests that reference them. This is a minimal refactor: edit the interface file to remove the two signatures, delete the corresponding method bodies from the OctokitGitHubClient class, remove any tests that reference these methods, then build and run tests to verify no compilation or test failures. If IGitHubClient is part of a public/distributed API consumed by external projects, do NOT merge without coordinating a breaking-change plan; the ProductOwner could not determine whether it is published externally, so that check must be performed before merging.
+
+## Acceptance Criteria (4)
+
+- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
+- Given the OctokitGitHubClient implementation, when CreateBranchAsync and DeleteBranchAsync implementations are removed and the solution is built, then dotnet build must complete successfully with no missing-member compile errors.
+- Given the repository test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by the removal of CreateBranchAsync/DeleteBranchAsync.
+- Given the repository codebase after removal, when performing a code/text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references to these method names (interface, implementation, or tests).
+
+## Questions
+
+**How to answer:**
+1. Edit this file and add your answer after the question
+2. Mark the checkbox with [x] when answered
+3. Commit and push changes
+4. Remove `blocked` label and add `dor` label to re-trigger
+
+- [x] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
+  **Answer (ProductOwner):** I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.
+

diff --git a/orchestrator/specs/issue-43.md b/orchestrator/specs/issue-43.md
--- a/orchestrator/specs/issue-43.md
+++ b/orchestrator/specs/issue-43.md
@@ -0,0 +1,148 @@
+# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
+
+STATUS: DRAFT  
+UPDATED: 2026-01-11 14:43:39 UTC
+
+## Goal
+Remove dead methods CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and delete their implementations in OctokitGitHubClient so the public surface no longer exposes unused GitHub branch API calls. Ensure tests are updated so the test suite continues to pass.
+
+## Non-Goals
+- Add any new GitHub branch functionality.
+- Replace branch operations with new abstractions or APIs.
+- Change how branches are created/pushed (branches remain managed locally via LibGit2Sharp as-is).
+
+## Components
+- Core/Interfaces/IGitHubClient.cs — remove unused method declarations
+- Infrastructure/GitHub/OctokitGitHubClient.cs — remove unused method implementations
+- tests/ — remove any tests that reference these methods (if present)
+
+## Touch List
+| Operation | Path | Notes |
+| --- | --- | --- |
+| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync method declarations |
+| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync implementations (approx lines 250-280) |
+| Modify | tests/ | Remove tests that call the removed methods (if any exist) |
+
+## Interfaces
+
+// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+public interface IGitHubClient
+{
+    Task<string> GetDefaultBranchAsync(string owner, string repo);
+    Task<bool> RepositoryExistsAsync(string owner, string repo);
+    Task CreateBranchAsync(string branchName);
+    Task DeleteBranchAsync(string branchName);
+}
+
+// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+public interface IGitHubClient
+{
+    Task<string> GetDefaultBranchAsync(string owner, string repo);
+    Task<bool> RepositoryExistsAsync(string owner, string repo);
+    // CreateBranchAsync and DeleteBranchAsync removed
+
+// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+public class OctokitGitHubClient : IGitHubClient
+{
+    // ... other members ...
+
+    public async Task CreateBranchAsync(string branchName)
+    {
+        // Implementation using Octokit REST API to create a reference
+        var owner = _options.Owner;
+        var repo = _options.Repository;
+        var masterRef = await _client.Git.Reference.Get(owner, repo, "heads/main");
+        var newRef = new NewReference($"refs/heads/{branchName}", masterRef.Object.Sha);
+        await _client.Git.Reference.Create(owner, repo, newRef);
+    }
+
+    public async Task DeleteBranchAsync(string branchName)
+    {
+        var owner = _options.Owner;
+        var repo = _options.Repository;
+        await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");
+    }
+
+    // ... other members ...
+}
+
+// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+public class OctokitGitHubClient : IGitHubClient
+{
+    // ... other members ...
+    // CreateBranchAsync and DeleteBranchAsync implementations removed
+    // Remaining IGitHubClient methods (GetDefaultBranchAsync, RepositoryExistsAsync, etc.) remain unchanged.
+}
+
+Note: the BEFORE snippets above show the exact method signatures that will be removed. The AFTER snippets show those signatures absent from the files.
+
+For tests:
+ // BEFORE: tests/
+ // Example: tests/Infrastructure/GitHub/OctokitGitHubClientTests.cs
+ // [Fact]
+ // public async Task CreateBranchAsync_CreatesReferenceOnGitHub() { ... calls CreateBranchAsync ... }
+ //
+ // [Fact]
+ // public async Task DeleteBranchAsync_DeletesReferenceOnGitHub() { ... calls DeleteBranchAsync ... }
+ //
+ // AFTER: tests/
+ // Any tests that referenced CreateBranchAsync/DeleteBranchAsync have been removed or updated so no tests call these methods.
+
+## Scenarios
+
+Scenario: Interface no longer exposes branch API methods  
+Given the IGitHubClient interface in the codebase  
+When CreateBranchAsync and DeleteBranchAsync are removed from IGitHubClient.cs  
+Then the IGitHubClient interface does not contain CreateBranchAsync or DeleteBranchAsync
+
+Scenario: Octokit implementation no longer contains implementations  
+Given the OctokitGitHubClient class that previously implemented CreateBranchAsync and DeleteBranchAsync  
+When the implementations are deleted from OctokitGitHubClient.cs  
+Then OctokitGitHubClient compiles and no longer contains those methods
+
+Scenario: Test suite remains green after removal  
+Given the full test suite runs after code removal  
+When any tests referencing the removed methods are deleted/updated and tests executed  
+Then all tests pass and no test depends on the removed methods
+
+Scenario: No runtime behavior change for branch operations  
+Given branch creation/deletion is performed locally via RepoGit.EnsureBranch and CommitAndPush  
+When IGitHubClient removal is deployed  
+Then existing behavior remains unchanged because no code path called the removed methods
+
+## Sequence
+1. Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs — remove CreateBranchAsync and DeleteBranchAsync method declarations.
+2. Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs — delete the CreateBranchAsync and DeleteBranchAsync method implementations (preserve the rest of the class).
+3. Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync. Remove those test methods or update them to not call removed methods. Prefer deletion unless the test verifies other behavior.
+4. Build solution; fix any compilation issues (none expected if methods were truly unused).
+5. Run unit tests: dotnet test (xUnit). Ensure all tests pass.
+6. Commit changes with a short message: "Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient and OctokitGitHubClient".
+
+## Test Matrix
+| Test | Files | Notes |
+| --- | --- | --- |
+| Unit | tests/ | Remove or update tests that reference CreateBranchAsync/DeleteBranchAsync. All other tests run unchanged (FW-02). |
+
+DESIGN CHECKLIST
+- Is this the SIMPLEST design that satisfies acceptance criteria?  
+  Yes — direct deletion of unused methods and their implementations without adding abstractions.
+
+- Are you adding abstractions/interfaces?  
+  No — we are removing two methods. No new interfaces added (follows NO ABSTRACTIONS).
+
+- Are you adding config?  
+  No.
+
+- Did you copy patterns from existing similar code?  
+  Yes — changes are confined to matching existing file/class patterns used in the repo.
+
+- Will implementation fit file size limits?  
+  Yes — changes are small removals.
+
+- Framework / Pattern references:  
+  Uses .NET 8 and xUnit as-is (FW-01, FW-02). This is a refactor rather than a feature; no new framework or pattern is added.
+
+Notes
+- Minimalist change: only remove declarations and their bodies. Do not change other members.
+- If any code unexpectedly references the removed methods (compilation error), update that code to call existing RepoGit methods instead — but per repository analysis these calls do not exist.
+- Keep commit atomic and small so reviewers can easily validate removal.
\ No newline at end of file

diff --git a/src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs b/src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
--- a/src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
+++ b/src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
@@ -28,10 +28,6 @@ internal interface IGitHubClient
     [ExcludeFromCodeCoverage]
     Task<string> GetPullRequestDiffAsync(int prNumber);
     [ExcludeFromCodeCoverage]
-    Task CreateBranchAsync(string branchName);
-    [ExcludeFromCodeCoverage]
-    Task DeleteBranchAsync(string branchName);
-    [ExcludeFromCodeCoverage]
     Task<bool> HasCommitsAsync(string baseBranch, string headBranch);
     [ExcludeFromCodeCoverage]
     Task<RepoFile?> TryGetFileContentAsync(string branch, string path);
@@ -44,4 +40,4 @@ Task<ProjectSnapshot> GetProjectSnapshotAsync(
         ProjectOwnerType ownerType);
     [ExcludeFromCodeCoverage]
     Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName);
-}
+}
\ No newline at end of file

diff --git a/src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs b/src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
--- a/src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
+++ b/src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
@@ -244,42 +244,6 @@ public async Task<string> GetPullRequestDiffAsync(int prNumber)
     // PHASE 3a: Git Operations (Octokit)
     // ========================================
 
-    /// <summary>
-    /// Create a branch
-    /// </summary>
-    public async Task CreateBranchAsync(string branchName)
-    {
-        var baseBranch = await _octokit.Git.Reference.Get(RepoOwner, RepoName, $"heads/{_cfg.Workflow.DefaultBaseBranch}");
-
-        try
-        {
-            await _octokit.Git.Reference.Create(
-                RepoOwner,
-                RepoName,
-                new NewReference($"refs/heads/{branchName}", baseBranch.Object.Sha)
-            );
-        }
-        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
-        {
-            // Branch already exists - ignore
-        }
-    }
-
-    /// <summary>
-    /// Delete a branch
-    /// </summary>
-    public async Task DeleteBranchAsync(string branchName)
-    {
-        try
-        {
-            await _octokit.Git.Reference.Delete(RepoOwner, RepoName, $"heads/{branchName}");
-        }
-        catch (NotFoundException)
-        {
-            // Branch doesn't exist - ignore
-        }
-    }
-
     /// <summary>
     /// Check if there are commits between base and head branches
     /// </summary>
@@ -702,4 +666,4 @@ private static System.Text.Json.JsonElement ResolveProjectElement(System.Text.Js
 
         return messages.Count > 0 ? string.Join(" | ", messages) : null;
     }
-}
+}
\ No newline at end of file



Return JSON with fields:
{
  "approved": boolean,
  "summary": string,
  "findings": [
    { "severity": "BLOCKER|MAJOR|MINOR", "category": string, "message": string, "file": string?, "line": number? }
  ]
}
