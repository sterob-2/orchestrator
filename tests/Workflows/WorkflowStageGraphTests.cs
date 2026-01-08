using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowStageGraphTests
{
    [Fact]
    public void StartStageFromLabels_ReturnsExpectedStage()
    {
        var config = MockWorkContext.CreateConfig();

        var workItemLabel = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.WorkItemLabel });
        var dorLabel = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.DorLabel });
        var devLabel = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.DevLabel });
        var codeReviewLabel = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.CodeReviewNeededLabel });
        var releaseLabel = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.ReleaseLabel });

        Assert.Equal(WorkflowStage.Refinement, WorkflowStageGraph.StartStageFromLabels(config.Labels, workItemLabel));
        Assert.Equal(WorkflowStage.DoR, WorkflowStageGraph.StartStageFromLabels(config.Labels, dorLabel));
        Assert.Equal(WorkflowStage.Dev, WorkflowStageGraph.StartStageFromLabels(config.Labels, devLabel));
        Assert.Equal(WorkflowStage.CodeReview, WorkflowStageGraph.StartStageFromLabels(config.Labels, codeReviewLabel));
        Assert.Equal(WorkflowStage.Release, WorkflowStageGraph.StartStageFromLabels(config.Labels, releaseLabel));
    }

    [Fact]
    public void NextStageFor_UsesSuccessAndFailureEdges()
    {
        Assert.Equal(WorkflowStage.Refinement, WorkflowStageGraph.NextStageFor(WorkflowStage.ContextBuilder));
        Assert.Equal(WorkflowStage.TechLead, WorkflowStageGraph.NextStageFor(WorkflowStage.DoR, success: true));
        Assert.Equal(WorkflowStage.Refinement, WorkflowStageGraph.NextStageFor(WorkflowStage.DoR, success: false));
        Assert.Equal(WorkflowStage.Dev, WorkflowStageGraph.NextStageFor(WorkflowStage.CodeReview, success: false));
        Assert.Equal(WorkflowStage.Release, WorkflowStageGraph.NextStageFor(WorkflowStage.DoD, success: true));
        Assert.Null(WorkflowStageGraph.NextStageFor(WorkflowStage.Release));
    }

    [Fact]
    public void ExecutorIdFor_MapsStages()
    {
        Assert.Equal("ContextBuilder", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.ContextBuilder));
        Assert.Equal("Refinement", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.Refinement));
        Assert.Equal("DoR", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.DoR));
        Assert.Equal("TechLead", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.TechLead));
        Assert.Equal("SpecGate", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.SpecGate));
        Assert.Equal("Dev", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.Dev));
        Assert.Equal("CodeReview", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.CodeReview));
        Assert.Equal("DoD", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.DoD));
        Assert.Equal("Release", WorkflowStageGraph.ExecutorIdFor(WorkflowStage.Release));
        Assert.Equal("Refinement", WorkflowStageGraph.ExecutorIdFor((WorkflowStage)999));
    }
}
