# Refinement: Issue #34 - [FEATURE] SonarQube MCP Server Integration

**Status**: Refinement Complete
**Generated**: 2026-01-09 20:11:08 UTC

## Clarified Story

Integrate a SonarQube MCP Server as a sibling Docker container managed by McpClientManager so the Orchestrator can query SonarQube metrics during SDLC workflows. OrchestratorConfig must read SONAR_HOST_URL and SONAR_TOKEN from environment variables and must never log or persist SONAR_TOKEN in plaintext; SONAR_TOKEN must only be supplied securely to the MCP container (via environment or secret mechanism). McpClientManager must be able to launch a configurable MCP container image via Docker (defaulting to Docker network mode 'bridge' unless overridden), perform an MCP handshake that lists available tools (at minimum sonarqube_get_new_issues and sonarqube_get_quality_gate_status), and allow programmatic invocation/parsing of those tools per the MCP contract. McpClientManager must support TLS verification by default, allow toggling TLS verification and providing a custom CA bundle path (validated before container start), and must pass proxy environment variables to the container by default with explicit opt-out/override options. Implement IProjectMappingService that composes RepoFileMappingRepository, CentralOverrideRepository, and NamingConventionFallback with resolution precedence (central override -> repo mapping file -> deterministic naming fallback), in-memory TTL caching, and webhook-triggered invalidation. CodeReviewExecutor must detect Sonar analysis presence, invoke sonarqube_get_new_issues when available, and include returned findings labelled 'SonarQube findings' distinct from AI findings; if unavailable it must report Sonar data as unavailable with configurable retry behavior. DodExecutor must invoke sonarqube_get_quality_gate_status, fail DoD when status is in a configured fail-list (default ERROR and WARN), and additionally enforce configured metric thresholds (default coverage threshold = 80%) when gates are insufficient. All handshake and tool invocation errors, network/TLS failures, and token handling must surface clear errors and follow configurable retry/timeouts in OrchestratorConfig. Implementation must follow .NET 8, Clean Architecture, Repository Pattern, dependency injection (no singletons), avoid Newtonsoft.Json, use xUnit for tests, include unit and integration tests, and provide CI documentation for SonarScanner and test steps.

## Acceptance Criteria (13)

- Given SONAR_HOST_URL and SONAR_TOKEN are set in the environment, when OrchestratorConfig is initialized, then the Orchestrator must read SONAR_HOST_URL and SONAR_TOKEN from the environment and must not log these values in plaintext during startup or runtime.
- Given McpClientManager has a configured Sonar MCP image, when McpClientManager initializes, then it must launch a Docker container using the configured image name/tag and perform an MCP handshake that lists Sonar-related tools including at minimum sonarqube_get_new_issues and sonarqube_get_quality_gate_status.
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor checks a PR/branch that has Sonar analysis available, then it must invoke sonarqube_get_new_issues, receive a parseable response, and include returned Sonar findings in the review output clearly labeled as 'SonarQube findings' (distinct from AI-generated findings).
- Given the MCP handshake exposes sonarqube_get_new_issues, when CodeReviewExecutor inspects a PR/branch where Sonar analysis is not available or the tool returns a not-found/no-analysis response, then the CodeReviewExecutor must report that Sonar data is unavailable and must not fail silently.
- Given DodExecutor runs for a PR/branch, when it invokes sonarqube_get_quality_gate_status and the returned status is in the configured fail-list (default includes ERROR and WARN), then the DodExecutor must fail the DoD and surface the quality gate status and reason in the failure report.
- Given DodExecutor runs for a PR/branch and sonarqube_get_quality_gate_status returns a passing gate but the reported metrics (for example: code coverage) are below configured thresholds (default coverage threshold = 80%), when thresholds are configured in OrchestratorConfig, then DodExecutor must enforce the configured metric thresholds independently and fail the DoD if thresholds are not met.
- Given SONAR_TOKEN is provided to the system, when the system logs, stores, or returns data, then SONAR_TOKEN must never be written to logs, persisted in plaintext in the repository or config files, or returned in MCP tool responses; the token must only be supplied to the MCP container via environment or other secure mechanism.
- Given network or TLS failures occur during MCP handshake or tool invocation, when such failures happen, then the McpClientManager and tool-invocation code must surface a clear error, CodeReviewExecutor and DodExecutor must treat Sonar data as unavailable, and configured retry behavior must be attempted according to OrchestratorConfig.
- Given the codebase and test suite, when unit tests are run, then unit tests must cover: reading and masking of SONAR_HOST_URL/SONAR_TOKEN, McpClientManager container startup logic (mocked), handshake parsing of tool list and contract version, CodeReviewExecutor behavior when Sonar data is present and absent, and DodExecutor behavior for pass/fail/warn statuses and metric threshold enforcement (xUnit, .NET 8).
- Given an integration test environment with either a running SonarQube instance or a test MCP server, when integration tests are executed (or documented local/integration steps are followed), then the end-to-end flow must be validated: container startup using the configured image, successful handshake exposing Sonar tools, invocation of sonarqube_get_new_issues and sonarqube_get_quality_gate_status, and correct handling of returned results by CodeReviewExecutor and DodExecutor.
- Given McpClientManager launches the Sonar MCP container with no explicit network mode provided, when the container starts, then the container's Docker NetworkMode should default to 'bridge' and the Orchestrator must log the effective network mode (without exposing secrets).
- Given a Sonar host URL uses https, when the Sonar MCP container initiates TLS connections, then TLS certificate validation must be enabled by default (SONAR_TLS_VERIFY=true) and the system must support supplying a custom CA bundle path (SONAR_CA_BUNDLE_PATH) which is validated readable before container start.
- Given the Orchestrator process has proxy environment variables present, when McpClientManager starts the Sonar MCP container, then by default the standard proxy env vars (HTTP_PROXY, HTTPS_PROXY, NO_PROXY and lowercase variants) must be passed through to the container unless explicitly disabled (SONAR_MCP_PASS_PROXY=false) or overridden (SONAR_HTTP_PROXY/SONAR_HTTPS_PROXY/SONAR_NO_PROXY).

## Answered Questions (1)

### Question #1
**Question:** What networking mode and TLS/proxy expectations should the Sonar MCP container use when communicating with the Sonar host? Specifically: should the container use bridge or host networking by default, must we support custom CA bundles and explicit TLS certificate validation toggles, and should proxy environment variables/custom CA bundles be passed through/configurable? Please state required defaults and which options must be configurable.

**Answer (from TechnicalAdvisor):**
Defaults and required configurability for the SonarQube MCP container:

1) Default network mode
- Default: use Docker bridge network (the normal default Docker network). Rationale: provides isolation, predictable outbound NAT for external Sonar services (cloud or on-prem with published ports) and avoids host-level port collision. Implement McpClientManager to launch the container with NetworkMode = "bridge" by default.
- Configurable option: allow selecting an explicit network mode per-run (e.g., "bridge", "host", or a user-created Docker network name). Provide this as a runtime config property in OrchestratorConfig (e.g., SONAR_MCP_NETWORK_MODE) and validate accepted values. Document when alternate modes are appropriate (host may be used in lab/dev where Sonar is bound to localhost and not port-mapped, but it reduces isolation).

2) TLS certificate validation and custom CA bundles
- Default behavior: strict TLS validation enabled by default (i.e., verify server certificate chains). This is the secure default and required for production (SONAR_HOST_URL uses https). Do not disable verification unless explicitly configured with a clear warning and audit log.
- Configurable options (must support):
  - TLS verification toggle: allow explicit control via env/config (e.g., SONAR_TLS_VERIFY=true|false). Default true. When false, log a prominent warning and require an explicit opt-in in config or CI flag.
  - Custom CA bundle support: support mounting and/or passing a CA bundle to the container to trust internal/self-signed Sonar servers. Implement support for either:
    • Mounting a host path with CA file into the container and pointing the tool to it (e.g., mount host path to /etc/ssl/certs/sonar_ca.pem or set SSL_CERT_FILE=/etc/ssl/certs/sonar_ca.pem), or
    • Passing the CA as a secret and writing it into the container's trust store during startup.
  - Validation: McpClientManager must validate that the provided CA path exists and is readable before launching the container and fail fast with a helpful error.

3) Proxy environment and networking headers
- Default: The container should inherit proxy behavior from the Orchestrator environment. Practically, pass-through the standard proxy environment variables to the container when present in the Orchestrator process: HTTP_PROXY, HTTPS_PROXY, NO_PROXY (and lowercase variants). This ensures MCP can reach Sonar hosts behind corporate proxies.
- Configurable options (must support):
  - Explicit opt-in/out to pass proxy envs (e.g., SONAR_MCP_PASS_PROXY=true|false). Default true (pass through). If false, the container must be started with a clean env to avoid leaking proxy config.
  - Allow overriding proxy settings specifically for Sonar calls via SONAR_HTTP_PROXY / SONAR_HTTPS_PROXY / SONAR_NO_PROXY if present; these override inherited values.
  - Support proxy credentials via environment or Docker secrets; prefer secrets for credentials (implement support for Docker secret mounting or reading from an Orchestrator secret store and injecting into the container environment safely).

4) Deployment and operational guidance
- For on-host Sonar services bound to 127.0.0.1: use one of the following (documented):
  • Best: expose Sonar service ports on a Docker-accessible address (use host.docker.internal where supported) or map ports so bridge network works.
  • Alternative: allow users to switch to network mode "host" via SONAR_MCP_NETWORK_MODE when necessary.
- For private Sonar with TLS interception or self-signed certs: require configuration of custom CA bundle and do not recommend disabling TLS verification in CI except for short-lived dev/test runs.
- Always log and surface the effective network mode, TLS verification setting, and whether a custom CA or proxy was provided so audits and debugging are easy.

5) Implementation checklist for McpClientManager / Orchestrator
- Add config properties: SONAR_MCP_NETWORK_MODE (default bridge), SONAR_TLS_VERIFY (default true), SONAR_CA_BUNDLE_PATH (optional), SONAR_MCP_PASS_PROXY (default true), SONAR_HTTP_PROXY / SONAR_HTTPS_PROXY / SONAR_NO_PROXY (optional overrides).
- When launching the MCP container, set Docker.NetworkMode accordingly; pass environment variables (SONAR_HOST_URL, SONAR_TOKEN) and proxy envs if configured; mount CA bundle path if provided and set SSL_CERT_FILE or update trust store in entrypoint.
- Validate inputs before start (token exists, CA path readable, network mode supported). Fail fast and emit actionable error messages.

Summary of required defaults and configurable options
- Defaults (required): network=bridge, TLS verification=enabled, pass proxy envs=true (if present). Custom CA not enabled by default.
- Must be configurable: network mode (bridge|host|custom), TLS verification toggle, custom CA bundle mount/path, proxy env pass-through and explicit Sonar proxy overrides, and secure handling of proxy credentials via secrets.

## Open Questions (5)

**How to answer:**
1. Add a comment to the GitHub issue with your answers
2. Remove `blocked` and `user-review-required` labels
3. Add the `dor` label to re-trigger refinement

Refinement will read your comment, incorporate answers, and stop re-asking those questions.

- [ ] **Question #1:** Question #2: What concrete default retry and timeout policies should be implemented and exposed in OrchestratorConfig for MCP handshake and tool invocations? Please provide defaults for: per-call timeout (seconds), max retry attempts, backoff strategy (linear or exponential), base backoff duration, and whether these settings are per-tool or global.
- [ ] **Question #2:** Question #3: Do we need to support multiple Sonar instances (multiple SONAR_HOST_URL/SONAR_TOKEN pairs)? If yes, how should credentials and instance selection be specified (per-repo config file, multi-entry OrchestratorConfig map, or an external credential store)?
- [ ] **Question #3:** Question #4: What level of integration test coverage is required for CI: are mocked MCP server smoke tests acceptable in CI pipelines, or must CI include full scenario tests that run against a real Sonar instance and sample project analysis? If real-instance tests are required, will a hosted SonarCloud account/test project be provided or must a local Sonar instance be run in CI?
- [ ] **Question #4:** Question #5: Are there mandatory compliance or secrets-management requirements for SONAR_TOKEN storage and rotation we must follow or document (for example: required use of the organization's secrets manager, rotation cadence, audit logging)? If yes, please point to the policy or required mechanism.
- [ ] **Question #5:** Question #6: What default MCP container image name and tag should be used if no explicit image is provided (the spec currently references sonarsource/sonarqube-mcp-server but image name/tag is TBC)? Should the image default be pinned to a specific tag in OrchestratorConfig or must it always be explicitly supplied?

