# Docker Deployment Guide

This guide explains how the Orchestrator runs in Docker with MCP (Model Context Protocol) server integration.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│  Docker Host                                            │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │  Orchestrator Container                           │ │
│  │  ┌─────────────────────────────────────────────┐  │ │
│  │  │  .NET Application                           │  │ │
│  │  │  - McpClientManager                         │  │ │
│  │  │  - Agents                                   │  │ │
│  │  │  - Workflows                                │  │ │
│  │  └─────────────────────────────────────────────┘  │ │
│  │                    │                              │ │
│  │                    │ spawns via Docker CLI        │ │
│  │                    ▼                              │ │
│  │         ┌──────────────────────┐                 │ │
│  │         │ Docker Socket        │                 │ │
│  │         │ /var/run/docker.sock │                 │ │
│  │         └──────────────────────┘                 │ │
│  └────────────────────│───────────────────────────────┘ │
│                       │                                 │
│  ┌────────────────────┴─────────────────────────────┐  │
│  │  Dynamically Spawned MCP Server Containers       │  │
│  │                                                   │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌────────┐ │  │
│  │  │ GitHub MCP   │  │ Filesystem   │  │ Git    │ │  │
│  │  │ Server       │  │ MCP Server   │  │ MCP    │ │  │
│  │  │ (Go/Docker)  │  │ (Node.js)    │  │ Server │ │  │
│  │  │              │  │              │  │(Python)│ │  │
│  │  └──────────────┘  └──────────────┘  └────────┘ │  │
│  │        ▲                  ▲               ▲      │  │
│  │        └──────────────────┴───────────────┘      │  │
│  │              stdio communication                 │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## How It Works

### 1. Docker-in-Docker Pattern

The Orchestrator container has access to the host's Docker socket (`/var/run/docker.sock`), which allows it to:
- Spawn new Docker containers for MCP servers
- Communicate with them via stdin/stdout
- Automatically clean up containers when done

### 2. MCP Server Lifecycle

When the Orchestrator initializes:

1. **McpClientManager.InitializeAsync()** is called
2. For each MCP server (GitHub, Filesystem, Git):
   - A Docker container is spawned with `docker run -i --rm`
   - The container runs the MCP server in stdio mode
   - `McpClient.CreateAsync()` connects via StdioClientTransport
   - Tools are retrieved via `ListToolsAsync()`
3. Containers remain running until disposed
4. On shutdown, containers are automatically removed (`--rm` flag)

## Deployment

### Prerequisites

- Docker and Docker Compose installed
- GitHub Personal Access Token (for GitHub MCP server)
- Workspace directory with your repository

### Environment Configuration

Create a `.env` file in the repository root:

```bash
# Repository Configuration
REPO_OWNER=your-github-username
REPO_NAME=your-repo-name
WORKSPACE_PATH=/workspace
DEFAULT_BASE_BRANCH=main

# GitHub Authentication
GITHUB_TOKEN=ghp_yourGitHubTokenHere

# OpenAI Configuration (for LLM)
OPENAI_API_KEY=sk-yourOpenAIKeyHere
OPENAI_BASE_URL=https://api.openai.com/v1

# Orchestrator Configuration
WORK_ITEM_LABEL=orchestrator:work-item

```

### Build and Run

```bash
# Build the image
docker-compose build

# Start the orchestrator
docker-compose up -d

# View logs
docker-compose logs -f orchestrator

# Stop
docker-compose down
```

## Dockerfile Breakdown

### Build Stage
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Orchestrator.App/ ./Orchestrator.App/
RUN dotnet publish ./Orchestrator.App/Orchestrator.App.csproj -c Release -o /out
```

- Uses .NET 10.0 SDK to build the application
- Publishes a release build

### Runtime Stage
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0
RUN apt-get update && apt-get install -y --no-install-recommends \
    git \
    ca-certificates \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Install Docker CLI for spawning MCP server containers
RUN curl -fsSL https://get.docker.com -o get-docker.sh && \
    sh get-docker.sh && \
    rm get-docker.sh

WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "Orchestrator.App.dll"]
```

- Uses smaller .NET runtime image
- Installs Git (for repository operations)
- **Installs Docker CLI** (for spawning MCP server containers)
- Copies compiled application from build stage

## docker-compose.yml Breakdown

```yaml
services:
  orchestrator:
    build:
      context: .
      dockerfile: Dockerfile
    env_file:
      - .env
    volumes:
      # Mount workspace directory
      - /opt/repos/conjunction:/workspace
      # Mount Docker socket to allow spawning MCP server containers
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      # Ensure GitHub token is available for MCP server spawning
      - GITHUB_PERSONAL_ACCESS_TOKEN=${GITHUB_TOKEN}
    restart: unless-stopped
    network_mode: bridge
```

### Key Configuration:

1. **Workspace Volume**:
   - Maps host directory `/opt/repos/conjunction` to `/workspace` in container
   - Update this to point to your repository location

2. **Docker Socket Mount**:
   - `/var/run/docker.sock:/var/run/docker.sock`
   - Allows container to spawn sibling containers
   - **Security Note**: This gives the container significant privileges

3. **Environment Variables**:
   - `GITHUB_PERSONAL_ACCESS_TOKEN` is passed through for MCP servers
   - Loaded from `.env` file via `${GITHUB_TOKEN}`

4. **Network Mode**:
   - `bridge` allows spawned MCP containers to communicate

## Current MCP Status

**All MCP Servers Operational! ✅**

- ✅ **Filesystem MCP Server**: 14 tools (read_file, write_file, edit_file, create_directory, list_directory, search_files, etc.)
- ✅ **Git MCP Server**: 12 tools (git_status, git_diff, git_commit, git_log, etc.)
- ✅ **GitHub MCP Server**: 40 tools (create_branch, create_pull_request, issue_read, push_files, etc.)

**Total Tools Available:** 66 tools

**Recent Fixes:**
- Fixed Docker DNS resolution in WSL2 by adding explicit DNS servers (8.8.8.8, 1.1.1.1)
- Fixed Git MCP server by correcting workspace host path for Docker-in-Docker volume mounts

## MCP Server Container Details

### GitHub MCP Server
```bash
docker run -i --rm \
  -e GITHUB_PERSONAL_ACCESS_TOKEN \
  ghcr.io/github/github-mcp-server
```
- **Image**: Official GitHub MCP server (Go-based)
- **Size**: ~50MB
- **Requires**: GitHub Personal Access Token

### Filesystem MCP Server
```bash
docker run -i --rm \
  node:lts-alpine
# Then: npx @modelcontextprotocol/server-filesystem /workspace
```
- **Image**: Node.js LTS Alpine
- **Size**: ~180MB
- **Mounted**: Workspace directory

### Git MCP Server
```bash
docker run -i --rm \
  python:3.12-alpine
# Then: uvx mcp-server-git --repository /workspace
```
- **Image**: Python 3.12 Alpine
- **Size**: ~50MB
- **Mounted**: Workspace directory

## Monitoring and Debugging

### View Running Containers

```bash
# From host machine
docker ps

# You should see:
# - orchestrator container (always running)
# - MCP server containers (when orchestrator is active)
```

### Check Orchestrator Logs

```bash
# View logs
docker-compose logs -f orchestrator

# Look for MCP initialization messages:
# [MCP] Initializing MCP clients...
# [MCP] Filesystem server connected. Tools: 8
# [MCP] Git server connected. Tools: 12
# [MCP] GitHub server connected. Tools: 20
```

### Access Container Shell

```bash
# Enter orchestrator container
docker-compose exec orchestrator /bin/bash

# Check if Docker CLI is available
docker --version

# Check if Docker socket is accessible
docker ps
```

### Debug MCP Server Spawning

```bash
# Watch containers being created/destroyed
watch -n 1 docker ps -a

# Check container logs for a specific MCP server
docker logs <container-id>
```

## Troubleshooting

### Issue: "Cannot connect to Docker daemon"

**Problem**: Orchestrator can't spawn MCP containers

**Solution**:
```bash
# Ensure Docker socket is properly mounted
docker-compose down
# Check docker-compose.yml has:
# - /var/run/docker.sock:/var/run/docker.sock

# Restart
docker-compose up -d
```

### Issue: "Permission denied" accessing Docker socket

**Problem**: Container user doesn't have permission to access Docker socket

**Solution**: Add Docker group to container or run as root (less secure):
```dockerfile
# In Dockerfile, add after Docker installation:
RUN groupadd -g 999 docker && usermod -aG docker app
USER app
```

### Issue: MCP initialization warnings

**Problem**: MCP servers fail to initialize

**Symptoms**:
```
[MCP] Warning: Failed to connect to GitHub server: ...
[MCP] Continuing without MCP tools.
```

**Solution**:
1. Check GitHub token is set in `.env`
2. Verify Docker CLI is installed in container:
   ```bash
   docker-compose exec orchestrator docker --version
   ```
3. Test Docker socket access:
   ```bash
   docker-compose exec orchestrator docker ps
   ```
4. Check Docker images are accessible:
   ```bash
   docker pull ghcr.io/github/github-mcp-server
   ```

### Issue: High disk usage from containers

**Problem**: Old MCP containers not being cleaned up

**Solution**:
```bash
# Remove stopped containers
docker container prune -f

# Remove unused images
docker image prune -a -f

# Check if --rm flag is being used in McpClientManager.cs
```

## Security Considerations

### Docker Socket Access

⚠️ **Mounting `/var/run/docker.sock` gives the container full Docker host access.**

This means:
- Container can spawn any Docker image
- Container can access other containers
- Container can modify Docker daemon configuration

**Mitigations**:
1. Run on dedicated Docker host
2. Use Docker socket proxy like [docker-socket-proxy](https://github.com/Tecnativa/docker-socket-proxy)
3. Implement AppArmor/SELinux profiles
4. Regular security audits

### Secrets Management

**Never commit `.env` file to git!**

Best practices:
- Use `.env.example` as a template
- Store secrets in external secret manager (Vault, AWS Secrets Manager)
- Use Docker secrets for production deployments

### Network Isolation

Current setup uses `bridge` network. For production:
- Create custom Docker network
- Limit container network access
- Use network policies if on Kubernetes

## Production Deployment

### Recommended Changes for Production:

1. **Use Docker Secrets**:
```yaml
services:
  orchestrator:
    secrets:
      - github_token
      - openai_api_key

secrets:
  github_token:
    external: true
  openai_api_key:
    external: true
```

2. **Add Health Checks**:
```yaml
services:
  orchestrator:
    healthcheck:
      test: ["CMD", "dotnet", "health-check"]
      interval: 30s
      timeout: 10s
      retries: 3
```

3. **Resource Limits**:
```yaml
services:
  orchestrator:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
        reservations:
          cpus: '1'
          memory: 2G
```

4. **Logging**:
```yaml
services:
  orchestrator:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

## Performance Optimization

### Container Image Size

Current optimizations:
- Multi-stage build (SDK vs runtime)
- Alpine-based images where possible
- `.dockerignore` to exclude unnecessary files

### Startup Time

- MCP server containers are spawned on-demand
- First connection downloads images (one-time cost)
- Subsequent connections are fast (~100ms)

### Resource Usage

Typical usage:
- **Orchestrator**: 200-500MB RAM
- **MCP Servers** (combined): 300-500MB RAM
- **Total**: ~1GB RAM under normal load

## Migration from Host Deployment

If migrating from running directly on the host:

1. Ensure `.env` file is configured
2. Update `WORKSPACE_PATH=/workspace` in `.env`
3. Update volume mount in `docker-compose.yml` to point to your repo:
   ```yaml
   volumes:
     - /path/to/your/repo:/workspace
   ```
4. Build and run:
   ```bash
   docker-compose up -d
   ```

The orchestrator will now run in Docker and spawn MCP servers as containers! 🎉
