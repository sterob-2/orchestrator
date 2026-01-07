using System;

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

        var lines = scenario.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        bool hasScenario = false;
        bool hasGiven = false;
        bool hasWhen = false;
        bool hasThen = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Scenario:", StringComparison.OrdinalIgnoreCase)) hasScenario = true;
            if (trimmed.StartsWith("Given ", StringComparison.OrdinalIgnoreCase)) hasGiven = true;
            if (trimmed.StartsWith("When ", StringComparison.OrdinalIgnoreCase)) hasWhen = true;
            if (trimmed.StartsWith("Then ", StringComparison.OrdinalIgnoreCase)) hasThen = true;
        }

        // A valid scenario must have a title and at least one Given/When/Then step.
        // While strict Gherkin usually requires When and Then, we'll enforce all three for quality.
        return hasScenario && hasGiven && hasWhen && hasThen;
    }
}