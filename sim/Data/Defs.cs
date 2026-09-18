using System.Text.Json;
using System.Text.Json.Serialization;

namespace Overmatch.Sim.Data;

/// <summary>Fields shared by units and buildings.</summary>
public abstract class ObjectDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public int Cost { get; set; }
    /// <summary>Seconds to produce (units) or construct with one builder (buildings).</summary>
    public float BuildTime { get; set; } = 10f;
    public float Hp { get; set; } = 100f;
    public string Armour { get; set; } = "infantry";
    public float Vision { get; set; } = 8f;
    public List<string> Weapons { get; set; } = new();
    /// <summary>Degrees per second for the turret. 0 means no turret: the hull must face the target.</summary>
    public float TurretTurnRate { get; set; } = 0f;
    /// <summary>Buildings (or tech tags) that must exist before this can be built.</summary>
    public List<string> Prereqs { get; set; } = new();
    public string Model { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    /// <summary>XP awarded to the killer.</summary>
    public int XpValue { get; set; } = 10;
    public string Description { get; set; } = "";

    [JsonIgnore] public bool HasTurret => TurretTurnRate > 0f;
    [JsonIgnore] public virtual bool IsAir => false;
    [JsonIgnore] public abstract float Radius { get; }
    [JsonIgnore] public bool IsBuilding => this is BuildingDef;
}

public sealed class HarvestDef
{
    /// <summary>Supplies carried per trip.</summary>
    public int Capacity { get; set; } = 300;
    /// <summary>Seconds spent loading at a pile.</summary>
    public float LoadTime { get; set; } = 4f;
    /// <summary>Seconds spent unloading at a supply centre.</summary>
    public float UnloadTime { get; set; } = 1.5f;
    /// <summary>How far from a supply centre a harvester will look for piles on its own.</summary>
    public float SearchRadius { get; set; } = 32f;
}

public sealed class UnitDef : ObjectDef
{
    public string BuiltAt { get; set; } = "";
    public string Locomotor { get; set; } = "tracked";
    /// <summary>World units per second.</summary>
    public float Speed { get; set; } = 3f;
    /// <summary>Degrees per second.</summary>
    public float TurnRate { get; set; } = 180f;
    /// <summary>Collision radius in world units.</summary>
    [JsonPropertyName("radius")] public float CollisionRadius { get; set; } = 0.5f;
    /// <summary>Metres above ground for air units (presentation only).</summary>
    public float FlightHeight { get; set; } = 0f;
    /// <summary>Can construct buildings.</summary>
    public bool Builder { get; set; }
    /// <summary>Non-null for harvesters.</summary>
    public HarvestDef? Harvest { get; set; }

    [JsonIgnore] public Locomotor LocomotorClass => LocomotorParse.Parse(Locomotor);
    [JsonIgnore] public override bool IsAir => LocomotorClass == Sim.Locomotor.Air;
    [JsonIgnore] public override float Radius => CollisionRadius;
}

public sealed class TrickleDef
{
    public int Amount { get; set; } = 100;
    public float Interval { get; set; } = 10f;
}

public sealed class BuildingDef : ObjectDef
{
    /// <summary>[width, height] in cells.</summary>
    public int[] Footprint { get; set; } = { 3, 3 };
    /// <summary>Positive supplies power, negative consumes it.</summary>
    public int Power { get; set; }
    /// <summary>Unit ids this building can produce.</summary>
    public List<string> Produces { get; set; } = new();
    /// <summary>Upgrade ids researched here.</summary>
    public List<string> Upgrades { get; set; } = new();
    /// <summary>Tech tags this building satisfies for prereqs (its own id always counts).</summary>
    public List<string> Provides { get; set; } = new();
    public int GarrisonSlots { get; set; }
    public TrickleDef? Trickle { get; set; }
    /// <summary>Harvesters unload here.</summary>
    public bool SupplyCenter { get; set; }
    /// <summary>The player's headquarters; losing all of these and all builders means elimination.</summary>
    public bool Hq { get; set; }
    /// <summary>Weapons stop working when the base is under-powered.</summary>
    public bool NeedsPower { get; set; } = true;

    [JsonIgnore] public int Width => Footprint.Length > 0 ? Footprint[0] : 3;
    [JsonIgnore] public int Height => Footprint.Length > 1 ? Footprint[1] : Width;
    [JsonIgnore] public override float Radius => MathF.Max(Width, Height) * 0.5f;
}

public sealed class EffectDef
{
    /// <summary>"weaponDamage" (weapon, mult), "hp" (tag or unit, mult), "speed" (tag or unit, mult), "power" (building, add).</summary>
    public string Type { get; set; } = "";
    public string Weapon { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Tag { get; set; } = "";
    public string Building { get; set; } = "";
    public float Mult { get; set; } = 1f;
    public float Add { get; set; }
}

public sealed class UpgradeDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public int Cost { get; set; }
    public float Time { get; set; } = 30f;
    public string ResearchedAt { get; set; } = "";
    public List<string> Prereqs { get; set; } = new();
    public List<EffectDef> Effects { get; set; } = new();
    public string Description { get; set; } = "";
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
    public string Hq { get; set; } = "";
    public bool NeedsPower { get; set; } = true;
    public int StartingCash { get; set; } = 10000;
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

public sealed class SupplyDef
{
    public float X { get; set; }
    public float Y { get; set; }
    public int Amount { get; set; } = 30000;
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
    public List<SupplyDef> Supplies { get; set; } = new();
}

/// <summary>A data file's relative path (e.g. "units/coalition/bulwark.json") and its JSON text.</summary>
public readonly record struct DataFile(string Path, string Json);

/// <summary>All static game data for a match. Immutable after load.</summary>
public sealed class GameRules
{
    public IReadOnlyDictionary<string, UnitDef> Units { get; }
    public IReadOnlyDictionary<string, BuildingDef> Buildings { get; }
    public IReadOnlyDictionary<string, WeaponDef> Weapons { get; }
    public IReadOnlyDictionary<string, UpgradeDef> Upgrades { get; }
    public IReadOnlyDictionary<string, FactionDef> Factions { get; }
    public IReadOnlyDictionary<string, MapDef> Maps { get; }
    public ArmourTable Armour { get; }

    private GameRules(Dictionary<string, UnitDef> units, Dictionary<string, BuildingDef> buildings, Dictionary<string, WeaponDef> weapons,
        Dictionary<string, UpgradeDef> upgrades, Dictionary<string, FactionDef> factions, Dictionary<string, MapDef> maps, ArmourTable armour)
    {
        Units = units;
        Buildings = buildings;
        Weapons = weapons;
        Upgrades = upgrades;
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
        var buildings = new Dictionary<string, BuildingDef>();
        var weapons = new Dictionary<string, WeaponDef>();
        var upgrades = new Dictionary<string, UpgradeDef>();
        var factions = new Dictionary<string, FactionDef>();
        var maps = new Dictionary<string, MapDef>();
        var armour = new ArmourTable();

        foreach (var file in files)
        {
            var path = file.Path.Replace('\\', '/');
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            if (path.StartsWith("units/")) Add(units, Parse<UnitDef>(file), d => d.Id, path);
            else if (path.StartsWith("buildings/")) Add(buildings, Parse<BuildingDef>(file), d => d.Id, path);
            else if (path.StartsWith("weapons/")) Add(weapons, Parse<WeaponDef>(file), d => d.Id, path);
            else if (path.StartsWith("upgrades/")) Add(upgrades, Parse<UpgradeDef>(file), d => d.Id, path);
            else if (path.StartsWith("factions/")) Add(factions, Parse<FactionDef>(file), d => d.Id, path);
            else if (path.StartsWith("maps/")) Add(maps, Parse<MapDef>(file), d => d.Id, path);
            else if (path == "armour.json") armour = ArmourTable.Parse(file.Json);
        }

        foreach (var def in units.Values.Cast<ObjectDef>().Concat(buildings.Values))
            foreach (var w in def.Weapons)
                if (!weapons.ContainsKey(w)) throw new InvalidDataException($"'{def.Id}' references unknown weapon '{w}'");
        foreach (var b in buildings.Values)
        {
            foreach (var u in b.Produces)
                if (!units.ContainsKey(u)) throw new InvalidDataException($"Building '{b.Id}' produces unknown unit '{u}'");
            foreach (var u in b.Upgrades)
                if (!upgrades.ContainsKey(u)) throw new InvalidDataException($"Building '{b.Id}' offers unknown upgrade '{u}'");
        }

        return new GameRules(units, buildings, weapons, upgrades, factions, maps, armour);
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

    public BuildingDef Building(string id) =>
        Buildings.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Unknown building '{id}'");

    public ObjectDef Object(string id) =>
        Units.TryGetValue(id, out var u) ? u : Buildings.TryGetValue(id, out var b) ? b : throw new KeyNotFoundException($"Unknown object '{id}'");

    public WeaponDef Weapon(string id) =>
        Weapons.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Unknown weapon '{id}'");

    public UpgradeDef Upgrade(string id) =>
        Upgrades.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Unknown upgrade '{id}'");

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
