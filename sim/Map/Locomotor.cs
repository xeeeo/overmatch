namespace Overmatch.Sim;

public enum Locomotor : byte { Infantry, Wheeled, Tracked, Air, Hover }

public static class LocomotorParse
{
    public static Locomotor Parse(string s) => s.ToLowerInvariant() switch
    {
        "infantry" => Locomotor.Infantry,
        "wheeled" => Locomotor.Wheeled,
        "tracked" => Locomotor.Tracked,
        "air" => Locomotor.Air,
        "hover" => Locomotor.Hover,
        _ => throw new ArgumentException($"Unknown locomotor '{s}'"),
    };
}
