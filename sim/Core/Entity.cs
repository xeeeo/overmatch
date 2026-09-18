using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>A live thing in the world: unit or building. Fat object for now; split into components as systems grow.</summary>
public sealed class Entity
{
    public int Id { get; init; }
    public int Owner { get; init; }
    public UnitDef Def { get; init; } = null!;

    public Vec2 Pos { get; set; }
    /// <summary>Hull facing in radians, 0 = +X (east), counter-clockwise.</summary>
    public float Facing { get; set; }
    /// <summary>Absolute turret facing in radians. Equals Facing for units without a turret.</summary>
    public float TurretFacing { get; set; }
    public float Hp { get; set; }
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

    public float Radius => Def.Radius;
    public bool IsMoving => Move is not null;
    public bool HasWeapons => Def.Weapons.Count > 0;
    public float HpFraction => Def.Hp > 0 ? Hp / Def.Hp : 0f;
}

public enum MoveKind : byte
{
    /// <summary>Go there, ignore enemies.</summary>
    Move,
    /// <summary>Go there, fight anything met on the way.</summary>
    AttackMove,
    /// <summary>Internal: closing on a combat target.</summary>
    Chase,
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
