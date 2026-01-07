namespace Orchestrator.App.Core.Interfaces;

public interface IRepoWorkspace
{
    string Root { get; }
    string ResolvePath(string relativePath);
    bool Exists(string relativePath);
    string ReadAllText(string relativePath);
    void WriteAllText(string relativePath, string content);
    IEnumerable<string> ListFiles(string relativeRoot, string searchPattern, int max);
    string ReadOrTemplate(string relativePath, string templatePath, Dictionary<string, string> tokens);
}
