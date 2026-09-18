using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public static class TestRules
{
    public const string TankJson = """
        { "id": "tank", "name": "Tank", "faction": "test", "speed": 4.0, "turnRate": 360, "radius": 0.5, "hp": 100 }
        """;

    public static GameRules Basic() => GameRules.Load(new[]
    {
        new DataFile("units/test/tank.json", TankJson),
        new DataFile("factions/test.json", """{ "id": "test", "name": "Test", "colour": "#ffffff" }"""),
        new DataFile("README.md", "ignored"),
    });
}
