using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows;
using Xunit;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowRunnerTests
{
    [Fact]
    public void BuildDefaultStages_ReturnsExpectedOrder()
    {
        var context = MockWorkContext.Create();
        var factory = new WorkflowFactory();

        var stages = factory.BuildDefaultStages(context);

        stages.Select(stage => stage.Name).Should().Equal(
            "Planner",
            "TechLead",
            "Dev",
            "CodeReview",
            "Test",
            "Release");
    }

    [Fact]
    public async Task RunAsync_AllStagesSucceed_ReturnsSuccess()
    {
        var context = MockWorkContext.Create();
        var runner = new WorkflowRunner(new WorkflowFactory());

        var result = await runner.RunAsync(context, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Notes.Should().Contain("Planner:").And.Contain("Release:");
    }
}
