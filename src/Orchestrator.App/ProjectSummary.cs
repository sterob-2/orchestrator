using System;
using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.App;

public sealed record ProjectItem(string Title, int? IssueNumber, string? Url, string Status);

public enum ProjectOwnerType
{
    User,
    Organization
}

public sealed record ProjectSnapshot(string Owner, int Number, ProjectOwnerType OwnerType, string Title, IReadOnlyList<ProjectItem> Items);

public sealed record ProjectMetadata(string ProjectId, string StatusFieldId, IReadOnlyDictionary<string, string> StatusOptions, IReadOnlyList<ProjectItemRef> Items);

public sealed record ProjectItemRef(string ItemId, int IssueNumber);

public sealed record ProjectReference(string Owner, int Number, ProjectOwnerType OwnerType);

public static class ProjectSummaryFormatter
{
    public static string Format(ProjectSnapshot snapshot)
    {
        var grouped = snapshot.Items
            .GroupBy(item => string.IsNullOrWhiteSpace(item.Status) ? "Unknown" : item.Status)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lines = new List<string>
        {
            $"Project: {snapshot.Title} ({snapshot.Owner} #{snapshot.Number})",
            "",
            "Status summary:"
        };

        foreach (var group in grouped)
        {
            lines.Add($"- {group.Key}: {group.Count()}");
        }

        lines.Add("");
        lines.Add("Top-3 next steps:");

        var candidates = snapshot.Items
            .Where(item => string.Equals(item.Status, "Ready", StringComparison.OrdinalIgnoreCase))
            .Concat(snapshot.Items.Where(item => string.Equals(item.Status, "Backlog", StringComparison.OrdinalIgnoreCase)))
            .Take(3)
            .ToList();

        if (candidates.Count == 0)
        {
            lines.Add("- No Ready/Backlog items found.");
        }
        else
        {
            foreach (var item in candidates)
            {
                var link = item.Url ?? "(no link)";
                lines.Add($"- {item.Title} ({link})");
            }
        }

        lines.Add("");
        var scope = snapshot.OwnerType == ProjectOwnerType.Organization ? "orgs" : "users";
        lines.Add($"Project link: https://github.com/{scope}/{snapshot.Owner}/projects/{snapshot.Number}");

        return string.Join('\n', lines);
    }
}

internal static class WorkItemParsers
{
    public static ProjectReference? TryParseProjectReference(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        var userRef = TryParseProjectReferenceFromMarker(body, "https://github.com/users/", ProjectOwnerType.User);
        if (userRef != null) return userRef;

        var orgRef = TryParseProjectReferenceFromMarker(body, "https://github.com/orgs/", ProjectOwnerType.Organization);
        if (orgRef != null) return orgRef;

        return null;
    }

    public static int? TryParseIssueNumber(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        var marker = "Issue #";
        var idx = body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var start = idx + marker.Length;
        var end = start;
        while (end < body.Length && char.IsDigit(body[end])) end++;

        if (end == start) return null;
        if (int.TryParse(body[start..end], out var number)) return number;
        return null;
    }

    public static List<string> TryParseAcceptanceCriteria(string body)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(body)) return results;

        var lines = body.Split('\n');
        var inSection = false;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("Acceptance criteria", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Acceptance Criteria", StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                continue;
            }

            if (!inSection)
            {
                continue;
            }

            if (line.Length == 0)
            {
                break;
            }

            if (line.StartsWith("-") || line.StartsWith("*"))
            {
                var item = line.TrimStart('-', '*', ' ');
                if (item.Length > 0)
                {
                    results.Add(item);
                }
            }
        }

        return results;
    }

    public static string MarkAcceptanceCriteriaDone(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;

        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("- [ ]"))
            {
                lines[i] = line.Replace("- [ ]", "- [x]", StringComparison.Ordinal);
            }
        }

        return string.Join('\n', lines);
    }

    public static List<string> TryParseSpecFiles(string content)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return results;

        var lines = content.Split('\n');
        var inFiles = false;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("## Files", StringComparison.OrdinalIgnoreCase))
            {
                inFiles = true;
                continue;
            }

            if (!inFiles)
            {
                continue;
            }

            if (line.StartsWith("## ", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (!line.StartsWith("- "))
            {
                continue;
            }

            var item = line[2..].Trim();
            if (item.Length == 0)
            {
                continue;
            }

            var splitIndex = item.IndexOfAny(new[] { ' ', '(' });
            var path = splitIndex >= 0 ? item[..splitIndex] : item;
            if (!string.IsNullOrWhiteSpace(path))
            {
                results.Add(path);
            }
        }

        return results;
    }

    public static string TryParseSection(string content, string header)
    {
        if (string.IsNullOrWhiteSpace(content)) return "";

        var start = content.IndexOf(header, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return "";

        var sectionStart = start + header.Length;
        var nextHeader = content.IndexOf("\n## ", sectionStart, StringComparison.OrdinalIgnoreCase);
        var end = nextHeader >= 0 ? nextHeader : content.Length;
        return content.Substring(sectionStart, end - sectionStart).Trim();
    }

    public static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (path.StartsWith("/") || path.StartsWith("\\") || path.Contains("..")) return false;
        return true;
    }

    private static ProjectReference? TryParseProjectReferenceFromMarker(string body, string marker, ProjectOwnerType ownerType)
    {
        var idx = body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var ownerStart = idx + marker.Length;
        var ownerEnd = ownerStart;
        while (ownerEnd < body.Length && body[ownerEnd] != '/' && body[ownerEnd] != '?' && body[ownerEnd] != '#')
        {
            ownerEnd++;
        }

        if (ownerEnd == ownerStart) return null;
        var owner = body[ownerStart..ownerEnd];

        if (body.IndexOf("/projects/", ownerEnd, StringComparison.OrdinalIgnoreCase) != ownerEnd)
            return null;

        var numberStart = ownerEnd + "/projects/".Length;
        var numberEnd = numberStart;
        while (numberEnd < body.Length && char.IsDigit(body[numberEnd])) numberEnd++;

        if (numberEnd == numberStart) return null;
        if (int.TryParse(body[numberStart..numberEnd], out var number))
        {
            return new ProjectReference(owner, number, ownerType);
        }

        return null;
    }
}
