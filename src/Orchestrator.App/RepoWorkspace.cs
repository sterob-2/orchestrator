using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Orchestrator.App;

internal sealed class RepoWorkspace
{
    public string Root { get; }

    public RepoWorkspace(string root)
    {
        Root = root;
    }

    public string ResolvePath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(Root, normalized);
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
