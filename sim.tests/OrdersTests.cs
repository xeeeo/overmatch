using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

/// <summary>Generals-style standing orders: guard and scatter.</summary>
public class OrdersTests
{
    private static World NewWorld() =>
        new(RealDataTests.LoadShipped(), new MapDef { Id = "t", Width = 100, Height = 100 }, new[] { "coalition", "coalition" });

    [Fact]
    public void Guard_EngagesIntrudersInTheArea_ThenReturnsToPost()
    {
        var w = NewWorld();
        var tank = w.Spawn("coalition_bulwark", 0, new Vec2(20, 50));
        w.Submit(new GuardCommand(0, new[] { tank.Id }, new Vec2(30, 50)));
        w.Step();
        TestRules.RunUntil(w, () => tank.Move is null, 20 * 30);
        Assert.True((tank.Pos - new Vec2(30, 50)).Length < 3f);
        // An unarmed spotter, so the intruder is seen even though it is past the tank's own sight.
        w.Spawn("coalition_dozer", 0, new Vec2(30 + Combat.GuardRadius - 1.5f, 46));
        // Beyond weapon range but inside the guard radius: an idle tank would ignore it, a guard goes after it.
        var intruder = w.Spawn("coalition_dozer", 1, new Vec2(30 + Combat.GuardRadius - 1.5f, 50));
        TestRules.RunUntil(w, () => !intruder.Alive, 20 * 60);
        Assert.False(intruder.Alive, "guard should hunt down an intruder in its area");
        TestRules.RunUntil(w, () => tank.Move is null && (tank.Pos - new Vec2(30, 50)).Length < 3f, 20 * 30);
        Assert.True((tank.Pos - new Vec2(30, 50)).Length < 3f, "guard should walk back to its post");
        Assert.NotNull(tank.GuardPos);
    }

    [Fact]
    public void Guard_IgnoresEnemiesOutsideTheArea_AndAnyOrderEndsIt()
    {
        var w = NewWorld();
        var tank = w.Spawn("coalition_bulwark", 0, new Vec2(30, 50));
        w.Submit(new GuardCommand(0, new[] { tank.Id }, new Vec2(30, 50)));
        var far = w.Spawn("coalition_dozer", 1, new Vec2(30 + Combat.GuardLeash + 8f, 50));
        TestRules.Run(w, 20 * 10);
        Assert.True(far.Hp >= far.MaxHp);
        Assert.True((tank.Pos - new Vec2(30, 50)).Length < 3f);
        w.Submit(new MoveCommand(0, new[] { tank.Id }, new Vec2(20, 20)));
        w.Step();
        Assert.Null(tank.GuardPos);
    }

    [Fact]
    public void Scatter_SpreadsABunchedGroup()
    {
        var w = NewWorld();
        var units = Enumerable.Range(0, 6).Select(i => w.Spawn("coalition_rifleman", 0, new Vec2(50 + i % 3 * 0.8f, 50 + i / 3 * 0.8f))).ToList();
        float Spread() => units.Max(a => units.Max(b => (a.Pos - b.Pos).Length));
        var before = Spread();
        w.Submit(new ScatterCommand(0, units.Select(u => u.Id).ToArray()));
        TestRules.Run(w, 20 * 6);
        Assert.True(Spread() > before + 6f, $"spread {before} -> {Spread()}");
    }
}
