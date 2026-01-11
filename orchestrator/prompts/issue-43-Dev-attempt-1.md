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
Remove dead methods CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and delete their implementations in OctokitGitHubClient so the public surface no longer exposes unused GitHub branch API calls. Ensure tests are updated so the test suite continues to pass.

=== TOUCH LIST ENTRY ===
Operation: Modify
File: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
Instructions: Remove CreateBranchAsync and DeleteBranchAsync method declarations

=== REQUIRED CHANGES (Before/After Examples) ===
// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
public interface IGitHubClient
{
    Task<string> GetDefaultBranchAsync(string owner, string repo);
    Task<bool> RepositoryExistsAsync(string owner, string repo);
    Task CreateBranchAsync(string branchName);
    Task DeleteBranchAsync(string branchName);
}

// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
public interface IGitHubClient
{
    Task<string> GetDefaultBranchAsync(string owner, string repo);
    Task<bool> RepositoryExistsAsync(string owner, string repo);
    // CreateBranchAsync and DeleteBranchAsync removed

// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
public class OctokitGitHubClient : IGitHubClient
{
    // ... other members ...

    public async Task CreateBranchAsync(string branchName)
    {
        // Implementation using Octokit REST API to create a reference
        var owner = _options.Owner;
        var repo = _options.Repository;
        var masterRef = await _client.Git.Reference.Get(owner, repo, "heads/main");
        var newRef = new NewReference($"refs/heads/{branchName}", masterRef.Object.Sha);
        await _client.Git.Reference.Create(owner, repo, newRef);
    }

    public async Task DeleteBranchAsync(string branchName)
    {
        var owner = _options.Owner;
        var repo = _options.Repository;
        await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");
    }

    // ... other members ...
}

// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
public class OctokitGitHubClient : IGitHubClient
{
    // ... other members ...
    // CreateBranchAsync and DeleteBranchAsync implementations removed
    // Remaining IGitHubClient methods (GetDefaultBranchAsync, RepositoryExistsAsync, etc.) remain unchanged.
}

Note: the BEFORE snippets above show the exact method signatures that will be removed. The AFTER snippets show those signatures absent from the files.

For tests:
 // BEFORE: tests/
 // Example: tests/Infrastructure/GitHub/OctokitGitHubClientTests.cs
 // [Fact]
 // public async Task CreateBranchAsync_CreatesReferenceOnGitHub() { ... calls CreateBranchAsync ... }
 //
 // [Fact]
 // public async Task DeleteBranchAsync_DeletesReferenceOnGitHub() { ... calls DeleteBranchAsync ... }
 //
 // AFTER: tests/
 // Any tests that referenced CreateBranchAsync/DeleteBranchAsync have been removed or updated so no tests call these methods.

Scenarios:
Scenario: Interface no longer exposes branch API methods  
Given the IGitHubClient interface in the codebase  
When CreateBranchAsync and DeleteBranchAsync are removed from IGitHubClient.cs  
Then the IGitHubClient interface does not contain CreateBranchAsync or DeleteBranchAsync

Scenario: Octokit implementation no longer contains implementations  
Given the OctokitGitHubClient class that previously implemented CreateBranchAsync and DeleteBranchAsync  
When the implementations are deleted from OctokitGitHubClient.cs  
Then OctokitGitHubClient compiles and no longer contains those methods

Scenario: Test suite remains green after removal  
Given the full test suite runs after code removal  
When any tests referencing the removed methods are deleted/updated and tests executed  
Then all tests pass and no test depends on the removed methods

Scenario: No runtime behavior change for branch operations  
Given branch creation/deletion is performed locally via RepoGit.EnsureBranch and CommitAndPush  
When IGitHubClient removal is deployed  
Then existing behavior remains unchanged because no code path called the removed methods

Sequence:
Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs — remove CreateBranchAsync and DeleteBranchAsync method declarations.
Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs — delete the CreateBranchAsync and DeleteBranchAsync method implementations (preserve the rest of the class).
Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync. Remove those test methods or update them to not call removed methods. Prefer deletion unless the test verifies other behavior.
Build solution; fix any compilation issues (none expected if methods were truly unused).
Run unit tests: dotnet test (xUnit). Ensure all tests pass.
Commit changes with a short message: "Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient and OctokitGitHubClient".

Test Matrix:
| Unit | tests/ | Remove or update tests that reference CreateBranchAsync/DeleteBranchAsync. All other tests run unchanged (FW-02). |

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
Follow the instructions: Remove CreateBranchAsync and DeleteBranchAsync method declarations
Output the complete updated file content below:
