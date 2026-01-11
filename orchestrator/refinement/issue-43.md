# Refinement: Issue #43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

**Status**: Refinement Complete
**Generated**: 2026-01-11 14:43:07 UTC

## Clarified Story

Remove two dead methods from the internal GitHub client surface: delete CreateBranchAsync(string) and DeleteBranchAsync(string) from IGitHubClient, remove their implementations from OctokitGitHubClient, and remove any unit tests that reference them. This is a minimal refactor: edit the interface file to remove the two signatures, delete the corresponding method bodies from the OctokitGitHubClient class, remove any tests that reference these methods, then build and run tests to verify no compilation or test failures. If IGitHubClient is part of a public/distributed API consumed by external projects, do NOT merge without coordinating a breaking-change plan; the ProductOwner could not determine whether it is published externally, so that check must be performed before merging.

## Acceptance Criteria (4)

- Given the IGitHubClient interface file, when the change is applied, then IGitHubClient must not declare CreateBranchAsync(string) or DeleteBranchAsync(string).
- Given the OctokitGitHubClient implementation, when CreateBranchAsync and DeleteBranchAsync implementations are removed and the solution is built, then dotnet build must complete successfully with no missing-member compile errors.
- Given the repository test suite, when tests are executed after the change, then all tests must pass and there must be no failing tests caused by the removal of CreateBranchAsync/DeleteBranchAsync.
- Given the repository codebase after removal, when performing a code/text search for 'CreateBranchAsync' and 'DeleteBranchAsync', then there must be zero remaining references to these method names (interface, implementation, or tests).

## Questions

**How to answer:**
1. Edit this file and add your answer after the question
2. Mark the checkbox with [x] when answered
3. Commit and push changes
4. Remove `blocked` label and add `dor` label to re-trigger

- [x] **Question #1:** Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?
  **Answer (ProductOwner):** I cannot determine from the provided information whether IGitHubClient is part of a public API or distributed NuGet package. If it is published and consumed externally, removing methods would be a breaking change; if it is only used internally within this repository, removing them is safe per the acceptance criteria.

