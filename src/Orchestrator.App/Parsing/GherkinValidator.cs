using System;
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
        var lines = normalized.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        bool hasScenarioHeader = false;
        bool hasPrimaryStep = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Scenario Outline:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Background:", StringComparison.OrdinalIgnoreCase))
            {
                hasScenarioHeader = true;
            }

            if (trimmed.StartsWith("Given ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("When ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Then ", StringComparison.OrdinalIgnoreCase))
            {
                hasPrimaryStep = true;
            }
        }

        // A valid scenario must have a header and at least one Given/When/Then step.
        return hasScenarioHeader && hasPrimaryStep;
    }
}
