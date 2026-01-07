using System;
using System.Collections.Generic;
using System.IO;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Utilities;

internal static class AgentHelpers
{
    internal static List<string> ValidateSpecFiles(IEnumerable<string> files, IRepoWorkspace workspace)
    {
        var invalid = new List<string>();
        foreach (var file in files)
        {
            if (!WorkItemParsers.IsSafeRelativePath(file))
            {
                invalid.Add(file);
                continue;
            }

            if (!IsAllowedPath(file))
            {
                invalid.Add(file);
                continue;
            }

            if (workspace.Exists(file))
            {
                continue;
            }

            var extension = Path.GetExtension(file);
            if (!IsAllowedExtension(extension))
            {
                invalid.Add(file);
            }
        }

        return invalid;
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

        foreach (var prefix in allowedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return string.Equals(path, "orchestrator/README.md", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsAllowedExtension(string extension)
    {
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".yml", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase);
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
