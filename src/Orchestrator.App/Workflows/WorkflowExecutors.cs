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
    private readonly WorkflowConfig _workflowConfig;

    protected WorkflowStageExecutor(string id, WorkflowConfig workflowConfig) : base(id)
    {
        _workflowConfig = workflowConfig;
    }

    protected abstract WorkflowStage Stage { get; }
    protected abstract string Notes { get; }

    public override async ValueTask<WorkflowOutput> HandleAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var attemptKey = $"attempt:{Stage}";
        var currentAttempts = await context.ReadOrInitStateAsync(
            attemptKey,
            () => 0,
            cancellationToken: cancellationToken);
        var nextAttempt = currentAttempts + 1;
        await context.QueueStateUpdateAsync(attemptKey, nextAttempt, cancellationToken);

        var limit = MaxIterationsForStage(_workflowConfig, Stage);
        if (nextAttempt > limit)
        {
            return new WorkflowOutput(
                Success: false,
                Notes: $"Iteration limit reached for {Stage} ({nextAttempt}/{limit}).",
                NextStage: null);
        }

        var (success, notes) = await ExecuteAsync(input, context, cancellationToken);
        var nextStage = DetermineNextStage(success, input);
        if (nextStage is not null)
        {
            await context.SendMessageAsync(input, WorkflowStageGraph.ExecutorIdFor(nextStage.Value), cancellationToken);
        }

        return new WorkflowOutput(
            Success: success,
            Notes: notes,
            NextStage: nextStage);
    }

    protected virtual ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((true, Notes));
    }

    protected virtual WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        return WorkflowStageGraph.NextStageFor(Stage, success);
    }

    private static int MaxIterationsForStage(WorkflowConfig config, WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.ContextBuilder => 1,
            WorkflowStage.Refinement or WorkflowStage.DoR => config.MaxRefinementIterations,
            WorkflowStage.TechLead or WorkflowStage.SpecGate => config.MaxTechLeadIterations,
            WorkflowStage.Dev => config.MaxDevIterations,
            WorkflowStage.CodeReview => config.MaxCodeReviewIterations,
            WorkflowStage.DoD => config.MaxDodIterations,
            WorkflowStage.Release => 1,
            _ => 1
        };
    }
}

internal sealed class ContextBuilderExecutor : WorkflowStageExecutor
{
    private readonly LabelConfig _labels;
    private readonly WorkflowStage? _startOverride;

    public ContextBuilderExecutor(WorkflowConfig workflowConfig, LabelConfig labels, WorkflowStage? startOverride)
        : base("ContextBuilder", workflowConfig)
    {
        _labels = labels;
        _startOverride = startOverride is WorkflowStage.ContextBuilder ? null : startOverride;
    }

    protected override WorkflowStage Stage => WorkflowStage.ContextBuilder;
    protected override string Notes => "Context builder placeholder executed.";

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        if (_startOverride is not null)
        {
            return _startOverride;
        }

        return WorkflowStageGraph.StartStageFromLabels(_labels, input.WorkItem);
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
    public RefinementExecutor(WorkflowConfig workflowConfig) : base("Refinement", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Refinement;
    protected override string Notes => "Refinement placeholder executed.";
}

internal sealed class DorExecutor : WorkflowStageExecutor
{
    public DorExecutor(WorkflowConfig workflowConfig) : base("DoR", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoR;
    protected override string Notes => "DoR placeholder executed.";
}

internal sealed class TechLeadExecutor : WorkflowStageExecutor
{
    public TechLeadExecutor(WorkflowConfig workflowConfig) : base("TechLead", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.TechLead;
    protected override string Notes => "TechLead placeholder executed.";
}

internal sealed class SpecGateExecutor : WorkflowStageExecutor
{
    public SpecGateExecutor(WorkflowConfig workflowConfig) : base("SpecGate", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.SpecGate;
    protected override string Notes => "Spec gate placeholder executed.";
}

internal sealed class DevExecutor : WorkflowStageExecutor
{
    public DevExecutor(WorkflowConfig workflowConfig) : base("Dev", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Dev;
    protected override string Notes => "Dev placeholder executed.";
}

internal sealed class CodeReviewExecutor : WorkflowStageExecutor
{
    public CodeReviewExecutor(WorkflowConfig workflowConfig) : base("CodeReview", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.CodeReview;
    protected override string Notes => "Code review placeholder executed.";
}

internal sealed class DodExecutor : WorkflowStageExecutor
{
    public DodExecutor(WorkflowConfig workflowConfig) : base("DoD", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoD;
    protected override string Notes => "DoD placeholder executed.";
}

internal sealed class ReleaseExecutor : WorkflowStageExecutor
{
    public ReleaseExecutor(WorkflowConfig workflowConfig) : base("Release", workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Release;
    protected override string Notes => "Release placeholder executed.";
}
