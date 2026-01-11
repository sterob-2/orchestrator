# System Prompt

You are a question classifier for an SDLC workflow. Classify questions as Technical, Product, or Ambiguous. Return JSON only.

# User Prompt

Issue Context:
Title: Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient
Body: ## Problem

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

Clarified Story:
Remove two dead methods (CreateBranchAsync and DeleteBranchAsync) from the IGitHubClient interface and their implementations in OctokitGitHubClient, and remove any associated tests. These methods are unused in the repo because branch creation/deletion is handled locally via LibGit2Sharp and pushed via CommitAndPush. The change should be minimal: delete the two interface method signatures, delete their implementations in OctokitGitHubClient, remove any tests that reference them, and ensure the solution builds and tests pass.

Acceptance Criteria:
- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
- Given the OctokitGitHubClient implementation, when the change is applied, then the CreateBranchAsync and DeleteBranchAsync method implementations must be removed and the project must compile successfully.
- Given the test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
- Given the repository codebase, when performing a text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references (aside from removed tests) to these method names.

Question to Classify:
Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?

Classification Guidelines:

TECHNICAL questions are about:
- Implementation details (how to code something)
- Architecture decisions (which pattern, structure)
- Framework/library choices (which tool to use)
- Code organization (where to put files)
- Error handling strategies
- Performance considerations
Examples: 'Which framework?', 'How should errors be handled?', 'What's the API structure?'

PRODUCT questions are about:
- User workflows (how users interact)
- Business logic (what should happen)
- Use cases (when/why feature is used)
- Requirements clarification (what exactly is needed)
- User expectations (what users see/experience)
- Feature behavior (how feature should work)
Examples: 'What happens when user clicks X?', 'What's the expected behavior?', 'Which users can access this?'

AMBIGUOUS questions:
- Can't be clearly classified
- Require human judgment
- Mix technical and product concerns

Return JSON:
{
  "question": string (the question being classified),
  "type": "Technical" | "Product" | "Ambiguous",
  "reasoning": string (brief explanation of classification)
}
