namespace Orchestrator.App.Tests.Workflows;

public class WorkflowCheckpointStoreTests
{
    [Fact]
    public void IncrementStage_IncrementsAndReturnsCount()
    {
        var store = new InMemoryWorkflowCheckpointStore();

        var first = store.IncrementStage(1, WorkflowStage.Dev);
        var second = store.IncrementStage(1, WorkflowStage.Dev);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, store.GetStageAttempts(1, WorkflowStage.Dev));
    }

    [Fact]
    public void Reset_RemovesStageAttempts()
    {
        var store = new InMemoryWorkflowCheckpointStore();

        store.IncrementStage(2, WorkflowStage.TechLead);
        store.Reset(2);

        Assert.Equal(0, store.GetStageAttempts(2, WorkflowStage.TechLead));
    }
}
