# Orchestrator Workflow

This document describes the workflow graph and label-driven orchestration used by the SDLC Orchestrator. The system is event-driven: each webhook trigger runs one full workflow graph execution.

## Workflow Graph

ContextBuilder → Refinement ↔ DoR → TechLead ↔ SpecGate → Dev ↔ CodeReview → DoD ↔ Dev → Release

- Gate failures loop back to the previous stage.
- Iteration limits are enforced per stage; exceeding limits escalates to human review.

## Labels

### Trigger and Stage Labels
- `ready-for-agents` (work item trigger)
- `agent:planner` (Refinement)
- `agent:dor` (DoR gate)
- `agent:techlead` (TechLead)
- `agent:spec-gate` (Spec gate)
- `agent:dev` (Dev)
- `code-review-needed` / `code-review-changes-requested` (Code review)
- `agent:test` (DoD gate)
- `agent:release` (Release)

### Status Labels
- `in-progress`
- `done`
- `blocked`

### Human-in-the-Loop
- `user-review-required` is applied when automation must stop and wait for a human decision.

## Artifacts

- Spec: `orchestrator/specs/issue-<id>.md`
- Code review: `orchestrator/reviews/issue-<id>.md`
- Release notes: `orchestrator/release/issue-<id>.md`
- Metrics: `orchestrator/metrics/workflow-metrics.jsonl`

## Triggering

- GitHub webhooks (issues/label events) are the primary trigger.
- Manual re-runs are performed by re-applying stage labels (or `ready-for-agents`).
