using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public class GridTests
{
    [Fact]
    public void CellOf_And_Centre_RoundTrip()
    {
        Assert.Equal((3, 7), MapGrid.CellOf(new Vec2(3.9f, 7.1f)));
        Assert.Equal(new Vec2(3.5f, 7.5f), MapGrid.Centre(3, 7));
        Assert.Equal((-1, 0), MapGrid.CellOf(new Vec2(-0.1f, 0.2f)));
    }

    [Fact]
    public void Passability_ByLocomotor()
    {
        var g = MapGrid.FromDef(new MapDef
        {
            Width = 10, Height = 10,
            Water = { new RectDef { X = 2, Y = 2 } },
            Blocked = { new RectDef { X = 5, Y = 5 } },
        });
        Assert.True(g.IsPassable(0, 0, Locomotor.Tracked));
        Assert.False(g.IsPassable(2, 2, Locomotor.Tracked));
        Assert.True(g.IsPassable(2, 2, Locomotor.Hover));
        Assert.True(g.IsPassable(2, 2, Locomotor.Air));
        Assert.False(g.IsPassable(5, 5, Locomotor.Air) == false && g.IsPassable(5, 5, Locomotor.Tracked));
        Assert.False(g.IsPassable(-1, 0, Locomotor.Tracked));
        Assert.False(g.IsPassable(10, 0, Locomotor.Tracked));
    }

    [Fact]
    public void NearestPassable_FindsRing()
    {
        var g = MapGrid.FromDef(new MapDef { Width = 10, Height = 10, Blocked = { new RectDef { X = 4, Y = 4, W = 3, H = 3 } } });
        Assert.Equal((5, 5), g.NearestPassable(5, 5, Locomotor.Air));
        var n = g.NearestPassable(5, 5, Locomotor.Tracked);
        Assert.NotNull(n);
        Assert.True(g.IsPassable(n!.Value.x, n.Value.y, Locomotor.Tracked));
        Assert.Equal(2, Math.Max(Math.Abs(n.Value.x - 5), Math.Abs(n.Value.y - 5)));
    }

    [Fact]
    public void LineClear_DetectsWall()
    {
        var g = MapGrid.FromDef(TestRules.WalledMap());
        Assert.True(g.LineClear(new Vec2(2, 2), new Vec2(30, 2), Locomotor.Tracked));   // above the wall (wall starts at y=5)
        Assert.False(g.LineClear(new Vec2(2, 20), new Vec2(30, 20), Locomotor.Tracked)); // through it
        Assert.True(g.LineClear(new Vec2(2, 20), new Vec2(30, 20), Locomotor.Air));
    }

    [Fact]
    public void FlowField_RoutesAroundWall()
    {
        var g = MapGrid.FromDef(TestRules.WalledMap());
        var f = FlowField.Build(g, 30, 20, Locomotor.Tracked);
        Assert.True(f.Reachable(5, 20));
        Assert.False(f.Reachable(20, 20));
        // Walk the field from (5,20) and make sure it arrives.
        var p = new Vec2(5.5f, 20.5f);
        for (var i = 0; i < 400; i++)
        {
            var d = f.Sample(p);
            if (d == Vec2.Zero) break;
            var next = p + d * 0.25f;
            Assert.True(g.IsPassable(next, Locomotor.Tracked), $"stepped into wall at {next}");
            p = next;
        }
        Assert.True(Vec2.Distance(p, new Vec2(30.5f, 20.5f)) < 0.6f, $"ended at {p}");
        // The detour is much longer than the straight line.
        Assert.True(f.Dist[20 * 40 + 5] > 35f);
    }

    [Fact]
    public void FlowFieldCache_ReusesAndInvalidates()
    {
        var g = MapGrid.FromDef(TestRules.WalledMap());
        var cache = new FlowFieldCache(g);
        var a = cache.Get(30, 20, Locomotor.Tracked);
        var b = cache.Get(30, 20, Locomotor.Tracked);
        Assert.Same(a, b);
        Assert.Equal(1, cache.Builds);
        g.Set(20, 20, CellType.Ground); // open a gap
        var c = cache.Get(30, 20, Locomotor.Tracked);
        Assert.NotSame(a, c);
        Assert.True(c.Dist[20 * 40 + 5] < 30f);
    }
}
