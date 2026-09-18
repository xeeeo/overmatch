using Godot;

namespace Overmatch.Game.UiReview;

/// <summary>Vector-machined panels: cut corners, recessed seams and subtle surface grain.</summary>
public partial class ConsolePanel : Control
{
    public Color Fill { get; set; } = CommandTheme.Panel;
    public Color Edge { get; set; } = CommandTheme.Line;
    public bool Accent { get; set; }
    public bool Texture { get; set; } = true;
    public float Cut { get; set; } = 10;

    public override void _Ready() { MouseFilter = MouseFilterEnum.Ignore; Resized += QueueRedraw; }
    public override void _Draw()
    {
        var w = Size.X; var h = Size.Y; var c = Cut;
        Vector2[] points = { new(c, 0), new(w, 0), new(w, h-c), new(w-c, h), new(0, h), new(0, c) };
        DrawColoredPolygon(points, Fill);
        DrawPolyline(points.Append(points[0]).ToArray(), Edge, 1, true);
        DrawLine(new(c + 1, 2), new(w - 2, 2), new Color(1, 1, 1, .07f));
        DrawLine(new(2, h - 2), new(w - c, h - 2), new Color(0, 0, 0, .6f));
        if (Texture)
            for (int y = 6; y < h - 3; y += 4)
                DrawLine(new(4, y), new(w - 4, y), new Color(1, 1, 1, .012f));
        if (Accent) DrawLine(new(c, 0), new(Mathf.Min(w - 12, 80), 0), CommandTheme.Gold, 3);
        foreach (var pos in new[] { new Vector2(9, h - 9), new Vector2(w - 9, 9) })
        {
            DrawCircle(pos, 2, CommandTheme.Ink);
            DrawLine(pos + new Vector2(-1, 0), pos + new Vector2(1, 0), Edge, 1);
        }
    }
}
