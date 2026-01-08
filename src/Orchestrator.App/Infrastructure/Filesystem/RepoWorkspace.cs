using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Infrastructure.Filesystem;

internal sealed class RepoWorkspace : IRepoWorkspace
{
    public string Root { get; }

    public RepoWorkspace(string root)
    {
        Root = root;
    }

    public string ResolvePath(string relativePath)
    {
        var rootFull = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return rootFull;
        }

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(rootFull, normalized));
        var rootPrefix = rootFull + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(combined, rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path escapes workspace root: {relativePath}");
        }

        return combined;
    }

    public bool Exists(string relativePath) => File.Exists(ResolvePath(relativePath));

    public string ReadAllText(string relativePath)
    {
        return File.ReadAllText(ResolvePath(relativePath));
    }

    public void WriteAllText(string relativePath, string content)
    {
        var fullPath = ResolvePath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }

    public IEnumerable<string> ListFiles(string relativeRoot, string searchPattern, int max)
    {
        var root = ResolvePath(relativeRoot);
        if (!Directory.Exists(root))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
            .Select(path => path.Replace(Root + Path.DirectorySeparatorChar, ""))
            .Take(max)
            .ToList();
    }

    public string ReadOrTemplate(string relativePath, string templatePath, Dictionary<string, string> tokens)
    {
        var content = Exists(relativePath)
            ? ReadAllText(relativePath)
            : ReadAllText(templatePath);

        foreach (var pair in tokens)
        {
            content = content.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
        }

        return content;
    }
}
