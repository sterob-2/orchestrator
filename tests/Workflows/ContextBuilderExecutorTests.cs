using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class ContextBuilderExecutorTests
{
    [Fact]
    public async Task HandleAsync_SuccessfullyCreatesBranch()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch("issue-1", config.Workflow.DefaultBaseBranch));
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ContextBuilderExecutor(workContext, config.Workflow, config.Labels, null);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Contains("Branch 'issue-1' ready for work", output.Notes);
        repo.Verify(r => r.EnsureBranch("issue-1", config.Workflow.DefaultBaseBranch), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_HandlesGitException()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new LibGit2Sharp.LibGit2SharpException("Git error"));
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ContextBuilderExecutor(workContext, config.Workflow, config.Labels, null);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Contains("Failed to create branch", output.Notes);
        Assert.Contains("Git error", output.Notes);
    }

    [Fact]
    public async Task HandleAsync_HandlesIOException()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new System.IO.IOException("IO error"));
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ContextBuilderExecutor(workContext, config.Workflow, config.Labels, null);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Contains("Failed to create branch", output.Notes);
        Assert.Contains("IO error", output.Notes);
    }

    [Fact]
    public async Task HandleAsync_UsesStartOverride()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string> { "agent:planner" });
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch("issue-1", config.Workflow.DefaultBaseBranch));
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ContextBuilderExecutor(workContext, config.Workflow, config.Labels, WorkflowStage.Dev);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Equal(WorkflowStage.Dev, output.NextStage);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNullWhenBranchCreationFails()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new LibGit2Sharp.LibGit2SharpException("Git error"));
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ContextBuilderExecutor(workContext, config.Workflow, config.Labels, null);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Null(output.NextStage);
    }

    [Fact]
    public async Task HandleAsync_IgnoresStartOverrideIfContextBuilder()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string> { "agent:planner" });
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch("issue-1", config.Workflow.DefaultBaseBranch));
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ContextBuilderExecutor(workContext, config.Workflow, config.Labels, WorkflowStage.ContextBuilder);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Equal(WorkflowStage.Refinement, output.NextStage); // Should use label-based detection, not override
    }
}
