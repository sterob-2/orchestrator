using System.Collections.Generic;
using Moq;
using Orchestrator.App;
using Orchestrator.App.Agents;
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
        var input = new WorkflowInput(3, "Title", "Body", labels);
        var output = new WorkflowOutput(true, "notes", "Next");

        Assert.Equal(3, input.IssueNumber);
        Assert.Equal("Title", input.Title);
        Assert.Equal("Body", input.Body);
        Assert.Equal(labels, input.Labels);

        Assert.True(output.Success);
        Assert.Equal("notes", output.Notes);
        Assert.Equal("Next", output.NextStage);
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
