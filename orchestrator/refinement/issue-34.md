# Refinement: Issue #34 - [FEATURE] SonarQube MCP Server Integration

**Status**: Refinement Complete
**Generated**: 2026-01-09 20:17:46 UTC

## Clarified Story

Integrate a SonarQube MCP Server as a sibling Docker container managed by McpClientManager so the Orchestrator can query SonarQube metrics during SDLC workflows. OrchestratorConfig will read SONAR_HOST_URL and SONAR_TOKEN from environment variables and must never log or persist SONAR_TOKEN in plaintext; SONAR_TOKEN must only be supplied securely to the MCP container. McpClientManager will launch a configurable MCP container image (default Docker network mode 'bridge' unless overridden), perform an MCP handshake that lists at minimum sonarqube_get_new_issues and sonarqube_get_quality_gate_status, and allow programmatic invocation/parsing of those tools per the MCP contract. McpClientManager must support TLS verification by default, allow toggling TLS verification, support supplying and validating a custom CA bundle path before container start, and pass proxy environment variables to the container by default with opt-out/override options. Implement an IProjectMappingService that composes RepoFileMappingRepository, CentralOverrideRepository, and NamingConventionFallback with precedence (central override -> repo mapping file -> deterministic naming fallback), in-memory TTL caching, and webhook-triggered invalidation. CodeReviewExecutor must detect Sonar analysis presence, invoke sonarqube_get_new_issues when available, and include returned findings labeled 'SonarQube findings' distinct from AI findings; if Sonar data is unavailable, report it clearly. DodExecutor must invoke sonarqube_get_quality_gate_status, fail DoD when status is in a configured fail-list (default ERROR and WARN), and enforce configured metric thresholds (default coverage threshold 80%) when gates are insufficient. All handshake and tool invocation errors, network/TLS failures, and token handling must surface clear errors and follow configurable retry/timeouts in OrchestratorConfig. Implement policies and defaults as per the answered configuration (McpHandshakePolicy, McpToolInvocationDefaultPolicy, PerToolPolicies, and LongRunningToolCallRecommendation). Follow .NET 8, Clean Architecture, Repository Pattern, dependency injection (no singletons), avoid Newtonsoft.Json, use xUnit for tests, include unit and integration tests and CI documentation for SonarScanner and test steps.

## Acceptance Criteria (14)

- Given SONAR_HOST_URL and SONAR_TOKEN are set in the environment, when OrchestratorConfig is initialized, then Orchestrator must read SONAR_HOST_URL and SONAR_TOKEN from the environment and must not log these values in plaintext during startup or runtime.
- Given SONAR_TOKEN is present, when the system emits logs or persists configs, then the system must ensure SONAR_TOKEN is never written to logs, persisted in plaintext in repository or config files, or returned in MCP tool responses.
- Given McpClientManager has a configured Sonar MCP image, when McpClientManager initializes, then it must launch a Docker container using the configured image name/tag, default network mode 'bridge' (unless overridden), and perform an MCP handshake that lists sonarqube_get_new_issues and sonarqube_get_quality_gate_status.
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor examines a PR/branch that has Sonar analysis available, then CodeReviewExecutor must invoke sonarqube_get_new_issues, receive a parseable response, and include returned Sonar findings in the review output clearly labeled as 'SonarQube findings' distinct from AI-generated findings.
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor inspects a PR/branch where Sonar analysis is not available or the tool returns a not-found/no-analysis response, then CodeReviewExecutor must report that Sonar data is unavailable in the review output and must not fail silently.
- Given DodExecutor runs for a PR/branch, when it invokes sonarqube_get_quality_gate_status and the returned status is in the configured fail-list (default includes ERROR and WARN), then DodExecutor must fail the DoD and surface the quality gate status and reason in the failure report.
- Given DodExecutor runs for a PR/branch and sonarqube_get_quality_gate_status returns passing but reported metrics (for example: code coverage) are below configured thresholds, when thresholds are configured in OrchestratorConfig, then DodExecutor must enforce the configured metric thresholds independently and fail the DoD if thresholds are not met.
- Given a Sonar host URL uses https, when the Sonar MCP container initiates TLS connections, then TLS certificate validation must be enabled by default (SONAR_TLS_VERIFY=true) and the system must support supplying a custom CA bundle path (SONAR_CA_BUNDLE_PATH) which McpClientManager must validate as readable before launching the container.
- Given the Orchestrator process has standard proxy environment variables present, when McpClientManager starts the Sonar MCP container, then by default the container must receive proxy env vars (HTTP_PROXY, HTTPS_PROXY, NO_PROXY and lowercase variants) unless SONAR_MCP_PASS_PROXY=false or explicit SONAR_HTTP_PROXY/SONAR_HTTPS_PROXY/SONAR_NO_PROXY overrides are provided.
- Given McpClientManager launches the Sonar MCP container with no explicit network mode provided, when the container starts, then the container's Docker NetworkMode should default to 'bridge' and Orchestrator must log the effective network mode (without exposing secrets).
- Given handshake or tool-invocation network/TLS failures occur, when such failures happen, then McpClientManager and tool-invocation code must surface a clear error, CodeReviewExecutor and DodExecutor must treat Sonar data as unavailable, and configured retry/timeouts from OrchestratorConfig must be attempted according to the resolution order (Per-tool -> operation-type policy -> global default).
- Given the system is instrumented for tests, when unit tests run, then unit tests (xUnit) must cover: reading and masking of SONAR_HOST_URL/SONAR_TOKEN, McpClientManager container startup logic (mocked), handshake parsing of tool list and contract version, CodeReviewExecutor behavior when Sonar data is present and absent, and DodExecutor behavior for pass/fail/warn statuses and metric threshold enforcement.
- Given an integration test environment with either a running SonarQube instance or a test MCP server, when integration tests are executed (or local integration steps are followed), then the end-to-end flow must be validated: container startup using the configured image, successful MCP handshake exposing Sonar tools, invocation of sonarqube_get_new_issues and sonarqube_get_quality_gate_status, and correct handling of returned results by CodeReviewExecutor and DodExecutor.
- Given Sonar-related long-running fetches are required (for example large issue reports), when such operations are configured per-tool, then PerToolPolicies or LongRunningToolCallRecommendation must permit overriding PerCallTimeoutSeconds to 300s and MaxRetryAttempts appropriately (as recommended) and the implementation must use full jitter exponential backoff when retrying.

## Answered Questions (1)

### Question #1
**Question:** What concrete default retry and timeout policies should be implemented and exposed in OrchestratorConfig for MCP handshake and tool invocations? Please provide defaults for: per-call timeout (seconds), max retry attempts, backoff strategy (linear or exponential), base backoff duration, and whether these settings are per-tool or global.

**Answer (from TechnicalAdvisor):**
Implement two named policy groups in OrchestratorConfig: McpHandshakePolicy (for the initial MCP handshake / container initialization / tool listing) and McpToolInvocationDefaultPolicy (for normal MCP tool invocations). Expose these as global defaults and allow per-tool overrides via a PerToolPolicies dictionary keyed by tool name (e.g., "sonarqube"). Use exponential backoff with full jitter and caps. Concrete defaults (numbers are seconds unless noted):

1) McpHandshakePolicy (default used during McpClientManager startup & initialize handshake)
- PerCallTimeoutSeconds: 30
- MaxRetryAttempts: 5
- BackoffStrategy: Exponential
- BaseBackoffSeconds: 1
- MaxBackoffSeconds: 16
- UseJitter: true
- RetryOn: transient network faults, connection refused/timeouts, 5xx responses from MCP; avoid retrying non-idempotent startup operations (retries apply to idempotent handshake RPCs)

2) McpToolInvocationDefaultPolicy (default used for normal tool calls like listing tools or simple queries)
- PerCallTimeoutSeconds: 120
- MaxRetryAttempts: 3
- BackoffStrategy: Exponential
- BaseBackoffSeconds: 2
- MaxBackoffSeconds: 30
- UseJitter: true
- RetryOn: transient network faults, timeouts, HTTP 429/5xx; do not retry non-idempotent operations by default

3) LongRunningToolCallRecommendation (for known long queries/aggregation operations, e.g., fetching big Sonar reports)
- PerCallTimeoutSeconds: 300 (override per-tool when needed)
- MaxRetryAttempts: 1 (or 0 if the operation triggers side-effects)
- BackoffStrategy: Exponential
- BaseBackoffSeconds: 5
- MaxBackoffSeconds: 60
- UseJitter: true

4) Per-tool vs Global
- Global defaults: McpHandshakePolicy and McpToolInvocationDefaultPolicy are the system-wide defaults exposed on OrchestratorConfig.
- Per-tool overrides: PerToolPolicies: Dictionary<string, PolicyConfig> where PolicyConfig contains the same properties (PerCallTimeoutSeconds, MaxRetryAttempts, BackoffStrategy, BaseBackoffSeconds, MaxBackoffSeconds, UseJitter, IsRetryable).
- The runtime resolution order: Per-tool override -> operation-type policy (handshake vs tool invocation) -> global default.

5) Additional implementation rules (actionable):
- Implement full jitter (Random(0, backoff)) for backoff intervals to avoid thundering herd.
- Enforce timeouts via CancellationToken + HttpClient/Socket timeouts to ensure resources are freed.
- Only retry idempotent operations by default. Add an explicit IsRetryable flag in PolicyConfig to opt-in per RPC.
- Provide observability: log each retry attempt with reason and elapsed time, expose metrics for retry counts and final failures.
- Prefer using a .NET resilience lib (Polly) to implement policies but encode only numeric values and flags in OrchestratorConfig (no runtime policy objects in config).

Suggested OrchestratorConfig property names (examples):
- McpHandshakePolicy: { PerCallTimeoutSeconds: int, MaxRetryAttempts: int, BackoffStrategy: "Exponential", BaseBackoffSeconds: int, MaxBackoffSeconds: int, UseJitter: bool }
- McpToolInvocationDefaultPolicy: same shape
- PerToolPolicies: Dictionary<string, PolicyConfig>

These defaults balance responsiveness (shorter handshake latency) with robustness (retries and backoff for transient faults) and allow safe per-tool tuning (e.g., Sonar fetches).

## Open Questions (4)

**How to answer:**
1. Add a comment to the GitHub issue with your answers
2. Remove `blocked` and `user-review-required` labels
3. Add the `dor` label to re-trigger refinement

Refinement will read your comment, incorporate answers, and stop re-asking those questions.

- [ ] **Question #1:** Question #2: Do we need to support multiple Sonar instances (multiple SONAR_HOST_URL/SONAR_TOKEN pairs)? If yes, how should credentials and instance selection be specified (per-repo config file, multi-entry OrchestratorConfig map, or an external credential store)?
- [ ] **Question #2:** Question #3: What level of integration test coverage is required for CI: are mocked MCP server smoke tests acceptable in CI pipelines, or must CI include full scenario tests that run against a real Sonar instance and sample project analysis? If real-instance tests are required, will a hosted SonarCloud account/test project be provided or must a local Sonar instance be run in CI?
- [ ] **Question #3:** Question #4: Are there mandatory compliance or secrets-management requirements for SONAR_TOKEN storage and rotation we must follow or document (for example: required use of the organization's secrets manager, rotation cadence, audit logging)? If yes, please point to the policy or required mechanism.
- [ ] **Question #4:** Question #5: What default MCP container image name and tag should be used if no explicit image is provided (the spec currently references sonarsource/sonarqube-mcp-server but image name/tag is TBC)? Should the image default be pinned to a specific tag in OrchestratorConfig or must it always be explicitly supplied?

