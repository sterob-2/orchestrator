using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

/// <summary>
/// Abstract base class for gate executors (DoR, DoD, SpecGate).
/// Provides common functionality for validation, metrics, state management, and optional file/git/GitHub operations.
/// </summary>
internal abstract class GateExecutor<TInput> : WorkflowStageExecutor
{
    protected GateExecutor(string executorName, WorkContext workContext, WorkflowConfig workflowConfig)
        : base(executorName, workContext, workflowConfig)
    {
    }

    protected sealed override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        // 1. Load gate input (executor-specific)
        var loadResult = await LoadGateInputAsync(input, context, cancellationToken);
        if (!loadResult.Success)
        {
            return (false, loadResult.ErrorMessage!);
        }

        // 2. Evaluate gate
        var result = EvaluateGate(loadResult.Input, input);

        // 3. Record metrics on failure
        if (!result.Passed && result.Failures.Count > 0)
        {
            WorkContext.Metrics?.RecordGateFailures(result.Failures);
        }

        // 4. Post-validation operations (optional: file write, git commit, GitHub comment)
        if (!result.Passed)
        {
            await HandleGateFailureAsync(input, loadResult.Input, result, cancellationToken);
        }
        else
        {
            await HandleGateSuccessAsync(input, loadResult.Input, result, cancellationToken);
        }

        // 5. Store result in state
        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(GetResultStateKey(), serializedResult, cancellationToken);
        WorkContext.State[GetResultStateKey()] = serializedResult;

        // 6. Generate notes and return
        var notes = BuildResultNotes(result);
        return (result.Passed, notes);
    }

    /// <summary>
    /// Load gate-specific input data. Return (input, success=true, null) on success or (default, success=false, errorMessage) on failure.
    /// </summary>
    protected abstract Task<GateInputResult<TInput>> LoadGateInputAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken);

    protected readonly record struct GateInputResult<T>(T Input, bool Success, string? ErrorMessage)
    {
        public static GateInputResult<T> Ok(T input) => new(input, true, null);
        public static GateInputResult<T> Fail(string errorMessage) => new(default!, false, errorMessage);
    }

    /// <summary>
    /// Evaluate the gate and return a GateResult.
    /// </summary>
    protected abstract GateResult EvaluateGate(TInput gateInput, WorkflowInput workflowInput);

    /// <summary>
    /// Get the state key for storing the gate result.
    /// </summary>
    protected abstract string GetResultStateKey();

    /// <summary>
    /// Build result notes for logging and state tracking.
    /// </summary>
    protected abstract string BuildResultNotes(GateResult result);

    /// <summary>
    /// Optional: Handle gate failure (write file, commit, post comment). Default: no-op.
    /// </summary>
    protected virtual Task HandleGateFailureAsync(
        WorkflowInput input,
        TInput gateInput,
        GateResult result,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Optional: Handle gate success (e.g., update spec status). Default: no-op.
    /// </summary>
    protected virtual Task HandleGateSuccessAsync(
        WorkflowInput input,
        TInput gateInput,
        GateResult result,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper: Commit a file to git with error handling (best effort).
    /// </summary>
    protected Task<bool> TryCommitAndPushAsync(
        WorkItem workItem,
        string filePath,
        string commitMessage)
    {
        try
        {
            var branchName = $"issue-{workItem.Number}";
            Logger.Debug($"[{Stage}] Committing {filePath} to branch '{branchName}'");
            var committed = WorkContext.Repo.CommitAndPush(branchName, commitMessage, new[] { filePath });

            if (committed)
            {
                Logger.Info($"[{Stage}] Committed and pushed to branch '{branchName}'");
                return Task.FromResult(true);
            }
            else
            {
                Logger.Warning($"[{Stage}] No changes to commit (file unchanged)");
                return Task.FromResult(false);
            }
        }
        catch (LibGit2Sharp.LibGit2SharpException ex)
        {
            Logger.Warning($"[{Stage}] Git commit failed (continuing anyway): {ex.Message}");
            return Task.FromResult(false);
        }
        catch (InvalidOperationException ex)
        {
            Logger.Warning($"[{Stage}] Git commit failed (continuing anyway): {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Helper: Post a comment to GitHub with error handling (best effort).
    /// </summary>
    protected async Task<bool> TryPostGitHubCommentAsync(WorkItem workItem, string comment)
    {
        try
        {
            await WorkContext.GitHub.CommentOnWorkItemAsync(workItem.Number, comment);
            Logger.Info($"[{Stage}] Posted comment to GitHub issue #{workItem.Number}");
            return true;
        }
        catch (Octokit.ApiException ex)
        {
            Logger.Error($"[{Stage}] GitHub API error posting comment: {ex.Message}");
            return false;
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            Logger.Error($"[{Stage}] Network error posting comment to GitHub: {ex.Message}");
            return false;
        }
        catch (OperationCanceledException ex)
        {
            Logger.Error($"[{Stage}] Timeout posting comment to GitHub: {ex.Message}");
            return false;
        }
    }
}
