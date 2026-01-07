using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Workflows;

public class SDLCWorkflowTests
{
    private sealed class TestExecutor : Executor<WorkflowInput, WorkflowOutput>
    {
        public TestExecutor() : base("Test")
        {
        }

        public override ValueTask<WorkflowOutput> HandleAsync(
            WorkflowInput input,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new WorkflowOutput(true, $"Handled {input.IssueNumber}"));
        }
    }

    [Fact]
    public void BuildPlannerOnlyWorkflow_ReturnsWorkflow()
    {
        var context = MockWorkContext.Create();

        var workflow = SDLCWorkflow.BuildPlannerOnlyWorkflow(context);

        workflow.Should().NotBeNull();
    }

    [Fact]
    public async Task RunWorkflowAsync_ReturnsFinalOutput()
    {
        var executor = new TestExecutor();
        var workflow = new WorkflowBuilder(executor)
            .WithOutputFrom(executor)
            .Build();

        var input = new WorkflowInput(1, "Test", "Body", new List<string> { "ready-for-agents" });

        var result = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Notes.Should().Contain("Handled 1");
    }
}
