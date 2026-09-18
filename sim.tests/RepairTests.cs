using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

/// <summary>Repair: airfields fix aircraft, vehicle factories fix vehicles, barracks heal infantry, builders fix buildings.</summary>
public class RepairTests
{
    private static World NewWorld() =>
        new(RealDataTests.LoadShipped(), new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "coalition", "coalition" });

    [Fact]
    public void Aircraft_ReturnToBase_FliesHomeAndIsRepaired()
    {
        var w = NewWorld();
        var field = w.PlaceBuilding("coalition_airfield", 0, 10, 10);
        w.PlaceBuilding("coalition_power_plant", 0, 20, 10);
        var heli = w.Spawn("coalition_kestrel", 0, new Vec2(60, 60));
        heli.Hp = heli.MaxHp * 0.3f;
        w.Submit(new ReturnToBaseCommand(0, new[] { heli.Id }));
        TestRules.RunUntil(w, () => heli.Hp >= heli.MaxHp, 20 * 90);
        Assert.Equal(heli.MaxHp, heli.Hp);
        Assert.True((heli.Pos - field.Pos).Length < 8f, "should be holding over the airfield");
        w.Step();
        Assert.False(heli.ReturningToBase);
    }

    [Fact]
    public void Aircraft_AwayFromAirfield_DoNotHeal_AndOrderNeedsAnAirfield()
    {
        var w = NewWorld();
        var heli = w.Spawn("coalition_kestrel", 0, new Vec2(60, 60));
        heli.Hp = 100;
        w.Submit(new ReturnToBaseCommand(0, new[] { heli.Id }));
        var rejected = false;
        for (var i = 0; i < 100; i++) { w.Step(); rejected |= w.Events.OfType<OrderRejectedEvent>().Any(r => r.Reason == "no_airfield"); }
        Assert.True(rejected);
        Assert.Equal(100, heli.Hp);
    }

    [Fact]
    public void RepairBays_OnlyFixTheirOwnKind()
    {
        var w = NewWorld();
        w.PlaceBuilding("coalition_power_plant", 0, 40, 40);
        var pool = w.PlaceBuilding("coalition_motor_pool", 0, 20, 20);
        var tank = w.Spawn("coalition_bulwark", 0, pool.Pos + new Vec2(5, 0));
        var soldier = w.Spawn("coalition_rifleman", 0, pool.Pos + new Vec2(5, 1.5f));
        var enemy = w.Spawn("coalition_bulwark", 1, pool.Pos + new Vec2(-5, 60));
        tank.Hp = 50; soldier.Hp = 10; enemy.Hp = 50;
        TestRules.Run(w, 20 * 10);
        Assert.True(tank.Hp > 110, $"tank should be repaired, hp {tank.Hp}");
        Assert.True(tank.BeingRepaired);
        Assert.Equal(10, soldier.Hp);
        Assert.Equal(50, enemy.Hp);
    }

    [Fact]
    public void Builder_RepairsDamagedBuilding_ForFree()
    {
        var w = NewWorld();
        var plant = w.PlaceBuilding("coalition_power_plant", 0, 30, 30);
        var dozer = w.Spawn("coalition_dozer", 0, new Vec2(20, 31));
        plant.Hp = plant.MaxHp * 0.5f;
        var cash = w.Player(0).Cash;
        w.Submit(new RepairCommand(0, new[] { dozer.Id }, plant.Id));
        TestRules.RunUntil(w, () => plant.Hp >= plant.MaxHp, 20 * 60);
        Assert.Equal(plant.MaxHp, plant.Hp);
        Assert.Equal(cash, w.Player(0).Cash);
        w.Step();
        Assert.Equal(0, dozer.RepairTargetId);
    }

    [Fact]
    public void Ai_SendsDamagedAircraftHome_AndRepairsItsBase()
    {
        var rules = RealDataTests.LoadShipped();
        var map = rules.Map("plain");
        var w = new World(rules, map, new[] { "coalition", "coalition" }, seed: 3);
        for (var p = 0; p < 2; p++)
        {
            w.PlaceBuilding("coalition_command_post", p, (int)map.Spawns[p].X - 2, (int)map.Spawns[p].Y - 2);
            w.Spawn("coalition_dozer", p, new Vec2(map.Spawns[p].X, map.Spawns[p].Y - 5));
            w.Spawn("coalition_dozer", p, new Vec2(map.Spawns[p].X + 2, map.Spawns[p].Y - 5));
        }
        w.AddAi(1, rules.Ai("coalition", "hard"));
        var s = map.Spawns[1];
        w.PlaceBuilding("coalition_power_plant", 1, (int)s.X + 8, (int)s.Y + 8);
        var field = w.PlaceBuilding("coalition_airfield", 1, (int)s.X - 14, (int)s.Y + 6);
        var heli = w.Spawn("coalition_kestrel", 1, new Vec2(s.X, s.Y - 20));
        heli.Hp = heli.MaxHp * 0.2f;
        var hq = w.Entities.First(e => e.Owner == 1 && e.Building is { Hq: true });
        hq.Hp = hq.MaxHp * 0.5f;
        TestRules.RunUntil(w, () => heli.Hp >= heli.MaxHp * 0.9f && hq.Hp > hq.MaxHp * 0.6f, 20 * 120);
        Assert.True(heli.Hp >= heli.MaxHp * 0.9f, $"helicopter hp {heli.Hp}/{heli.MaxHp}");
        Assert.True(hq.Hp > hq.MaxHp * 0.6f, $"hq hp {hq.Hp}/{hq.MaxHp}");
    }
}
