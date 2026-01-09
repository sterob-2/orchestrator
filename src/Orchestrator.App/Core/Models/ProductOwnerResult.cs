namespace Orchestrator.App.Core.Models;

/// <summary>
/// Result of ProductOwner answering a product/business question
/// </summary>
public sealed record ProductOwnerResult(
    string Question,
    string Answer,
    string Reasoning
);
