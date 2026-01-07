using System;
using System.Linq;
using Orchestrator.App.Utilities;

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

        var normalized = CodeHelpers.StripCodeFence(scenario);
        var lines = normalized.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .ToList();

        var hasScenarioHeader = lines.Any(l =>
            l.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith("Scenario Outline:", StringComparison.OrdinalIgnoreCase) ||
            l.StartsWith("Background:", StringComparison.OrdinalIgnoreCase));
        var hasGiven = lines.Any(l => l.StartsWith("Given ", StringComparison.OrdinalIgnoreCase));
        var hasWhen = lines.Any(l => l.StartsWith("When ", StringComparison.OrdinalIgnoreCase));
        var hasThen = lines.Any(l => l.StartsWith("Then ", StringComparison.OrdinalIgnoreCase));

        return hasScenarioHeader && hasGiven && hasWhen && hasThen;
    }
}
