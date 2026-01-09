# Refinement: Issue #34 - [FEATURE] SonarQube MCP Server Integration

**Status**: Refinement Complete
**Generated**: 2026-01-09 14:05:43 UTC

## Clarified Story

Integrate a SonarQube MCP Server as a sibling Docker container managed by McpClientManager so the Orchestrator can query SonarQube metrics during SDLC workflows. Add SONAR_HOST_URL and SONAR_TOKEN to OrchestratorConfig (read from environment and not logged). The MCP server container image must be configurable. The MCP handshake must expose Sonar-related tools (minimum: sonarqube_get_new_issues and sonarqube_get_quality_gate_status or agreed equivalents) and the Orchestrator must be able to invoke these tools programmatically and parse responses. CodeReviewExecutor must detect when Sonar analysis exists for a PR/branch, call the new-issues tool when available, and include Sonar findings in the review output clearly labeled as static analysis. DodExecutor must call the quality-gate-status tool, fail the DoD when returned status is in a configurable fail list (default include ERROR and WARN), and independently enforce configured metric thresholds (default coverage threshold = 80%) when the quality gate is too loose. Secrets (SONAR_TOKEN) must not be logged or persisted in plaintext and are supplied only to the MCP container via environment or other secure mechanisms. Define behavior for network/TLS failures so that handshake/tools surface clear errors and executors treat Sonar data as unavailable with documented, configurable retry behavior. Implementation must follow .NET 8, xUnit, Clean Architecture, and Repository Pattern constraints. Add unit and integration tests and documentation covering CI SonarScanner requirements, configuration, and failure modes.

## Acceptance Criteria (11)

- OrchestratorConfig reads SONAR_HOST_URL and SONAR_TOKEN from environment; these values are not logged in plaintext during startup or runtime.
- McpClientManager launches the SonarQube MCP server container during initialization and performs a successful handshake that lists Sonar-related tools. The container image name is configurable; tests will assert that the configured image is used.
- The MCP handshake exposes at least the following tools (or agreed equivalents) to the Orchestrator: sonarqube_get_new_issues and sonarqube_get_quality_gate_status. The Orchestrator can invoke these tools programmatically and receive parseable responses.
- CodeReviewExecutor: when Sonar analysis exists for the PR/branch, the executor invokes the new-issues tool, incorporates the returned Sonar findings into the review output, and clearly distinguishes them from LLM/AI-generated findings. When no Sonar analysis is available, the executor reports that Sonar data is unavailable (no silent failures).
- DodExecutor: the executor invokes the quality-gate-status tool and fails the DoD when the returned status is in a configurable fail list (default include ERROR and WARN). The executor must also be able to inspect specific metrics (e.g., coverage) and fail when configured thresholds are not met (default coverage threshold = 80%). The fail-list and metric thresholds are configurable via OrchestratorConfig.
- If the Sonar quality gate is considered 'too loose' (e.g., passes but coverage < configured threshold), DodExecutor enforces the configured metric checks independently of quality gate status.
- Unit tests cover: reading and masking of SONAR_HOST_URL/SONAR_TOKEN, McpClientManager container startup logic (mocked), handshake parsing of tool list, CodeReviewExecutor behavior when Sonar data present and absent, DodExecutor behavior for pass/fail/warn statuses and metric threshold enforcement.
- Integration tests (or documented local/integration steps) validate end-to-end flow against a running SonarQube instance or a test MCP server: container startup, handshake, invocation of tools, and correct handling of returned results.
- Documentation added to the repo describing: required SonarScanner CI step for branches created by the Orchestrator, how to set SONAR_HOST_URL and SONAR_TOKEN, how to configure fail statuses and metric thresholds, and the expected MCP tool names/endpoints.
- Secrets (SONAR_TOKEN) are never written to logs, persisted in plaintext in repo/config files, or returned in tool responses; code must treat them as secrets and supply them only to the MCP container via environment or secure mechanism.
- Behavior under network/TLS issues is defined: on connection failures to SonarHost the MCP handshake or tool invocation surfaces a clear error and CodeReviewExecutor/DodExecutor treat Sonar data as unavailable (with configurable retry behavior documented).

## Open Questions (12)

**How to answer:**
1. Add a comment to the GitHub issue with your answers
2. Remove `blocked` and `user-review-required` labels
3. Add the `dor` label to re-trigger refinement

Refinement will read your comment, incorporate answers, and stop re-asking those questions.

- [ ] What is the exact container image name and tag for the Sonar MCP server to be used (the plan states 'image name TBC')? Provide repository/name:tag or confirm it will be configurable at runtime.
- [ ] What are the exact MCP tool names and their request/response schemas for: (a) retrieving new issues for a PR/branch and (b) retrieving quality gate status and metrics? If these are not fixed, who will provide the MCP tool contract and where will the schema be documented?
- [ ] How should the system detect 'Sonar analysis is available' for a given PR/branch? Options include: query Sonar API for project/branch existence, check for a recent analysis timestamp, or rely on a CI success flag. Which approach is preferred?
- [ ] What is the authoritative mapping between Orchestrator repositories/branches/PRs and Sonar projects/branches? Is there an existing naming convention or configuration (e.g., project key mapping) to locate the Sonar project key for an Orchestrator repo?
- [ ] What exact statuses should cause DodExecutor to fail by default? The plan suggests ERROR and WARN — confirm the default fail set and whether WARN should be a fail by default or optional.
- [ ] Should metric thresholds (e.g., coverage 80%) be global defaults, per-project configuration, or overridable in the DoD configuration? Where will those overrides be stored/read from (OrchestratorConfig, repository config, DoD payload, or external store)?
- [ ] How should the Sonar MCP container be networked relative to SonarHost (host network vs bridge) and will TLS certificate validation or proxy settings need to be supported? Provide any networking constraints or expectations (e.g., environment must allow outbound HTTPS to SONAR_HOST_URL, proxy env vars).
- [ ] Where should SonarScanner execution live? Confirm that SonarScanner runs in the CI pipeline for branches created by the Orchestrator and that the Orchestrator itself will not run SonarScanner as part of its process.
- [ ] What are the expected retry and timeout policies for MCP handshake and tool invocations (e.g., number of retries, exponential backoff, per-call timeout)?
- [ ] Do we need to support multiple Sonar instances (per-organization) or just a single SONAR_HOST_URL/SONAR_TOKEN pair? If multiple instances are required, how should credentials/configuration be specified and selected per-repository or per-workflow?
- [ ] What level of test coverage is required for integration tests (e.g., smoke test only vs full scenario with sample project analysis)? Is using a mocked MCP server acceptable for CI, or must CI include a real Sonar instance for integration tests?
- [ ] Are there any compliance or audit requirements around storing/rotating SONAR_TOKEN that we must implement or document (e.g., use of a secrets manager, rotation policies, audit logging)?

