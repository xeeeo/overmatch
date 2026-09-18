using Overmatch.Sim;

namespace Overmatch.Sim.Tests;

public class WorldTests
{
    [Fact]
    public void Step_AdvancesTickAndTime()
    {
        var w = new World(TestRules.Basic());
        for (var i = 0; i < World.TicksPerSecond; i++) w.Step();
        Assert.Equal(20, w.Tick);
        Assert.Equal(1f, w.Time, 5);
    }

    [Fact]
    public void MoveCommand_UnitArrivesAtTarget()
    {
        var w = new World(TestRules.Basic());
        var e = w.Spawn("tank", 0, Vec2.Zero);
        w.Submit(new MoveCommand(0, new[] { e.Id }, new Vec2(10, 0)));
        // 10 units at 4 u/s = 2.5 s = 50 ticks; allow slack for turning.
        for (var i = 0; i < 70; i++) w.Step();
        Assert.False(e.IsMoving);
        Assert.Equal(new Vec2(10, 0), e.Pos);
    }

    [Fact]
    public void MoveCommand_IgnoresUnitsOfOtherPlayers()
    {
        var w = new World(TestRules.Basic());
        var mine = w.Spawn("tank", 0, Vec2.Zero);
        var theirs = w.Spawn("tank", 1, new Vec2(5, 5));
        w.Submit(new MoveCommand(0, new[] { mine.Id, theirs.Id }, new Vec2(10, 0)));
        w.Step();
        Assert.True(mine.IsMoving);
        Assert.False(theirs.IsMoving);
    }

    [Fact]
    public void StopCommand_ClearsMove()
    {
        var w = new World(TestRules.Basic());
        var e = w.Spawn("tank", 0, Vec2.Zero);
        w.Submit(new MoveCommand(0, new[] { e.Id }, new Vec2(10, 0)));
        w.Step();
        w.Submit(new StopCommand(0, new[] { e.Id }));
        w.Step();
        Assert.False(e.IsMoving);
    }

    [Fact]
    public void Step_IsDeterministic()
    {
        static Vec2[] Run()
        {
            var w = new World(TestRules.Basic());
            var ids = new List<int>();
            for (var i = 0; i < 9; i++) ids.Add(w.Spawn("tank", 0, new Vec2(i % 3 * 1.2f, i / 3 * 1.2f)).Id);
            w.Submit(new MoveCommand(0, ids.ToArray(), new Vec2(15, 7)));
            for (var t = 0; t < 200; t++) w.Step();
            return w.Entities.Select(e => e.Pos).ToArray();
        }
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void Separation_KeepsUnitsApart()
    {
        var w = new World(TestRules.Basic());
        var a = w.Spawn("tank", 0, Vec2.Zero);
        var b = w.Spawn("tank", 0, new Vec2(0.1f, 0));
        for (var t = 0; t < 40; t++) w.Step();
        Assert.True(Vec2.Distance(a.Pos, b.Pos) >= 0.95f, $"distance was {Vec2.Distance(a.Pos, b.Pos)}");
    }
}
