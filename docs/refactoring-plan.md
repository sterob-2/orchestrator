# Refactoring Plan (SDLC Orchestrator Concept v3.1)

This plan breaks the v3.1 concept into parallelizable workstreams. Each workstream lists concrete tasks, outputs, and key dependencies.

## Workstream 1: Architecture Skeleton and Folder Layout
- Create target folder structure under `src/Orchestrator.App/` (Core, Workflows, Infrastructure, Parsing, Utilities, Watcher) and update namespaces.
- Move existing infrastructure files (GitHub, Git, Filesystem, LLM, MCP) into `Infrastructure/` without behavior changes.
- Introduce project-level `GlobalUsings` and shared `AssemblyInfo` (InternalsVisibleTo) to reduce boilerplate.
- Wire the app entrypoint to new structure with minimal `Program.cs` that loads config and starts watcher; no legacy fallback paths.
- Deliverable: compiles with watcher entrypoint; no legacy runner or feature flags.

## Workstream 2: Core Domain Models and Configuration
- Define Core/Models records: `WorkItem`, `WorkflowInput`, `GateResult`, `ParsedSpec`, `TouchListEntry`, `ProjectContext`, `ComplexityIndicators`.
- Define Core/Configuration types: `OrchestratorConfig`, `LabelConfig`, `WorkflowConfig`; keep `FromEnvironment` parity with current settings.
- Add Core/Interfaces: `IGitHubClient`, `IRepoGit`, `IRepoWorkspace`, `ILlmClient` mirroring current infrastructure contracts.
- Provide factories/adapters to wrap existing `OctokitGitHubClient`, `RepoGit`, `RepoWorkspace`, `LlmClient` behind interfaces.
- Deliverable: Core project compiles; infrastructure can be swapped via DI without changing callers.

## Workstream 3: Workflow Engine and Watcher
- Implement `Watcher/GitHubIssueWatcher` to translate labels into workflow start/reset signals; move polling logic into the watcher (no legacy runner).
- Build `Workflows/WorkflowFactory` and `WorkflowRunner` to assemble the graph from the concept (ContextBuilder → Refinement ↔ DoR → TechLead ↔ SpecGate → Dev → CodeReview ↔ Dev → DoD ↔ Dev → Release).
- Add checkpointing and iteration limit enforcement with configurable thresholds.
- Implement label synchronization handlers (`LabelSyncHandler`, `HumanInLoopHandler`) that project workflow state to board labels.
- Remove legacy agents and all feature-flagged flow switching while introducing the workflow runner.
- Deliverable: Running workflow skeleton with stub executors that return placeholder results and drive label updates.

## Workstream 4: Gates and Playbook Validation
- Implement DoR gate rules (DoR-01..07) against refinement output and issue metadata.
- Implement Spec Gate checks: required sections, Touch List format, Gherkin validity, file existence, playbook pattern/framework constraints.
- Implement DoD gate checks: GitHub checks API, SonarQube API placeholders, spec compliance (AKs, Touch List, forbidden files), review state, cleanup rules.
- Create `docs/architecture-playbook.yaml` template v2 and a parser/validator that executors can consume.
- Deliverable: Gate validators with unit tests and clear failure payloads for executor loops.

## Workstream 5: Executors Implementation
- RefinementExecutor: call LLM with constrained prompt, produce `RefinementResult`, loop with DoR gate feedback.
- TechLeadExecutor: generate spec per schema, produce `TechLeadResult`, include Touch List, interfaces, scenarios, sequences, test matrix.
- DevExecutor: honor Touch List, implement code and tests per spec; support modes (minimal/batch/tdd) and iteration limits.
- CodeReviewExecutor: implement finding categories, thresholds (blocker/major), loop control; emit `CodeReviewResult`.
- ReleaseExecutor: draft PR body, update labels, ensure DoD passed; do not auto-merge.
- Deliverable: Executors wired into workflow, respecting gate outputs and iteration limits.

## Workstream 6: Labeling, Modes, and State Sync
- Map labels from concept to runtime (`ready-for-agent`, stage labels, gate result labels, review labels).
- Implement mode overrides (`mode:batch`, `mode:tdd`) and default mode selection in DevExecutor.
- Ensure label updates happen idempotently and are driven from workflow events rather than ad hoc calls.
- Deliverable: Label/state sync module with tests; manual label interventions correctly steer workflow.

## Workstream 7: Metrics, Telemetry, and Limits
- Add metrics capture per workflow run (issue, mode, LLM calls, iterations, cost, duration, findings count, approval flag).
- Aggregate metrics scaffolding for future dashboards; persist to lightweight store (file or in-memory) initially.
- Enforce iteration limits (Refinement↔DoR, TechLead↔SpecGate, Dev↔Review, Dev↔DoD) with escalation to human review.
- Deliverable: Metrics recorder and limit enforcer integrated into WorkflowRunner.

## Workstream 8: Parsing Utilities and Spec Schema Support
- Implement `Parsing/SpecParser`, `TouchListParser`, `GherkinValidator`, `PlaybookParser` aligned to schema in Section 8.
- Migrate existing helpers (`AgentHelpers`, `AgentTemplateUtil`) into `Utilities/` and extend to support new schema requirements.
- Deliverable: Parsing utilities with unit tests covering happy paths and failure diagnostics.

## Workstream 9: Testing and Quality Gates
- Build unit tests for Core models/config, parsers, gates, label sync, and executor control flow (stubbed LLM/GitHub).
- Add integration tests for workflow happy path and gate failure loops using in-memory adapters.
- Align CI to run unit/integration suites; placeholder SonarQube and GitHub checks API clients for DoD validation.
- Deliverable: Green CI with coverage on new modules; documented test matrix mapping to DoD/Spec Gate rules.

## Workstream 10: Migration and Cleanup
- Remove legacy agents (Planner/TechLead/Dev/etc.) and any related utilities once workflow executors are in place; no feature flags or fallback paths.
- Remove legacy runner entrypoints and feature flags (including `UseWorkflowMode` and associated env vars).
- Migrate configuration files, environment variables, and docs to new structure; update README and samples.
- Deliverable: Legacy code removed; documentation updated; migration guide for operators.

## Parallelization Notes
- Workstreams 1, 2, and 8 can start immediately and unblock others.
- Workstream 3 can proceed in parallel once interfaces from Workstream 2 are stable.
- Gates (4) and Executors (5) can progress independently, coordinating on result payload shapes.
- Label/Mode sync (6) can run alongside workflow work; integrate late-stage.
- Metrics/limits (7) can be layered after workflow skeleton exists.
- Testing/CI (9) runs continuously as each workstream lands; migration/cleanup (10) after parity is reached.
