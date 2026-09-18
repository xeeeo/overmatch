namespace Overmatch.Sim;

/// <summary>Everything a player or AI does is a command. Commands are applied at the start of the next tick.</summary>
public abstract record Command(int Player);

public sealed record MoveCommand(int Player, int[] Units, Vec2 Target) : Command(Player);

public sealed record StopCommand(int Player, int[] Units) : Command(Player);
