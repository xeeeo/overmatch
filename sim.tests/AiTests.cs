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
