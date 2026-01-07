using System.Collections.Generic;
using Moq;
using Orchestrator.App;
using Orchestrator.App.Agents;
using Orchestrator.App.Core.Configuration;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class ModelsTests
{
    [Fact]
    public void WorkItem_RecordCreation()
    {
        var labels = new List<string> { "label1", "label2" };
        var item = new WorkItem(1, "Title", "Body", "https://example.com", labels);

        Assert.Equal(1, item.Number);
        Assert.Equal("Title", item.Title);
        Assert.Equal("Body", item.Body);
        Assert.Equal("https://example.com", item.Url);
        Assert.Equal(labels, item.Labels);
    }

    [Fact]
    public void RepoFile_RecordCreation()
    {
        var file = new RepoFile("src/file.txt", "content", "sha123");

        Assert.Equal("src/file.txt", file.Path);
        Assert.Equal("content", file.Content);
        Assert.Equal("sha123", file.Sha);
    }

    [Fact]
    public void IssueComment_RecordCreation()
    {
        var comment = new IssueComment("alice", "Looks good");

        Assert.Equal("alice", comment.Author);
        Assert.Equal("Looks good", comment.Body);
    }

    [Fact]
    public void ProjectModels_RecordCreation()
    {
        var item = new ProjectItem("Title", 7, "https://example.com/issue/7", "Ready");
        var snapshot = new ProjectSnapshot(
            Owner: "octo",
            Number: 1,
            OwnerType: ProjectOwnerType.Organization,
            Title: "Roadmap",
            Items: new List<ProjectItem> { item });

        var itemRef = new ProjectItemRef("item-1", 7);
        var metadata = new ProjectMetadata(
            ProjectId: "proj-1",
            StatusFieldId: "status-1",
            StatusOptions: new Dictionary<string, string> { ["Ready"] = "opt-1" },
            Items: new List<ProjectItemRef> { itemRef });

        var reference = new ProjectReference("octo", 1, ProjectOwnerType.Organization);

        Assert.Equal("Title", item.Title);
        Assert.Equal(7, item.IssueNumber);
        Assert.Equal("https://example.com/issue/7", item.Url);
        Assert.Equal("Ready", item.Status);

        Assert.Equal("octo", snapshot.Owner);
        Assert.Equal(1, snapshot.Number);
        Assert.Equal(ProjectOwnerType.Organization, snapshot.OwnerType);
        Assert.Equal("Roadmap", snapshot.Title);
        Assert.Single(snapshot.Items);

        Assert.Equal("item-1", itemRef.ItemId);
        Assert.Equal(7, itemRef.IssueNumber);
        Assert.Equal("proj-1", metadata.ProjectId);
        Assert.Equal("status-1", metadata.StatusFieldId);
        Assert.Single(metadata.StatusOptions);
        Assert.Single(metadata.Items);

        Assert.Equal("octo", reference.Owner);
        Assert.Equal(1, reference.Number);
        Assert.Equal(ProjectOwnerType.Organization, reference.OwnerType);
    }

    [Fact]
    public void WorkContext_RecordCreation()
    {
        var item = new WorkItem(2, "Title", "Body", "https://example.com", new List<string>());
        var config = OrchestratorConfig.FromEnvironment();

        var context = new WorkContext(item, null!, config, null!, null!, null!);

        Assert.Same(item, context.WorkItem);
        Assert.Same(config, context.Config);
        Assert.Null(context.GitHub);
        Assert.Null(context.Workspace);
        Assert.Null(context.Repo);
        Assert.Null(context.Llm);
    }

    [Fact]
    public void AgentResult_FactoryMethods()
    {
        var ok = AgentResult.Ok("ok");
        var fail = AgentResult.Fail("fail");

        Assert.True(ok.Success);
        Assert.Equal("ok", ok.Notes);
        Assert.False(fail.Success);
        Assert.Equal("fail", fail.Notes);
    }

    [Fact]
    public void AgentResult_WithLabels()
    {
        var add = new List<string> { "add" };
        var remove = new List<string> { "remove" };

        var result = new AgentResult(true, "notes", "next", add, remove);

        Assert.True(result.Success);
        Assert.Equal("notes", result.Notes);
        Assert.Equal("next", result.NextStageLabel);
        Assert.Equal(add, result.AddLabels);
        Assert.Equal(remove, result.RemoveLabels);
    }

    [Fact]
    public void PipelineResult_FactoryMethods()
    {
        var ok = PipelineResult.Ok("summary", "title", "body");
        var fail = PipelineResult.Fail("summary");

        Assert.True(ok.Success);
        Assert.Equal("summary", ok.Summary);
        Assert.Equal("title", ok.PullRequestTitle);
        Assert.Equal("body", ok.PullRequestBody);

        Assert.False(fail.Success);
        Assert.Equal("summary", fail.Summary);
        Assert.Equal(string.Empty, fail.PullRequestTitle);
        Assert.Equal(string.Empty, fail.PullRequestBody);
    }

    [Fact]
    public void WorkflowInputOutput_Creation()
    {
        var labels = new List<string> { "ready" };
        var item = new WorkItem(3, "Title", "Body", "https://example.com", labels);
        var project = new ProjectContext(
            RepoOwner: "owner",
            RepoName: "repo",
            DefaultBaseBranch: "main",
            WorkspacePath: "/workspace",
            WorkspaceHostPath: "/workspace",
            ProjectOwner: "owner",
            ProjectOwnerType: "user",
            ProjectNumber: 7);
        var input = new WorkflowInput(item, project, "planner", 1);
        var output = new WorkflowOutput(true, "notes", WorkflowStage.Dev);

        Assert.Same(item, input.WorkItem);
        Assert.Same(project, input.ProjectContext);
        Assert.Equal("planner", input.Mode);
        Assert.Equal(1, input.Attempt);

        Assert.True(output.Success);
        Assert.Equal("notes", output.Notes);
        Assert.Equal(WorkflowStage.Dev, output.NextStage);
    }

    [Fact]
    public void WorkContext_McpFiles_WithMcp_ReturnsOperationsInstance()
    {
        var item = new WorkItem(1, "Title", "Body", "https://example.com", new List<string>());
        var config = OrchestratorConfig.FromEnvironment();
        var mockMcp = new Mock<McpClientManager>();

        var context = new WorkContext(item, null!, config, null!, null!, null!, mockMcp.Object);

        Assert.NotNull(context.McpFiles);
        Assert.IsType<McpFileOperations>(context.McpFiles);
    }

    [Fact]
    public void WorkContext_McpFiles_WithoutMcp_ReturnsNull()
    {
        var item = new WorkItem(1, "Title", "Body", "https://example.com", new List<string>());
        var config = OrchestratorConfig.FromEnvironment();

        var context = new WorkContext(item, null!, config, null!, null!, null!, null);

        Assert.Null(context.McpFiles);
    }
}
