using FluentAssertions;
using Orchestrator.App;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class WorkItemBranchTests
{
    [Fact]
    public void BuildBranchName_WithNormalTitle_CreatesValidBranchName()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 42, title: "Add new feature");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-42-add-new-feature");
    }

    [Fact]
    public void BuildBranchName_WithSpecialCharacters_SanitizesName()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 10, title: "Fix bug #123 & improve performance!");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-10-fix-bug-123-improve-performance");
    }

    [Fact]
    public void BuildBranchName_WithEmptyTitle_UsesDefaultSlug()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 5, title: "");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-5-work-item");
    }

    [Fact]
    public void BuildBranchName_WithWhitespaceOnly_UsesDefaultSlug()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 7, title: "   ");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-7-work-item");
    }

    [Fact]
    public void BuildBranchName_WithMultipleSpaces_CollapseToSingleDash()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 15, title: "Multiple    spaces    here");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-15-multiple-spaces-here");
    }

    [Fact]
    public void BuildBranchName_WithUnicodeCharacters_ReplacesWithDash()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 20, title: "Fix äöü encoding issues");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-20-fix-encoding-issues");
    }

    [Fact]
    public void BuildBranchName_WithLeadingAndTrailingDashes_TrimsCorrectly()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 25, title: "---Title---");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-25-title");
    }

    [Fact]
    public void BuildBranchName_WithMixedCase_ConvertsToLowerCase()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 30, title: "Fix CamelCase Issue");

        var branchName = WorkItemBranch.BuildBranchName(workItem);

        branchName.Should().Be("agent/issue-30-fix-camelcase-issue");
    }
}
