namespace Overmatch.Sim;

/// <summary>Infantry inside buildings and transports, the shared tunnel network, and capturing neutral buildings.</summary>
public static class Garrison
{
    public const float CaptureTime = 12f;

    public static bool CanEnter(Entity unit, Entity container)
    {
        if (!unit.Alive || !container.Alive || unit.IsBuilding || !unit.Def.IsInfantry) return false;
        if (container.Def.GarrisonSlots <= 0) return false;
        if (container.Owner >= 0 && container.Owner != unit.Owner) return false;
        if (container.Building is { TunnelHub: true }) return true;
        return container.Passengers.Count < container.Def.GarrisonSlots;
    }

    public static void Update(World world)
    {
        foreach (var u in world.Entities)
        {
            if (!u.Alive || u.IsInside) continue;
            if (u.EnterTargetId != 0) Approach(world, u);
            else if (u.CaptureTargetId != 0) Capture(world, u);
        }
    }

    private static void Approach(World world, Entity u)
    {
        var c = world.Get(u.EnterTargetId);
        if (c is null || !CanEnter(u, c)) { u.EnterTargetId = 0; u.Move = null; return; }
        var margin = u.Radius + 0.6f;
        var dist = c.IsBuilding ? World.DistanceToBounds(c, u.Pos) : (c.Pos - u.Pos).Length - c.Radius;
        if (dist <= margin + 0.3f)
        {
            Enter(world, u, c);
            return;
        }
        if (u.Move is null || u.Move.Kind != MoveKind.Work || (world.Tick + u.Id) % 20 == 0)
        {
            var target = c.IsBuilding ? World.ApproachPoint(c, u.Pos, margin) : c.Pos;
            u.Move = new MoveOrder { Target = target, Kind = MoveKind.Work, ArriveRadius = 0.3f };
        }
    }

    public static void Enter(World world, Entity u, Entity c)
    {
        u.EnterTargetId = 0;
        u.Move = null;
        u.SuspendedMove = null;
        u.TargetId = 0;
        u.InsideId = c.Id;
        u.Pos = c.Pos;
        u.PrevPos = c.Pos;
        if (c.Building is { TunnelHub: true }) world.Player(u.Owner).TunnelPool.Add(u.Id);
        else c.Passengers.Add(u.Id);
        world.Emit(new GarrisonEvent(c.Id, u.Id, true));
    }

    /// <summary>Everyone out at this container. Tunnels release the player's whole pool here.</summary>
    public static void Exit(World world, Entity c, int player)
    {
        List<int> ids;
        if (c.Building is { TunnelHub: true }) { ids = new List<int>(world.Player(player).TunnelPool); world.Player(player).TunnelPool.Clear(); }
        else { ids = new List<int>(c.Passengers); c.Passengers.Clear(); }
        var n = 0;
        foreach (var id in ids)
        {
            var u = world.Get(id);
            if (u is null) continue;
            var (x0, y0, x1, _) = c.Bounds;
            var want = c.IsBuilding ? new Vec2(x0 + (n % Math.Max(1, (int)(x1 - x0))) + 0.5f, y0 - 1.2f) : c.Pos + Vec2.FromAngle(n * 1.1f) * (c.Radius + 0.8f);
            var (cx, cy) = MapGrid.CellOf(want);
            var free = world.Grid.NearestPassable(cx, cy, Locomotor.Infantry, 6);
            u.Pos = free is null ? want : MapGrid.Centre(free.Value.x, free.Value.y) + new Vec2((n % 3) * 0.3f, (n / 3) * 0.3f);
            u.PrevPos = u.Pos;
            u.InsideId = 0;
            n++;
            world.Emit(new GarrisonEvent(c.Id, u.Id, false));
        }
    }

    /// <summary>Container destroyed: occupants die with it (transports and tunnels too, if it was the last tunnel).</summary>
    public static void ContainerDied(World world, Entity c)
    {
        foreach (var id in c.Passengers.ToArray())
            if (world.Get(id) is { } u) Combat.Kill(world, u, null, silent: true);
        c.Passengers.Clear();
        if (c.Building is { TunnelHub: true } && c.Owner >= 0)
        {
            var others = world.Entities.Any(e => e.Alive && e.Id != c.Id && e.Owner == c.Owner && e.Building is { TunnelHub: true });
            if (!others)
            {
                foreach (var id in world.Player(c.Owner).TunnelPool.ToArray())
                    if (world.Get(id) is { } u) Combat.Kill(world, u, null, silent: true);
                world.Player(c.Owner).TunnelPool.Clear();
            }
        }
    }

    private static void Capture(World world, Entity u)
    {
        var b = world.Get(u.CaptureTargetId);
        if (b is null || b.Building is not { Capturable: true } || b.Owner == u.Owner) { u.CaptureTargetId = 0; u.CaptureProgress = 0f; u.Move = null; return; }
        var margin = u.Radius + 0.8f;
        if (World.DistanceToBounds(b, u.Pos) > margin + 0.3f)
        {
            if (u.Move is null || u.Move.Kind != MoveKind.Work || (world.Tick + u.Id) % 20 == 0)
                u.Move = new MoveOrder { Target = World.ApproachPoint(b, u.Pos, margin), Kind = MoveKind.Work, ArriveRadius = 0.3f };
            return;
        }
        u.Move = null;
        u.CaptureProgress += World.Dt / CaptureTime;
        if (u.CaptureProgress < 1f) return;
        var old = b.Owner;
        b.Owner = u.Owner;
        b.Rally = null;
        if (b.Queue is not null) b.Queue.Items.Clear();
        b.TargetId = 0;
        foreach (var other in world.Entities) if (other.CaptureTargetId == b.Id) { other.CaptureTargetId = 0; other.CaptureProgress = 0f; }
        Economy.RecomputePower(world);
        world.Emit(new CapturedEvent(b.Id, old, b.Owner));
    }
}
