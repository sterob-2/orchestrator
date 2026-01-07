using System;
using System.Linq;

namespace Orchestrator.App.Parsing;

/// <summary>
/// Validates Gherkin syntax for scenarios.
/// </summary>
public class GherkinValidator
{
    public bool IsValid(string scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario))
        {
            return false;
        }

        var lines = scenario.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .ToList();

        var hasScenario = lines.Any(l => l.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase));
        var hasGiven = lines.Any(l => l.StartsWith("Given ", StringComparison.OrdinalIgnoreCase));
        var hasWhen = lines.Any(l => l.StartsWith("When ", StringComparison.OrdinalIgnoreCase));
        var hasThen = lines.Any(l => l.StartsWith("Then ", StringComparison.OrdinalIgnoreCase));

        return hasScenario && hasGiven && hasWhen && hasThen;
    }
}
