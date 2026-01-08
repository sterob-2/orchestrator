namespace Orchestrator.App.Workflows;

internal static class PlaybookValidator
{
    public static IReadOnlyList<string> Validate(Playbook playbook)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(playbook.Project))
        {
            failures.Add("Playbook-01: Project is required.");
        }

        if (string.IsNullOrWhiteSpace(playbook.Version))
        {
            failures.Add("Playbook-02: Version is required.");
        }

        var frameworkIds = playbook.AllowedFrameworks
            .Where(f => !string.IsNullOrWhiteSpace(f.Id))
            .Select(f => f.Id.Trim())
            .ToList();
        if (frameworkIds.Count != frameworkIds.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            failures.Add("Playbook-03: Allowed framework IDs must be unique.");
        }

        if (playbook.AllowedFrameworks.Any(f => string.IsNullOrWhiteSpace(f.Name)))
        {
            failures.Add("Playbook-04: Allowed framework names are required.");
        }

        if (playbook.AllowedFrameworks.Any(f => string.IsNullOrWhiteSpace(f.Id)))
        {
            failures.Add("Playbook-09: Allowed framework IDs are required.");
        }

        if (playbook.ForbiddenFrameworks.Any(f => string.IsNullOrWhiteSpace(f.Name)))
        {
            failures.Add("Playbook-05: Forbidden framework names are required.");
        }

        var patternIds = playbook.AllowedPatterns
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .Select(p => p.Id.Trim())
            .ToList();
        if (patternIds.Count != patternIds.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            failures.Add("Playbook-06: Allowed pattern IDs must be unique.");
        }

        if (playbook.AllowedPatterns.Any(p => string.IsNullOrWhiteSpace(p.Name)))
        {
            failures.Add("Playbook-07: Allowed pattern names are required.");
        }

        if (playbook.AllowedPatterns.Any(p => string.IsNullOrWhiteSpace(p.Id)))
        {
            failures.Add("Playbook-10: Allowed pattern IDs are required.");
        }

        if (playbook.ForbiddenPatterns.Any(p => string.IsNullOrWhiteSpace(p.Name) && string.IsNullOrWhiteSpace(p.Id)))
        {
            failures.Add("Playbook-08: Forbidden patterns must include a name or ID.");
        }

        return failures;
    }
}
