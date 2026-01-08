using Microsoft.Agents.AI.Workflows;

namespace Orchestrator.App.Workflows;

/// <summary>
/// Output message from executors
/// </summary>
internal sealed record WorkflowOutput(
    bool Success,
    string Notes,
    WorkflowStage? NextStage = null
);

internal abstract class WorkflowStageExecutor : Executor<WorkflowInput, WorkflowOutput>
{
    protected WorkflowStageExecutor(string id) : base(id)
    {
    }

    protected abstract WorkflowStage Stage { get; }
    protected abstract string Notes { get; }

    public override ValueTask<WorkflowOutput> HandleAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new WorkflowOutput(
            Success: true,
            Notes: Notes,
            NextStage: WorkflowStageGraph.NextStageFor(Stage)
        ));
    }
}

internal sealed class ContextBuilderExecutor : WorkflowStageExecutor
{
    public ContextBuilderExecutor() : base("ContextBuilder")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.ContextBuilder;
    protected override string Notes => "Context builder placeholder executed.";
}

internal sealed class RefinementExecutor : WorkflowStageExecutor
{
    public RefinementExecutor() : base("Refinement")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Refinement;
    protected override string Notes => "Refinement placeholder executed.";
}

internal sealed class DorExecutor : WorkflowStageExecutor
{
    public DorExecutor() : base("DoR")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoR;
    protected override string Notes => "DoR placeholder executed.";
}

internal sealed class TechLeadExecutor : WorkflowStageExecutor
{
    public TechLeadExecutor() : base("TechLead")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.TechLead;
    protected override string Notes => "TechLead placeholder executed.";
}

internal sealed class SpecGateExecutor : WorkflowStageExecutor
{
    public SpecGateExecutor() : base("SpecGate")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.SpecGate;
    protected override string Notes => "Spec gate placeholder executed.";
}

internal sealed class DevExecutor : WorkflowStageExecutor
{
    public DevExecutor() : base("Dev")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Dev;
    protected override string Notes => "Dev placeholder executed.";
}

internal sealed class CodeReviewExecutor : WorkflowStageExecutor
{
    public CodeReviewExecutor() : base("CodeReview")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.CodeReview;
    protected override string Notes => "Code review placeholder executed.";
}

internal sealed class DodExecutor : WorkflowStageExecutor
{
    public DodExecutor() : base("DoD")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoD;
    protected override string Notes => "DoD placeholder executed.";
}

internal sealed class ReleaseExecutor : WorkflowStageExecutor
{
    public ReleaseExecutor() : base("Release")
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Release;
    protected override string Notes => "Release placeholder executed.";
}
