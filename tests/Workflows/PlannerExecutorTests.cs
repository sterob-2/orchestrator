using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Workflows;

public class PlannerExecutorTests
{
    [Fact]
    public async Task HandleAsync_PlanAlreadyComplete_SkipsWork()
    {
        using var temp = new TempWorkspace();
        var config = MockWorkContext.CreateConfig(temp.WorkspacePath);
        var planPath = "plans/issue-1.md";
        temp.CreateFile(planPath, "STATUS: COMPLETE\n");

        var workItem = MockWorkContext.CreateWorkItem(number: 1);
        var github = new OctokitGitHubClient(config);
        var repo = new RepoGit(config, temp.WorkspacePath);
        var llm = new LlmClient(config);
        var context = new WorkContext(workItem, github, config, temp.Workspace, repo, llm);

        var executor = new PlannerExecutor(context);
        var input = new WorkflowInput(
            IssueNumber: workItem.Number,
            Title: workItem.Title,
            Body: workItem.Body,
            Labels: workItem.Labels.ToList());

        var workflowContext = new Mock<IWorkflowContext>().Object;

        var output = await executor.HandleAsync(input, workflowContext, CancellationToken.None);

        output.Success.Should().BeTrue();
        output.NextStage.Should().Be("TechLead");
        output.Notes.Should().Contain("Plan already complete");
    }
}
