using System;

namespace Orchestrator.App.Core.Configuration;

public sealed record LabelConfig(
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
    string ResetLabel
);
