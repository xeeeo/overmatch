using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>Per-building production queues for units and upgrades.</summary>
public static class Production
{
    public static void Enqueue(World world, ProduceCommand cmd)
    {
        var building = world.Get(cmd.BuildingId);
        if (building is null || building.Owner != cmd.Player || !building.Operational || building.Queue is null) { world.Reject(cmd.Player, "no producer"); return; }
        var def = building.Building!;
        var player = world.Player(cmd.Player);
        var queue = building.Queue;
        if (queue.Items.Count >= ProductionQueue.MaxItems) { world.Reject(cmd.Player, "queue full"); return; }

        if (def.Produces.Contains(cmd.ItemId))
        {
            var unit = world.Rules.Unit(cmd.ItemId);
            if (!world.HasPrereqs(cmd.Player, unit.Prereqs, out var missing)) { world.Reject(cmd.Player, $"requires {missing}"); return; }
            if (unit.Ammo > 0)
            {
                // Jets need a free pad.
                var pads = world.Entities.Where(e => e.Owner == cmd.Player && e.Operational && e.Building is { Pads: > 0 }).Sum(e => e.Building!.Pads);
                var jets = world.Entities.Count(e => e.Owner == cmd.Player && e.Alive && e.Unit is { Ammo: > 0 })
                           + world.Entities.Where(e => e.Owner == cmd.Player && e.Queue is not null).Sum(e => e.Queue!.Items.Count(i => !i.IsUpgrade && world.Rules.Units.TryGetValue(i.Id, out var d) && d.Ammo > 0));
                if (jets >= pads) { world.Reject(cmd.Player, "no free airfield pad"); return; }
            }
            var cost = (int)(unit.Cost * player.DiscountMult);
            if (player.Cash < cost) { world.Reject(cmd.Player, "insufficient funds"); return; }
            player.Cash -= cost;
            queue.Items.Add(new QueueItem { Id = unit.Id, Cost = cost, Time = unit.BuildTime });
        }
        else if (def.Upgrades.Contains(cmd.ItemId))
        {
            var up = world.Rules.Upgrade(cmd.ItemId);
            if (player.Has(up.Id)) { world.Reject(cmd.Player, "already researched"); return; }
            if (queue.Items.Any(i => i.IsUpgrade && i.Id == up.Id) || IsQueuedAnywhere(world, cmd.Player, up.Id)) { world.Reject(cmd.Player, "already in progress"); return; }
            if (!world.HasPrereqs(cmd.Player, up.Prereqs, out var missing)) { world.Reject(cmd.Player, $"requires {missing}"); return; }
            if (player.Cash < up.Cost) { world.Reject(cmd.Player, "insufficient funds"); return; }
            player.Cash -= up.Cost;
            queue.Items.Add(new QueueItem { Id = up.Id, IsUpgrade = true, Cost = up.Cost, Time = up.Time });
        }
        else world.Reject(cmd.Player, "cannot build that here");
    }

    private static bool IsQueuedAnywhere(World world, int player, string upgradeId)
    {
        foreach (var e in world.Entities)
            if (e.Owner == player && e.Queue is not null && e.Queue.Items.Any(i => i.IsUpgrade && i.Id == upgradeId)) return true;
        return false;
    }

    public static void Cancel(World world, CancelProduceCommand cmd)
    {
        var building = world.Get(cmd.BuildingId);
        if (building?.Queue is null || building.Owner != cmd.Player) return;
        var items = building.Queue.Items;
        if (cmd.Index < 0 || cmd.Index >= items.Count) return;
        world.Player(cmd.Player).Cash += items[cmd.Index].Cost;
        items.RemoveAt(cmd.Index);
    }

    /// <summary>Refund everything queued (building sold or destroyed).</summary>
    internal static void RefundAll(World world, Entity building)
    {
        if (building.Queue is null) return;
        var player = world.Player(building.Owner);
        foreach (var item in building.Queue.Items) player.Cash += item.Cost;
        building.Queue.Items.Clear();
    }

    public static void Update(World world)
    {
        // Index loop: completing a unit appends to the entity list.
        var count = world.Entities.Count;
        for (var i = 0; i < count; i++)
        {
            var b = world.Entities[i];
            if (!b.Operational || b.Queue?.Head is not { } item) continue;
            var player = world.Player(b.Owner);
            var rate = World.Dt / MathF.Max(0.1f, item.Time);
            if (player.LowPower) rate *= 0.5f;
            item.Progress += rate;
            if (item.Progress < 1f) continue;

            b.Queue.Items.RemoveAt(0);
            if (item.IsUpgrade)
            {
                var up = world.Rules.Upgrade(item.Id);
                player.GrantUpgrade(up);
                ApplyHpUpgrade(world, player, up);
                Economy.RecomputePower(world);
                world.Emit(new UpgradeCompletedEvent(player.Id, up.Id));
            }
            else
            {
                var def = world.Rules.Unit(item.Id);
                var at = world.ExitPoint(b, def.LocomotorClass);
                var unit = world.SpawnProduced(def, b.Owner, at, -MathF.PI / 2f);
                var rally = b.Rally ?? new Vec2(b.Pos.X, b.Bounds.y0 - 4f);
                unit.Move = new MoveOrder { Target = world.ClampToMap(rally), Kind = MoveKind.Move, ArriveRadius = 0.6f };
                if (unit.IsHarvester) Economy.AutoHarvest(world, unit, b);
                world.Emit(new ProductionCompletedEvent(b.Id, b.Owner, item.Id, unit.Id));
            }
        }
    }

    /// <summary>Existing units get the new max HP, keeping their damage fraction.</summary>
    private static void ApplyHpUpgrade(World world, Player player, UpgradeDef up)
    {
        if (!up.Effects.Any(f => f.Type == "hp")) return;
        foreach (var e in world.Entities)
        {
            if (e.Owner != player.Id || !e.Alive) continue;
            var newMax = e.Def.Hp * player.HpMult(e.Def);
            if (MathF.Abs(newMax - e.MaxHp) < 0.01f) continue;
            var frac = e.HpFraction;
            e.MaxHp = newMax;
            e.Hp = newMax * frac;
        }
    }
}
