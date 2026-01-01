namespace Orchestrator.App;

internal sealed record WorkItem(int Number, string Title, string Body, string Url, IReadOnlyList<string> Labels);

internal sealed record WorkContext(
    WorkItem WorkItem,
    OctokitGitHubClient GitHub,
    OrchestratorConfig Config,
    RepoWorkspace Workspace,
    RepoGit Repo,
    LlmClient Llm);

internal sealed record RepoFile(string Path, string Content, string Sha);

internal sealed record IssueComment(string Author, string Body);

internal sealed record PipelineResult(bool Success, string Summary, string PullRequestTitle, string PullRequestBody)
{
    public static PipelineResult Fail(string summary) => new(false, summary, "", "");
    public static PipelineResult Ok(string summary, string prTitle, string prBody) => new(true, summary, prTitle, prBody);
}

internal static class WorkItemBranch
{
    public static string BuildBranchName(WorkItem item)
    {
        var slug = Slugify(item.Title);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "work-item";
        }

        return $"agent/issue-{item.Number}-{slug}";
    }

    private static string Slugify(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        var buffer = new char[input.Length];
        var length = 0;
        var dash = false;

        foreach (var ch in input.ToLowerInvariant())
        {
            var normalized = char.IsLetterOrDigit(ch) ? ch : '-';
            if (normalized == '-')
            {
                if (dash) continue;
                dash = true;
            }
            else
            {
                dash = false;
            }

            buffer[length++] = normalized;
        }

        var result = new string(buffer, 0, length).Trim('-');
        return result;
    }
}
