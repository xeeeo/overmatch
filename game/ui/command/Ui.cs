using Godot;

namespace Overmatch.Game.UiReview;

/// <summary>Shared builders for the command-console look, used by the live HUD, menus and overlays.</summary>
public static class Ui
{
    /// <summary>Design canvas: everything is laid out at 1600 wide and scaled uniformly to the window.</summary>
    public const float DesignWidth = 1600f;
    public const float MinDesignHeight = 900f;

    /// <summary>Fit a design canvas to the viewport. Returns the scale and the design height.</summary>
    public static (float scale, float height) Fit(Control canvas, Vector2 viewport)
    {
        // The window's content scale factor (UiScaler) already makes the viewport at least 1600 x 900 design units,
        // so nothing is stretched here: the 1600-wide page is simply centred.
        var height = Math.Max(MinDesignHeight, viewport.Y);
        canvas.Scale = Vector2.One;
        canvas.Size = new Vector2(DesignWidth, height);
        canvas.Position = new Vector2(Mathf.Round((viewport.X - DesignWidth) / 2f), 0);
        return (1f, height);
    }

    public static ConsolePanel Panel(Node parent, float x, float y, float w, float h, Color fill, bool accent = false, float cut = 8, bool blockMouse = false)
    {
        var p = new ConsolePanel { Position = new Vector2(x, y), Size = new Vector2(w, h), Fill = fill, Accent = accent, Cut = cut };
        parent.AddChild(p);
        if (blockMouse) p.Ready += () => p.MouseFilter = Control.MouseFilterEnum.Stop;
        return p;
    }

    public static Label Text(Node parent, string text, float x, float y, float w, float h, int size, Color color, bool display = false,
        HorizontalAlignment align = HorizontalAlignment.Left, bool wrap = false)
    {
        var label = new Label
        {
            Text = text, Position = new Vector2(x, y), Size = new Vector2(w, h), HorizontalAlignment = align,
            MouseFilter = Control.MouseFilterEnum.Ignore, ClipText = !wrap,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        if (wrap) label.MaxLinesVisible = Math.Max(1, (int)(h / (size * 1.35f)));
        label.AddThemeFontOverride("font", display ? CommandTheme.Display : CommandTheme.Body);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
        // A Label grows to its minimum size when added; pin it back to the design rectangle so nothing spills out of a panel.
        label.Size = new Vector2(w, h);
        return label;
    }

    public static Button Button(Node parent, string text, float x, float y, float w, float h, Action pressed, bool accent = false, int fontSize = 18)
    {
        var b = new Button
        {
            Text = text, Position = new Vector2(x, y), Size = new Vector2(w, h),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand, FocusMode = Control.FocusModeEnum.None, ClipText = true,
        };
        CommandTheme.Style(b, accent);
        b.AddThemeFontSizeOverride("font_size", fontSize);
        b.Pressed += pressed;
        parent.AddChild(b);
        return b;
    }

    public static TextureRect Icon(Node parent, string name, float x, float y, float size, Color color)
    {
        var t = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = CommandTheme.Icon(name), Position = new Vector2(x, y), Size = new Vector2(size, size),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Modulate = color, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(t);
        return t;
    }

    public static TextureRect Picture(Node parent, Texture2D texture, float x, float y, float w, float h)
    {
        var t = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = texture, Position = new Vector2(x, y), Size = new Vector2(w, h),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(t);
        return t;
    }

    public static ColorRect Rule(Node parent, float x, float y, float w, Color color, float h = 1)
    {
        var r = new ColorRect { Position = new Vector2(x, y), Size = new Vector2(w, h), Color = color, MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(r);
        return r;
    }

    public static SegmentMeter Meter(Node parent, float x, float y, float w, float h, int segments, Color color)
    {
        var m = new SegmentMeter { Position = new Vector2(x, y), Size = new Vector2(w, h), Segments = segments, Colour = color };
        parent.AddChild(m);
        return m;
    }

    public static OptionButton Picker(Node parent, string[] items, int selected, float x, float y, float w, float h = 51, int fontSize = 21)
    {
        var option = new OptionButton { Position = new Vector2(x, y), Size = new Vector2(w, h), MouseDefaultCursorShape = Control.CursorShape.PointingHand, FocusMode = Control.FocusModeEnum.None };
        foreach (var item in items) option.AddItem(item);
        option.Select(Math.Clamp(selected, 0, Math.Max(0, items.Length - 1)));
        CommandTheme.Style(option);
        option.AddThemeFontOverride("font", CommandTheme.Body);
        option.AddThemeFontSizeOverride("font_size", fontSize);
        option.AddThemeConstantOverride("arrow_margin", 12);
        var popup = option.GetPopup();
        popup.AddThemeFontOverride("font", CommandTheme.Body);
        popup.AddThemeFontSizeOverride("font_size", fontSize);
        popup.AddThemeColorOverride("font_color", CommandTheme.Text);
        popup.AddThemeColorOverride("font_hover_color", CommandTheme.Gold);
        popup.AddThemeStyleboxOverride("panel", CommandTheme.Box(CommandTheme.Panel, CommandTheme.Line));
        popup.AddThemeStyleboxOverride("hover", CommandTheme.Box(CommandTheme.Raised, CommandTheme.Gold));
        popup.AddThemeConstantOverride("v_separation", 12);
        parent.AddChild(option);
        return option;
    }

    public static string FactionIcon(string factionId) => factionId is "coalition" or "directorate" or "network" ? factionId : "command";

    public static string Clock(float seconds)
    {
        var s = Math.Max(0, (int)seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }

    public static void Clear(Node parent)
    {
        foreach (var child in parent.GetChildren()) { parent.RemoveChild(child); child.QueueFree(); }
    }
}

/// <summary>Segmented bar whose fill can change every frame without rebuilding controls.</summary>
public partial class SegmentMeter : Control
{
    public int Segments { get; set; } = 12;
    public Color Colour { get; set; } = CommandTheme.Green;
    private float _fraction = 1f;
    public float Fraction
    {
        get => _fraction;
        set { value = Mathf.Clamp(value, 0f, 1f); if (Mathf.Abs(value - _fraction) < 0.004f) return; _fraction = value; QueueRedraw(); }
    }

    public void SetColour(Color c) { if (c == Colour) return; Colour = c; QueueRedraw(); }

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw()
    {
        var step = Size.X / Segments;
        for (var i = 0; i < Segments; i++)
            DrawRect(new Rect2(step * i, 0, step - 2, Size.Y), i < _fraction * Segments - 0.001f ? Colour.Darkened(.1f) : CommandTheme.Line.Darkened(.5f));
    }
}
