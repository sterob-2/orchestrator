namespace Orchestrator.App.Tests.Workflows;

public class PlaybookValidatorTests
{
    [Fact]
    public void Validate_ReturnsFailures_ForMissingRequiredFields()
    {
        var playbook = new Playbook();

        var failures = PlaybookValidator.Validate(playbook);

        Assert.Contains(failures, failure => failure.StartsWith("Playbook-01", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.StartsWith("Playbook-02", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReturnsNoFailures_ForValidPlaybook()
    {
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

        var failures = PlaybookValidator.Validate(playbook);

        Assert.Empty(failures);
    }
}
