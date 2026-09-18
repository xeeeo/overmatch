using Godot;

namespace Overmatch.Game.UiReview;

/// <summary>Original command-console visual language. No global theme or gameplay hooks.</summary>
public static class CommandTheme
{
    public static readonly Color Ink = new("101819");
    public static readonly Color Panel = new("1a2426");
    public static readonly Color Raised = new("283437");
    public static readonly Color Line = new("4b5b5b");
    public static readonly Color Text = new("e4e8dd");
    public static readonly Color Muted = new("99aaa8");
    public static readonly Color Gold = new("e8bd65");
    public static readonly Color Green = new("9bc599");
    public static readonly Color Red = new("f08a72");
    public static readonly Color Blue = new("8cbed1");
    public static Font Body => GD.Load<Font>("res://ui/command/assets/fonts/Barlow-Regular.ttf");
    public static Font Display => GD.Load<Font>("res://ui/command/assets/fonts/BarlowCondensed-SemiBold.ttf");

    public static StyleBoxFlat Box(Color fill, Color border, int width = 1)
    {
        return new StyleBoxFlat
        {
            BgColor = fill, BorderColor = border,
            BorderWidthLeft = width, BorderWidthTop = width,
            BorderWidthRight = width, BorderWidthBottom = width,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 6, ContentMarginBottom = 6,
        };
    }

    public static void Style(Button button, bool accent = false)
    {
        button.AddThemeFontOverride("font", Display);
        button.AddThemeFontSizeOverride("font_size", 18);
        button.AddThemeColorOverride("font_color", accent ? Gold : Text);
        button.AddThemeColorOverride("font_hover_color", Text);
        button.AddThemeColorOverride("font_pressed_color", Gold);
        button.AddThemeColorOverride("font_disabled_color", Muted.Darkened(.3f));
        button.AddThemeStyleboxOverride("normal", Box(accent ? new Color("34382e") : Panel, accent ? Gold.Darkened(.3f) : Line));
        button.AddThemeStyleboxOverride("hover", Box(Raised.Lightened(.1f), Gold));
        button.AddThemeStyleboxOverride("pressed", Box(Ink, Gold, 2));
        button.AddThemeStyleboxOverride("disabled", Box(Ink, Line.Darkened(.5f)));
        button.AddThemeStyleboxOverride("focus", new StyleBoxFlat
        {
            BgColor = Colors.Transparent, BorderColor = Gold,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            ExpandMarginLeft = 3, ExpandMarginRight = 3, ExpandMarginTop = 3, ExpandMarginBottom = 3,
        });
    }

    public static Texture2D Icon(string name) => GD.Load<Texture2D>($"res://ui/command/assets/icons/{name}.svg");
}
