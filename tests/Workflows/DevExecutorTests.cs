using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class DevExecutorTests
{
    [Fact]
    public async Task HandleAsync_UpdatesFilesAndStoresResult()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(4, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("updated-content");
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var specPath = WorkflowPaths.SpecPath(workItem.Number);
        var specContent = "## Ziel\nGoal\n## Nicht-Ziele\nNone\n## Komponenten\n- Api\n## Touch List\n| Operation | Path | Notes |\n| --- | --- | --- |\n| Modify | src/App.cs | |\n## Interfaces\n```csharp\ninterface ITest {}\n```\n## Szenarien\nScenario: One\nGiven A\nWhen B\nThen C\nScenario: Two\nGiven A\nWhen B\nThen C\nScenario: Three\nGiven A\nWhen B\nThen C\n## Sequenz\n1. Step\n2. Step\n## Testmatrix\n| Test | Files | Notes |\n| --- | --- | --- |\n| Unit | tests/AppTests.cs | |\n";
        workspace.Setup(w => w.Exists(specPath)).Returns(true);
        workspace.Setup(w => w.ReadAllText(specPath)).Returns(specContent);
        workspace.Setup(w => w.Exists("src/App.cs")).Returns(true);
        workspace.Setup(w => w.ReadAllText("src/App.cs")).Returns("original");

        var executor = new DevExecutor(workContext, config.Workflow);
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
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        repo.Verify(r => r.EnsureBranch(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        repo.Verify(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Once);
        workspace.Verify(w => w.WriteAllText("src/App.cs", "updated-content"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_FailsWhenForbiddenTouchListPresent()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(5, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var specPath = WorkflowPaths.SpecPath(workItem.Number);
        var specContent = "## Ziel\nGoal\n## Nicht-Ziele\nNone\n## Komponenten\n- Api\n## Touch List\n| Operation | Path | Notes |\n| --- | --- | --- |\n| Forbidden | src/Secret.cs | |\n## Interfaces\n```csharp\ninterface ITest {}\n```\n## Szenarien\nScenario: One\nGiven A\nWhen B\nThen C\nScenario: Two\nGiven A\nWhen B\nThen C\nScenario: Three\nGiven A\nWhen B\nThen C\n## Sequenz\n1. Step\n2. Step\n## Testmatrix\n| Test | Files | Notes |\n| --- | --- | --- |\n| Unit | tests/AppTests.cs | |\n";
        workspace.Setup(w => w.Exists(specPath)).Returns(true);
        workspace.Setup(w => w.ReadAllText(specPath)).Returns(specContent);

        var executor = new DevExecutor(workContext, config.Workflow);
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

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Contains("forbidden", output.Notes, StringComparison.OrdinalIgnoreCase);
    }
}
