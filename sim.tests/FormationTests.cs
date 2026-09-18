using Overmatch.Sim;

namespace Overmatch.Sim.Tests;

public class FormationTests
{
    [Fact]
    public void Spread_SingleUnitGetsExactTarget()
    {
        var w = new World(TestRules.Basic());
        var e = w.Spawn("tank", 0, Vec2.Zero);
        var slots = Formation.Spread(new[] { e }, new Vec2(3, 3));
        Assert.Equal(new Vec2(3, 3), slots[0]);
    }

    [Fact]
    public void Spread_GivesEveryUnitADistinctSlotCentredOnTarget()
    {
        var w = new World(TestRules.Basic());
        var units = Enumerable.Range(0, 7).Select(i => w.Spawn("tank", 0, new Vec2(i, 0))).ToArray();
        var target = new Vec2(20, 10);
        var slots = Formation.Spread(units, target);
        Assert.Equal(7, slots.Distinct().Count());
        var centroid = new Vec2(slots.Average(s => s.X), slots.Average(s => s.Y));
        Assert.True(Vec2.Distance(centroid, target) < 1.5f);
        for (var i = 0; i < slots.Length; i++)
            for (var j = i + 1; j < slots.Length; j++)
                Assert.True(Vec2.Distance(slots[i], slots[j]) >= 1.0f);
    }
}
