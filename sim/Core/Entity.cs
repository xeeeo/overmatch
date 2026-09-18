using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>A live thing in the world: unit, building, projectile. Fat object for now; split into components as systems grow.</summary>
public sealed class Entity
{
    public int Id { get; init; }
    public int Owner { get; init; }
    public UnitDef Def { get; init; } = null!;

    public Vec2 Pos { get; set; }
    /// <summary>Facing in radians, 0 = +X (east), counter-clockwise.</summary>
    public float Facing { get; set; }
    public float Hp { get; set; }
    public bool Alive { get; set; } = true;

    /// <summary>State at the previous tick, for render interpolation.</summary>
    public Vec2 PrevPos { get; set; }
    public float PrevFacing { get; set; }

    public MoveOrder? Move { get; set; }

    public float Radius => Def.Radius;
    public bool IsMoving => Move is not null;
}

public sealed class MoveOrder
{
    public Vec2 Target { get; init; }
    /// <summary>Distance at which the unit counts as arrived.</summary>
    public float ArriveRadius { get; init; } = 0.15f;
}
