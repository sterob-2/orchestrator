using System.Collections.Generic;
using System.Linq;
using Orchestrator.App.Core.Adapters;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class RepoWorkspaceAdapterTests
{
    [Fact]
    public void Adapter_ForwardsWorkspaceOperations()
    {
        using var temp = new TempWorkspace();
        var adapter = new RepoWorkspaceAdapter(temp.Workspace);

        var relativePath = "docs/sample.txt";
        adapter.WriteAllText(relativePath, "content");

        Assert.Equal(temp.WorkspacePath, adapter.Root);
        Assert.True(adapter.Exists(relativePath));
        Assert.Equal("content", adapter.ReadAllText(relativePath));

        var files = adapter.ListFiles("docs", "*.txt", 10).ToList();
        Assert.Contains(relativePath, files);

        var templatePath = "templates/template.txt";
        adapter.WriteAllText(templatePath, "Hello {{NAME}}");
        var tokens = new Dictionary<string, string> { ["{{NAME}}"] = "world" };
        var rendered = adapter.ReadOrTemplate("docs/output.txt", templatePath, tokens);

        Assert.Equal("Hello world", rendered);
    }
}
