using System.Collections.Generic;
using Orchestrator.App;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class ProjectSummaryFormatterTests
{
    [Fact]
    public void Format_WithGroupedItems()
    {
        var snapshot = new ProjectSnapshot(
            Owner: "robin",
            Number: 1,
            OwnerType: ProjectOwnerType.User,
            Title: "Demo",
            Items: new List<ProjectItem>
            {
                new("First", 1, "https://example.com/1", "Ready"),
                new("Second", 2, "https://example.com/2", "Backlog"),
                new("Third", 3, "https://example.com/3", "In Progress"),
                new("Fourth", 4, "https://example.com/4", "")
            });

        var result = ProjectSummaryFormatter.Format(snapshot);

        Assert.Contains("- Ready: 1", result);
        Assert.Contains("- Backlog: 1", result);
        Assert.Contains("- In Progress: 1", result);
        Assert.Contains("- Unknown: 1", result);
    }

    [Fact]
    public void Format_WithEmptyProject()
    {
        var snapshot = new ProjectSnapshot(
            Owner: "robin",
            Number: 2,
            OwnerType: ProjectOwnerType.User,
            Title: "Empty",
            Items: new List<ProjectItem>());

        var result = ProjectSummaryFormatter.Format(snapshot);

        Assert.Contains("Top-3 next steps:", result);
        Assert.Contains("- No Ready/Backlog items found.", result);
    }

    [Fact]
    public void Format_WithReadyAndBacklogItems()
    {
        var snapshot = new ProjectSnapshot(
            Owner: "robin",
            Number: 3,
            OwnerType: ProjectOwnerType.User,
            Title: "Queue",
            Items: new List<ProjectItem>
            {
                new("First", 1, "https://example.com/1", "Ready"),
                new("Second", 2, "https://example.com/2", "Backlog"),
                new("Third", 3, "https://example.com/3", "Ready")
            });

        var result = ProjectSummaryFormatter.Format(snapshot);

        Assert.Contains("- First (https://example.com/1)", result);
        Assert.Contains("- Second (https://example.com/2)", result);
        Assert.Contains("- Third (https://example.com/3)", result);
    }

    [Fact]
    public void Format_WithNoReadyOrBacklogItems()
    {
        var snapshot = new ProjectSnapshot(
            Owner: "robin",
            Number: 4,
            OwnerType: ProjectOwnerType.User,
            Title: "No Ready",
            Items: new List<ProjectItem>
            {
                new("First", 1, "https://example.com/1", "In Progress")
            });

        var result = ProjectSummaryFormatter.Format(snapshot);

        Assert.Contains("- No Ready/Backlog items found.", result);
    }

    [Fact]
    public void Format_UserVsOrganizationUrls()
    {
        var userSnapshot = new ProjectSnapshot(
            Owner: "robin",
            Number: 5,
            OwnerType: ProjectOwnerType.User,
            Title: "User",
            Items: new List<ProjectItem>());

        var orgSnapshot = new ProjectSnapshot(
            Owner: "acme",
            Number: 6,
            OwnerType: ProjectOwnerType.Organization,
            Title: "Org",
            Items: new List<ProjectItem>());

        var userResult = ProjectSummaryFormatter.Format(userSnapshot);
        var orgResult = ProjectSummaryFormatter.Format(orgSnapshot);

        Assert.Contains("https://github.com/users/robin/projects/5", userResult);
        Assert.Contains("https://github.com/orgs/acme/projects/6", orgResult);
    }
}
