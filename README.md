# Orchestrator

Repo-local orchestrator service built with Microsoft Agent Framework packages.

## Runtime

- .NET 8
- Runs as a long-lived service via Docker Compose
- Polls GitHub issues with a specific label and opens draft PRs

## Configuration

Copy `orchestrator/.env.example` to `orchestrator/.env` and fill:

- OPENAI_BASE_URL
  - Default: https://api.openai.com/v1
  - For OpenAI-compatible gateways: set your base URL, for example `https://adesso-ai-hub.3asabc.de/v1`
- OPENAI_API_KEY
- OPENAI_MODEL
- DEV_MODEL
  - Model override for the `agent:dev` stage (default: `gpt-5`)
- TECHLEAD_MODEL
  - Model override for TechLead reviews/specs
- WORKSPACE_PATH
- GIT_REMOTE_URL
  - Optional. If omitted, the orchestrator uses `GITHUB_TOKEN` to build an authenticated GitHub HTTPS URL.
- GIT_AUTHOR_NAME
- GIT_AUTHOR_EMAIL
  - Model override for the `agent:dev` stage (default: `qwen3-coder-480b`)

- GITHUB_TOKEN
  - Personal Access Token or GitHub App token with repo scope
- REPO_OWNER
- REPO_NAME
- DEFAULT_BASE_BRANCH

Labels and polling:
- WORK_ITEM_LABEL
- IN_PROGRESS_LABEL
- DONE_LABEL
- BLOCKED_LABEL
- USER_REVIEW_REQUIRED_LABEL
- SPEC_QUESTIONS_LABEL
- SPEC_CLARIFIED_LABEL
- CODE_REVIEW_NEEDED_LABEL
- CODE_REVIEW_APPROVED_LABEL
- CODE_REVIEW_CHANGES_REQUESTED_LABEL
- RESET_LABEL
- POLL_INTERVAL_SECONDS
- FAST_POLL_INTERVAL_SECONDS
- PROJECT_OWNER
- PROJECT_OWNER_TYPE
- PROJECT_NUMBER

## Run with Docker

Build and start:

- docker compose -f orchestrator/docker-compose.yml up -d --build
- docker compose -f orchestrator/docker-compose.yml logs -f

## Notes

This version keeps the role agents as placeholders and focuses on:
- correct OpenAI configuration (API key + base URL)
- reliable GitHub issue polling and PR creation

Next step is to replace the placeholder role agents with actual Agent Framework agents using the OpenAI client.
