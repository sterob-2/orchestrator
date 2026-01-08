using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class SpecGateExecutorTests
{
    [Fact]
    public async Task HandleAsync_PassesWithValidSpec()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(3, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var specContent = "## Ziel\nGoal using .NET 8 with Clean Architecture.\n## Nicht-Ziele\nNone\n## Komponenten\n- Api\n## Touch List\n| Operation | Path | Notes |\n| --- | --- | --- |\n| Modify | src/App.cs | |\n## Interfaces\n```csharp\ninterface ITest {}\n```\n## Szenarien\nScenario: One\nGiven A\nWhen B\nThen C\nScenario: Two\nGiven A\nWhen B\nThen C\nScenario: Three\nGiven A\nWhen B\nThen C\n## Sequenz\n1. Step\n2. Step\n## Testmatrix\n| Test | Files | Notes |\n| --- | --- | --- |\n| Unit | tests/AppTests.cs | |\n";
        workspace.Setup(w => w.Exists(WorkflowPaths.SpecPath(workItem.Number))).Returns(true);
        workspace.Setup(w => w.ReadAllText(WorkflowPaths.SpecPath(workItem.Number))).Returns(specContent);
        workspace.Setup(w => w.Exists("src/App.cs")).Returns(true);
        var playbookContent = "project: Orchestrator\nversion: 2.0\nallowed_frameworks:\n  - id: FW-01\n    name: .NET 8\n    version: 8.x\nallowed_patterns:\n  - id: PAT-01\n    name: Clean Architecture\n    reference: docs/arch.md\n";
        workspace.Setup(w => w.Exists(WorkflowPaths.PlaybookPath)).Returns(true);
        workspace.Setup(w => w.ReadAllText(WorkflowPaths.PlaybookPath)).Returns(playbookContent);

        var executor = new SpecGateExecutor(workContext, config.Workflow);
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
    }
}
