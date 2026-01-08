using System.IO;
using System.Text.Json;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows;

internal sealed class InMemoryWorkflowMetricsStore : IWorkflowMetricsStore
{
    private readonly List<WorkflowRunMetrics> _runs = new();
    private readonly object _lock = new();

    public Task AppendAsync(WorkflowRunMetrics metrics, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _runs.Add(metrics);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<WorkflowRunMetrics> GetRecent(int count)
    {
        lock (_lock)
        {
            if (count <= 0)
            {
                return Array.Empty<WorkflowRunMetrics>();
            }

            var start = Math.Max(0, _runs.Count - count);
            return _runs.Skip(start).ToList();
        }
    }
}

internal sealed class FileWorkflowMetricsStore : IWorkflowMetricsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IRepoWorkspace _workspace;
    private readonly string _path;

    public FileWorkflowMetricsStore(IRepoWorkspace workspace, string path)
    {
        _workspace = workspace;
        _path = path;
    }

    public async Task AppendAsync(WorkflowRunMetrics metrics, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(metrics, Options);
            var fullPath = _workspace.ResolvePath(_path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.AppendAllTextAsync(fullPath, json + Environment.NewLine, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to write metrics: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to write metrics: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to write metrics: {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to write metrics: {ex.Message}");
        }
        catch (IOException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to write metrics: {ex.Message}");
        }
    }

    public IReadOnlyList<WorkflowRunMetrics> GetRecent(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<WorkflowRunMetrics>();
        }

        try
        {
            var fullPath = _workspace.ResolvePath(_path);
            if (!File.Exists(fullPath))
            {
                return Array.Empty<WorkflowRunMetrics>();
            }

            var lines = File.ReadAllLines(fullPath);
            var results = new List<WorkflowRunMetrics>();
            for (var i = lines.Length - 1; i >= 0 && results.Count < count; i--)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var metrics = JsonSerializer.Deserialize<WorkflowRunMetrics>(line, Options);
                if (metrics is not null)
                {
                    results.Add(metrics);
                }
            }

            results.Reverse();
            return results;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to read metrics: {ex.Message}");
            return Array.Empty<WorkflowRunMetrics>();
        }
        catch (InvalidOperationException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to read metrics: {ex.Message}");
            return Array.Empty<WorkflowRunMetrics>();
        }
        catch (JsonException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to read metrics: {ex.Message}");
            return Array.Empty<WorkflowRunMetrics>();
        }
        catch (NotSupportedException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to read metrics: {ex.Message}");
            return Array.Empty<WorkflowRunMetrics>();
        }
        catch (IOException ex)
        {
            Logger.WriteLine($"[Metrics] Failed to read metrics: {ex.Message}");
            return Array.Empty<WorkflowRunMetrics>();
        }
    }
}
