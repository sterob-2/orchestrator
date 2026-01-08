using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowFactoryTests
{
    [Theory]
    [InlineData("Refinement")]
    [InlineData("DoR")]
    [InlineData("TechLead")]
    [InlineData("SpecGate")]
    [InlineData("Dev")]
    [InlineData("CodeReview")]
    [InlineData("DoD")]
    [InlineData("Release")]
    [InlineData("ContextBuilder")]
    public async Task Build_ReturnsWorkflowWithExpectedNextStage(string stageName)
    {
        var config = MockWorkContext.CreateConfig();
        var stage = Enum.Parse<WorkflowStage>(stageName);
        var workflow = WorkflowFactory.Build(stage, config.Workflow, config.Labels);
        var input = new WorkflowInput(
            new WorkItem(1, "Title", "Body", "url", new List<string>()),
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        Assert.NotNull(output);
        Assert.True(output!.Success);
        Assert.Equal(WorkflowStageGraph.NextStageFor(stage), output.NextStage);
    }
}
