namespace Overmatch.Sim;

/// <summary>Flow-field steering with hull turning, cell collision, separation and stuck detection.</summary>
public static class Movement
{
    /// <summary>Units only drive forward once they are within this many radians of their heading.</summary>
    private const float DriveCone = 0.7f;
    /// <summary>Within this distance of the goal, steer straight at it if the line is clear.</summary>
    private const float DirectRange = 2.5f;
    private const int StuckLimitTicks = World.TicksPerSecond * 3;

    public static void Update(Entity e, World world)
    {
        var order = e.Move;
        if (order is null || e.Unit is not { } unit) return;

        var dt = World.Dt;
        var loco = unit.LocomotorClass;
        var grid = world.Grid;

        var toTarget = order.Target - e.Pos;
        var dist = toTarget.Length;
        if (dist <= order.ArriveRadius)
        {
            e.Pos = order.Target;
            Arrive(e);
            return;
        }

        // Resolve the flow field lazily; snap the goal to the nearest passable cell.
        if (order.Field is null && loco != Locomotor.Air)
        {
            var (tx, ty) = MapGrid.CellOf(order.Target);
            var goal = grid.NearestPassable(tx, ty, loco);
            if (goal is null) { Arrive(e); return; }
            if (goal.Value != (tx, ty)) order.Target = MapGrid.Centre(goal.Value.x, goal.Value.y);
            order.Field = world.Fields.Get(goal.Value.x, goal.Value.y, loco);
            toTarget = order.Target - e.Pos;
            dist = toTarget.Length;
        }

        Vec2 desired;
        if (loco == Locomotor.Air || (dist < DirectRange && grid.LineClear(e.Pos, order.Target, loco)))
            desired = toTarget.Normalized;
        else
        {
            desired = order.Field!.Sample(e.Pos);
            if (desired == Vec2.Zero)
            {
                // Unreachable from here (e.g. pushed into a pocket): give up cleanly.
                if (!order.Field.Reachable(MapGrid.CellOf(e.Pos).x, MapGrid.CellOf(e.Pos).y)) { Arrive(e); return; }
                desired = toTarget.Normalized;
            }
        }

        var desiredAngle = desired.Angle;
        var maxTurn = Angles.DegToRad(unit.TurnRate) * dt;
        e.Facing = Angles.TurnToward(e.Facing, desiredAngle, maxTurn);
        if (!e.Def.HasTurret) e.TurretFacing = e.Facing;

        var headingError = MathF.Abs(Angles.Wrap(desiredAngle - e.Facing));
        if (headingError <= DriveCone || loco == Locomotor.Air)
        {
            var speed = unit.Speed * world.Player(e.Owner).SpeedMult(unit);
            var step = MathF.Min(speed * dt, dist);
            var next = e.Pos + Vec2.FromAngle(e.Facing) * step;
            if (grid.IsPassable(next, loco)) e.Pos = next;
            else if (grid.IsPassable(new Vec2(next.X, e.Pos.Y), loco)) e.Pos = new Vec2(next.X, e.Pos.Y);
            else if (grid.IsPassable(new Vec2(e.Pos.X, next.Y), loco)) e.Pos = new Vec2(e.Pos.X, next.Y);
        }

        // Stuck detection: no progress toward the goal for a while means something is in the way for good.
        var now = (order.Target - e.Pos).Length;
        if (now < order.BestDist - 0.01f) { order.BestDist = now; order.StuckTicks = 0; }
        else if (++order.StuckTicks > StuckLimitTicks) Arrive(e);
    }

    private static void Arrive(Entity e)
    {
        e.Move = null;
    }

    /// <summary>Push overlapping units apart, then keep everyone out of impassable cells.</summary>
    public static void Separate(World world)
    {
        var entities = world.Entities;
        var grid = world.Grid;
        foreach (var a in entities)
        {
            if (!a.Alive || a.Def.IsAir || a.IsBuilding) continue;
            var near = world.Spatial.Query(a.Pos, a.Radius + 2f);
            foreach (var b in near)
            {
                if (b.Id <= a.Id || !b.Alive || b.Def.IsAir || b.IsBuilding) continue;
                var minDist = a.Radius + b.Radius;
                var delta = b.Pos - a.Pos;
                var d2 = delta.LengthSq;
                if (d2 >= minDist * minDist) continue;
                Vec2 dir;
                float d;
                if (d2 < 1e-8f)
                {
                    // Exactly overlapping: pick a deterministic direction from the ids.
                    dir = Vec2.FromAngle((a.Id * 0.7f + b.Id * 1.3f) % Angles.Tau);
                    d = 0f;
                }
                else
                {
                    d = MathF.Sqrt(d2);
                    dir = delta / d;
                }
                var overlap = minDist - d;
                // A moving unit yields less than a stationary one so groups flow past parked units.
                var wa = a.IsMoving ? 0.35f : 0.65f;
                var wb = b.IsMoving ? 0.35f : 0.65f;
                var total = wa + wb;
                a.Pos -= dir * (overlap * (wa / total));
                b.Pos += dir * (overlap * (wb / total));
            }
        }

        foreach (var e in entities)
        {
            if (!e.Alive || e.Def.IsAir || e.Unit is not { } u) continue;
            var loco = u.LocomotorClass;
            if (grid.IsPassable(e.Pos, loco)) continue;
            // Pushed into a wall: fall back to where we were at the start of the tick, else nearest free cell centre.
            if (grid.IsPassable(e.PrevPos, loco)) { e.Pos = e.PrevPos; continue; }
            var (cx, cy) = MapGrid.CellOf(e.Pos);
            var free = grid.NearestPassable(cx, cy, loco);
            if (free is not null) e.Pos = MapGrid.Centre(free.Value.x, free.Value.y);
        }
    }
}
