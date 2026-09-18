using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public class MapTests
{
    public static IEnumerable<object[]> MapIds() => RealDataTests.LoadShipped().Maps.Keys.OrderBy(k => k).Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(MapIds))]
    public void Map_IsPlayable(string mapId)
    {
        var rules = RealDataTests.LoadShipped();
        var map = rules.Map(mapId);
        Assert.True(map.Spawns.Count >= 2, "needs at least two spawns");
        Assert.True(map.Spawns.Count <= 8, "at most eight players are supported");

        // Neutral buildings must sit on buildable ground and not overlap each other.
        var probe = new World(rules, new MapDef { Id = "probe", Width = map.Width, Height = map.Height, Blocked = map.Blocked, Water = map.Water, Rough = map.Rough, Road = map.Road, Supplies = map.Supplies },
            Enumerable.Repeat("coalition", map.Spawns.Count));
        foreach (var n in map.Neutrals)
        {
            var def = rules.Building(n.Id);
            Assert.True(probe.CanPlace(def, n.X, n.Y, out var why), $"{mapId}: neutral {n.Id} at {n.X},{n.Y}: {why}");
            probe.PlaceBuilding(n.Id, -1, n.X, n.Y);
        }

        var w = new World(rules, map, Enumerable.Repeat("coalition", map.Spawns.Count));
        // Every faction's HQ fits at every spawn, with room for a builder next to it.
        foreach (var f in rules.Factions.Values)
        {
            var hq = rules.Building(f.Hq);
            foreach (var s in map.Spawns)
                Assert.True(w.CanPlace(hq, (int)s.X - hq.Width / 2, (int)s.Y - hq.Height / 2, out var why), $"{mapId}: {f.Id} HQ does not fit at spawn {s.X},{s.Y}: {why}");
        }

        // All spawns are connected by land, and every pile can be reached from some spawn.
        var first = map.Spawns[0];
        var field = FlowField.Build(w.Grid, (int)first.X, (int)first.Y - 4, Locomotor.Tracked);
        foreach (var s in map.Spawns)
            Assert.True(field.Reachable((int)s.X, (int)s.Y - 4), $"{mapId}: spawn {s.X},{s.Y} is cut off from spawn 0");
        foreach (var pile in map.Supplies)
        {
            var (cx, cy) = ((int)pile.X, (int)pile.Y);
            var near = w.Grid.NearestPassable(cx, cy, Locomotor.Tracked, 3);
            Assert.True(near is not null && field.Reachable(near.Value.x, near.Value.y), $"{mapId}: pile at {pile.X},{pile.Y} is unreachable");
        }

        // Fair start: every spawn has the same supplies within 20 cells.
        var near0 = map.Supplies.Where(p => Vec2.Distance(new Vec2(p.X, p.Y), new Vec2(first.X, first.Y)) < 20).Sum(p => p.Amount);
        foreach (var s in map.Spawns)
            Assert.Equal(near0, map.Supplies.Where(p => Vec2.Distance(new Vec2(p.X, p.Y), new Vec2(s.X, s.Y)) < 20).Sum(p => p.Amount));
    }

    [Theory]
    [MemberData(nameof(MapIds))]
    public void Map_AiCanPlayIt(string mapId)
    {
        var rules = RealDataTests.LoadShipped();
        var map = rules.Map(mapId);
        var w = new World(rules, map, new[] { "coalition", "directorate" });
        for (var p = 0; p < 2; p++)
        {
            var s = map.Spawns[p];
            var fd = w.Player(p).Faction;
            var hq = rules.Building(fd.Hq);
            w.PlaceBuilding(fd.Hq, p, (int)s.X - hq.Width / 2, (int)s.Y - hq.Height / 2);
            w.Spawn(fd.Builder, p, new Vec2(s.X, s.Y - 5));
            w.AddAi(p, rules.Ai(fd.Id, "medium"));
        }
        TestRules.Run(w, 20 * 240);
        for (var p = 0; p < 2; p++)
        {
            var mine = w.Entities.Where(e => e.Owner == p).ToList();
            Assert.True(mine.Count(e => e.IsBuilding) >= 5, $"{mapId}: player {p} built only {mine.Count(e => e.IsBuilding)} buildings");
            Assert.True(mine.Count(e => e.IsHarvester) >= 2, $"{mapId}: player {p} has no economy");
        }
    }
}
