using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class SDLCWorkflowTests
{
    [Fact]
    public void BuildStageWorkflow_ReturnsWorkflow()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);
        var workflow = SDLCWorkflow.BuildStageWorkflow(WorkflowStage.Refinement, context);

        Assert.NotNull(workflow);
    }

    [Fact]
    public async Task RunWorkflowAsync_ReturnsOutputFromExecutor()
    {
        var executor = new StubExecutor();
        var workflow = new WorkflowBuilder(executor)
            .WithOutputFrom(executor)
            .Build();

        var input = new WorkflowInput(
            new WorkItem(1, "Test", "Body", "url", new List<string>()),
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        Assert.NotNull(output);
        Assert.True(output!.Success);
        Assert.Equal("ok", output.Notes);
    }

    private sealed class StubExecutor : Executor<WorkflowInput, WorkflowOutput>
    {
        public StubExecutor() : base("Stub")
        {
        }

        public override ValueTask<WorkflowOutput> HandleAsync(
            WorkflowInput input,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new WorkflowOutput(true, "ok"));
        }
    }
}
