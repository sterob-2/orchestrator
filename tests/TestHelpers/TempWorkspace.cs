using Orchestrator.App;

namespace Orchestrator.App.Tests.TestHelpers;

internal sealed class TempWorkspace : IDisposable
{
    private readonly string _path;
    private readonly RepoWorkspace _workspace;

    public TempWorkspace()
    {
        _path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test-workspace-{Guid.NewGuid()}");
        Directory.CreateDirectory(_path);
        _workspace = new RepoWorkspace(_path);
    }

    public RepoWorkspace Workspace => _workspace;
    public string WorkspacePath => _path;

    public void CreateFile(string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(_path, relativePath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_path))
        {
            try
            {
                Directory.Delete(_path, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
