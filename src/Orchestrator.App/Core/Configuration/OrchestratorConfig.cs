using System;

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
    WorkflowConfig Workflow,
    LabelConfig Labels,
    string ProjectStatusInProgress,
    string ProjectStatusInReview,
    string ProjectOwner,
    string ProjectOwnerType,
    int? ProjectNumber,
    string ProjectStatusDone
)
{
    public static OrchestratorConfig FromEnvironment()
    {
        string Get(string key, string fallback = "")
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return fallback;
        }

        int GetInt(string key, int fallback)
        {
            var raw = Get(key, "");
            return int.TryParse(raw, out var parsed) ? parsed : fallback;
        }

        int? GetNullableInt(string key)
        {
            var raw = Get(key, "");
            return int.TryParse(raw, out var parsed) ? parsed : null;
        }

        var workspacePath = Get("WORKSPACE_PATH", "/workspace");
        var labels = new LabelConfig(
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
            ResetLabel: Get("RESET_LABEL", "agent:reset")
        );

        var workflow = new WorkflowConfig(
            DefaultBaseBranch: Get("DEFAULT_BASE_BRANCH", "main"),
            PollIntervalSeconds: GetInt("POLL_INTERVAL_SECONDS", 120),
            FastPollIntervalSeconds: GetInt("FAST_POLL_INTERVAL_SECONDS", 30),
            MaxRefinementIterations: GetInt("MAX_REFINEMENT_ITERATIONS", 3),
            MaxTechLeadIterations: GetInt("MAX_TECHLEAD_ITERATIONS", 3),
            MaxDevIterations: GetInt("MAX_DEV_ITERATIONS", 3),
            MaxCodeReviewIterations: GetInt("MAX_CODE_REVIEW_ITERATIONS", 3),
            MaxDodIterations: GetInt("MAX_DOD_ITERATIONS", 3)
        );

        return new OrchestratorConfig(
            OpenAiBaseUrl: Get("OPENAI_BASE_URL", "https://api.openai.com/v1"),
            OpenAiApiKey: Get("OPENAI_API_KEY"),
            OpenAiModel: Get("OPENAI_MODEL", "gpt-5-mini"),
            DevModel: Get("DEV_MODEL", "gpt-5"),
            TechLeadModel: Get("TECHLEAD_MODEL", "gpt-5-mini"),
            WorkspacePath: workspacePath,
            WorkspaceHostPath: Get("WORKSPACE_HOST_PATH", workspacePath),
            GitRemoteUrl: Get("GIT_REMOTE_URL"),
            GitAuthorName: Get("GIT_AUTHOR_NAME", "Orchestrator Agent"),
            GitAuthorEmail: Get("GIT_AUTHOR_EMAIL", "orchestrator@example.local"),
            GitHubToken: Get("GITHUB_TOKEN"),
            RepoOwner: Get("REPO_OWNER"),
            RepoName: Get("REPO_NAME"),
            Workflow: workflow,
            Labels: labels,
            ProjectStatusInProgress: Get("PROJECT_STATUS_IN_PROGRESS", "In progress"),
            ProjectStatusInReview: Get("PROJECT_STATUS_IN_REVIEW", "In Review"),
            ProjectOwner: Get("PROJECT_OWNER"),
            ProjectOwnerType: Get("PROJECT_OWNER_TYPE", "user"),
            ProjectNumber: GetNullableInt("PROJECT_NUMBER"),
            ProjectStatusDone: Get("PROJECT_STATUS_DONE", "Done")
        );
    }
}
