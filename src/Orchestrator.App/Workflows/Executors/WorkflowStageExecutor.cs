using System.Diagnostics;
using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

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
    protected WorkContext WorkContext { get; }
    protected int CurrentAttempt { get; private set; }

    protected WorkflowStageExecutor(string id, WorkContext workContext, WorkflowConfig workflowConfig) : base(id)
    {
        WorkContext = workContext;
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
        CurrentAttempt = nextAttempt;
        WorkContext.Metrics?.RecordIteration(Stage, nextAttempt);

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

    protected async Task<string> CallLlmAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (WorkContext.Config.Debug)
        {
            var debugPath = $"orchestrator/prompts/issue-{WorkContext.WorkItem.Number}-{Stage}-attempt-{CurrentAttempt}.md";
            var debugContent = $"# System Prompt\n\n{systemPrompt}\n\n# User Prompt\n\n{userPrompt}";
            WorkContext.Workspace.WriteAllText(debugPath, debugContent);

            try
            {
                var branchName = WorkItemBranch.BuildBranchName(WorkContext.WorkItem);
                WorkContext.Repo.CommitAndPush(branchName, $"debug: prompt for {Stage} attempt {CurrentAttempt}", new[] { debugPath });
            }
            catch (Exception ex)
            {
                // Swallow error to not fail workflow if debug commit fails
                System.Console.WriteLine($"[WARN] Failed to commit debug file: {ex.Message}");
            }
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await WorkContext.Llm.GetUpdatedFileAsync(model, systemPrompt, userPrompt);
        stopwatch.Stop();

        if (WorkContext.Config.Debug)
        {
            var responsePath = $"orchestrator/prompts/issue-{WorkContext.WorkItem.Number}-{Stage}-attempt-{CurrentAttempt}-response.md";
            WorkContext.Workspace.WriteAllText(responsePath, response);

            try
            {
                var branchName = WorkItemBranch.BuildBranchName(WorkContext.WorkItem);
                WorkContext.Repo.CommitAndPush(branchName, $"debug: response for {Stage} attempt {CurrentAttempt}", new[] { responsePath });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[WARN] Failed to commit debug response file: {ex.Message}");
            }
        }

        WorkContext.Metrics?.RecordLlmCall(new LlmCallMetrics(
            Model: model,
            PromptChars: systemPrompt.Length + userPrompt.Length,
            CompletionChars: response.Length,
            ElapsedMilliseconds: stopwatch.Elapsed.TotalMilliseconds,
            Cost: null));

        return response;
    }

    /// <summary>
    /// Reads state with automatic fallback to WorkContext.State if workflow context returns empty
    /// </summary>
    protected async Task<string> ReadStateWithFallbackAsync(
        IWorkflowContext context,
        string stateKey,
        CancellationToken cancellationToken)
    {
        var value = await context.ReadOrInitStateAsync(
            stateKey,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(value) && WorkContext.State.TryGetValue(stateKey, out var fallbackValue))
        {
            value = fallbackValue;
        }

        return value;
    }

    private static int MaxIterationsForStage(WorkflowConfig config, WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.ContextBuilder => 1,
            WorkflowStage.Refinement or WorkflowStage.DoR => config.MaxRefinementIterations,
            WorkflowStage.QuestionClassifier or WorkflowStage.ProductOwner or WorkflowStage.TechnicalAdvisor => config.MaxRefinementIterations,
            WorkflowStage.TechLead or WorkflowStage.SpecGate => config.MaxTechLeadIterations,
            WorkflowStage.Dev => config.MaxDevIterations,
            WorkflowStage.CodeReview => config.MaxCodeReviewIterations,
            WorkflowStage.DoD => config.MaxDodIterations,
            WorkflowStage.Release => 1,
            _ => 1
        };
    }
}
