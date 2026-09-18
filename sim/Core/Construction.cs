namespace Overmatch.Sim;

/// <summary>Builders place and raise buildings. Cost is charged at placement; construction needs a builder standing next to the site.</summary>
public static class Construction
{
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
        builder.Move = null;
        builder.TargetId = 0;
        builder.HarvestState = HarvestState.Idle;
        world.Emit(new ConstructionStartedEvent(site.Id, cmd.Player));
    }

    public static void Update(World world)
    {
        var dt = World.Dt;
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
