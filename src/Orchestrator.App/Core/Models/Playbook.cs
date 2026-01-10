using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Orchestrator.App.Core.Models;

public class Playbook
{
    [YamlMember(Alias = "project")]
    public string Project { get; set; } = "";

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "";

    [YamlMember(Alias = "core_principles")]
    public List<PrincipleDef> CorePrinciples { get; set; } = new();

    [YamlMember(Alias = "allowed_frameworks")]
    public List<FrameworkDef> AllowedFrameworks { get; set; } = new();

    [YamlMember(Alias = "forbidden_frameworks")]
    public List<ForbiddenFrameworkDef> ForbiddenFrameworks { get; set; } = new();

    [YamlMember(Alias = "allowed_patterns")]
    public List<PatternDef> AllowedPatterns { get; set; } = new();

    [YamlMember(Alias = "forbidden_patterns")]
    public List<ForbiddenPatternDef> ForbiddenPatterns { get; set; } = new();

    [YamlMember(Alias = "file_size_limits")]
    public FileSizeLimits? FileSizeLimits { get; set; }
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
    [YamlMember(Alias = "reason")]
    public string Reason { get; set; } = "";
}

public class PrincipleDef
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";
    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";
    [YamlMember(Alias = "rationale")]
    public string Rationale { get; set; } = "";
}

public class FileSizeLimits
{
    [YamlMember(Alias = "executors")]
    public int Executors { get; set; }
    [YamlMember(Alias = "validators")]
    public int Validators { get; set; }
    [YamlMember(Alias = "prompts")]
    public int Prompts { get; set; }
    [YamlMember(Alias = "models")]
    public int Models { get; set; }
    [YamlMember(Alias = "utilities")]
    public int Utilities { get; set; }
    [YamlMember(Alias = "note")]
    public string Note { get; set; } = "";
}
