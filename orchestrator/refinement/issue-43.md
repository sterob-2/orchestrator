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

