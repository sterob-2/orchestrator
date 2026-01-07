using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class HumanInLoopHandlerTests
{
    [Fact]
    public async Task ApplyAsync_WhenSuccess_DoesNothing()
    {
        var workItem = MockWorkContext.CreateWorkItem();
        var config = MockWorkContext.CreateConfig();
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var handler = new HumanInLoopHandler(github.Object, config.Labels);

        await handler.ApplyAsync(workItem, new WorkflowOutput(true, "ok", WorkflowStage.Dev));

        github.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenFailure_AddsBlockedAndUserReviewLabels()
    {
        var workItem = MockWorkContext.CreateWorkItem();
        var config = MockWorkContext.CreateConfig();
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        github.Setup(g => g.RemoveLabelsAsync(workItem.Number, It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        github.Setup(g => g.AddLabelsAsync(workItem.Number, config.Labels.BlockedLabel, config.Labels.UserReviewRequiredLabel))
            .Returns(Task.CompletedTask);

        var handler = new HumanInLoopHandler(github.Object, config.Labels);

        await handler.ApplyAsync(workItem, new WorkflowOutput(false, "failed", null));

        github.VerifyAll();
    }
}
