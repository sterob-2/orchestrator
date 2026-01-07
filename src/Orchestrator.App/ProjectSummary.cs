using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App;

internal static class ProjectSummaryFormatter
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
