using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class DevExecutor : WorkflowStageExecutor
{
    public DevExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("Dev", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.Dev;
    protected override string Notes => "Dev implementation completed.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.Info($"[Dev] Starting development for issue #{input.WorkItem.Number}");
            Logger.Debug($"[Dev] Mode: {input.Mode}, Attempt: {input.Attempt}");

            var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
            Logger.Debug($"[Dev] Reading spec from: {specPath}");
            var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath);
            if (string.IsNullOrWhiteSpace(specContent))
            {
                return (false, $"Dev blocked: missing spec at {specPath}.");
            }

            var parsedSpec = new SpecParser().Parse(specContent);
            var forbidden = parsedSpec.TouchList.FirstOrDefault(entry => entry.Operation == TouchOperation.Forbidden);
            if (forbidden != null)
            {
                return (false, $"Dev blocked: forbidden path in touch list ({forbidden.Path}).");
            }

            var mode = ResolveMode(input);

            // Ensure branch exists BEFORE modifying files
            var branchName = WorkItemBranch.BuildBranchName(input.WorkItem);
            Logger.Debug($"[Dev] Ensuring branch: {branchName}");
            WorkContext.Repo.EnsureBranch(branchName, WorkContext.Config.Workflow.DefaultBaseBranch);

            var changedFiles = new List<string>();
            foreach (var entry in parsedSpec.TouchList)
        {
            Logger.Debug($"[Dev] Processing touch list entry: {entry.Operation} | {entry.Path}");

            if (!WorkItemParsers.IsSafeRelativePath(entry.Path))
            {
                Logger.Warning($"[Dev] Unsafe path detected: {entry.Path}");
                return (false, $"Dev blocked: unsafe path {entry.Path}.");
            }

            // Skip directory entries - only process files
            if (entry.Path.EndsWith('/') || entry.Path.EndsWith('\\'))
            {
                Logger.Debug($"[Dev] Skipping directory entry: {entry.Path}");
                continue;
            }

            switch (entry.Operation)
            {
                case TouchOperation.Add:
                case TouchOperation.Modify:
                    Logger.Debug($"[Dev] Reading existing content for: {entry.Path}");
                    string? existing = null;
                    try
                    {
                        existing = entry.Operation == TouchOperation.Modify
                            ? await FileOperationHelper.ReadAllTextAsync(WorkContext, entry.Path)
                            : null;
                        Logger.Debug($"[Dev] Read {existing?.Length ?? 0} characters from: {entry.Path}");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"[Dev] ERROR reading file {entry.Path}: {ex}");
                        throw;
                    }
                    Logger.Debug($"[Dev] Building prompt for: {entry.Path}");
                    var prompt = DevPrompt.Build(mode, parsedSpec, entry, existing);
                    Logger.Debug($"[Dev] Calling LLM for: {entry.Path}");
                    var updated = await CallLlmAsync(
                        WorkContext.Config.DevModel,
                        prompt.System,
                        prompt.User,
                        cancellationToken);
                    Logger.Debug($"[Dev] LLM response received for: {entry.Path} (length: {updated?.Length ?? 0})");

                    // Reflection loop: If LLM returned unchanged file, try again with explicit feedback
                    if (entry.Operation == TouchOperation.Modify && existing != null &&
                        !string.IsNullOrWhiteSpace(updated) &&
                        string.Equals(updated.Trim(), existing.Trim(), StringComparison.Ordinal))
                    {
                        Logger.Warning($"[Dev] LLM returned unchanged file on first attempt for: {entry.Path}");
                        Logger.Warning($"[Dev] Retrying with explicit feedback about the failure");

                        var reflectionPrompt = $"CRITICAL ERROR: Your previous response was IDENTICAL to the original file.\n" +
                                             $"You MUST make the changes specified in the instructions.\n" +
                                             $"Instructions: {entry.Notes}\n\n" +
                                             $"BEFORE (original file - DO NOT return this):\n{existing}\n\n" +
                                             $"You must output the MODIFIED version with the required changes applied.\n" +
                                             $"If removing methods, those methods MUST BE ABSENT from your output.";

                        updated = await CallLlmAsync(
                            WorkContext.Config.DevModel,
                            prompt.System,
                            reflectionPrompt,
                            cancellationToken);
                        Logger.Debug($"[Dev] Reflection attempt response received for: {entry.Path} (length: {updated?.Length ?? 0})");
                    }

                    // Debug: Log first and last 500 chars of LLM response for inspection
                    if (updated != null && updated.Length > 0)
                    {
                        var preview = updated.Length > 500 ? updated.Substring(0, 500) + "..." : updated;
                        Logger.Debug($"[Dev] LLM response preview for {entry.Path}: {preview}");

                        var endPreview = updated.Length > 500 ? "..." + updated.Substring(updated.Length - 500) : "";
                        if (!string.IsNullOrEmpty(endPreview))
                        {
                            Logger.Debug($"[Dev] LLM response end for {entry.Path}: {endPreview}");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(updated))
                    {
                        Logger.Warning($"[Dev] Empty LLM output for: {entry.Path}");
                        return (false, $"Dev blocked: empty output for {entry.Path}.");
                    }

                    // Validate: For MODIFY operations, check if LLM actually made changes
                    if (entry.Operation == TouchOperation.Modify && existing != null)
                    {
                        if (string.Equals(updated.Trim(), existing.Trim(), StringComparison.Ordinal))
                        {
                            Logger.Warning($"[Dev] LLM returned unchanged file for: {entry.Path}");
                            Logger.Warning($"[Dev] Expected: Code removal/modification, Got: Identical content");
                            return (false, $"Dev blocked: LLM did not modify {entry.Path} as instructed. File content unchanged.");
                        }
                    }
                    Logger.Debug($"[Dev] Writing updated content to: {entry.Path}");
                    await FileOperationHelper.WriteAllTextAsync(WorkContext, entry.Path, updated);
                    Logger.Debug($"[Dev] File written successfully: {entry.Path}");

                    // Debug: Check if specific methods were actually removed
                    if (entry.Path.Contains("IGitHubClient.cs") || entry.Path.Contains("OctokitGitHubClient.cs"))
                    {
                        var hasCreateBranch = updated.Contains("CreateBranchAsync");
                        var hasDeleteBranch = updated.Contains("DeleteBranchAsync");
                        Logger.Warning($"[Dev] Method check for {entry.Path}: CreateBranchAsync={hasCreateBranch}, DeleteBranchAsync={hasDeleteBranch}");

                        if (hasCreateBranch || hasDeleteBranch)
                        {
                            Logger.Warning($"[Dev] LLM FAILED to remove methods from {entry.Path}!");
                        }
                    }

                    changedFiles.Add(entry.Path);
                    break;
                case TouchOperation.Delete:
                    Logger.Debug($"[Dev] Deleting file: {entry.Path}");
                    await FileOperationHelper.DeleteAsync(WorkContext, entry.Path);
                    changedFiles.Add(entry.Path);
                    break;
            }
        }

        var updatedSpec = WorkItemParsers.MarkAcceptanceCriteriaDone(specContent);
        if (!string.Equals(updatedSpec, specContent, StringComparison.Ordinal))
        {
            await FileOperationHelper.WriteAllTextAsync(WorkContext, specPath, updatedSpec);
            changedFiles.Add(specPath);
        }

        // Include debug files in the commit if any were generated
        if (GeneratedDebugFiles.Count > 0)
        {
            Logger.Debug($"[Dev] Including {GeneratedDebugFiles.Count} debug file(s) in commit");
            changedFiles.AddRange(GeneratedDebugFiles);
        }

        Logger.Info($"[Dev] Processed {changedFiles.Count} file(s)");

        Logger.Debug($"[Dev] Committing and pushing {changedFiles.Count} changed file(s)");
        var commitOk = WorkContext.Repo.CommitAndPush(branchName, $"feat: issue {input.WorkItem.Number}", changedFiles);
        Logger.Info($"[Dev] Commit result: {(commitOk ? "SUCCESS" : "FAILED")}");

        var result = new DevResult(commitOk, input.WorkItem.Number, changedFiles, "");
        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.DevResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.DevResult] = serializedResult;

            return commitOk
                ? (true, $"Dev changes committed on {branchName}.")
                : (false, "Dev changes could not be committed.");
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"[Dev] EXCEPTION during execution for issue #{input.WorkItem.Number}: {ex}");
            throw;
        }
    }

    private string ResolveMode(WorkflowInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.Mode))
        {
            return input.Mode;
        }

        var labels = WorkContext.WorkItem.Labels;
        if (labels.Any(label => string.Equals(label, "mode:batch", StringComparison.OrdinalIgnoreCase)))
        {
            return "batch";
        }
        if (labels.Any(label => string.Equals(label, "mode:tdd", StringComparison.OrdinalIgnoreCase)))
        {
            return "tdd";
        }

        return "minimal";
    }
}
