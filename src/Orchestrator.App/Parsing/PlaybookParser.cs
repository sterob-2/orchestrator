using YamlDotNet.Serialization;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Parsing;

/// <summary>
/// Parses the architecture-playbook.yaml file.
/// </summary>
public class PlaybookParser
{
    private readonly IDeserializer _deserializer;

    public PlaybookParser()
    {
        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public Playbook Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new Playbook();
        }
        return _deserializer.Deserialize<Playbook>(content);
    }
}