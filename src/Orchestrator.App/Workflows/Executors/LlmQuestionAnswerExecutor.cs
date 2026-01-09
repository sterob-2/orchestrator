using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

/// <summary>
/// Abstract base class for LLM-based question-answer executors (QuestionClassifier, ProductOwner, TechLead Q&A).
/// Provides common functionality for extracting questions, calling LLM, parsing responses, and storing answers.
/// </summary>
internal abstract class LlmQuestionAnswerExecutor<TAnswer> : WorkflowStageExecutor
{
    protected LlmQuestionAnswerExecutor(string executorName, WorkContext workContext, WorkflowConfig workflowConfig)
        : base(executorName, workContext, workflowConfig)
    {
    }

    protected sealed override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        // 1. Get the question to answer
        var questionResult = await GetQuestionAsync(input, context, cancellationToken);
        if (questionResult.FailureMessage != null)
        {
            return (false, questionResult.FailureMessage);
        }

        // 2. Build prompt (executor-specific)
        var (systemPrompt, userPrompt) = BuildPrompt(questionResult.Question!, input);

        // 3. Call LLM
        var llmModel = GetLlmModel();
        Logger.Debug($"[{Stage}] Calling LLM for answer");
        var response = await CallLlmAsync(llmModel, systemPrompt, userPrompt, cancellationToken);
        Logger.Debug($"[{Stage}] LLM response received");

        // 4. Parse response
        if (!TryParseAnswer(response, out var answer) || answer == null)
        {
            Logger.Warning($"[{Stage}] Failed to parse LLM response");
            return (false, $"{Stage} failed: could not parse answer.");
        }

        Logger.Info($"[{Stage}] Answer generated successfully");

        // 5. Store answer in state
        await StoreAnswerAsync(answer, context, cancellationToken);

        // 6. Return success
        var notes = BuildSuccessNotes(answer);
        return (true, notes);
    }

    /// <summary>
    /// Extract the question to answer. Return (question, null) on success or (null, errorMessage) on failure.
    /// </summary>
    protected abstract Task<(string? Question, string? FailureMessage)> GetQuestionAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Build the LLM prompt (system and user prompts) for answering the question.
    /// </summary>
    protected abstract (string System, string User) BuildPrompt(string question, WorkflowInput input);

    /// <summary>
    /// Get the LLM model to use for this executor.
    /// </summary>
    protected abstract string GetLlmModel();

    /// <summary>
    /// Try to parse the LLM response into a typed answer.
    /// </summary>
    protected abstract bool TryParseAnswer(string? response, out TAnswer? answer);

    /// <summary>
    /// Store the answer in workflow state.
    /// </summary>
    protected abstract Task StoreAnswerAsync(
        TAnswer answer,
        IWorkflowContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Build success notes for logging and state tracking.
    /// </summary>
    protected abstract string BuildSuccessNotes(TAnswer answer);
}
