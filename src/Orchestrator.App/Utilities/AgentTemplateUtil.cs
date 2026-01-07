using System;
using System.Collections.Generic;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Utilities;

internal static class AgentTemplateUtil
{
    public const string ReviewTemplateFallback =
        "# TechLead Review: Issue {{ISSUE_NUMBER}} - {{ISSUE_TITLE}}\n\n" +
        "STATUS: PENDING\n" +
        "UPDATED: {{UPDATED_AT_UTC}}\n\n" +
        "## Decision\n" +
        "APPROVED | CHANGES_REQUESTED\n\n" +
        "## Blockers\n" +
        "- None\n\n" +
        "## Notes\n" +
        "- Review notes here.\n";

    public static Dictionary<string, string> BuildTokens(WorkContext ctx)
    {
        return new Dictionary<string, string>
        {
            ["{{ISSUE_NUMBER}}"] = ctx.WorkItem.Number.ToString(),
            ["{{ISSUE_TITLE}}"] = ctx.WorkItem.Title,
            ["{{ISSUE_URL}}"] = ctx.WorkItem.Url,
            ["{{UPDATED_AT_UTC}}"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
        };
    }

    public static string RenderTemplate(
        IRepoWorkspace workspace,
        string templatePath,
        Dictionary<string, string> tokens,
        string fallbackTemplate)
    {
        var content = workspace.Exists(templatePath)
            ? workspace.ReadAllText(templatePath)
            : fallbackTemplate;

        foreach (var pair in tokens)
        {
            content = content.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
        }

        return content;
    }

    public static string UpdateStatus(string content, string status)
    {
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("STATUS:", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"STATUS: {status}";
            }
            if (lines[i].StartsWith("UPDATED:", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"UPDATED: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            }
        }

        return string.Join('\n', lines);
    }

    public static bool IsStatus(string content, string status)
    {
        foreach (var line in content.Split('\n'))
        {
            if (line.StartsWith("STATUS:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Contains(status, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    public static bool IsStatusComplete(string content)
    {
        return IsStatus(content, "COMPLETE");
    }

    public static string AppendQuestion(string content, string question)
    {
        if (content.Contains(question, StringComparison.OrdinalIgnoreCase))
        {
            return content;
        }

        var marker = "## Questions";
        var idx = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return content + $"\n\n## Questions\n- {question}\n";
        }

        return content.Insert(idx + marker.Length, $"\n- {question}");
    }

    public static string EnsureTemplateHeader(string spec, WorkContext ctx, string templatePath)
    {
        if (spec.StartsWith("# Spec: Issue", StringComparison.OrdinalIgnoreCase))
        {
            return spec;
        }

        var template = ctx.Workspace.ReadOrTemplate(templatePath, templatePath, BuildTokens(ctx));
        return template + "\n\n" + spec;
    }

    public static string ReplaceSection(string content, string header, string body)
    {
        var start = content.IndexOf(header, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return content + $"\n\n{header}\n{body.TrimEnd()}\n";
        }

        var sectionStart = start + header.Length;
        var nextHeader = content.IndexOf("\n## ", sectionStart, StringComparison.OrdinalIgnoreCase);
        var end = nextHeader >= 0 ? nextHeader : content.Length;
        var before = content[..start];
        var after = content[end..];
        return before + header + "\n" + body.TrimEnd() + "\n" + after.TrimStart('\n');
    }
}
