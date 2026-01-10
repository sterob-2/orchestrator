# Refinement: Issue #34 - [FEATURE] SonarQube MCP Server Integration

**Status**: Refinement Complete
**Generated**: 2026-01-10 08:24:35 UTC

## Clarified Story

Add a SonarQube MCP Server as a sibling Docker container managed by McpClientManager so the Orchestrator can query SonarQube metrics during SDLC workflows. OrchestratorConfig must read SONAR_HOST_URL and SONAR_TOKEN from environment variables (masking SONAR_TOKEN in logs and never persisting it plaintext). McpClientManager must launch a configurable Sonar MCP container (default network mode 'bridge'), perform an MCP initialize/handshake that lists at minimum the tools sonarqube_get_new_issues and sonarqube_get_quality_gate_status, and allow programmatic invocation/parsing of those tools per the MCP contract. TLS verification must be enabled by default (SONAR_TLS_VERIFY=true) with optional SONAR_CA_BUNDLE_PATH validated readable before container start. Proxy env vars should be passed into the container by default unless explicitly opted-out/overridden. Implement an IProjectMappingService with defined precedence and TTL caching with webhook-triggered invalidation. Update CodeReviewExecutor to detect Sonar analysis availability, call sonarqube_get_new_issues when available, and include findings labeled exactly 'SonarQube findings' separate from AI findings. Update DodExecutor to call sonarqube_get_quality_gate_status and fail DoD for statuses in a configurable fail-list (default ERROR and WARN) and separately enforce metric thresholds (default coverage 80%). Expose resilience policies in OrchestratorConfig and conform to .NET 8, Clean Architecture, Repository Pattern, DI (no singletons), avoid Newtonsoft.Json, and use xUnit for tests.

## Acceptance Criteria (15)

- Given SONAR_HOST_URL and SONAR_TOKEN are set in the environment, when OrchestratorConfig is initialized, then Orchestrator must read SONAR_HOST_URL and SONAR_TOKEN from the environment and must not log these values in plaintext during startup or runtime.
- Given SONAR_TOKEN is present, when the system emits logs or persists configs, then the system must ensure SONAR_TOKEN is never written to logs, persisted in plaintext in repository or config files, or returned in MCP tool responses.
- Given McpClientManager has a configured Sonar MCP image, when McpClientManager initializes, then it must launch a Docker container using the configured image name/tag, default the container network mode to 'bridge' if not overridden, and complete an MCP handshake that lists sonarqube_get_new_issues and sonarqube_get_quality_gate_status.
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor inspects a PR/branch that has Sonar analysis available, then CodeReviewExecutor must invoke sonarqube_get_new_issues, receive a parseable response, and include returned findings in the review output labeled exactly 'SonarQube findings' distinct from AI-generated findings.
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor examines a PR/branch where Sonar analysis is not available or the tool returns a not-found/no-analysis response, then CodeReviewExecutor must report that Sonar data is unavailable in the review output and must not fail silently.
- Given DodExecutor runs for a PR/branch, when it invokes sonarqube_get_quality_gate_status and the returned status is included in the configured fail-list (default includes ERROR and WARN), then DodExecutor must fail the DoD and surface the quality gate status and reason in the failure report.
- Given DodExecutor runs for a PR/branch and sonarqube_get_quality_gate_status returns passing, when reported metrics (for example: code coverage) are below configured thresholds, then DodExecutor must enforce the configured metric thresholds from OrchestratorConfig (default coverage 80%) and fail the DoD if thresholds are not met.
- Given a Sonar host URL uses https, when the Sonar MCP container initiates TLS connections, then TLS certificate validation must be enabled by default (SONAR_TLS_VERIFY=true) and McpClientManager must validate that a provided SONAR_CA_BUNDLE_PATH is readable before launching the container.
- Given the Orchestrator process has standard proxy environment variables present, when McpClientManager starts the Sonar MCP container, then by default the container must receive proxy env vars (HTTP_PROXY, HTTPS_PROXY, NO_PROXY and lowercase variants) unless SONAR_MCP_PASS_PROXY=false or explicit SONAR_HTTP_PROXY/SONAR_HTTPS_PROXY/SONAR_NO_PROXY overrides are provided.
- Given McpClientManager launches the Sonar MCP container with no explicit network mode provided, when the container starts, then the container's Docker NetworkMode should default to 'bridge' and Orchestrator must log the effective network mode without exposing secrets.
- Given handshake or tool-invocation network/TLS failures occur, when such failures happen, then McpClientManager and tool-invocation code must surface a clear error, CodeReviewExecutor and DodExecutor must treat Sonar data as unavailable, and configured retry/timeouts from OrchestratorConfig must be attempted according to resolution order (Per-tool -> operation-type -> global default).
- Given the system is instrumented for tests, when unit tests run, then xUnit unit tests must cover: reading and masking of SONAR_HOST_URL/SONAR_TOKEN, McpClientManager container startup logic (mocked), handshake parsing of tool list and contract version, CodeReviewExecutor behavior when Sonar data is present and absent, and DodExecutor behavior for pass/warn/fail statuses and metric threshold enforcement.
- Given an integration test environment with either a running SonarQube instance or a test MCP server, when integration tests are executed, then the end-to-end flow must validate: container startup using the configured image, successful MCP handshake exposing Sonar tools, invocation of sonarqube_get_new_issues and sonarqube_get_quality_gate_status, and correct handling of returned results by CodeReviewExecutor and DodExecutor.
- Given Sonar-related long-running fetches are required, when PerToolPolicies permit long-running calls, then PerToolPolicies must allow overriding PerCallTimeoutSeconds to 300s and MaxRetryAttempts appropriately and the implementation must use full-jitter exponential backoff when retrying.
- Given SONAR_TOKEN is delivered into the MCP container, when McpClientManager starts the container, then SONAR_TOKEN must be injected only via container environment or secrets mechanism and must not be written to disk in plaintext on the host or in Orchestrator persisted config.

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

