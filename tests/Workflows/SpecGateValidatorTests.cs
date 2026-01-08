using Moq;

namespace Orchestrator.App.Tests.Workflows;

public class SpecGateValidatorTests
{
    [Fact]
    public void Evaluate_ReturnsPass_WhenSpecIsValid()
    {
        var spec = new ParsedSpec(
            Goal: "Implement feature using .NET 8 with Clean Architecture.",
            NonGoals: "No database changes.",
            Components: new List<string> { "Api", "Domain" },
            TouchList: new List<TouchListEntry>
            {
                new TouchListEntry(TouchOperation.Modify, "src/App.cs", null)
            },
            Interfaces: new List<string> { "IExampleService" },
            Scenarios: new List<string>
            {
                "Scenario: success\nGiven a user\nWhen they act\nThen it works",
                "Scenario: failure\nGiven a user\nWhen they fail\nThen an error shows",
                "Scenario: retry\nGiven a user\nWhen they retry\nThen it succeeds"
            },
            Sequence: new List<string> { "Step 1", "Step 2" },
            TestMatrix: new List<string> { "| case | test |" },
            Sections: new Dictionary<string, string> { ["Touch List"] = "present" });

        var playbook = new Playbook
        {
            Project = "Orchestrator",
            Version = "2.0",
            AllowedFrameworks = new List<FrameworkDef>
            {
                new FrameworkDef { Id = "FW-01", Name = ".NET 8", Version = "8.x" }
            },
            AllowedPatterns = new List<PatternDef>
            {
                new PatternDef { Id = "PAT-01", Name = "Clean Architecture", Reference = "docs/arch.md" }
            }
        };

        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists("src/App.cs")).Returns(true);

        var result = SpecGateValidator.Evaluate(spec, playbook, workspace.Object);

        Assert.True(result.Passed);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Evaluate_ReturnsFailures_WhenSpecViolatesRules()
    {
        var spec = new ParsedSpec(
            Goal: "",
            NonGoals: "Avoid BadLib usage.",
            Components: new List<string>(),
            TouchList: new List<TouchListEntry>
            {
                new TouchListEntry(TouchOperation.Modify, "src/Missing.cs", null)
            },
            Interfaces: new List<string>(),
            Scenarios: new List<string>
            {
                "Scenario: incomplete\nGiven something"
            },
            Sequence: new List<string> { "Step 1" },
            TestMatrix: new List<string>(),
            Sections: new Dictionary<string, string> { ["Touch List"] = "present" });

        var playbook = new Playbook
        {
            Project = "Orchestrator",
            Version = "2.0",
            ForbiddenFrameworks = new List<ForbiddenFrameworkDef>
            {
                new ForbiddenFrameworkDef { Name = "BadLib", UseInstead = "GoodLib" }
            }
        };

        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists("src/Missing.cs")).Returns(false);

        var result = SpecGateValidator.Evaluate(spec, playbook, workspace.Object);

        Assert.False(result.Passed);
        Assert.Contains(result.Failures, failure => failure.StartsWith("Spec-01", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("Spec-08", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("Spec-09", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("Spec-12", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.StartsWith("Spec-13", StringComparison.Ordinal));
    }
}
