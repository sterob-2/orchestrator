using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Parsing;

/// <summary>
/// Parses the touch list table from the technical specification.
/// </summary>
public class TouchListParser
{
    public List<TouchListEntry> Parse(string content)
    {
        var result = new List<TouchListEntry>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var tableStarted = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith('|') || !trimmedLine.EndsWith('|'))
            {
                if (tableStarted) break; // End of table
                continue;
            }

            // Skip separator line
            if (trimmedLine.Contains("---"))
            {
                tableStarted = true;
                continue;
            }

            // Skip header line (check if it looks like a header)
            if (!tableStarted)
            {
                 // Heuristic: If we haven't seen the separator yet, this might be the header.
                 // We'll confirm it's a table when we see the separator line.
                 // For now, we rely on the loop order: Header -> Separator -> Data
                 continue;
            }

            var parts = trimmedLine.Split('|')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p)) // The split on | with leading/trailing | creates empty first/last entries
                .ToList();
            
            // Handle split resulting in empty entries at start/end due to pipe guards
             var cleanParts = trimmedLine.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            if (cleanParts.Count < 2)
            {
                continue;
            }

            if (Enum.TryParse<TouchOperation>(cleanParts[0], true, out var operation))
            {
                var path = cleanParts[1];
                var notes = cleanParts.Count > 2 ? cleanParts[2] : null;
                result.Add(new TouchListEntry(operation, path, notes));
            }
        }

        return result;
    }
}