namespace Overmatch.Sim;

/// <summary>Things that happened during a tick, for presentation (VFX, audio) and tests. Cleared at the start of every Step.</summary>
public abstract record GameEvent;

public sealed record SpawnedEvent(int EntityId) : GameEvent;

public sealed record WeaponFiredEvent(int SourceId, string WeaponId, Vec2 From, Vec2 To, int ProjectileId) : GameEvent;

public sealed record HitEvent(Vec2 Pos, string WeaponId, int TargetId) : GameEvent;

public sealed record DamagedEvent(int EntityId, float Amount, int AttackerId) : GameEvent;

public sealed record DiedEvent(int EntityId, string DefId, int Owner, Vec2 Pos, float Facing, float TurretFacing, int KillerId, bool WasBuilding) : GameEvent;

public sealed record ConstructionStartedEvent(int BuildingId, int Owner) : GameEvent;

public sealed record ConstructionCompletedEvent(int BuildingId, int Owner) : GameEvent;

public sealed record ProductionCompletedEvent(int BuildingId, int Owner, string ItemId, int EntityId) : GameEvent;

public sealed record UpgradeCompletedEvent(int Owner, string UpgradeId) : GameEvent;

public sealed record SoldEvent(int BuildingId, int Owner, int Refund) : GameEvent;

public sealed record SupplyDeliveredEvent(int HarvesterId, int Owner, int Amount) : GameEvent;

public sealed record OrderRejectedEvent(int Player, string Reason) : GameEvent;
