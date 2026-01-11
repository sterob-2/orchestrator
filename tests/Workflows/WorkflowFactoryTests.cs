using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowFactoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("ContextBuilder")]
    [InlineData("Refinement")]
    [InlineData("DoR")]
    [InlineData("TechLead")]
    [InlineData("SpecGate")]
    [InlineData("Dev")]
    [InlineData("CodeReview")]
    [InlineData("DoD")]
    public void BuildGraph_ReturnsWorkflowForAnyStartStage(string? stageName)
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        WorkflowStage? startStage = stageName is null ? null : Enum.Parse<WorkflowStage>(stageName);
        var workflow = WorkflowFactory.BuildGraph(context, startStage);

        Assert.NotNull(workflow);
    }
}
