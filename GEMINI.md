# GEMINI.md - Project Overview: Orchestrator

This document provides a comprehensive overview of the Orchestrator project, its purpose, architecture, and development conventions.

## Project Overview

The Orchestrator is an AI-powered GitHub repository orchestrator built with the Microsoft Agent Framework. It automates development workflows by processing GitHub issues, generating specifications, implementing code changes, and creating pull requests. The project is built on .NET 8 and is designed to run as a long-lived service using Docker.

### Key Technologies

*   **.NET 8:** The core framework for the application.
*   **Microsoft Agent Framework:** Used for creating and managing AI agents that perform development tasks.
*   **OpenAI:** The application interacts with OpenAI-compatible APIs for language model processing.
*   **Octokit.NET:** The library used for interacting with the GitHub API.
*   **LibGit2Sharp:** Used for Git operations.
*   **Docker:** The application is designed to be deployed and run in a Docker container.

### Architecture

The application's entry point is the `Orchestrator.App.dll` assembly, which is executed when the Docker container starts. The main components of the application are:

*   **`LlmClient`:** Handles communication with the OpenAI API.
*   **`OctokitGitHubClient`:** Manages interactions with the GitHub API, including issues, pull requests, and labels.
*   **`McpClientManager`:** Manages the Model Context Protocol (MCP) clients, which provide AI agents with access to the filesystem, Git, and GitHub operations.
*   **Configuration:** The application is configured through environment variables, as defined in the `.env.example` file. The app reads environment variables and `.env` files. MCP integrations use Docker, so ensure the Docker daemon is available when running locally.

## Building and Running

### Building Locally

To build the project locally, use the following commands:

```bash
dotnet restore src/Orchestrator.App/Orchestrator.App.csproj
dotnet build src/Orchestrator.App/Orchestrator.App.csproj --configuration Release
```

### Running Locally
```bash
dotnet run --project src/Orchestrator.App/Orchestrator.App.csproj
```

### Running Tests

To run the project's tests, use the following command:

```bash
dotnet test tests/Orchestrator.App.Tests.csproj --configuration Release
```

### Running with Docker

The recommended way to run the application is with Docker Compose.

1.  **Configure Environment:** Copy the `.env.example` file to `.env` and fill in the required environment variables.
2.  **Start the Service:**
    ```bash
    docker compose -f docker-compose.yml up -d --build
    ```
3.  **View Logs:**
    ```bash
    docker compose -f docker-compose.yml logs -f
    ```
4.  **Stop the Service:**
    ```bash
    docker compose -f docker-compose.yml down
    ```

## Development Conventions

### Project Structure
- `src/Orchestrator.App/` holds the .NET application (entry point, core domain, infrastructure, utilities).
- `tests/` contains xUnit tests, organized by area (`tests/Core`, `tests/Infrastructure`, `tests/Utilities`, `tests/Agents`).
- `docs/` contains architecture, workflow, and deployment documentation.
- `docker-compose.yml` and `Dockerfile` support containerized runs.

### Coding Style
*   **C#:** with `net8.0`, implicit usings, and nullable enabled; follow existing namespace-to-folder layout.
*   **Indentation:** 4 spaces. 
*   **Naming:** Types and methods use PascalCase; locals/parameters use camelCase.
*   **File Naming:** Keep files aligned with their primary type name (e.g., `RepoGit.cs` for `RepoGit`).
*   **Dependency Management:** Dependencies are managed using NuGet packages, as defined in the `src/Orchestrator.App/Orchestrator.App.csproj` file.
*   **Continuous Integration:** The project uses GitHub Actions for continuous integration, including building, testing, and security scanning.

### Testing Guidelines
*   **Frameworks:** xUnit with Moq and FluentAssertions.
*   **Test Files:** use `*Tests.cs` naming and live under `tests/`.
*   **Test Helpers:** Use `tests/TestHelpers` for shared setup (e.g., env-scoped tests).
*   **Coverage:** Add tests for new behavior and keep coverage stable; avoid committing generated artifacts like `coverage.xml`.

### Work Chunks & Quality Gates
*   Work in small, reviewable chunks.
*   After each chunk: add/adjust tests, run build and tests, then run coverage before pushing.
*   Only commit when build and tests are green and coverage is over 80% for the chunk.
*   Do not use emojis in commits, PRs, or documentation.

### Commit & Pull Request Guidelines
*   **Commit Messages:** Prefer conventional prefixes when possible: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.
*   **Pull Requests:** Keep commits focused and include test results in the PR description (example: `dotnet test ...`). PRs should summarize changes, list testing, and link related issues or workstreams.
