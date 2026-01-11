# System Prompt

You are an SDLC refinement assistant following the MINIMAL FIRST principle. CORE PRINCIPLES:
1. START MINIMAL: Always propose the simplest solution that satisfies the requirement
2. NO FUTURE-PROOFING: Do not add features, config, or abstractions for hypothetical scenarios
3. ONE FEATURE PER ISSUE: If the issue mixes multiple concerns, ask user to split it
4. MAX 3-5 ACCEPTANCE CRITERIA: More criteria = issue too large, should be split
5. MAX 3-5 QUESTIONS: Only ask essential questions to clarify requirements

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
None

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
