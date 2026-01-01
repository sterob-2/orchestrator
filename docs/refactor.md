# Orchestrator Refactor Plan (Microsoft Agent Framework)

## Principles
- Reuse first: prefer existing frameworks/libraries over new code.
- Build only what is necessary.
- Mix .NET and Python only when it avoids custom work and does not require local rebuilds.

## Target Stack
- Microsoft Agent Framework (AF) for agent definitions and graph-based workflows.
- OpenAI provider with API key auth.
- AF DevUI for local debugging.
- AF OpenTelemetry middleware for observability.
- GitHub API via Octokit (REST) and Octokit.GraphQL (Projects V2).

## High-Level Architecture
- Workflow graph with nodes: Planner -> TechLead -> Dev -> Test -> Release.
- Deterministic nodes for GitHub labels, PRs, repo operations, and artifact I/O.
- State/checkpointing at each node (issue metadata, labels, paths, last run).
- Artifacts remain in:
  - orchestrator/plans/issue-<id>.md
  - orchestrator/specs/issue-<id>.md
  - orchestrator/questions/issue-<id>.md
  - orchestrator/reviews/issue-<id>.md
  - orchestrator/release/issue-<id>.md

## GitHub Client Migration (Octokit)
- Replace custom HTTP in GitHubClient with Octokit.
- Use Octokit.GraphQL for Project V2 status queries/mutations.
- Preserve existing behaviors (labels, PR logic, head owner:branch fix).

## Migration Phases
1) Foundations
   - Add Octokit + Octokit.GraphQL NuGet packages.
   - Add AF packages and DevUI/OTel wiring.
   - Implement minimal GitHub adapter using Octokit.

2) Planner on AF Workflow
   - Implement Planner node using AF agent and existing template system.
   - Ensure plan file and draft PR creation match current behavior.

3) TechLead + Questions
   - Add TechLead node and spec generation/answers flow.
   - Ensure spec-questions + spec-clarified labels behave as today.

4) Dev + Self-Check
   - Port Dev agent to AF workflow.
   - Enforce self-check, remediation, retry budget, and block on repeated failure.
   - Avoid re-asking spec questions if answers are CLARIFIED.

5) Test + Release
   - Port Test and Release nodes to AF.
   - Ensure acceptance criteria updates and release notes are written.

6) Cleanup
   - Remove legacy orchestration once parity tests pass.
   - Keep adapters thin; no new custom GitHub HTTP code.

## Testing and Parity
- Add workflow graph tests for transitions and labels.
- Add adapter contract tests for GitHub operations.
- Validate artifacts and labels match current pipeline expectations.

## Dependencies
- NuGet: Microsoft.Agents.AI (and provider), Octokit, Octokit.GraphQL.
- Optional: AF DevUI package, AF OpenTelemetry integration.
