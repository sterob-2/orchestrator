using System;
using System.Collections.Generic;
using System.Linq;
using CoreConfig = Orchestrator.App.Core.Configuration;
using Xunit;

namespace Orchestrator.App.Tests.Core;

[Collection("Environment")]
public class CoreOrchestratorConfigTests
{
    private static readonly string[] AllKeys =
    {
        "OPENAI_BASE_URL",
        "OPENAI_API_KEY",
        "OPENAI_MODEL",
        "DEV_MODEL",
        "TECHLEAD_MODEL",
        "WORKSPACE_PATH",
        "WORKSPACE_HOST_PATH",
        "GIT_REMOTE_URL",
        "GIT_AUTHOR_NAME",
        "GIT_AUTHOR_EMAIL",
        "GITHUB_TOKEN",
        "REPO_OWNER",
        "REPO_NAME",
        "DEFAULT_BASE_BRANCH",
        "POLL_INTERVAL_SECONDS",
        "FAST_POLL_INTERVAL_SECONDS",
        "MAX_REFINEMENT_ITERATIONS",
        "MAX_TECHLEAD_ITERATIONS",
        "MAX_DEV_ITERATIONS",
        "MAX_CODE_REVIEW_ITERATIONS",
        "MAX_DOD_ITERATIONS",
        "WORK_ITEM_LABEL",
        "IN_PROGRESS_LABEL",
        "DONE_LABEL",
        "BLOCKED_LABEL",
        "PLANNER_LABEL",
        "TECHLEAD_LABEL",
        "DEV_LABEL",
        "TEST_LABEL",
        "RELEASE_LABEL",
        "USER_REVIEW_REQUIRED_LABEL",
        "REVIEW_NEEDED_LABEL",
        "REVIEWED_LABEL",
        "SPEC_QUESTIONS_LABEL",
        "SPEC_CLARIFIED_LABEL",
        "CODE_REVIEW_NEEDED_LABEL",
        "CODE_REVIEW_APPROVED_LABEL",
        "CODE_REVIEW_CHANGES_REQUESTED_LABEL",
        "RESET_LABEL",
        "PROJECT_STATUS_IN_PROGRESS",
        "PROJECT_STATUS_IN_REVIEW",
        "PROJECT_OWNER",
        "PROJECT_OWNER_TYPE",
        "PROJECT_NUMBER",
        "PROJECT_STATUS_DONE"
    };

    [Fact]
    public void FromEnvironment_WithAllValuesSet()
    {
        var values = new Dictionary<string, string?>
        {
            ["OPENAI_BASE_URL"] = "https://example.test/v1",
            ["OPENAI_API_KEY"] = "key",
            ["OPENAI_MODEL"] = "model-a",
            ["DEV_MODEL"] = "model-b",
            ["TECHLEAD_MODEL"] = "model-c",
            ["WORKSPACE_PATH"] = "/tmp/workspace",
            ["WORKSPACE_HOST_PATH"] = "/host/workspace",
            ["GIT_REMOTE_URL"] = "https://example.com/repo.git",
            ["GIT_AUTHOR_NAME"] = "Agent",
            ["GIT_AUTHOR_EMAIL"] = "agent@example.com",
            ["GITHUB_TOKEN"] = "token",
            ["REPO_OWNER"] = "owner",
            ["REPO_NAME"] = "repo",
            ["DEFAULT_BASE_BRANCH"] = "develop",
            ["POLL_INTERVAL_SECONDS"] = "45",
            ["FAST_POLL_INTERVAL_SECONDS"] = "12",
            ["MAX_REFINEMENT_ITERATIONS"] = "2",
            ["MAX_TECHLEAD_ITERATIONS"] = "4",
            ["MAX_DEV_ITERATIONS"] = "5",
            ["MAX_CODE_REVIEW_ITERATIONS"] = "6",
            ["MAX_DOD_ITERATIONS"] = "7",
            ["WORK_ITEM_LABEL"] = "work",
            ["IN_PROGRESS_LABEL"] = "in-progress",
            ["DONE_LABEL"] = "done",
            ["BLOCKED_LABEL"] = "blocked",
            ["PLANNER_LABEL"] = "planner",
            ["TECHLEAD_LABEL"] = "techlead",
            ["DEV_LABEL"] = "dev",
            ["TEST_LABEL"] = "test",
            ["RELEASE_LABEL"] = "release",
            ["USER_REVIEW_REQUIRED_LABEL"] = "user-review",
            ["REVIEW_NEEDED_LABEL"] = "review-needed",
            ["REVIEWED_LABEL"] = "reviewed",
            ["SPEC_QUESTIONS_LABEL"] = "spec-questions",
            ["SPEC_CLARIFIED_LABEL"] = "spec-clarified",
            ["CODE_REVIEW_NEEDED_LABEL"] = "code-review-needed",
            ["CODE_REVIEW_APPROVED_LABEL"] = "code-review-approved",
            ["CODE_REVIEW_CHANGES_REQUESTED_LABEL"] = "code-review-changes-requested",
            ["RESET_LABEL"] = "reset",
            ["PROJECT_STATUS_IN_PROGRESS"] = "In Progress",
            ["PROJECT_STATUS_IN_REVIEW"] = "In Review",
            ["PROJECT_OWNER"] = "proj-owner",
            ["PROJECT_OWNER_TYPE"] = "org",
            ["PROJECT_NUMBER"] = "42",
            ["PROJECT_STATUS_DONE"] = "Done"
        };

        using var scope = new EnvScope(values);

        var expected = new CoreConfig.OrchestratorConfig(
            OpenAiBaseUrl: "https://example.test/v1",
            OpenAiApiKey: "key",
            OpenAiModel: "model-a",
            DevModel: "model-b",
            TechLeadModel: "model-c",
            WorkspacePath: "/tmp/workspace",
            WorkspaceHostPath: "/host/workspace",
            GitRemoteUrl: "https://example.com/repo.git",
            GitAuthorName: "Agent",
            GitAuthorEmail: "agent@example.com",
            GitHubToken: "token",
            RepoOwner: "owner",
            RepoName: "repo",
            Workflow: new CoreConfig.WorkflowConfig(
                DefaultBaseBranch: "develop",
                PollIntervalSeconds: 45,
                FastPollIntervalSeconds: 12,
                MaxRefinementIterations: 2,
                MaxTechLeadIterations: 4,
                MaxDevIterations: 5,
                MaxCodeReviewIterations: 6,
                MaxDodIterations: 7
            ),
            Labels: new CoreConfig.LabelConfig(
                WorkItemLabel: "work",
                InProgressLabel: "in-progress",
                DoneLabel: "done",
                BlockedLabel: "blocked",
                PlannerLabel: "planner",
                TechLeadLabel: "techlead",
                DevLabel: "dev",
                TestLabel: "test",
                ReleaseLabel: "release",
                UserReviewRequiredLabel: "user-review",
                ReviewNeededLabel: "review-needed",
                ReviewedLabel: "reviewed",
                SpecQuestionsLabel: "spec-questions",
                SpecClarifiedLabel: "spec-clarified",
                CodeReviewNeededLabel: "code-review-needed",
                CodeReviewApprovedLabel: "code-review-approved",
                CodeReviewChangesRequestedLabel: "code-review-changes-requested",
                ResetLabel: "reset"
            ),
            ProjectStatusInProgress: "In Progress",
            ProjectStatusInReview: "In Review",
            ProjectOwner: "proj-owner",
            ProjectOwnerType: "org",
            ProjectNumber: 42,
            ProjectStatusDone: "Done"
        );

        var actual = CoreConfig.OrchestratorConfig.FromEnvironment();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromEnvironment_WithDefaults()
    {
        using var scope = new EnvScope(AllKeys.ToDictionary(key => key, _ => (string?)null));

        var expected = new CoreConfig.OrchestratorConfig(
            OpenAiBaseUrl: "https://api.openai.com/v1",
            OpenAiApiKey: "",
            OpenAiModel: "gpt-5-mini",
            DevModel: "gpt-5",
            TechLeadModel: "gpt-5-mini",
            WorkspacePath: "/workspace",
            WorkspaceHostPath: "/workspace",
            GitRemoteUrl: "",
            GitAuthorName: "Orchestrator Agent",
            GitAuthorEmail: "orchestrator@example.local",
            GitHubToken: "",
            RepoOwner: "",
            RepoName: "",
            Workflow: new CoreConfig.WorkflowConfig(
                DefaultBaseBranch: "main",
                PollIntervalSeconds: 120,
                FastPollIntervalSeconds: 30,
                MaxRefinementIterations: 3,
                MaxTechLeadIterations: 3,
                MaxDevIterations: 3,
                MaxCodeReviewIterations: 3,
                MaxDodIterations: 3
            ),
            Labels: new CoreConfig.LabelConfig(
                WorkItemLabel: "ready-for-agents",
                InProgressLabel: "in-progress",
                DoneLabel: "done",
                BlockedLabel: "blocked",
                PlannerLabel: "agent:planner",
                TechLeadLabel: "agent:techlead",
                DevLabel: "agent:dev",
                TestLabel: "agent:test",
                ReleaseLabel: "agent:release",
                UserReviewRequiredLabel: "user-review-required",
                ReviewNeededLabel: "agent:review-needed",
                ReviewedLabel: "agent:reviewed",
                SpecQuestionsLabel: "spec-questions",
                SpecClarifiedLabel: "spec-clarified",
                CodeReviewNeededLabel: "code-review-needed",
                CodeReviewApprovedLabel: "code-review-approved",
                CodeReviewChangesRequestedLabel: "code-review-changes-requested",
                ResetLabel: "agent:reset"
            ),
            ProjectStatusInProgress: "In progress",
            ProjectStatusInReview: "In Review",
            ProjectOwner: "",
            ProjectOwnerType: "user",
            ProjectNumber: null,
            ProjectStatusDone: "Done"
        );

        var actual = CoreConfig.OrchestratorConfig.FromEnvironment();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromEnvironment_WithInvalidValues()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["POLL_INTERVAL_SECONDS"] = "abc",
            ["FAST_POLL_INTERVAL_SECONDS"] = "nope",
            ["MAX_REFINEMENT_ITERATIONS"] = "invalid",
            ["MAX_TECHLEAD_ITERATIONS"] = "invalid",
            ["MAX_DEV_ITERATIONS"] = "invalid",
            ["MAX_CODE_REVIEW_ITERATIONS"] = "invalid",
            ["MAX_DOD_ITERATIONS"] = "invalid",
            ["PROJECT_NUMBER"] = "invalid"
        });

        var actual = CoreConfig.OrchestratorConfig.FromEnvironment();

        Assert.Equal(120, actual.Workflow.PollIntervalSeconds);
        Assert.Equal(30, actual.Workflow.FastPollIntervalSeconds);
        Assert.Equal(3, actual.Workflow.MaxRefinementIterations);
        Assert.Equal(3, actual.Workflow.MaxTechLeadIterations);
        Assert.Equal(3, actual.Workflow.MaxDevIterations);
        Assert.Equal(3, actual.Workflow.MaxCodeReviewIterations);
        Assert.Equal(3, actual.Workflow.MaxDodIterations);
        Assert.Null(actual.ProjectNumber);
    }

    [Fact]
    public void FromEnvironment_WorkspaceHostPathFallsBackToWorkspacePath()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["WORKSPACE_PATH"] = "/tmp/overridden",
            ["WORKSPACE_HOST_PATH"] = null
        });

        var actual = CoreConfig.OrchestratorConfig.FromEnvironment();

        Assert.Equal("/tmp/overridden", actual.WorkspacePath);
        Assert.Equal("/tmp/overridden", actual.WorkspaceHostPath);
    }

    [Fact]
    public void FromEnvironment_UsesWorkspaceHostPathWhenProvided()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["WORKSPACE_PATH"] = "/tmp/overridden",
            ["WORKSPACE_HOST_PATH"] = "/host/path"
        });

        var actual = CoreConfig.OrchestratorConfig.FromEnvironment();

        Assert.Equal("/tmp/overridden", actual.WorkspacePath);
        Assert.Equal("/host/path", actual.WorkspaceHostPath);
    }

    [Fact]
    public void FromEnvironment_IgnoresWhitespaceValues()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["OPENAI_BASE_URL"] = "   ",
            ["GIT_AUTHOR_NAME"] = "  ",
            ["WORKSPACE_PATH"] = "   "
        });

        var actual = CoreConfig.OrchestratorConfig.FromEnvironment();

        Assert.Equal("https://api.openai.com/v1", actual.OpenAiBaseUrl);
        Assert.Equal("Orchestrator Agent", actual.GitAuthorName);
        Assert.Equal("/workspace", actual.WorkspacePath);
        Assert.Equal("/workspace", actual.WorkspaceHostPath);
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _original = new();

        public EnvScope(IDictionary<string, string?> values)
        {
            foreach (var pair in values)
            {
                _original[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }

        public void Dispose()
        {
            foreach (var pair in _original)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }
}
