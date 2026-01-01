# Orchestrator Workflow

This workflow defines how agent tasks move from planning to release, including optional review steps and handoffs via labels.

## Labels

State
- ready-for-agents
- agent:planner
- agent:techlead
- agent:dev
- agent:test
- agent:release
- agent:blocked
- agent:done

Review
- user-review-required
- agent:review-needed
- agent:reviewed

Spec/Review
- spec-questions
- spec-clarified
- code-review-needed
- code-review-approved
- code-review-changes-requested

## Required Artifacts

- orchestrator/plans/issue-<id>.md (Planner output)
- orchestrator/specs/issue-<id>.md (TechLead output)
- orchestrator/questions/issue-<id>.md (Spec questions + answers)
- orchestrator/reviews/issue-<id>.md (TechLead review decision)
- orchestrator/release/issue-<id>.md (Release notes + emulator check)

## Workflow Stages

1) Planner
- Produces: plan markdown
- Default: auto-handoff to TechLead
- If user-review-required: sets agent:review-needed and waits for agent:reviewed

2) TechLead
- Produces: technical spec markdown
- Default: auto-handoff to Dev
- If user-review-required: sets agent:review-needed and waits for agent:reviewed
- If spec-questions: updates spec, sets spec-clarified, removes spec-questions, returns to Dev
- Spec must include a Files section with the exact files DevAgent is allowed to edit.
- Spec must include at least one test file (e.g., under Assets/Tests or Tests).
- TechLead will auto-insert a minimal Files section if missing to avoid review loops.

3) Dev
- Produces: code changes + unit tests
- If spec-clarified missing: posts questions, sets spec-questions, returns to TechLead
- On completion: sets code-review-needed and waits for code-review-approved
- Dev updates acceptance criteria checkboxes in the spec when implemented and tested.

4) Test
- Produces: integration/playmode/UI tests
- Always stops for user review: sets agent:review-needed and waits for agent:reviewed

5) Release
- Produces: release notes + Android emulator check
- Default: marks done after successful release step
- If user-review-required: sets agent:review-needed and waits for agent:reviewed

## PR Policy

- All PRs require owner approval before merge.
- A stage can advance without review unless user-review-required is present.

## Handoff Comment Format (example)

Handoff: TechLead -> Dev
Review status: needs dev review
Open questions:
- ...
Risks:
- ...
Next action:
- Please review spec in orchestrator/specs/issue-<id>.md
