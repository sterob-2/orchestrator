# Product Vision: Orchestrator

## Vision Statement

**Orchestrator is an SDLC-driven autonomous software development system that produces predictable, professional-quality code through structured workflow gates and human oversight.**

We solve the "AI Slop" problem: LLMs that drift off-task and generate technically correct but useless code. By enforcing SDLC structure with human checkpoints, we enable building real software—maybe not SAP, but at least a decent mobile game.

## Target Users

**Primary:** Developers who want to offload implementation work while maintaining quality control
- Have clear vision/requirements
- Understand software architecture
- Want predictable results over speed
- Willing to review at strategic gates

**Not For:** Teams seeking fully autonomous "fire and forget" code generation

## Core Principles

### 1. Quality Over Speed
- Slow and correct beats fast and broken
- SDLC structure prevents drift
- Human checkpoints at gates (DoR, SpecGate, DoD)

### 2. Minimal Viable First
- **Always start with the simplest solution that works**
- No edge cases until reviews request them
- No "future-proofing" or "nice-to-haves"
- One feature per issue

### 3. Small, Focused Issues
- Each issue implements ONE clear feature
- Max 3-5 acceptance criteria per issue
- If it feels large, split it
- Examples:
  - ✅ "Add health check endpoint"
  - ❌ "Implement monitoring infrastructure with metrics, logging, and alerting"

### 4. Human Oversight, Not Micromanagement
- LLM works autonomously between gates
- Human reviews specs and code at checkpoints
- GitHub labels/PR reviews as control mechanism

## What We DON'T Do

### ❌ Over-Engineering
- No abstractions for single use cases
- No frameworks unless essential
- No "enterprise patterns" for simple problems

### ❌ Speculative Features
- No "might need this later"
- No configuration for hypothetical scenarios
- Build what's needed now

### ❌ Massive Issues
- No epics or large stories
- No mixing multiple concerns
- Break down instead of bulking up

### ❌ Autonomous Drift
- No unchecked LLM generation
- No "smart" assumptions without context
- Gates exist to prevent runaway complexity

## Success Criteria

**A successful implementation:**
1. Solves the stated problem completely
2. Contains ONLY code needed for requirements
3. Passes all tests
4. Follows existing architecture patterns
5. Can be reviewed in under 15 minutes
6. Could ship to production

**We measure success by:**
- Issues completed end-to-end (Refinement → Release)
- Code quality at PR review (minimal changes requested)
- Time to first working version
- Absence of over-engineering in specs

## Decision Framework

When unsure, ask:
1. **Is this needed NOW?** If not, skip it.
2. **Does this solve the stated problem?** If not, remove it.
3. **Can it be simpler?** If yes, simplify.
4. **Would this fit in one PR review?** If not, split it.

## Examples

### ✅ Good Issue
```
Title: Add JSON logging to HTTP requests
Body:
- Log request method, path, status code as JSON
- Use existing logger infrastructure
- No PII in logs

Acceptance Criteria:
1. Given an HTTP request, when processed, then log JSON with method/path/status
2. Given the request contains auth tokens, when logged, then tokens are redacted
3. Given logging is enabled, when tests run, then verify JSON format
```

### ❌ Bad Issue (Too Large)
```
Title: Implement comprehensive observability platform
Body:
- Structured logging with correlation IDs
- OpenTelemetry tracing
- Prometheus metrics with custom exporters
- Distributed tracing across services
- Log aggregation and querying
- Real-time alerting

Acceptance Criteria: [14 criteria covering multiple systems]
```

## Alignment with SDLC Workflow

```
Issue → Refinement → DoR Gate → TechLead → SpecGate → Dev → CodeReview → DoD → Release
        ↓                        ↓                             ↓
    Keep simple          Minimal design              Clean implementation
```

Each stage enforces minimalism:
- **Refinement:** Clarify requirements, resist scope creep
- **TechLead:** Simplest design that works
- **Dev:** Only implement what's in spec
- **Gates:** Reject over-engineering
