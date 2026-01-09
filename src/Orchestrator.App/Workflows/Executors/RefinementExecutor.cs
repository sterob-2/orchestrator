using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class RefinementExecutor : WorkflowStageExecutor
{
    public RefinementExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("Refinement", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Refinement;
    protected override string Notes => "Refinement completed.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.Info($"[Refinement] Starting refinement for issue #{input.WorkItem.Number}");
            Logger.Debug($"[Refinement] Checking for existing spec at: {WorkflowPaths.SpecPath(input.WorkItem.Number)}");

            var refinement = await BuildRefinementAsync(input, cancellationToken);

            Logger.Info($"[Refinement] Refinement complete: {refinement.AcceptanceCriteria.Count} criteria, {refinement.OpenQuestions.Count} questions");
            Logger.Debug($"[Refinement] Storing refinement result in workflow state");

            var serialized = WorkflowJson.Serialize(refinement);
            await context.QueueStateUpdateAsync(WorkflowStateKeys.RefinementResult, serialized, cancellationToken);
            WorkContext.State[WorkflowStateKeys.RefinementResult] = serialized;

            // Write refinement output to file
            var refinementPath = WorkflowPaths.RefinementPath(input.WorkItem.Number);
            Logger.Debug($"[Refinement] Writing refinement output to {refinementPath}");
            await WriteRefinementFileAsync(input.WorkItem, refinement, refinementPath);
            Logger.Info($"[Refinement] Wrote refinement output to {refinementPath}");

            // Commit the refinement file (best effort - don't fail workflow if git fails)
            try
            {
                var branchName = $"issue-{input.WorkItem.Number}";
                var commitMessage = $"refine: Update refinement for issue #{input.WorkItem.Number}\n\n" +
                                   $"- {refinement.AcceptanceCriteria.Count} acceptance criteria\n" +
                                   $"- {refinement.OpenQuestions.Count} open questions";

                Logger.Debug($"[Refinement] Committing {refinementPath} to branch '{branchName}'");
                var committed = WorkContext.Repo.CommitAndPush(branchName, commitMessage, new[] { refinementPath });

                if (committed)
                {
                    Logger.Info($"[Refinement] Committed and pushed refinement to branch '{branchName}'");
                }
                else
                {
                    Logger.Warning($"[Refinement] No changes to commit (file unchanged)");
                }
            }
            catch (LibGit2Sharp.LibGit2SharpException ex)
            {
                Logger.Warning($"[Refinement] Git commit failed (continuing anyway): {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warning($"[Refinement] Git commit failed (continuing anyway): {ex.Message}");
            }

            var summary = $"Refinement captured ({refinement.AcceptanceCriteria.Count} criteria, {refinement.OpenQuestions.Count} open questions).";
            return (true, summary);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Refinement] Failed with exception: {ex.GetType().Name}: {ex.Message}");
            Logger.Debug($"[Refinement] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task<RefinementResult> BuildRefinementAsync(WorkflowInput input, CancellationToken cancellationToken)
    {
        var workItem = input.WorkItem;
        var existingSpec = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.SpecPath(workItem.Number));
        Logger.Debug($"[Refinement] Existing spec found: {existingSpec != null}");
        if (existingSpec != null)
        {
            Logger.Debug($"[Refinement] Spec content length: {existingSpec.Length} chars, first 100 chars: {existingSpec.Substring(0, Math.Min(100, existingSpec.Length))}");
        }

        // Read previous refinement to see what questions were asked
        var previousRefinement = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.RefinementPath(workItem.Number));
        Logger.Debug($"[Refinement] Previous refinement found: {previousRefinement != null}");

        // Read issue comments to get answers
        var comments = (await WorkContext.GitHub.GetIssueCommentsAsync(workItem.Number))?.ToList() ?? new List<IssueComment>();
        Logger.Debug($"[Refinement] Fetched {comments.Count} issue comment(s)");

        // Check if we have an answer from TechLead or ProductOwner
        if (WorkContext.State.TryGetValue(WorkflowStateKeys.CurrentQuestionAnswer, out var answer) && !string.IsNullOrWhiteSpace(answer))
        {
            Logger.Info($"[Refinement] Incorporating answer from previous stage: {answer.Substring(0, Math.Min(100, answer.Length))}");

            // Add answer as synthetic comment for the LLM to process
            var syntheticComment = new IssueComment(
                Author: "orchestrator-bot",
                Body: $"**Answer from automated question routing:**\n\n{answer}"
            );
            comments.Add(syntheticComment);

            // Clear the answer from state after incorporating
            WorkContext.State.Remove(WorkflowStateKeys.CurrentQuestionAnswer, out _);
            Logger.Debug($"[Refinement] Cleared CurrentQuestionAnswer from state");
        }

        var playbookContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath) ?? "";
        Logger.Debug($"[Refinement] Playbook loaded: {playbookContent.Length} chars");

        var playbook = new PlaybookParser().Parse(playbookContent);
        var prompt = RefinementPrompt.Build(workItem, playbook, existingSpec, previousRefinement, comments);

        Logger.Debug($"[Refinement] Calling LLM with model: {WorkContext.Config.TechLeadModel}");
        var response = await CallLlmAsync(
            WorkContext.Config.TechLeadModel,
            prompt.System,
            prompt.User,
            cancellationToken);

        Logger.Debug($"[Refinement] LLM response received: {response.Length} chars");

        if (!WorkflowJson.TryDeserialize(response, out RefinementResult? result) || result is null)
        {
            Logger.Warning($"[Refinement] Failed to parse LLM response, using fallback");
            return RefinementPrompt.Fallback(workItem);
        }

        Logger.Debug($"[Refinement] Successfully parsed refinement result");
        return result;
    }

    private async Task WriteRefinementFileAsync(WorkItem workItem, RefinementResult refinement, string filePath)
    {
        var content = new System.Text.StringBuilder();
        content.AppendLine($"# Refinement: Issue #{workItem.Number} - {workItem.Title}");
        content.AppendLine();
        content.AppendLine($"**Status**: Refinement Complete");
        content.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        content.AppendLine();

        RefinementMarkdownBuilder.AppendClarifiedStory(content, refinement.ClarifiedStory);
        RefinementMarkdownBuilder.AppendAcceptanceCriteria(content, refinement.AcceptanceCriteria);

        if (refinement.OpenQuestions.Count > 0)
        {
            content.AppendLine($"## Open Questions ({refinement.OpenQuestions.Count})");
            content.AppendLine();
            content.AppendLine("**How to answer:**");
            content.AppendLine("1. Add a comment to the GitHub issue with your answers");
            content.AppendLine("2. Remove `blocked` and `user-review-required` labels");
            content.AppendLine("3. Add the `dor` label to re-trigger refinement");
            content.AppendLine();
            content.AppendLine("Refinement will read your comment, incorporate answers, and stop re-asking those questions.");
            content.AppendLine();
            RefinementMarkdownBuilder.AppendOpenQuestions(content, refinement.OpenQuestions);
        }

        await FileOperationHelper.WriteAllTextAsync(WorkContext, filePath, content.ToString());
    }

    protected override WorkflowStage? DetermineNextStage(bool success, WorkflowInput input)
    {
        if (!success)
        {
            return null;
        }

        // Get refinement result from state
        if (!WorkContext.State.TryGetValue(WorkflowStateKeys.RefinementResult, out var refinementJson))
        {
            Logger.Warning($"[Refinement] No refinement result in state");
            return null;
        }

        if (!WorkflowJson.TryDeserialize(refinementJson, out RefinementResult? refinement) || refinement is null)
        {
            Logger.Warning($"[Refinement] Failed to deserialize refinement result");
            return null;
        }

        // Check if there are open questions
        if (refinement.OpenQuestions.Count == 0)
        {
            Logger.Info($"[Refinement] No open questions, proceeding to DoR");
            return WorkflowStage.DoR;
        }

        // We have questions - implement one-at-a-time routing with 2 attempt limit
        var firstQuestion = refinement.OpenQuestions[0];
        var lastQuestion = WorkContext.State.TryGetValue(WorkflowStateKeys.LastProcessedQuestion, out var last) ? last : null;
        var attemptCount = WorkContext.State.TryGetValue(WorkflowStateKeys.QuestionAttemptCount, out var countStr) && int.TryParse(countStr, out var count) ? count : 0;

        if (firstQuestion == lastQuestion)
        {
            // Same question still exists after routing to TechLead/ProductOwner
            attemptCount++;
            Logger.Debug($"[Refinement] Question persists, incrementing attempt count to {attemptCount}");

            if (attemptCount >= 2)
            {
                // Failed after 2 attempts, block for human intervention
                Logger.Warning($"[Refinement] Question unresolved after 2 attempts: {firstQuestion}");
                Logger.Info($"[Refinement] Blocking workflow - human intervention required");
                return null; // Workflow will be blocked
            }
        }
        else
        {
            // Different question (or first time), reset counter
            attemptCount = 1;
            Logger.Debug($"[Refinement] New question detected, resetting attempt counter");
        }

        // Store tracking state
        WorkContext.State[WorkflowStateKeys.LastProcessedQuestion] = firstQuestion;
        WorkContext.State[WorkflowStateKeys.QuestionAttemptCount] = attemptCount.ToString();

        Logger.Info($"[Refinement] Routing question to classifier (attempt {attemptCount}/2): {firstQuestion.Substring(0, Math.Min(80, firstQuestion.Length))}...");
        return WorkflowStage.QuestionClassifier;
    }
}
