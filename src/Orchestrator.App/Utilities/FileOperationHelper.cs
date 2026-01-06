using System.Threading.Tasks;

namespace Orchestrator.App.Utilities;

/// <summary>
/// Helper for file operations that uses MCP if available, otherwise falls back to Workspace.
/// </summary>
internal static class FileOperationHelper
{
    /// <summary>
    /// Checks if a file exists using MCP if available, otherwise Workspace.
    /// </summary>
    public static async Task<bool> ExistsAsync(WorkContext ctx, string path)
    {
        if (ctx.McpFiles != null)
        {
            return await ctx.McpFiles.ExistsAsync(path);
        }
        return ctx.Workspace.Exists(path);
    }

    /// <summary>
    /// Reads file content using MCP if available, otherwise Workspace.
    /// </summary>
    public static async Task<string> ReadAllTextAsync(WorkContext ctx, string path)
    {
        if (ctx.McpFiles != null)
        {
            return await ctx.McpFiles.ReadAllTextAsync(path);
        }
        return ctx.Workspace.ReadAllText(path);
    }

    /// <summary>
    /// Writes file content using MCP if available, otherwise Workspace.
    /// </summary>
    public static async Task WriteAllTextAsync(WorkContext ctx, string path, string content)
    {
        if (ctx.McpFiles != null)
        {
            await ctx.McpFiles.WriteAllTextAsync(path, content);
        }
        else
        {
            ctx.Workspace.WriteAllText(path, content);
        }
    }

    /// <summary>
    /// Reads file content if it exists, otherwise returns null.
    /// </summary>
    public static async Task<string?> ReadAllTextIfExistsAsync(WorkContext ctx, string path)
    {
        if (!await ExistsAsync(ctx, path))
        {
            return null;
        }
        return await ReadAllTextAsync(ctx, path);
    }
}
