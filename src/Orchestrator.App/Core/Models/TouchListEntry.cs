namespace Orchestrator.App.Core.Models;

internal enum TouchOperation
{
    Add,
    Modify,
    Delete,
    Forbidden
}

internal sealed record TouchListEntry(
    TouchOperation Operation,
    string Path,
    string? Notes
);
