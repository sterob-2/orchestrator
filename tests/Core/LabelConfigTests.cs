using Orchestrator.App.Core.Configuration;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class LabelConfigTests
{
    [Fact]
    public void LabelConfig_RecordCreation()
    {
        var config = new LabelConfig(
            WorkItemLabel: "work",
            InProgressLabel: "in-progress",
            DoneLabel: "done",
            BlockedLabel: "blocked",
            PlannerLabel: "planner",
            DorLabel: "dor",
            TechLeadLabel: "techlead",
            SpecGateLabel: "spec-gate",
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

        Assert.Equal("work", config.WorkItemLabel);
        Assert.Equal("in-progress", config.InProgressLabel);
        Assert.Equal("done", config.DoneLabel);
        Assert.Equal("blocked", config.BlockedLabel);
        Assert.Equal("planner", config.PlannerLabel);
        Assert.Equal("dor", config.DorLabel);
        Assert.Equal("techlead", config.TechLeadLabel);
        Assert.Equal("spec-gate", config.SpecGateLabel);
        Assert.Equal("dev", config.DevLabel);
        Assert.Equal("test", config.TestLabel);
        Assert.Equal("release", config.ReleaseLabel);
        Assert.Equal("user-review", config.UserReviewRequiredLabel);
        Assert.Equal("review-needed", config.ReviewNeededLabel);
        Assert.Equal("reviewed", config.ReviewedLabel);
        Assert.Equal("spec-questions", config.SpecQuestionsLabel);
        Assert.Equal("spec-clarified", config.SpecClarifiedLabel);
        Assert.Equal("code-review-needed", config.CodeReviewNeededLabel);
        Assert.Equal("code-review-approved", config.CodeReviewApprovedLabel);
        Assert.Equal("code-review-changes-requested", config.CodeReviewChangesRequestedLabel);
        Assert.Equal("reset", config.ResetLabel);
    }
}
