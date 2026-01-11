# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

STATUS: DRAFT  
UPDATED: 2026-01-11 14:31:57 UTC

## Goal
Remove two dead methods from the GitHub client surface: CreateBranchAsync and DeleteBranchAsync. This reduces unused code, simplifies the interface, and does not change runtime behavior because branch operations are performed locally via LibGit2Sharp.

## Non-Goals
- Replacing local branch operations with API calls
- Adding alternate implementations or abstractions
- Introducing configuration or feature flags

## Components
- Core/Interfaces/IGitHubClient.cs (interface)
- Infrastructure/GitHub/OctokitGitHubClient.cs (concrete implementation)
- tests/ (remove any tests that reference the removed methods)

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs | Remove CreateBranchAsync/DeleteBranchAsync signatures |
| Modify | src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Remove corresponding implementations (around lines ~250-280) |
| Modify | tests/ | Remove any unit tests that assert/Create/Delete branch via IGitHubClient (if present) |

## Interfaces
// BEFORE: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
```csharp
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
```

// AFTER: src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs
```csharp
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
```

// BEFORE: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
```csharp
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
```

// AFTER: src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs
```csharp
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
```

// BEFORE: tests/ (example test methods that may exist)
```csharp
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
```

// AFTER: tests/ (those tests removed)
```csharp
using System.Threading.Tasks;
using Xunit;
using Moq;
using Orchestrator.App.Core.Interfaces;

public class GitHubClientTests
{
    // Tests for CreateBranchAsync/DeleteBranchAsync removed because methods no longer exist.
    // Other tests (GetRepositoryAsync, GetDefaultBranchNameAsync, etc.) remain unchanged.
}
```

## Scenarios

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

## Sequence
1. Edit src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs and remove the two method signatures.
2. Edit src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs and delete the corresponding method implementations.
3. Search tests/ for any tests that reference CreateBranchAsync or DeleteBranchAsync and remove those test methods or files.
4. Build solution and run test suite (xUnit) and fix any incidental compile errors (e.g., using directives only).
5. Commit changes.

## Test Matrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/ | Run full xUnit suite after removal; ensure no test refers to removed methods |
| Compile | src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs<br>src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs | Ensure project compiles with removed signatures/implementations |

DESIGN CHECKLIST - Before finalizing spec:
- Is this the SIMPLEST design that satisfies acceptance criteria?
  - Yes. We perform minimal removals only.
- Are you adding abstractions/interfaces?
  - No. We remove unused members; no new abstractions added.
- Are you adding config?
  - No.
- Did you copy patterns from existing similar code?
  - Yes. Follow existing class/interface layout; no new patterns introduced.
- Will implementation fit file size limits?
  - Yes — only small deletions required.

Notes:
- This change follows MINIMAL FIRST: remove dead API surface without introducing behavior changes.  
- Testing: use existing xUnit test runner (FW-02).