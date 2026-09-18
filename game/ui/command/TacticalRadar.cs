using Godot;
using Overmatch.Sim;

namespace Overmatch.Game.UiReview;

/// <summary>
/// Review-only tactical map for the frozen UI fixture. This displays the fixture's
/// full terrain and entities; it does not implement gameplay radar or fog rules.
/// </summary>
public partial class TacticalRadar : Control
{
    public World World { get; set; } = null!;
    public event Action<Vector2>? Navigated;

    private Vector2 _focusNormalized = new(24f / 96f, 1f - 16f / 96f);
    public Vector2 FocusNormalized
    {
        get => _focusNormalized;
        set
        {
            _focusNormalized = new Vector2(Mathf.Clamp(value.X, 0f, 1f), Mathf.Clamp(value.Y, 0f, 1f));
            QueueRedraw();
        }
    }

    private ImageTexture? _terrain;
    private bool _dragging;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.Cross;
        TooltipText = "Click to inspect the review battlefield";
        Resized += QueueRedraw;
        BuildTerrain();
    }

    private Rect2 MapRect()
    {
        var available = new Vector2(Mathf.Max(Size.X - 8f, 1f), Mathf.Max(Size.Y - 8f, 1f));
        var aspect = (float)World.Grid.Width / World.Grid.Height;
        var dimensions = available.X / available.Y > aspect
            ? new Vector2(available.Y * aspect, available.Y)
            : new Vector2(available.X, available.X / aspect);
        return new Rect2((Size - dimensions) * 0.5f, dimensions);
    }

    private void BuildTerrain()
    {
        var grid = World.Grid;
        var image = Image.CreateEmpty(grid.Width, grid.Height, false, Image.Format.Rgb8);
        var olive = CommandTheme.Panel.Lerp(new Color("46503c"), 0.64f);
        var rough = olive.Lerp(CommandTheme.Gold.Darkened(0.6f), 0.22f);
        var road = olive.Lerp(CommandTheme.Gold.Darkened(0.35f), 0.28f);
        var water = CommandTheme.Ink.Lerp(CommandTheme.Blue, 0.22f);
        var cliff = CommandTheme.Ink.Lerp(CommandTheme.Muted, 0.12f);

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var colour = grid.Get(x, y) switch
                {
                    CellType.Road => road,
                    CellType.Rough => rough,
                    CellType.Water => water,
                    CellType.Cliff => cliff,
                    CellType.Structure => olive.Darkened(0.12f),
                    _ => olive,
                };
                var grain = 0.018f * Mathf.Sin(x * 1.91f + y * 0.79f);
                image.SetPixel(x, grid.Height - 1 - y, colour.Lightened(grain));
            }
        }
        _terrain = ImageTexture.CreateFromImage(image);
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        if (World is null || _terrain is null) return;
        DrawRect(new Rect2(Vector2.Zero, Size), CommandTheme.Ink);
        var map = MapRect();
        DrawTextureRect(_terrain, map, false);

        var gridColour = new Color(CommandTheme.Muted, 0.11f);
        for (var division = 1; division < 4; division++)
        {
            var t = division / 4f;
            DrawLine(map.Position + new Vector2(map.Size.X * t, 0), map.Position + new Vector2(map.Size.X * t, map.Size.Y), gridColour);
            DrawLine(map.Position + new Vector2(0, map.Size.Y * t), map.Position + new Vector2(map.Size.X, map.Size.Y * t), gridColour);
        }

        foreach (var pile in World.Piles)
        {
            if (pile.Depleted) continue;
            var at = ToRadar(pile.Pos, map);
            DrawRect(new Rect2(at - Vector2.One * 1.5f, Vector2.One * 3f), new Color(CommandTheme.Gold, 0.65f));
        }

        foreach (var entity in World.Entities)
        {
            if (!entity.Alive || entity.IsInside) continue;
            var at = ToRadar(entity.Pos, map);
            var colour = entity.Owner == 0 ? CommandTheme.Blue : entity.Owner < 0 ? CommandTheme.Muted : CommandTheme.Red;
            if (entity.IsBuilding)
            {
                var dimensions = new Vector2(
                    Mathf.Max(3f, entity.Building!.Width * map.Size.X / World.Grid.Width),
                    Mathf.Max(3f, entity.Building.Height * map.Size.Y / World.Grid.Height));
                DrawRect(new Rect2(at - dimensions * 0.5f, dimensions), new Color(colour, entity.Owner < 0 ? 0.45f : 0.85f));
            }
            else
            {
                var radius = entity.Def.IsInfantry ? 1.25f : 1.8f;
                DrawCircle(at, radius + 1.3f, new Color(colour, 0.15f));
                DrawCircle(at, radius, colour);
            }
        }

        // A composition indicator for the review camera, intentionally independent
        // of future gameplay minimap and visibility implementations.
        var focus = map.Position + map.Size * FocusNormalized;
        var half = map.Size * new Vector2(0.17f, 0.14f);
        var start = new Vector2(Mathf.Max(map.Position.X, focus.X - half.X), Mathf.Max(map.Position.Y, focus.Y - half.Y));
        var end = new Vector2(Mathf.Min(map.End.X, focus.X + half.X), Mathf.Min(map.End.Y, focus.Y + half.Y));
        DrawRect(new Rect2(start, end - start), new Color(CommandTheme.Blue, 0.045f));
        DrawRect(new Rect2(start, end - start), new Color(CommandTheme.Blue, 0.92f), false, 1.25f);
        DrawLine(focus - new Vector2(3f, 0f), focus + new Vector2(3f, 0f), new Color(CommandTheme.Blue, 0.7f));
        DrawLine(focus - new Vector2(0f, 3f), focus + new Vector2(0f, 3f), new Color(CommandTheme.Blue, 0.7f));

        DrawRect(map, new Color(CommandTheme.Line, 0.75f), false);
        DrawCorner(map.Position, new Vector2(1f, 1f));
        DrawCorner(new Vector2(map.End.X, map.Position.Y), new Vector2(-1f, 1f));
        DrawCorner(new Vector2(map.Position.X, map.End.Y), new Vector2(1f, -1f));
        DrawCorner(map.End, new Vector2(-1f, -1f));
        DrawString(CommandTheme.Display, map.Position + new Vector2(map.Size.X * 0.5f - 3f, 12f), "N", fontSize: 11, modulate: CommandTheme.Muted);
    }

    private void DrawCorner(Vector2 corner, Vector2 inward)
    {
        var colour = new Color(CommandTheme.Blue, 0.7f);
        DrawLine(corner, corner + new Vector2(inward.X * 7f, 0f), colour, 1.5f);
        DrawLine(corner, corner + new Vector2(0f, inward.Y * 7f), colour, 1.5f);
    }

    private Vector2 ToRadar(Vec2 point, Rect2 map) => map.Position + new Vector2(
        point.X / World.Grid.Width * map.Size.X,
        (1f - point.Y / World.Grid.Height) * map.Size.Y);

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            _dragging = button.Pressed;
            if (_dragging) Navigate(button.Position);
            AcceptEvent();
        }
        else if (@event is InputEventMouseMotion motion && _dragging)
        {
            Navigate(motion.Position);
            AcceptEvent();
        }
    }

    private void Navigate(Vector2 local)
    {
        var map = MapRect();
        if (!map.HasPoint(local)) return;
        FocusNormalized = (local - map.Position) / map.Size;
        Navigated?.Invoke(FocusNormalized);
    }
}
