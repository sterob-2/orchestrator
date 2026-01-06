namespace Orchestrator.App.Core.Models;

internal sealed record WorkItem(
    int Number,
    string Title,
    string Body,
    string Url,
    IReadOnlyList<string> Labels
);
