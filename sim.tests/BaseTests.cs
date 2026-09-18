using Overmatch.Sim;

namespace Overmatch.Sim.Tests;

public class BaseTests
{
    private static (World w, Entity dozer) Setup()
    {
        var w = new World(TestRules.Basic());
        w.PlaceBuilding("hq", 0, 4, 4);
        var dozer = w.Spawn("dozer", 0, new Vec2(10, 6));
        return (w, dozer);
    }

    [Fact]
    public void PlaceBuilding_MarksCellsAndPower()
    {
        var (w, _) = Setup();
        Assert.Equal(CellType.Structure, w.Grid.Get(5, 5));
        Assert.False(w.Grid.IsPassable(5, 5, Locomotor.Tracked));
        Assert.NotEqual(0, w.BuildingAt(7, 7));
        Assert.Equal(0, w.BuildingAt(8, 8));
        w.PlaceBuilding("power", 0, 20, 20);
        Assert.Equal(10, w.Player(0).PowerSupply);
        Assert.Equal(0, w.Player(0).PowerDemand);
    }

    [Fact]
    public void CanPlace_RejectsOverlapWaterAndOffMap()
    {
        var w = new World(TestRules.Basic(), new Overmatch.Sim.Data.MapDef
        {
            Width = 32, Height = 32, Water = { new Overmatch.Sim.Data.RectDef { X = 10, Y = 10, W = 3, H = 3 } },
        });
        var def = w.Rules.Building("power");
        Assert.True(w.CanPlace(def, 2, 2, out _));
        Assert.False(w.CanPlace(def, 30, 30, out var r1)); Assert.Equal("off map", r1);
        Assert.False(w.CanPlace(def, 9, 9, out var r2)); Assert.Equal("terrain", r2);
        w.PlaceBuilding("power", 0, 2, 2);
        Assert.False(w.CanPlace(def, 3, 3, out var r3)); Assert.Equal("occupied", r3);
        Assert.True(w.CanPlace(def, 5, 2, out _));
    }

    [Fact]
    public void BuildCommand_ChargesCashAndDozerConstructs()
    {
        var (w, dozer) = Setup();
        var cash = w.Player(0).Cash;
        w.Submit(new BuildCommand(0, dozer.Id, "power", 16, 6));
        w.Step();
        Assert.Equal(cash - 800, w.Player(0).Cash);
        Assert.Contains(w.Events, e => e is ConstructionStartedEvent);
        var site = w.Entities.Single(e => e.Building?.Id == "power");
        Assert.True(site.UnderConstruction);
        Assert.Equal(0, w.Player(0).PowerSupply); // not until finished

        TestRules.RunUntil(w, () => !site.UnderConstruction, 20 * 20);
        Assert.False(site.UnderConstruction);
        Assert.Equal(site.MaxHp, site.Hp);
        Assert.Equal(10, w.Player(0).PowerSupply);
        Assert.Equal(0, dozer.BuildTargetId);
        Assert.True(World.DistanceToBounds(site, dozer.Pos) < 3f, "dozer should be next to the site");
    }

    [Fact]
    public void BuildCommand_RejectedWhenPrereqOrCashMissing()
    {
        var (w, dozer) = Setup();
        w.Submit(new BuildCommand(0, dozer.Id, "factory", 16, 6)); // needs power
        w.Step();
        Assert.Contains(w.Events, e => e is OrderRejectedEvent { Reason: "requires power" });
        w.Player(0).Cash = 100;
        w.Submit(new BuildCommand(0, dozer.Id, "power", 16, 6));
        w.Step();
        Assert.Contains(w.Events, e => e is OrderRejectedEvent { Reason: "insufficient funds" });
        Assert.DoesNotContain(w.Entities, e => e.Building?.Id == "power");
    }

    [Fact]
    public void TwoDozers_BuildFaster()
    {
        var (w, d1) = Setup();
        var d2 = w.Spawn("dozer", 0, new Vec2(11, 8));
        w.Submit(new BuildCommand(0, d1.Id, "power", 14, 6));
        w.Step();
        var site = w.Entities.Single(e => e.Building?.Id == "power");
        w.Submit(new AssistBuildCommand(0, new[] { d2.Id }, site.Id));
        var t0 = w.Tick;
        TestRules.RunUntil(w, () => !site.UnderConstruction, 20 * 20);
        var ticks = w.Tick - t0;
        Assert.True(ticks < 20 * 4, $"two dozers should beat the 4 s build time, took {ticks} ticks");
    }

    [Fact]
    public void Production_QueuesChargesAndSpawnsAtRally()
    {
        var (w, _) = Setup();
        w.PlaceBuilding("power", 0, 20, 4);
        var factory = w.PlaceBuilding("factory", 0, 12, 12);
        var cash = w.Player(0).Cash;
        w.Submit(new ProduceCommand(0, factory.Id, "tank"));
        w.Submit(new ProduceCommand(0, factory.Id, "tank"));
        w.Submit(new RallyCommand(0, factory.Id, new Vec2(14, 4)));
        w.Step();
        Assert.Equal(cash - 1000, w.Player(0).Cash);
        Assert.Equal(2, factory.Queue!.Items.Count);
        TestRules.RunUntil(w, () => w.Entities.Count(e => e.Def.Id == "tank") == 2, 20 * 10);
        var tanks = w.Entities.Where(e => e.Def.Id == "tank").ToList();
        Assert.Equal(2, tanks.Count);
        TestRules.Run(w, 20 * 6);
        foreach (var t in tanks) Assert.True(Vec2.Distance(t.Pos, new Vec2(14, 4)) < 3f, $"tank at {t.Pos}");
        Assert.Empty(factory.Queue.Items);
    }

    [Fact]
    public void Production_CancelRefunds()
    {
        var (w, _) = Setup();
        w.PlaceBuilding("power", 0, 20, 4);
        var factory = w.PlaceBuilding("factory", 0, 12, 12);
        var cash = w.Player(0).Cash;
        w.Submit(new ProduceCommand(0, factory.Id, "scout"));
        w.Step();
        w.Submit(new CancelProduceCommand(0, factory.Id, 0));
        w.Step();
        Assert.Equal(cash, w.Player(0).Cash);
        Assert.Empty(factory.Queue!.Items);
    }

    [Fact]
    public void LowPower_HalvesProduction()
    {
        var (w, _) = Setup();
        var factory = w.PlaceBuilding("factory", 0, 12, 12); // demand 5, no supply
        Assert.True(w.Player(0).LowPower);
        w.Submit(new ProduceCommand(0, factory.Id, "scout")); // 1 s build
        var t0 = w.Tick;
        TestRules.RunUntil(w, () => w.Entities.Any(e => e.Def.Id == "scout"), 20 * 10);
        var ticks = w.Tick - t0;
        Assert.InRange(ticks, 38, 44);
    }

    [Fact]
    public void Upgrade_ResearchAppliesDamageAndHp()
    {
        var (w, _) = Setup();
        w.PlaceBuilding("power", 0, 20, 4);
        var factory = w.PlaceBuilding("factory", 0, 12, 12);
        var tank = w.Spawn("tank", 0, new Vec2(30, 30));
        tank.Hp = 50f;
        w.Submit(new ProduceCommand(0, factory.Id, "big_guns"));
        TestRules.RunUntil(w, () => w.Player(0).Has("big_guns"), 20 * 10);
        Assert.True(w.Player(0).Has("big_guns"));
        Assert.Equal(200f, tank.MaxHp);
        Assert.Equal(100f, tank.Hp, 3); // kept its 50% fraction
        Assert.Equal(200f, w.Spawn("tank", 0, new Vec2(31, 31)).MaxHp);
        Assert.Equal(1.5f, w.Player(0).WeaponDamageMult("cannon"));
        // Second research attempt is rejected.
        w.Submit(new ProduceCommand(0, factory.Id, "big_guns"));
        w.Step();
        Assert.Contains(w.Events, e => e is OrderRejectedEvent { Reason: "already researched" });
    }

    [Fact]
    public void Harvester_LoopDeliversCash()
    {
        var w = new World(TestRules.Basic(), TestRules.SupplyMap());
        w.PlaceBuilding("hq", 0, 4, 4);
        var depot = w.PlaceBuilding("depot", 0, 10, 10);
        var truck = w.Spawn("truck", 0, new Vec2(16, 12));
        var cash = w.Player(0).Cash;
        w.Submit(new HarvestCommand(0, new[] { truck.Id }, 0));
        TestRules.RunUntil(w, () => w.Player(0).Cash > cash, 20 * 60);
        Assert.Equal(cash + 300, w.Player(0).Cash);
        Assert.Contains(w.Events, e => e is SupplyDeliveredEvent { Amount: 300 });
        Assert.Equal(700, w.Piles[0].Remaining);
        // Keeps going until the pile is empty, then idles.
        TestRules.RunUntil(w, () => w.Piles[0].Depleted && truck.HarvestState == HarvestState.Idle, 20 * 200);
        Assert.Equal(cash + 1000, w.Player(0).Cash);
        Assert.Equal(HarvestState.Idle, truck.HarvestState);
    }

    [Fact]
    public void ProducedHarvester_StartsAutomatically()
    {
        var w = new World(TestRules.Basic(), TestRules.SupplyMap());
        w.PlaceBuilding("hq", 0, 4, 4);
        var depot = w.PlaceBuilding("depot", 0, 10, 10);
        w.Submit(new ProduceCommand(0, depot.Id, "truck"));
        TestRules.RunUntil(w, () => w.Entities.Any(e => e.Def.Id == "truck"), 20 * 5);
        var truck = w.Entities.Single(e => e.Def.Id == "truck");
        Assert.Equal(HarvestState.ToPile, truck.HarvestState);
        var cash = w.Player(0).Cash;
        TestRules.RunUntil(w, () => w.Player(0).Cash > cash, 20 * 60);
        Assert.True(w.Player(0).Cash > cash);
    }

    [Fact]
    public void Trickle_PaysOnInterval()
    {
        var (w, _) = Setup();
        w.PlaceBuilding("bank", 0, 20, 20);
        var cash = w.Player(0).Cash;
        TestRules.Run(w, 20 * 2 + 1);
        Assert.Equal(cash + 100, w.Player(0).Cash);
        TestRules.Run(w, 20 * 2);
        Assert.Equal(cash + 200, w.Player(0).Cash);
    }

    [Fact]
    public void Sell_RefundsAndFreesCells()
    {
        var (w, _) = Setup();
        var power = w.PlaceBuilding("power", 0, 20, 20);
        var cash = w.Player(0).Cash;
        w.Submit(new SellCommand(0, power.Id));
        w.Step();
        Assert.Equal(cash + 400, w.Player(0).Cash);
        Assert.Null(w.Get(power.Id));
        Assert.Equal(0, w.BuildingAt(21, 21));
        Assert.True(w.Grid.IsPassable(21, 21, Locomotor.Tracked));
        Assert.Equal(0, w.Player(0).PowerSupply);
    }

    [Fact]
    public void Turret_ShootsEnemiesButNotWhenUnpowered()
    {
        var (w, _) = Setup();
        var turret = w.PlaceBuilding("turret", 0, 20, 20); // demand 2, no supply → offline
        var scout = w.Spawn("scout", 1, new Vec2(25, 21));
        TestRules.Run(w, 40);
        Assert.Equal(scout.Def.Hp, scout.Hp);
        w.PlaceBuilding("power", 0, 30, 30);
        Assert.False(w.Player(0).LowPower);
        TestRules.RunUntil(w, () => scout.Hp < scout.Def.Hp, 20 * 5);
        Assert.True(scout.Hp < scout.Def.Hp, "powered turret should fire");
    }

    [Fact]
    public void Tanks_AttackBuildingsAndDestroyThem()
    {
        var w = new World(TestRules.Basic());
        var power = w.PlaceBuilding("power", 1, 20, 20);
        for (var i = 0; i < 4; i++) w.Spawn("tank", 0, new Vec2(10 + i * 1.5f, 20));
        w.Submit(new AttackCommand(0, w.Entities.Where(e => e.Owner == 0).Select(e => e.Id).ToArray(), power.Id));
        TestRules.RunUntil(w, () => w.Get(power.Id) is null, 20 * 60);
        Assert.Null(w.Get(power.Id));
        Assert.True(w.Grid.IsPassable(21, 21, Locomotor.Tracked), "footprint should be freed");
        Assert.Contains(w.Events.Concat(Array.Empty<GameEvent>()), _ => true);
    }

    [Fact]
    public void UnitsInsideNewFootprint_GetPushedOut()
    {
        var (w, dozer) = Setup();
        var tank = w.Spawn("tank", 0, new Vec2(17.5f, 7.5f));
        w.Submit(new BuildCommand(0, dozer.Id, "power", 16, 6));
        TestRules.Run(w, 5);
        Assert.True(w.Grid.IsPassable(tank.Pos, Locomotor.Tracked), $"tank at {tank.Pos} is inside the site");
    }
}
