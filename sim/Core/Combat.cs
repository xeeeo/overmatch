using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>Target acquisition, aiming, firing, projectiles and damage.</summary>
public static class Combat
{
    /// <summary>How far beyond weapon range a unit will notice and chase enemies.</summary>
    private const float AcquireBonus = 3f;
    private const int AcquireEvery = 5;
    private const int ChaseRefreshTicks = 8;

    public static void Update(World world)
    {
        var dt = World.Dt;
        foreach (var e in world.Entities)
        {
            if (!e.Operational || !e.HasWeapons) continue;
            for (var i = 0; i < e.Cooldowns.Length; i++)
                if (e.Cooldowns[i] > 0f) e.Cooldowns[i] -= dt;
            if (e.Building is { NeedsPower: true } && world.Player(e.Owner).LowPower) { e.TargetId = 0; continue; }

            var target = ValidateTarget(e, world);
            if (target is null && (world.Tick + e.Id) % AcquireEvery == 0 && CanAutoAcquire(e))
                target = Acquire(e, world);

            if (target is null)
            {
                Idle(e, world);
                continue;
            }
            Engage(e, target, world);
        }
    }

    private static bool CanAutoAcquire(Entity e) =>
        (e.Move is null || e.Move.Kind == MoveKind.AttackMove || e.Move.Kind == MoveKind.Chase) && e.BuildTargetId == 0 && e.HarvestState == HarvestState.Idle;

    private static Entity? ValidateTarget(Entity e, World world)
    {
        if (e.TargetId == 0) return null;
        var t = world.Get(e.TargetId);
        if (t is null || t.Owner == e.Owner || !CanHit(e, t, world.Rules) ||
            (!e.ExplicitTarget && !world.Vision.IsVisible(e.Owner, t.Pos)))
        {
            e.TargetId = 0;
            e.ExplicitTarget = false;
            return null;
        }
        return t;
    }

    private static bool CanHit(Entity e, Entity t, GameRules rules)
    {
        foreach (var wid in e.Def.Weapons)
        {
            var w = rules.Weapon(wid);
            if (t.Def.IsAir ? w.CanTargetAir : w.CanTargetGround) return true;
        }
        return false;
    }

    private static Entity? Acquire(Entity e, World world)
    {
        var range = MaxRange(e, world.Rules) + AcquireBonus;
        Entity? best = null;
        var bestD = float.MaxValue;
        foreach (var c in world.Spatial.Query(e.Pos, range + 6f))
        {
            if (c.Owner == e.Owner || !c.Alive || !CanHit(e, c, world.Rules)) continue;
            if (!world.Vision.IsVisible(e.Owner, c.Pos)) continue;
            var d = c.IsBuilding ? World.DistanceToBounds(c, e.Pos) : (c.Pos - e.Pos).Length;
            if (d > range) continue;
            // Prefer things that shoot back, then the closest.
            var score = d + (c.HasWeapons ? 0f : 4f);
            if (score < bestD) { bestD = score; best = c; }
        }
        if (best is not null)
        {
            e.TargetId = best.Id;
            e.ExplicitTarget = false;
            if (e.Move is { Kind: MoveKind.AttackMove }) { e.SuspendedMove = e.Move; e.Move = null; }
        }
        return best;
    }

    private static float MaxRange(Entity e, GameRules rules)
    {
        var r = 0f;
        foreach (var wid in e.Def.Weapons) r = MathF.Max(r, rules.Weapon(wid).Range);
        return r;
    }

    private static void Idle(Entity e, World world)
    {
        if (e.Move is { Kind: MoveKind.Chase }) e.Move = null;
        if (e.Move is null && e.SuspendedMove is not null)
        {
            e.Move = e.SuspendedMove;
            e.SuspendedMove = null;
        }
        // Turret drifts back to the hull heading.
        if (e.Def.HasTurret && e.Move is null)
            e.TurretFacing = Angles.TurnToward(e.TurretFacing, e.Facing, Angles.DegToRad(e.Def.TurretTurnRate) * 0.5f * World.Dt);
    }

    private static void Engage(Entity e, Entity target, World world)
    {
        var rules = world.Rules;
        var toTarget = target.Pos - e.Pos;
        var dist = target.IsBuilding ? World.DistanceToBounds(target, e.Pos) : toTarget.Length;
        var aim = toTarget.Angle;

        // Pick the first weapon that can reach and hit.
        WeaponDef? weapon = null;
        var weaponIndex = -1;
        for (var i = 0; i < e.Def.Weapons.Count; i++)
        {
            var w = rules.Weapon(e.Def.Weapons[i]);
            if (target.Def.IsAir ? !w.CanTargetAir : !w.CanTargetGround) continue;
            var reach = target.IsBuilding ? w.Range : w.Range + target.Radius;
            if (dist <= reach && dist >= w.MinRange) { weapon = w; weaponIndex = i; break; }
        }

        if (weapon is null)
        {
            // Out of range: close in (unless we were told to just move somewhere). Buildings cannot.
            if (e.IsBuilding) { e.TargetId = 0; e.ExplicitTarget = false; return; }
            if (e.Move is null || e.Move.Kind == MoveKind.Chase)
            {
                if (e.Move is null || (world.Tick + e.Id) % ChaseRefreshTicks == 0)
                    e.Move = new MoveOrder { Target = target.Pos, Kind = MoveKind.Chase, ArriveRadius = 0.5f };
            }
            if (e.Def.HasTurret)
                e.TurretFacing = Angles.TurnToward(e.TurretFacing, aim, Angles.DegToRad(e.Def.TurretTurnRate) * World.Dt);
            return;
        }

        // In range: stop chasing and aim.
        if (e.Move is { Kind: MoveKind.Chase }) e.Move = null;
        bool aimed;
        if (e.Def.HasTurret)
        {
            e.TurretFacing = Angles.TurnToward(e.TurretFacing, aim, Angles.DegToRad(e.Def.TurretTurnRate) * World.Dt);
            aimed = MathF.Abs(Angles.Wrap(aim - e.TurretFacing)) <= Angles.DegToRad(weapon.AimTolerance);
        }
        else if (e.IsBuilding)
        {
            aimed = true;
        }
        else
        {
            if (e.Move is null) e.Facing = Angles.TurnToward(e.Facing, aim, Angles.DegToRad(e.Unit!.TurnRate) * World.Dt);
            e.TurretFacing = e.Facing;
            aimed = MathF.Abs(Angles.Wrap(aim - e.Facing)) <= Angles.DegToRad(weapon.AimTolerance);
        }

        if (aimed && e.Cooldowns[weaponIndex] <= 0f)
        {
            e.Cooldowns[weaponIndex] = weapon.Cooldown;
            Fire(e, target, weapon, world);
        }
    }

    private static void Fire(Entity e, Entity target, WeaponDef weapon, World world)
    {
        if (weapon.Projectile.Kind == "instant" || weapon.Projectile.Speed <= 0f)
        {
            world.Emit(new WeaponFiredEvent(e.Id, weapon.Id, e.Pos, target.Pos, 0));
            ApplyHit(target.Pos, target, weapon, e, world);
            return;
        }
        var p = new Projectile
        {
            Id = world.NextId(),
            Weapon = weapon,
            Owner = e.Owner,
            SourceId = e.Id,
            TargetId = target.Id,
            Pos = e.Pos,
            PrevPos = e.Pos,
            Aim = target.Pos,
            Homing = weapon.Projectile.Kind == "missile",
            TotalDistance = MathF.Max(0.01f, (target.Pos - e.Pos).Length),
        };
        world.AddProjectile(p);
        world.Emit(new WeaponFiredEvent(e.Id, weapon.Id, e.Pos, target.Pos, p.Id));
    }

    public static void UpdateProjectiles(World world)
    {
        foreach (var p in world.Projectiles)
        {
            if (!p.Alive) continue;
            var target = world.Get(p.TargetId);
            if (p.Homing && target is not null) p.Aim = target.Pos;
            var to = p.Aim - p.Pos;
            var dist = to.Length;
            var step = p.Weapon.Projectile.Speed * World.Dt;
            if (dist <= step + 0.05f)
            {
                p.Pos = p.Aim;
                p.Alive = false;
                var source = world.Get(p.SourceId);
                // A shell hits whoever is where it lands; a missile hits its target if it is still there.
                Entity? direct = target is not null && (target.Pos - p.Aim).Length <= target.Radius + 0.4f ? target : null;
                ApplyHit(p.Aim, direct, p.Weapon, source, world, p.Owner);
                continue;
            }
            p.Pos += to / dist * step;
            p.Progress = 1f - dist / p.TotalDistance;
        }
    }

    private static void ApplyHit(Vec2 at, Entity? direct, WeaponDef weapon, Entity? attacker, World world, int owner = -1)
    {
        var attackerId = attacker?.Id ?? 0;
        var attackerOwner = attacker?.Owner ?? owner;
        world.Emit(new HitEvent(at, weapon.Id, direct?.Id ?? 0));

        if (direct is not null) Damage(direct, weapon.Damage, weapon, attacker, attackerId, world);

        if (weapon.Splash is { Radius: > 0f } splash)
        {
            foreach (var c in world.Spatial.Query(at, splash.Radius).ToArray())
            {
                if (!c.Alive || c == direct) continue;
                if (c.Owner == attackerOwner) continue; // no friendly fire for now
                var d = (c.Pos - at).Length;
                var t = MathF.Min(1f, d / splash.Radius);
                var mult = 1f - (1f - splash.Falloff) * t;
                Damage(c, weapon.Damage * mult, weapon, attacker, attackerId, world);
            }
        }
    }

    private static void Damage(Entity target, float baseDamage, WeaponDef weapon, Entity? attacker, int attackerId, World world)
    {
        if (!target.Alive) return;
        var amount = baseDamage * world.Rules.Armour.Multiplier(target.Def.Armour, weapon.DamageType);
        if (attacker is not null) amount *= world.Player(attacker.Owner).WeaponDamageMult(weapon.Id);
        target.Hp -= amount;
        world.Emit(new DamagedEvent(target.Id, amount, attackerId));
        if (target.Hp <= 0f)
        {
            target.Hp = 0f;
            target.Alive = false;
            if (attacker is { Alive: true })
            {
                attacker.Kills++;
                attacker.Xp += target.Def.XpValue;
            }
            if (target.IsBuilding) Production.RefundAll(world, target);
            world.Emit(new DiedEvent(target.Id, target.Def.Id, target.Owner, target.Pos, target.Facing, target.TurretFacing, attackerId, target.IsBuilding));
        }
    }
}
