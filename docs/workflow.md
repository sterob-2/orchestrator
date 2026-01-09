# Orchestrator Workflow

This document describes the workflow graph and label-driven orchestration used by the SDLC Orchestrator. The system is event-driven: each webhook trigger runs one full workflow graph execution.

## Workflow Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SDLC Workflow Stages                              │
└─────────────────────────────────────────────────────────────────────────┘

[GitHub Issue #N]
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 1. CONTEXTBUILDER                                                         │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  GitHub Issue                                                      │
│ Action: Create git branch (issue-N)                                       │
│ Output: Git branch ready                                                  │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 2. REFINEMENT                                                             │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  - Issue title & body                                              │
│         - Existing spec (if any)                                          │
│         - Previous refinement (if exists)                                 │
│         - Issue comments (for answers)                                    │
│         - Playbook constraints                                            │
│ Action: LLM analyzes and refines requirements                             │
│ Output: orchestrator/refinement/issue-N.md                                │
│         - Clarified story                                                 │
│         - Acceptance criteria (list)                                      │
│         - Open questions (list)                                           │
│         - Complexity indicators                                           │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 3. DoR (Definition of Ready) - GATE                                       │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  RefinementResult                                                  │
│ Checks: - Description ≥ 50 chars                                          │
│         - ≥ 3 acceptance criteria                                         │
│         - Acceptance criteria are testable (Given/When/Then)              │
│         - No open questions                                               │
│         - Estimate label present                                          │
│ Output: orchestrator/dor/issue-N.md (if failed)                           │
│         - Gate status (✅ PASSED / ❌ FAILED)                            │
│         - Failure reasons                                                 │
│         - Open questions that need answers                                │
│ Result: ✅ → TechLead  |  ❌ → Blocks, needs user input                  │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 4. TECHLEAD                                                               │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  - RefinementResult                                                │
│         - Playbook (frameworks, patterns)                                 │
│         - Spec template                                                   │
│ Action: LLM generates technical specification                             │
│ Output: orchestrator/specs/issue-N.md                                     │
│         - Goal & non-goals                                                │
│         - Components                                                      │
│         - Touch list (files to add/modify/delete)                         │
│         - Interfaces                                                      │
│         - Scenarios (Given/When/Then)                                     │
│         - Sequence                                                        │
│         - Test matrix                                                     │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 5. SPECGATE - GATE                                                        │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  - Spec document                                                   │
│         - Playbook                                                        │
│ Checks: - Touch list not empty                                            │
│         - Paths are valid                                                 │
│         - Frameworks are allowed                                          │
│         - Patterns are allowed                                            │
│         - No forbidden items used                                         │
│ Action: Updates spec status to "COMPLETE" if passed                       │
│ Result: ✅ → Dev  |  ❌ → Back to TechLead                               │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 6. DEV                                                                    │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  Spec (touch list)                                                 │
│ Action: LLM generates code for each file in touch list:                   │
│         - Add new files                                                   │
│         - Modify existing files                                           │
│         - Delete files                                                    │
│ Output: - Code changes committed to branch                                │
│         - Updated spec (acceptance criteria marked done)                  │
│ Result: DevResult (list of changed files)                                 │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 7. CODEREVIEW                                                             │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  DevResult (changed files)                                         │
│ Action: LLM reviews code changes                                          │
│ Output: orchestrator/reviews/issue-N.md                                   │
│         - Approved: true/false                                            │
│         - Findings (Blocker/Major/Minor/Info)                             │
│         - Summary                                                         │
│ Result: ✅ Approved → DoD  |  ❌ Blockers → Back to Dev                  │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 8. DoD (Definition of Done) - GATE                                        │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  - Spec                                                            │
│         - DevResult                                                       │
│         - CodeReviewResult                                                │
│ Checks: - AI code review passed (approved)                                │
│         - No blocker findings                                             │
│         - All acceptance criteria complete (no [ ])                       │
│         - Touch list satisfied (all files changed)                        │
│         - No TODO/FIXME markers in code                                   │
│         - Spec status is COMPLETE                                         │
│ Result: ✅ → Release  |  ❌ → Back to Dev                                │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────────────┐
│ 9. RELEASE                                                                │
├──────────────────────────────────────────────────────────────────────────┤
│ Input:  - DoD gate result (passed)                                        │
│         - Spec                                                            │
│ Action: - Create/update Pull Request                                      │
│         - Generate PR description                                         │
│ Output: orchestrator/release/issue-N.md                                   │
│         - Release notes                                                   │
│         - PR URL                                                          │
│         - Changes summary                                                 │
│ Result: Pull Request ready for human review & merge                       │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ↓
   [END - PR Ready]
```

## Workflow Graph

**Simple Flow:**
```
ContextBuilder → Refinement ↔ DoR → TechLead ↔ SpecGate → Dev ↔ CodeReview → DoD ↔ Dev → Release
```

**Loop-Back Conditions:**
- **DoR fails** → User answers questions → Re-trigger Refinement
- **SpecGate fails** → Back to TechLead
- **CodeReview has blockers** → Back to Dev (iteration loop, max 3 attempts)
- **DoD fails** → Back to Dev

## Stage Details

### 1. ContextBuilder
**Purpose:** Initialize work context and create isolated git branch

**Inputs:**
- GitHub Issue

**Actions:**
- Create branch `issue-N` from default base branch
- Checkout branch

**Outputs:**
- Git branch ready for work

**Labels:** Initial stage, triggered by `ready-for-agents`

---

### 2. Refinement
**Purpose:** Clarify requirements and identify open questions

**Inputs:**
- Issue title and body
- Existing spec (if any)
- Previous refinement output (to avoid re-asking questions)
- Issue comments (containing answers from users)
- Playbook constraints

**Actions:**
- LLM analyzes issue and generates structured refinement
- Commits refinement file to branch

**Outputs:**
- `orchestrator/refinement/issue-N.md`
  - Clarified story
  - Acceptance criteria (list)
  - Open questions (list)
  - Complexity indicators

**Labels:** `agent:planner`

**Special Features:**
- Reads previous refinement to avoid re-asking answered questions
- Reads issue comments to incorporate user answers
- If questions remain, workflow blocks with `blocked` + `user-review-required`

---

### 3. DoR (Definition of Ready) - GATE
**Purpose:** Validate that requirements are ready for implementation

**Inputs:**
- RefinementResult from previous stage

**Gate Checks:**
- Description must be ≥ 50 characters
- At least 3 acceptance criteria required
- Acceptance criteria must be testable (Given/When/Then format)
- No open questions remaining
- Estimate label present on issue

**Outputs (if failed):**
- `orchestrator/dor/issue-N.md`
  - Gate status (PASSED/FAILED)
  - List of failures
  - Open questions requiring answers
  - GitHub comment with pointer to file

**Labels:** `agent:dor`

**Behavior:**
- ✅ Pass → Proceed to TechLead
- ❌ Fail → Block with `blocked` + `user-review-required`

---

### 4. TechLead
**Purpose:** Generate technical specification

**Inputs:**
- RefinementResult
- Playbook (allowed/forbidden frameworks and patterns)
- Spec template (`docs/templates/spec.md`)

**Actions:**
- LLM generates technical specification
- Commits spec file to branch

**Outputs:**
- `orchestrator/specs/issue-N.md`
  - Goal & non-goals
  - Components
  - **Touch list** (files to add/modify/delete)
  - Interfaces (code signatures)
  - Scenarios (Given/When/Then test scenarios)
  - Sequence (step-by-step implementation)
  - Test matrix

**Labels:** `agent:techlead`

**Special Features:**
- Respects playbook constraints (frameworks, patterns)
- Uses template for consistent spec format
- Touch list drives the Dev stage

---

### 5. SpecGate - GATE
**Purpose:** Validate technical specification

**Inputs:**
- Spec document
- Playbook

**Gate Checks:**
- Touch list not empty
- All paths are valid and safe
- Only allowed frameworks used
- Only allowed patterns used
- No forbidden items present

**Actions:**
- Updates spec status to "COMPLETE" if passed

**Labels:** `agent:spec-gate`

**Behavior:**
- ✅ Pass → Proceed to Dev
- ❌ Fail → Back to TechLead

---

### 6. Dev
**Purpose:** Generate code implementation

**Inputs:**
- Spec (particularly touch list)

**Actions:**
- For each entry in touch list:
  - **Add:** LLM generates new file content
  - **Modify:** LLM updates existing file
  - **Delete:** Remove file
- Commits all changes to branch
- Marks acceptance criteria as done in spec

**Outputs:**
- Code changes committed to `issue-N` branch
- Updated spec with completed criteria
- DevResult (list of changed files)

**Labels:** `agent:dev`

**Special Features:**
- Iteration tracking (max 3 attempts per stage)
- Safe path validation
- Fails if forbidden paths in touch list

---

### 7. CodeReview
**Purpose:** AI reviews AI-generated code

**Inputs:**
- DevResult (list of changed files)

**Actions:**
- LLM reviews code changes
- Generates review report with findings

**Outputs:**
- `orchestrator/reviews/issue-N.md`
  - Approved: true/false
  - Findings categorized by severity:
    - **Blocker:** Must fix (blocks DoD)
    - **Major:** Should fix
    - **Minor:** Nice to fix
    - **Info:** Informational
  - Summary

**Labels:** `code-review-needed`, `code-review-changes-requested`

**Behavior:**
- ✅ Approved (no blockers) → Proceed to DoD
- ❌ Has blockers → Back to Dev

---

### 8. DoD (Definition of Done) - GATE
**Purpose:** Validate that work is complete and ready for release

**Inputs:**
- Spec
- DevResult
- CodeReviewResult

**Gate Checks:**
- AI code review passed (approved)
- No blocker findings remain
- All acceptance criteria complete (no `[ ]` checkboxes)
- Touch list satisfied (all specified files changed)
- No TODO/FIXME markers in changed code
- Spec status is COMPLETE

**Labels:** `agent:test`

**Behavior:**
- ✅ Pass → Proceed to Release
- ❌ Fail → Back to Dev

---

### 9. Release
**Purpose:** Create Pull Request for human review and merge

**Inputs:**
- DoD gate result (passed)
- Spec

**Actions:**
- Create/update Pull Request to default base branch
- Generate PR description from spec
- Write release notes

**Outputs:**
- `orchestrator/release/issue-N.md`
  - Release notes
  - PR URL
  - Changes summary
- GitHub Pull Request

**Labels:** `agent:release`

**Final State:**
- PR ready for human review
- Human merges when satisfied

---

## Artifacts by Stage

| Stage | Artifact Produced | Location |
|-------|-------------------|----------|
| ContextBuilder | Git branch | `issue-N` |
| Refinement | Refinement document | `orchestrator/refinement/issue-N.md` |
| DoR (gate) | DoR result (if failed) | `orchestrator/dor/issue-N.md` |
| TechLead | Technical specification | `orchestrator/specs/issue-N.md` |
| SpecGate (gate) | Spec validation | (updates spec status in-place) |
| Dev | Code changes | Files per touch list |
| CodeReview | Review report | `orchestrator/reviews/issue-N.md` |
| DoD (gate) | Gate validation | (in-memory result) |
| Release | Release notes + PR | `orchestrator/release/issue-N.md` + GitHub PR |

## Labels

### Trigger and Stage Labels
- `ready-for-agents` - Work item trigger (starts ContextBuilder)
- `agent:planner` - Refinement stage
- `agent:dor` - DoR gate
- `agent:techlead` - TechLead stage
- `agent:spec-gate` - Spec gate
- `agent:dev` - Dev stage
- `code-review-needed` / `code-review-changes-requested` - Code review stage
- `agent:test` - DoD gate
- `agent:release` - Release stage

### Status Labels
- `in-progress` - Work currently executing
- `done` - Stage/workflow completed
- `blocked` - Cannot proceed (human input needed)

### Special Labels
- `user-review-required` - Applied when automation must stop and wait for human decision
- `estimate:N` - Required by DoR gate (work item must have effort estimate)

### Human-in-the-Loop
When the workflow applies `blocked` + `user-review-required`:
1. User addresses the issue (e.g., answers questions in refinement)
2. User removes `blocked` and `user-review-required` labels
3. User applies appropriate stage label to resume

## Triggering

### Webhook-Based (Primary)
- GitHub webhooks (issues/label events) trigger workflow execution
- Each label change runs full workflow graph execution from that stage

### Polling (Secondary)
- Configurable polling interval (default: 60s idle, 10s when active)
- Adaptive intervals based on work item labels
- Can be disabled by setting `POLL_INTERVAL_SECONDS=0`

### Manual Re-runs
- Re-apply stage labels to retry from specific stage
- Apply `ready-for-agents` to restart from beginning

## Configuration

See `.env.example` for all configuration options:
- Model selection per stage (refinement, techlead, dev, review)
- Iteration limits
- Polling intervals
- Webhook settings
- Repository and GitHub configuration
