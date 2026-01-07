using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class UnityCliTests
{
    [Fact]
    public void Placeholder_DoesNotThrow()
    {
        var config = MockWorkContext.CreateConfig();
        var cli = new UnityCli(config);

        cli.Placeholder();
    }
}
