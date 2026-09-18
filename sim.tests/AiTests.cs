using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public class AiTests
{
    private static World NewMatch(GameRules rules, params (int player, string difficulty)[] ais)
    {
        var map = rules.Map("plain");
        var w = new World(rules, map, new[] { "coalition", "coalition" });
        for (var p = 0; p < 2; p++)
        {
            var s = map.Spawns[p];
            var hq = w.PlaceBuilding("coalition_command_post", p, (int)s.X - 2, (int)s.Y - 2);
            w.Spawn("coalition_dozer", p, new Vec2(s.X, s.Y - 5));
        }
        foreach (var (player, difficulty) in ais) w.AddAi(player, rules.Ai("coalition", difficulty));
        return w;
    }

    [Fact]
    public void Profiles_LoadForEveryDifficulty()
    {
        var rules = RealDataTests.LoadShipped();
        foreach (var d in new[] { "easy", "medium", "hard", "brutal" })
        {
            var p = rules.Ai("coalition", d);
            Assert.NotEmpty(p.BuildOrder);
            Assert.True(p.Composition.Values.Sum() > 0);
            foreach (var b in p.BuildOrder) Assert.Contains(b, rules.Buildings.Keys);
            foreach (var u in p.Composition.Keys) Assert.Contains(u, rules.Units.Keys);
            foreach (var u in p.UpgradeOrder) Assert.Contains(u, rules.Upgrades.Keys);
        }
    }

    [Fact]
    public void MediumAi_BuildsEconomyAndArmy()
    {
        var rules = RealDataTests.LoadShipped();
        var w = NewMatch(rules, (1, "medium"));
        TestRules.Run(w, 20 * 240); // 4 minutes
        var mine = w.Entities.Where(e => e.Owner == 1).ToList();
        Assert.Contains(mine, e => e.Building?.Id == "coalition_power_plant" && e.Operational);
        Assert.Contains(mine, e => e.Building?.Id == "coalition_supply_center" && e.Operational);
        Assert.Contains(mine, e => e.Building?.Id == "coalition_motor_pool");
        Assert.True(mine.Count(e => e.IsHarvester) >= 2, "should have harvesters");
        Assert.True(mine.Count(e => !e.IsBuilding && e.HasWeapons && !e.IsHarvester) >= 4, $"should have an army, has {mine.Count(e => !e.IsBuilding && e.HasWeapons)}");
        Assert.False(w.Player(1).LowPower, $"AI base under-powered {w.Player(1).PowerSupply}/{w.Player(1).PowerDemand}");
    }

    [Fact]
    public void MediumAi_DefeatsPassivePlayer()
    {
        var rules = RealDataTests.LoadShipped();
        var w = NewMatch(rules, (1, "medium"));
        TestRules.RunUntil(w, () => w.Finished, 20 * 60 * 14);
        Assert.True(w.Finished, "AI should win within 14 minutes against a player who does nothing");
        Assert.Equal(1, w.Winner);
        Assert.True(w.Player(0).Eliminated);
    }

    [Fact]
    public void AiVsAi_IsDeterministicOnThisMachine()
    {
        // Same inputs, same machine → identical outcome. (Across CPU architectures float results can differ,
        // so a hard-vs-medium match may end differently on x64 and arm64; that is expected until the sim moves to fixed-point.)
        var rules = RealDataTests.LoadShipped();
        static (int winner, int tick, int entities, float cash) Run(GameRules rules)
        {
            var w = NewMatch(rules, (0, "hard"), (1, "medium"));
            TestRules.RunUntil(w, () => w.Finished, 20 * 60 * 12);
            return (w.Winner, w.Tick, w.Entities.Count, w.Player(0).Cash + w.Player(1).Cash);
        }
        var a = Run(rules);
        var b = Run(rules);
        Assert.Equal(a, b);
    }

    [Fact]
    public void BrutalAi_BeatsEasyAi()
    {
        var rules = RealDataTests.LoadShipped();
        var w = NewMatch(rules, (0, "brutal"), (1, "easy"));
        TestRules.RunUntil(w, () => w.Finished, 20 * 60 * 20);
        Assert.True(w.Finished, "brutal vs easy should be over within 20 minutes");
        Assert.Equal(0, w.Winner);
    }

    [Fact]
    public void Elimination_NoBuildingsNoBuilders()
    {
        var rules = RealDataTests.LoadShipped();
        var w = new World(rules, rules.Map("plain"), new[] { "coalition", "coalition" });
        w.PlaceBuilding("coalition_command_post", 0, 10, 10);
        w.PlaceBuilding("coalition_command_post", 1, 60, 60);
        var lone = w.Spawn("coalition_bulwark", 1, new Vec2(50, 50));
        TestRules.Run(w, 25);
        Assert.False(w.Finished);
        var hq1 = w.Entities.First(e => e.Owner == 1 && e.IsBuilding);
        hq1.Alive = false; // simulate destruction
        TestRules.Run(w, 25);
        Assert.True(w.Player(1).Eliminated);
        Assert.Equal(0, w.Winner);
        Assert.Contains(w.Entities, e => e.Id == lone.Id); // units linger but the player is out
    }
}

public class FactionAiTests
{
    private static World Match(GameRules rules, string f0, string d0, string f1, string d1)
    {
        var map = rules.Map("plain");
        var w = new World(rules, map, new[] { f0, f1 });
        for (var p = 0; p < 2; p++)
        {
            var s = map.Spawns[p];
            var fd = w.Player(p).Faction;
            var hqDef = rules.Building(fd.Hq);
            w.PlaceBuilding(fd.Hq, p, (int)s.X - hqDef.Width / 2, (int)s.Y - hqDef.Height / 2);
            w.Spawn(fd.Builder, p, new Vec2(s.X, s.Y - 5));
        }
        w.AddAi(0, rules.Ai(f0, d0));
        w.AddAi(1, rules.Ai(f1, d1));
        return w;
    }

    [Theory]
    [InlineData("directorate")]
    [InlineData("network")]
    public void Ai_BuildsEconomyAndArmy(string faction)
    {
        var rules = RealDataTests.LoadShipped();
        var w = Match(rules, faction, "medium", "coalition", "easy");
        TestRules.Run(w, 20 * 300);
        var mine = w.Entities.Where(e => e.Owner == 0).ToList();
        var prof = rules.Ai(faction, "medium");
        Assert.Contains(mine, e => e.Building?.Id == prof.SupplyBuilding && e.Operational);
        Assert.True(mine.Count(e => e.IsHarvester) >= 2, $"{faction}: should have harvesters");
        Assert.True(mine.Count(e => !e.IsBuilding && e.HasWeapons && !e.IsHarvester) >= 4, $"{faction}: should have an army");
        Assert.True(mine.Count(e => e.IsBuilding) >= 5, $"{faction}: should have a base, has {mine.Count(e => e.IsBuilding)}");
        Assert.False(w.Player(0).LowPower, $"{faction}: under-powered {w.Player(0).PowerSupply}/{w.Player(0).PowerDemand}");
    }

    [Theory]
    [InlineData("directorate", "network")]
    [InlineData("network", "coalition")]
    [InlineData("coalition", "directorate")]
    public void BrutalBeatsEasy_AcrossFactions(string strong, string weak)
    {
        var rules = RealDataTests.LoadShipped();
        var w = Match(rules, strong, "brutal", weak, "easy");
        TestRules.RunUntil(w, () => w.Finished, 20 * 60 * 22);
        string Summary(int pl) => $"{w.Player(pl).Faction.Id}: {w.Entities.Count(e => e.Owner == pl && e.IsBuilding)} buildings [{string.Join(",", w.Entities.Where(e => e.Owner == pl && e.IsBuilding).Select(e => e.Def.Id + (e.Def.Stealth ? "(s)" : "")))}], {w.Entities.Count(e => e.Owner == pl && !e.IsBuilding)} units, cash {w.Player(pl).Cash}, ai '{w.Ais[pl].Status}'";
        // Float results differ between CPU architectures, so a match can play out differently on CI. Accept either a win
        // or a clear lead for the brutal side; a loss or a level game is a real balance or AI problem.
        if (w.Finished)
        {
            Assert.True(w.Winner == 0, $"{strong} brutal should beat {weak} easy.\n  {Summary(0)}\n  {Summary(1)}");
            return;
        }
        int Value(int pl) => w.Entities.Where(e => e.Owner == pl && e.Alive).Sum(e => e.Def.Cost);
        Assert.True(Value(0) > Value(1) * 1.5f, $"{strong} brutal should be clearly ahead of {weak} easy after 22 minutes.\n  {Summary(0)}\n  {Summary(1)}");
    }

    [Theory]
    [InlineData("coalition")]
    [InlineData("directorate")]
    [InlineData("network")]
    public void Ai_DoesNotStrikeTheEnemyBaseInTheOpeningMinutes(string aiFaction)
    {
        var rules = RealDataTests.LoadShipped();
        var map = rules.Map("plain");
        var w = new World(rules, map, new[] { "coalition", aiFaction });
        for (var p = 0; p < 2; p++)
        {
            var s = map.Spawns[p];
            var fd = w.Player(p).Faction;
            var hqDef = rules.Building(fd.Hq);
            w.PlaceBuilding(fd.Hq, p, (int)s.X - hqDef.Width / 2, (int)s.Y - hqDef.Height / 2);
            w.Spawn(fd.Builder, p, new Vec2(s.X, s.Y - 5));
        }
        w.AddAi(1, rules.Ai(aiFaction, "brutal"));
        var hq = w.Entities.First(e => e.Owner == 0 && e.IsBuilding);
        var events = new List<GameEvent>();
        for (var t = 0; t < 20 * 120; t++)
        {
            w.Step();
            events.AddRange(w.Events.Where(e => e is PowerUsedEvent { Player: 1 } or DamagedEvent));
        }
        Assert.Equal(hq.MaxHp, hq.Hp);
        Assert.DoesNotContain(events, e => e is DamagedEvent d && w.Get(d.EntityId)?.Owner == 0);
        Assert.DoesNotContain(events, e => e is PowerUsedEvent);
    }

    [Fact]
    public void CashBounty_IsPassiveAndImmediate()
    {
        var rules = RealDataTests.LoadShipped();
        var w = new World(rules, new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "network", "coalition" });
        w.Submit(new BuyPowerCommand(0, "network_cash_bounty"));
        w.Step();
        Assert.True(w.Player(0).BountyPerKill > 0f);
        var cash = w.Player(0).Cash;
        var tech = w.Spawn("network_marauder", 0, new Vec2(30, 30));
        var victim = w.Spawn("coalition_rifleman", 1, new Vec2(33, 30));
        TestRules.RunUntil(w, () => w.Get(victim.Id) is null, 20 * 30);
        Assert.True(w.Player(0).Cash > cash, "a kill should pay a bounty");
    }

    [Fact]
    public void Network_HolesRegrowBuildings()
    {
        var rules = RealDataTests.LoadShipped();
        var w = new World(rules, new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "network", "coalition" });
        var stash = w.PlaceBuilding("network_supply_stash", 0, 30, 30);
        Combat.Kill(w, stash, null);
        w.Step();
        var hole = w.Entities.Single(e => e.Building is { IsHole: true });
        Assert.Equal(0, hole.Owner);
        TestRules.RunUntil(w, () => w.Entities.Any(e => e.Building?.Id == "network_supply_stash" && e.Owner == 0), 20 * 70);
        Assert.Contains(w.Entities, e => e.Building?.Id == "network_supply_stash" && e.Owner == 0 && e.Operational);
        Assert.DoesNotContain(w.Entities, e => e.Building is { IsHole: true });
    }

    [Fact]
    public void Network_TunnelsMoveUnitsAcrossTheMap()
    {
        var rules = RealDataTests.LoadShipped();
        var w = new World(rules, new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "network", "coalition" });
        var t1 = w.PlaceBuilding("network_tunnel_network", 0, 10, 10);
        var t2 = w.PlaceBuilding("network_tunnel_network", 0, 60, 60);
        var r = w.Spawn("network_rebel", 0, new Vec2(8, 12));
        w.Submit(new GarrisonCommand(0, new[] { r.Id }, t1.Id));
        TestRules.RunUntil(w, () => r.IsInside, 20 * 15);
        Assert.Contains(r.Id, w.Player(0).TunnelPool);
        w.Submit(new UngarrisonCommand(0, t2.Id));
        w.Step();
        Assert.False(r.IsInside);
        Assert.True(Vec2.Distance(r.Pos, t2.Pos) < 5f, $"rebel should pop out at the far tunnel, is at {r.Pos}");
    }

    [Fact]
    public void Directorate_ReactorExplodesAndHordeBonusApplies()
    {
        var rules = RealDataTests.LoadShipped();
        var w = new World(rules, new MapDef { Id = "t", Width = 80, Height = 80 }, new[] { "directorate", "coalition" });
        var reactor = w.PlaceBuilding("directorate_reactor", 0, 30, 30);
        var bystander = w.Spawn("coalition_rifleman", 1, new Vec2(34.5f, 31.5f));
        w.Step(); // populate the spatial index
        Combat.Kill(w, reactor, null);
        w.Step();
        Assert.Null(w.Get(bystander.Id));
        Assert.NotEmpty(w.Hazards);
        var tanks = Enumerable.Range(0, 6).Select(i => w.Spawn("directorate_vanguard", 0, new Vec2(50 + i % 3 * 2.4f, 50 + i / 3 * 2.4f))).ToList();
        TestRules.Run(w, Effects.AuraInterval + 1);
        Assert.Equal(1.25f, tanks[0].HordeMult, 3);
        var lone = w.Spawn("directorate_vanguard", 0, new Vec2(70, 70));
        TestRules.Run(w, Effects.AuraInterval + 1);
        Assert.Equal(1f, lone.HordeMult, 3);
    }
}
