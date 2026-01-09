# Migration Guide (Legacy to Workflow Graph)

This guide helps operators move from legacy agent-based orchestration to the current workflow-graph implementation.

## What Changed

- Legacy agents (Planner/TechLead/Dev/etc.) are removed; the workflow graph is the source of truth.
- Polling is removed. The watcher is event-driven and runs one full graph per webhook trigger.
- Stage routing is driven by workflow state and labels, not feature flags.

## Required Actions

1. **Update environment variables**
   - Remove legacy/unused keys:
     - `SPEC_QUESTIONS_LABEL`, `SPEC_CLARIFIED_LABEL`
     - `PROJECT_STATUS_IN_PROGRESS`, `PROJECT_STATUS_IN_REVIEW`, `PROJECT_STATUS_DONE`
   - Ensure the required keys are set:
     - `REPO_OWNER`, `REPO_NAME`, `OPENAI_API_KEY`
     - `WORK_ITEM_LABEL` (default: `ready-for-agents`)
     - `DOR_LABEL` (default: `agent:dor`)
     - `SPEC_GATE_LABEL` (default: `agent:spec-gate`)

2. **Webhook configuration**
   - Configure GitHub webhooks to post issue and label events to `WEBHOOK_LISTEN_HOST:WEBHOOK_PORT/WEBHOOK_PATH`.
   - Set `WEBHOOK_SECRET` to enable signature validation in production.

3. **Labels**
   - Ensure stage labels exist in GitHub (planner, dor, techlead, spec-gate, dev, test, release).
   - The workflow applies and removes labels automatically based on stage output.

## Artifacts

- Spec: `orchestrator/specs/issue-<id>.md`
- Code review: `orchestrator/reviews/issue-<id>.md`
- Release notes: `orchestrator/release/issue-<id>.md`
- Metrics: `orchestrator/metrics/workflow-metrics.jsonl`

## Rollback

If you need to rollback, keep a copy of your previous environment configuration and branch state. The current workflow graph does not support legacy agent execution paths.
