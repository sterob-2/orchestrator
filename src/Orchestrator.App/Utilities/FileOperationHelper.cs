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
    /// Falls back to Workspace if MCP fails.
    /// </summary>
    public static async Task<bool> ExistsAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);
        if (ctx.McpFiles != null)
        {
            try
            {
                return await ctx.McpFiles.ExistsAsync(path);
            }
            catch (Exception ex)
            {
                Logger.Debug($"[FileOp] MCP error checking existence of {path}, falling back to Workspace: {ex.Message}");
                // Fallback to Workspace if MCP has any error
            }
        }
        return ctx.Workspace.Exists(path);
    }

    /// <summary>
    /// Reads file content using MCP if available, otherwise Workspace.
    /// Falls back to Workspace if MCP fails.
    /// </summary>
    public static async Task<string> ReadAllTextAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);
        if (ctx.McpFiles != null)
        {
            try
            {
                return await ctx.McpFiles.ReadAllTextAsync(path);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Debug($"[FileOp] MCP failed to read {path}, falling back to Workspace: {ex.Message}");
                // Fallback to Workspace if MCP cannot find the file
            }
            catch (Exception ex)
            {
                Logger.Warning($"[FileOp] MCP error reading {path}, falling back to Workspace: {ex.Message}");
                // Fallback to Workspace if MCP has any other error
            }
        }
        return ctx.Workspace.ReadAllText(path);
    }

    /// <summary>
    /// Writes file content to both MCP and Workspace to ensure git operations see the changes.
    /// </summary>
    public static async Task WriteAllTextAsync(WorkContext ctx, string path, string content)
    {
        EnsureSafeRelativePath(path);

        // Always write to Workspace first (git operates on this)
        ctx.Workspace.WriteAllText(path, content);

        // Also try to write to MCP if available (keep MCP in sync)
        if (ctx.McpFiles != null)
        {
            try
            {
                await ctx.McpFiles.WriteAllTextAsync(path, content);
            }
            catch (Exception ex)
            {
                Logger.Debug($"[FileOp] MCP write failed for {path}, but Workspace write succeeded: {ex.Message}");
                // Continue - Workspace write succeeded which is what matters for git
            }
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
    /// Falls back to Workspace if MCP fails.
    /// </summary>
    public static async Task DeleteAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);
        if (ctx.McpFiles != null)
        {
            try
            {
                await ctx.McpFiles.DeleteAsync(path);
                return;
            }
            catch (Exception ex)
            {
                Logger.Warning($"[FileOp] MCP error deleting {path}, falling back to Workspace: {ex.Message}");
                // Fallback to Workspace if MCP has any error
            }
        }

        var fullPath = ctx.Workspace.ResolvePath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
