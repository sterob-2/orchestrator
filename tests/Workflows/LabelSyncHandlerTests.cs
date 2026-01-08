using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class LabelSyncHandlerTests
{
    [Fact]
    public async Task ApplyAsync_AddsNextStageAndInProgress()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());

        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        github.Setup(g => g.RemoveLabelsAsync(workItem.Number, It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        github.Setup(g => g.AddLabelsAsync(workItem.Number, config.Labels.InProgressLabel, config.Labels.TechLeadLabel))
            .Returns(Task.CompletedTask);

        var handler = new LabelSyncHandler(github.Object, config.Labels);
        var output = new WorkflowOutput(true, "notes", WorkflowStage.TechLead);

        await handler.ApplyAsync(workItem, output);

        github.Verify(g => g.RemoveLabelsAsync(workItem.Number, It.IsAny<string[]>()), Times.Once);
        github.Verify(g => g.AddLabelsAsync(workItem.Number, config.Labels.InProgressLabel, config.Labels.TechLeadLabel), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_AddsDoneWhenNoNextStage()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(2, "Title", "Body", "url", new List<string>());

        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        github.Setup(g => g.RemoveLabelsAsync(workItem.Number, It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        github.Setup(g => g.AddLabelsAsync(workItem.Number, config.Labels.DoneLabel))
            .Returns(Task.CompletedTask);

        var handler = new LabelSyncHandler(github.Object, config.Labels);
        var output = new WorkflowOutput(true, "notes", null);

        await handler.ApplyAsync(workItem, output);

        github.Verify(g => g.AddLabelsAsync(workItem.Number, config.Labels.DoneLabel), Times.Once);
    }
}
