using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Orchestrator.App.Core.Models;

public class Playbook
{
    [YamlMember(Alias = "project")]
    public string Project { get; set; } = "";
    
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "";

    [YamlMember(Alias = "allowed_frameworks")]
    public List<FrameworkDef> AllowedFrameworks { get; set; } = new();

    [YamlMember(Alias = "forbidden_frameworks")]
    public List<ForbiddenFrameworkDef> ForbiddenFrameworks { get; set; } = new();

    [YamlMember(Alias = "allowed_patterns")]
    public List<PatternDef> AllowedPatterns { get; set; } = new();

    [YamlMember(Alias = "forbidden_patterns")]
    public List<ForbiddenPatternDef> ForbiddenPatterns { get; set; } = new();
}

public class FrameworkDef
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "";
}

public class ForbiddenFrameworkDef
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";
    [YamlMember(Alias = "use_instead")]
    public string UseInstead { get; set; } = "";
}

public class PatternDef
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";
    [YamlMember(Alias = "reference")]
    public string Reference { get; set; } = "";
}

public class ForbiddenPatternDef
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";
}
