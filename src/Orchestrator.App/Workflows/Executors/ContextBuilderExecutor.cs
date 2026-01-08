using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class ContextBuilderExecutor : WorkflowStageExecutor
{
    private readonly LabelConfig _labels;
    private readonly WorkflowStage? _startOverride;

    public ContextBuilderExecutor(WorkContext workContext, WorkflowConfig workflowConfig, LabelConfig labels, WorkflowStage? startOverride)
        : base("ContextBuilder", workContext, workflowConfig)
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
