using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>Target acquisition, aiming, firing, projectiles and damage.</summary>
public static class Combat
{
    /// <summary>How far beyond weapon range a unit will notice and chase enemies.</summary>
    private const float AcquireBonus = 3f;
    /// <summary>Guard mode: enemies within this distance of the post are engaged; the chase is abandoned past the leash.</summary>
    public const float GuardRadius = 13f;
    public const float GuardLeash = 19f;
    public const float ScatterDistance = 6f;
    private const int AcquireEvery = 5;
    private const int ChaseRefreshTicks = 8;

    public static void Update(World world)
    {
        var dt = World.Dt;
        var count = world.Entities.Count;
        for (var idx = 0; idx < count; idx++)
        {
            var e = world.Entities[idx];
            if (!e.Operational || !e.HasWeapons || e.Owner < 0) continue;
            for (var i = 0; i < e.Cooldowns.Length; i++)
                if (e.Cooldowns[i] > 0f) e.Cooldowns[i] -= dt;
            if (e.Disabled || e.Rearming || (e.IsInside && world.Get(e.InsideId) is { Building.TunnelHub: true })) { e.TargetId = 0; continue; }
            if (e.Building is { NeedsPower: true } && world.Player(e.Owner).LowPower) { e.TargetId = 0; continue; }

            var target = ValidateTarget(e, world);
            // Guards do not chase past their leash; they let go and walk back to their post.
            if (target is not null && e.GuardPos is { } post && !e.ExplicitTarget && (target.Pos - post).Length > GuardLeash) { e.TargetId = 0; target = null; }
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
        (e.Move is null || e.Move.Kind == MoveKind.AttackMove || e.Move.Kind == MoveKind.Chase) && e.BuildTargetId == 0 && e.HarvestState == HarvestState.Idle
        && e.EnterTargetId == 0 && e.CaptureTargetId == 0;

    private static Entity? ValidateTarget(Entity e, World world)
    {
        if (e.TargetId == 0) return null;
        var t = world.Get(e.TargetId);
        if (t is null || t.Owner == e.Owner || t.IsInside || !CanHit(e, t, world.Rules) ||
            (!e.ExplicitTarget && !world.CanSee(e.Owner, t)) || (e.ExplicitTarget && t.Def.Stealth && !world.CanSee(e.Owner, t)))
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
        // A guard watches its whole area, not just what is in weapon range.
        if (e.GuardPos is not null) range = MathF.Max(range, GuardRadius);
        Entity? best = null;
        var bestD = float.MaxValue;
        foreach (var c in world.Spatial.Query(e.Pos, range + 6f))
        {
            if (c.Owner == e.Owner || c.Owner < 0 || !c.Alive || c.IsInside || !CanHit(e, c, world.Rules)) continue;
            if (!world.CanSee(e.Owner, c)) continue;
            if (c.Building is { IsHole: true } && (world.Tick / 20) % 3 != 0) continue; // holes are low priority
            var d = c.IsBuilding ? World.DistanceToBounds(c, e.Pos) : (c.Pos - e.Pos).Length;
            if (d > range) continue;
            if (e.GuardPos is { } post && (c.Pos - post).Length > GuardRadius + 2f) continue;
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
        // A guard with nothing to shoot walks back to its post.
        if (e.Move is null && e.GuardPos is { } post && (e.Pos - post).Length > 2.5f && !e.IsBuilding && !e.IsInside)
            e.Move = new MoveOrder { Target = post, Kind = MoveKind.AttackMove, ArriveRadius = 1.5f };
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

        // Pick the first weapon that can reach and hit. Garrisoned infantry get extra reach from the building.
        var bonus = e.IsInside ? 2.5f : 0f;
        WeaponDef? weapon = null;
        var weaponIndex = -1;
        for (var i = 0; i < e.Def.Weapons.Count; i++)
        {
            var w = rules.Weapon(e.Def.Weapons[i]);
            if (target.Def.IsAir ? !w.CanTargetAir : !w.CanTargetGround) continue;
            var reach = (target.IsBuilding ? w.Range : w.Range + target.Radius) + bonus;
            if (w.Suicide) reach = target.Radius + e.Radius + 0.4f;
            if (dist <= reach && dist >= w.MinRange) { weapon = w; weaponIndex = i; break; }
        }

        if (weapon is null)
        {
            // Out of range: close in (unless we were told to just move somewhere). Buildings and garrisoned units cannot.
            if (e.IsBuilding || e.IsInside) { e.TargetId = 0; e.ExplicitTarget = false; return; }
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
        if (e.IsInside) aimed = true;
        else if (e.Def.HasTurret)
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
            if (e.Def.Stealth || world.Player(e.Owner).StealthFor(e.Def)) Effects.AddStatus(e, "revealed", 2f);
            if (weapon.Suicide) { Detonate(e, target, weapon, world); return; }
            Fire(e, target, weapon, world);
            if (e.Unit is { Ammo: > 0 } && --e.Ammo <= 0) { e.Rearming = true; e.RearmTimer = e.Unit.RearmTime; e.TargetId = 0; e.Move = null; }
        }
    }

    private static void Detonate(Entity e, Entity target, WeaponDef weapon, World world)
    {
        world.Emit(new WeaponFiredEvent(e.Id, weapon.Id, e.Pos, target.Pos, 0));
        var at = e.Pos;
        e.Alive = false;
        world.Emit(new DiedEvent(e.Id, e.Def.Id, e.Owner, e.Pos, e.Facing, e.TurretFacing, 0, false));
        ApplyHit(at, target, weapon, e, world);
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
            if (p.Homing && target is not null)
            {
                // Missiles inside an enemy jamming aura lose guidance and fly on straight.
                var jammed = false;
                foreach (var j in world.Spatial.Query(p.Pos, 10f))
                    if (j.Owner != p.Owner && j.Operational && j.Def.Auras.Any(a => a.Type == "jam" && (j.Pos - p.Pos).Length <= a.Radius)) { jammed = true; break; }
                if (jammed) p.Aim = p.Pos + (p.Aim - p.Pos).Normalized * 6f + Vec2.FromAngle(p.Id) * 2f;
                else p.Aim = target.Pos;
            }
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

        if (direct is not null)
        {
            if (weapon.ClearsGarrison && direct.Passengers.Count > 0)
                foreach (var pid in direct.Passengers.ToArray())
                    if (world.Get(pid) is { } occupant) Damage(occupant, weapon.Damage, weapon, attacker, attackerId, world);
            Damage(direct, weapon.Damage, weapon, attacker, attackerId, world);
            if (weapon.Status is { } st && direct.Alive) Effects.AddStatus(direct, st.Type, st.Duration, st.Magnitude, attackerId);
        }
        if (weapon.Hazard is { } hz) Effects.AddHazard(world, attackerOwner, at, hz);

        if (weapon.Splash is { Radius: > 0f } splash)
        {
            foreach (var c in world.Spatial.Query(at, splash.Radius).ToArray())
            {
                if (!c.Alive || c == direct) continue;
                if (c.Owner == attackerOwner || c.IsInside) continue; // no friendly fire for now
                var d = c.IsBuilding ? World.DistanceToBounds(c, at) : (c.Pos - at).Length;
                if (d > splash.Radius) continue;
                var t = MathF.Min(1f, d / splash.Radius);
                var mult = 1f - (1f - splash.Falloff) * t;
                Damage(c, weapon.Damage * mult, weapon, attacker, attackerId, world);
                if (weapon.Status is { } st2 && c.Alive) Effects.AddStatus(c, st2.Type, st2.Duration, st2.Magnitude, attackerId);
            }
        }
    }

    private static void Damage(Entity target, float baseDamage, WeaponDef weapon, Entity? attacker, int attackerId, World world)
    {
        if (!target.Alive) return;
        var amount = baseDamage * world.Rules.Armour.Multiplier(target.Def.Armour, weapon.DamageType);
        if (attacker is not null && attacker.Owner >= 0) amount *= world.Player(attacker.Owner).WeaponDamageMult(weapon.Id) * attacker.DamageMult;
        target.Hp -= amount;
        target.LastDamagedTick = world.Tick;
        world.Emit(new DamagedEvent(target.Id, amount, attackerId));
        if (target.Hp <= 0f) Kill(world, target, attacker);
    }

    /// <summary>Damage outside the weapon pipeline: hazards, auras, crushing, poison.</summary>
    public static void DirectDamage(World world, Entity target, float baseDamage, string damageType, Entity? attacker, int owner = -1)
    {
        if (!target.Alive) return;
        var amount = baseDamage * world.Rules.Armour.Multiplier(target.Def.Armour, damageType);
        if (amount <= 0f) return;
        target.Hp -= amount;
        target.LastDamagedTick = world.Tick;
        world.Emit(new DamagedEvent(target.Id, amount, attacker?.Id ?? 0));
        if (target.Hp <= 0f) Kill(world, target, attacker, owner: owner);
    }

    /// <summary>Area damage with falloff, used by strikes, death explosions and power effects.</summary>
    public static void AreaDamage(World world, Vec2 at, float radius, float damage, string damageType, float falloff, Entity? attacker, int owner, bool friendlyFire)
    {
        foreach (var c in world.Spatial.Query(at, radius + 3f).ToArray())
        {
            if (!c.Alive || c.IsInside) continue;
            if (!friendlyFire && c.Owner == owner) continue;
            var d = c.IsBuilding ? World.DistanceToBounds(c, at) : (c.Pos - at).Length;
            if (d > radius) continue;
            var t = MathF.Min(1f, d / radius);
            DirectDamage(world, c, damage * (1f - (1f - falloff) * t), damageType, attacker, owner);
        }
    }

    public static void Kill(World world, Entity target, Entity? attacker, bool silent = false, int owner = -1)
    {
        if (!target.Alive) return;
        target.Hp = 0f;
        target.Alive = false;
        var attackerId = attacker?.Id ?? 0;
        var killerOwner = attacker?.Owner ?? owner;
        if (attacker is { Alive: true }) { attacker.Kills++; attacker.Xp += target.Def.XpValue; }
        if (!silent && target.Owner >= 0) world.Player(target.Owner).UnitsLost++;
        if (!silent && killerOwner >= 0 && killerOwner != target.Owner) world.Player(killerOwner).UnitsKilled++;
        if (killerOwner >= 0 && killerOwner != target.Owner && !silent)
        {
            var killer = world.Player(killerOwner);
            Powers.AddXp(world, killer, target.Def.XpValue);
            if (killer.BountyPerKill > 0f) killer.Cash += (int)(target.Def.Cost * killer.BountyPerKill);
            if (killer.Faction.Salvage && target.Def.Tags.Contains("vehicle") && !target.Def.IsAir)
            {
                var crate = new Crate { Id = world.NextId(), Pos = target.Pos };
                world.CrateList.Add(crate);
                world.Emit(new CrateEvent(crate.Id, crate.Pos, true));
            }
        }
        if (target.IsBuilding) Production.RefundAll(world, target);
        if (target.Passengers.Count > 0 || target.Building is { TunnelHub: true }) Garrison.ContainerDied(world, target);
        world.Emit(new DiedEvent(target.Id, target.Def.Id, target.Owner, target.Pos, target.Facing, target.TurretFacing, attackerId, target.IsBuilding));
        if (target.Def.DeathDamage is { } dd)
        {
            AreaDamage(world, target.Pos, dd.Radius, dd.Damage, dd.DamageType, 0.3f, null, -1, friendlyFire: true);
            if (dd.Hazard is { } hz) Effects.AddHazard(world, target.Owner, target.Pos, hz);
            world.Emit(new StrikeImpactEvent(target.Pos, dd.Radius, "death"));
        }
        if (target.Building is { RebuildHole: true, IsHole: false } && target.Owner >= 0 && world.Rules.Buildings.TryGetValue("hole", out var holeDef))
        {
            // The footprint is freed at the end of the tick; the hole is placed then.
            world.PendingHoles.Add((target.Def.Id, target.Owner, target.CellX, target.CellY));
        }
    }
}
