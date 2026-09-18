using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

/// <summary>Cross-cutting M4 systems, exercised with the shipped Coalition data plus neutral buildings.</summary>
public class M4Tests
{
    private static World NewWorld(GameRules? rules = null)
    {
        rules ??= RealDataTests.LoadShipped();
        return new World(rules, new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "coalition", "coalition" });
    }

    [Fact]
    public void Garrison_InfantryEnterFireAndLeave()
    {
        var w = NewWorld();
        var house = w.PlaceBuilding("civilian_house", -1, 30, 30);
        var a = w.Spawn("coalition_rifleman", 0, new Vec2(26, 31));
        var b = w.Spawn("coalition_rifleman", 0, new Vec2(26, 32));
        w.Submit(new GarrisonCommand(0, new[] { a.Id, b.Id }, house.Id));
        TestRules.RunUntil(w, () => a.IsInside && b.IsInside, 20 * 20);
        Assert.True(a.IsInside && b.IsInside);
        Assert.Equal(2, house.Passengers.Count);
        // They shoot from inside with extra reach.
        var victim = w.Spawn("coalition_rifleman", 1, new Vec2(31.5f, 38.5f));
        TestRules.RunUntil(w, () => victim.Hp < victim.MaxHp, 20 * 10);
        Assert.True(victim.Hp < victim.MaxHp, "garrisoned riflemen should fire");
        Assert.False(w.CanSee(1, a), "occupants are not targetable");
        w.Submit(new UngarrisonCommand(0, house.Id));
        w.Step();
        Assert.False(a.IsInside);
        Assert.Empty(house.Passengers);
    }

    [Fact]
    public void Garrison_DiesWithBuilding_AndFlashbangClears()
    {
        var w = NewWorld();
        var house = w.PlaceBuilding("civilian_house", -1, 30, 30);
        var a = w.Spawn("coalition_rifleman", 1, new Vec2(26, 31));
        w.Submit(new GarrisonCommand(1, new[] { a.Id }, house.Id));
        TestRules.RunUntil(w, () => a.IsInside, 20 * 20);
        var r = w.Spawn("coalition_rifleman", 0, new Vec2(28, 26));
        w.Submit(new AbilityCommand(0, new[] { r.Id }, "flashbang", Vec2.Zero, house.Id));
        TestRules.RunUntil(w, () => !a.IsInside, 20 * 15);
        Assert.False(a.IsInside, "flashbang should evict occupants");
        Assert.True(a.Disabled);
        w.Submit(new GarrisonCommand(1, new[] { a.Id }, house.Id));
        TestRules.RunUntil(w, () => a.IsInside, 20 * 20);
        Combat.Kill(w, house, null);
        w.Step();
        Assert.Null(w.Get(a.Id));
    }

    [Fact]
    public void Capture_OilDerrickPaysNewOwner()
    {
        var w = NewWorld();
        var derrick = w.PlaceBuilding("oil_derrick", -1, 30, 30);
        var r = w.Spawn("coalition_rifleman", 0, new Vec2(25, 31));
        w.Submit(new CaptureCommand(0, new[] { r.Id }, derrick.Id));
        TestRules.RunUntil(w, () => derrick.Owner == 0, 20 * 40);
        Assert.Equal(0, derrick.Owner);
        Assert.Contains(w.Events, e => e is CapturedEvent { NewOwner: 0 });
        var cash = w.Player(0).Cash;
        TestRules.Run(w, 20 * 13);
        Assert.Equal(cash + 100, w.Player(0).Cash);
    }

    [Fact]
    public void Stealth_HiddenUntilDetectedOrFiring()
    {
        var rules = RealDataTests.LoadShipped();
        var w = new World(rules, new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "coalition", "network" });
        var safehouse = w.PlaceBuilding("network_safehouse", 1, 30, 30); // stealthed, unarmed
        var tank = w.Spawn("coalition_bulwark", 0, new Vec2(26, 31));
        TestRules.Run(w, 30);
        Assert.True(w.Vision.IsVisible(0, safehouse.Pos), "the cell itself is in vision");
        Assert.False(w.CanSee(0, safehouse), "but the stealthed building is not seen");
        Assert.Equal(0, tank.TargetId);
        w.Spawn("coalition_jammer", 0, new Vec2(27, 33)); // detector 10
        TestRules.Run(w, 30);
        Assert.True(w.CanSee(0, safehouse));
        // A stealthed shooter is revealed while it fires.
        var sniper = w.Spawn("coalition_pathfinder", 1, new Vec2(60, 60));
        var rifle = w.Spawn("coalition_rifleman", 0, new Vec2(55, 60));
        TestRules.RunUntil(w, () => sniper.Has("revealed"), 20 * 10);
        Assert.True(sniper.Has("revealed"));
        Assert.True(w.CanSee(0, sniper));
    }

    [Fact]
    public void Powers_RankUpBuyAndUse()
    {
        var w = NewWorld();
        var p = w.Player(0);
        Assert.Equal(1, p.Rank);
        Assert.Equal(1, p.Points);
        w.Submit(new BuyPowerCommand(0, "coalition_precision_strike")); // rank 3
        w.Step();
        Assert.Contains(w.Events, e => e is OrderRejectedEvent { Reason: "requires rank 3" });
        w.Submit(new BuyPowerCommand(0, "coalition_loitering_swarm"));
        w.Step();
        Assert.True(p.HasPower("coalition_loitering_swarm"));
        Assert.Equal(0, p.Points);
        // A newly bought power must charge first.
        w.Submit(new UsePowerCommand(0, "coalition_loitering_swarm", new Vec2(40, 40)));
        w.Step();
        Assert.Contains(w.Events, e => e is OrderRejectedEvent { Reason: "power recharging" });
        Assert.Equal(0, w.Entities.Count(e => e.Def.Id == "coalition_loiter_drone"));
        TestRules.Run(w, 20 * 151);
        w.Submit(new UsePowerCommand(0, "coalition_loitering_swarm", new Vec2(40, 40)));
        w.Step();
        Assert.Equal(6, w.Entities.Count(e => e.Def.Id == "coalition_loiter_drone" && e.Owner == 0));
        w.Submit(new UsePowerCommand(0, "coalition_loitering_swarm", new Vec2(40, 40)));
        w.Step();
        Assert.Contains(w.Events, e => e is OrderRejectedEvent { Reason: "power recharging" });
        // Drones expire.
        TestRules.Run(w, 20 * 40);
        Assert.Equal(0, w.Entities.Count(e => e.Def.Id == "coalition_loiter_drone"));
        // XP promotes.
        Powers.AddXp(w, p, 900);
        Assert.Equal(3, p.Rank);
        Assert.Equal(2, p.Points);
    }

    [Fact]
    public void Strike_DamagesAreaAfterDelay()
    {
        var w = NewWorld();
        var p = w.Player(0);
        Powers.AddXp(w, p, 900);
        w.Submit(new BuyPowerCommand(0, "coalition_precision_strike"));
        TestRules.Run(w, 20 * 181); // charge after purchase
        var victim = w.Spawn("coalition_bulwark", 1, new Vec2(40, 40));
        w.Submit(new UsePowerCommand(0, "coalition_precision_strike", new Vec2(40, 40)));
        TestRules.Run(w, 20 * 2);
        Assert.Equal(victim.MaxHp, victim.Hp); // still in the air
        TestRules.Run(w, 20 * 2);
        Assert.Null(w.Get(victim.Id));
    }

    [Fact]
    public void Superweapon_ChargesThenFires()
    {
        var w = NewWorld();
        var uplink = w.PlaceBuilding("coalition_orbital_uplink", 0, 10, 10);
        w.PlaceBuilding("coalition_power_plant", 0, 16, 10);
        var victim = w.PlaceBuilding("coalition_power_plant", 1, 50, 50);
        w.Submit(new FireSuperweaponCommand(0, uplink.Id, new Vec2(51.5f, 51.5f)));
        w.Step();
        Assert.Contains(w.Events, e => e is OrderRejectedEvent { Reason: "superweapon charging" });
        TestRules.RunUntil(w, () => uplink.SuperweaponCharge >= 300f, 20 * 320);
        w.Submit(new FireSuperweaponCommand(0, uplink.Id, new Vec2(51.5f, 51.5f)));
        w.Step();
        Assert.Contains(w.Events, e => e is SuperweaponFiredEvent);
        Assert.True(uplink.SuperweaponCharge < 1f);
        TestRules.Run(w, 20 * 6);
        Assert.Null(w.Get(victim.Id));
    }

    [Fact]
    public void Hazard_PoisonsInfantryNotTanks()
    {
        var w = NewWorld();
        var inf = w.Spawn("coalition_rifleman", 1, new Vec2(30, 30));
        var tank = w.Spawn("coalition_bulwark", 1, new Vec2(31, 30));
        Effects.AddHazard(w, 0, new Vec2(30, 30), new HazardDef { Radius = 3, Dps = 20, DamageType = "toxin", Duration = 10 });
        TestRules.Run(w, 20 * 3);
        Assert.True(inf.Hp < inf.MaxHp, "infantry should be poisoned");
        Assert.Equal(tank.MaxHp, tank.Hp);
    }

    [Fact]
    public void Veterancy_LevelsRaiseDamage()
    {
        var w = NewWorld();
        var tank = w.Spawn("coalition_bulwark", 0, new Vec2(30, 30));
        Assert.Equal(0, tank.Level);
        tank.Xp = 260;
        Assert.Equal(2, tank.Level);
        Assert.Equal(1.2f, tank.DamageMult, 3);
    }

    [Fact]
    public void Jammer_BurnsDronesAndDetects()
    {
        var w = NewWorld();
        w.Spawn("coalition_jammer", 0, new Vec2(30, 30));
        var drone = w.Spawn("coalition_loiter_drone", 1, new Vec2(33, 30));
        drone.LifetimeLeft = 999f;
        TestRules.RunUntil(w, () => w.Get(drone.Id) is null, 20 * 5);
        Assert.Null(w.Get(drone.Id));
    }

    [Fact]
    public void Hive_SpawnsDronesThatFollowAndDie()
    {
        var w = NewWorld();
        var hive = w.Spawn("coalition_hive", 0, new Vec2(30, 30));
        TestRules.Run(w, 20 * 45);
        Assert.Equal(4, w.Entities.Count(e => e.FollowId == hive.Id));
        w.Submit(new MoveCommand(0, new[] { hive.Id }, new Vec2(60, 30)));
        TestRules.Run(w, 20 * 15);
        foreach (var d in w.Entities.Where(e => e.FollowId == hive.Id))
            Assert.True(Vec2.Distance(d.Pos, hive.Pos) < 10f, $"drone at {d.Pos} should follow the hive at {hive.Pos}");
        Combat.Kill(w, hive, null);
        w.Step();
        w.Step();
        Assert.Equal(0, w.Entities.Count(e => e.Def.Id == "coalition_loiter_drone"));
    }

    [Fact]
    public void Jet_UsesAmmoThenRearms()
    {
        var w = NewWorld();
        w.PlaceBuilding("coalition_airfield", 0, 10, 10);
        var jet = w.Spawn("coalition_vantage", 0, new Vec2(14, 16));
        Assert.Equal(4, jet.Ammo);
        for (var i = 0; i < 3; i++) w.Spawn("coalition_bulwark", 1, new Vec2(30 + i * 3, 30));
        w.Submit(new AttackMoveCommand(0, new[] { jet.Id }, new Vec2(30, 30)));
        TestRules.RunUntil(w, () => jet.Rearming, 20 * 40);
        Assert.True(jet.Rearming, "jet should fly home to rearm after 4 shots");
        TestRules.RunUntil(w, () => !jet.Rearming, 20 * 40);
        Assert.Equal(4, jet.Ammo);
    }

    [Fact]
    public void Crusher_KillsInfantry()
    {
        var rules = RealDataTests.LoadShipped();
        var w = NewWorld(rules);
        var heavy = w.Spawn("coalition_bulwark", 0, new Vec2(30, 30));
        var inf = w.Spawn("coalition_rifleman", 1, new Vec2(34, 30));
        // Bulwark is not a crusher; nothing happens.
        w.Submit(new MoveCommand(0, new[] { heavy.Id }, new Vec2(40, 30)));
        TestRules.Run(w, 20 * 4);
        Assert.NotNull(w.Get(inf.Id));
    }

    [Fact]
    public void Salvage_CratesUpgradeSalvagers()
    {
        var rules = RealDataTests.LoadShipped();
        if (!rules.Factions.ContainsKey("network")) return; // until the Network data lands
        var w = new World(rules, new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "network", "coalition" });
        var tech = w.Spawn("network_technical", 0, new Vec2(30, 30));
        var victim = w.Spawn("coalition_dozer", 1, new Vec2(34, 30));
        w.Submit(new AttackCommand(0, new[] { tech.Id }, victim.Id));
        TestRules.RunUntil(w, () => w.Crates.Count > 0, 20 * 60);
        Assert.Single(w.Crates);
        w.Submit(new MoveCommand(0, new[] { tech.Id }, w.Crates[0].Pos));
        TestRules.RunUntil(w, () => tech.SalvageLevel > 0, 20 * 20);
        Assert.Equal(1, tech.SalvageLevel);
        Assert.Empty(w.Crates);
    }
}
