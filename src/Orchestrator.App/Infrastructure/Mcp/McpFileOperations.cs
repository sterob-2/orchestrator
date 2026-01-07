using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestrator.App.Infrastructure.Mcp;

/// <summary>
/// Provides file operations using MCP filesystem tools.
/// This is a hybrid approach that keeps agent orchestration while using MCP tools.
/// </summary>
public sealed class McpFileOperations
{
    private readonly McpClientManager _mcpManager;

    public McpFileOperations(McpClientManager mcpManager)
    {
        _mcpManager = mcpManager ?? throw new ArgumentNullException(nameof(mcpManager));
    }

    /// <summary>
    /// Reads the entire contents of a file using MCP read_file tool.
    /// </summary>
    public async Task<string> ReadAllTextAsync(string path)
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = path
        };

        var result = await _mcpManager.CallToolAsync("read_file", args);
        return result;
    }

    /// <summary>
    /// Writes content to a file using MCP write_file tool.
    /// </summary>
    public async Task WriteAllTextAsync(string path, string content)
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = path,
            ["content"] = content
        };

        await _mcpManager.CallToolAsync("write_file", args);
    }

    /// <summary>
    /// Checks if a file exists using MCP get_file_info tool.
    /// </summary>
    public async Task<bool> ExistsAsync(string path)
    {
        try
        {
            var args = new Dictionary<string, object?>
            {
                ["path"] = path
            };

            await _mcpManager.CallToolAsync("get_file_info", args);
            return true;
        }
        catch (ArgumentException)
        {
            // Treat invalid path or similar argument issues as "file does not exist"
            return false;
        }
        catch (InvalidOperationException)
        {
            // Treat expected operational failures (e.g., not found) as "file does not exist"
            return false;
        }
    }

    /// <summary>
    /// Lists files in a directory using MCP list_directory tool.
    /// </summary>
    public async Task<string[]> ListFilesAsync(string path, string pattern = "*")
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = path
        };

        var result = await _mcpManager.CallToolAsync("list_directory", args);

        // Parse the result to extract file names
        // For now, return the raw result split by newlines
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines;
    }
}
