using Overmatch.Sim.Data;

namespace Overmatch.Sim;

public enum CellType : byte { Ground, Road, Rough, Water, Cliff, Structure }

/// <summary>The cell grid under the map. Cells are 1×1 world units; cell (x, y) covers [x, x+1) × [y, y+1).</summary>
public sealed class MapGrid
{
    public int Width { get; }
    public int Height { get; }
    /// <summary>Bumped whenever passability changes; flow fields cache against it.</summary>
    public int Version { get; private set; }

    private readonly CellType[] _cells;

    public MapGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new CellType[width * height];
    }

    public static MapGrid FromDef(MapDef def)
    {
        var g = new MapGrid(def.Width, def.Height);
        foreach (var r in def.Road) g.Fill(r, CellType.Road);
        foreach (var r in def.Rough) g.Fill(r, CellType.Rough);
        foreach (var r in def.Water) g.Fill(r, CellType.Water);
        foreach (var r in def.Blocked) g.Fill(r, CellType.Cliff);
        return g;
    }

    public void Fill(RectDef r, CellType t)
    {
        for (var y = r.Y; y < r.Y + r.H; y++)
            for (var x = r.X; x < r.X + r.W; x++)
                if (InBounds(x, y)) _cells[y * Width + x] = t;
        Version++;
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    public CellType Get(int x, int y) => InBounds(x, y) ? _cells[y * Width + x] : CellType.Cliff;

    public void Set(int x, int y, CellType t)
    {
        if (!InBounds(x, y)) return;
        _cells[y * Width + x] = t;
        Version++;
    }

    public static (int x, int y) CellOf(Vec2 p) => ((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y));
    public static Vec2 Centre(int x, int y) => new(x + 0.5f, y + 0.5f);

    public bool IsPassable(int x, int y, Locomotor loco)
    {
        if (!InBounds(x, y)) return false;
        if (loco == Locomotor.Air) return true;
        return _cells[y * Width + x] switch
        {
            CellType.Ground or CellType.Road or CellType.Rough => true,
            CellType.Water => loco == Locomotor.Hover,
            _ => false,
        };
    }

    public bool IsPassable(Vec2 p, Locomotor loco)
    {
        var (x, y) = CellOf(p);
        return IsPassable(x, y, loco);
    }

    /// <summary>Movement cost multiplier for entering a cell (1 = normal).</summary>
    public float Cost(int x, int y, Locomotor loco)
    {
        var t = _cells[y * Width + x];
        return t switch
        {
            CellType.Road => loco is Locomotor.Wheeled or Locomotor.Tracked ? 0.8f : 1f,
            CellType.Rough => loco switch { Locomotor.Infantry => 1.2f, Locomotor.Wheeled => 2.5f, Locomotor.Tracked => 1.6f, _ => 1f },
            _ => 1f,
        };
    }

    /// <summary>Nearest passable cell to (x, y) by ring search, or null if none within maxRadius.</summary>
    public (int x, int y)? NearestPassable(int x, int y, Locomotor loco, int maxRadius = 12)
    {
        if (IsPassable(x, y, loco)) return (x, y);
        for (var r = 1; r <= maxRadius; r++)
        {
            var best = ((int x, int y)?)null;
            var bestD = float.MaxValue;
            for (var dy = -r; dy <= r; dy++)
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                    var cx = x + dx;
                    var cy = y + dy;
                    if (!IsPassable(cx, cy, loco)) continue;
                    var d = dx * dx + dy * dy;
                    if (d < bestD) { bestD = d; best = (cx, cy); }
                }
            if (best is not null) return best;
        }
        return null;
    }

    /// <summary>True if the straight segment a→b crosses only passable cells (grid DDA).</summary>
    public bool LineClear(Vec2 a, Vec2 b, Locomotor loco)
    {
        if (loco == Locomotor.Air) return true;
        var (x, y) = CellOf(a);
        var (ex, ey) = CellOf(b);
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var stepX = dx > 0 ? 1 : -1;
        var stepY = dy > 0 ? 1 : -1;
        var tDeltaX = dx == 0 ? float.MaxValue : MathF.Abs(1f / dx);
        var tDeltaY = dy == 0 ? float.MaxValue : MathF.Abs(1f / dy);
        var tMaxX = dx == 0 ? float.MaxValue : (dx > 0 ? (x + 1 - a.X) : (a.X - x)) * tDeltaX;
        var tMaxY = dy == 0 ? float.MaxValue : (dy > 0 ? (y + 1 - a.Y) : (a.Y - y)) * tDeltaY;

        for (var guard = 0; guard < Width + Height + 2; guard++)
        {
            if (!IsPassable(x, y, loco)) return false;
            if (x == ex && y == ey) return true;
            if (tMaxX < tMaxY) { tMaxX += tDeltaX; x += stepX; }
            else { tMaxY += tDeltaY; y += stepY; }
        }
        return false;
    }
}
