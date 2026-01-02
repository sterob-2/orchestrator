using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Orchestrator.App;

/// <summary>
/// Manages MCP (Model Context Protocol) client connections and provides AI tools
/// for filesystem, git, and GitHub operations.
/// </summary>
internal class McpClientManager : IAsyncDisposable
{
    private readonly List<McpClient> _clients = new();
    private readonly List<McpClientTool> _tools = new();
    private readonly Dictionary<string, string> _toolToServer = new();
    private bool _initialized = false;

    /// <summary>
    /// Gets all tools available across all MCP servers.
    /// </summary>
    public virtual IReadOnlyList<McpClientTool> Tools => _tools.AsReadOnly();

    /// <summary>
    /// Gets tools filtered by server name.
    /// </summary>
    public virtual IEnumerable<McpClientTool> GetToolsByServer(string serverName)
    {
        return _tools.Where(tool => _toolToServer.TryGetValue(tool.Name, out var server) && server == serverName);
    }

    /// <summary>
    /// Invokes an MCP tool by name with the specified arguments.
    /// </summary>
    /// <param name="toolName">The name of the tool to invoke</param>
    /// <param name="arguments">Tool arguments as a dictionary</param>
    /// <returns>The tool invocation result as a string</returns>
    public virtual async Task<string> CallToolAsync(string toolName, IDictionary<string, object?> arguments)
    {
        // Find the tool
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        if (tool == null)
        {
            throw new InvalidOperationException($"Tool '{toolName}' not found in MCP tools");
        }

        // Find which client owns this tool
        if (!_toolToServer.TryGetValue(toolName, out var serverName))
        {
            throw new InvalidOperationException($"Server for tool '{toolName}' not found");
        }

        // Find the client for this server
        var client = _clients.FirstOrDefault(c => c.ServerInfo?.Name?.Contains(serverName, StringComparison.OrdinalIgnoreCase) ?? false);
        if (client == null)
        {
            throw new InvalidOperationException($"Client for server '{serverName}' not found");
        }

        // Invoke the tool - convert to IReadOnlyDictionary
        var readOnlyArgs = arguments as IReadOnlyDictionary<string, object?>
            ?? new Dictionary<string, object?>(arguments);
        var result = await client.CallToolAsync(toolName, readOnlyArgs);

        // Extract text from result
        var textBlocks = result.Content.OfType<TextContentBlock>();
        return string.Join("\n", textBlocks.Select(block => block.Text));
    }

    /// <summary>
    /// Initializes all MCP clients and retrieves their tools.
    /// </summary>
    public async Task InitializeAsync(OrchestratorConfig config)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("MCP clients already initialized.");
        }

        try
        {
            Logger.WriteLine("[MCP] Initializing MCP clients...");

            // Initialize Filesystem MCP server
            if (!string.IsNullOrWhiteSpace(config.WorkspaceHostPath))
            {
                await InitializeFilesystemServerAsync(config.WorkspaceHostPath);
            }

            // Initialize Git MCP server
            if (!string.IsNullOrWhiteSpace(config.WorkspaceHostPath))
            {
                await InitializeGitServerAsync(config.WorkspaceHostPath);
            }

            // Initialize GitHub MCP server
            if (!string.IsNullOrWhiteSpace(config.GitHubToken))
            {
                await InitializeGitHubServerAsync(config.GitHubToken);
            }

            _initialized = true;
            Logger.WriteLine($"[MCP] Initialization complete. Total tools available: {_tools.Count}");

            // Log available tools for debugging
            foreach (var tool in _tools)
            {
                var serverName = _toolToServer.TryGetValue(tool.Name, out var server) ? server : "unknown";
                Logger.WriteLine($"[MCP]   - {tool.Name} ({serverName})");
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"[MCP] Initialization failed: {ex.Message}");
            await DisposeAsync();
            throw;
        }
    }

    private async Task InitializeFilesystemServerAsync(string workspaceHostPath)
    {
        try
        {
            Logger.WriteLine("[MCP] Connecting to Filesystem MCP server...");

            // Use Docker to run the Filesystem MCP server
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "FilesystemServer",
                Command = "docker",
                Arguments = [
                    "run",
                    "-i",
                    "--rm",
                    "-v",
                    $"{workspaceHostPath}:/workspace",
                    "-w",
                    "/workspace",
                    "node:lts-alpine",
                    "sh",
                    "-c",
                    "npx -y @modelcontextprotocol/server-filesystem /workspace"
                ]
            });

            var client = await McpClient.CreateAsync(transport);

            _clients.Add(client);

            var tools = await client.ListToolsAsync().ConfigureAwait(false);
            foreach (var tool in tools)
            {
                _tools.Add(tool);
                _toolToServer[tool.Name] = "filesystem";
            }

            Logger.WriteLine($"[MCP] Filesystem server connected. Tools: {tools.Count}");
        }
        catch (InvalidOperationException ex)
        {
            Logger.WriteLine($"[MCP] Warning: Filesystem server configuration error: {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            Logger.WriteLine($"[MCP] Warning: Failed to start Filesystem server: {ex.Message}");
            Logger.WriteLine($"[MCP] Ensure Docker is installed and workspace path is accessible.");
        }
        catch (TimeoutException ex)
        {
            Logger.WriteLine($"[MCP] Warning: Filesystem server connection timeout: {ex.Message}");
        }
    }

    private async Task InitializeGitServerAsync(string repositoryHostPath)
    {
        try
        {
            Logger.WriteLine("[MCP] Connecting to Git MCP server...");

            // Use Docker to run the Git MCP server
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "GitServer",
                Command = "docker",
                Arguments = [
                    "run",
                    "-i",
                    "--rm",
                    "-v",
                    $"{repositoryHostPath}:/workspace",
                    "-w",
                    "/workspace",
                    "python:3.12-alpine",
                    "sh",
                    "-c",
                    "apk add --no-cache git && pip install --no-cache-dir uv > /dev/null 2>&1 && uvx mcp-server-git --repository /workspace"
                ]
            });

            var client = await McpClient.CreateAsync(transport);

            _clients.Add(client);

            var tools = await client.ListToolsAsync().ConfigureAwait(false);
            foreach (var tool in tools)
            {
                _tools.Add(tool);
                _toolToServer[tool.Name] = "git";
            }

            Logger.WriteLine($"[MCP] Git server connected. Tools: {tools.Count}");
        }
        catch (InvalidOperationException ex)
        {
            Logger.WriteLine($"[MCP] Warning: Git server configuration error: {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            Logger.WriteLine($"[MCP] Warning: Failed to start Git server: {ex.Message}");
            Logger.WriteLine($"[MCP] Ensure Docker is installed and repository path is accessible.");
        }
        catch (TimeoutException ex)
        {
            Logger.WriteLine($"[MCP] Warning: Git server connection timeout: {ex.Message}");
        }
    }

    private async Task InitializeGitHubServerAsync(string githubToken)
    {
        try
        {
            Logger.WriteLine("[MCP] Connecting to GitHub MCP server...");

            // Set GitHub token as environment variable for the Docker container
            var previousToken = Environment.GetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN");
            Environment.SetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", githubToken);

            try
            {
                // Use Docker to run the official GitHub MCP server
                var transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "GitHubServer",
                    Command = "docker",
                    Arguments = [
                        "run",
                        "-i",
                        "--rm",
                        "-e",
                        "GITHUB_PERSONAL_ACCESS_TOKEN",
                        "ghcr.io/github/github-mcp-server"
                    ]
                });

                var client = await McpClient.CreateAsync(transport);

                _clients.Add(client);

                var tools = await client.ListToolsAsync().ConfigureAwait(false);
                foreach (var tool in tools)
                {
                    _tools.Add(tool);
                    _toolToServer[tool.Name] = "github";
                }

                Logger.WriteLine($"[MCP] GitHub server connected. Tools: {tools.Count}");
            }
            finally
            {
                // Restore previous token value
                Environment.SetEnvironmentVariable("GITHUB_PERSONAL_ACCESS_TOKEN", previousToken);
            }
        }
        catch (InvalidOperationException ex)
        {
            Logger.WriteLine($"[MCP] Warning: GitHub server configuration error: {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            Logger.WriteLine($"[MCP] Warning: Failed to start GitHub server: {ex.Message}");
            Logger.WriteLine($"[MCP] Ensure Docker is installed, running, and GitHub token is valid.");
        }
        catch (TimeoutException ex)
        {
            Logger.WriteLine($"[MCP] Warning: GitHub server connection timeout: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Logger.WriteLine("[MCP] Disposing MCP clients...");

        foreach (var client in _clients)
        {
            try
            {
                await client.DisposeAsync();
            }
            catch (InvalidOperationException ex)
            {
                Logger.WriteLine($"[MCP] Error disposing client: {ex.Message}");
            }
            catch (System.IO.IOException ex)
            {
                Logger.WriteLine($"[MCP] I/O error disposing client: {ex.Message}");
            }
        }

        _clients.Clear();
        _tools.Clear();
        _initialized = false;

        Logger.WriteLine("[MCP] MCP clients disposed.");
    }
}
