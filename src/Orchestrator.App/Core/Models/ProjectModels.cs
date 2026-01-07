using System.Collections.Generic;

namespace Orchestrator.App.Core.Models;

internal sealed record ProjectItem(string Title, int? IssueNumber, string? Url, string Status);

internal enum ProjectOwnerType
{
    User,
    Organization
}

internal sealed record ProjectSnapshot(
    string Owner,
    int Number,
    ProjectOwnerType OwnerType,
    string Title,
    IReadOnlyList<ProjectItem> Items);

internal sealed record ProjectMetadata(
    string ProjectId,
    string StatusFieldId,
    IReadOnlyDictionary<string, string> StatusOptions,
    IReadOnlyList<ProjectItemRef> Items);

internal sealed record ProjectItemRef(string ItemId, int IssueNumber);

internal sealed record ProjectReference(string Owner, int Number, ProjectOwnerType OwnerType);
