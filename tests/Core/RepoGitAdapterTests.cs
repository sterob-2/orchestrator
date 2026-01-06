using Moq;
using Orchestrator.App.Core.Adapters;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class RepoGitAdapterTests
{
    [Fact]
    public void Adapter_ForwardsRepoOperations()
    {
        var legacy = new Mock<ILegacyRepoGit>(MockBehavior.Strict);
        legacy.Setup(c => c.EnsureConfigured());
        legacy.Setup(c => c.IsGitRepo()).Returns(true);
        legacy.Setup(c => c.EnsureBranch("feature", "main"));
        legacy.Setup(c => c.HardResetToRemote("feature"));
        legacy.Setup(c => c.CommitAndPush("feature", "msg", It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        var adapter = new RepoGitAdapter(legacy.Object);

        adapter.EnsureConfigured();
        Assert.True(adapter.IsGitRepo());
        adapter.EnsureBranch("feature", "main");
        adapter.HardResetToRemote("feature");
        var committed = adapter.CommitAndPush("feature", "msg", new[] { "file.txt" });

        Assert.True(committed);
        legacy.VerifyAll();
    }
}
