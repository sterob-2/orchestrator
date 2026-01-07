using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class SDLCWorkflowTests
{
    [Fact]
    public void BuildStageWorkflow_ReturnsWorkflow()
    {
        var workflow = SDLCWorkflow.BuildStageWorkflow(WorkflowStage.Refinement);

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
