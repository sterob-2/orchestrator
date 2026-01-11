# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

STATUS: DRAFT  
UPDATED: 2026-01-11 14:43:39 UTC

## Goal
Remove dead methods CreateBranchAsync and DeleteBranchAsync from the IGitHubClient interface and delete their implementations in OctokitGitHubClient so the public surface no longer exposes unused GitHub branch API calls. Ensure tests are updated so the test suite continues to pass.

## Non-Goals
- Add any new GitHub branch functionality.
- Replace branch operations with new abstractions or APIs.
- Change how branches are created/pushed (branches remain managed locally via LibGit2Sharp as-is).

## Components
- Core/Interfaces/IGitHubClient.cs — remove unused method declarations
- Infrastructure/GitHub/OctokitGitHubClient.cs — remove unused method implementations
- tests/ — remove any tests that reference these methods (if present)

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync method declarations |
| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync implementations (approx lines 250-280) |
| Modify | tests/ | Remove tests that call the removed methods (if any exist) |

## Interfaces

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

## Scenarios

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

## Sequence
1. Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs — remove CreateBranchAsync and DeleteBranchAsync method declarations.
2. Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs — delete the CreateBranchAsync and DeleteBranchAsync method implementations (preserve the rest of the class).
3. Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync. Remove those test methods or update them to not call removed methods. Prefer deletion unless the test verifies other behavior.
4. Build solution; fix any compilation issues (none expected if methods were truly unused).
5. Run unit tests: dotnet test (xUnit). Ensure all tests pass.
6. Commit changes with a short message: "Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient and OctokitGitHubClient".

## Test Matrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/ | Remove or update tests that reference CreateBranchAsync/DeleteBranchAsync. All other tests run unchanged (FW-02). |

DESIGN CHECKLIST
- Is this the SIMPLEST design that satisfies acceptance criteria?  
  Yes — direct deletion of unused methods and their implementations without adding abstractions.

- Are you adding abstractions/interfaces?  
  No — we are removing two methods. No new interfaces added (follows NO ABSTRACTIONS).

- Are you adding config?  
  No.

- Did you copy patterns from existing similar code?  
  Yes — changes are confined to matching existing file/class patterns used in the repo.

- Will implementation fit file size limits?  
  Yes — changes are small removals.

- Framework / Pattern references:  
  Uses .NET 8 and xUnit as-is (FW-01, FW-02). This is a refactor rather than a feature; no new framework or pattern is added.

Notes
- Minimalist change: only remove declarations and their bodies. Do not change other members.
- If any code unexpectedly references the removed methods (compilation error), update that code to call existing RepoGit methods instead — but per repository analysis these calls do not exist.
- Keep commit atomic and small so reviewers can easily validate removal.