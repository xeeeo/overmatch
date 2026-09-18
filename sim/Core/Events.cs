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

/// <summary>A trickle building (Drop Zone, Cyber Center, Black Market, Oil Derrick) paid out.</summary>
public sealed record IncomeEvent(int BuildingId, int Owner, int Amount, Vec2 Pos) : GameEvent;

public sealed record SupplyDeliveredEvent(int HarvesterId, int Owner, int Amount) : GameEvent;

public sealed record OrderRejectedEvent(int Player, string Reason) : GameEvent;

public sealed record PlayerEliminatedEvent(int Player) : GameEvent;

public sealed record MatchEndedEvent(int Winner) : GameEvent;

public sealed record StrikeImpactEvent(Vec2 Pos, float Radius, string Kind) : GameEvent;

public sealed record PowerUsedEvent(int Player, string PowerId, Vec2 Target) : GameEvent;

public sealed record SuperweaponFiredEvent(int Player, string Name, Vec2 Target, float Delay) : GameEvent;

public sealed record SuperweaponReadyEvent(int Player, int BuildingId) : GameEvent;

public sealed record RankUpEvent(int Player, int Rank) : GameEvent;

public sealed record CapturedEvent(int BuildingId, int OldOwner, int NewOwner) : GameEvent;

public sealed record GarrisonEvent(int ContainerId, int UnitId, bool Entered) : GameEvent;

public sealed record HazardEvent(Vec2 Pos, float Radius, float Duration, string DamageType) : GameEvent;

public sealed record CrateEvent(int CrateId, Vec2 Pos, bool Spawned) : GameEvent;

public sealed record AbilityUsedEvent(int UnitId, string AbilityId, Vec2 Target) : GameEvent;

public sealed record HoleEvent(int HoleId, string BuildingId, bool Regrew) : GameEvent;
