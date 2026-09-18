namespace Overmatch.Sim;

/// <summary>Uniform grid bucket index over entities, rebuilt every tick. Cell size is a few world units.</summary>
public sealed class SpatialHash
{
    private readonly float _cell;
    private readonly Dictionary<long, List<Entity>> _buckets = new();

    public SpatialHash(float cellSize = 4f)
    {
        _cell = cellSize;
    }

    private long Key(int x, int y) => ((long)x << 32) ^ (uint)y;

    public void Rebuild(IReadOnlyList<Entity> entities)
    {
        foreach (var b in _buckets.Values) b.Clear();
        foreach (var e in entities)
        {
            if (!e.Alive) continue;
            var k = Key((int)MathF.Floor(e.Pos.X / _cell), (int)MathF.Floor(e.Pos.Y / _cell));
            if (!_buckets.TryGetValue(k, out var list)) _buckets[k] = list = new List<Entity>();
            list.Add(e);
        }
    }

    /// <summary>Entities within radius of p. Returns a fresh list so nested queries are safe.</summary>
    public List<Entity> Query(Vec2 p, float radius)
    {
        var _scratch = new List<Entity>();
        var x0 = (int)MathF.Floor((p.X - radius) / _cell);
        var x1 = (int)MathF.Floor((p.X + radius) / _cell);
        var y0 = (int)MathF.Floor((p.Y - radius) / _cell);
        var y1 = (int)MathF.Floor((p.Y + radius) / _cell);
        var r2 = radius * radius;
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                if (!_buckets.TryGetValue(Key(x, y), out var list)) continue;
                foreach (var e in list)
                    if (e.Alive && (e.Pos - p).LengthSq <= r2) _scratch.Add(e);
            }
        return _scratch;
    }
}
