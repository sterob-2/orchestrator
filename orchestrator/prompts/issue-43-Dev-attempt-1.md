# System Prompt

You are a software engineer implementing a spec. CRITICAL INSTRUCTIONS:
1. Read the Touch List Entry to understand what operation to perform
2. Study the Interfaces section which shows the required changes (before/after examples)
3. Apply those exact changes to the Current File Content
4. For 'Modify' operations: update/remove code as specified in the notes
5. When removing code: COMPLETELY OMIT it from your output - do NOT include it with comments
6. Output ONLY the complete updated file content
7. Do NOT include before/after comments or explanations
8. Do NOT preserve code marked for removal
9. VERIFY your output does not contain any code that should be removed
Follow the spec strictly. Code removal means the code must be absent from your output.

# User Prompt

Mode: minimal

Spec Goal:
Remove dead methods CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and their corresponding implementations in OctokitGitHubClient, and delete any unit tests that reference them. No behavioral changes to the application are expected.

=== TOUCH LIST ENTRY ===
Operation: Modify
File: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
Instructions: Remove CreateBranchAsync and DeleteBranchAsync method signatures.

=== REQUIRED CHANGES (Before/After Examples) ===
// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
public interface IGitHubClient
{
    Task<User> GetCurrentUserAsync();
    Task<Repository> GetRepositoryAsync(string owner, string repo);
    Task<IReadOnlyList<Branch>> ListBranchesAsync(string owner, string repo);

    // Dead methods - to be removed
    Task CreateBranchAsync(string branchName);
    Task DeleteBranchAsync(string branchName);
}

// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
public interface IGitHubClient
{
    Task<User> GetCurrentUserAsync();
    Task<Repository> GetRepositoryAsync(string owner, string repo);
    Task<IReadOnlyList<Branch>> ListBranchesAsync(string owner, string repo);

    // CreateBranchAsync and DeleteBranchAsync removed as unused
}

---

// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
public class OctokitGitHubClient : IGitHubClient
{
    private readonly GitHubClient _client;

    public OctokitGitHubClient(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("Orchestrator"))
        {
            Credentials = new Credentials(token)
        };
    }

    public Task<User> GetCurrentUserAsync() => _client.User.Current();

    public Task<Repository> GetRepositoryAsync(string owner, string repo) => _client.Repository.Get(owner, repo);

    public Task<IReadOnlyList<Branch>> ListBranchesAsync(string owner, string repo) => _client.Repository.Branch.GetAll(owner, repo);

    // DEAD: remote branch operations (unused by codebase)
    public async Task CreateBranchAsync(string branchName)
    {
        // Implementation that uses GitHub API to create a branch
        var masterRef = await _client.Git.Reference.Get("owner", "repo", "heads/main");
        var newRef = new NewReference($"refs/heads/{branchName}", masterRef.Object.Sha);
        await _client.Git.Reference.Create("owner", "repo", newRef);
    }

    public async Task DeleteBranchAsync(string branchName)
    {
        // Implementation that uses GitHub API to delete a branch
        await _client.Git.Reference.Delete("owner", "repo", $"heads/{branchName}");
    }
}

// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
public class OctokitGitHubClient : IGitHubClient
{
    private readonly GitHubClient _client;

    public OctokitGitHubClient(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("Orchestrator"))
        {
            Credentials = new Credentials(token)
        };
    }

    public Task<User> GetCurrentUserAsync() => _client.User.Current();

    public Task<Repository> GetRepositoryAsync(string owner, string repo) => _client.Repository.Get(owner, repo);

    public Task<IReadOnlyList<Branch>> ListBranchesAsync(string owner, string repo) => _client.Repository.Branch.GetAll(owner, repo);

    // CreateBranchAsync and DeleteBranchAsync implementations removed
}

---

// BEFORE: tests/OctokitGitHubClientTests.cs
public class OctokitGitHubClientTests
{
    [Fact]
    public async Task CreateBranchAsync_CreatesBranch()
    {
        var client = new OctokitGitHubClient("token");
        await client.CreateBranchAsync("feature/x"); // references removed method
        // asserts...
    }

    [Fact]
    public async Task DeleteBranchAsync_DeletesBranch()
    {
        var client = new OctokitGitHubClient("token");
        await client.DeleteBranchAsync("feature/x"); // references removed method
        // asserts...
    }
}

// AFTER: tests/OctokitGitHubClientTests.cs
public class OctokitGitHubClientTests
{
    // Tests that depended on CreateBranchAsync/DeleteBranchAsync removed.
    // Other tests in this file (GetCurrentUserAsync, GetRepositoryAsync, ListBranchesAsync) remain unchanged.
}

Scenarios:
Scenario: Interface no longer exposes branch API methods
Given the IGitHubClient interface contains CreateBranchAsync and DeleteBranchAsync
When the interface is modified to remove those methods
Then the interface no longer defines CreateBranchAsync or DeleteBranchAsync
And compilation fails if any code still referenced them

Scenario: Implementation no longer contains branch API methods
Given OctokitGitHubClient implements IGitHubClient and includes CreateBranchAsync/DeleteBranchAsync implementations
When the implementations are removed
Then OctokitGitHubClient no longer contains CreateBranchAsync or DeleteBranchAsync methods
And the class still compiles and implements the remaining IGitHubClient members

Scenario: Tests updated to reflect code removal
Given unit tests that call CreateBranchAsync or DeleteBranchAsync exist
When those tests are removed or updated to stop referencing the methods
Then the test suite compiles and runs without references to the removed methods
And all remaining tests pass

Scenario: No runtime behavioral change
Given branch creation/deletion in the codebase is performed by RepoGit via LibGit2Sharp
When IGitHubClient methods are removed
Then branch behavior remains unchanged because branch flows use RepoGit.EnsureBranch and CommitAndPush

Sequence:
Update IGitHubClient interface:
Remove Task CreateBranchAsync(string branchName);
Remove Task DeleteBranchAsync(string branchName);
Update OctokitGitHubClient:
Remove CreateBranchAsync and DeleteBranchAsync method implementations and any using statements only required for them.
Run solution build and fix any call sites (expected none). If the build reports missing members, inspect callers and remove/update.
Locate and remove tests that reference removed members (search for CreateBranchAsync/DeleteBranchAsync in tests/).
Run full test suite (xUnit) and ensure all tests pass.
Commit changes with message: "Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient and OctokitGitHubClient; remove related tests."

Test Matrix:
| Unit | tests/ | Remove tests that reference CreateBranchAsync/DeleteBranchAsync and run all unit tests. (FW-02) |
| Build | src/Orchestrator.App/** | Solution builds successfully after method removal. (FW-01) |

=== CURRENT FILE CONTENT (TO BE MODIFIED) ===
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
    Task<IReadOnlyList<IssueComment>> GetIssueCommentsAsync(int issueNumber);
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
    Task<RepoFile?> TryGetFileContentAsync(string branch, string path);
    [ExcludeFromCodeCoverage]
    Task CreateOrUpdateFileAsync(string branch, string path, string content, string message);
    [ExcludeFromCodeCoverage]
    Task<ProjectSnapshot> GetProjectSnapshotAsync(
        string owner,
        int projectNumber,
        ProjectOwnerType ownerType);
    [ExcludeFromCodeCoverage]
    Task UpdateProjectItemStatusAsync(string owner, int projectNumber, int issueNumber, string statusName);
}


=== EXAMPLE: HOW TO REMOVE CODE ===
If the instruction says 'Remove CreateBranchAsync method':

BEFORE (current file content):
  Task<bool> CreateBranchAsync(string branchName, string baseBranch);
  Task DeleteFileAsync(string path);

AFTER (your output should NOT include CreateBranchAsync):
  Task DeleteFileAsync(string path);

CRITICAL: When removing code:
- DO NOT include removed methods/classes/properties in your output
- DO NOT add comments like '// removed' or '// deleted'
- Simply omit the code completely from your response

=== YOUR TASK ===
Apply the changes shown in 'REQUIRED CHANGES' section to the 'CURRENT FILE CONTENT'.
Follow the instructions: Remove CreateBranchAsync and DeleteBranchAsync method signatures.
Output the complete updated file content below:
