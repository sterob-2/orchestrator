using System.Text;
using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class ProductOwnerExecutor : LlmQuestionAnswerExecutor<ProductOwnerResult>
{
    public ProductOwnerExecutor(WorkContext workContext, WorkflowConfig workflowConfig)
        : base("ProductOwner", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.ProductOwner;
    protected override string Notes => "Product question answered.";

    protected override async Task<(string? Question, string? FailureMessage)> GetQuestionAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Info($"[ProductOwner] Answering product question for issue #{input.WorkItem.Number}");

        // Get classification to know which question to answer
        var classificationJson = await ReadStateWithFallbackAsync(
            context,
            WorkflowStateKeys.QuestionClassificationResult,
            cancellationToken);

        if (!WorkflowJson.TryDeserialize(classificationJson, out QuestionClassificationResult? classificationResult) || classificationResult is null)
        {
            Logger.Warning($"[ProductOwner] No classification result found");
            return (null, "Product owner failed: missing classification.");
        }

        var question = classificationResult.Classification.Question;
        Logger.Info($"[ProductOwner] Answering: {question}");
        return (question, null);
    }

    protected override (string System, string User) BuildPrompt(string question, WorkflowInput input)
    {
        // Get refinement for context (already in state from previous stages)
        var refinementJson = WorkContext.State.GetValueOrDefault(WorkflowStateKeys.RefinementResult, string.Empty);
        WorkflowJson.TryDeserialize(refinementJson, out RefinementResult? refinement);

        // Check for existing spec
        var existingSpec = FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.SpecPath(input.WorkItem.Number))
            .GetAwaiter().GetResult();

        return BuildProductOwnerPrompt(question, input.WorkItem, refinement!, existingSpec);
    }

    protected override string GetLlmModel() => WorkContext.Config.TechLeadModel;

    protected override bool TryParseAnswer(string? response, out ProductOwnerResult? answer)
    {
        return WorkflowJson.TryDeserialize(response, out answer);
    }

    protected override async Task StoreAnswerAsync(
        ProductOwnerResult answer,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Debug($"[ProductOwner] Answer: {answer.Answer}");

        // Store result
        var serialized = WorkflowJson.Serialize(answer);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.ProductOwnerResult, serialized, cancellationToken);
        WorkContext.State[WorkflowStateKeys.ProductOwnerResult] = serialized;

        // Store answer for Refinement to use
        await context.QueueStateUpdateAsync(WorkflowStateKeys.CurrentQuestionAnswer, answer.Answer, cancellationToken);
        WorkContext.State[WorkflowStateKeys.CurrentQuestionAnswer] = answer.Answer;
    }

    protected override string BuildSuccessNotes(ProductOwnerResult answer)
    {
        return "Product question answered.";
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
        PromptBuilders.AppendIssueContext(builder, workItem, refinement, includeBody: false);
        builder.AppendLine("Original Requirements:");
        builder.AppendLine(workItem.Body);
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
