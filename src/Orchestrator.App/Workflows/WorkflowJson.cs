using System.Text.Json;

namespace Orchestrator.App.Workflows;

internal static class WorkflowJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static bool TryDeserialize<T>(string? json, out T? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(json, Options);
            return result != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
