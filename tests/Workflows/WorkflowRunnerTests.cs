using Moq;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsExpectedOutput()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var config = OrchestratorConfig.FromEnvironment();
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var workspace = new Mock<IRepoWorkspace>(MockBehavior.Strict);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var runner = new WorkflowRunner();

        var output = await runner.RunAsync(context, WorkflowStage.Dev, CancellationToken.None);

        Assert.NotNull(output);
        Assert.Equal(WorkflowStage.CodeReview, output!.NextStage);
    }
}
