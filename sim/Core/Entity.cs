using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>A live thing in the world: unit or building. Fat object for now; split into components as systems grow.</summary>
public sealed class Entity
{
    public int Id { get; init; }
    /// <summary>Player id, or -1 for neutral.</summary>
    public int Owner { get; set; }
    public ObjectDef Def { get; init; } = null!;
    public UnitDef? Unit => Def as UnitDef;
    public BuildingDef? Building => Def as BuildingDef;
    public bool IsBuilding => Def is BuildingDef;

    public Vec2 Pos { get; set; }
    /// <summary>Hull facing in radians, 0 = +X (east), counter-clockwise.</summary>
    public float Facing { get; set; }
    /// <summary>Absolute turret facing in radians. Equals Facing for units without a turret.</summary>
    public float TurretFacing { get; set; }
    public float Hp { get; set; }
    public float MaxHp { get; set; }
    public bool Alive { get; set; } = true;

    /// <summary>State at the previous tick, for render interpolation.</summary>
    public Vec2 PrevPos { get; set; }
    public float PrevFacing { get; set; }
    public float PrevTurretFacing { get; set; }

    public MoveOrder? Move { get; set; }
    /// <summary>An attack-move that was paused to fight; resumed when the target is gone.</summary>
    public MoveOrder? SuspendedMove { get; set; }

    /// <summary>Current combat target, or 0.</summary>
    public int TargetId { get; set; }
    /// <summary>True when the player ordered this target explicitly (chase it anywhere).</summary>
    public bool ExplicitTarget { get; set; }
    /// <summary>Per-weapon cooldown remaining, in seconds.</summary>
    public float[] Cooldowns { get; set; } = Array.Empty<float>();
    public int Kills { get; set; }
    public int Xp { get; set; }

    // --- buildings ---
    /// <summary>Footprint origin cell (buildings only).</summary>
    public int CellX { get; set; }
    public int CellY { get; set; }
    public bool UnderConstruction { get; set; }
    /// <summary>0..1 while under construction.</summary>
    public float BuildProgress { get; set; }
    public ProductionQueue? Queue { get; set; }
    public Vec2? Rally { get; set; }
    /// <summary>Seconds until the next trickle payout.</summary>
    public float TrickleTimer { get; set; }

    // --- builders ---
    /// <summary>Building entity this builder is constructing, or 0.</summary>
    public int BuildTargetId { get; set; }

    // --- harvesters ---
    public HarvestState HarvestState { get; set; }
    public int PileId { get; set; }
    public int Carried { get; set; }
    public float StateTimer { get; set; }

    // --- M4 systems ---
    public List<Status> Statuses { get; } = new();
    public float[] AbilityCooldowns { get; set; } = Array.Empty<float>();
    /// <summary>Entities riding inside (garrison or transport).</summary>
    public List<int> Passengers { get; } = new();
    /// <summary>Container this entity is inside, or 0.</summary>
    public int InsideId { get; set; }
    /// <summary>Container this unit is walking to, or 0.</summary>
    public int EnterTargetId { get; set; }
    public int CaptureTargetId { get; set; }
    public float CaptureProgress { get; set; }
    public int Ammo { get; set; }
    /// <summary>True while a heal aura or a builder is restoring this entity. Presentation only.</summary>
    public bool BeingRepaired { get; set; }
    /// <summary>Builder job: the completed building being repaired.</summary>
    public int RepairTargetId { get; set; }
    /// <summary>Aircraft ordered home. Stays on the pad until fixed or given another order.</summary>
    public bool ReturningToBase { get; set; }
    public int LastDamagedTick { get; set; } = -100000;
    /// <summary>Guard mode: the post this unit holds. Null when not guarding.</summary>
    public Vec2? GuardPos { get; set; }
    public bool Rearming { get; set; }
    public float RearmTimer { get; set; }
    public int SalvageLevel { get; set; }
    /// <summary>Leader this spawned unit escorts, or 0.</summary>
    public int FollowId { get; set; }
    public float LifetimeLeft { get; set; }
    public float SpawnTimer { get; set; }
    /// <summary>Seconds of charge accumulated by a superweapon building.</summary>
    public float SuperweaponCharge { get; set; }
    /// <summary>For holes: the building def to regrow and where its footprint origin was.</summary>
    public string HoleDefId { get; set; } = "";
    public int HoleCellX { get; set; }
    public int HoleCellY { get; set; }
    /// <summary>Horde bonus currently applied (1 = none).</summary>
    public float HordeMult { get; set; } = 1f;
    public int Level => Xp >= Def.Veterancy[2] ? 3 : Xp >= Def.Veterancy[1] ? 2 : Xp >= Def.Veterancy[0] ? 1 : 0;
    public bool IsInside => InsideId != 0;
    public bool Has(string status) { foreach (var s in Statuses) if (s.Type == status) return true; return false; }
    public bool Disabled => Has("disabled");
    /// <summary>Damage multiplier from veterancy and salvage.</summary>
    public float DamageMult => (1f + 0.1f * Level) * (1f + 0.3f * SalvageLevel) * (Has("empowered") ? 1.5f : 1f) * HordeMult;
    public float MaxHpMult => (1f + 0.1f * Level) * (1f + 0.25f * SalvageLevel);

    public float Radius => Def.Radius;
    public bool IsMoving => Move is not null;
    public bool HasWeapons => Def.Weapons.Count > 0;
    public float HpFraction => MaxHp > 0 ? Hp / MaxHp : 0f;
    public bool IsBuilder => Unit?.Builder ?? false;
    public bool IsHarvester => Unit?.Harvest is not null;
    /// <summary>A finished building or a live unit.</summary>
    public bool Operational => Alive && !UnderConstruction;

    /// <summary>Axis-aligned footprint in world units (buildings), or a square around the unit.</summary>
    public (float x0, float y0, float x1, float y1) Bounds => Building is { } b
        ? (CellX, CellY, CellX + b.Width, CellY + b.Height)
        : (Pos.X - Radius, Pos.Y - Radius, Pos.X + Radius, Pos.Y + Radius);
}

public enum HarvestState : byte { Idle, ToPile, Loading, ToCenter, Unloading }

public enum MoveKind : byte
{
    /// <summary>Go there, ignore enemies.</summary>
    Move,
    /// <summary>Go there, fight anything met on the way.</summary>
    AttackMove,
    /// <summary>Internal: closing on a combat target.</summary>
    Chase,
    /// <summary>Internal: builder or harvester travelling for its job.</summary>
    Work,
}

public sealed class MoveOrder
{
    public Vec2 Target { get; set; }
    public MoveKind Kind { get; init; } = MoveKind.Move;
    /// <summary>Distance at which the unit counts as arrived.</summary>
    public float ArriveRadius { get; init; } = 0.15f;

    internal FlowField? Field;
    internal float BestDist = float.MaxValue;
    internal int StuckTicks;
}

/// <summary>An item in a building's production queue: a unit or an upgrade.</summary>
public sealed class QueueItem
{
    public string Id { get; init; } = "";
    public bool IsUpgrade { get; init; }
    public int Cost { get; init; }
    public float Time { get; init; }
    public float Progress { get; set; }
}

public sealed class ProductionQueue
{
    public const int MaxItems = 9;
    public List<QueueItem> Items { get; } = new();
    public QueueItem? Head => Items.Count > 0 ? Items[0] : null;
}

public sealed class Status
{
    public string Type = "";
    public float Remaining;
    public float Magnitude = 1f;
    public int SourceId;
}
