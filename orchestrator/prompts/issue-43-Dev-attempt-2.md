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
Remove two dead methods from the GitHub client surface: CreateBranchAsync and DeleteBranchAsync. This reduces unused code, simplifies the interface, and does not change runtime behavior because branch operations are performed locally via LibGit2Sharp.

=== TOUCH LIST ENTRY ===
Operation: Modify
File: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
Instructions: Remove CreateBranchAsync/DeleteBranchAsync signatures

=== REQUIRED CHANGES (Before/After Examples) ===
using System.Threading.Tasks;

namespace Orchestrator.App.Core.Interfaces
{
    public interface IGitHubClient
    {
        Task<string> GetDefaultBranchNameAsync(string owner, string repo);
        Task<RepositoryInfo> GetRepositoryAsync(string owner, string repo);

        // DEAD: never used in codebase
        Task CreateBranchAsync(string branchName);
        Task DeleteBranchAsync(string branchName);

        Task<IEnumerable<IssueInfo>> GetOpenIssuesAsync(string owner, string repo);
    }
}
using System.Threading.Tasks;

namespace Orchestrator.App.Core.Interfaces
{
    public interface IGitHubClient
    {
        Task<string> GetDefaultBranchNameAsync(string owner, string repo);
        Task<RepositoryInfo> GetRepositoryAsync(string owner, string repo);
        Task<IEnumerable<IssueInfo>> GetOpenIssuesAsync(string owner, string repo);
    }
}
using System.Threading.Tasks;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Infrastructure.GitHub
{
    public class OctokitGitHubClient : IGitHubClient
    {
        // ... other members ...

        // DEAD: not referenced anywhere
        public async Task CreateBranchAsync(string branchName)
        {
            // Implementation using Octokit REST API to create a branch
            var @ref = new NewReference($"refs/heads/{branchName}", baseSha);
            await _client.Git.Reference.Create(owner, repo, @ref);
        }

        public async Task DeleteBranchAsync(string branchName)
        {
            // Implementation using Octokit REST API to delete a branch ref
            await _client.Git.Reference.Delete(owner, repo, $"heads/{branchName}");
        }

        // ... other members ...
    }
}
using System.Threading.Tasks;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Infrastructure.GitHub
{
    public class OctokitGitHubClient : IGitHubClient
    {
        // ... other members ...

        // CreateBranchAsync and DeleteBranchAsync removed as they were unused.

        // ... other members ...
    }
}
using System.Threading.Tasks;
using Xunit;
using Moq;
using Orchestrator.App.Core.Interfaces;

public class GitHubClientTests
{
    [Fact]
    public async Task CreateBranchAsync_CallsApi()
    {
        // test body
    }

    [Fact]
    public async Task DeleteBranchAsync_CallsApi()
    {
        // test body
    }
}
using System.Threading.Tasks;
using Xunit;
using Moq;
using Orchestrator.App.Core.Interfaces;

public class GitHubClientTests
{
    // Tests for CreateBranchAsync/DeleteBranchAsync removed because methods no longer exist.
    // Other tests (GetRepositoryAsync, GetDefaultBranchNameAsync, etc.) remain unchanged.
}

Scenarios:
Scenario: Interface no longer exposes CreateBranchAsync/DeleteBranchAsync
Given the codebase compiles,
When IGitHubClient is inspected,
Then CreateBranchAsync and DeleteBranchAsync are not present on IGitHubClient.

Scenario: Implementation methods removed
Given the OctokitGitHubClient implementation class,
When the file is compiled after changes,
Then there are no CreateBranchAsync or DeleteBranchAsync method implementations in OctokitGitHubClient.

Scenario: Tests continue to pass
Given the test suite is executed,
When the dead methods and any tests referencing them are removed,
Then all tests pass (no compilation or runtime failures due to removed methods).

Scenario: No behavioral change for branch operations
Given branch operations are performed via RepoGit.EnsureBranch and CommitAndPush,
When creating and pushing a branch in existing workflows,
Then behavior is unchanged because the removed API methods were never invoked.

Sequence:
Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs and remove the two method signatures.
Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs and delete the corresponding method implementations.
Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync and remove those test methods or files.
Build solution and run test suite (xUnit) and fix any incidental compile errors (e.g., using directives only).
Commit changes.

Test Matrix:
| Unit | tests/ | Run full xUnit suite after removal; ensure no test refers to removed methods |
| Compile | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs<br>src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Ensure project compiles with removed signatures/implementations |

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
Follow the instructions: Remove CreateBranchAsync/DeleteBranchAsync signatures
Output the complete updated file content below:
