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

