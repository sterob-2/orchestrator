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
**Generated**: 2026-01-11 13:22:20 UTC

## Clarified Story

Remove two dead methods from the internal GitHub client surface: delete CreateBranchAsync(string) and DeleteBranchAsync(string) from IGitHubClient, remove their implementations from OctokitGitHubClient, and remove any tests that reference them. These methods are unused because branch creation/deletion is performed locally via LibGit2Sharp and pushed via CommitAndPush. The change must be minimal: only remove the interface signatures, the corresponding method bodies, and any direct tests; then ensure the solution builds and the test suite passes.

## Acceptance Criteria (4)

- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
- Given the OctokitGitHubClient implementation file, when the change is applied, then the CreateBranchAsync and DeleteBranchAsync method implementations must be removed and the project must compile successfully (dotnet build returns success).
- Given the repository test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
- Given the repository codebase after removing the methods and any tests referencing them, when performing a text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references to these method names.

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
