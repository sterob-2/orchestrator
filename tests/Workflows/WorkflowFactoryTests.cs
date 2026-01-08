using Moq;
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
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);
        var workflow = WorkflowFactory.Build(stage, context);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        Assert.NotNull(output);
        if (stage == WorkflowStage.DoR)
        {
            Assert.False(output!.Success);
            Assert.Equal(WorkflowStage.Refinement, output.NextStage);
        }
        else if (stage == WorkflowStage.SpecGate)
        {
            Assert.False(output!.Success);
            Assert.Equal(WorkflowStage.TechLead, output.NextStage);
        }
        else if (stage == WorkflowStage.Dev)
        {
            Assert.False(output!.Success);
            Assert.Equal(WorkflowStage.CodeReview, output.NextStage);
        }
        else if (stage == WorkflowStage.CodeReview)
        {
            Assert.False(output!.Success);
            Assert.Equal(WorkflowStage.Dev, output.NextStage);
        }
        else if (stage == WorkflowStage.DoD)
        {
            Assert.False(output!.Success);
            Assert.Equal(WorkflowStage.Dev, output.NextStage);
        }
        else if (stage == WorkflowStage.Release)
        {
            Assert.False(output!.Success);
            Assert.Null(output.NextStage);
        }
        else
        {
            Assert.True(output!.Success);
            Assert.Equal(WorkflowStageGraph.NextStageFor(stage), output.NextStage);
        }
    }
}
