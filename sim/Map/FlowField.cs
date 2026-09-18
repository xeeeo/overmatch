namespace Overmatch.Sim;

/// <summary>Direction-to-target for every cell, from one Dijkstra pass out of the target cell. Shared by every unit heading to that cell.</summary>
public sealed class FlowField
{
    public int TargetX { get; }
    public int TargetY { get; }
    public Locomotor Locomotor { get; }
    public int GridVersion { get; }
    public float[] Dist { get; }
    public Vec2[] Dir { get; }

    private readonly int _w;
    private readonly int _h;
    private readonly MapGrid _grid;

    private FlowField(MapGrid grid, int tx, int ty, Locomotor loco)
    {
        _grid = grid;
        _w = grid.Width;
        _h = grid.Height;
        TargetX = tx;
        TargetY = ty;
        Locomotor = loco;
        GridVersion = grid.Version;
        Dist = new float[_w * _h];
        Dir = new Vec2[_w * _h];
    }

    private static readonly (int dx, int dy, float cost)[] Neighbours =
    {
        (1, 0, 1f), (-1, 0, 1f), (0, 1, 1f), (0, -1, 1f),
        (1, 1, 1.41421f), (-1, 1, 1.41421f), (1, -1, 1.41421f), (-1, -1, 1.41421f),
    };

    public static FlowField Build(MapGrid grid, int tx, int ty, Locomotor loco)
    {
        var f = new FlowField(grid, tx, ty, loco);
        Array.Fill(f.Dist, float.PositiveInfinity);
        if (!grid.IsPassable(tx, ty, loco)) return f;

        var pq = new PriorityQueue<int, float>();
        var start = ty * f._w + tx;
        f.Dist[start] = 0f;
        pq.Enqueue(start, 0f);

        while (pq.TryDequeue(out var idx, out var d))
        {
            if (d > f.Dist[idx]) continue;
            var x = idx % f._w;
            var y = idx / f._w;
            foreach (var (dx, dy, cost) in Neighbours)
            {
                var nx = x + dx;
                var ny = y + dy;
                if (!grid.IsPassable(nx, ny, loco)) continue;
                // No corner cutting: a diagonal step needs both orthogonal neighbours free.
                if (dx != 0 && dy != 0 && (!grid.IsPassable(x + dx, y, loco) || !grid.IsPassable(x, y + dy, loco))) continue;
                var nIdx = ny * f._w + nx;
                var nd = d + cost * grid.Cost(nx, ny, loco);
                if (nd < f.Dist[nIdx])
                {
                    f.Dist[nIdx] = nd;
                    pq.Enqueue(nIdx, nd);
                }
            }
        }

        // Direction = toward the reachable neighbour with the lowest distance.
        for (var y = 0; y < f._h; y++)
            for (var x = 0; x < f._w; x++)
            {
                var idx = y * f._w + x;
                if (float.IsInfinity(f.Dist[idx]) || (x == tx && y == ty)) continue;
                var best = f.Dist[idx];
                var bestDir = Vec2.Zero;
                foreach (var (dx, dy, _) in Neighbours)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (!grid.InBounds(nx, ny)) continue;
                    if (dx != 0 && dy != 0 && (!grid.IsPassable(x + dx, y, loco) || !grid.IsPassable(x, y + dy, loco))) continue;
                    var nd = f.Dist[ny * f._w + nx];
                    if (nd < best) { best = nd; bestDir = new Vec2(dx, dy); }
                }
                f.Dir[idx] = bestDir.Normalized;
            }
        return f;
    }

    public bool Reachable(int x, int y) => _grid.InBounds(x, y) && !float.IsInfinity(Dist[y * _w + x]);

    /// <summary>Bilinear blend of the four surrounding cell directions, ignoring unreachable cells. Smooths out grid zig-zag.</summary>
    public Vec2 Sample(Vec2 p)
    {
        var fx = p.X - 0.5f;
        var fy = p.Y - 0.5f;
        var x0 = (int)MathF.Floor(fx);
        var y0 = (int)MathF.Floor(fy);
        var tx = fx - x0;
        var ty = fy - y0;
        var sum = Vec2.Zero;
        Accumulate(x0, y0, (1 - tx) * (1 - ty), ref sum);
        Accumulate(x0 + 1, y0, tx * (1 - ty), ref sum);
        Accumulate(x0, y0 + 1, (1 - tx) * ty, ref sum);
        Accumulate(x0 + 1, y0 + 1, tx * ty, ref sum);
        if (sum.LengthSq > 1e-6f) return sum.Normalized;
        var (cx, cy) = MapGrid.CellOf(p);
        return Reachable(cx, cy) ? Dir[cy * _w + cx] : Vec2.Zero;
    }

    private void Accumulate(int x, int y, float w, ref Vec2 sum)
    {
        if (w <= 0f || !Reachable(x, y)) return;
        sum += Dir[y * _w + x] * w;
    }
}

/// <summary>Keeps recently used flow fields; rebuilt when the grid changes.</summary>
public sealed class FlowFieldCache
{
    private readonly MapGrid _grid;
    private readonly int _capacity;
    private readonly Dictionary<(int, int, Locomotor), LinkedListNode<FlowField>> _map = new();
    private readonly LinkedList<FlowField> _lru = new();

    public int Builds { get; private set; }

    public FlowFieldCache(MapGrid grid, int capacity = 128)
    {
        _grid = grid;
        _capacity = capacity;
    }

    /// <summary>Searches are whole-map; this many cells' worth may be searched per tick before callers are asked to wait.</summary>
    public int CellBudgetPerTick { get; set; } = 60_000;
    private int _spent;

    public void BeginTick() => _spent = 0;

    /// <summary>The field if it is cached or there is budget left this tick; otherwise null, and the caller tries again next tick.</summary>
    public FlowField? TryGet(int tx, int ty, Locomotor loco)
    {
        if (_map.TryGetValue((tx, ty, loco), out var node) && node.Value.GridVersion == _grid.Version) return Get(tx, ty, loco);
        if (_spent > 0 && _spent + _grid.Width * _grid.Height > CellBudgetPerTick) return null;
        _spent += _grid.Width * _grid.Height;
        return Get(tx, ty, loco);
    }

    public FlowField Get(int tx, int ty, Locomotor loco)
    {
        var key = (tx, ty, loco);
        if (_map.TryGetValue(key, out var node))
        {
            if (node.Value.GridVersion == _grid.Version)
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                return node.Value;
            }
            _lru.Remove(node);
            _map.Remove(key);
        }
        var field = FlowField.Build(_grid, tx, ty, loco);
        Builds++;
        var n = _lru.AddFirst(field);
        _map[key] = n;
        if (_lru.Count > _capacity)
        {
            var last = _lru.Last!;
            _map.Remove((last.Value.TargetX, last.Value.TargetY, last.Value.Locomotor));
            _lru.RemoveLast();
        }
        return field;
    }
}
