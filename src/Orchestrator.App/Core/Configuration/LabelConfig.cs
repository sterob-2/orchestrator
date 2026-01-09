using System;

namespace Orchestrator.App.Core.Configuration;

public sealed record LabelConfig(
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
    string CodeReviewNeededLabel,
    string CodeReviewApprovedLabel,
    string CodeReviewChangesRequestedLabel,
    string ResetLabel
);
