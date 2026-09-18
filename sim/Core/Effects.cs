using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>A lingering damage area on the ground.</summary>
public sealed class Hazard
{
    public int Id { get; init; }
    public int Owner { get; init; }
    public Vec2 Pos { get; init; }
    public float Radius { get; init; }
    public float Dps { get; init; }
    public string DamageType { get; init; } = "toxin";
    public float Remaining { get; set; }
    public float Duration { get; init; }
}

/// <summary>A scheduled series of impacts (artillery barrage, carpet bomb, superweapon).</summary>
public sealed class Strike
{
    public int Owner { get; init; }
    public string Kind { get; init; } = "strike";
    public float Damage { get; init; }
    public string DamageType { get; init; } = "explosive";
    public float Radius { get; init; }
    public float Falloff { get; init; }
    public HazardDef? Hazard { get; init; }
    public StatusDef? Status { get; init; }
    public readonly List<(int tick, Vec2 pos)> Impacts = new();
}

public sealed class Crate
{
    public int Id { get; init; }
    public Vec2 Pos { get; init; }
    public float Remaining { get; set; } = 60f;
}

public sealed class Reveal
{
    public int Player { get; init; }
    public Vec2 Pos { get; init; }
    public float Radius { get; init; }
    public float Remaining { get; set; }
}

/// <summary>Statuses, auras, hazards, strikes, lifetimes, spawners, salvage, ammo, and the shared effect applier.</summary>
public static class Effects
{
    public const int AuraInterval = 5;

    public static void AddStatus(Entity e, string type, float duration, float magnitude = 1f, int source = 0)
    {
        foreach (var s in e.Statuses)
            if (s.Type == type) { s.Remaining = MathF.Max(s.Remaining, duration); s.Magnitude = magnitude; return; }
        e.Statuses.Add(new Status { Type = type, Remaining = duration, Magnitude = magnitude, SourceId = source });
    }

    public static void Update(World world)
    {
        var dt = World.Dt;
        var tick = world.Tick;

        // Statuses, cooldowns, lifetimes, poison.
        foreach (var e in world.Entities)
        {
            if (!e.Alive) continue;
            for (var i = e.Statuses.Count - 1; i >= 0; i--)
            {
                var s = e.Statuses[i];
                s.Remaining -= dt;
                if (s.Type == "poisoned" && tick % 10 == 0) Combat.DirectDamage(world, e, s.Magnitude * 0.5f, "toxin", world.Get(s.SourceId));
                if (s.Remaining <= 0f) e.Statuses.RemoveAt(i);
            }
            for (var i = 0; i < e.AbilityCooldowns.Length; i++)
                if (e.AbilityCooldowns[i] > 0f) e.AbilityCooldowns[i] -= dt;
            if (e.LifetimeLeft > 0f)
            {
                e.LifetimeLeft -= dt;
                if (e.LifetimeLeft <= 0f) Combat.Kill(world, e, null, silent: true);
            }
            // Veteran level 3 self-heals.
            if (e.Level >= 3 && tick % 20 == 0 && e.Hp < e.MaxHp) e.Hp = MathF.Min(e.MaxHp, e.Hp + e.MaxHp * 0.02f);
        }

        if (tick % AuraInterval == 0) Auras(world);
        Hazards(world);
        Strikes(world);
        Spawners(world);
        Salvage(world);
        Rearm(world);
        for (var i = world.RevealList.Count - 1; i >= 0; i--)
        {
            world.RevealList[i].Remaining -= dt;
            if (world.RevealList[i].Remaining <= 0f) world.RevealList.RemoveAt(i);
        }
    }

    private static void Auras(World world)
    {
        var dt = World.Dt * AuraInterval;
        foreach (var e in world.Entities) { e.HordeMult = 1f; if (!e.IsBuilding) e.BeingRepaired = false; }
        foreach (var src in world.Entities)
        {
            if (!src.Operational || src.IsInside) continue;
            if (src.Def.Horde)
            {
                // Five or more horde units of the same owner within 8 → bonus for this one.
                var n = 0;
                foreach (var o in world.Spatial.Query(src.Pos, 8f))
                    if (o.Owner == src.Owner && o.Def.Horde && o.Alive) n++;
                if (n >= 5) src.HordeMult = 1f + world.Player(src.Owner).HordeBonus;
            }
            foreach (var aura in src.Def.Auras)
            {
                switch (aura.Type)
                {
                    case "heal":
                        foreach (var o in world.Spatial.Query(src.Pos, aura.Radius))
                            if (o.Owner == src.Owner && o.Alive && !o.IsBuilding && !o.IsInside && o.Hp < o.MaxHp && aura.Affects(o.Def))
                            {
                                o.Hp = MathF.Min(o.MaxHp, o.Hp + aura.Amount * dt);
                                o.BeingRepaired = true;
                            }
                        break;
                    case "jam":
                        foreach (var o in world.Spatial.Query(src.Pos, aura.Radius).ToArray())
                        {
                            if (o.Owner == src.Owner || !o.Alive || o.Owner < 0) continue;
                            if (o.Def.Tags.Contains("drone")) Combat.DirectDamage(world, o, aura.Amount * dt, "jam", src);
                            else AddStatus(o, "jammed", 1.5f, 1f, src.Id);
                        }
                        break;
                }
            }
        }
    }

    private static void Hazards(World world)
    {
        var dt = World.Dt;
        for (var i = world.HazardList.Count - 1; i >= 0; i--)
        {
            var h = world.HazardList[i];
            h.Remaining -= dt;
            if (h.Remaining <= 0f) { world.HazardList.RemoveAt(i); continue; }
            if ((world.Tick + h.Id) % 10 != 0) continue;
            foreach (var e in world.Spatial.Query(h.Pos, h.Radius).ToArray())
            {
                if (!e.Alive || e.IsInside || e.Def.IsAir) continue;
                Combat.DirectDamage(world, e, h.Dps * 0.5f, h.DamageType, null, h.Owner);
            }
        }
    }

    private static void Strikes(World world)
    {
        for (var i = world.StrikeList.Count - 1; i >= 0; i--)
        {
            var s = world.StrikeList[i];
            for (var k = s.Impacts.Count - 1; k >= 0; k--)
            {
                var (tick, pos) = s.Impacts[k];
                if (tick > world.Tick) continue;
                s.Impacts.RemoveAt(k);
                Combat.AreaDamage(world, pos, s.Radius, s.Damage, s.DamageType, s.Falloff, null, s.Owner, friendlyFire: true);
                if (s.Status is { } st)
                    foreach (var e in world.Spatial.Query(pos, s.Radius).ToArray())
                        if (e.Alive && e.Owner != s.Owner) AddStatus(e, st.Type, st.Duration, st.Magnitude);
                if (s.Hazard is { } hz) AddHazard(world, s.Owner, pos, hz);
                world.Emit(new StrikeImpactEvent(pos, s.Radius, s.Kind));
            }
            if (s.Impacts.Count == 0) world.StrikeList.RemoveAt(i);
        }
    }

    public static void AddHazard(World world, int owner, Vec2 pos, HazardDef def)
    {
        world.HazardList.Add(new Hazard { Id = world.NextId(), Owner = owner, Pos = pos, Radius = def.Radius, Dps = def.Dps, DamageType = def.DamageType, Remaining = def.Duration, Duration = def.Duration });
        world.Emit(new HazardEvent(pos, def.Radius, def.Duration, def.DamageType));
    }

    private static void Spawners(World world)
    {
        var count = world.Entities.Count;
        for (var i = 0; i < count; i++)
        {
            var e = world.Entities[i];
            if (!e.Operational || e.Def.Spawner is not { } sp) continue;
            e.SpawnTimer -= World.Dt;
            if (e.SpawnTimer > 0f) continue;
            e.SpawnTimer = sp.Interval;
            var alive = 0;
            foreach (var o in world.Entities) if (o.Alive && o.FollowId == e.Id) alive++;
            if (alive >= sp.Max) continue;
            var d = world.Spawn(sp.Unit, e.Owner, e.Pos + Vec2.FromAngle(alive * 1.7f) * 1.5f, e.Facing);
            d.FollowId = e.Id;
        }
    }

    private static void Salvage(World world)
    {
        var dt = World.Dt;
        for (var i = world.CrateList.Count - 1; i >= 0; i--)
        {
            var c = world.CrateList[i];
            c.Remaining -= dt;
            if (c.Remaining <= 0f) { world.CrateList.RemoveAt(i); world.Emit(new CrateEvent(c.Id, c.Pos, false)); continue; }
            Entity? taker = null;
            foreach (var e in world.Spatial.Query(c.Pos, 2.5f))
                if (e.Alive && e.Def.Salvager && e.SalvageLevel < 2 && (e.Pos - c.Pos).Length <= e.Radius + 0.8f) { taker = e; break; }
            if (taker is null) continue;
            var frac = taker.HpFraction;
            taker.SalvageLevel++;
            taker.MaxHp = taker.Def.Hp * world.HpMultFor(taker.Owner, taker.Def) * taker.MaxHpMult;
            taker.Hp = taker.MaxHp * frac;
            world.CrateList.RemoveAt(i);
            world.Emit(new CrateEvent(c.Id, c.Pos, false));
        }
    }

    private static void Rearm(World world)
    {
        foreach (var e in world.Entities)
        {
            if (!e.Alive || e.Unit is not { IsAir: true } u) continue;
            if (e.ReturningToBase && !e.Rearming)
            {
                // Ordered home for repairs: hold over the nearest airfield until fixed.
                var home = NearestPad(world, e);
                if (home is null || e.Hp >= e.MaxHp) { e.ReturningToBase = false; continue; }
                if ((home.Pos - e.Pos).Length > 2.5f && (e.Move is null || e.Move.Kind != MoveKind.Work))
                    e.Move = new MoveOrder { Target = home.Pos, Kind = MoveKind.Work, ArriveRadius = 1.5f };
                continue;
            }
            if (u.Ammo <= 0 || !e.Rearming) continue;
            // Fly to the nearest own airfield pad; wait; refill.
            var pad = NearestPad(world, e);
            if (pad is null) { e.Rearming = false; e.Ammo = u.Ammo; continue; }
            if ((pad.Pos - e.Pos).Length > 2.5f)
            {
                if (e.Move is null || e.Move.Kind != MoveKind.Work)
                    e.Move = new MoveOrder { Target = pad.Pos, Kind = MoveKind.Work, ArriveRadius = 1.5f };
                continue;
            }
            e.Move = null;
            e.RearmTimer -= World.Dt;
            if (e.RearmTimer <= 0f) { e.Ammo = u.Ammo; e.Rearming = false; }
        }
    }

    public static Entity? NearestPad(World world, Entity e) =>
        world.Entities.Where(b => b.Owner == e.Owner && b.Alive && b.Operational && b.Building is { Pads: > 0 })
            .OrderBy(b => (b.Pos - e.Pos).LengthSq).FirstOrDefault();

    /// <summary>Apply a power/ability/superweapon effect at a point for a player.</summary>
    public static void Apply(World world, EffectSpec fx, int owner, Vec2 at, Entity? source, string kind)
    {
        switch (fx.Type)
        {
            case "spawn":
            {
                var def = world.Rules.Unit(fx.Unit);
                for (var i = 0; i < fx.Count; i++)
                {
                    var off = fx.Count == 1 ? Vec2.Zero : Vec2.FromAngle(i * (MathF.PI * 2f / fx.Count)) * (1.2f + fx.Count * 0.25f);
                    var pos = world.ClampToMap(at + off);
                    if (!def.IsAir) { var (cx, cy) = MapGrid.CellOf(pos); var free = world.Grid.NearestPassable(cx, cy, def.LocomotorClass); if (free is not null) pos = MapGrid.Centre(free.Value.x, free.Value.y); }
                    var u = world.Spawn(def.Id, owner, pos, (at - pos).Angle);
                    if (fx.Lifetime > 0f) u.LifetimeLeft = fx.Lifetime;
                    if (source is not null && def.Lifetime > 0f) u.FollowId = source.Id;
                }
                break;
            }
            case "strike":
            {
                var s = new Strike { Owner = owner, Kind = kind, Damage = fx.Damage, DamageType = fx.DamageType, Radius = fx.Radius, Falloff = fx.Falloff, Hazard = fx.Hazard, Status = fx.Status };
                var start = world.Tick + (int)(fx.Delay * World.TicksPerSecond);
                var span = (int)(fx.Interval * World.TicksPerSecond);
                for (var i = 0; i < fx.Impacts; i++)
                {
                    var t = start + (fx.Impacts > 1 ? span * i / (fx.Impacts - 1) : 0);
                    var off = fx.Scatter > 0f ? Vec2.FromAngle(world.Rng.NextSingle() * MathF.PI * 2f) * (world.Rng.NextSingle() * fx.Scatter) : Vec2.Zero;
                    s.Impacts.Add((t, at + off));
                }
                world.StrikeList.Add(s);
                break;
            }
            case "reveal":
                world.RevealList.Add(new Reveal { Player = owner, Pos = at, Radius = fx.Radius, Remaining = fx.Duration });
                break;
            case "status":
                if (fx.Status is { } st)
                    foreach (var e in world.Spatial.Query(at, fx.Radius).ToArray())
                    {
                        if (!e.Alive) continue;
                        var friend = e.Owner == owner;
                        if (fx.Targets == "enemies" ? friend || e.Owner < 0 : !friend) continue;
                        AddStatus(e, st.Type, st.Duration, st.Magnitude, source?.Id ?? 0);
                    }
                break;
            case "heal":
                foreach (var e in world.Spatial.Query(at, fx.Radius))
                    if (e.Alive && e.Owner == owner) e.Hp = MathF.Min(e.MaxHp, e.Hp + e.MaxHp * fx.Amount);
                break;
            case "damage":
                Combat.AreaDamage(world, at, fx.Radius, fx.Damage, fx.DamageType, fx.Falloff, source, owner, friendlyFire: false);
                if (fx.Hazard is { } hz2) AddHazard(world, owner, at, hz2);
                break;
            case "hazard":
                if (fx.Hazard is { } hz) AddHazard(world, owner, at, hz);
                break;
            case "bounty":
                world.Player(owner).BountyPerKill += fx.Amount;
                break;
            case "discount":
                world.Player(owner).DiscountMult = fx.Amount;
                world.Player(owner).DiscountUntil = world.Time + fx.Duration;
                break;
        }
    }
}
