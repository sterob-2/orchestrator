using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Parsing;

/// <summary>
/// Parses the touch list table from the technical specification.
/// </summary>
public static class TouchListParser
{
    private static readonly string[] NewLineSeparators = ["\r\n", "\r", "\n"];

    public static List<TouchListEntry> Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<TouchListEntry>();
        }

        var lines = content.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .ToList();

        var separatorIndex = lines.FindIndex(l => l.StartsWith('|') && l.EndsWith('|') && l.Contains("---"));

        if (separatorIndex < 1)
        {
            return new List<TouchListEntry>();
        }

        // Data starts after separator
        return lines.Skip(separatorIndex + 1)
            .TakeWhile(l => l.StartsWith('|') && l.EndsWith('|'))
            .Select(ParseLine)
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
    }

    private static TouchListEntry? ParseLine(string line)
    {
        var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToList();

        if (parts.Count < 2)
        {
            return null;
        }

        if (Enum.TryParse<TouchOperation>(parts[0], true, out var operation))
        {
            var path = parts[1];
            var notes = parts.Count > 2 ? parts[2] : null;
            return new TouchListEntry(operation, path, notes);
        }

        return null;
    }
}
