using Overmatch.Sim;

namespace Overmatch.Sim.Tests;

public class CombatTests
{
    [Fact]
    public void IdleTank_AutoEngagesVisibleEnemyInRange()
    {
        var w = new World(TestRules.Basic());
        var a = w.Spawn("tank", 0, new Vec2(10, 10));
        var b = w.Spawn("tank", 1, new Vec2(14, 10));
        TestRules.Run(w, 25); // enough for acquisition and one shot, not enough to kill
        Assert.Equal(b.Id, a.TargetId);
        Assert.True(b.Hp < b.Def.Hp, "target should have taken damage");
        Assert.True(b.Alive);
    }

    [Fact]
    public void Damage_UsesArmourTable()
    {
        var w = new World(TestRules.Basic());
        w.Spawn("tank", 0, new Vec2(10, 10));
        var scout = w.Spawn("scout", 1, new Vec2(14, 10));
        // Wait for exactly one hit.
        DamagedEvent? dmg = null;
        for (var i = 0; i < 200 && dmg is null; i++)
        {
            w.Step();
            dmg = w.Events.OfType<DamagedEvent>().FirstOrDefault();
        }
        Assert.NotNull(dmg);
        Assert.Equal(40f * 1.5f, dmg!.Amount, 3); // cannon 40 × light_vehicle vs armour_piercing 1.5
        Assert.Equal(scout.Id, dmg.EntityId);
    }

    [Fact]
    public void Kill_RemovesEntityAndCreditsKiller()
    {
        var w = new World(TestRules.Basic());
        var a = w.Spawn("tank", 0, new Vec2(10, 10));
        var b = w.Spawn("scout", 1, new Vec2(14, 10));
        DiedEvent? died = null;
        for (var i = 0; i < 400 && died is null; i++)
        {
            w.Step();
            died = w.Events.OfType<DiedEvent>().FirstOrDefault();
        }
        Assert.NotNull(died);
        Assert.Equal(b.Id, died!.EntityId);
        Assert.Equal(a.Id, died.KillerId);
        Assert.Null(w.Get(b.Id));
        Assert.DoesNotContain(w.Entities, e => e.Id == b.Id);
        Assert.Equal(1, a.Kills);
        Assert.Equal(10, a.Xp); // scout has the default xpValue
        w.Step(); // target validation runs on the next tick
        Assert.Equal(0, a.TargetId);
    }

    [Fact]
    public void Tank_DoesNotSeeThroughFog()
    {
        var w = new World(TestRules.Basic(), 80, 80);
        var a = w.Spawn("tank", 0, new Vec2(10, 10));
        var far = w.Spawn("scout", 1, new Vec2(40, 10)); // outside vision 10
        TestRules.Run(w, 40);
        Assert.Equal(0, a.TargetId);
        Assert.False(w.Vision.IsVisible(0, far.Pos));
        Assert.True(w.Vision.IsVisible(0, a.Pos));
        Assert.Equal(Visibility.Shroud, w.Vision.Get(0, far.Pos));
    }

    [Fact]
    public void Vision_ExploredStaysExplored()
    {
        var w = new World(TestRules.Basic(), 80, 80);
        var s = w.Spawn("scout", 0, new Vec2(10, 10));
        TestRules.Run(w, 5);
        Assert.Equal(Visibility.Visible, w.Vision.Get(0, new Vec2(15, 10)));
        w.Submit(new MoveCommand(0, new[] { s.Id }, new Vec2(60, 10)));
        TestRules.Run(w, 20 * 12);
        Assert.Equal(Visibility.Explored, w.Vision.Get(0, new Vec2(15, 10)));
        Assert.Equal(Visibility.Visible, w.Vision.Get(0, new Vec2(58, 10)));
    }

    [Fact]
    public void AttackMove_FightsThenContinues()
    {
        var w = new World(TestRules.Basic(), 80, 80);
        var a = w.Spawn("tank", 0, new Vec2(5, 20));
        var victim = w.Spawn("scout", 1, new Vec2(20, 20));
        w.Submit(new AttackMoveCommand(0, new[] { a.Id }, new Vec2(60, 20)));
        var sawSuspend = false;
        for (var i = 0; i < 20 * 40; i++)
        {
            w.Step();
            if (a.SuspendedMove is not null) sawSuspend = true;
            if (!a.IsMoving && a.SuspendedMove is null && w.Get(victim.Id) is null && a.TargetId == 0 && Vec2.Distance(a.Pos, new Vec2(60, 20)) < 0.5f) break;
        }
        Assert.True(sawSuspend, "attack-move should pause to fight");
        Assert.Null(w.Get(victim.Id));
        Assert.True(Vec2.Distance(a.Pos, new Vec2(60, 20)) < 0.5f, $"should resume to destination, ended at {a.Pos}");
    }

    [Fact]
    public void PlainMove_DoesNotStopToFight()
    {
        var w = new World(TestRules.Basic(), 80, 80);
        var a = w.Spawn("tank", 0, new Vec2(5, 20));
        var victim = w.Spawn("scout", 1, new Vec2(20, 23));
        w.Submit(new MoveCommand(0, new[] { a.Id }, new Vec2(40, 20)));
        TestRules.Run(w, 20 * 12);
        Assert.False(a.IsMoving);
        Assert.NotNull(w.Get(victim.Id));
        Assert.Equal(victim.Def.Hp, victim.Hp);
    }

    [Fact]
    public void ExplicitAttack_ChasesTarget()
    {
        var w = new World(TestRules.Basic(), 80, 80);
        var a = w.Spawn("tank", 0, new Vec2(5, 20));
        var victim = w.Spawn("scout", 1, new Vec2(30, 20));
        w.Submit(new AttackCommand(0, new[] { a.Id }, victim.Id));
        DiedEvent? died = null;
        for (var i = 0; i < 20 * 30 && died is null; i++)
        {
            w.Step();
            died = w.Events.OfType<DiedEvent>().FirstOrDefault();
        }
        Assert.NotNull(died);
        Assert.Equal(victim.Id, died!.EntityId);
    }

    [Fact]
    public void Projectiles_TravelThenHit()
    {
        var w = new World(TestRules.Basic());
        w.Spawn("tank", 0, new Vec2(10, 10));
        w.Spawn("scout", 1, new Vec2(15.5f, 10));
        WeaponFiredEvent? fired = null;
        for (var i = 0; i < 200 && fired is null; i++)
        {
            w.Step();
            fired = w.Events.OfType<WeaponFiredEvent>().FirstOrDefault();
        }
        Assert.NotNull(fired);
        Assert.NotEqual(0, fired!.ProjectileId);
        Assert.Single(w.Projectiles);
        Assert.Empty(w.Events.OfType<HitEvent>());
        HitEvent? hit = null;
        for (var i = 0; i < 20 && hit is null; i++)
        {
            w.Step();
            hit = w.Events.OfType<HitEvent>().FirstOrDefault();
        }
        Assert.NotNull(hit);
        Assert.Empty(w.Projectiles);
    }

    [Fact]
    public void TwoGroups_FightToTheEnd()
    {
        var w = new World(TestRules.Basic(), 80, 80);
        for (var i = 0; i < 6; i++) w.Spawn("tank", 0, new Vec2(10 + i * 1.5f, 10));
        for (var i = 0; i < 4; i++) w.Spawn("tank", 1, new Vec2(10 + i * 1.5f, 40));
        w.Submit(new AttackMoveCommand(0, w.Entities.Where(e => e.Owner == 0).Select(e => e.Id).ToArray(), new Vec2(14, 40)));
        TestRules.Run(w, 20 * 90);
        Assert.DoesNotContain(w.Entities, e => e.Owner == 1);
        Assert.Contains(w.Entities, e => e.Owner == 0);
    }
}
