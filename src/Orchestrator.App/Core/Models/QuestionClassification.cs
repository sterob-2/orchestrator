namespace Orchestrator.App.Core.Models;

/// <summary>
/// Type of question that needs to be answered
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// Technical question about implementation, architecture, patterns, frameworks
    /// Should be routed to TechLead executor
    /// </summary>
    Technical,

    /// <summary>
    /// Product/business question about use cases, user workflows, requirements
    /// Should be routed to ProductOwner executor
    /// </summary>
    Product,

    /// <summary>
    /// Ambiguous question that can't be clearly classified
    /// Should be escalated to human
    /// </summary>
    Ambiguous
}

/// <summary>
/// Result of classifying a single question
/// </summary>
public sealed record QuestionClassification(
    string Question,
    QuestionType Type,
    string Reasoning
);

/// <summary>
/// Result of the Question Classifier stage
/// </summary>
public sealed record QuestionClassificationResult(
    QuestionClassification Classification
);
