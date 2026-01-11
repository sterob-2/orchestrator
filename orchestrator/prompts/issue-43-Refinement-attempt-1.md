# System Prompt

You are an SDLC refinement assistant following the MINIMAL FIRST principle. CORE PRINCIPLES:
1. START MINIMAL: Always propose the simplest solution that satisfies the requirement
2. NO FUTURE-PROOFING: Do not add features, config, or abstractions for hypothetical scenarios
3. ONE FEATURE PER ISSUE: If the issue mixes multiple concerns, ask user to split it
4. MAX 3-5 ACCEPTANCE CRITERIA: More criteria = issue too large, should be split
5. AFTER INCORPORATING ANSWERS: Return ZERO open questions. Do not generate new questions.

Do not invent requirements. Clarify ambiguity and produce structured JSON only. CRITICAL: All acceptance criteria MUST be testable using BDD format (Given/When/Then) or keywords (should, must, verify, ensure).

# User Prompt

PRODUCT VISION:
- Quality over speed: Slow and correct beats fast and broken
- Minimal viable first: Start with simplest solution, extend only when reviews request
- Small focused issues: One feature, max 3-5 acceptance criteria
- No over-engineering: No abstractions for single use, no speculative features

Issue Title:
Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

Issue Body:
## Problem

`IGitHubClient` contains `CreateBranchAsync` and `DeleteBranchAsync` methods that are **never used** in the codebase:

**Dead code:**
- `IGitHubClient.CreateBranchAsync(string branchName)` 
- `IGitHubClient.DeleteBranchAsync(string branchName)`
- `OctokitGitHubClient.CreateBranchAsync(...)` (implementation)
- `OctokitGitHubClient.DeleteBranchAsync(...)` (implementation)

**Why they exist:**
These methods create/delete branches via GitHub REST API.

**Why they're unused:**
Branch operations are done **locally via LibGit2Sharp** in `RepoGit.EnsureBranch()`:
```csharp
// ContextBuilderExecutor.cs:35
WorkContext.Repo.EnsureBranch(branchName, baseBranch);

// RepoGit.cs:82 - Fetches from remote
Commands.Fetch(repo, "origin", ...);

// RepoGit.cs:107-128 - Creates branch locally
localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
```

Branches are pushed to remote during `CommitAndPush()`, not via API calls.

## Proposal

Remove dead code for YAGNI compliance and reduced surface area.

## Acceptance Criteria

1. Given `IGitHubClient` interface, when unused methods are removed, then `CreateBranchAsync` and `DeleteBranchAsync` no longer exist
2. Given `OctokitGitHubClient` implementation, when unused methods are removed, then implementations are deleted
3. Given the test suite, when dead code is removed, then all tests pass (no code depends on these methods)

## Files to Update

- `src/Orchestrator.App/Core/Interfaces/IGitHubClient.cs` (remove interface methods)
- `src/Orchestrator.App/Infrastructure/GitHub/OctokitGitHubClient.cs` (remove implementations around lines 250-280)
- `tests/` (remove any tests for these methods, if they exist)

## Impact

- Cleaner interface
- Less dead code
- YAGNI compliance
- No functional change (methods were never called)

Answered Questions (1 total):

- [x] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
  **Answer (ProductOwner):** I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.

IMPORTANT: Incorporate these answers into your refinement.
Return openQuestions: [] (empty array) in your JSON response.

Previous Refinement:
# Refinement: Issue #43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

**Status**: Refinement Complete
**Generated**: 2026-01-11 14:31:56 UTC

## Clarified Story

Remove two unused methods from the internal GitHub client surface: delete CreateBranchAsync(string) and DeleteBranchAsync(string) from IGitHubClient, remove their implementations from OctokitGitHubClient, and remove any unit tests that reference them. The change is minimal: only remove the interface signatures, the corresponding method bodies, and any direct tests; then ensure the solution builds and the test suite passes. Note: the ProductOwner could not determine whether IGitHubClient is published or consumed externally; if it is part of a public API or distributed NuGet package this change would be breaking and must not be merged without coordinating a major-version change or alternative compatibility plan.

## Acceptance Criteria (4)

- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
- Given the OctokitGitHubClient implementation file, when the CreateBranchAsync and DeleteBranchAsync methods are removed, then dotnet build must complete successfully for the solution (no missing-member compile errors).
- Given the repository test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
- Given the repository codebase after removing the methods and any tests referencing them, when performing a code/text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references to these method names.

## Questions

**How to answer:**
1. Edit this file and add your answer after the question
2. Mark the checkbox with [x] when answered
3. Commit and push changes
4. Remove `blocked` label and add `dor` label to re-trigger

- [x] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
  **Answer (ProductOwner):** I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.



Playbook Constraints:
- Core Principles:
  - Minimal First: Always start with simplest solution that satisfies requirements
  - No Future-Proofing: Do not add features, config, or abstractions for hypothetical scenarios
  - Small Focused Issues: One feature per issue, max 3-5 acceptance criteria
  - Follow Existing Patterns: Copy structure from similar code in codebase before inventing new patterns
  - Quality Over Speed: Slow and correct beats fast and broken
- Allowed Frameworks:
  - .NET 8 (FW-01)
  - xUnit (FW-02)
  - Moq (FW-03)
- Forbidden Frameworks:
  - Newtonsoft.Json
- Allowed Patterns:
  - Clean Architecture (PAT-01)
  - Repository Pattern (PAT-02)
  - Records for DTOs (PAT-03)
  - Dependency Injection (PAT-04)
- Forbidden Patterns:
  - Singleton (ANTI-01)
  - God Objects (ANTI-02)
  - Premature Abstraction (ANTI-03)
  - Config Overload (ANTI-04)
  - Speculative Features (ANTI-05)

Existing Spec (if any):
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

IMPORTANT - Acceptance Criteria Requirements:
- You MUST write at least 3 testable acceptance criteria
- Each criterion MUST use BDD format or testable keywords
- BDD format: 'Given [context], when [action], then [outcome]'
- Testable keywords: 'should', 'must', 'verify', 'ensure', 'given', 'when', 'then'
- Each criterion must be specific, verifiable, and testable

Examples of VALID acceptance criteria:
  ✓ 'Given a user is logged in, when they click logout, then they should be redirected to the login page'
  ✓ 'The system must validate email format before saving'
  ✓ 'Should display error message when required fields are empty'
  ✓ 'Given invalid credentials, when user attempts login, then access must be denied'
  ✓ 'The API must return 401 status code for unauthorized requests'

Examples of INVALID acceptance criteria (will be rejected):
  ✗ 'User can log out' (not testable - no verification criteria)
  ✗ 'Good error handling' (vague, not verifiable)
  ✗ 'Works correctly' (not specific)

SCOPE CHECK - Before finalizing, verify:
- Does this issue implement ONE clear feature? If not, suggest splitting.
- Are there 3-5 acceptance criteria? More than 5 = too large, suggest splitting.
- Are you adding config/features for 'what if' scenarios? Remove them.
- Can the solution be simpler? If yes, simplify.

Return JSON with fields:
{
  "clarifiedStory": string,
  "acceptanceCriteria": [string],
  "openQuestions": [string],  // IMPORTANT: Do NOT include 'Question #X:' prefix - just the question text
  "complexitySignals": [string],
  "complexitySummary": string,
  "answeredQuestions": [{ "questionNumber": int, "question": string, "answer": string, "answeredBy": string }] (optional)
}
