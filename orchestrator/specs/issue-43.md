# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

STATUS: DRAFT
UPDATED: 2026-01-11 13:22:21 UTC

## Goal
Remove dead methods CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and their corresponding implementations in OctokitGitHubClient, and delete any unit tests that reference them. No behavioral changes to the application are expected.

## Non-Goals
- Add new abstractions or alternate implementations.
- Change branching behavior (branches remain managed locally via LibGit2Sharp).
- Add configuration or feature flags.

## Components
- Core/Interfaces/IGitHubClient.cs — interface to modify.
- Infrastructure/GitHub/OctokitGitHubClient.cs — implementation to modify.
- tests/ — remove tests that reference the removed methods if present.

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync method signatures. |
| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove implementations of CreateBranchAsync and DeleteBranchAsync (approx. lines 250-280). |
| Modify | tests/ | Remove tests that reference CreateBranchAsync/DeleteBranchAsync (if any). Use entire test directory to indicate test sweep. |

## Interfaces
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

## Scenarios

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

## Sequence
1. Update IGitHubClient interface:
   - Remove Task CreateBranchAsync(string branchName);
   - Remove Task DeleteBranchAsync(string branchName);
2. Update OctokitGitHubClient:
   - Remove CreateBranchAsync and DeleteBranchAsync method implementations and any using statements only required for them.
3. Run solution build and fix any call sites (expected none). If the build reports missing members, inspect callers and remove/update.
4. Locate and remove tests that reference removed members (search for CreateBranchAsync/DeleteBranchAsync in tests/).
5. Run full test suite (xUnit) and ensure all tests pass.
6. Commit changes with message: "Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient and OctokitGitHubClient; remove related tests."

## Test Matrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/ | Remove tests that reference CreateBranchAsync/DeleteBranchAsync and run all unit tests. (FW-02) |
| Build | src/Orchestrator.App/** | Solution builds successfully after method removal. (FW-01) |

DESIGN CHECKLIST - Before finalizing spec:
- Is this the SIMPLEST design that satisfies acceptance criteria? Yes — remove unused methods and tests; no extra changes.
- Are you adding abstractions/interfaces? No.
- Are you adding config? No.
- Did you copy patterns from existing similar code? Yes — modifications mirror existing file structures and patterns.
- Will implementation fit file size limits? Yes — changes are small deletions.
- Frameworks / Patterns referenced: .NET 8 (FW-01), xUnit (FW-02), Repository/Clean patterns already in repo (PAT-01/PAT-02).

Notes
- This change is purely a removal of unused surface area; it intentionally does not rework branching flows that are intentionally managed via LibGit2Sharp (RepoGit.EnsureBranch and CommitAndPush).
- If any unexpected callers are discovered during compilation, they should be removed or updated to use RepoGit behaviors; do not reintroduce API calls for branch management.