using System;
using System.Collections.Generic;
using Orchestrator.App;
using Orchestrator.App.Utilities;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class TemplateUtilTests
{
    [Fact]
    public void BuildTokens_CreatesCorrectDictionary()
    {
        using var temp = new TempWorkspace();
        var ctx = CreateContext(temp.Workspace);

        var tokens = TemplateUtil.BuildTokens(ctx);

        Assert.Equal("42", tokens["{{ISSUE_NUMBER}}"]);
        Assert.Equal("Fix bug", tokens["{{ISSUE_TITLE}}"]);
        Assert.Equal("https://example.com/issue/42", tokens["{{ISSUE_URL}}"]);
        Assert.EndsWith("UTC", tokens["{{UPDATED_AT_UTC}}"]);
    }

    [Fact]
    public void RenderTemplate_WithExistingTemplateFile()
    {
        using var temp = new TempWorkspace();
        var ctx = CreateContext(temp.Workspace);
        var templatePath = "templates/review.md";
        temp.Workspace.WriteAllText(templatePath, "Issue {{ISSUE_NUMBER}}: {{ISSUE_TITLE}}");

        var tokens = TemplateUtil.BuildTokens(ctx);
        var result = TemplateUtil.RenderTemplate(temp.Workspace, templatePath, tokens, "fallback");

        Assert.Equal("Issue 42: Fix bug", result);
    }

    [Fact]
    public void RenderTemplate_WithFallback()
    {
        using var temp = new TempWorkspace();
        var ctx = CreateContext(temp.Workspace);

        var tokens = TemplateUtil.BuildTokens(ctx);
        var result = TemplateUtil.RenderTemplate(temp.Workspace, "missing.md", tokens, "Issue {{ISSUE_NUMBER}} fallback");

        Assert.Equal("Issue 42 fallback", result);
    }

    [Fact]
    public void RenderTemplate_TokenReplacementIsCaseInsensitive()
    {
        using var temp = new TempWorkspace();
        var ctx = CreateContext(temp.Workspace);
        var templatePath = "templates/review.md";
        temp.Workspace.WriteAllText(templatePath, "Issue {{issue_number}}: {{issue_title}}");

        var tokens = TemplateUtil.BuildTokens(ctx);
        var result = TemplateUtil.RenderTemplate(temp.Workspace, templatePath, tokens, "fallback");

        Assert.Equal("Issue 42: Fix bug", result);
    }

    [Fact]
    public void UpdateStatus_UpdatesStatusLine()
    {
        var content = "STATUS: PENDING\nUPDATED: 2020-01-01 00:00:00 UTC\n";

        var updated = TemplateUtil.UpdateStatus(content, "COMPLETE");

        Assert.Contains("STATUS: COMPLETE", updated);
    }

    [Fact]
    public void UpdateStatus_UpdatesUpdatedLine()
    {
        var content = "STATUS: PENDING\nUPDATED: 2020-01-01 00:00:00 UTC\n";

        var updated = TemplateUtil.UpdateStatus(content, "COMPLETE");

        foreach (var line in updated.Split('\n'))
        {
            if (line.StartsWith("UPDATED:", StringComparison.OrdinalIgnoreCase))
            {
                Assert.EndsWith(" UTC", line);
                Assert.Contains("UPDATED:", line);
            }
        }
    }

    [Fact]
    public void UpdateStatus_WithMissingMarkers()
    {
        var content = "No status here.";

        var updated = TemplateUtil.UpdateStatus(content, "COMPLETE");

        Assert.Equal(content, updated);
    }

    [Fact]
    public void IsStatus_Detection()
    {
        var content = "STATUS: PENDING";

        Assert.True(TemplateUtil.IsStatus(content, "pending"));
        Assert.False(TemplateUtil.IsStatus(content, "complete"));
    }

    [Fact]
    public void IsStatusComplete_Detection()
    {
        var content = "STATUS: COMPLETE";

        Assert.True(TemplateUtil.IsStatusComplete(content));
    }

    [Fact]
    public void AppendQuestion_ToExistingQuestionsSection()
    {
        var content = "## Questions\n- Existing\n";

        var updated = TemplateUtil.AppendQuestion(content, "New question?");

        Assert.Contains("- New question?", updated);
        Assert.Contains("- Existing", updated);
    }

    [Fact]
    public void AppendQuestion_CreatesNewSection()
    {
        var content = "Some content";

        var updated = TemplateUtil.AppendQuestion(content, "New question?");

        Assert.Contains("## Questions", updated);
        Assert.Contains("- New question?", updated);
    }

    [Fact]
    public void AppendQuestion_AvoidsDuplicates()
    {
        var content = "## Questions\n- Duplicate?\n";

        var updated = TemplateUtil.AppendQuestion(content, "Duplicate?");

        Assert.Equal(content.Trim(), updated.Trim());
    }

    [Fact]
    public void EnsureTemplateHeader_AddsHeaderIfMissing()
    {
        using var temp = new TempWorkspace();
        var ctx = CreateContext(temp.Workspace);
        var templatePath = "templates/spec.md";
        temp.Workspace.WriteAllText(templatePath, "# Spec: Issue {{ISSUE_NUMBER}} - {{ISSUE_TITLE}}");

        var updated = TemplateUtil.EnsureTemplateHeader("Body section", ctx, templatePath);

        Assert.StartsWith("# Spec: Issue 42 - Fix bug", updated, StringComparison.Ordinal);
        Assert.Contains("Body section", updated);
    }

    [Fact]
    public void EnsureTemplateHeader_PreservesExistingHeader()
    {
        using var temp = new TempWorkspace();
        var ctx = CreateContext(temp.Workspace);
        var content = "# Spec: Issue 42 - Fix bug\n\nBody";

        var updated = TemplateUtil.EnsureTemplateHeader(content, ctx, "templates/spec.md");

        Assert.Equal(content, updated);
    }

    [Fact]
    public void ReplaceSection_ReplacesExistingSection()
    {
        var content = "## Files\n- old\n## Next\n";

        var updated = TemplateUtil.ReplaceSection(content, "## Files", "- new");

        Assert.Contains("## Files\n- new", updated);
        Assert.Contains("## Next", updated);
    }

    [Fact]
    public void ReplaceSection_CreatesNewSection()
    {
        var content = "Existing content";

        var updated = TemplateUtil.ReplaceSection(content, "## Files", "- new");

        Assert.Contains("## Files\n- new", updated);
    }

    private static WorkContext CreateContext(RepoWorkspace workspace)
    {
        var item = new WorkItem(42, "Fix bug", "body", "https://example.com/issue/42", new List<string>());
        return new WorkContext(item, null!, OrchestratorConfig.FromEnvironment(), workspace, null!, null!);
    }
}
