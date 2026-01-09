using System.Text;
using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class QuestionClassifierExecutor : WorkflowStageExecutor
{
    public QuestionClassifierExecutor(WorkContext workContext, WorkflowConfig workflowConfig)
        : base("QuestionClassifier", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.QuestionClassifier;
    protected override string Notes => "Question classified.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Info($"[QuestionClassifier] Classifying question for issue #{input.WorkItem.Number}");

        // Get refinement result
        var refinementJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.RefinementResult,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(refinementJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.RefinementResult, out var fallbackJson))
        {
            refinementJson = fallbackJson;
        }

        if (!WorkflowJson.TryDeserialize(refinementJson, out RefinementResult? refinement) || refinement is null)
        {
            Logger.Warning($"[QuestionClassifier] No refinement result found");
            return (false, "Question classification failed: missing refinement output.");
        }

        if (refinement.OpenQuestions.Count == 0)
        {
            Logger.Warning($"[QuestionClassifier] No open questions to classify");
            return (false, "Question classification failed: no open questions.");
        }

        // Get first question
        var question = refinement.OpenQuestions[0];
        Logger.Info($"[QuestionClassifier] Classifying: {question}");

        // Build prompt for classification
        var (systemPrompt, userPrompt) = BuildClassificationPrompt(question, input.WorkItem, refinement);

        // Call LLM
        Logger.Debug($"[QuestionClassifier] Calling LLM for classification");
        var response = await CallLlmAsync(
            WorkContext.Config.TechLeadModel,
            systemPrompt,
            userPrompt,
            cancellationToken);

        Logger.Debug($"[QuestionClassifier] LLM response: {response}");

        // Parse classification
        if (!WorkflowJson.TryDeserialize(response, out QuestionClassification? classification) || classification is null)
        {
            Logger.Warning($"[QuestionClassifier] Failed to parse LLM response, defaulting to Ambiguous");
            classification = new QuestionClassification(question, QuestionType.Ambiguous, "Failed to parse LLM response");
        }

        Logger.Info($"[QuestionClassifier] Question classified as: {classification.Type}");
        Logger.Debug($"[QuestionClassifier] Reasoning: {classification.Reasoning}");

        // Store classification result
        var result = new QuestionClassificationResult(classification);
        var serialized = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.QuestionClassificationResult, serialized, cancellationToken);
        WorkContext.State[WorkflowStateKeys.QuestionClassificationResult] = serialized;

        // Store the question itself for tracking
        await context.QueueStateUpdateAsync(WorkflowStateKeys.LastProcessedQuestion, question, cancellationToken);
        WorkContext.State[WorkflowStateKeys.LastProcessedQuestion] = question;

        return (true, $"Question classified as {classification.Type}.");
    }

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        if (!success)
        {
            return null;
        }

        // Get classification from state
        if (!WorkContext.State.TryGetValue(WorkflowStateKeys.QuestionClassificationResult, out var classificationJson))
        {
            Logger.Warning($"[QuestionClassifier] No classification result in state");
            return null;
        }

        if (!WorkflowJson.TryDeserialize(classificationJson, out QuestionClassificationResult? result) || result is null)
        {
            Logger.Warning($"[QuestionClassifier] Failed to deserialize classification result");
            return null;
        }

        // Route based on question type
        return result.Classification.Type switch
        {
            QuestionType.Technical => WorkflowStage.TechLead,
            QuestionType.Product => WorkflowStage.ProductOwner,
            QuestionType.Ambiguous => null, // Block - needs human intervention
            _ => null
        };
    }

    private static (string System, string User) BuildClassificationPrompt(string question, WorkItem workItem, RefinementResult refinement)
    {
        var system = "You are a question classifier for an SDLC workflow. " +
                     "Classify questions as Technical, Product, or Ambiguous. " +
                     "Return JSON only.";

        var builder = new StringBuilder();
        PromptBuilders.AppendIssueContext(builder, workItem, refinement);
        builder.AppendLine("Question to Classify:");
        builder.AppendLine(question);
        builder.AppendLine();
        builder.AppendLine("Classification Guidelines:");
        builder.AppendLine();
        builder.AppendLine("TECHNICAL questions are about:");
        builder.AppendLine("- Implementation details (how to code something)");
        builder.AppendLine("- Architecture decisions (which pattern, structure)");
        builder.AppendLine("- Framework/library choices (which tool to use)");
        builder.AppendLine("- Code organization (where to put files)");
        builder.AppendLine("- Error handling strategies");
        builder.AppendLine("- Performance considerations");
        builder.AppendLine("Examples: 'Which framework?', 'How should errors be handled?', 'What's the API structure?'");
        builder.AppendLine();
        builder.AppendLine("PRODUCT questions are about:");
        builder.AppendLine("- User workflows (how users interact)");
        builder.AppendLine("- Business logic (what should happen)");
        builder.AppendLine("- Use cases (when/why feature is used)");
        builder.AppendLine("- Requirements clarification (what exactly is needed)");
        builder.AppendLine("- User expectations (what users see/experience)");
        builder.AppendLine("- Feature behavior (how feature should work)");
        builder.AppendLine("Examples: 'What happens when user clicks X?', 'What's the expected behavior?', 'Which users can access this?'");
        builder.AppendLine();
        builder.AppendLine("AMBIGUOUS questions:");
        builder.AppendLine("- Can't be clearly classified");
        builder.AppendLine("- Require human judgment");
        builder.AppendLine("- Mix technical and product concerns");
        builder.AppendLine();
        builder.AppendLine("Return JSON:");
        builder.AppendLine("{");
        builder.AppendLine("  \"question\": string (the question being classified),");
        builder.AppendLine("  \"type\": \"Technical\" | \"Product\" | \"Ambiguous\",");
        builder.AppendLine("  \"reasoning\": string (brief explanation of classification)");
        builder.AppendLine("}");

        return (system, builder.ToString());
    }
}
