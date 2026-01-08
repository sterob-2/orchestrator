using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class ReleaseExecutorTests
{
    [Fact]
    public async Task HandleAsync_FailsWhenDodMissing()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(8, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ReleaseExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(It.IsAny<string>(), It.IsAny<Func<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.DodGateResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
    }

    [Fact]
    public async Task HandleAsync_CreatesReleaseNotes()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(9, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetPullRequestNumberAsync(It.IsAny<string>())).ReturnsAsync((int?)null);
        github.Setup(g => g.OpenPullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://github.com/org/repo/pull/123");
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var specPath = WorkflowPaths.SpecPath(workItem.Number);
        var specContent = "## Ziel\nGoal\n## Nicht-Ziele\nNone\n## Komponenten\n- Api\n## Touch List\n| Operation | Path | Notes |\n| --- | --- | --- |\n| Modify | src/App.cs | |\n## Interfaces\n```csharp\ninterface ITest {}\n```\n## Szenarien\nScenario: One\nGiven A\nWhen B\nThen C\nScenario: Two\nGiven A\nWhen B\nThen C\nScenario: Three\nGiven A\nWhen B\nThen C\n## Sequenz\n1. Step\n2. Step\n## Testmatrix\n| Test | Files | Notes |\n| --- | --- | --- |\n| Unit | tests/AppTests.cs | |\n";
        workspace.Setup(w => w.Exists(specPath)).Returns(true);
        workspace.Setup(w => w.ReadAllText(specPath)).Returns(specContent);

        var executor = new ReleaseExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var dodResult = new GateResult(true, "ok", new List<string>());
        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(It.IsAny<string>(), It.IsAny<Func<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.DodGateResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowJson.Serialize(dodResult));

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        workspace.Verify(w => w.WriteAllText(WorkflowPaths.ReleasePath(workItem.Number), It.IsAny<string>()), Times.Once);
    }
}
