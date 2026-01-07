namespace Orchestrator.App.Core.Models;

public enum TouchOperation
{
    Add,
    Modify,
    Delete,
    Forbidden
}

public sealed record TouchListEntry(
    TouchOperation Operation,
    string Path,
    string? Notes
);
