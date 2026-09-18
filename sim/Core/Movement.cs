namespace Overmatch.Sim;

/// <summary>M0 movement: turn toward the target, drive straight, simple separation. Flow fields replace the steering in M1.</summary>
public static class Movement
{
    /// <summary>Units only drive forward once they are within this many radians of their heading.</summary>
    private const float DriveCone = 0.6f;

    public static void Update(Entity e, float dt)
    {
        var order = e.Move;
        if (order is null) return;

        var toTarget = order.Target - e.Pos;
        var dist = toTarget.Length;
        if (dist <= order.ArriveRadius)
        {
            e.Pos = order.Target;
            e.Move = null;
            return;
        }

        var desired = toTarget.Angle;
        var maxTurn = Angles.DegToRad(e.Def.TurnRate) * dt;
        e.Facing = Angles.TurnToward(e.Facing, desired, maxTurn);

        var headingError = MathF.Abs(Angles.Wrap(desired - e.Facing));
        if (headingError > DriveCone) return;

        var step = MathF.Min(e.Def.Speed * dt, dist);
        e.Pos += Vec2.FromAngle(e.Facing) * step;
    }

    /// <summary>Push overlapping units apart. O(n²); fine for M0, spatial hash later.</summary>
    public static void Separate(IReadOnlyList<Entity> entities)
    {
        for (var i = 0; i < entities.Count; i++)
        {
            var a = entities[i];
            if (!a.Alive) continue;
            for (var j = i + 1; j < entities.Count; j++)
            {
                var b = entities[j];
                if (!b.Alive) continue;
                var minDist = a.Radius + b.Radius;
                var delta = b.Pos - a.Pos;
                var d2 = delta.LengthSq;
                if (d2 >= minDist * minDist || d2 < 1e-8f) continue;
                var d = MathF.Sqrt(d2);
                var push = delta / d * ((minDist - d) * 0.5f);
                // A moving unit yields less than a stationary one so groups flow past each other.
                var wa = a.IsMoving ? 0.3f : 0.7f;
                var wb = b.IsMoving ? 0.3f : 0.7f;
                var total = wa + wb;
                a.Pos -= push * (wa / total) * 2f;
                b.Pos += push * (wb / total) * 2f;
            }
        }
    }
}
