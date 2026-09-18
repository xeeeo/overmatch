namespace Overmatch.Sim;

/// <summary>Everything a player or AI does is a command. Commands are applied at the start of the next tick.</summary>
public abstract record Command(int Player);

public sealed record MoveCommand(int Player, int[] Units, Vec2 Target) : Command(Player);

public sealed record AttackMoveCommand(int Player, int[] Units, Vec2 Target) : Command(Player);

public sealed record AttackCommand(int Player, int[] Units, int TargetId) : Command(Player);

/// <summary>Guard an area: go there, engage anything that comes within the guard radius, then return to post.</summary>
public sealed record GuardCommand(int Player, int[] Units, Vec2 Target) : Command(Player);

/// <summary>Scatter: every unit dashes a short way in its own direction, to get out from under artillery or a bomb.</summary>
public sealed record ScatterCommand(int Player, int[] Units) : Command(Player);

public sealed record StopCommand(int Player, int[] Units) : Command(Player);

/// <summary>Have a builder construct a building with its footprint origin at (CellX, CellY).</summary>
public sealed record BuildCommand(int Player, int BuilderId, string BuildingId, int CellX, int CellY) : Command(Player);

/// <summary>Queue a unit or upgrade at a building.</summary>
public sealed record ProduceCommand(int Player, int BuildingId, string ItemId) : Command(Player);

/// <summary>Remove one queue entry (refunds).</summary>
public sealed record CancelProduceCommand(int Player, int BuildingId, int Index) : Command(Player);

public sealed record RallyCommand(int Player, int BuildingId, Vec2 Target) : Command(Player);

/// <summary>Sell a building for half its cost (full refund while under construction).</summary>
public sealed record SellCommand(int Player, int BuildingId) : Command(Player);

/// <summary>Send harvesters to a pile (PileId 0 = nearest).</summary>
public sealed record HarvestCommand(int Player, int[] Units, int PileId) : Command(Player);

/// <summary>Send builders to help on an existing construction site.</summary>
public sealed record AssistBuildCommand(int Player, int[] Units, int BuildingId) : Command(Player);

/// <summary>Use a unit ability. Target point or entity depending on the ability.</summary>
/// <summary>Builders repair a finished, damaged building of their own. Free, like the original.</summary>
public sealed record RepairCommand(int Player, int[] Units, int BuildingId) : Command(Player);

/// <summary>Aircraft fly to the nearest own airfield to be repaired (and rearmed, if they carry ammo).</summary>
public sealed record ReturnToBaseCommand(int Player, int[] Units) : Command(Player);

public sealed record AbilityCommand(int Player, int[] Units, string AbilityId, Vec2 Target, int TargetId) : Command(Player);

/// <summary>Infantry enter a garrisonable building, transport or tunnel.</summary>
public sealed record GarrisonCommand(int Player, int[] Units, int ContainerId) : Command(Player);

/// <summary>Everyone out of a building/transport (tunnels: out of the network at this tunnel).</summary>
public sealed record UngarrisonCommand(int Player, int ContainerId) : Command(Player);

public sealed record CaptureCommand(int Player, int[] Units, int BuildingId) : Command(Player);

public sealed record BuyPowerCommand(int Player, string PowerId) : Command(Player);

public sealed record UsePowerCommand(int Player, string PowerId, Vec2 Target) : Command(Player);

public sealed record FireSuperweaponCommand(int Player, int BuildingId, Vec2 Target) : Command(Player);
