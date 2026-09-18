using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>Generals' promotion ranks and powers, and superweapon charging.</summary>
public static class Powers
{
    public static void AddXp(World world, Player p, int amount)
    {
        p.Xp += amount;
        var thresholds = p.Faction.RankXp;
        while (p.Rank < thresholds.Length + 1 && p.Xp >= thresholds[p.Rank - 1])
        {
            p.Rank++;
            p.Points++;
            world.Emit(new RankUpEvent(p.Id, p.Rank));
        }
    }

    public static void Buy(World world, BuyPowerCommand cmd)
    {
        var p = world.Player(cmd.Player);
        if (!world.Rules.Powers.TryGetValue(cmd.PowerId, out var def) || !p.Faction.Powers.Contains(def.Id)) { world.Reject(cmd.Player, "unknown power"); return; }
        if (p.HasPower(def.Id)) { world.Reject(cmd.Player, "already owned"); return; }
        if (p.Rank < def.Rank) { world.Reject(cmd.Player, $"requires rank {def.Rank}"); return; }
        if (p.Points < 1) { world.Reject(cmd.Player, "no promotion points"); return; }
        p.Points--;
        p.GrantPower(def.Id);
        if (def.Effect.Type == "bounty")
        {
            // Passive powers take effect at once and never need firing.
            Effects.Apply(world, def.Effect, cmd.Player, Vec2.Zero, null, def.Id);
            p.SetPowerReadyAt(def.Id, float.MaxValue);
        }
        else
        {
            // A newly bought power has to charge before its first use, as in Generals.
            p.SetPowerReadyAt(def.Id, world.Time + def.Cooldown);
        }
    }

    public static void Use(World world, UsePowerCommand cmd)
    {
        var p = world.Player(cmd.Player);
        if (!p.HasPower(cmd.PowerId)) { world.Reject(cmd.Player, "power not owned"); return; }
        var def = world.Rules.Power(cmd.PowerId);
        if (world.Time < p.PowerReadyAt(def.Id)) { world.Reject(cmd.Player, "power recharging"); return; }
        p.SetPowerReadyAt(def.Id, world.Time + def.Cooldown);
        Effects.Apply(world, def.Effect, cmd.Player, world.ClampToMap(cmd.Target), null, def.Id);
        world.Emit(new PowerUsedEvent(cmd.Player, def.Id, cmd.Target));
    }

    public static void UpdateSuperweapons(World world)
    {
        foreach (var b in world.Entities)
        {
            if (!b.Operational || b.Building?.Superweapon is not { } sw) continue;
            if (b.SuperweaponCharge >= sw.ChargeTime) continue;
            var p = world.Player(b.Owner);
            b.SuperweaponCharge += p.LowPower && b.Building.NeedsPower ? World.Dt * 0.5f : World.Dt;
            if (b.SuperweaponCharge >= sw.ChargeTime)
            {
                b.SuperweaponCharge = sw.ChargeTime;
                world.Emit(new SuperweaponReadyEvent(b.Owner, b.Id));
            }
        }
    }

    public static void Fire(World world, FireSuperweaponCommand cmd)
    {
        var b = world.Get(cmd.BuildingId);
        if (b is null || b.Owner != cmd.Player || b.Building?.Superweapon is not { } sw || !b.Operational) { world.Reject(cmd.Player, "no superweapon"); return; }
        if (b.SuperweaponCharge < sw.ChargeTime) { world.Reject(cmd.Player, "superweapon charging"); return; }
        b.SuperweaponCharge = 0f;
        var target = world.ClampToMap(cmd.Target);
        Effects.Apply(world, sw.Effect, cmd.Player, target, b, sw.Name);
        world.Emit(new SuperweaponFiredEvent(cmd.Player, sw.Name, target, sw.Effect.Delay));
    }

    /// <summary>Unit abilities: cooldown, range and target checks, then the shared effect applier.</summary>
    public static void UseAbility(World world, AbilityCommand cmd)
    {
        var any = false;
        foreach (var id in cmd.Units)
        {
            var u = world.Get(id);
            if (u is null || u.Owner != cmd.Player || u.Disabled) continue;
            var idx = u.Def.Abilities.FindIndex(a => a.Id == cmd.AbilityId);
            if (idx < 0 || u.AbilityCooldowns[idx] > 0f) continue;
            var ab = u.Def.Abilities[idx];
            var at = cmd.Target;
            Entity? targetEntity = null;
            if (ab.Target is "unit" or "building")
            {
                targetEntity = world.Get(cmd.TargetId);
                if (targetEntity is null || (ab.Target == "building" && !targetEntity.IsBuilding)) continue;
                at = targetEntity.Pos;
            }
            else if (ab.Target == "none") at = u.Pos;
            var dist = targetEntity is { IsBuilding: true } ? World.DistanceToBounds(targetEntity, u.Pos) : (at - u.Pos).Length;
            if (ab.Target != "none" && dist > ab.Range)
            {
                // Walk into range first; the order is re-issued by the caller (UI/AI) — keep it simple: move toward.
                u.Move = new MoveOrder { Target = at, Kind = MoveKind.Work, ArriveRadius = MathF.Max(0.5f, ab.Range - 0.5f) };
                continue;
            }
            u.AbilityCooldowns[idx] = ab.Cooldown;
            if (targetEntity is not null && ab.Effect.Type == "status" && ab.Effect.Status is { } st)
            {
                Effects.AddStatus(targetEntity, st.Type, st.Duration, st.Magnitude, u.Id);
                if (targetEntity.Passengers.Count > 0 && st.Type == "disabled")
                {
                    var occupants = targetEntity.Passengers.ToArray();
                    Garrison.Exit(world, targetEntity, targetEntity.Owner);
                    foreach (var oid in occupants)
                        if (world.Get(oid) is { } o) Effects.AddStatus(o, st.Type, st.Duration, st.Magnitude, u.Id);
                }
            }
            else Effects.Apply(world, ab.Effect, cmd.Player, at, u, ab.Id);
            u.TurretFacing = (at - u.Pos).LengthSq > 0.01f ? (at - u.Pos).Angle : u.TurretFacing;
            world.Emit(new AbilityUsedEvent(u.Id, ab.Id, at));
            any = true;
            if (ab.Target != "none") break; // one caster per targeted ability
        }
        if (!any) world.Reject(cmd.Player, "ability not ready");
    }
}
