namespace Orchestrator.App.Core.Models;

public sealed record WorkItem(
    int Number,
    string Title,
    string Body,
    string Url,
    IReadOnlyList<string> Labels
);
