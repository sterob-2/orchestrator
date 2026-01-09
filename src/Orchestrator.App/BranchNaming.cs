using System.Text.RegularExpressions;

namespace Orchestrator.App;

internal static class BranchNaming
{
    public static string FeatureFromTitle(int issueNumber, string title)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, "[^a-z0-9]+", "-", RegexOptions.None, TimeSpan.FromSeconds(1));
        slug = Regex.Replace(slug, "^-+|-+$", "", RegexOptions.None, TimeSpan.FromSeconds(1));
        if (slug.Length > 40)
            slug = slug.Substring(0, 40).Trim('-');
        return $"feature/{issueNumber}-{slug}";
    }
}
