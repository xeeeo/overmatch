namespace Overmatch.Sim;

public enum Visibility : byte { Shroud = 0, Explored = 1, Visible = 2 }

/// <summary>Per-player fog of war over the cell grid. Recomputed every few ticks.</summary>
public sealed class VisionMap
{
    public const int UpdateInterval = 4;

    public int Width { get; }
    public int Height { get; }
    public int PlayerCount { get; }
    private readonly byte[][] _state;
    /// <summary>Bumped on every recompute so renderers know when to re-upload.</summary>
    public int Version { get; private set; }

    public VisionMap(int width, int height, int playerCount)
    {
        Width = width;
        Height = height;
        PlayerCount = playerCount;
        _state = new byte[playerCount][];
        for (var p = 0; p < playerCount; p++) _state[p] = new byte[width * height];
    }

    public Visibility Get(int player, int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return Visibility.Shroud;
        return (Visibility)_state[player][y * Width + x];
    }

    public Visibility Get(int player, Vec2 p)
    {
        var (x, y) = MapGrid.CellOf(p);
        return Get(player, x, y);
    }

    public bool IsVisible(int player, Vec2 p) => Get(player, p) == Visibility.Visible;

    /// <summary>Raw per-cell state for player (read-only use by renderers).</summary>
    public ReadOnlySpan<byte> Raw(int player) => _state[player];

    public void Recompute(IReadOnlyList<Entity> entities, IReadOnlyList<Reveal>? reveals = null)
    {
        for (var p = 0; p < PlayerCount; p++)
        {
            var s = _state[p];
            for (var i = 0; i < s.Length; i++) if (s[i] == (byte)Visibility.Visible) s[i] = (byte)Visibility.Explored;
        }
        foreach (var e in entities)
        {
            if (!e.Alive || e.Owner < 0 || e.Owner >= PlayerCount) continue;
            Stamp(_state[e.Owner], e.Pos, e.IsInside ? MathF.Max(e.Def.Vision, 8f) : e.Def.Vision);
        }
        if (reveals is not null)
            foreach (var r in reveals)
                if (r.Player >= 0 && r.Player < PlayerCount) Stamp(_state[r.Player], r.Pos, r.Radius);
        Version++;
    }

    private void Stamp(byte[] s, Vec2 centre, float radius)
    {
        var r2 = radius * radius;
        var x0 = Math.Max(0, (int)MathF.Floor(centre.X - radius));
        var x1 = Math.Min(Width - 1, (int)MathF.Floor(centre.X + radius));
        var y0 = Math.Max(0, (int)MathF.Floor(centre.Y - radius));
        var y1 = Math.Min(Height - 1, (int)MathF.Floor(centre.Y + radius));
        for (var y = y0; y <= y1; y++)
        {
            var dy = y + 0.5f - centre.Y;
            for (var x = x0; x <= x1; x++)
            {
                var dx = x + 0.5f - centre.X;
                if (dx * dx + dy * dy <= r2) s[y * Width + x] = (byte)Visibility.Visible;
            }
        }
    }

    public void RevealAll(int player)
    {
        Array.Fill(_state[player], (byte)Visibility.Visible);
        Version++;
    }
}
