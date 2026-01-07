using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Parsing;

public class SpecParser
{
    private readonly TouchListParser _touchListParser;

    public SpecParser()
    {
        _touchListParser = new TouchListParser();
    }

    public ParsedSpec Parse(string content)
    {
        var sections = ParseSections(content);

        var goal = GetSection(sections, "Ziel", "Goal");
        var nonGoals = GetSection(sections, "Nicht-Ziele", "Non-Goals");
        var components = ParseList(GetSection(sections, "Komponenten", "Components"));
        var touchListContent = GetSection(sections, "Touch List");
        var touchList = _touchListParser.Parse(touchListContent);
        var interfaces = ParseCodeBlocks(GetSection(sections, "Interfaces"));
        var scenarios = ParseScenarios(GetSection(sections, "Szenarien", "Scenarios"));
        var sequence = ParseList(GetSection(sections, "Sequenz", "Sequence"));
        var testMatrix = ParseTableRows(GetSection(sections, "Testmatrix", "Test Matrix", "Testmatrix"));

        return new ParsedSpec(
            Goal: goal,
            NonGoals: nonGoals,
            Components: components,
            TouchList: touchList,
            Interfaces: interfaces,
            Scenarios: scenarios,
            Sequence: sequence,
            TestMatrix: testMatrix,
            Sections: sections
        );
    }

    private Dictionary<string, string> ParseSections(string content)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(content)) return sections;

        // Regex to find headers like "## Header"
        // We look for "## " at the start of a line
        var regex = new Regex(@"^##\s+(.+)$", RegexOptions.Multiline);
        var matches = regex.Matches(content);

        if (matches.Count == 0) return sections;

        for (int i = 0; i < matches.Count; i++)
        {
            var currentMatch = matches[i];
            var header = currentMatch.Groups[1].Value.Trim();
            var start = currentMatch.Index + currentMatch.Length;
            var end = (i + 1 < matches.Count) ? matches[i + 1].Index : content.Length;
            
            var body = content.Substring(start, end - start).Trim();
            sections[header] = body;
        }

        return sections;
    }

    private string GetSection(Dictionary<string, string> sections, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (sections.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return string.Empty;
    }

    private List<string> ParseList(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new List<string>();
        
        // Matches lines starting with - or * or 1.
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var items = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || Regex.IsMatch(trimmed, @"^\d+\."))
            {
                // Remove the bullet/number
                var clean = Regex.Replace(trimmed, @"^([-*]|\d+\.)\s+", "");
                items.Add(clean);
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                // Also accept non-bulleted lines if it's a simple list section
                 items.Add(trimmed);
            }
        }
        return items;
    }

    private List<string> ParseCodeBlocks(string content)
    {
        var blocks = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return blocks;

        var regex = new Regex(@"```.*?\r?\n(.*?)\r?\n```", RegexOptions.Singleline);
        foreach (Match match in regex.Matches(content))
        {
            blocks.Add(match.Groups[1].Value.Trim());
        }

        // If no code blocks found, return the whole content split by lines? 
        // Or just the whole content as one block if it's not empty?
        // Requirement says "Interfaces" section. It might contain code blocks.
        if (blocks.Count == 0 && !string.IsNullOrWhiteSpace(content))
        {
             blocks.Add(content);
        }

        return blocks;
    }

    private List<string> ParseScenarios(string content)
    {
        var scenarios = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return scenarios;

        // Split by "Scenario:" but keep the delimiter
        // A simple way is to find indices of "Scenario:"
        // Note: This is a simple parser, might be confused by "Scenario:" inside comments or strings.
        
        var regex = new Regex(@"(^|\n)Scenario:", RegexOptions.Multiline);
        var matches = regex.Matches(content);
        
        if (matches.Count == 0) return scenarios;

        for (int i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            // If the match starts with \n, we want to include the "Scenario:" part which is at match.Index + 1
            if (content[start] == '\n') start++;

            var end = (i + 1 < matches.Count) ? matches[i + 1].Index : content.Length;
            // Adjust end if next match starts with \n
             if (i + 1 < matches.Count && content[end] == '\n') end++; // actually we split AT the newline before Scenario

            var body = content.Substring(start, end - start).Trim();
            if (!string.IsNullOrWhiteSpace(body))
            {
                scenarios.Add(body);
            }
        }

        return scenarios;
    }

    private List<string> ParseTableRows(string content)
    {
        // For Testmatrix, we just want the rows (maybe excluding header)
        // This returns raw rows for now.
        if (string.IsNullOrWhiteSpace(content)) return new List<string>();

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var rows = new List<string>();
        
        bool tableStarted = false;
        foreach (var line in lines)
        {
             var trimmed = line.Trim();
             if (!trimmed.StartsWith("|")) continue;
             
             if (trimmed.Contains("---"))
             {
                 tableStarted = true;
                 continue;
             }

             if (tableStarted)
             {
                 rows.Add(trimmed);
             }
        }

        return rows;
    }
}
