namespace Orchestrator.App.Workflows;

internal static class SpecGateValidator
{
    public static GateResult Evaluate(ParsedSpec spec, Playbook playbook, IRepoWorkspace workspace)
    {
        Logger.Debug($"[SpecGate] Starting validation");
        Logger.Debug($"[SpecGate] Spec has {spec.Components.Count} component(s), {spec.TouchList.Count} touch list item(s), {spec.Scenarios.Count} scenario(s)");

        var failures = new List<string>();

        var specText = BuildSpecText(spec);

        Logger.Debug($"[SpecGate] Checking required sections...");
        AddRequiredSectionsFailures(failures, spec);

        Logger.Debug($"[SpecGate] Checking touch list...");
        AddTouchListFailures(failures, spec);

        Logger.Debug($"[SpecGate] Checking scenarios...");
        AddScenarioFailures(failures, spec);

        Logger.Debug($"[SpecGate] Checking sequence and test matrix...");
        AddSequenceAndMatrixFailures(failures, spec);

        Logger.Debug($"[SpecGate] Checking missing files/directories...");
        AddMissingTouchFilesFailures(failures, spec, workspace);

        Logger.Debug($"[SpecGate] Checking playbook constraints...");
        AddPlaybookFailures(failures, playbook);

        Logger.Debug($"[SpecGate] Checking forbidden references...");
        AddForbiddenReferencesFailures(failures, playbook, specText);

        Logger.Debug($"[SpecGate] Checking allowed framework/pattern references...");
        AddAllowedReferencesFailures(failures, spec, playbook, specText);

        Logger.Info($"[SpecGate] Validation complete: {failures.Count} failure(s)");
        if (failures.Count > 0)
        {
            foreach (var failure in failures)
            {
                Logger.Info($"[SpecGate]   - {failure}");
            }
        }

        return new GateResult(
            Passed: failures.Count == 0,
            Summary: failures.Count == 0 ? "Spec gate passed." : "Spec gate failed.",
            Failures: failures);
    }

    private static string BuildSpecText(ParsedSpec spec)
    {
        var parts = new List<string>
        {
            spec.Goal,
            spec.NonGoals,
            string.Join("\n", spec.Components),
            string.Join("\n", spec.Interfaces),
            string.Join("\n", spec.Scenarios),
            string.Join("\n", spec.Sequence),
            string.Join("\n", spec.TestMatrix),
            string.Join("\n", spec.Sections.Values)
        };

        return string.Join("\n", parts).ToLowerInvariant();
    }

    private static bool ContainsToken(string text, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return text.Contains(token.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    private static void AddRequiredSectionsFailures(List<string> failures, ParsedSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Goal))
        {
            failures.Add("Spec-01: Goal section is required.");
        }

        if (string.IsNullOrWhiteSpace(spec.NonGoals))
        {
            failures.Add("Spec-02: Non-goals section is required.");
        }

        if (spec.Components.Count < 1)
        {
            failures.Add("Spec-03: Components section must list at least one component.");
        }

        if (spec.Interfaces.Count < 1)
        {
            failures.Add("Spec-07: Interfaces section is required.");
        }
    }

    private static void AddTouchListFailures(List<string> failures, ParsedSpec spec)
    {
        var hasTouchSection = spec.Sections.ContainsKey("Touch List");
        if (spec.TouchList.Count < 1)
        {
            failures.Add("Spec-04: Touch List is missing or empty.");
            if (hasTouchSection)
            {
                failures.Add("Spec-05: Touch List format is invalid.");
            }
        }

        var invalidTouchEntries = spec.TouchList.Where(entry => string.IsNullOrWhiteSpace(entry.Path)).ToList();
        if (invalidTouchEntries.Count > 0)
        {
            failures.Add("Spec-06: Touch List contains entries with empty paths.");
        }
    }

    private static void AddScenarioFailures(List<string> failures, ParsedSpec spec)
    {
        if (spec.Scenarios.Count < 3)
        {
            failures.Add("Spec-08: At least 3 scenarios are required.");
        }

        var invalidScenarios = spec.Scenarios.Where(s => !GherkinValidator.IsValid(s)).ToList();
        if (invalidScenarios.Count > 0)
        {
            failures.Add($"Spec-09: {invalidScenarios.Count} scenarios are not valid Gherkin.");
        }
    }

    private static void AddSequenceAndMatrixFailures(List<string> failures, ParsedSpec spec)
    {
        if (spec.Sequence.Count < 2)
        {
            failures.Add("Spec-10: Sequence section must include at least 2 steps.");
        }

        if (spec.TestMatrix.Count < 1)
        {
            failures.Add("Spec-11: Test matrix is required.");
        }
    }

    private static void AddMissingTouchFilesFailures(List<string> failures, ParsedSpec spec, IRepoWorkspace workspace)
    {
        var entriesToCheck = spec.TouchList
            .Where(entry => entry.Operation is TouchOperation.Modify or TouchOperation.Delete)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Path))
            .ToList();

        Logger.Debug($"[SpecGate] Checking {entriesToCheck.Count} touch list path(s) for existence");

        foreach (var entry in entriesToCheck)
        {
            var exists = FileOrDirectoryExists(workspace, entry.Path);
            Logger.Debug($"[SpecGate]   {entry.Operation} | {entry.Path} -> {(exists ? "EXISTS" : "MISSING")}");
        }

        var missingPaths = entriesToCheck
            .Where(entry => !FileOrDirectoryExists(workspace, entry.Path))
            .Select(entry => entry.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingPaths.Count > 0)
        {
            failures.Add($"Spec-12: Missing files or directories for touch list entries: {string.Join(", ", missingPaths)}.");
        }
    }

    private static bool FileOrDirectoryExists(IRepoWorkspace workspace, string relativePath)
    {
        // First check if workspace.Exists returns true (for files and backward compatibility)
        if (workspace.Exists(relativePath))
        {
            return true;
        }

        // If not found as a file, check if it's a directory
        try
        {
            var fullPath = workspace.ResolvePath(relativePath);
            return System.IO.Directory.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    private static void AddPlaybookFailures(List<string> failures, Playbook playbook)
    {
        var playbookFailures = PlaybookValidator.Validate(playbook);
        failures.AddRange(playbookFailures.Select(failure => $"Playbook: {failure}"));
    }

    private static void AddForbiddenReferencesFailures(List<string> failures, Playbook playbook, string specText)
    {
        foreach (var forbidden in playbook.ForbiddenFrameworks.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
        {
            if (ContainsToken(specText, forbidden.Name))
            {
                failures.Add($"Spec-13: Forbidden framework referenced ({forbidden.Name}).");
            }
        }

        foreach (var forbidden in playbook.ForbiddenPatterns.Where(p => !string.IsNullOrWhiteSpace(p.Name) || !string.IsNullOrWhiteSpace(p.Id)))
        {
            if (ContainsToken(specText, forbidden.Name) || ContainsToken(specText, forbidden.Id))
            {
                failures.Add($"Spec-14: Forbidden pattern referenced ({forbidden.Name} {forbidden.Id}).");
            }
        }
    }

    private static void AddAllowedReferencesFailures(List<string> failures, ParsedSpec spec, Playbook playbook, string specText)
    {
        // Only require framework/pattern references when adding new code
        // Simple deletions or modifications don't need to reference frameworks/patterns
        var hasAddOperations = spec.TouchList.Any(entry => entry.Operation == TouchOperation.Add);

        Logger.Debug($"[SpecGate] Touch list has Add operations: {hasAddOperations}");

        if (!hasAddOperations)
        {
            Logger.Debug($"[SpecGate] Skipping framework/pattern validation (no Add operations)");
            return; // Skip framework/pattern validation for deletion-only or modification-only changes
        }

        if (playbook.AllowedFrameworks.Count > 0)
        {
            var foundFramework = playbook.AllowedFrameworks.Any(f => ContainsToken(specText, f.Name) || ContainsToken(specText, f.Id));
            Logger.Debug($"[SpecGate] Found allowed framework reference: {foundFramework}");
            if (!foundFramework)
            {
                failures.Add("Spec-15: No allowed frameworks referenced from the playbook.");
            }
        }

        if (playbook.AllowedPatterns.Count > 0)
        {
            var foundPattern = playbook.AllowedPatterns.Any(p => ContainsToken(specText, p.Name) || ContainsToken(specText, p.Id));
            Logger.Debug($"[SpecGate] Found allowed pattern reference: {foundPattern}");
            if (!foundPattern)
            {
                failures.Add("Spec-16: No allowed patterns referenced from the playbook.");
            }
        }
    }
}
