using Moq;
using Orchestrator.App;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Tests.TestHelpers;

internal static class MockWorkContext
{
    public static WorkContext Create(
        WorkItem? workItem = null,
        IGitHubClient? github = null,
        OrchestratorConfig? config = null,
        IRepoWorkspace? workspace = null,
        IRepoGit? repo = null,
        ILlmClient? llm = null)
    {
        workItem ??= CreateWorkItem();
        github ??= CreateGitHubClient();
        config ??= CreateConfig();
        workspace ??= CreateWorkspace();
        repo ??= CreateRepo(config);
        llm ??= CreateLlmClient(config);

        return new WorkContext(workItem, github, config, workspace, repo, llm);
    }

    public static WorkItem CreateWorkItem(
        int number = 1,
        string title = "Test Issue",
        string body = "Test body",
        string url = "https://github.com/test/repo/issues/1",
        IReadOnlyList<string>? labels = null)
    {
        labels ??= new List<string> { "ready-for-agents" };
        return new WorkItem(number, title, body, url, labels);
    }

    public static IGitHubClient CreateGitHubClient()
    {
        var config = CreateConfig();
        return new OctokitGitHubClient(config);
    }

    public static OrchestratorConfig CreateConfig(
        string? workspacePath = null)
    {
        return new OrchestratorConfig(
            OpenAiBaseUrl: "https://api.openai.com/v1",
            OpenAiApiKey: "test-key",
            OpenAiModel: "gpt-4o-mini",
            DevModel: "gpt-4o",
            TechLeadModel: "gpt-4o-mini",
            WorkspacePath: workspacePath ?? "/tmp/test-workspace",
            WorkspaceHostPath: workspacePath ?? "/tmp/test-workspace",
            GitRemoteUrl: "https://github.com/test/repo.git",
            GitAuthorName: "Test Agent",
            GitAuthorEmail: "test@example.com",
            GitHubToken: "test-token",
            RepoOwner: "test-owner",
            RepoName: "test-repo",
            Workflow: new WorkflowConfig(
                DefaultBaseBranch: "main",
                MaxRefinementIterations: 3,
                MaxTechLeadIterations: 3,
                MaxDevIterations: 3,
                MaxCodeReviewIterations: 3,
                MaxDodIterations: 3
            ),
            Labels: new LabelConfig(
                WorkItemLabel: "ready-for-agents",
                InProgressLabel: "in-progress",
                DoneLabel: "done",
                BlockedLabel: "blocked",
                PlannerLabel: "agent:planner",
                DorLabel: "agent:dor",
                TechLeadLabel: "agent:techlead",
                SpecGateLabel: "agent:spec-gate",
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
            WebhookListenHost: "localhost",
            WebhookPort: 5005,
            WebhookPath: "/webhook",
            WebhookSecret: "test-secret",
            ProjectStatusInProgress: "In Progress",
            ProjectStatusInReview: "In Review",
            ProjectOwner: "test-owner",
            ProjectOwnerType: "user",
            ProjectNumber: 1,
            ProjectStatusDone: "Done"
        );
    }

    public static RepoWorkspace CreateWorkspace(string? path = null)
    {
        path ??= Path.Combine(Path.GetTempPath(), $"test-workspace-{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        return new RepoWorkspace(path);
    }

    public static IRepoGit CreateRepo(OrchestratorConfig config)
    {
        return new RepoGit(config, config.WorkspacePath);
    }

    public static ILlmClient CreateLlmClient(OrchestratorConfig config)
    {
        return new LlmClient(config);
    }
}
