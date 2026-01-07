namespace Orchestrator.App.Core.Configuration;

internal sealed record LabelConfig(
    string WorkItemLabel,
    string InProgressLabel,
    string DoneLabel,
    string BlockedLabel,
    string PlannerLabel,
    string DorLabel,
    string TechLeadLabel,
    string SpecGateLabel,
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
    string ResetLabel
);
