using System;
using System.Collections.Generic;

namespace Orchestrator.App.Core.Configuration;

internal sealed record OrchestratorConfig(
    string OpenAiBaseUrl,
    string OpenAiApiKey,
    string OpenAiModel,
    string DevModel,
    string TechLeadModel,
    string WorkspacePath,
    string WorkspaceHostPath,
    string GitRemoteUrl,
    string GitAuthorName,
    string GitAuthorEmail,
    string GitHubToken,
    string RepoOwner,
    string RepoName,
    string DefaultBaseBranch,
    int PollIntervalSeconds,
    int FastPollIntervalSeconds,
    string WorkItemLabel,
    string InProgressLabel,
    string DoneLabel,
    string BlockedLabel,
    string PlannerLabel,
    string TechLeadLabel,
    string DevLabel,
    string TestLabel,
    string ReleaseLabel,
    string UserReviewRequiredLabel,
    string ReviewNeededLabel,
    string ReviewedLabel,
    string SpecQuestionsLabel,
    string SpecClarifiedLabel,
    string CodeReviewNeededLabel,
    string CodeReviewApprovedLabel,
    string CodeReviewChangesRequestedLabel,
    string ResetLabel,
    string ProjectStatusInProgress,
    string ProjectStatusInReview,
    string ProjectOwner,
    string ProjectOwnerType,
    int? ProjectNumber,
    string ProjectStatusDone,
    bool UseWorkflowMode
)
{
    public static OrchestratorConfig FromEnvironment()
    {
        string Get(string k, string fallback = "")
        {
            var v = Environment.GetEnvironmentVariable(k);
            if (!string.IsNullOrWhiteSpace(v)) return v!;
            return fallback;
        }

        int GetInt(string k, int fallback)
        {
            var s = Get(k, "");
            return int.TryParse(s, out var i) ? i : fallback;
        }

        int? GetNullableInt(string k)
        {
            var s = Get(k, "");
            if (int.TryParse(s, out var i)) return i;
            return null;
        }

        bool GetBool(string k, bool fallback)
        {
            var s = Get(k, "");
            if (bool.TryParse(s, out var b)) return b;
            return fallback;
        }

        return new OrchestratorConfig(
            OpenAiBaseUrl: Get("OPENAI_BASE_URL", "https://api.openai.com/v1"),
            OpenAiApiKey: Get("OPENAI_API_KEY"),
            OpenAiModel: Get("OPENAI_MODEL", "gpt-5-mini"),
            DevModel: Get("DEV_MODEL", "gpt-5"),
            TechLeadModel: Get("TECHLEAD_MODEL", "gpt-5-mini"),
            WorkspacePath: Get("WORKSPACE_PATH", "/workspace"),
            WorkspaceHostPath: Get("WORKSPACE_HOST_PATH", Get("WORKSPACE_PATH", "/workspace")),
            GitRemoteUrl: Get("GIT_REMOTE_URL"),
            GitAuthorName: Get("GIT_AUTHOR_NAME", "Orchestrator Agent"),
            GitAuthorEmail: Get("GIT_AUTHOR_EMAIL", "orchestrator@example.local"),
            GitHubToken: Get("GITHUB_TOKEN"),
            RepoOwner: Get("REPO_OWNER"),
            RepoName: Get("REPO_NAME"),
            DefaultBaseBranch: Get("DEFAULT_BASE_BRANCH", "main"),
            PollIntervalSeconds: GetInt("POLL_INTERVAL_SECONDS", 120),
            FastPollIntervalSeconds: GetInt("FAST_POLL_INTERVAL_SECONDS", 30),
            WorkItemLabel: Get("WORK_ITEM_LABEL", "ready-for-agents"),
            InProgressLabel: Get("IN_PROGRESS_LABEL", "in-progress"),
            DoneLabel: Get("DONE_LABEL", "done"),
            BlockedLabel: Get("BLOCKED_LABEL", "blocked"),
            PlannerLabel: Get("PLANNER_LABEL", "agent:planner"),
            TechLeadLabel: Get("TECHLEAD_LABEL", "agent:techlead"),
            DevLabel: Get("DEV_LABEL", "agent:dev"),
            TestLabel: Get("TEST_LABEL", "agent:test"),
            ReleaseLabel: Get("RELEASE_LABEL", "agent:release"),
            UserReviewRequiredLabel: Get("USER_REVIEW_REQUIRED_LABEL", "user-review-required"),
            ReviewNeededLabel: Get("REVIEW_NEEDED_LABEL", "agent:review-needed"),
            ReviewedLabel: Get("REVIEWED_LABEL", "agent:reviewed"),
            SpecQuestionsLabel: Get("SPEC_QUESTIONS_LABEL", "spec-questions"),
            SpecClarifiedLabel: Get("SPEC_CLARIFIED_LABEL", "spec-clarified"),
            CodeReviewNeededLabel: Get("CODE_REVIEW_NEEDED_LABEL", "code-review-needed"),
            CodeReviewApprovedLabel: Get("CODE_REVIEW_APPROVED_LABEL", "code-review-approved"),
            CodeReviewChangesRequestedLabel: Get("CODE_REVIEW_CHANGES_REQUESTED_LABEL", "code-review-changes-requested"),
            ResetLabel: Get("RESET_LABEL", "agent:reset"),
            ProjectStatusInProgress: Get("PROJECT_STATUS_IN_PROGRESS", "In progress"),
            ProjectStatusInReview: Get("PROJECT_STATUS_IN_REVIEW", "In Review"),
            ProjectOwner: Get("PROJECT_OWNER"),
            ProjectOwnerType: Get("PROJECT_OWNER_TYPE", "user"),
            ProjectNumber: GetNullableInt("PROJECT_NUMBER"),
            ProjectStatusDone: Get("PROJECT_STATUS_DONE", "Done"),
            UseWorkflowMode: GetBool("USE_WORKFLOW_MODE", false)
        );
    }
}
