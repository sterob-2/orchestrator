using System.Collections.Generic;
using Orchestrator.App;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class WorkItemParsersTests
{
    [Fact]
    public void TryParseProjectReference_WithUserProjects()
    {
        var body = "See https://github.com/users/robin/projects/12 for details.";

        var result = WorkItemParsers.TryParseProjectReference(body);

        Assert.NotNull(result);
        Assert.Equal("robin", result!.Owner);
        Assert.Equal(12, result.Number);
        Assert.Equal(ProjectOwnerType.User, result.OwnerType);
    }

    [Fact]
    public void TryParseProjectReference_WithOrgProjects()
    {
        var body = "Project: https://github.com/orgs/acme/projects/3";

        var result = WorkItemParsers.TryParseProjectReference(body);

        Assert.NotNull(result);
        Assert.Equal("acme", result!.Owner);
        Assert.Equal(3, result.Number);
        Assert.Equal(ProjectOwnerType.Organization, result.OwnerType);
    }

    [Fact]
    public void TryParseProjectReference_WithInvalidUrls()
    {
        Assert.Null(WorkItemParsers.TryParseProjectReference("https://github.com/users/robin/project/1"));
        Assert.Null(WorkItemParsers.TryParseProjectReference("https://github.com/users/robin/projects/"));
        Assert.Null(WorkItemParsers.TryParseProjectReference("https://github.com/orgs/acme/projects/foo"));
    }

    [Fact]
    public void TryParseProjectReference_WithNullOrEmptyBody()
    {
        Assert.Null(WorkItemParsers.TryParseProjectReference(""));
        Assert.Null(WorkItemParsers.TryParseProjectReference("   "));
        Assert.Null(WorkItemParsers.TryParseProjectReference(null!));
    }

    [Fact]
    public void TryParseIssueNumber_WithVariousFormats()
    {
        Assert.Equal(123, WorkItemParsers.TryParseIssueNumber("Issue #123"));
        Assert.Equal(45, WorkItemParsers.TryParseIssueNumber("issue #45 - done"));
        Assert.Equal(7, WorkItemParsers.TryParseIssueNumber("ISSUE #7 is ready"));
    }

    [Fact]
    public void TryParseIssueNumber_EdgeCases()
    {
        Assert.Null(WorkItemParsers.TryParseIssueNumber("Issue #"));
        Assert.Null(WorkItemParsers.TryParseIssueNumber("Issue #abc"));
        Assert.Equal(12, WorkItemParsers.TryParseIssueNumber("Issue #12a"));
    }

    [Fact]
    public void TryParseAcceptanceCriteria_WithBulletLists()
    {
        var body = "Acceptance Criteria\n- First item\n- Second item\n";

        var results = WorkItemParsers.TryParseAcceptanceCriteria(body);

        Assert.Equal(new List<string> { "First item", "Second item" }, results);
    }

    [Fact]
    public void TryParseAcceptanceCriteria_WithNumberedLists()
    {
        var body = "Acceptance criteria\n1. First item\n2. Second item\n";

        var results = WorkItemParsers.TryParseAcceptanceCriteria(body);

        Assert.Empty(results);
    }

    [Fact]
    public void TryParseAcceptanceCriteria_WithNoCriteria()
    {
        var body = "No acceptance section here.";

        var results = WorkItemParsers.TryParseAcceptanceCriteria(body);

        Assert.Empty(results);
    }

    [Fact]
    public void MarkAcceptanceCriteriaDone_CheckboxReplacement()
    {
        var content = "- [ ] First item\n- [x] Second item\n* [ ] Third item\n";

        var updated = WorkItemParsers.MarkAcceptanceCriteriaDone(content);

        Assert.Contains("- [x] First item", updated);
        Assert.Contains("- [x] Second item", updated);
        Assert.Contains("* [ ] Third item", updated);
    }

    [Fact]
    public void TryParseSpecFiles_FromMarkdown()
    {
        var content = "## Files\n- src/Orchestrator.App/Program.cs\n- docs/spec.md\n## Next\n";

        var results = WorkItemParsers.TryParseSpecFiles(content);

        Assert.Equal(new List<string> { "src/Orchestrator.App/Program.cs", "docs/spec.md" }, results);
    }

    [Fact]
    public void TryParseSpecFiles_WithInlineDescriptions()
    {
        var content = "## Files\n- src/Orchestrator.App/Program.cs (main entry point)\n- docs/spec.md (notes)\n";

        var results = WorkItemParsers.TryParseSpecFiles(content);

        Assert.Equal(new List<string> { "src/Orchestrator.App/Program.cs", "docs/spec.md" }, results);
    }

    [Fact]
    public void TryParseSection_ExtractsSection()
    {
        var content = "## Answers\nOne line\nTwo line\n## Next\n";

        var result = WorkItemParsers.TryParseSection(content, "## Answers");

        Assert.Equal("One line\nTwo line", result);
    }

    [Fact]
    public void IsSafeRelativePath_Validation()
    {
        Assert.False(WorkItemParsers.IsSafeRelativePath("/etc/passwd"));
        Assert.False(WorkItemParsers.IsSafeRelativePath("\\share\\file"));
        Assert.False(WorkItemParsers.IsSafeRelativePath("src/../secret.txt"));
        Assert.False(WorkItemParsers.IsSafeRelativePath("src..//file.txt"));
        Assert.True(WorkItemParsers.IsSafeRelativePath("src/file.txt"));
        Assert.True(WorkItemParsers.IsSafeRelativePath("docs/readme.md"));
    }
}
