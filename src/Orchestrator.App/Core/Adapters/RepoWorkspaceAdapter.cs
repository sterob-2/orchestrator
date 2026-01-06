using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Core.Adapters;

internal sealed class RepoWorkspaceAdapter : IRepoWorkspace
{
    private readonly ILegacyRepoWorkspace _workspace;

    public RepoWorkspaceAdapter(ILegacyRepoWorkspace workspace)
    {
        _workspace = workspace;
    }

    public string Root => _workspace.Root;

    public string ResolvePath(string relativePath) => _workspace.ResolvePath(relativePath);

    public bool Exists(string relativePath) => _workspace.Exists(relativePath);

    public string ReadAllText(string relativePath) => _workspace.ReadAllText(relativePath);

    public void WriteAllText(string relativePath, string content) => _workspace.WriteAllText(relativePath, content);

    public IEnumerable<string> ListFiles(string relativeRoot, string searchPattern, int max)
    {
        return _workspace.ListFiles(relativeRoot, searchPattern, max);
    }

    public string ReadOrTemplate(string relativePath, string templatePath, Dictionary<string, string> tokens)
    {
        return _workspace.ReadOrTemplate(relativePath, templatePath, tokens);
    }
}
