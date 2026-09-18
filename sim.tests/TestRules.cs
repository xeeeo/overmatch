using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public static class TestRules
{
    public const string TankJson = """
        { "id": "tank", "name": "Tank", "faction": "test", "speed": 4.0, "turnRate": 360, "turretTurnRate": 720, "radius": 0.5, "hp": 100, "armour": "tank", "vision": 10, "weapons": ["cannon"], "xpValue": 25 }
        """;

    public const string ScoutJson = """
        { "id": "scout", "name": "Scout", "faction": "test", "speed": 6.0, "turnRate": 360, "radius": 0.4, "hp": 50, "armour": "light_vehicle", "vision": 12 }
        """;

    public const string CannonJson = """
        { "id": "cannon", "damage": 40, "damageType": "armour_piercing", "range": 6, "cooldown": 1.0, "projectile": { "kind": "shell", "speed": 40 } }
        """;

    public const string ArmourJson = """
        { "tank": { "armour_piercing": 1.0, "small_arms": 0.1 }, "light_vehicle": { "armour_piercing": 1.5 } }
        """;

    public static GameRules Basic() => GameRules.Load(new[]
    {
        new DataFile("units/test/tank.json", TankJson),
        new DataFile("units/test/scout.json", ScoutJson),
        new DataFile("weapons/cannon.json", CannonJson),
        new DataFile("armour.json", ArmourJson),
        new DataFile("factions/test.json", """{ "id": "test", "name": "Test", "colour": "#ffffff" }"""),
        new DataFile("README.md", "ignored"),
    });

    /// <summary>A 40×40 map with a vertical wall from (20,5) to (20,35): the only way round is via the top or bottom gap.</summary>
    public static MapDef WalledMap() => new()
    {
        Id = "walled", Width = 40, Height = 40,
        Blocked = { new RectDef { X = 20, Y = 5, W = 1, H = 30 } },
    };

    public static void Run(World w, int ticks)
    {
        for (var i = 0; i < ticks; i++) w.Step();
    }
}
