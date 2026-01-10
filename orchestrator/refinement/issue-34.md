# Refinement: Issue #34 - [FEATURE] SonarQube MCP Server Integration

**Status**: Refinement Complete
**Generated**: 2026-01-10 08:42:50 UTC

## Clarified Story

Integrate a SonarQube MCP Server as a sibling Docker container managed by McpClientManager so the Orchestrator can query SonarQube metrics during SDLC workflows. OrchestratorConfig must read SONAR_HOST_URL and SONAR_TOKEN from environment and must mask SONAR_TOKEN in logs and never persist it in plaintext. McpClientManager must be able to launch a configurable Sonar MCP container (default Docker NetworkMode 'bridge'), pass proxy variables by default, validate SONAR_CA_BUNDLE_PATH readability if provided, inject SONAR_TOKEN only via container environment or a container secret mechanism, and complete an MCP initialize/handshake that includes sonarqube_get_new_issues and sonarqube_get_quality_gate_status. Implement PerToolPolicies with resolution order (Per-tool -> operation-type -> global default) supporting full-jitter exponential backoff and allowing PerCallTimeoutSeconds overrides up to 300s. Implement an IProjectMappingService with precedence and TTL caching plus webhook-triggered invalidation. Update CodeReviewExecutor to detect Sonar analysis availability, call sonarqube_get_new_issues when available, and include findings labeled exactly 'SonarQube findings' distinct from AI findings. Update DodExecutor to call sonarqube_get_quality_gate_status and fail DoD for statuses in a configurable fail-list (default ERROR and WARN) and enforce metric thresholds (default coverage 80%) when Quality Gate is insufficient. Conform to .NET 8, Clean Architecture, Repository Pattern, dependency injection (no singletons), avoid Newtonsoft.Json, and cover logic with xUnit unit and integration tests. Ensure SONAR_TOKEN is never logged or persisted in plaintext anywhere in Orchestrator telemetry or files.

## Acceptance Criteria (15)

- Given SONAR_HOST_URL and SONAR_TOKEN are present in the environment, when OrchestratorConfig initializes, then the system must read SONAR_HOST_URL and SONAR_TOKEN from the environment and must not log SONAR_TOKEN in plaintext during startup or runtime.
- Given SONAR_TOKEN exists, when the system emits logs or persists configuration, then the system must ensure SONAR_TOKEN is never written to logs, persisted in plaintext config files or repos, or returned in MCP tool responses.
- Given McpClientManager has a configured Sonar MCP image, when McpClientManager initializes, then it must launch a Docker container using the configured image name/tag, default the container Docker NetworkMode to 'bridge' if not overridden, and complete an MCP handshake that lists at minimum sonarqube_get_new_issues and sonarqube_get_quality_gate_status.
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor inspects a PR/branch and the Sonar MCP tool returns findings, then CodeReviewExecutor must invoke sonarqube_get_new_issues, parse the response, and include the returned findings in the review output labeled exactly 'SonarQube findings' distinct from any AI-generated findings.
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor inspects a PR/branch where Sonar analysis is not available or the tool returns a not-found/no-analysis response, then CodeReviewExecutor must report that Sonar data is unavailable in the review output and must not silently ignore the absence.
- Given DodExecutor runs for a PR/branch, when it invokes sonarqube_get_quality_gate_status and the returned status is included in the configured fail-list (default includes ERROR and WARN), then DodExecutor must fail the DoD, surface the quality gate status and reason in the failure report, and record the failing tool invocation in the operation log without logging secrets.
- Given DodExecutor runs for a PR/branch and sonarqube_get_quality_gate_status returns passing, when reported metrics (for example: code coverage) are below configured thresholds, then DodExecutor must enforce metric thresholds from OrchestratorConfig (default coverage 80%) and must fail the DoD if thresholds are not met, describing which metric failed and its measured value.
- Given a Sonar host URL uses https, when the Sonar MCP container initiates TLS connections, then TLS certificate validation must be enabled by default (SONAR_TLS_VERIFY=true) and McpClientManager must validate that a provided SONAR_CA_BUNDLE_PATH is readable before launching the container.
- Given the Orchestrator process has proxy environment variables present, when McpClientManager starts the Sonar MCP container, then by default the container must receive HTTP_PROXY, HTTPS_PROXY, NO_PROXY and lowercase variants unless SONAR_MCP_PASS_PROXY=false or explicit SONAR_HTTP_PROXY/SONAR_HTTPS_PROXY/SONAR_NO_PROXY overrides are provided.
- Given McpClientManager launches the Sonar MCP container with no explicit network mode provided, when the container starts, then the container's Docker NetworkMode must default to 'bridge' and Orchestrator must log the effective network mode without exposing secrets (verify logs do not contain the value of SONAR_TOKEN).
- Given MCP handshake or tool-invocation network/TLS failures occur, when such failures happen, then McpClientManager and tool-invocation code must surface a clear error, CodeReviewExecutor and DodExecutor must treat Sonar data as unavailable, and the system must attempt retries/timeouts according to the configured resolution order (Per-tool -> operation-type -> global default).
- Given PerToolPolicies permit long-running Sonar calls, when such calls are required, then PerToolPolicies must be able to override PerCallTimeoutSeconds up to 300s, must allow configuration of MaxRetryAttempts, and retries must use full-jitter exponential backoff.
- Given SONAR_TOKEN is delivered into the MCP container, when McpClientManager starts the container, then SONAR_TOKEN must be injected only via container environment variables or a container secrets mechanism and must not be written to disk in plaintext on the host or persisted by Orchestrator in plaintext.
- Given the system is instrumented for tests, when unit tests run, then xUnit unit tests must cover: reading and masking of SONAR_HOST_URL and SONAR_TOKEN, McpClientManager container startup logic (mocked), MCP handshake parsing of tool list and contract version, CodeReviewExecutor behavior when Sonar data is present and absent, and DodExecutor behavior for pass/warn/fail statuses and metric threshold enforcement.
- Given an integration test environment with either a running SonarQube instance or a compliant test MCP server, when integration tests are executed, then end-to-end tests must validate: container startup using the configured image, successful MCP handshake exposing Sonar tools, invocation of sonarqube_get_new_issues and sonarqube_get_quality_gate_status, and correct handling of returned results by CodeReviewExecutor and DodExecutor.

## Questions

**How to answer:**
1. Edit this file and add your answer after the question
2. Mark the checkbox with [x] when answered
3. Commit and push changes
4. Remove `blocked` label and add `dor` label to re-trigger

- [ ] **Question #1:** Do we need to support multiple Sonar instances (multiple SONAR_HOST_URL/SONAR_TOKEN pairs)? If yes, how should credentials and instance selection be specified (per-repo config file, multi-entry OrchestratorConfig map, or an external credential store)?
  **Answer:** _[Pending]_

- [ ] **Question #2:** What level of integration test coverage is required for CI: are mocked MCP server smoke tests acceptable in CI pipelines, or must CI include full scenario tests that run against a real Sonar instance and sample project analysis? If real-instance tests are required, will a hosted SonarCloud account/test project be provided or must a local Sonar instance be run in CI?
  **Answer:** _[Pending]_

- [ ] **Question #3:** Are there mandatory compliance or secrets-management requirements for SONAR_TOKEN storage and rotation we must follow or document (for example: required use of the organization's secrets manager, rotation cadence, audit logging)? If yes, please point to the policy or required mechanism.
  **Answer:** _[Pending]_

- [ ] **Question #4:** What default MCP container image name and tag should be used if no explicit image is provided (the spec currently references sonarsource/sonarqube-mcp-server but image name/tag is TBC)? Should the image default be pinned to a specific tag in OrchestratorConfig or must it always be explicitly supplied?
  **Answer:** _[Pending]_

