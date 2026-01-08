namespace Orchestrator.App.Core.Models;

public enum ReviewSeverity
{
    Blocker,
    Major,
    Minor
}

public sealed record ReviewFinding(
    ReviewSeverity Severity,
    string Category,
    string Message,
    string? File = null,
    int? Line = null
);

public sealed record CodeReviewResult(
    bool Approved,
    IReadOnlyList<ReviewFinding> Findings,
    string Summary,
    string ReviewPath,
    bool RequiresHumanReview = false
);
