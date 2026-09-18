namespace Overmatch.Sim;

/// <summary>Builders place and raise buildings. Cost is charged at placement; construction needs a builder standing next to the site.</summary>
public static class Construction
{
    /// <summary>Holes regrow their building on their own; a builder standing by speeds it up (via BuildTargetId).</summary>
    private static void Holes(World world)
    {
        var count = world.Entities.Count;
        for (var i = 0; i < count; i++)
        {
            var h = world.Entities[i];
            if (!h.Alive || h.Building is not { IsHole: true } || h.HoleDefId == "") continue;
            h.BuildProgress += World.Dt / HoleRules.RegrowTime;
            if (h.BuildProgress < 1f) continue;
            var def = world.Rules.Building(h.HoleDefId);
            h.Alive = false;
            world.FreeFootprint(h);
            if (world.CanPlace(def, h.HoleCellX, h.HoleCellY, out _))
            {
                var b = world.PlaceBuilding(def.Id, h.Owner, h.HoleCellX, h.HoleCellY, complete: true);
                b.Hp = b.MaxHp * 0.5f;
                world.Emit(new HoleEvent(h.Id, def.Id, true));
            }
        }
    }

    /// <summary>How close to the footprint a builder must stand to work.</summary>
    public const float WorkRange = 1.4f;

    public static void Begin(World world, BuildCommand cmd)
    {
        var builder = world.Get(cmd.BuilderId);
        if (builder is null || builder.Owner != cmd.Player || !builder.IsBuilder) { world.Reject(cmd.Player, "no builder"); return; }
        if (!world.Rules.Buildings.TryGetValue(cmd.BuildingId, out var def)) { world.Reject(cmd.Player, "unknown building"); return; }
        var player = world.Player(cmd.Player);
        if (def.Faction != "" && def.Faction != player.Faction.Id) { world.Reject(cmd.Player, "wrong faction"); return; }
        if (!world.HasPrereqs(cmd.Player, def.Prereqs, out var missing)) { world.Reject(cmd.Player, $"requires {missing}"); return; }
        if (!world.CanPlace(def, cmd.CellX, cmd.CellY, out var reason)) { world.Reject(cmd.Player, reason); return; }
        if (player.Cash < def.Cost) { world.Reject(cmd.Player, "insufficient funds"); return; }

        player.Cash -= def.Cost;
        var site = world.PlaceBuilding(def.Id, cmd.Player, cmd.CellX, cmd.CellY, complete: false);
        builder.BuildTargetId = site.Id;
        builder.RepairTargetId = 0;
        builder.Move = null;
        builder.TargetId = 0;
        builder.HarvestState = HarvestState.Idle;
        world.Emit(new ConstructionStartedEvent(site.Id, cmd.Player));
    }

    /// <summary>Builders standing at a damaged, finished building of their own restore it for free.</summary>
    private static void Repairs(World world)
    {
        var dt = World.Dt;
        foreach (var e in world.Entities) if (e.IsBuilding) e.BeingRepaired = false;
        foreach (var b in world.Entities)
        {
            if (!b.Alive || b.RepairTargetId == 0) continue;
            var target = world.Get(b.RepairTargetId);
            if (target is null || !target.Alive || target.Owner != b.Owner || target.UnderConstruction || target.Hp >= target.MaxHp)
            {
                b.RepairTargetId = 0;
                continue;
            }
            if (World.DistanceToBounds(target, b.Pos) > WorkRange + b.Radius)
            {
                if (b.Move is null || b.Move.Kind != MoveKind.Work || (world.Tick + b.Id) % 20 == 0)
                    b.Move = new MoveOrder { Target = World.ApproachPoint(target, b.Pos, b.Radius + 0.4f), Kind = MoveKind.Work, ArriveRadius = 0.4f };
                continue;
            }
            if (b.Move is { Kind: MoveKind.Work }) b.Move = null;
            b.Facing = Angles.TurnToward(b.Facing, (target.Pos - b.Pos).Angle, Angles.DegToRad(b.Unit!.TurnRate) * dt);
            // No patching a building while it is being shot at; the builder waits for a lull.
            if (world.Tick - target.LastDamagedTick < RepairRules.UnderFireDelay * World.TicksPerSecond) continue;
            target.Hp = MathF.Min(target.MaxHp, target.Hp + target.MaxHp * dt / MathF.Max(RepairRules.MinFullRepairTime, target.Def.BuildTime * 1.5f));
            target.BeingRepaired = true;
        }
    }

    public static void Update(World world)
    {
        var dt = World.Dt;
        Holes(world);
        Repairs(world);
        // Progress contributed by each builder standing at its site.
        foreach (var b in world.Entities)
        {
            if (!b.Alive || b.BuildTargetId == 0) continue;
            var site = world.Get(b.BuildTargetId);
            if (site is null || !site.UnderConstruction || site.Owner != b.Owner)
            {
                b.BuildTargetId = 0;
                continue;
            }
            if (World.DistanceToBounds(site, b.Pos) > WorkRange + b.Radius)
            {
                if (b.Move is null || b.Move.Kind != MoveKind.Work || (world.Tick + b.Id) % 20 == 0)
                    b.Move = new MoveOrder { Target = World.ApproachPoint(site, b.Pos, b.Radius + 0.4f), Kind = MoveKind.Work, ArriveRadius = 0.4f };
                continue;
            }
            if (b.Move is { Kind: MoveKind.Work }) b.Move = null;
            // Face the site while working.
            b.Facing = Angles.TurnToward(b.Facing, (site.Pos - b.Pos).Angle, Angles.DegToRad(b.Unit!.TurnRate) * dt);
            var step = dt / MathF.Max(0.1f, site.Def.BuildTime);
            site.BuildProgress = MathF.Min(1f, site.BuildProgress + step);
            site.Hp = MathF.Min(site.MaxHp, site.Hp + site.MaxHp * 0.9f * step);
            if (site.BuildProgress >= 1f)
            {
                site.UnderConstruction = false;
                site.Hp = site.MaxHp;
                Economy.RecomputePower(world);
                world.Emit(new ConstructionCompletedEvent(site.Id, site.Owner));
                b.BuildTargetId = 0;
            }
        }
    }
}

public static class RepairRules
{
    /// <summary>A lone builder restores a building from nothing in this many seconds, or its build time if longer.</summary>
    public const float MinFullRepairTime = 60f;
    /// <summary>Seconds after the last hit before repair work resumes.</summary>
    public const float UnderFireDelay = 6f;
}

public static class HoleRules
{
    public const float RegrowTime = 60f;
}
