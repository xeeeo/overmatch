using Overmatch.Sim;

namespace Overmatch.Sim.Tests;

public class WorldTests
{
    [Fact]
    public void Step_AdvancesTickAndTime()
    {
        var w = new World(TestRules.Basic());
        TestRules.Run(w, World.TicksPerSecond);
        Assert.Equal(20, w.Tick);
        Assert.Equal(1f, w.Time, 5);
    }

    [Fact]
    public void MoveCommand_UnitArrivesAtTarget()
    {
        var w = new World(TestRules.Basic());
        var e = w.Spawn("tank", 0, new Vec2(5, 5));
        w.Submit(new MoveCommand(0, new[] { e.Id }, new Vec2(15, 5)));
        TestRules.Run(w, 80);
        Assert.False(e.IsMoving);
        Assert.Equal(new Vec2(15, 5), e.Pos);
    }

    [Fact]
    public void MoveCommand_RoutesAroundWall()
    {
        var w = new World(TestRules.Basic(), TestRules.WalledMap());
        var e = w.Spawn("tank", 0, new Vec2(5.5f, 20.5f));
        w.Submit(new MoveCommand(0, new[] { e.Id }, new Vec2(35.5f, 20.5f)));
        w.Step();
        Assert.True(e.IsMoving);
        for (var i = 0; i < 20 * 25 && e.IsMoving; i++)
        {
            w.Step();
            Assert.True(w.Grid.IsPassable(e.Pos, Locomotor.Tracked), $"tank inside wall at {e.Pos} tick {w.Tick}");
        }
        Assert.False(e.IsMoving, "should have arrived");
        Assert.True(Vec2.Distance(e.Pos, new Vec2(35.5f, 20.5f)) < 0.3f, $"ended at {e.Pos}");
    }

    [Fact]
    public void MoveCommand_ToBlockedCellSnapsToNearestFree()
    {
        var w = new World(TestRules.Basic(), TestRules.WalledMap());
        var e = w.Spawn("tank", 0, new Vec2(15.5f, 20.5f));
        w.Submit(new MoveCommand(0, new[] { e.Id }, new Vec2(20.5f, 20.5f)));
        TestRules.Run(w, 100);
        Assert.False(e.IsMoving);
        Assert.True(w.Grid.IsPassable(e.Pos, Locomotor.Tracked));
        Assert.True(e.Pos.X < 20f && e.Pos.X > 18f, $"ended at {e.Pos}");
    }

    [Fact]
    public void MoveCommand_IgnoresUnitsOfOtherPlayers()
    {
        var w = new World(TestRules.Basic());
        var mine = w.Spawn("tank", 0, new Vec2(5, 5));
        var theirs = w.Spawn("tank", 1, new Vec2(50, 50));
        w.Submit(new MoveCommand(0, new[] { mine.Id, theirs.Id }, new Vec2(10, 5)));
        w.Step();
        Assert.True(mine.IsMoving);
        Assert.False(theirs.IsMoving);
    }

    [Fact]
    public void StopCommand_ClearsMove()
    {
        var w = new World(TestRules.Basic());
        var e = w.Spawn("tank", 0, new Vec2(5, 5));
        w.Submit(new MoveCommand(0, new[] { e.Id }, new Vec2(10, 5)));
        w.Step();
        w.Submit(new StopCommand(0, new[] { e.Id }));
        w.Step();
        Assert.False(e.IsMoving);
    }

    [Fact]
    public void Step_IsDeterministic()
    {
        static (Vec2[] pos, int alive) Run()
        {
            var w = new World(TestRules.Basic());
            var ids = new List<int>();
            for (var i = 0; i < 9; i++) ids.Add(w.Spawn("tank", 0, new Vec2(5 + i % 3 * 1.2f, 5 + i / 3 * 1.2f)).Id);
            for (var i = 0; i < 6; i++) w.Spawn("tank", 1, new Vec2(30 + i * 1.5f, 30));
            w.Submit(new AttackMoveCommand(0, ids.ToArray(), new Vec2(30, 30)));
            TestRules.Run(w, 600);
            return (w.Entities.Select(e => e.Pos).ToArray(), w.Entities.Count);
        }
        var a = Run();
        var b = Run();
        Assert.Equal(a.alive, b.alive);
        Assert.Equal(a.pos, b.pos);
    }

    [Fact]
    public void Separation_KeepsUnitsApart()
    {
        var w = new World(TestRules.Basic());
        var a = w.Spawn("tank", 0, new Vec2(10, 10));
        var b = w.Spawn("tank", 0, new Vec2(10.1f, 10));
        TestRules.Run(w, 40);
        Assert.True(Vec2.Distance(a.Pos, b.Pos) >= 0.95f, $"distance was {Vec2.Distance(a.Pos, b.Pos)}");
    }

    [Fact]
    public void GroupMove_AllArriveWithoutOverlap()
    {
        var w = new World(TestRules.Basic());
        var ids = new List<int>();
        for (var i = 0; i < 12; i++) ids.Add(w.Spawn("tank", 0, new Vec2(5 + i % 4 * 1.5f, 5 + i / 4 * 1.5f)).Id);
        w.Submit(new MoveCommand(0, ids.ToArray(), new Vec2(40, 40)));
        TestRules.Run(w, 20 * 20);
        foreach (var e in w.Entities)
        {
            Assert.False(e.IsMoving, $"entity {e.Id} still moving at {e.Pos}");
            Assert.True(Vec2.Distance(e.Pos, new Vec2(40, 40)) < 5f, $"entity {e.Id} far away at {e.Pos}");
        }
        var list = w.Entities.ToList();
        for (var i = 0; i < list.Count; i++)
            for (var j = i + 1; j < list.Count; j++)
                Assert.True(Vec2.Distance(list[i].Pos, list[j].Pos) >= 0.9f, $"{list[i].Id} and {list[j].Id} overlap");
    }
}
