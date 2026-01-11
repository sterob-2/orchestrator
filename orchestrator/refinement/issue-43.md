# Refinement: Issue #43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

**Status**: Refinement Complete
**Generated**: 2026-01-11 13:21:46 UTC

## Clarified Story

Remove two dead methods (CreateBranchAsync and DeleteBranchAsync) from the IGitHubClient interface and their implementations in OctokitGitHubClient, and remove any associated tests. These methods are unused in the repo because branch creation/deletion is handled locally via LibGit2Sharp and pushed via CommitAndPush. The change should be minimal: delete the two interface method signatures, delete their implementations in OctokitGitHubClient, remove any tests that reference them, and ensure the solution builds and tests pass.

## Acceptance Criteria (4)

- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
- Given the OctokitGitHubClient implementation, when the change is applied, then the CreateBranchAsync and DeleteBranchAsync method implementations must be removed and the project must compile successfully.
- Given the test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by missing CreateBranchAsync/DeleteBranchAsync members.
- Given the repository codebase, when performing a text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references (aside from removed tests) to these method names.

## Questions

**How to answer:**
1. Edit this file and add your answer after the question
2. Mark the checkbox with [x] when answered
3. Commit and push changes
4. Remove `blocked` label and add `dor` label to re-trigger

- [ ] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
  **Answer:** _[Pending]_

- [ ] **Question #2:** Should I remove any unit/integration tests that reference these methods, or do you prefer converting them to assert that branch operations occur via LibGit2Sharp instead?
  **Answer:** _[Pending]_

