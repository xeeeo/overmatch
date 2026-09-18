namespace Overmatch.Sim;

/// <summary>Cash: harvesting, trickles, selling. Power: supply versus demand per player.</summary>
public static class Economy
{
    public static void RecomputePower(World world)
    {
        foreach (var p in world.Players) { p.PowerSupply = 0; p.PowerDemand = 0; }
        foreach (var e in world.Entities)
        {
            if (!e.Operational || e.Building is not { } b || e.Owner < 0) continue;
            var player = world.Player(e.Owner);
            var power = b.Power + (int)player.PowerAdd(b.Id);
            if (power >= 0) player.PowerSupply += power;
            else player.PowerDemand -= power;
        }
    }

    public static void Update(World world)
    {
        var dt = World.Dt;
        foreach (var e in world.Entities)
        {
            if (!e.Alive) continue;
            if (e.Building is { Trickle: { } trickle } && e.Operational && e.Owner >= 0)
            {
                var player = world.Player(e.Owner);
                e.TrickleTimer -= player.LowPower && e.Building.NeedsPower ? dt * 0.5f : dt;
                if (e.TrickleTimer <= 0f)
                {
                    e.TrickleTimer += trickle.Interval;
                    var amount = trickle.Amount + e.Building.TricklePerPassenger * e.Passengers.Count;
                    player.Cash += (int)(amount * player.IncomeMult);
                }
            }
            else if (e.IsHarvester) Harvest(world, e);
        }
    }

    public static SupplyPile? NearestPile(World world, Vec2 from, float maxDist)
    {
        SupplyPile? best = null;
        var bestD = maxDist * maxDist;
        foreach (var p in world.Piles)
        {
            if (p.Depleted) continue;
            var d = (p.Pos - from).LengthSq;
            if (d < bestD) { bestD = d; best = p; }
        }
        return best;
    }

    public static Entity? NearestSupplyCenter(World world, int owner, Vec2 from)
    {
        Entity? best = null;
        var bestD = float.MaxValue;
        foreach (var e in world.Entities)
        {
            if (e.Owner != owner || !e.Operational || e.Building is not { SupplyCenter: true }) continue;
            var d = (e.Pos - from).LengthSq;
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    /// <summary>A freshly produced harvester starts on the nearest pile to its supply centre.</summary>
    internal static void AutoHarvest(World world, Entity harvester, Entity producer)
    {
        var pile = NearestPile(world, producer.Pos, harvester.Unit!.Harvest!.SearchRadius);
        if (pile is null) return;
        harvester.PileId = pile.Id;
        harvester.HarvestState = HarvestState.ToPile;
        harvester.Move = null;
    }

    private static void Harvest(World world, Entity h)
    {
        var def = h.Unit!.Harvest!;
        var dt = World.Dt;
        switch (h.HarvestState)
        {
            case HarvestState.Idle:
                return;

            case HarvestState.ToPile:
            {
                var pile = world.Pile(h.PileId);
                if (pile is null || pile.Depleted)
                {
                    pile = NearestPile(world, h.Pos, def.SearchRadius);
                    if (pile is null) { h.HarvestState = HarvestState.Idle; return; }
                    h.PileId = pile.Id;
                }
                if ((pile.Pos - h.Pos).Length <= h.Radius + 1.6f)
                {
                    h.Move = null;
                    h.HarvestState = HarvestState.Loading;
                    h.StateTimer = def.LoadTime;
                    return;
                }
                if (h.Move is null || h.Move.Kind != MoveKind.Work)
                    h.Move = new MoveOrder { Target = pile.Pos, Kind = MoveKind.Work, ArriveRadius = h.Radius + 1.2f };
                return;
            }

            case HarvestState.Loading:
            {
                h.StateTimer -= dt;
                if (h.StateTimer > 0f) return;
                var pile = world.Pile(h.PileId);
                var take = pile is null ? 0 : Math.Min(def.Capacity - h.Carried, pile.Remaining);
                if (pile is not null) pile.Remaining -= take;
                h.Carried += take;
                h.HarvestState = h.Carried > 0 ? HarvestState.ToCenter : HarvestState.ToPile;
                return;
            }

            case HarvestState.ToCenter:
            {
                var center = NearestSupplyCenter(world, h.Owner, h.Pos);
                if (center is null) { h.HarvestState = HarvestState.Idle; h.Move = null; return; }
                if (World.DistanceToBounds(center, h.Pos) <= h.Radius + 1.2f)
                {
                    h.Move = null;
                    h.HarvestState = HarvestState.Unloading;
                    h.StateTimer = def.UnloadTime;
                    return;
                }
                if (h.Move is null || h.Move.Kind != MoveKind.Work || (world.Tick + h.Id) % 40 == 0)
                    h.Move = new MoveOrder { Target = World.ApproachPoint(center, h.Pos, h.Radius + 0.5f), Kind = MoveKind.Work, ArriveRadius = 0.5f };
                return;
            }

            case HarvestState.Unloading:
            {
                h.StateTimer -= dt;
                if (h.StateTimer > 0f) return;
                var delivered = (int)(h.Carried * world.Player(h.Owner).IncomeMult);
                world.Player(h.Owner).Cash += delivered;
                world.Emit(new SupplyDeliveredEvent(h.Id, h.Owner, delivered));
                h.Carried = 0;
                var pile = world.Pile(h.PileId);
                if (pile is null || pile.Depleted) pile = NearestPile(world, h.Pos, def.SearchRadius);
                if (pile is null) { h.HarvestState = HarvestState.Idle; return; }
                h.PileId = pile.Id;
                h.HarvestState = HarvestState.ToPile;
                return;
            }
        }
    }

    public static void Sell(World world, SellCommand cmd)
    {
        var b = world.Get(cmd.BuildingId);
        if (b is null || b.Owner != cmd.Player || b.Building is null) return;
        var refund = b.UnderConstruction ? b.Def.Cost : b.Def.Cost / 2;
        Production.RefundAll(world, b);
        world.Player(cmd.Player).Cash += refund;
        b.Alive = false;
        world.Emit(new SoldEvent(b.Id, b.Owner, refund));
    }
}
