using System.Text;
using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class TechnicalAdvisorExecutor : LlmQuestionAnswerExecutor<TechnicalAdvisorResult>
{
    public TechnicalAdvisorExecutor(WorkContext workContext, WorkflowConfig workflowConfig)
        : base("TechnicalAdvisor", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.TechnicalAdvisor;
    protected override string Notes => "Technical question answered.";

    protected override (string System, string User) BuildPrompt(string question, WorkflowInput input)
    {
        // Get playbook for context
        var playbookContent = FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath)
            .GetAwaiter().GetResult() ?? "";
        var playbook = new PlaybookParser().Parse(playbookContent);

        // Get existing spec for context
        var existingSpec = FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.SpecPath(input.WorkItem.Number))
            .GetAwaiter().GetResult();

        return BuildTechnicalQuestionPrompt(question, input.WorkItem, playbook, existingSpec);
    }

    protected override string GetLlmModel() => WorkContext.Config.TechLeadModel;

    protected override bool TryParseAnswer(string? response, out TechnicalAdvisorResult? answer)
    {
        return WorkflowJson.TryDeserialize(response, out answer);
    }

    protected override async Task StoreAnswerAsync(
        TechnicalAdvisorResult answer,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Debug($"[TechnicalAdvisor] Answer: {answer.Answer}");

        // Store result
        var serialized = WorkflowJson.Serialize(answer);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.TechnicalAdvisorResult, serialized, cancellationToken);
        WorkContext.State[WorkflowStateKeys.TechnicalAdvisorResult] = serialized;

        // Store answer for Refinement to use
        await context.QueueStateUpdateAsync(WorkflowStateKeys.CurrentQuestionAnswer, answer.Answer, cancellationToken);
        WorkContext.State[WorkflowStateKeys.CurrentQuestionAnswer] = answer.Answer;
    }

    protected override string BuildSuccessNotes(TechnicalAdvisorResult answer)
    {
        return "Technical question answered.";
    }

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        // Always go back to Refinement with the answer
        return success ? WorkflowStage.Refinement : null;
    }

    private static (string System, string User) BuildTechnicalQuestionPrompt(
        string question,
        WorkItem workItem,
        Playbook playbook,
        string? existingSpec)
    {
        var system = "You are a Technical Lead in an SDLC workflow. " +
                     "Answer technical questions about implementation, architecture, patterns, and frameworks. " +
                     "Base your answers on best practices, playbook constraints, and existing specifications. " +
                     "If you cannot answer confidently, say so explicitly. " +
                     "Return JSON only.";

        var builder = new StringBuilder();
        PromptBuilders.AppendIssueContext(builder, workItem);

        if (!string.IsNullOrWhiteSpace(existingSpec))
        {
            builder.AppendLine("Existing Specification:");
            builder.AppendLine(existingSpec);
            builder.AppendLine();
        }

        PromptBuilders.AppendPlaybookConstraints(builder, playbook);

        builder.AppendLine("Technical Question:");
        builder.AppendLine(question);
        builder.AppendLine();
        builder.AppendLine("Guidelines:");
        builder.AppendLine("- Answer based on best practices, playbook constraints, and existing spec");
        builder.AppendLine("- Focus on implementation details, architecture, patterns, frameworks");
        builder.AppendLine("- Be specific and actionable");
        builder.AppendLine("- If the answer is not clear, state 'CANNOT_ANSWER' in reasoning");
        builder.AppendLine();
        PromptBuilders.AppendQuestionAnswerSchema(builder);

        return (system, builder.ToString());
    }
}
