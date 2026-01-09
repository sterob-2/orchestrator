# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

An AI-driven SDLC orchestrator built on the Microsoft Agent Framework. Processes GitHub issues through a workflow graph (Refinement → DoR → TechLead → Spec Gate → Dev → Code Review → DoD → Release) and keeps GitHub labels in sync with workflow state. Built with .NET 8, integrates with Claude/OpenAI for intelligent task execution, and supports Model Context Protocol (MCP) for filesystem, git, and GitHub operations.

## Build and Test Commands

**Restore and Build:**
```bash
dotnet restore src/Orchestrator.App/Orchestrator.App.csproj
dotnet build src/Orchestrator.App/Orchestrator.App.csproj --configuration Release
```

**Run Tests:**
```bash
dotnet test tests/Orchestrator.App.Tests.csproj --configuration Release
```

**Run Tests with Coverage:**
```bash
dotnet tool update --global dotnet-coverage
dotnet-coverage collect "dotnet test tests/Orchestrator.App.Tests.csproj --configuration Release" -f xml -o coverage.xml
```

**Run a Single Test:**
```bash
dotnet test tests/Orchestrator.App.Tests.csproj --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

**Run Locally:**
```bash
dotnet run --project src/Orchestrator.App/Orchestrator.App.csproj
```

**Docker Commands:**
```bash
# Build and start
docker compose -f docker-compose.yml up -d --build

# View logs
docker compose -f docker-compose.yml logs -f

# Stop
docker compose -f docker-compose.yml down
```

## High-Level Architecture

### Application Entry Point and Initialization

**File:** `src/Orchestrator.App/Program.cs`

The application follows this initialization flow:
1. Load environment files (`.env`, `orchestrator/.env`)
2. Load `OrchestratorConfig` from environment variables
3. Create `AppServices` via `ServiceFactory` (GitHub client, Workspace, Git client, LLM client, MCP Manager)
4. Initialize git repository and MCP client manager
5. Setup supporting handlers: `InMemoryWorkflowCheckpointStore`, `LabelSyncHandler`, `HumanInLoopHandler`, `FileWorkflowMetricsStore`
6. Create `WorkflowRunner` and `GitHubIssueWatcher`
7. Start `GitHubWebhookListener` on configured host/port
8. Wait for webhook triggers or manual scan requests

### Workflow Engine Architecture

The orchestrator uses a graph-based workflow engine with 9 sequential stages:

1. **ContextBuilder** - Entry point, determines start stage from GitHub labels
2. **Refinement** - Clarifies requirements (LLM: TechLeadModel)
3. **DoR** - Definition of Ready validation gate
4. **TechLead** - Architectural review (LLM: TechLeadModel)
5. **SpecGate** - Specification completeness validation (16 checks: Spec-01 to Spec-16)
6. **Dev** - Code implementation (LLM: DevModel)
7. **CodeReview** - AI-driven code review (LLM: OpenAiModel)
8. **DoD** - Definition of Done validation (42 checks: DoD-01 to DoD-42)
9. **Release** - Release finalization

**Key Files:**
- `src/Orchestrator.App/Workflows/WorkflowRunner.cs` - Central orchestrator
- `src/Orchestrator.App/Workflows/WorkflowFactory.cs` - Builds workflow graphs
- `src/Orchestrator.App/Workflows/WorkflowStageGraph.cs` - Defines state transitions
- `src/Orchestrator.App/Workflows/SDLCWorkflow.cs` - Microsoft Agent Framework integration

**Transition Rules:**
- Success path: advances to next stage
- Failure path: may loop back (e.g., CodeReview→Dev, SpecGate→TechLead)
- Iteration limits enforced per stage (configurable via MAX_*_ITERATIONS env vars)

### Stage Executors Pattern

All executors extend `WorkflowStageExecutor : Executor<WorkflowInput, WorkflowOutput>` and follow a template method pattern:
- `HandleAsync()` - Common flow: limit checking, attempt tracking, execution, next stage determination
- `ExecuteAsync()` - Override for stage-specific logic
- `DetermineNextStage()` - Override to customize routing

**Executor Locations:** `src/Orchestrator.App/Workflows/Executors/`

### Data Flow

```
GitHub Issue Event
    ↓ (webhook)
GitHubWebhookListener (validates HMAC-SHA256)
    ↓
GitHubIssueWatcher.RequestScan()
    ↓
Retrieve open issues with workflow labels
    ↓
WorkflowRunner.RunAsync(context, stage)
    ↓
Build workflow graph → Execute via Microsoft Agent Framework
    ↓
Post-execution: LabelSyncHandler, HumanInLoopHandler, MetricsRecorder
```

### WorkContext Structure

The `WorkContext` record passes shared state through all executors:
- `WorkItem` - GitHub issue being processed
- `IGitHubClient` - GitHub API operations
- `OrchestratorConfig` - Global configuration
- `IRepoWorkspace` - Filesystem access
- `IRepoGit` - Git operations
- `ILlmClient` - LLM client for AI calls
- `IMetricsRecorder` - Metrics tracking
- `McpClientManager` - MCP tool access
- `SharedState` - ConcurrentDictionary for inter-executor state

### MCP (Model Context Protocol) Integration

**File:** `src/Orchestrator.App/Infrastructure/Mcp/McpClientManager.cs`

Manages connections to MCP servers running in Docker containers:
- **Filesystem MCP** - `@modelcontextprotocol/server-filesystem`
- **Git MCP** - `mcp-server-git`
- **GitHub MCP** - `github-mcp-server`

`McpFileOperations` wrapper provides abstraction: `ReadAllTextAsync()`, `WriteAllTextAsync()`, `ExistsAsync()`, `DeleteAsync()`, `ListFilesAsync()`

`FileOperationHelper` implements fallback: try MCP first, fall back to local Workspace operations.

### Watcher Trigger Mechanisms

The orchestrator supports two trigger mechanisms that can run independently or simultaneously in **hybrid mode**:

#### Polling (Default: Enabled)

**File:** `src/Orchestrator.App/Watcher/GitHubIssueWatcher.cs`

Automatic periodic scanning with adaptive intervals:
- **Idle polling** (default: 60s): Used when no work items found or only `ready-for-agents` label present
- **Fast polling** (default: 10s): Activated when work items with active labels detected:
  - `agent:planner`, `agent:dor`, `agent:techlead`, `agent:spec-gate`
  - `agent:dev`, `agent:test`, `agent:release`
  - `code-review-needed`, `code-review-changes-requested`

**Configuration:**
- `POLL_INTERVAL_SECONDS=60` - Idle polling interval
- `FAST_POLL_INTERVAL_SECONDS=10` - Fast polling interval
- Set `POLL_INTERVAL_SECONDS=0` to disable polling (webhook-only mode)

#### Webhook Listener (Optional)

**File:** `src/Orchestrator.App/Watcher/GitHubWebhookListener.cs`

HTTP listener for GitHub webhooks:
- Listens on configurable host/port (default: `localhost:5005`)
- Validates HMAC-SHA256 signatures using `WEBHOOK_SECRET`
- Filters relevant events: issues opened/edited/labeled/unlabeled/reopened
- Attempts HTTPS first, falls back to HTTP with warning

**Configuration:**
- `WEBHOOK_PORT=5005` - Port to listen on
- `WEBHOOK_LISTEN_HOST=localhost` - Host to bind
- `WEBHOOK_PATH=/webhook` - Endpoint path
- `WEBHOOK_SECRET=` - Optional signature validation
- Set `WEBHOOK_PORT=0` to disable webhooks (polling-only mode)

#### Hybrid Mode (Default)

When both polling and webhooks are enabled:
- Watcher responds to EITHER polling timer OR webhook signals
- Webhook provides instant triggers when available
- Polling ensures work gets processed even if webhooks fail or aren't configured
- Provides maximum reliability and responsiveness

### Gate Validators

**SpecGateValidator** (`src/Orchestrator.App/Workflows/Executors/SpecGateValidator.cs`):
- Validates specification completeness (16 checks)
- Checks Goal, Non-Goals, Components, Touch List, Interfaces, Scenarios (Gherkin), Sequence, Test Matrix
- Validates against playbook (allowed/forbidden frameworks and patterns)

**DodGateValidator** (`src/Orchestrator.App/Workflows/Executors/DodGateValidator.cs`):
- Validates Definition of Done (42 checks)
- Checks CI status, SonarQube quality gate, coverage thresholds, acceptance criteria completion, touch list satisfaction
- Validates no TODO/FIXME markers, code review passed, spec status COMPLETE

**DorGateValidator** (`src/Orchestrator.App/Workflows/Executors/DorGateValidator.cs`):
- Validates Definition of Ready
- Checks proper labeling, acceptance criteria defined, touch list specified

## Important File Locations

**Configuration:**
- `docs/architecture-playbook.yaml` - Allowed/forbidden frameworks & patterns (used by SpecGate)
- `.env` or `orchestrator/.env` - Environment configuration

**Templates:** (in `docs/templates/`)
- `spec.md` - Technical specification template
- `plan.md` - Plan template
- `review.md` - Code review template
- `questions.md` - Questions template

**Artifacts Created During Workflow:** (in `orchestrator/`)
- `specs/issue-{NUMBER}.md` - Generated technical specifications
- `questions/issue-{NUMBER}.md` - Open questions from refinement
- `reviews/issue-{NUMBER}.md` - AI code review findings
- `release/issue-{NUMBER}.md` - Release notes
- `metrics/workflow-metrics.jsonl` - Execution metrics

**Path Constants:** `src/Orchestrator.App/Core/Models/WorkflowPaths.cs`

## Special Conventions

### Branch Naming

Branches created by the orchestrator follow the pattern:
```
agent/issue-{NUMBER}-{slug}
```
Example: `agent/issue-123-implement-auth-system`

Slugification: lowercase, ASCII letters/digits only, hyphens for separators, no leading/trailing hyphens.

**Implementation:** `src/Orchestrator.App/Utilities/WorkItemBranch.cs`

### Label-Based State Machine

The orchestrator determines workflow stage from GitHub labels:
- `ready-for-agents` - Initial work item label
- `in-progress` - Currently being processed
- `done` - Workflow complete
- `blocked` - Blocked waiting for human
- `agent:planner`, `agent:dor`, `agent:techlead`, `agent:spec-gate`, `agent:dev`, `agent:test`, `agent:release` - Stage-specific labels
- `code-review-needed`, `code-review-approved`, `code-review-changes-requested` - Code review states
- `user-review-required` - Needs human attention

Labels are configurable via environment variables (see `.env.example`).

### Mode System

Work items can have mode labels affecting execution:
- `mode:minimal` (default) - Lightweight execution
- `mode:batch` - Batch processing mode
- `mode:tdd` - Test-driven development mode

Modes are passed to the Dev executor to customize implementation approach.

### Spec Status Tracking

Specifications track completion status in markdown:
```markdown
## Status: PENDING
## Status: IN_PROGRESS
## Status: COMPLETE
```

Updated during workflow execution by TechLead, SpecGate, and Dev executors.

### Parsing Utilities

**SpecParser** (`src/Orchestrator.App/Parsing/SpecParser.cs`):
- Parses markdown specs into structured sections
- Bilingual support (English + German section names)

**TouchListParser** (`src/Orchestrator.App/Parsing/TouchListParser.cs`):
- Parses markdown table format for file operations
- Operations: Add, Modify, Delete, Forbidden

**PlaybookParser** (`src/Orchestrator.App/Parsing/PlaybookParser.cs`):
- YAML deserialization for architecture playbook
- Defines allowed/forbidden frameworks and design patterns

**GherkinValidator** (`src/Orchestrator.App/Parsing/GherkinValidator.cs`):
- Validates scenario syntax ("Scenario:" keyword required)
- Supports Given/When/Then structure

## Development Guidelines

### Coding Style
- C# with `net8.0`, implicit usings, and nullable enabled
- Indentation: 4 spaces
- Naming: PascalCase for types/methods, camelCase for locals/parameters
- File naming: align with primary type name (e.g., `RepoGit.cs` for `RepoGit`)

### Testing
- Frameworks: xUnit with Moq and FluentAssertions
- Test files: `*Tests.cs` naming in `tests/` directory
- Organize by area: `tests/Core`, `tests/Infrastructure`, `tests/Utilities`, `tests/Agents`
- Use `tests/TestHelpers` for shared setup
- Keep coverage stable (target: 80%+)
- Do not commit generated artifacts like `coverage.xml`

### Work Chunks & Quality Gates
- Work in small, reviewable chunks
- After each chunk: add/adjust tests, run build and tests, then run coverage before pushing
- Only commit when build and tests are green and coverage is maintained
- Do not use emojis in commits, PRs, or documentation

### Commit & Pull Request Guidelines
- Prefer conventional prefixes: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`
- Keep commits focused
- Include test results in PR descriptions
- PRs should summarize changes, list testing, and link related issues

### Configuration & Security
- The app reads environment variables and `.env` files (see `.env.example` for all options)
- MCP integrations use Docker; ensure Docker daemon is available when running locally
- Never commit secrets (API keys, tokens) to the repository
- Use `WEBHOOK_SECRET` for webhook signature validation in production

## Architecture Playbook

**File:** `docs/architecture-playbook.yaml`

Defines project-specific constraints enforced by SpecGate:

**Allowed Frameworks:**
- FW-01: .NET 8 (8.x)
- FW-02: xUnit (2.x)

**Forbidden Frameworks:**
- Newtonsoft.Json (use System.Text.Json instead)

**Allowed Patterns:**
- PAT-01: Clean Architecture
- PAT-02: Repository Pattern

**Forbidden Patterns:**
- PAT-99: Singleton

Specifications must reference allowed patterns and avoid forbidden ones to pass SpecGate validation.
