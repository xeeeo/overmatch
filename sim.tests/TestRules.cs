using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public static class TestRules
{
    public const string TankJson = """
        { "id": "tank", "name": "Tank", "faction": "test", "cost": 500, "buildTime": 2, "speed": 4.0, "turnRate": 360, "turretTurnRate": 720, "radius": 0.5, "hp": 100, "armour": "tank", "vision": 10, "weapons": ["cannon"], "xpValue": 25, "tags": ["vehicle"] }
        """;

    public const string ScoutJson = """
        { "id": "scout", "name": "Scout", "faction": "test", "cost": 200, "buildTime": 1, "speed": 6.0, "turnRate": 360, "radius": 0.4, "hp": 50, "armour": "light_vehicle", "vision": 12 }
        """;

    public const string DozerJson = """
        { "id": "dozer", "name": "Dozer", "faction": "test", "cost": 1000, "buildTime": 2, "speed": 4.0, "turnRate": 360, "radius": 0.5, "hp": 200, "armour": "light_vehicle", "vision": 8, "builder": true }
        """;

    public const string TruckJson = """
        { "id": "truck", "name": "Truck", "faction": "test", "cost": 600, "buildTime": 2, "speed": 5.0, "turnRate": 360, "radius": 0.5, "hp": 200, "armour": "harvester", "vision": 8,
          "harvest": { "capacity": 300, "loadTime": 1, "unloadTime": 0.5, "searchRadius": 30 } }
        """;

    public const string HqJson = """
        { "id": "hq", "name": "HQ", "faction": "test", "cost": 0, "buildTime": 10, "footprint": [4, 4], "hp": 3000, "armour": "structure", "vision": 12, "hq": true, "produces": ["dozer"] }
        """;

    public const string PowerJson = """
        { "id": "power", "name": "Power Plant", "faction": "test", "cost": 800, "buildTime": 4, "footprint": [3, 3], "hp": 1000, "armour": "structure", "power": 10 }
        """;

    public const string FactoryJson = """
        { "id": "factory", "name": "Factory", "faction": "test", "cost": 2000, "buildTime": 4, "footprint": [4, 3], "hp": 2000, "armour": "structure", "power": -5,
          "prereqs": ["power"], "produces": ["tank", "scout"], "upgrades": ["big_guns"] }
        """;

    public const string DepotJson = """
        { "id": "depot", "name": "Supply Depot", "faction": "test", "cost": 1500, "buildTime": 4, "footprint": [4, 4], "hp": 1500, "armour": "structure", "power": -3,
          "supplyCenter": true, "produces": ["truck"] }
        """;

    public const string TurretJson = """
        { "id": "turret", "name": "Turret", "faction": "test", "cost": 900, "buildTime": 3, "footprint": [2, 2], "hp": 800, "armour": "fortification", "power": -2,
          "weapons": ["cannon"], "turretTurnRate": 360, "vision": 10 }
        """;

    public const string TrickleJson = """
        { "id": "bank", "name": "Bank", "faction": "test", "cost": 1500, "buildTime": 4, "footprint": [3, 3], "hp": 1000, "armour": "structure", "trickle": { "amount": 100, "interval": 2 } }
        """;

    public const string CannonJson = """
        { "id": "cannon", "damage": 40, "damageType": "armour_piercing", "range": 6, "cooldown": 1.0, "projectile": { "kind": "shell", "speed": 40 } }
        """;

    public const string UpgradeJson = """
        { "id": "big_guns", "name": "Big Guns", "faction": "test", "cost": 1000, "time": 2, "researchedAt": "factory",
          "effects": [ { "type": "weaponDamage", "weapon": "cannon", "mult": 1.5 }, { "type": "hp", "tag": "vehicle", "mult": 2.0 } ] }
        """;

    public const string ArmourJson = """
        { "tank": { "armour_piercing": 1.0, "small_arms": 0.1 }, "light_vehicle": { "armour_piercing": 1.5 } }
        """;

    public static GameRules Basic() => GameRules.Load(new[]
    {
        new DataFile("units/test/tank.json", TankJson),
        new DataFile("units/test/scout.json", ScoutJson),
        new DataFile("units/test/dozer.json", DozerJson),
        new DataFile("units/test/truck.json", TruckJson),
        new DataFile("buildings/test/hq.json", HqJson),
        new DataFile("buildings/test/power.json", PowerJson),
        new DataFile("buildings/test/factory.json", FactoryJson),
        new DataFile("buildings/test/depot.json", DepotJson),
        new DataFile("buildings/test/turret.json", TurretJson),
        new DataFile("buildings/test/bank.json", TrickleJson),
        new DataFile("weapons/cannon.json", CannonJson),
        new DataFile("upgrades/big_guns.json", UpgradeJson),
        new DataFile("armour.json", ArmourJson),
        new DataFile("factions/test.json", """{ "id": "test", "name": "Test", "colour": "#ffffff", "builder": "dozer", "hq": "hq", "startingCash": 10000 }"""),
        new DataFile("README.md", "ignored"),
    });

    /// <summary>A 40×40 map with a vertical wall from (20,5) to (20,35): the only way round is via the top or bottom gap.</summary>
    public static MapDef WalledMap() => new()
    {
        Id = "walled", Width = 40, Height = 40,
        Blocked = { new RectDef { X = 20, Y = 5, W = 1, H = 30 } },
    };

    public static MapDef SupplyMap() => new()
    {
        Id = "supply", Width = 64, Height = 64,
        Supplies = { new SupplyDef { X = 30, Y = 10, Amount = 1000 } },
    };

    public static void Run(World w, int ticks)
    {
        for (var i = 0; i < ticks; i++) w.Step();
    }

    public static void RunUntil(World w, Func<bool> done, int maxTicks)
    {
        for (var i = 0; i < maxTicks && !done(); i++) w.Step();
    }
}
