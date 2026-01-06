using System.Collections.Generic;
using Moq;
using Orchestrator.App.Core.Adapters;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class OctokitGitHubClientAdapterTests
{
    [Fact]
    public async Task Adapter_ForwardsCallsAndMapsWorkItems()
    {
        var legacy = new Mock<ILegacyGitHubClient>(MockBehavior.Strict);
        var legacyItems = new List<global::Orchestrator.App.WorkItem>
        {
            new(1, "Title", "Body", "https://example.com", new List<string> { "label" })
        };
        var repoFile = new global::Orchestrator.App.RepoFile("path", "content", "sha");
        var snapshot = new global::Orchestrator.App.ProjectSnapshot(
            "owner",
            1,
            global::Orchestrator.App.ProjectOwnerType.User,
            "title",
            new List<global::Orchestrator.App.ProjectItem>()
        );

        legacy.Setup(c => c.GetOpenWorkItemsAsync(25)).ReturnsAsync(legacyItems);
        legacy.Setup(c => c.GetIssueLabelsAsync(1)).ReturnsAsync(new List<string> { "one" });
        legacy.Setup(c => c.OpenPullRequestAsync("head", "base", "title", "body"))
            .ReturnsAsync("https://pr");
        legacy.Setup(c => c.GetPullRequestNumberAsync("branch")).ReturnsAsync(42);
        legacy.Setup(c => c.ClosePullRequestAsync(42)).Returns(Task.CompletedTask);
        legacy.Setup(c => c.GetIssueCommentsAsync(1))
            .ReturnsAsync(new List<global::Orchestrator.App.IssueComment> { new("user", "comment") });
        legacy.Setup(c => c.CommentOnWorkItemAsync(1, "note")).Returns(Task.CompletedTask);
        legacy.Setup(c => c.AddLabelsAsync(1, "a", "b")).Returns(Task.CompletedTask);
        legacy.Setup(c => c.RemoveLabelAsync(1, "a")).Returns(Task.CompletedTask);
        legacy.Setup(c => c.RemoveLabelsAsync(1, "a", "b")).Returns(Task.CompletedTask);
        legacy.Setup(c => c.GetPullRequestDiffAsync(42)).ReturnsAsync("diff");
        legacy.Setup(c => c.CreateBranchAsync("branch")).Returns(Task.CompletedTask);
        legacy.Setup(c => c.DeleteBranchAsync("branch")).Returns(Task.CompletedTask);
        legacy.Setup(c => c.HasCommitsAsync("base", "head")).ReturnsAsync(true);
        legacy.Setup(c => c.TryGetFileContentAsync("branch", "path")).ReturnsAsync(repoFile);
        legacy.Setup(c => c.CreateOrUpdateFileAsync("branch", "path", "content", "msg"))
            .Returns(Task.CompletedTask);
        legacy.Setup(c => c.GetProjectSnapshotAsync("owner", 1, global::Orchestrator.App.ProjectOwnerType.User))
            .ReturnsAsync(snapshot);
        legacy.Setup(c => c.UpdateProjectItemStatusAsync("owner", 1, 2, "Done"))
            .Returns(Task.CompletedTask);

        var adapter = new OctokitGitHubClientAdapter(legacy.Object);

        var mapped = await adapter.GetOpenWorkItemsAsync(25);
        Assert.Single(mapped);
        Assert.Equal(legacyItems[0].Number, mapped[0].Number);
        Assert.Equal(legacyItems[0].Title, mapped[0].Title);
        Assert.Equal(legacyItems[0].Body, mapped[0].Body);
        Assert.Equal(legacyItems[0].Url, mapped[0].Url);
        Assert.Equal(legacyItems[0].Labels, mapped[0].Labels);

        var labels = await adapter.GetIssueLabelsAsync(1);
        Assert.Equal(new[] { "one" }, labels);

        var prUrl = await adapter.OpenPullRequestAsync("head", "base", "title", "body");
        Assert.Equal("https://pr", prUrl);

        var prNumber = await adapter.GetPullRequestNumberAsync("branch");
        Assert.Equal(42, prNumber);

        await adapter.ClosePullRequestAsync(42);

        var comments = await adapter.GetIssueCommentsAsync(1);
        Assert.Single(comments);
        Assert.Equal("user", comments[0].Author);

        await adapter.CommentOnWorkItemAsync(1, "note");
        await adapter.AddLabelsAsync(1, "a", "b");
        await adapter.RemoveLabelAsync(1, "a");
        await adapter.RemoveLabelsAsync(1, "a", "b");

        var diff = await adapter.GetPullRequestDiffAsync(42);
        Assert.Equal("diff", diff);

        await adapter.CreateBranchAsync("branch");
        await adapter.DeleteBranchAsync("branch");

        var hasCommits = await adapter.HasCommitsAsync("base", "head");
        Assert.True(hasCommits);

        var file = await adapter.TryGetFileContentAsync("branch", "path");
        Assert.Same(repoFile, file);

        await adapter.CreateOrUpdateFileAsync("branch", "path", "content", "msg");

        var project = await adapter.GetProjectSnapshotAsync(
            "owner",
            1,
            global::Orchestrator.App.ProjectOwnerType.User);
        Assert.Same(snapshot, project);

        await adapter.UpdateProjectItemStatusAsync("owner", 1, 2, "Done");

        legacy.VerifyAll();
    }
}
