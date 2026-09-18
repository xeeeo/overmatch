namespace Overmatch.Sim;

/// <summary>Things that happened during a tick, for presentation (VFX, audio) and tests. Cleared at the start of every Step.</summary>
public abstract record GameEvent;

public sealed record SpawnedEvent(int EntityId) : GameEvent;

public sealed record WeaponFiredEvent(int SourceId, string WeaponId, Vec2 From, Vec2 To, int ProjectileId) : GameEvent;

public sealed record HitEvent(Vec2 Pos, string WeaponId, int TargetId) : GameEvent;

public sealed record DamagedEvent(int EntityId, float Amount, int AttackerId) : GameEvent;

public sealed record DiedEvent(int EntityId, string DefId, int Owner, Vec2 Pos, float Facing, float TurretFacing, int KillerId) : GameEvent;
