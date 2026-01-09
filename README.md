# Orchestrator

[![CI](https://github.com/sterob-2/orchestrator/actions/workflows/ci.yml/badge.svg)](https://github.com/sterob-2/orchestrator/actions/workflows/ci.yml)
[![SonarCloud Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=sterob-2_orchestrator&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=sterob-2_orchestrator)
[![Release](https://github.com/sterob-2/orchestrator/actions/workflows/release.yml/badge.svg)](https://github.com/sterob-2/orchestrator/actions/workflows/release.yml)

An AI-driven SDLC orchestrator built on the Microsoft Agent Framework. It processes GitHub issues through a workflow graph (Refinement → DoR → TechLead → Spec Gate → Dev → Code Review → DoD → Release) and keeps GitHub labels in sync with workflow state.

## Features

- **Workflow graph execution**: Full graph runs per trigger, with loopbacks on gate failures.
- **Gate validators**: DoR, Spec, and DoD gates with explicit failure payloads.
- **Spec tooling**: Spec schema parsing, Touch List parsing, and Gherkin validation.
- **Playbook enforcement**: Allowed/forbidden frameworks and patterns via `docs/architecture-playbook.yaml`.
- **Event-driven triggering**: Webhook listener triggers scans; no polling loop.
- **MCP integration**: Optional filesystem/git/GitHub MCP tools for file ops.

## Runtime

- .NET 8 target framework
- Runs locally or via Docker Compose
- Triggered by GitHub webhooks (recommended)

## Quick Start

### Prerequisites

- Docker and Docker Compose
- GitHub Personal Access Token or GitHub App token with repo scope
- OpenAI-compatible API endpoint (OpenAI, Azure OpenAI, or compatible gateway)

### Configuration

1. Copy the example environment file:
   ```bash
   cp orchestrator/.env.example orchestrator/.env
   ```

2. Configure required environment variables in `orchestrator/.env`:

   **Required**
   - `REPO_OWNER` - Repository owner username/organization
   - `REPO_NAME` - Repository name
   - `OPENAI_API_KEY` - LLM API key (required for executor calls)

   **Recommended for full functionality**
   - `GITHUB_TOKEN` - Personal Access Token or GitHub App token (labels, PRs, projects)
   - `WORKSPACE_PATH` - Local workspace path for cloning (must be a git repo)
   - `DEFAULT_BASE_BRANCH` - Base branch for PRs (usually `main`)

   **OpenAI Configuration (optional overrides)**
   - `OPENAI_BASE_URL` - API endpoint (default: `https://api.openai.com/v1`)
   - `OPENAI_MODEL` - Default model to use (default: `gpt-5-mini`)
   - `DEV_MODEL` - Model for development stage (default: `gpt-5`)
   - `TECHLEAD_MODEL` - Model for tech lead stage (default: `gpt-5-mini`)

   **Git Configuration (optional overrides)**
   - `WORKSPACE_HOST_PATH` - Host path for MCP Docker mounts (default: `WORKSPACE_PATH`)
   - `GIT_REMOTE_URL` - Override git remote URL
   - `GIT_AUTHOR_NAME` - Commit author name
   - `GIT_AUTHOR_EMAIL` - Commit author email

   **Webhook Configuration (optional, defaults provided)**
   - `WEBHOOK_LISTEN_HOST` - Host to bind (default: `localhost`)
   - `WEBHOOK_PORT` - Port to bind (default: `5005`)
   - `WEBHOOK_PATH` - Path to listen on (default: `/webhook`)
   - `WEBHOOK_SECRET` - GitHub webhook secret for signature verification
   - The listener attempts HTTPS first; if no HTTPS binding is available, it falls back to HTTP with a warning. Configure an HTTPS binding for production.

   **Workflow Labels (optional overrides)**
   - `WORK_ITEM_LABEL` - Label to trigger workflow
   - `IN_PROGRESS_LABEL` - Applied when work starts
   - `DONE_LABEL` - Applied when complete
   - `BLOCKED_LABEL` - Applied when blocked
   - `DOR_LABEL` - DoR gate label
   - `SPEC_GATE_LABEL` - Spec gate label
   - `USER_REVIEW_REQUIRED_LABEL` - Request user clarification
   - `CODE_REVIEW_NEEDED_LABEL` - Code review requested
   - `CODE_REVIEW_APPROVED_LABEL` - Code review approved
   - `CODE_REVIEW_CHANGES_REQUESTED_LABEL` - Changes requested
   - `RESET_LABEL` - Reset workflow state

   **GitHub Projects (optional)**
   - `PROJECT_OWNER` - Project owner
   - `PROJECT_OWNER_TYPE` - Owner type (user/organization)
   - `PROJECT_NUMBER` - Project number

### Running with Docker

```bash
docker compose -f orchestrator/docker-compose.yml up -d --build
```

```bash
docker compose -f orchestrator/docker-compose.yml logs -f
```

```bash
docker compose -f orchestrator/docker-compose.yml down
```

## Development

### Building Locally

```bash
dotnet restore src/Orchestrator.App/Orchestrator.App.csproj
dotnet build src/Orchestrator.App/Orchestrator.App.csproj --configuration Release
```

### Running Tests

```bash
dotnet test tests/Orchestrator.App.Tests.csproj --configuration Release
```

## Architecture Overview

- **Workflow engine**: `WorkflowFactory` builds the SDLC graph; `WorkflowRunner` executes full runs.
- **Executors**: Refinement, TechLead, Dev, CodeReview, Release plus gates (DoR, Spec, DoD).
- **Artifacts**:
  - Specs: `orchestrator/specs/issue-<id>.md`
  - Questions: `orchestrator/questions/issue-<id>.md`
  - Reviews: `orchestrator/reviews/issue-<id>.md`
  - Releases: `orchestrator/release/issue-<id>.md`
- **Playbook**: `docs/architecture-playbook.yaml` for framework/pattern constraints.

## Documentation

- Concept: `docs/sdlc-orchestrator-konzept-v3.md`
- Refactoring plan: `docs/refactoring-plan.md`
- Workflow: `docs/workflow.md`
- Migration guide: `docs/migration-guide.md`
- MCP Setup: `docs/MCP_SETUP.md`
- Docker Deployment: `docs/DOCKER_DEPLOYMENT.md`

## License

This project is licensed under the MIT License.
