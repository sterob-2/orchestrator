using System.Text;
using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class ProductOwnerExecutor : WorkflowStageExecutor
{
    public ProductOwnerExecutor(WorkContext workContext, WorkflowConfig workflowConfig)
        : base("ProductOwner", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.ProductOwner;
    protected override string Notes => "Product question answered.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Info($"[ProductOwner] Answering product question for issue #{input.WorkItem.Number}");

        // Get classification to know which question to answer
        var classificationJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.QuestionClassificationResult,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(classificationJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.QuestionClassificationResult, out var fallbackJson))
        {
            classificationJson = fallbackJson;
        }

        if (!WorkflowJson.TryDeserialize(classificationJson, out QuestionClassificationResult? classificationResult) || classificationResult is null)
        {
            Logger.Warning($"[ProductOwner] No classification result found");
            return (false, "Product owner failed: missing classification.");
        }

        var question = classificationResult.Classification.Question;
        Logger.Info($"[ProductOwner] Answering: {question}");

        // Get refinement for context
        var refinementJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.RefinementResult,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(refinementJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.RefinementResult, out var refinementFallback))
        {
            refinementJson = refinementFallback;
        }

        if (!WorkflowJson.TryDeserialize(refinementJson, out RefinementResult? refinement) || refinement is null)
        {
            Logger.Warning($"[ProductOwner] No refinement result found");
            return (false, "Product owner failed: missing refinement.");
        }

        // Check for existing spec
        var existingSpec = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.SpecPath(input.WorkItem.Number));

        // Build prompt
        var (systemPrompt, userPrompt) = BuildProductOwnerPrompt(question, input.WorkItem, refinement, existingSpec);

        // Call LLM
        Logger.Debug($"[ProductOwner] Calling LLM for answer");
        var response = await CallLlmAsync(
            WorkContext.Config.TechLeadModel,
            systemPrompt,
            userPrompt,
            cancellationToken);

        Logger.Debug($"[ProductOwner] LLM response: {response}");

        // Parse answer
        if (!WorkflowJson.TryDeserialize(response, out ProductOwnerResult? result) || result is null)
        {
            Logger.Warning($"[ProductOwner] Failed to parse LLM response");
            return (false, "Product owner failed: could not parse answer.");
        }

        Logger.Info($"[ProductOwner] Answer generated");
        Logger.Debug($"[ProductOwner] Answer: {result.Answer}");

        // Store result
        var serialized = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.ProductOwnerResult, serialized, cancellationToken);
        WorkContext.State[WorkflowStateKeys.ProductOwnerResult] = serialized;

        // Store answer for Refinement to use
        await context.QueueStateUpdateAsync(WorkflowStateKeys.CurrentQuestionAnswer, result.Answer, cancellationToken);
        WorkContext.State[WorkflowStateKeys.CurrentQuestionAnswer] = result.Answer;

        return (true, "Product question answered.");
    }

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        // Always go back to Refinement with the answer
        return success ? WorkflowStage.Refinement : null;
    }

    private static (string System, string User) BuildProductOwnerPrompt(
        string question,
        WorkItem workItem,
        RefinementResult refinement,
        string? existingSpec)
    {
        var system = "You are a Product Owner in an SDLC workflow. " +
                     "Answer product/business questions based on requirements, use cases, and user workflows. " +
                     "Base your answers on the provided context. " +
                     "If you cannot answer confidently, say so explicitly. " +
                     "Return JSON only.";

        var builder = new StringBuilder();
        builder.AppendLine("Issue Context:");
        builder.AppendLine($"Title: {workItem.Title}");
        builder.AppendLine();
        builder.AppendLine("Original Requirements:");
        builder.AppendLine(workItem.Body);
        builder.AppendLine();
        builder.AppendLine("Clarified Story:");
        builder.AppendLine(refinement.ClarifiedStory);
        builder.AppendLine();
        builder.AppendLine("Acceptance Criteria:");
        foreach (var criterion in refinement.AcceptanceCriteria)
        {
            builder.AppendLine($"- {criterion}");
        }
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(existingSpec))
        {
            builder.AppendLine("Existing Specification:");
            builder.AppendLine(existingSpec);
            builder.AppendLine();
        }

        builder.AppendLine("Product Question:");
        builder.AppendLine(question);
        builder.AppendLine();
        builder.AppendLine("Guidelines:");
        builder.AppendLine("- Answer based on the requirements, acceptance criteria, and existing spec");
        builder.AppendLine("- Focus on user workflows, business logic, and expected behavior");
        builder.AppendLine("- Be specific and actionable");
        builder.AppendLine("- If the answer is not clear from context, state 'CANNOT_ANSWER' in reasoning");
        builder.AppendLine();
        builder.AppendLine("Return JSON:");
        builder.AppendLine("{");
        builder.AppendLine("  \"question\": string (repeat the question),");
        builder.AppendLine("  \"answer\": string (your answer to the question),");
        builder.AppendLine("  \"reasoning\": string (brief explanation or 'CANNOT_ANSWER' if unsure)");
        builder.AppendLine("}");

        return (system, builder.ToString());
    }
}
