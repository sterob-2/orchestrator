# CI/CD Workflows

This directory contains all GitHub Actions workflows for the Orchestrator project.

## Workflows Overview

### Core CI/CD

#### `ci.yml` - Continuous Integration
**Triggers**: Push to main/develop, Pull Requests
**Purpose**: Primary build and test pipeline

- Multi-platform builds (Linux, Windows, macOS)
- Unit and integration tests
- Code coverage collection
- Code quality analysis with dotnet-format
- Dependency vulnerability scanning
- Artifact publishing on main branch

**Quality Gates**:
- All builds must succeed
- Code formatting must pass
- SonarCloud quality gate must pass

#### `sonarcloud.yml` - Code Quality Gate
**Triggers**: Push to main/develop, Pull Requests
**Purpose**: Deep code quality and security analysis

- Static code analysis
- Code coverage reporting
- Security vulnerability detection
- Code smell identification
- Technical debt tracking
- Quality gate enforcement

**Required Secret**: `SONAR_TOKEN`

### Security

#### `security.yml` - Security Scanning
**Triggers**: Push, Pull Requests, Weekly schedule
**Purpose**: Comprehensive security analysis

- CodeQL security analysis
- Secret scanning with TruffleHog
- Dependency vulnerability review
- SBOM (Software Bill of Materials) generation
- License compliance checking

**Permissions**: Requires `security-events: write`

### Container Management

#### `docker.yml` - Docker Build & Publish
**Triggers**: Push to main, Tags, Pull Requests
**Purpose**: Container image lifecycle management

- Multi-architecture builds (amd64, arm64)
- Automated publishing to GitHub Container Registry
- Docker image vulnerability scanning with Trivy
- Dockerfile linting with hadolint
- Semantic versioning support

**Registry**: `ghcr.io/sterob-2/orchestrator`

### Release Management

#### `release.yml` - Release Automation
**Triggers**: Version tags (v*), Manual dispatch
**Purpose**: Automated release creation and distribution

- Multi-platform binary builds:
  - Linux (x64, ARM64)
  - Windows (x64)
  - macOS (x64, ARM64)
- Automated changelog generation
- GitHub Release creation
- Docker image publishing with version tags
- Release artifact uploads

**Tag Format**: `v1.0.0`, `v1.0.0-rc1`, `v1.0.0-beta1`

## Configuration Files

### `dependabot.yml`
Automated dependency updates for:
- NuGet packages (weekly, Mondays 09:00)
- GitHub Actions (weekly)
- Docker base images (weekly)

**Reviewer**: @sterob-2

### `changelog-config.json`
Release changelog generation configuration:
- Categorizes PRs by label (features, bugs, security, docs, maintenance)
- Automatically formats release notes
- Generates comparison links

## Required Secrets

Add these secrets in Repository Settings > Secrets and variables > Actions:

| Secret | Purpose | How to Get |
|--------|---------|------------|
| `SONAR_TOKEN` | SonarCloud authentication | [SonarCloud Account Settings](https://sonarcloud.io/account/security) |
| `GITHUB_TOKEN` | Automatically provided | No action needed |

## Status Checks

The following checks must pass for PR merges:

1. **Build & Test** - All platforms
2. **Code Quality** - Formatting and analysis
3. **SonarCloud Quality Gate** - Code quality metrics
4. **Security Scanning** - No critical vulnerabilities
5. **Docker Build** - Container builds successfully

## Workflow Dependencies

```
Pull Request:
  ├── ci.yml (build-and-test)
  ├── ci.yml (code-quality)
  ├── ci.yml (dependency-scan)
  ├── sonarcloud.yml (quality gate) ← BLOCKS merge
  ├── security.yml (CodeQL, secrets)
  └── docker.yml (build-only)

Main Branch Push:
  ├── ci.yml (full pipeline + publish artifacts)
  ├── sonarcloud.yml (analysis + quality gate)
  ├── security.yml (full scan + SBOM)
  └── docker.yml (build + publish to registry)

Version Tag (v*):
  ├── release.yml (create release + binaries)
  └── docker.yml (publish versioned images)
```

## Performance Optimizations

- **Caching**: SonarCloud scanner, NuGet packages, Docker layers
- **Parallel Jobs**: Independent jobs run concurrently
- **Artifact Reuse**: Coverage reports shared between jobs
- **Conditional Execution**: Some steps only run on specific branches/events

## Maintenance

### Adding a New Workflow
1. Create `.github/workflows/<name>.yml`
2. Define clear triggers and permissions
3. Add documentation to this README
4. Test in a feature branch first

### Modifying Quality Gates
1. Update SonarCloud settings in [project settings](https://sonarcloud.io/project/configuration?id=sterob-2_orchestrator)
2. Adjust `sonar-project.properties` for exclusions
3. Update workflow `continue-on-error` flags if needed

### Troubleshooting
- **SonarCloud fails**: Check `SONAR_TOKEN` secret is set
- **Docker publish fails**: Verify `GITHUB_TOKEN` has package write permission
- **Coverage not uploading**: Ensure tests are running and generating reports
