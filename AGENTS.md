# Repository Guidelines

## Project Structure & Module Organization
- `src/Orchestrator.App/` holds the .NET application (entry point, core domain, infrastructure, utilities).
- `tests/` contains xUnit tests, organized by area (`tests/Core`, `tests/Infrastructure`, `tests/Utilities`, `tests/Agents`).
- `docs/` contains architecture, workflow, and deployment documentation.
- `docker-compose.yml` and `Dockerfile` support containerized runs.

## Build, Test, and Development Commands
- `dotnet restore src/Orchestrator.App/Orchestrator.App.csproj` restores packages.
- `dotnet build src/Orchestrator.App/Orchestrator.App.csproj --configuration Release` builds the app.
- `dotnet test tests/Orchestrator.App.Tests.csproj --configuration Release` runs unit tests.
- `dotnet run --project src/Orchestrator.App/Orchestrator.App.csproj` runs locally.
- `docker compose up -d --build` starts the service via Docker.

## Coding Style & Naming Conventions
- C# with `net8.0`, implicit usings, and nullable enabled; follow existing namespace-to-folder layout.
- Indentation: 4 spaces. Types and methods use PascalCase; locals/parameters use camelCase.
- Keep files aligned with their primary type name (e.g., `RepoGit.cs` for `RepoGit`).

## Testing Guidelines
- Frameworks: xUnit with Moq and FluentAssertions.
- Test files use `*Tests.cs` naming and live under `tests/`.
- Use `tests/TestHelpers` for shared setup (e.g., env-scoped tests).
- Add tests for new behavior and keep coverage stable; avoid committing generated artifacts like `coverage.xml`.

## Work Chunks & Quality Gates
- Work in small, reviewable chunks.
- After each chunk: add/adjust tests, run build and tests, then run coverage before pushing.
- Only commit when build and tests are green and coverage is over 80% for the chunk.
- Do not use emojis in commits, PRs, or documentation.

## Commit & Pull Request Guidelines
- Prefer conventional prefixes when possible: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.
- Keep commits focused and include test results in the PR description (example: `dotnet test ...`).
- PRs should summarize changes, list testing, and link related issues or workstreams.

## Configuration & Security Notes
- The app reads environment variables and `.env` files (see README for keys).
- MCP integrations use Docker; ensure the Docker daemon is available when running locally.
