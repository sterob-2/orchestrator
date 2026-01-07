using System;
using System.Collections.Generic;
using CoreModels = Orchestrator.App.Core.Models;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class CoreModelsTests
{
    [Fact]
    public void WorkItem_RecordCreation()
    {
        var labels = new List<string> { "label1", "label2" };
        var item = new CoreModels.WorkItem(1, "Title", "Body", "https://example.com", labels);

        Assert.Equal(1, item.Number);
        Assert.Equal("Title", item.Title);
        Assert.Equal("Body", item.Body);
        Assert.Equal("https://example.com", item.Url);
        Assert.Equal(labels, item.Labels);
    }

    [Fact]
    public void ProjectContext_RecordCreation()
    {
        var context = new CoreModels.ProjectContext(
            RepoOwner: "owner",
            RepoName: "repo",
            DefaultBaseBranch: "main",
            WorkspacePath: "/workspace",
            WorkspaceHostPath: "/host/workspace",
            ProjectOwner: "owner",
            ProjectOwnerType: "user",
            ProjectNumber: 5
        );

        Assert.Equal("owner", context.RepoOwner);
        Assert.Equal("repo", context.RepoName);
        Assert.Equal("main", context.DefaultBaseBranch);
        Assert.Equal("/workspace", context.WorkspacePath);
        Assert.Equal("/host/workspace", context.WorkspaceHostPath);
        Assert.Equal("owner", context.ProjectOwner);
        Assert.Equal("user", context.ProjectOwnerType);
        Assert.Equal(5, context.ProjectNumber);
    }

    [Fact]
    public void WorkflowInput_RecordCreation()
    {
        var item = new CoreModels.WorkItem(2, "Title", "Body", "https://example.com", new List<string>());
        var project = new CoreModels.ProjectContext("owner", "repo", "main", "/workspace", "/host/workspace", "owner", "user", 4);

        var input = new CoreModels.WorkflowInput(item, project, "batch", 2);

        Assert.Same(item, input.WorkItem);
        Assert.Same(project, input.ProjectContext);
        Assert.Equal("batch", input.Mode);
        Assert.Equal(2, input.Attempt);
    }

    [Fact]
    public void GateResult_RecordCreation()
    {
        var failures = new List<string> { "missing title" };
        var result = new CoreModels.GateResult(false, "summary", failures);

        Assert.False(result.Passed);
        Assert.Equal("summary", result.Summary);
        Assert.Equal(failures, result.Failures);
    }

    [Fact]
    public void TouchListEntry_RecordCreation()
    {
        var entry = new CoreModels.TouchListEntry(CoreModels.TouchOperation.Modify, "src/File.cs", "Update logic");

        Assert.Equal(CoreModels.TouchOperation.Modify, entry.Operation);
        Assert.Equal("src/File.cs", entry.Path);
        Assert.Equal("Update logic", entry.Notes);
    }

    [Fact]
    public void ParsedSpec_RecordCreation()
    {
        var touchList = new List<CoreModels.TouchListEntry>
        {
            new(CoreModels.TouchOperation.Add, "src/NewFile.cs", "Add new flow")
        };
        var sections = new Dictionary<string, string>
        {
            ["Goal"] = "Ship feature",
            ["NonGoals"] = "No redesign"
        };

        var spec = new CoreModels.ParsedSpec(
            Goal: "Ship feature",
            NonGoals: "No redesign",
            Status: "Draft",
            Updated: new DateTime(2024, 1, 12),
            ArchitectureReferences: new List<string> { "ADR-1" },
            Risks: new List<string> { "Risk-1" },
            Components: new List<string> { "API" },
            TouchList: touchList,
            Interfaces: new List<string> { "IService" },
            Scenarios: new List<string> { "Given ..." },
            Sequence: new List<string> { "Step 1", "Step 2" },
            TestMatrix: new List<string> { "Unit" },
            Sections: sections
        );

        Assert.Equal("Ship feature", spec.Goal);
        Assert.Equal("No redesign", spec.NonGoals);
        Assert.Equal("Draft", spec.Status);
        Assert.Equal(new DateTime(2024, 1, 12), spec.Updated);
        Assert.Equal("ADR-1", spec.ArchitectureReferences[0]);
        Assert.Equal("Risk-1", spec.Risks[0]);
        Assert.Equal("API", spec.Components[0]);
        Assert.Equal(touchList, spec.TouchList);
        Assert.Equal("IService", spec.Interfaces[0]);
        Assert.Equal("Given ...", spec.Scenarios[0]);
        Assert.Equal("Step 1", spec.Sequence[0]);
        Assert.Equal("Unit", spec.TestMatrix[0]);
        Assert.Equal(sections, spec.Sections);
    }

    [Fact]
    public void ComplexityIndicators_RecordCreation()
    {
        var indicators = new CoreModels.ComplexityIndicators(new List<string> { "cross-cutting" }, "Needs coordination");

        Assert.Single(indicators.Signals);
        Assert.Equal("cross-cutting", indicators.Signals[0]);
        Assert.Equal("Needs coordination", indicators.Summary);
    }
}
