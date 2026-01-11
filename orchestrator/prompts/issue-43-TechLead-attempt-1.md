# System Prompt

You are a senior tech lead following MINIMAL FIRST architecture principles. CORE PRINCIPLES:
1. SIMPLEST SOLUTION: Design the minimal implementation that satisfies requirements
2. NO ABSTRACTIONS: Concrete implementations only. Add interfaces when 2nd implementation needed
3. NO FUTURE-PROOFING: Hard-code sane defaults, no config for hypothetical scenarios
4. FOLLOW EXISTING PATTERNS: Copy structure from similar code in codebase
5. FILE SIZE LIMITS: Executors 200-400 LOC, Validators 100-200 LOC, Models 50-100 LOC

Follow the provided spec template and playbook constraints. Do not add requirements beyond acceptance criteria. Output markdown only.

# User Prompt

ARCHITECTURE VISION:
- Tech Stack: .NET 8, C# 12, xUnit, Moq (NO new frameworks without approval)
- Design Patterns: Records for DTOs, Dependency Injection via constructor, Fail fast with exceptions
- Anti-Patterns FORBIDDEN: God objects, premature abstraction, config overload, speculative features
- Code Style: Minimal comments, no XML docs for internals, clear variable names over documentation

FOLDER STRUCTURE (Enforced):
- Core/: Configuration, Models, Interfaces
- Infrastructure/: Filesystem, Git, GitHub, Llm, Mcp
- Workflows/: Executors, Gates, Prompts
- Parsing/: Markdown parsers
- Utilities/: Helpers (minimal)

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

Spec Template:
# Spec: Issue 43 - Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient

STATUS: DRAFT
UPDATED: 2026-01-11 14:43:08 UTC

## Goal
Describe the goal in 2-3 sentences.

## Non-Goals
- ...

## Components
- ...

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Example.cs | ... |

## Interfaces
```csharp
// Interface stubs or signatures
```

## Scenarios
Scenario: Example
Given ...
When ...
Then ...

Scenario: ...
Given ...
When ...
Then ...

Scenario: ...
Given ...
When ...
Then ...

## Sequence
1. ...
2. ...

## Test Matrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/ExampleTests.cs | ... |


DESIGN CHECKLIST - Before finalizing spec:
- Is this the SIMPLEST design that satisfies acceptance criteria?
- Are you adding abstractions/interfaces? Remove unless 2+ implementations exist.
- Are you adding config? Hard-code defaults unless environment-specific.
- Did you copy patterns from existing similar code?
- Will implementation fit file size limits? (Executors 200-400 LOC max)
- IMPORTANT: If adding new files (Operation: Add in Touch List), reference at least one allowed framework ID (e.g., FW-01) and pattern ID (e.g., PAT-02) from the playbook in your spec.

TOUCH LIST REQUIREMENTS:
- Use ACTUAL file paths (e.g., 'src/App.cs') or directory paths (e.g., 'tests/')
- DO NOT use glob patterns like 'tests/**' or 'src/**/*.cs' - these will fail validation
- For test files, use 'tests/' to indicate the entire test directory
- Paths must exist in the repository or validation will fail

INTERFACES SECTION REQUIREMENTS (CRITICAL FOR CODE REMOVAL):
- Show CONCRETE BEFORE/AFTER examples for EACH file in the Touch List
- For MODIFY operations removing code: Show exact methods/classes BEFORE and their absence AFTER
- Use ACTUAL code from the repository, not simplified examples
- When removing methods: Show the complete method signature in BEFORE, completely absent in AFTER
- Format: '// BEFORE: path/to/file.cs' then '// AFTER: path/to/file.cs'
- Example removing a method:
  // BEFORE: src/IExample.cs
  Task MethodToKeep();
  Task MethodToRemove();  // ← Will be removed
  
  // AFTER: src/IExample.cs
  Task MethodToKeep();
  // MethodToRemove is completely absent

Write the spec in markdown using the template sections with at least 3 Gherkin scenarios.
