using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>An in-flight shot. Instant weapons never create one.</summary>
public sealed class Projectile
{
    public int Id { get; init; }
    public WeaponDef Weapon { get; init; } = null!;
    public int Owner { get; init; }
    public int SourceId { get; init; }
    public int TargetId { get; init; }
    public Vec2 Pos { get; set; }
    public Vec2 PrevPos { get; set; }
    /// <summary>Where it is heading. Missiles update this to the target's position every tick.</summary>
    public Vec2 Aim { get; set; }
    public bool Homing { get; init; }
    public bool Alive { get; set; } = true;
    public float Progress { get; set; }
    public float TotalDistance { get; init; }
}
