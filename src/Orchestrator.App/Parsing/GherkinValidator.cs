using System;
using System.Linq;

namespace Orchestrator.App.Parsing;

/// <summary>
/// Validates Gherkin syntax for scenarios.
/// </summary>
public static class GherkinValidator
{
    private static readonly string[] NewLineSeparators = ["\r\n", "\r", "\n"];

    public static bool IsValid(string scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario))
        {
            return false;
        }

        var lines = scenario.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .ToList();

        var hasScenario = lines.Any(l => l.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase));
        var hasGiven = lines.Any(l => l.StartsWith("Given ", StringComparison.OrdinalIgnoreCase));
        var hasWhen = lines.Any(l => l.StartsWith("When ", StringComparison.OrdinalIgnoreCase));
        var hasThen = lines.Any(l => l.StartsWith("Then ", StringComparison.OrdinalIgnoreCase));

        return hasScenario && hasGiven && hasWhen && hasThen;
    }
}
