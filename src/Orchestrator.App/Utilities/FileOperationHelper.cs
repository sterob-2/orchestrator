using System;
using System.IO;
using System.Threading.Tasks;

namespace Orchestrator.App.Utilities;

/// <summary>
/// Helper for file operations using direct filesystem access via IRepoWorkspace.
/// Simplified to use System.IO directly (no MCP fallback needed).
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
    /// Checks if a file exists.
    /// </summary>
    public static Task<bool> ExistsAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);
        return Task.FromResult(ctx.Workspace.Exists(path));
    }

    /// <summary>
    /// Reads file content.
    /// </summary>
    public static Task<string> ReadAllTextAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);
        return Task.FromResult(ctx.Workspace.ReadAllText(path));
    }

    /// <summary>
    /// Writes file content.
    /// </summary>
    public static Task WriteAllTextAsync(WorkContext ctx, string path, string content)
    {
        EnsureSafeRelativePath(path);

        Logger.Debug($"[FileOp] Writing {content.Length} bytes to: {path}");
        ctx.Workspace.WriteAllText(path, content);
        Logger.Debug($"[FileOp] Write completed for: {path}");

        return Task.CompletedTask;
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
            Logger.Debug($"[FileOp] File not found: {path} - {ex.Message}");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Debug($"[FileOp] Access denied reading file: {path} - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes a file.
    /// </summary>
    public static Task DeleteAsync(WorkContext ctx, string path)
    {
        EnsureSafeRelativePath(path);

        var fullPath = ctx.Workspace.ResolvePath(path);
        if (File.Exists(fullPath))
        {
            Logger.Debug($"[FileOp] Deleting file: {path}");
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
