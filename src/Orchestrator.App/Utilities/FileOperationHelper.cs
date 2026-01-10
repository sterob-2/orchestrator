using System;
using System.IO;
using System.Threading.Tasks;

namespace Orchestrator.App.Utilities;

/// <summary>
/// Helper for file operations that uses MCP if available, otherwise falls back to Workspace.
/// </summary>
internal static class FileOperationHelper
{
    private static void EnsureSafeRelativePath(string path)
    {
        if (!WorkItemParsers.IsSafeRelativePath(path))
        {
            throw new ArgumentException($"Invalid path: {path}", nameof(path));
        }
    }

    /// <summary>
    /// Checks if a file exists using MCP if available, otherwise Workspace.
    /// </summary>
    public static async Task<bool> ExistsAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);
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
        EnsureSafeRelativePath(path);
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
        EnsureSafeRelativePath(path);
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
        try
        {
            if (!await ExistsAsync(ctx, path))
            {
                Logger.Debug($"[FileOp] File does not exist: {path}");
                return null;
            }

            Logger.Debug($"[FileOp] Reading file: {path}");
            return await ReadAllTextAsync(ctx, path);
        }
        catch (FileNotFoundException ex)
        {
            // File was deleted between ExistsAsync and ReadAllTextAsync, or permission issue
            Logger.Debug($"[FileOp] File not found during read (race condition or permissions): {path} - {ex.Message}");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied
            Logger.Debug($"[FileOp] Access denied reading file: {path} - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes a file using MCP if available, otherwise Workspace.
    /// </summary>
    public static async Task DeleteAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);
        if (ctx.McpFiles != null)
        {
            await ctx.McpFiles.DeleteAsync(path);
        }

        var fullPath = ctx.Workspace.ResolvePath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
