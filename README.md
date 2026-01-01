# Orchestrator

[![CI](https://github.com/sterob-2/orchestrator/actions/workflows/ci.yml/badge.svg)](https://github.com/sterob-2/orchestrator/actions/workflows/ci.yml)
[![Security Scanning](https://github.com/sterob-2/orchestrator/actions/workflows/security.yml/badge.svg)](https://github.com/sterob-2/orchestrator/actions/workflows/security.yml)
[![SonarCloud Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=sterob-2_orchestrator&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=sterob-2_orchestrator)
[![Docker Build](https://github.com/sterob-2/orchestrator/actions/workflows/docker.yml/badge.svg)](https://github.com/sterob-2/orchestrator/actions/workflows/docker.yml)

An AI-powered GitHub repository orchestrator built with [Microsoft Agent Framework](https://github.com/microsoft/agents). Automatically manages development workflows by processing GitHub issues, creating specifications, implementing changes, and opening pull requests.

## Features

- **Automated Workflow Orchestration**: Processes GitHub issues labeled as work items through a complete development lifecycle
- **AI-Powered Development**: Uses LLM agents for planning, specification, implementation, and code review
- **Microsoft Agent Framework**: Built on Microsoft's agent framework for structured AI workflows
- **GitHub Integration**: Seamless integration with GitHub Issues, Projects, and Pull Requests
- **Quality Gates**: Comprehensive CI/CD pipeline with security scanning, code quality analysis, and automated testing
- **Docker Support**: Containerized deployment with Docker Compose

## Runtime

- **.NET 8** target framework
- **.NET 10** runtime support via Docker
- Runs as a long-lived service via Docker Compose
- Self-hosted GitHub Actions runners supported

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

   **OpenAI Configuration:**
   - `OPENAI_BASE_URL` - API endpoint (default: `https://api.openai.com/v1`)
   - `OPENAI_API_KEY` - Your API key
   - `OPENAI_MODEL` - Default model to use
   - `DEV_MODEL` - Model for development stage (optional override)
   - `TECHLEAD_MODEL` - Model for tech lead reviews (optional override)

   **GitHub Configuration:**
   - `GITHUB_TOKEN` - Personal Access Token or GitHub App token
   - `REPO_OWNER` - Repository owner username/organization
   - `REPO_NAME` - Repository name
   - `DEFAULT_BASE_BRANCH` - Base branch for PRs (usually `main`)

   **Git Configuration:**
   - `WORKSPACE_PATH` - Local workspace path for cloning
   - `GIT_REMOTE_URL` - (Optional) Git remote URL
   - `GIT_AUTHOR_NAME` - Commit author name
   - `GIT_AUTHOR_EMAIL` - Commit author email

   **Workflow Labels:**
   - `WORK_ITEM_LABEL` - Label to trigger workflow
   - `IN_PROGRESS_LABEL` - Applied when work starts
   - `DONE_LABEL` - Applied when complete
   - `BLOCKED_LABEL` - Applied when blocked
   - `USER_REVIEW_REQUIRED_LABEL` - Request user clarification
   - `SPEC_QUESTIONS_LABEL` - Specification questions pending
   - `SPEC_CLARIFIED_LABEL` - Specification clarified
   - `CODE_REVIEW_NEEDED_LABEL` - Code review requested
   - `CODE_REVIEW_APPROVED_LABEL` - Code review approved
   - `CODE_REVIEW_CHANGES_REQUESTED_LABEL` - Changes requested
   - `RESET_LABEL` - Reset workflow state

   **Polling Configuration:**
   - `POLL_INTERVAL_SECONDS` - Normal polling interval
   - `FAST_POLL_INTERVAL_SECONDS` - Fast polling when work in progress

   **GitHub Projects (Optional):**
   - `PROJECT_OWNER` - Project owner
   - `PROJECT_OWNER_TYPE` - Owner type (user/organization)
   - `PROJECT_NUMBER` - Project number

### Running with Docker

Build and start the orchestrator:

```bash
docker compose -f orchestrator/docker-compose.yml up -d --build
```

View logs:

```bash
docker compose -f orchestrator/docker-compose.yml logs -f
```

Stop the service:

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

## CI/CD Pipeline

This repository includes a comprehensive CI/CD pipeline with multiple quality gates:

### Continuous Integration

- **Build & Test**: Automated build and test execution
- **Code Quality Analysis**: Code formatting and style enforcement
- **Dependency Scanning**: Vulnerability detection in dependencies

### Security

- **CodeQL Analysis**: Static code analysis for security vulnerabilities
- **Secret Scanning**: TruffleHog integration for secret detection
- **Dependency Review**: Automated dependency vulnerability scanning
- **SBOM Generation**: Software Bill of Materials for releases

### Quality Gates

- **SonarCloud**: Code quality, coverage, and security analysis
- **Docker Scanning**: Trivy vulnerability scanning for container images
- **Dockerfile Linting**: hadolint validation

### Release Pipeline

- **Multi-Platform Builds**: Linux (x64, ARM64), Windows (x64), macOS (x64, ARM64)
- **GitHub Releases**: Automated release creation with changelog
- **Docker Registry**: Automated container publishing to GitHub Container Registry

### Setting Up SonarCloud

To enable SonarCloud quality gate:

1. Sign up at [SonarCloud](https://sonarcloud.io) with your GitHub account
2. Import the `sterob-2/orchestrator` repository
3. Get your SonarCloud token
4. Add the `SONAR_TOKEN` secret to your GitHub repository:
   - Go to Repository Settings > Secrets and variables > Actions
   - Create new repository secret named `SONAR_TOKEN`
   - Paste your SonarCloud token

The SonarCloud workflow will automatically analyze code quality and generate coverage reports on every push and pull request.

## Architecture

The orchestrator implements a state machine workflow for processing GitHub issues:

1. **Discovery**: Polls GitHub for issues with the work item label
2. **Planning**: Generates implementation plan and specification
3. **Specification Review**: Allows user to review and clarify requirements
4. **Implementation**: Creates implementation using AI agents
5. **Code Review**: Optional automated code review
6. **Pull Request**: Opens draft PR with implemented changes

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please ensure all CI checks pass before submitting pull requests.
