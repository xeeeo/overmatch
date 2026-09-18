using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Radar view: terrain, fog, units, buildings, the camera frustum and attack pings. Click to look, right-click to order.</summary>
public partial class Minimap : Control
{
    public GameRoot Root { get; set; } = null!;

    private ImageTexture _terrain = null!;
    private ImageTexture _fog = null!;
    private Image _fogImg = null!;
    private byte[] _fogBytes = Array.Empty<byte>();
    private int _fogVersion = -1;
    private bool _dragging;
    private readonly List<(Vec2 pos, float ttl)> _pings = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var grid = Root.World.Grid;
        var img = Image.CreateEmpty(grid.Width, grid.Height, false, Image.Format.Rgb8);
        for (var y = 0; y < grid.Height; y++)
            for (var x = 0; x < grid.Width; x++)
                img.SetPixel(x, grid.Height - 1 - y, grid.Get(x, y) switch
                {
                    CellType.Road => new Color(0.42f, 0.40f, 0.36f),
                    CellType.Rough => new Color(0.40f, 0.34f, 0.23f),
                    CellType.Water => new Color(0.18f, 0.36f, 0.52f),
                    CellType.Cliff => new Color(0.24f, 0.24f, 0.23f),
                    _ => new Color(0.30f, 0.36f, 0.20f),
                });
        _terrain = ImageTexture.CreateFromImage(img);
        _fogBytes = new byte[grid.Width * grid.Height * 4];
        _fogImg = Image.CreateFromData(grid.Width, grid.Height, false, Image.Format.Rgba8, _fogBytes);
        _fog = ImageTexture.CreateFromImage(_fogImg);
    }

    public void Ping(Vec2 at)
    {
        if (_pings.Any(p => Vec2.Distance(p.pos, at) < 8f)) return;
        _pings.Add((at, 4f));
    }

    private Rect2 MapRect()
    {
        var grid = Root.World.Grid;
        var scale = MathF.Min(Size.X / grid.Width, Size.Y / grid.Height);
        var sz = new Vector2(grid.Width * scale, grid.Height * scale);
        return new Rect2((Size - sz) / 2f, sz);
    }

    private Vector2 ToMini(Vec2 p)
    {
        var r = MapRect();
        var grid = Root.World.Grid;
        return r.Position + new Vector2(p.X / grid.Width * r.Size.X, (1f - p.Y / grid.Height) * r.Size.Y);
    }

    private Vec2 ToSim(Vector2 local)
    {
        var r = MapRect();
        var grid = Root.World.Grid;
        var u = Mathf.Clamp((local.X - r.Position.X) / r.Size.X, 0f, 1f);
        var v = Mathf.Clamp((local.Y - r.Position.Y) / r.Size.Y, 0f, 1f);
        return new Vec2(u * grid.Width, (1f - v) * grid.Height);
    }

    public override void _Process(double delta)
    {
        for (var i = _pings.Count - 1; i >= 0; i--)
        {
            _pings[i] = (_pings[i].pos, _pings[i].ttl - (float)delta);
            if (_pings[i].ttl <= 0) _pings.RemoveAt(i);
        }
        var vision = Root.World.Vision;
        if (vision.Version != _fogVersion)
        {
            _fogVersion = vision.Version;
            var grid = Root.World.Grid;
            var raw = vision.Raw(Root.LocalPlayer);
            for (var y = 0; y < grid.Height; y++)
                for (var x = 0; x < grid.Width; x++)
                {
                    var i = ((grid.Height - 1 - y) * grid.Width + x) * 4;
                    _fogBytes[i + 3] = (Visibility)raw[y * grid.Width + x] switch { Visibility.Visible => 0, Visibility.Explored => 130, _ => 245 };
                }
            _fogImg.SetData(grid.Width, grid.Height, false, Image.Format.Rgba8, _fogBytes);
            _fog.Update(_fogImg);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var r = MapRect();
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.03f, 0.04f, 0.04f));
        DrawTextureRect(_terrain, r, false);
        var w = Root.World;
        var scale = r.Size.X / w.Grid.Width;

        foreach (var pile in w.Piles)
            if (!pile.Depleted && w.Vision.Get(Root.LocalPlayer, pile.Pos) != Visibility.Shroud)
                DrawRect(new Rect2(ToMini(pile.Pos) - Vector2.One * 1.5f, Vector2.One * 3f), new Color(0.95f, 0.8f, 0.25f));

        foreach (var e in w.Entities)
        {
            if (!e.Alive || e.IsInside) continue;
            var mine = e.Owner == Root.LocalPlayer;
            if (!mine && !(w.CanSee(Root.LocalPlayer, e) || (e.IsBuilding && !e.Def.Stealth && w.Vision.Get(Root.LocalPlayer, e.Pos) == Visibility.Explored))) continue;
            var colour = e.Owner < 0 ? new Color(0.75f, 0.75f, 0.75f) : Root.PlayerColour(e.Owner).Lightened(0.15f);
            if (e.IsBuilding)
            {
                var (x0, y0, x1, y1) = e.Bounds;
                var a = ToMini(new Vec2(x0, y1));
                DrawRect(new Rect2(a, new Vector2((x1 - x0) * scale, (y1 - y0) * scale)), colour);
            }
            else DrawRect(new Rect2(ToMini(e.Pos) - Vector2.One * 1.2f, Vector2.One * 2.4f), colour);
        }

        DrawTextureRect(_fog, r, false);

        // Camera footprint on the ground.
        var vp = GetViewport().GetVisibleRect().Size;
        var corners = new[] { new Vector2(0, 0), new Vector2(vp.X, 0), new Vector2(vp.X, vp.Y - Hud.BarHeight), new Vector2(0, vp.Y - Hud.BarHeight) };
        var pts = new List<Vector2>();
        foreach (var c in corners)
            if (Root.Camera.GroundPoint(c) is { } g) pts.Add(ToMini(MapView.ToSim(g)));
        if (pts.Count == 4)
        {
            pts.Add(pts[0]);
            DrawPolyline(pts.ToArray(), new Color(1, 1, 1, 0.9f), 1.2f);
        }

        foreach (var (pos, ttl) in _pings)
        {
            var phase = 1f - (ttl % 1f);
            DrawArc(ToMini(pos), 3f + phase * 9f, 0, Mathf.Tau, 20, new Color(1f, 0.3f, 0.25f, 1f - phase), 2f);
        }
        DrawRect(r, new Color(0.7f, 0.75f, 0.7f, 0.6f), false, 1f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb:
                _dragging = mb.Pressed;
                if (mb.Pressed) Look(mb.Position);
                AcceptEvent();
                break;
            case InputEventMouseMotion mm when _dragging:
                Look(mm.Position);
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } rb:
                Root.Selection.OrderMoveTo(ToSim(rb.Position));
                AcceptEvent();
                break;
        }
    }

    private void Look(Vector2 local) => Root.Camera.Position = MapView.ToWorld(ToSim(local));
}
