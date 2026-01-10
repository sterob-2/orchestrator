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

            var refinement = await BuildRefinementAsync(input, context, cancellationToken);

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

            // Check if we should block workflow due to ambiguous questions
            var ambiguousCount = refinement.AmbiguousQuestions?.Count ?? 0;
            if (refinement.OpenQuestions.Count == 0 && ambiguousCount > 0)
            {
                Logger.Warning($"[Refinement] No more answerable questions, but {ambiguousCount} ambiguous question(s) require human clarification");
                Logger.Info($"[Refinement] Posting GitHub comment and blocking workflow");

                // Post informational GitHub comment
                await PostAmbiguousQuestionsCommentAsync(refinement.AmbiguousQuestions!, cancellationToken);

                var blockingSummary = $"Blocked: {ambiguousCount} ambiguous question(s) require clarification.";
                return (false, blockingSummary);
            }

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

    private async Task<RefinementResult> BuildRefinementAsync(WorkflowInput input, IWorkflowContext context, CancellationToken cancellationToken)
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

        // Parse answered questions from previous refinement markdown file
        var previousAnsweredQuestions = new List<AnsweredQuestion>();
        if (previousRefinement != null)
        {
            var parsedAnswers = RefinementMarkdownParser.ParseAnsweredQuestions(previousRefinement);
            previousAnsweredQuestions.AddRange(parsedAnswers);
            Logger.Debug($"[Refinement] Parsed {previousAnsweredQuestions.Count} answered question(s) from markdown");
        }

        // Load ambiguous questions from previous refinement result
        var previousAmbiguousQuestions = new List<OpenQuestion>();
        var previousOpenQuestions = new List<OpenQuestion>();
        var prevRefinementJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.RefinementResult,
            () => "",
            cancellationToken);

        Logger.Debug($"[Refinement] Previous refinement JSON from state: {(string.IsNullOrEmpty(prevRefinementJson) ? "EMPTY" : $"{prevRefinementJson.Length} chars")}");

        if (!string.IsNullOrEmpty(prevRefinementJson))
        {
            if (WorkflowJson.TryDeserialize(prevRefinementJson, out RefinementResult? prevResult))
            {
                Logger.Debug($"[Refinement] Deserialized previous refinement: {prevResult?.OpenQuestions.Count ?? 0} open, {prevResult?.AnsweredQuestions?.Count ?? 0} answered, {prevResult?.AmbiguousQuestions?.Count ?? 0} ambiguous");

                if (prevResult?.AmbiguousQuestions != null)
                {
                    previousAmbiguousQuestions.AddRange(prevResult.AmbiguousQuestions);
                    Logger.Info($"[Refinement] Loaded {previousAmbiguousQuestions.Count} ambiguous question(s) from previous state");
                    foreach (var aq in previousAmbiguousQuestions)
                    {
                        Logger.Debug($"[Refinement]   - Ambiguous Q#{aq.QuestionNumber}: {aq.Question.Substring(0, Math.Min(60, aq.Question.Length))}...");
                    }
                }
                else
                {
                    Logger.Debug($"[Refinement] No ambiguous questions in previous refinement result");
                }

                if (prevResult?.OpenQuestions != null)
                {
                    previousOpenQuestions.AddRange(prevResult.OpenQuestions);
                    Logger.Debug($"[Refinement] Loaded {previousOpenQuestions.Count} open question(s) from previous state");
                }
            }
            else
            {
                Logger.Warning($"[Refinement] Failed to deserialize previous refinement JSON");
            }
        }
        else if (previousRefinement != null)
        {
            // State is empty but markdown file exists - parse it to recover state
            Logger.Info($"[Refinement] State empty but markdown exists, parsing to recover state");

            var parsedOpenQuestions = RefinementMarkdownParser.ParseOpenQuestions(previousRefinement);
            previousOpenQuestions.AddRange(parsedOpenQuestions);
            Logger.Info($"[Refinement] Parsed {parsedOpenQuestions.Count} open question(s) from markdown");

            var parsedAmbiguousQuestions = RefinementMarkdownParser.ParseAmbiguousQuestions(previousRefinement);
            previousAmbiguousQuestions.AddRange(parsedAmbiguousQuestions);
            Logger.Info($"[Refinement] Parsed {parsedAmbiguousQuestions.Count} ambiguous question(s) from markdown");
        }
        else
        {
            Logger.Debug($"[Refinement] No previous refinement result in state or markdown file");
        }

        // Check if we have an answer from ProductOwner or TechnicalAdvisor
        // Within-workflow: check WorkContext.State first (set by previous stage in same workflow run)
        // Cross-workflow: fall back to IWorkflowContext (persisted from previous workflow run)
        var answer = WorkContext.State.TryGetValue(WorkflowStateKeys.CurrentQuestionAnswer, out var inMemoryAnswer)
            ? inMemoryAnswer
            : await context.ReadOrInitStateAsync(WorkflowStateKeys.CurrentQuestionAnswer, () => "", cancellationToken);

        if (!string.IsNullOrWhiteSpace(answer))
        {
            Logger.Debug($"[Refinement] Found answer from previous stage ({answer.Length} chars)");

            // Determine which executor provided the answer
            var productOwnerResult = WorkContext.State.TryGetValue(WorkflowStateKeys.ProductOwnerResult, out var inMemoryPO)
                ? inMemoryPO
                : await context.ReadOrInitStateAsync(WorkflowStateKeys.ProductOwnerResult, () => "", cancellationToken);
            var techAdvisorResult = WorkContext.State.TryGetValue(WorkflowStateKeys.TechnicalAdvisorResult, out var inMemoryTA)
                ? inMemoryTA
                : await context.ReadOrInitStateAsync(WorkflowStateKeys.TechnicalAdvisorResult, () => "", cancellationToken);

            var answerSource = !string.IsNullOrEmpty(productOwnerResult) ? "ProductOwner"
                : !string.IsNullOrEmpty(techAdvisorResult) ? "TechnicalAdvisor"
                : "unknown";

            // Get the question number that was answered
            var qNumStr = WorkContext.State.TryGetValue(WorkflowStateKeys.LastProcessedQuestionNumber, out var inMemoryQNumAns)
                ? inMemoryQNumAns
                : await context.ReadOrInitStateAsync(WorkflowStateKeys.LastProcessedQuestionNumber, () => "0", cancellationToken);
            var questionNumber = int.TryParse(qNumStr, out var qNumInt) ? qNumInt : 0;

            var answeredQuestion = WorkContext.State.TryGetValue(WorkflowStateKeys.LastProcessedQuestion, out var inMemoryQTextAns)
                ? inMemoryQTextAns
                : await context.ReadOrInitStateAsync(WorkflowStateKeys.LastProcessedQuestion, () => "unknown question", cancellationToken);

            Logger.Info($"[Refinement] Incorporating answer from {answerSource}");
            Logger.Info($"[Refinement] Question #{questionNumber}: {answeredQuestion.Substring(0, Math.Min(80, answeredQuestion.Length))}...");
            Logger.Info($"[Refinement] Answer preview: {answer.Substring(0, Math.Min(100, answer.Length))}...");
            Logger.Debug($"[Refinement] Full answer: {answer}");

            // Add to answered questions history - will be written to markdown file
            if (questionNumber > 0)
            {
                var answeredQuestionEntry = new AnsweredQuestion(
                    QuestionNumber: questionNumber,
                    Question: answeredQuestion,
                    Answer: answer,
                    AnsweredBy: answerSource
                );
                previousAnsweredQuestions.Add(answeredQuestionEntry);
                Logger.Debug($"[Refinement] Added answered question to history (total: {previousAnsweredQuestions.Count})");
            }

            // Clear the answer from state after incorporating
            await context.QueueStateUpdateAsync(WorkflowStateKeys.CurrentQuestionAnswer, "", cancellationToken);
            WorkContext.State.Remove(WorkflowStateKeys.CurrentQuestionAnswer, out _);
            Logger.Debug($"[Refinement] Cleared CurrentQuestionAnswer from state");

            // Reset stage attempt counter - we made progress by incorporating an answer
            await context.QueueStateUpdateAsync($"attempt:{WorkflowStage.Refinement}", 0, cancellationToken);
            Logger.Debug($"[Refinement] Reset stage attempt counter after incorporating answer");
        }
        // Check if the last question was classified as Ambiguous
        // Within-workflow: check WorkContext.State first (set by previous stage in same workflow run)
        // Cross-workflow: fall back to IWorkflowContext (persisted from previous workflow run)
        var classificationJson = WorkContext.State.TryGetValue(WorkflowStateKeys.QuestionClassificationResult, out var inMemoryClassification)
            ? inMemoryClassification
            : await context.ReadOrInitStateAsync(WorkflowStateKeys.QuestionClassificationResult, () => "", cancellationToken);

        if (!string.IsNullOrEmpty(classificationJson))
        {
            Logger.Debug($"[Refinement] Found classification result ({classificationJson.Length} chars)");
            if (WorkflowJson.TryDeserialize(classificationJson, out QuestionClassificationResult? classificationResult) &&
                classificationResult?.Classification.Type == QuestionType.Ambiguous)
            {
                // Get the ambiguous question details
                var qNumStr = WorkContext.State.TryGetValue(WorkflowStateKeys.LastProcessedQuestionNumber, out var inMemoryQNum)
                    ? inMemoryQNum
                    : await context.ReadOrInitStateAsync(WorkflowStateKeys.LastProcessedQuestionNumber, () => "0", cancellationToken);
                var questionNumber = int.TryParse(qNumStr, out var qNumInt) ? qNumInt : 0;

                var questionText = WorkContext.State.TryGetValue(WorkflowStateKeys.LastProcessedQuestion, out var inMemoryQText)
                    ? inMemoryQText
                    : await context.ReadOrInitStateAsync(WorkflowStateKeys.LastProcessedQuestion, () => "", cancellationToken);

                if (questionNumber > 0 && !string.IsNullOrEmpty(questionText))
                {
                    var ambiguousQuestion = new OpenQuestion(questionNumber, questionText);

                    // Check if not already in ambiguous list
                    if (!previousAmbiguousQuestions.Any(q => q.QuestionNumber == questionNumber))
                    {
                        previousAmbiguousQuestions.Add(ambiguousQuestion);
                        Logger.Info($"[Refinement] Question #{questionNumber} classified as Ambiguous, moving to ambiguous questions list");
                        Logger.Info($"[Refinement] Total ambiguous questions now: {previousAmbiguousQuestions.Count}");
                        Logger.Debug($"[Refinement] Reasoning: {classificationResult.Classification.Reasoning}");
                    }
                    else
                    {
                        Logger.Debug($"[Refinement] Question #{questionNumber} already in ambiguous list, skipping");
                    }
                }

                // Clear classification from state (both in-memory and persistent)
                await context.QueueStateUpdateAsync(WorkflowStateKeys.QuestionClassificationResult, "", cancellationToken);
                WorkContext.State.Remove(WorkflowStateKeys.QuestionClassificationResult, out _);
                WorkContext.State.Remove(WorkflowStateKeys.LastProcessedQuestion, out _);
                WorkContext.State.Remove(WorkflowStateKeys.LastProcessedQuestionNumber, out _);
                Logger.Debug($"[Refinement] Cleared QuestionClassificationResult from state");

                // Reset stage attempt counter - we made progress by classifying a question as ambiguous
                await context.QueueStateUpdateAsync($"attempt:{WorkflowStage.Refinement}", 0, cancellationToken);
                Logger.Debug($"[Refinement] Reset stage attempt counter after classifying question as ambiguous");
            }
        }
        else
        {
            Logger.Debug($"[Refinement] No answer from previous stage to incorporate");
        }

        var playbookContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath) ?? "";
        Logger.Debug($"[Refinement] Playbook loaded: {playbookContent.Length} chars");

        var playbook = new PlaybookParser().Parse(playbookContent);
        var prompt = RefinementPrompt.Build(workItem, playbook, existingSpec, previousRefinement, previousAnsweredQuestions);

        Logger.Debug($"[Refinement] Calling LLM with model: {WorkContext.Config.TechLeadModel}");
        var response = await CallLlmAsync(
            WorkContext.Config.TechLeadModel,
            prompt.System,
            prompt.User,
            cancellationToken);

        Logger.Debug($"[Refinement] LLM response received: {response.Length} chars");

        // Parse LLM response (questions as strings)
        if (!WorkflowJson.TryDeserialize(response, out RefinementLlmResult? llmResult) || llmResult is null)
        {
            Logger.Warning($"[Refinement] Failed to parse LLM response, using fallback");
            return RefinementPrompt.Fallback(workItem);
        }

        Logger.Debug($"[Refinement] Successfully parsed refinement result");

        // Clean question text - strip any "Question #X:" prefix that LLM might have added
        var cleanedQuestionStrings = llmResult.OpenQuestions
            .Select(q => System.Text.RegularExpressions.Regex.Replace(q.Trim(), @"^Question\s+#\d+:\s*", "", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)))
            .ToList();

        // previousOpenQuestions already loaded earlier from state or markdown file

        // Filter out questions that match ambiguous questions (to prevent duplicates)
        var ambiguousQuestionTexts = previousAmbiguousQuestions.Select(q => q.Question.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filteredQuestionStrings = cleanedQuestionStrings
            .Where(q => !ambiguousQuestionTexts.Contains(q.Trim()))
            .ToList();

        Logger.Debug($"[Refinement] Filtered {cleanedQuestionStrings.Count - filteredQuestionStrings.Count} question(s) matching ambiguous questions");

        // Exclude ambiguous questions from previousOpenQuestions (they've moved to ambiguous list)
        var ambiguousQuestionNumbers = previousAmbiguousQuestions.Select(q => q.QuestionNumber).ToHashSet();
        var filteredPreviousOpenQuestions = previousOpenQuestions
            .Where(q => !ambiguousQuestionNumbers.Contains(q.QuestionNumber))
            .ToList();

        // Assign stable question numbers
        var openQuestionsWithNumbers = AssignStableQuestionNumbers(
            filteredQuestionStrings,
            filteredPreviousOpenQuestions,
            previousAnsweredQuestions,
            previousAmbiguousQuestions);

        Logger.Debug($"[Refinement] Assigned question numbers: {string.Join(", ", openQuestionsWithNumbers.Select(q => $"#{q.QuestionNumber}"))}");

        // Create final result with stable question numbers
        var result = new RefinementResult(
            ClarifiedStory: llmResult.ClarifiedStory,
            AcceptanceCriteria: llmResult.AcceptanceCriteria,
            OpenQuestions: openQuestionsWithNumbers,
            Complexity: llmResult.Complexity,
            AnsweredQuestions: previousAnsweredQuestions,
            AmbiguousQuestions: previousAmbiguousQuestions
        );

        Logger.Info($"[Refinement] Final result: {openQuestionsWithNumbers.Count} open, {previousAnsweredQuestions.Count} answered, {previousAmbiguousQuestions.Count} ambiguous");
        foreach (var oq in openQuestionsWithNumbers)
        {
            Logger.Debug($"[Refinement]   - Open Q#{oq.QuestionNumber}: {oq.Question.Substring(0, Math.Min(60, oq.Question.Length))}...");
        }

        return result;
    }

    private static List<OpenQuestion> AssignStableQuestionNumbers(
        List<string> questionTexts,
        List<OpenQuestion> previousOpenQuestions,
        List<AnsweredQuestion> answeredQuestions,
        List<OpenQuestion> ambiguousQuestions)
    {
        // Find the highest question number used so far
        var maxNumber = 0;
        if (previousOpenQuestions.Count > 0)
            maxNumber = Math.Max(maxNumber, previousOpenQuestions.Max(q => q.QuestionNumber));
        if (answeredQuestions.Count > 0)
            maxNumber = Math.Max(maxNumber, answeredQuestions.Max(q => q.QuestionNumber));
        if (ambiguousQuestions.Count > 0)
            maxNumber = Math.Max(maxNumber, ambiguousQuestions.Max(q => q.QuestionNumber));

        var result = new List<OpenQuestion>();
        foreach (var questionText in questionTexts)
        {
            // Try to find this question in previous open questions (reuse number)
            var existing = previousOpenQuestions.FirstOrDefault(q =>
                string.Equals(q.Question.Trim(), questionText.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Reuse existing number
                result.Add(new OpenQuestion(existing.QuestionNumber, questionText));
            }
            else
            {
                // Assign new number
                maxNumber++;
                result.Add(new OpenQuestion(maxNumber, questionText));
            }
        }

        return result;
    }

    private async Task PostAmbiguousQuestionsCommentAsync(
        IReadOnlyList<OpenQuestion> ambiguousQuestions,
        CancellationToken cancellationToken)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## ⚠️ Ambiguous Questions Require Clarification");
        sb.AppendLine();
        sb.AppendLine($"The following {ambiguousQuestions.Count} question(s) mix product and technical concerns:");
        sb.AppendLine();

        foreach (var question in ambiguousQuestions)
        {
            sb.AppendLine($"**Question #{question.QuestionNumber}:** {question.Question}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("**Next Steps:**");
        sb.AppendLine("1. Review the questions in the refinement markdown file");
        sb.AppendLine("2. Provide answers using the checkbox format:");
        sb.AppendLine("   ```");
        sb.AppendLine("   - [x] Your answer here");
        sb.AppendLine("   ```");
        sb.AppendLine("3. Remove the `blocked` label when ready");
        sb.AppendLine();
        sb.AppendLine("Workflow will resume automatically once `blocked` label is removed.");

        await WorkContext.GitHub.CommentOnWorkItemAsync(
            WorkContext.WorkItem.Number,
            sb.ToString());
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

        // Combined questions section with checkboxes
        if (refinement.OpenQuestions.Count > 0 || (refinement.AnsweredQuestions != null && refinement.AnsweredQuestions.Count > 0))
        {
            content.AppendLine("## Questions");
            content.AppendLine();
            content.AppendLine("**How to answer:**");
            content.AppendLine("1. Edit this file and add your answer after the question");
            content.AppendLine("2. Mark the checkbox with [x] when answered");
            content.AppendLine("3. Commit and push changes");
            content.AppendLine("4. Remove `blocked` label and add `dor` label to re-trigger");
            content.AppendLine();
            RefinementMarkdownBuilder.AppendQuestions(content, refinement.OpenQuestions, refinement.AnsweredQuestions);
        }

        // Ambiguous questions require human clarification
        if (refinement.AmbiguousQuestions != null && refinement.AmbiguousQuestions.Count > 0)
        {
            RefinementMarkdownBuilder.AppendAmbiguousQuestions(content, refinement.AmbiguousQuestions);
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
            // No more open questions - ambiguous question blocking is handled in ExecuteAsync
            Logger.Info($"[Refinement] No open questions, proceeding to DoR");
            return WorkflowStage.DoR;
        }

        // We have questions - implement one-at-a-time routing with 2 attempt limit
        var firstQuestion = refinement.OpenQuestions[0];
        var lastQuestionText = WorkContext.State.TryGetValue(WorkflowStateKeys.LastProcessedQuestion, out var last) ? last : null;
        var attemptCount = WorkContext.State.TryGetValue(WorkflowStateKeys.QuestionAttemptCount, out var countStr) && int.TryParse(countStr, out var count) ? count : 0;

        if (firstQuestion.Question == lastQuestionText)
        {
            // Same question still exists after routing to TechLead/ProductOwner
            attemptCount++;
            Logger.Debug($"[Refinement] Question persists, incrementing attempt count to {attemptCount}");

            if (attemptCount >= 2)
            {
                // Failed after 2 attempts, block for human intervention
                Logger.Warning($"[Refinement] Question unresolved after 2 attempts: {firstQuestion.Question}");
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
        WorkContext.State[WorkflowStateKeys.LastProcessedQuestion] = firstQuestion.Question;
        WorkContext.State[WorkflowStateKeys.QuestionAttemptCount] = attemptCount.ToString();

        Logger.Info($"[Refinement] Routing question to classifier (attempt {attemptCount}/2): Question #{firstQuestion.QuestionNumber}: {firstQuestion.Question.Substring(0, Math.Min(80, firstQuestion.Question.Length))}...");
        return WorkflowStage.QuestionClassifier;
    }
}
