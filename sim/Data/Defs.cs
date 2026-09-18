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
    /// <summary>Degrees per second for the turret. 0 means no turret: the hull must face the target.</summary>
    public float TurretTurnRate { get; set; } = 0f;
    /// <summary>Collision radius in world units.</summary>
    public float Radius { get; set; } = 0.5f;
    public float Hp { get; set; } = 100f;
    public string Armour { get; set; } = "infantry";
    public float Vision { get; set; } = 8f;
    public List<string> Weapons { get; set; } = new();
    public string Model { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    /// <summary>XP awarded to the killer.</summary>
    public int XpValue { get; set; } = 10;

    [JsonIgnore] public Locomotor LocomotorClass => LocomotorParse.Parse(Locomotor);
    [JsonIgnore] public bool HasTurret => TurretTurnRate > 0f;
    [JsonIgnore] public bool IsAir => LocomotorClass == Sim.Locomotor.Air;
}

public sealed class ProjectileDef
{
    /// <summary>"instant" (hitscan), "shell" (ballistic, aims at the target's position when fired), "missile" (homes on the target).</summary>
    public string Kind { get; set; } = "instant";
    public float Speed { get; set; } = 30f;
    /// <summary>Visual hint only: lob the projectile.</summary>
    public bool Arc { get; set; }
}

public sealed class SplashDef
{
    public float Radius { get; set; }
    /// <summary>Damage multiplier at the edge of the radius (1 = no falloff).</summary>
    public float Falloff { get; set; } = 0.5f;
}

public sealed class WeaponDef
{
    public string Id { get; set; } = "";
    public float Damage { get; set; } = 10f;
    public string DamageType { get; set; } = "small_arms";
    public float Range { get; set; } = 6f;
    public float MinRange { get; set; } = 0f;
    /// <summary>Seconds between shots.</summary>
    public float Cooldown { get; set; } = 1f;
    public ProjectileDef Projectile { get; set; } = new();
    public SplashDef? Splash { get; set; }
    /// <summary>"ground", "air".</summary>
    public List<string> Targets { get; set; } = new() { "ground" };
    /// <summary>Degrees of aim error tolerated before firing.</summary>
    public float AimTolerance { get; set; } = 6f;

    [JsonIgnore] public bool CanTargetAir => Targets.Contains("air");
    [JsonIgnore] public bool CanTargetGround => Targets.Contains("ground");
}

public sealed class FactionDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#ffffff";
    public string Builder { get; set; } = "";
    public bool NeedsPower { get; set; } = true;
}

public sealed class RectDef
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; } = 1;
    public int H { get; set; } = 1;
}

public sealed class SpawnDef
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>A map: a grid of cells with terrain types plus player start points. Cells are 1×1 world units; cell (x, y) covers [x, x+1) × [y, y+1).</summary>
public sealed class MapDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Width { get; set; } = 64;
    public int Height { get; set; } = 64;
    public List<RectDef> Blocked { get; set; } = new();
    public List<RectDef> Water { get; set; } = new();
    public List<RectDef> Rough { get; set; } = new();
    public List<RectDef> Road { get; set; } = new();
    public List<SpawnDef> Spawns { get; set; } = new();
}

/// <summary>A data file's relative path (e.g. "units/coalition/bulwark.json") and its JSON text.</summary>
public readonly record struct DataFile(string Path, string Json);

/// <summary>All static game data for a match. Immutable after load.</summary>
public sealed class GameRules
{
    public IReadOnlyDictionary<string, UnitDef> Units { get; }
    public IReadOnlyDictionary<string, WeaponDef> Weapons { get; }
    public IReadOnlyDictionary<string, FactionDef> Factions { get; }
    public IReadOnlyDictionary<string, MapDef> Maps { get; }
    public ArmourTable Armour { get; }

    private GameRules(Dictionary<string, UnitDef> units, Dictionary<string, WeaponDef> weapons,
        Dictionary<string, FactionDef> factions, Dictionary<string, MapDef> maps, ArmourTable armour)
    {
        Units = units;
        Weapons = weapons;
        Factions = factions;
        Maps = maps;
        Armour = armour;
    }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static GameRules Load(IEnumerable<DataFile> files)
    {
        var units = new Dictionary<string, UnitDef>();
        var weapons = new Dictionary<string, WeaponDef>();
        var factions = new Dictionary<string, FactionDef>();
        var maps = new Dictionary<string, MapDef>();
        var armour = new ArmourTable();

        foreach (var file in files)
        {
            var path = file.Path.Replace('\\', '/');
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            if (path.StartsWith("units/")) Add(units, Parse<UnitDef>(file), d => d.Id, path);
            else if (path.StartsWith("weapons/")) Add(weapons, Parse<WeaponDef>(file), d => d.Id, path);
            else if (path.StartsWith("factions/")) Add(factions, Parse<FactionDef>(file), d => d.Id, path);
            else if (path.StartsWith("maps/")) Add(maps, Parse<MapDef>(file), d => d.Id, path);
            else if (path == "armour.json") armour = ArmourTable.Parse(file.Json);
        }

        foreach (var u in units.Values)
            foreach (var w in u.Weapons)
                if (!weapons.ContainsKey(w)) throw new InvalidDataException($"Unit '{u.Id}' references unknown weapon '{w}'");

        return new GameRules(units, weapons, factions, maps, armour);
    }

    private static T Parse<T>(DataFile file) =>
        JsonSerializer.Deserialize<T>(file.Json, JsonOptions) ?? throw new InvalidDataException($"Empty definition: {file.Path}");

    private static void Add<T>(Dictionary<string, T> dict, T def, Func<T, string> id, string path)
    {
        var key = id(def);
        if (string.IsNullOrEmpty(key)) throw new InvalidDataException($"Definition without id: {path}");
        dict[key] = def;
    }

    public UnitDef Unit(string id) =>
        Units.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Unknown unit '{id}'");

    public WeaponDef Weapon(string id) =>
        Weapons.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Unknown weapon '{id}'");

    public MapDef Map(string id) =>
        Maps.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Unknown map '{id}'");
}

/// <summary>armourType → damageType → multiplier. Missing entries are 1.0.</summary>
public sealed class ArmourTable
{
    private readonly Dictionary<string, Dictionary<string, float>> _table = new();

    public static ArmourTable Parse(string json)
    {
        var t = new ArmourTable();
        var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, float>>>(json, GameRules.JsonOptions)
                  ?? new Dictionary<string, Dictionary<string, float>>();
        foreach (var (armour, row) in raw) t._table[armour] = row;
        return t;
    }

    public float Multiplier(string armour, string damageType)
    {
        if (_table.TryGetValue(armour, out var row) && row.TryGetValue(damageType, out var m)) return m;
        return 1f;
    }

    public void Set(string armour, string damageType, float mult)
    {
        if (!_table.TryGetValue(armour, out var row)) _table[armour] = row = new Dictionary<string, float>();
        row[damageType] = mult;
    }
}
