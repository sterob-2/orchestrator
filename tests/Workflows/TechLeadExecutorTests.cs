using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class TechLeadExecutorTests
{
    [Fact]
    public async Task HandleAsync_WritesSpecAndStoresResult()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        workspace.Setup(w => w.ReadOrTemplate(WorkflowPaths.SpecTemplatePath, WorkflowPaths.SpecTemplatePath, It.IsAny<Dictionary<string, string>>()))
            .Returns("template");
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("## Ziel\nGoal\n## Nicht-Ziele\nNone\n## Komponenten\n- Api\n## Touch List\n| Operation | Path | Notes |\n| --- | --- | --- |\n| Modify | src/App.cs | |\n## Interfaces\n```csharp\ninterface ITest {}\n```\n## Szenarien\nScenario: One\nGiven A\nWhen B\nThen C\nScenario: Two\nGiven A\nWhen B\nThen C\nScenario: Three\nGiven A\nWhen B\nThen C\n## Sequenz\n1. Step\n2. Step\n## Testmatrix\n| Test | Files | Notes |\n| --- | --- | --- |\n| Unit | tests/AppTests.cs | |\n");
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new TechLeadExecutor(workContext, config.Workflow);
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

        string? stored = null;
        workflowContext.Setup(c => c.QueueStateUpdateAsync(WorkflowStateKeys.TechLeadResult, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, value, _) => stored = value)
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.NotNull(stored);
        workspace.Verify(w => w.WriteAllText(WorkflowPaths.SpecPath(workItem.Number), It.IsAny<string>()), Times.Once);
    }
}
