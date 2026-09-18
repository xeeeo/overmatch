using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

/// <summary>Loads the shipped game data and walks the whole Coalition tech tree in the sim.</summary>
public class RealDataTests
{
    private static string DataDir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "game", "data");
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("game/data not found above " + AppContext.BaseDirectory);
    }

    public static GameRules LoadShipped()
    {
        var root = DataDir();
        var files = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .Select(f => new DataFile(Path.GetRelativePath(root, f).Replace('\\', '/'), File.ReadAllText(f)));
        return GameRules.Load(files);
    }

    [Fact]
    public void ShippedData_Loads()
    {
        var rules = LoadShipped();
        Assert.True(rules.Units.Count >= 9);
        Assert.True(rules.Buildings.Count >= 10);
        Assert.Contains("plain", rules.Maps.Keys);
        Assert.Equal("coalition_command_post", rules.Factions["coalition"].Hq);
    }

    [Fact]
    public void Coalition_FullTree_BuildsEverythingWithoutRejection()
    {
        var rules = LoadShipped();
        var w = new World(rules, rules.Map("plain"), new[] { "coalition", "coalition" });
        var p = w.Player(0);
        p.Cash = 1_000_000;
        w.PlaceBuilding("coalition_command_post", 0, 14, 14);
        var dozer = w.Spawn("coalition_dozer", 0, new Vec2(20, 12));

        // Build every Coalition building, always picking one whose prereqs are already met.
        var remaining = rules.Buildings.Values.Where(b => b.Faction == "coalition" && !b.Hq).OrderBy(b => b.Cost).ToList();
        var cx = 30; var cy = 4;
        while (remaining.Count > 0)
        {
            var b = remaining.FirstOrDefault(x => w.HasPrereqs(0, x.Prereqs, out _));
            Assert.True(b is not null, "no buildable building left; prereq cycle? remaining: " + string.Join(",", remaining.Select(r => r.Id)));
            remaining.Remove(b!);
            w.Submit(new BuildCommand(0, dozer.Id, b.Id, cx, cy));
            w.Step();
            var rejected = w.Events.OfType<OrderRejectedEvent>().FirstOrDefault();
            Assert.True(rejected is null, $"{b.Id} rejected: {rejected?.Reason}");
            var site = w.Entities.Single(e => e.Building?.Id == b.Id && e.Owner == 0);
            TestRules.RunUntil(w, () => !site.UnderConstruction, 20 * 200);
            Assert.False(site.UnderConstruction, $"{b.Id} never finished");
            cx += b.Width + 2;
            if (cx > 58) { cx = 30; cy += 8; }
        }
        // One plant is not enough for the whole tree; add plants until the base is powered, as a player would.
        for (var guard = 0; p.LowPower && guard < 5; guard++)
        {
            w.Submit(new BuildCommand(0, dozer.Id, "coalition_power_plant", 30 + guard * 5, 56));
            w.Step();
            var site = w.Entities.Last(e => e.Building?.Id == "coalition_power_plant" && e.Owner == 0);
            TestRules.RunUntil(w, () => !site.UnderConstruction, 20 * 100);
        }
        Assert.False(p.LowPower, $"base should be powered: {p.PowerSupply}/{p.PowerDemand}");

        // Produce every unit at its producer and research every upgrade.
        foreach (var u in rules.Units.Values.Where(u => u.Faction == "coalition"))
        {
            var producer = w.Entities.First(e => e.Owner == 0 && e.Building?.Produces.Contains(u.Id) == true);
            w.Submit(new ProduceCommand(0, producer.Id, u.Id));
            w.Step();
            var rejected = w.Events.OfType<OrderRejectedEvent>().FirstOrDefault();
            Assert.True(rejected is null, $"{u.Id} rejected: {rejected?.Reason}");
        }
        foreach (var up in rules.Upgrades.Values.Where(u => u.Faction == "coalition"))
        {
            var lab = w.Entities.First(e => e.Owner == 0 && e.Building?.Upgrades.Contains(up.Id) == true);
            w.Submit(new ProduceCommand(0, lab.Id, up.Id));
            w.Step();
            var rejected = w.Events.OfType<OrderRejectedEvent>().FirstOrDefault();
            Assert.True(rejected is null, $"{up.Id} rejected: {rejected?.Reason}");
        }
        TestRules.RunUntil(w, () => w.Entities.All(e => e.Queue is null || e.Queue.Items.Count == 0), 20 * 300);
        foreach (var u in rules.Units.Values.Where(u => u.Faction == "coalition"))
            Assert.Contains(w.Entities, e => e.Def.Id == u.Id && e.Owner == 0);
        foreach (var up in rules.Upgrades.Values.Where(u => u.Faction == "coalition"))
            Assert.True(p.Has(up.Id), $"{up.Id} not researched");

        // The tiltrotor harvests on its own from the supply centre.
        var tilt = w.Entities.First(e => e.Def.Id == "coalition_tiltrotor");
        Assert.NotEqual(HarvestState.Idle, tilt.HarvestState);
    }

    [Fact]
    public void Coalition_MixedArmy_BeatsSentryAndBase()
    {
        var rules = LoadShipped();
        var w = new World(rules, rules.Map("plain"), new[] { "coalition", "coalition" });
        w.PlaceBuilding("coalition_command_post", 1, 60, 60);
        w.PlaceBuilding("coalition_power_plant", 1, 66, 60);
        w.PlaceBuilding("coalition_sentry_battery", 1, 58, 56);
        var ids = new List<int>();
        for (var i = 0; i < 6; i++) ids.Add(w.Spawn("coalition_bulwark", 0, new Vec2(40 + i * 2.5f, 40)).Id);
        for (var i = 0; i < 3; i++) ids.Add(w.Spawn("coalition_lancer", 0, new Vec2(40 + i * 2.5f, 36)).Id);
        for (var i = 0; i < 2; i++) ids.Add(w.Spawn("coalition_kestrel", 0, new Vec2(44 + i * 2.5f, 34)).Id);
        for (var i = 0; i < 4; i++) ids.Add(w.Spawn("coalition_rocket_trooper", 0, new Vec2(44 + i * 1.2f, 38)).Id);
        w.Submit(new AttackMoveCommand(0, ids.ToArray(), new Vec2(62, 62)));
        TestRules.RunUntil(w, () => !w.Entities.Any(e => e.Owner == 1), 20 * 240);
        Assert.DoesNotContain(w.Entities, e => e.Owner == 1);
        Assert.Contains(w.Entities, e => e.Owner == 0 && e.Def.Id == "coalition_lancer");
    }
}
