# MCP Server Dependencies Setup

This guide covers installing the required dependencies for the Model Context Protocol (MCP) servers used by the Orchestrator.

## Overview

The Orchestrator uses three MCP servers:

1. **Filesystem MCP Server** - File operations (requires Node.js/npx)
2. **Git MCP Server** - Git operations (requires Python/uvx)
3. **GitHub MCP Server** - GitHub API operations (requires Docker)

## Quick Setup

### Windows (WSL2/Ubuntu)

```bash
# Install Node.js (for npx)
curl -fsSL https://deb.nodesource.com/setup_lts.sh | sudo -E bash -
sudo apt-get install -y nodejs

# Verify installation
node --version
npx --version

# Install Python and uv (for uvx)
sudo apt-get update
sudo apt-get install -y python3 python3-pip

# Install uv (Python package installer with built-in uvx)
curl -LsSf https://astral.sh/uv/install.sh | sh

# Add uv to PATH (add this to your ~/.bashrc or ~/.zshrc)
export PATH="$HOME/.cargo/bin:$PATH"

# Reload shell or source the config
source ~/.bashrc

# Verify uv/uvx installation
uv --version
uvx --version

# Install Docker (for GitHub MCP server)
# Install Docker Desktop for Windows with WSL2 integration
# Download from: https://www.docker.com/products/docker-desktop/

# Alternatively, install Docker Engine in WSL2:
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Start Docker service
sudo service docker start

# Verify Docker installation
docker --version
docker ps
```

### Windows (Native)

#### Node.js

1. Download Node.js LTS from https://nodejs.org/
2. Run the installer (includes npm and npx)
3. Verify installation:
   ```powershell
   node --version
   npx --version
   ```

#### Python and uv

1. Download Python from https://www.python.org/downloads/
2. Run installer and check "Add Python to PATH"
3. Open PowerShell and install uv:
   ```powershell
   powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
   ```
4. Restart PowerShell and verify:
   ```powershell
   uv --version
   uvx --version
   ```

### macOS

```bash
# Install Node.js via Homebrew
brew install node

# Verify installation
node --version
npx --version

# Install uv (includes uvx)
curl -LsSf https://astral.sh/uv/install.sh | sh

# Add to PATH (add to ~/.zshrc or ~/.bash_profile)
export PATH="$HOME/.cargo/bin:$PATH"

# Reload shell
source ~/.zshrc

# Verify uv/uvx installation
uv --version
uvx --version
```

### Linux (Ubuntu/Debian)

```bash
# Install Node.js
curl -fsSL https://deb.nodesource.com/setup_lts.sh | sudo -E bash -
sudo apt-get install -y nodejs

# Verify installation
node --version
npx --version

# Install Python
sudo apt-get update
sudo apt-get install -y python3 python3-pip

# Install uv
curl -LsSf https://astral.sh/uv/install.sh | sh

# Add to PATH
export PATH="$HOME/.cargo/bin:$PATH"
source ~/.bashrc

# Verify
uv --version
uvx --version
```

### Linux (Fedora/RHEL/CentOS)

```bash
# Install Node.js
curl -fsSL https://rpm.nodesource.com/setup_lts.x | sudo bash -
sudo dnf install -y nodejs

# Verify installation
node --version
npx --version

# Install Python
sudo dnf install -y python3 python3-pip

# Install uv
curl -LsSf https://astral.sh/uv/install.sh | sh

# Add to PATH
export PATH="$HOME/.cargo/bin:$PATH"
source ~/.bashrc

# Verify
uv --version
uvx --version
```

## Testing MCP Server Connections

After installing dependencies, you can test that the MCP servers work correctly.

### Test Filesystem MCP Server

```bash
# This will download and run the filesystem server temporarily
npx -y @modelcontextprotocol/server-filesystem /tmp

# Expected: Server starts and waits for input (press Ctrl+C to exit)
```

### Test Git MCP Server

```bash
# This will download and run the git server temporarily
uvx mcp-server-git --repository .

# Expected: Server starts and waits for input (press Ctrl+C to exit)
```

### Test GitHub MCP Server

```bash
# Set your GitHub token first
export GITHUB_PERSONAL_ACCESS_TOKEN="your_token_here"

# Run the GitHub server (using Docker)
docker run -i --rm -e GITHUB_PERSONAL_ACCESS_TOKEN ghcr.io/github/github-mcp-server

# Expected: Server starts and waits for input (press Ctrl+C to exit)
# Note: First run will download the Docker image (~50MB)
```

## Configuration

### GitHub Token

The GitHub MCP server requires a personal access token. Create one at:
https://github.com/settings/tokens

Required scopes:
- `repo` - Full control of private repositories
- `read:org` - Read org and team membership
- `read:user` - Read user profile data

Add the token to your `.env` file:

```bash
GITHUB_TOKEN=ghp_yourTokenHere
```

**Important Note:** The GitHub MCP server has moved from the deprecated npm package `@modelcontextprotocol/server-github` to the official GitHub-maintained server at `github/github-mcp-server`. The new server is written in Go and distributed via Docker container at `ghcr.io/github/github-mcp-server`. See [GitHub's official MCP server documentation](https://github.com/github/github-mcp-server) for details.

### Workspace Path

The Filesystem MCP server uses the workspace path from your configuration. Ensure your `.env` file has:

```bash
WORKSPACE_PATH=/path/to/your/repository
```

## Troubleshooting

### npx command not found

**Problem**: `npx: command not found` when testing MCP servers

**Solution**: Ensure Node.js is properly installed and in your PATH:
```bash
which node
which npx
```

If not found, reinstall Node.js or add it to your PATH.

### uvx command not found

**Problem**: `uvx: command not found` when testing MCP servers

**Solution**: Ensure uv is installed and in your PATH:
```bash
# Check if uv is installed
ls ~/.cargo/bin/uvx

# Add to PATH if needed
export PATH="$HOME/.cargo/bin:$PATH"

# Make permanent by adding to ~/.bashrc or ~/.zshrc
echo 'export PATH="$HOME/.cargo/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

### Permission Denied (Linux/macOS)

**Problem**: Permission errors when running installation scripts

**Solution**: Ensure you have proper permissions or use sudo where appropriate:
```bash
# For system-wide Node.js installation
sudo apt-get install -y nodejs

# For user-specific uv installation (recommended)
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### MCP Server Initialization Failures

**Problem**: Orchestrator logs show MCP initialization warnings

**Solution**:
1. Check that all dependencies are installed (see Testing section above)
2. Verify your PATH includes both Node.js and uv binaries
3. Check the Orchestrator logs for specific error messages
4. The application will continue to run using legacy file/git operations if MCP servers fail to initialize

### Windows Long Path Issues

**Problem**: File paths too long on Windows

**Solution**: Enable long path support:
1. Run as Administrator:
   ```powershell
   New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
   ```
2. Restart your computer

## Verification

To verify everything is set up correctly, run the Orchestrator in a test environment:

```bash
# Build the project
dotnet build src/Orchestrator.App/Orchestrator.App.csproj

# Run the application
dotnet run --project src/Orchestrator.App/Orchestrator.App.csproj
```

Look for these log messages indicating successful MCP initialization:

```
[MCP] Initializing MCP clients...
[MCP] Connecting to Filesystem MCP server...
[MCP] Filesystem server connected. Tools: <number>
[MCP] Connecting to Git MCP server...
[MCP] Git server connected. Tools: <number>
[MCP] Connecting to GitHub MCP server...
[MCP] GitHub server connected. Tools: <number>
[MCP] Initialization complete. Total tools available: <number>
```

If you see warnings instead, review the troubleshooting section above.

## References

- [Node.js Download](https://nodejs.org/)
- [uv Documentation](https://docs.astral.sh/uv/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
