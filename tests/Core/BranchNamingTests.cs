using FluentAssertions;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class BranchNamingTests
{
    [Fact]
    public void FeatureFromTitle_NormalizesTitle()
    {
        var result = BranchNaming.FeatureFromTitle(42, "Hello, World!");

        result.Should().Be("feature/42-hello-world");
    }

    [Fact]
    public void FeatureFromTitle_TruncatesLongSlugs()
    {
        var title = new string('a', 50);

        var result = BranchNaming.FeatureFromTitle(1, title);

        result.Should().Be($"feature/1-{new string('a', 40)}");
    }
}
