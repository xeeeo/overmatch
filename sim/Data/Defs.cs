using System.Text.Json;
using System.Text.Json.Serialization;

namespace Overmatch.Sim.Data;

public sealed class UnitDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public int Cost { get; set; }
    public float BuildTime { get; set; }
    public string BuiltAt { get; set; } = "";
    public string Locomotor { get; set; } = "tracked";
    /// <summary>World units per second.</summary>
    public float Speed { get; set; } = 3f;
    /// <summary>Degrees per second.</summary>
    public float TurnRate { get; set; } = 180f;
    /// <summary>Collision radius in world units.</summary>
    public float Radius { get; set; } = 0.5f;
    public float Hp { get; set; } = 100f;
    public string Armour { get; set; } = "infantry";
    public float Vision { get; set; } = 8f;
    public List<string> Weapons { get; set; } = new();
    public string Model { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}

public sealed class FactionDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#ffffff";
    public string Builder { get; set; } = "";
    public bool NeedsPower { get; set; } = true;
}

/// <summary>A data file's relative path (e.g. "units/coalition/bulwark.json") and its JSON text.</summary>
public readonly record struct DataFile(string Path, string Json);

/// <summary>All static game data for a match. Immutable after load.</summary>
public sealed class GameRules
{
    public IReadOnlyDictionary<string, UnitDef> Units { get; }
    public IReadOnlyDictionary<string, FactionDef> Factions { get; }

    private GameRules(Dictionary<string, UnitDef> units, Dictionary<string, FactionDef> factions)
    {
        Units = units;
        Factions = factions;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static GameRules Load(IEnumerable<DataFile> files)
    {
        var units = new Dictionary<string, UnitDef>();
        var factions = new Dictionary<string, FactionDef>();
        foreach (var file in files)
        {
            var path = file.Path.Replace('\\', '/');
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            if (path.StartsWith("units/"))
            {
                var def = JsonSerializer.Deserialize<UnitDef>(file.Json, JsonOptions)
                          ?? throw new InvalidDataException($"Empty unit def: {path}");
                if (string.IsNullOrEmpty(def.Id)) throw new InvalidDataException($"Unit def without id: {path}");
                units[def.Id] = def;
            }
            else if (path.StartsWith("factions/"))
            {
                var def = JsonSerializer.Deserialize<FactionDef>(file.Json, JsonOptions)
                          ?? throw new InvalidDataException($"Empty faction def: {path}");
                factions[def.Id] = def;
            }
        }
        return new GameRules(units, factions);
    }

    public UnitDef Unit(string id) =>
        Units.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Unknown unit '{id}'");
}
