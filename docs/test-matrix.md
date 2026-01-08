# Test Matrix: Spec Gate and DoD Rules

This matrix maps gate rules to automated tests in the repository.

## Spec Gate Coverage
| Rule | Check | Tests |
| --- | --- | --- |
| Spec-01..Spec-03 | Required sections (Goal, Non-Goals, Components) | tests/Workflows/SpecGateValidatorTests.cs |
| Spec-04..Spec-06 | Touch List presence and format | tests/Workflows/SpecGateValidatorTests.cs, tests/Parsing/TouchListParserTests.cs |
| Spec-07 | Interfaces section | tests/Workflows/SpecGateValidatorTests.cs |
| Spec-08..Spec-09 | Scenarios count and Gherkin validity | tests/Workflows/SpecGateValidatorTests.cs, tests/Parsing/GherkinValidatorTests.cs |
| Spec-10..Spec-11 | Sequence and Test Matrix | tests/Workflows/SpecGateValidatorTests.cs |
| Spec-12 | Missing files for touch list entries | tests/Workflows/SpecGateValidatorTests.cs |
| Spec-13..Spec-16 | Playbook allowed/forbidden references | tests/Workflows/SpecGateValidatorTests.cs, tests/Parsing/PlaybookParserTests.cs |

## DoD Gate Coverage
| Rule | Check | Tests |
| --- | --- | --- |
| DoD-01..DoD-03 | CI checks and pending status | tests/Workflows/DodGateValidatorTests.cs |
| DoD-10..DoD-15 | Quality gate, coverage, duplication | tests/Workflows/DodGateValidatorTests.cs |
| DoD-20..DoD-23 | Spec compliance (AKs, touch list, forbidden files) | tests/Workflows/DodGateValidatorTests.cs, tests/Workflows/DevExecutorTests.cs |
| DoD-30..DoD-31 | AI code review thresholds | tests/Workflows/CodeReviewExecutorTests.cs |
| DoD-40..DoD-41 | TODO/FIXME cleanup on changed files | tests/Workflows/DodGateValidatorTests.cs |
| DoD-42 | Spec status COMPLETE | tests/Workflows/DodGateValidatorTests.cs, tests/Utilities/TemplateUtilTests.cs |

## Workflow Integration Coverage
| Scenario | Tests |
| --- | --- |
| Happy path (Refinement -> Release) | tests/Integration/WorkflowIntegrationTests.cs |
| Spec Gate failure loop (SpecGate -> TechLead -> SpecGate) | tests/Integration/WorkflowIntegrationTests.cs |
