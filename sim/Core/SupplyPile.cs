namespace Overmatch.Sim;

/// <summary>A finite pile of supplies on the map. Not an entity: it has no owner and cannot be attacked.</summary>
public sealed class SupplyPile
{
    public int Id { get; init; }
    public Vec2 Pos { get; init; }
    public int Initial { get; init; }
    public int Remaining { get; set; }
    public bool Depleted => Remaining <= 0;
    public float Fraction => Initial > 0 ? Remaining / (float)Initial : 0f;
}
