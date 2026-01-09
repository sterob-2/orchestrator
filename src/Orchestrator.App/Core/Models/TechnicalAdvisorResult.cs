namespace Orchestrator.App.Core.Models;

/// <summary>
/// Result of TechnicalAdvisor answering a technical question
/// </summary>
public sealed record TechnicalAdvisorResult(
    string Question,
    string Answer,
    string Reasoning
);
