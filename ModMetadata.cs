using SPTarkov.Server.Core.Models.Spt.Mod;

namespace SWAG;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.arys.swag";
    public string Name { get; init; } = "SWAG";
    public string Author { get; init; } = "ArysWasTaken";
    public List<string>? Contributors { get; init; } = ["nooky", "props", "dvize"];
    public SemanticVersioning.Version Version { get; init; } = new("5.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/ArysWasTaken/SWAG";
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}
