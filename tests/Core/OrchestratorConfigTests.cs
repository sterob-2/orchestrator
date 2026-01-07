using System;
using System.Collections.Generic;
using System.Linq;
using Orchestrator.App;
using Xunit;

namespace Orchestrator.App.Tests.Core;

[Collection("Environment")]
public class OrchestratorConfigTests
{
    private static readonly string[] AllKeys =
    {
        "OPENAI_BASE_URL",
        "OPENAI_API_KEY",
        "OPENAI_MODEL",
        "DEV_MODEL",
        "TECHLEAD_MODEL",
        "WORKSPACE_PATH",
        "GIT_REMOTE_URL",
        "GIT_AUTHOR_NAME",
        "GIT_AUTHOR_EMAIL",
        "GITHUB_TOKEN",
        "REPO_OWNER",
        "REPO_NAME",
        "DEFAULT_BASE_BRANCH",
        "POLL_INTERVAL_SECONDS",
        "FAST_POLL_INTERVAL_SECONDS",
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
        "PROJECT_STATUS_DONE",
        "USE_WORKFLOW_MODE"
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
            ["GIT_REMOTE_URL"] = "https://example.com/repo.git",
            ["GIT_AUTHOR_NAME"] = "Agent",
            ["GIT_AUTHOR_EMAIL"] = "agent@example.com",
            ["GITHUB_TOKEN"] = "token",
            ["REPO_OWNER"] = "owner",
            ["REPO_NAME"] = "repo",
            ["DEFAULT_BASE_BRANCH"] = "develop",
            ["POLL_INTERVAL_SECONDS"] = "45",
            ["FAST_POLL_INTERVAL_SECONDS"] = "12",
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
            ["PROJECT_STATUS_DONE"] = "Done",
            ["USE_WORKFLOW_MODE"] = "true"
        };

        using var scope = new EnvScope(values);

        var labels = new LabelConfig(
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
        );

        var workflow = new WorkflowConfig(
            DefaultBaseBranch: "develop",
            PollIntervalSeconds: 45,
            FastPollIntervalSeconds: 12,
            UseWorkflowMode: true
        );

        var expected = new OrchestratorConfig(
            OpenAiBaseUrl: "https://example.test/v1",
            OpenAiApiKey: "key",
            OpenAiModel: "model-a",
            DevModel: "model-b",
            TechLeadModel: "model-c",
            WorkspacePath: "/tmp/workspace",
            WorkspaceHostPath: "/tmp/workspace",
            GitRemoteUrl: "https://example.com/repo.git",
            GitAuthorName: "Agent",
            GitAuthorEmail: "agent@example.com",
            GitHubToken: "token",
            RepoOwner: "owner",
            RepoName: "repo",
            Workflow: workflow,
            Labels: labels,
            ProjectStatusInProgress: "In Progress",
            ProjectStatusInReview: "In Review",
            ProjectOwner: "proj-owner",
            ProjectOwnerType: "org",
            ProjectNumber: 42,
            ProjectStatusDone: "Done"
        );

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromEnvironment_WithDefaults()
    {
        using var scope = new EnvScope(AllKeys.ToDictionary(key => key, _ => (string?)null));

        var labels = new LabelConfig(
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
        );

        var workflow = new WorkflowConfig(
            DefaultBaseBranch: "main",
            PollIntervalSeconds: 120,
            FastPollIntervalSeconds: 30,
            UseWorkflowMode: false
        );

        var expected = new OrchestratorConfig(
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
            Workflow: workflow,
            Labels: labels,
            ProjectStatusInProgress: "In progress",
            ProjectStatusInReview: "In Review",
            ProjectOwner: "",
            ProjectOwnerType: "user",
            ProjectNumber: null,
            ProjectStatusDone: "Done"
        );

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromEnvironment_WithMissingRequiredValues()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["REPO_OWNER"] = "   ",
            ["REPO_NAME"] = null
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Equal(string.Empty, actual.RepoOwner);
        Assert.Equal(string.Empty, actual.RepoName);
    }

    [Fact]
    public void FromEnvironment_WithInvalidIntegers()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["POLL_INTERVAL_SECONDS"] = "abc",
            ["FAST_POLL_INTERVAL_SECONDS"] = "not-a-number",
            ["PROJECT_NUMBER"] = "nope"
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Equal(120, actual.Workflow.PollIntervalSeconds);
        Assert.Equal(30, actual.Workflow.FastPollIntervalSeconds);
        Assert.Null(actual.ProjectNumber);
    }

    [Fact]
    public void FromEnvironment_WithBooleanParsing()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["USE_WORKFLOW_MODE"] = "true"
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.True(actual.Workflow.UseWorkflowMode);
    }

    [Fact]
    public void FromEnvironment_GetIntWithValidValues()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["POLL_INTERVAL_SECONDS"] = "10",
            ["FAST_POLL_INTERVAL_SECONDS"] = "20"
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Equal(10, actual.Workflow.PollIntervalSeconds);
        Assert.Equal(20, actual.Workflow.FastPollIntervalSeconds);
    }

    [Fact]
    public void FromEnvironment_GetIntWithInvalidValues()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["POLL_INTERVAL_SECONDS"] = "bad"
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Equal(120, actual.Workflow.PollIntervalSeconds);
    }

    [Fact]
    public void FromEnvironment_GetNullableIntEdgeCases()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["PROJECT_NUMBER"] = "0"
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Equal(0, actual.ProjectNumber);
    }

    [Fact]
    public void FromEnvironment_GetNullableIntWithEmptyValue()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["PROJECT_NUMBER"] = ""
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.Null(actual.ProjectNumber);
    }

    [Fact]
    public void FromEnvironment_WithInvalidBooleanValue()
    {
        using var scope = new EnvScope(new Dictionary<string, string?>
        {
            ["USE_WORKFLOW_MODE"] = "not-a-bool"
        });

        var actual = OrchestratorConfig.FromEnvironment();

        Assert.False(actual.Workflow.UseWorkflowMode);
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
