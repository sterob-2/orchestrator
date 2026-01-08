using System;
using System.Collections.Generic;

namespace Orchestrator.App.Parsing;

internal static class WorkItemParsers
{
    public static ProjectReference? TryParseProjectReference(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var userRef = TryParseProjectReferenceFromMarker(body, "https://github.com/users/", ProjectOwnerType.User);
        if (userRef != null)
            return userRef;

        var orgRef = TryParseProjectReferenceFromMarker(body, "https://github.com/orgs/", ProjectOwnerType.Organization);
        if (orgRef != null)
            return orgRef;

        return null;
    }

    public static int? TryParseIssueNumber(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var marker = "Issue #";
        var idx = body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var start = idx + marker.Length;
        var end = start;
        while (end < body.Length && char.IsDigit(body[end]))
            end++;

        if (end == start)
            return null;
        if (int.TryParse(body[start..end], out var number))
            return number;
        return null;
    }

    public static List<string> TryParseAcceptanceCriteria(string body)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(body))
            return results;

        var lines = body.Split('\n');
        var inSection = false;
        foreach (var line in lines.Select(raw => raw.Trim()))
        {
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
        if (string.IsNullOrWhiteSpace(content))
            return content;

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
        if (string.IsNullOrWhiteSpace(content))
            return results;

        var lines = content.Split('\n');
        var inFiles = false;
        foreach (var line in lines.Select(raw => raw.Trim()))
        {
            if (IsFilesHeader(line))
            {
                inFiles = true;
                continue;
            }

            if (!inFiles)
            {
                continue;
            }

            if (IsSectionHeader(line))
            {
                break;
            }

            if (TryParseFileItem(line, out var path))
            {
                results.Add(path);
            }
        }

        return results;
    }

    public static string TryParseSection(string content, string header)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "";

        var start = content.IndexOf(header, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return "";

        var sectionStart = start + header.Length;
        var nextHeader = content.IndexOf("\n## ", sectionStart, StringComparison.OrdinalIgnoreCase);
        var end = nextHeader >= 0 ? nextHeader : content.Length;
        return content.Substring(sectionStart, end - sectionStart).Trim();
    }

    public static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        if (path.StartsWith("/") || path.StartsWith("\\") || path.Contains(".."))
            return false;
        return true;
    }

    private static ProjectReference? TryParseProjectReferenceFromMarker(string body, string marker, ProjectOwnerType ownerType)
    {
        var idx = body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var ownerStart = idx + marker.Length;
        var ownerEnd = ownerStart;
        while (ownerEnd < body.Length && body[ownerEnd] != '/' && body[ownerEnd] != '?' && body[ownerEnd] != '#')
        {
            ownerEnd++;
        }

        if (ownerEnd == ownerStart)
            return null;
        var owner = body[ownerStart..ownerEnd];

        if (body.IndexOf("/projects/", ownerEnd, StringComparison.OrdinalIgnoreCase) != ownerEnd)
            return null;

        var numberStart = ownerEnd + "/projects/".Length;
        var numberEnd = numberStart;
        while (numberEnd < body.Length && char.IsDigit(body[numberEnd]))
            numberEnd++;

        if (numberEnd == numberStart)
            return null;
        if (int.TryParse(body[numberStart..numberEnd], out var number))
        {
            return new ProjectReference(owner, number, ownerType);
        }

        return null;
    }

    private static bool IsFilesHeader(string line)
    {
        return line.StartsWith("## Files", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSectionHeader(string line)
    {
        return line.StartsWith("## ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseFileItem(string line, out string path)
    {
        path = string.Empty;
        if (!line.StartsWith("- "))
            return false;

        var item = line[2..].Trim();
        if (item.Length == 0)
            return false;

        var splitIndex = item.IndexOfAny(new[] { ' ', '(' });
        path = splitIndex >= 0 ? item[..splitIndex] : item;
        return !string.IsNullOrWhiteSpace(path);
    }
}
