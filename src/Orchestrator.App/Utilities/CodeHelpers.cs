using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orchestrator.App.Utilities;

public static class CodeHelpers
{
    internal static List<string> ValidateSpecFiles(IEnumerable<string> files, RepoWorkspace workspace)
    {
        return files.Where(file =>
        {
            if (!WorkItemParsers.IsSafeRelativePath(file))
                return true;
            if (!IsAllowedPath(file))
                return true;
            if (workspace.Exists(file))
                return false;

            return !IsAllowedExtension(Path.GetExtension(file));
        }).ToList();
    }

    internal static bool IsAllowedPath(string path)
    {
        var allowedPrefixes = new[]
        {
            "orchestrator/src/Orchestrator.App/",
            "orchestrator/tests/",
            "Assets/Scripts/",
            "Assets/Tests/"
        };

        return allowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ||
               string.Equals(path, "orchestrator/README.md", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsAllowedExtension(string extension)
    {
        var allowed = new[] { ".cs", ".md", ".json", ".yml", ".yaml" };
        return allowed.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    internal static bool IsTestFile(string path)
    {
        return path.Contains("/Tests/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("\\Tests\\", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase);
    }

    internal static string StripCodeFence(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var end = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (end <= 0)
        {
            return trimmed;
        }

        var inner = trimmed[3..end];
        var firstNewline = inner.IndexOf('\n');
        if (firstNewline >= 0)
        {
            inner = inner[(firstNewline + 1)..];
        }

        return inner.Trim();
    }

    internal static string Truncate(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
        {
            return content;
        }

        return content[..maxLength] + "\n...truncated...";
    }
}
