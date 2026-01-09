using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

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
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        github.Setup(g => g.CommentOnWorkItemAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        workspace.Setup(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        workspace.Setup(w => w.ReadOrTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(string.Empty);
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(false);
        repo.Setup(r => r.EnsureBranch(It.IsAny<string>(), It.IsAny<string>()));
        var llm = new Mock<ILlmClient>();
        var refinementJson = WorkflowJson.Serialize(new RefinementResult(
            "Clarified story",
            new List<string> { "Acceptance criteria 1" },
            new List<OpenQuestion>(),
            new ComplexityIndicators(new List<string>(), null)));
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(refinementJson));
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
