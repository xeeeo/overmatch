using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>Per-player match state: cash, power, upgrades.</summary>
public sealed class Player
{
    public int Id { get; }
    public FactionDef Faction { get; }
    public int Cash { get; set; }
    public int PowerSupply { get; internal set; }
    public int PowerDemand { get; internal set; }
    public bool Eliminated { get; internal set; }
    public int Xp { get; internal set; }
    /// <summary>Multiplier on harvested and trickled income (AI handicap).</summary>
    public float IncomeMult { get; set; } = 1f;
    /// <summary>Display name for menus and results.</summary>
    public string Name { get; set; } = "";
    public bool IsAi { get; internal set; }
    public int Rank { get; internal set; } = 1;
    public int Points { get; internal set; } = 1;
    private readonly HashSet<string> _powers = new();
    private readonly Dictionary<string, float> _powerReady = new();
    public IReadOnlyCollection<string> PowersOwned => _powers;
    public bool HasPower(string id) => _powers.Contains(id);
    internal void GrantPower(string id) => _powers.Add(id);
    /// <summary>Sim time at which the power can be used again.</summary>
    public float PowerReadyAt(string id) => _powerReady.GetValueOrDefault(id, 0f);
    internal void SetPowerReadyAt(string id, float t) => _powerReady[id] = t;
    /// <summary>Fraction of a victim's cost paid on each kill (Cash Bounty).</summary>
    public float BountyPerKill { get; internal set; }
    public float DiscountMult { get; internal set; } = 1f;
    public float DiscountUntil { get; internal set; }
    /// <summary>Units waiting inside the shared tunnel network.</summary>
    public List<int> TunnelPool { get; } = new();

    private readonly HashSet<string> _upgrades = new();
    private readonly Dictionary<string, float> _weaponDamageMult = new();
    private readonly Dictionary<string, float> _hpMultByUnit = new();
    private readonly Dictionary<string, float> _hpMultByTag = new();
    private readonly Dictionary<string, float> _speedMultByUnit = new();
    private readonly Dictionary<string, float> _speedMultByTag = new();
    private readonly Dictionary<string, float> _powerAddByBuilding = new();
    /// <summary>Damage bonus for horde units in a group (0.25 base, upgrades add).</summary>
    public float HordeBonus { get; private set; } = 0.25f;
    private readonly HashSet<string> _stealthTags = new();
    /// <summary>Upgrades can make whole unit classes stealthy (Network camouflage).</summary>
    public bool StealthFor(ObjectDef def)
    {
        if (_stealthTags.Count == 0) return false;
        foreach (var t in def.Tags) if (_stealthTags.Contains(t)) return true;
        return _stealthTags.Contains(def.Id);
    }

    public Player(int id, FactionDef faction)
    {
        Id = id;
        Faction = faction;
        Cash = faction.StartingCash;
    }

    public IReadOnlyCollection<string> Upgrades => _upgrades;
    public bool Has(string upgradeId) => _upgrades.Contains(upgradeId);
    /// <summary>True when demand exceeds supply and the faction cares about power.</summary>
    public bool LowPower => Faction.NeedsPower && PowerDemand > PowerSupply;

    internal void GrantUpgrade(UpgradeDef def)
    {
        if (!_upgrades.Add(def.Id)) return;
        foreach (var fx in def.Effects)
        {
            switch (fx.Type)
            {
                case "weaponDamage": Mul(_weaponDamageMult, fx.Weapon, fx.Mult); break;
                case "hp":
                    if (fx.Unit != "") Mul(_hpMultByUnit, fx.Unit, fx.Mult);
                    if (fx.Tag != "") Mul(_hpMultByTag, fx.Tag, fx.Mult);
                    break;
                case "speed":
                    if (fx.Unit != "") Mul(_speedMultByUnit, fx.Unit, fx.Mult);
                    if (fx.Tag != "") Mul(_speedMultByTag, fx.Tag, fx.Mult);
                    break;
                case "power": _powerAddByBuilding[fx.Building] = _powerAddByBuilding.GetValueOrDefault(fx.Building) + fx.Add; break;
                case "horde": HordeBonus += fx.Add; break;
                case "stealth": if (fx.Tag != "") _stealthTags.Add(fx.Tag); if (fx.Unit != "") _stealthTags.Add(fx.Unit); break;
            }
        }
    }

    private static void Mul(Dictionary<string, float> d, string key, float m) => d[key] = d.GetValueOrDefault(key, 1f) * m;

    public float WeaponDamageMult(string weaponId) => _weaponDamageMult.GetValueOrDefault(weaponId, 1f);

    public float HpMult(ObjectDef def)
    {
        var m = _hpMultByUnit.GetValueOrDefault(def.Id, 1f);
        foreach (var t in def.Tags) m *= _hpMultByTag.GetValueOrDefault(t, 1f);
        return m;
    }

    public float SpeedMult(ObjectDef def)
    {
        var m = _speedMultByUnit.GetValueOrDefault(def.Id, 1f);
        foreach (var t in def.Tags) m *= _speedMultByTag.GetValueOrDefault(t, 1f);
        return m;
    }

    public float PowerAdd(string buildingId) => _powerAddByBuilding.GetValueOrDefault(buildingId);
}
