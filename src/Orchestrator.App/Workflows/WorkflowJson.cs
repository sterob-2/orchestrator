using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orchestrator.App.Workflows;

internal static class WorkflowJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true) }
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
            return !EqualityComparer<T>.Default.Equals(result, default);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
