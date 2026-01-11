# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

STATUS: DRAFT
UPDATED: 2026-01-11 14:43:08 UTC

## Goal
Remove two unused GitHub branch API methods from the codebase to reduce dead surface area: IGitHubClient.CreateBranchAsync and IGitHubClient.DeleteBranchAsync, and the corresponding implementations in OctokitGitHubClient. Ensure tests are updated if any referenced these methods so the test suite remains green.

## Non-Goals
- Replacing branch operations with a new mechanism
- Adding new abstractions or alternative branch APIs
- Changing any behavior of RepoGit or CommitAndPush flow

## Components
- Core/Interfaces: IGitHubClient (interface removal)
- Infrastructure/GitHub: OctokitGitHubClient (remove implementations)
- tests/: Remove any unit tests that target the removed methods

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync and DeleteBranchAsync method declarations |
| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove corresponding method implementations (around lines ~250-280) |
| Modify | tests/GitHubClientTests.cs | Remove tests that reference CreateBranchAsync/DeleteBranchAsync, if present; keep unrelated tests |

## Interfaces
// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
```csharp
using System.Threading.Tasks;

namespace Orchestrator.App.Core.Interfaces
{
    public interface IGitHubClient
    {
        // existing methods kept
        Task<string> GetRepositoryDefaultBranchAsync(string owner, string repo);
        Task<bool> RepositoryExistsAsync(string owner, string repo);

        // Dead code - unused in codebase
        Task CreateBranchAsync(string branchName);
        Task DeleteBranchAsync(string branchName);
    }
}
```

// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
```csharp
using System.Threading.Tasks;

namespace Orchestrator.App.Core.Interfaces
{
    public interface IGitHubClient
    {
        // existing methods kept
        Task<string> GetRepositoryDefaultBranchAsync(string owner, string repo);
        Task<bool> RepositoryExistsAsync(string owner, string repo);

        // CreateBranchAsync and DeleteBranchAsync removed - no longer part of interface
    }
}
```

// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
```csharp
using System.Threading.Tasks;
using Octokit;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Infrastructure.GitHub
{
    public class OctokitGitHubClient : IGitHubClient
    {
        // other existing members and constructor omitted for brevity

        public async Task CreateBranchAsync(string branchName)
        {
            // Implementation that used GitHub REST API to create a branch (unused)
            var reference = new ReferenceUpdate("refs/heads/" + branchName);
            // ... implementation details
            await Task.CompletedTask;
        }

        public async Task DeleteBranchAsync(string branchName)
        {
            // Implementation that used GitHub REST API to delete a branch (unused)
            await Client.Git.Reference.Delete(owner, repo, "heads/" + branchName);
        }

        // other IGitHubClient members implemented below
    }
}
```

// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
```csharp
using System.Threading.Tasks;
using Octokit;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Infrastructure.GitHub
{
    public class OctokitGitHubClient : IGitHubClient
    {
        // other existing members and constructor omitted for brevity

        // CreateBranchAsync and DeleteBranchAsync implementations removed
        // Remaining IGitHubClient methods continue to be implemented below
    }
}
```

// BEFORE: tests/GitHubClientTests.cs
```csharp
using System.Threading.Tasks;
using Xunit;
using Moq;
using Orchestrator.App.Core.Interfaces;

public class GitHubClientTests
{
    [Fact]
    public async Task CreateBranchAsync_CreatesBranch()
    {
        // Test that wired up the Octokit implementation or IGitHubClient.CreateBranchAsync
        // (This test will be removed because the method is removed)
    }

    [Fact]
    public async Task DeleteBranchAsync_DeletesBranch()
    {
        // Test that wired up the Octokit implementation or IGitHubClient.DeleteBranchAsync
        // (This test will be removed because the method is removed)
    }

    [Fact]
    public async Task RepositoryExistsAsync_ReturnsTrue()
    {
        // Unrelated test retained
    }
}
```

// AFTER: tests/GitHubClientTests.cs
```csharp
using System.Threading.Tasks;
using Xunit;
using Moq;
using Orchestrator.App.Core.Interfaces;

public class GitHubClientTests
{
    // Tests that referenced CreateBranchAsync/DeleteBranchAsync removed.

    [Fact]
    public async Task RepositoryExistsAsync_ReturnsTrue()
    {
        // Unrelated test retained
    }
}
```

## Scenarios

Scenario: Remove interface methods
Given the IGitHubClient interface contains CreateBranchAsync and DeleteBranchAsync
When the repository is updated per this spec
Then IGitHubClient no longer contains CreateBranchAsync or DeleteBranchAsync declarations

Scenario: Remove Octokit implementations
Given OctokitGitHubClient implemented CreateBranchAsync and DeleteBranchAsync
When the repository is updated per this spec
Then OctokitGitHubClient no longer contains those method implementations and still compiles

Scenario: Tests remain passing
Given the test suite (tests/) may include tests for the removed methods
When tests referencing CreateBranchAsync/DeleteBranchAsync are removed
Then the full test suite passes with no references to the removed methods

Scenario: No runtime behavioral change
Given branch creation/deletion is already performed locally via RepoGit.EnsureBranch and CommitAndPush
When the API methods are removed
Then runtime behavior of branch creation/push remains unchanged

## Sequence
1. Modify src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs to remove CreateBranchAsync and DeleteBranchAsync declarations (exact lines removed shown above).
2. Modify src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs to remove corresponding method implementations (around the noted region).
3. Run test suite and remove any tests that reference the removed methods (update tests/GitHubClientTests.cs).
4. Build project; run tests; commit changes.

## Test Matrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/GitHubClientTests.cs | Remove tests that directly assert CreateBranchAsync/DeleteBranchAsync behavior; keep unrelated tests |
| Integration/Unit | tests/ (full suite) | Run entire test suite to ensure no remaining references or compile errors |

DESIGN CHECKLIST - Before finalizing spec:
- Is this the SIMPLEST design that satisfies acceptance criteria?
  - Yes. Directly remove two unused methods and their implementations; no new abstractions or features.
- Are you adding abstractions/interfaces?
  - No. We only remove methods; no new interfaces added.
- Are you adding config?
  - No.
- Did you copy patterns from existing similar code?
  - Yes. This follows the existing pattern of removing dead code without changing callers.
- Will implementation fit file size limits?
  - Yes. Changes are small edits to existing files.
- IMPORTANT: If adding new files (Operation: Add in Touch List), reference at least one allowed framework ID and pattern ID from the playbook
  - No new files added.

Acceptance Criteria mapping
1. IGitHubClient no longer contains CreateBranchAsync/DeleteBranchAsync — covered in Interfaces BEFORE/AFTER.
2. OctokitGitHubClient implementations removed — covered in Interfaces BEFORE/AFTER.
3. Test suite passes after removing any tests targeting these methods — covered by Scenarios and Test Matrix.

Implementation notes (minimal)
- Do not alter any other IGitHubClient methods.
- Do not change RepoGit behavior; this change is orthogonal.
- Ensure no compile-time references remain to the removed methods. Run 'dotnet build' and 'dotnet test' to verify.