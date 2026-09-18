using Godot;
using Overmatch.Game.UiReview;

namespace Overmatch.Game;

/// <summary>Display, audio and controls. Opened from the title screen and the pause menu; changes apply at once and are saved on close.</summary>
public partial class SettingsPanel : CanvasLayer
{
    public Action? Closed { get; set; }

    private Control _shade = null!;
    private Control _canvas = null!;
    private int _tab;
    private static readonly string[] Tabs = { "DISPLAY", "AUDIO", "CONTROLS" };
    private static readonly (int W, int H)[] Sizes = { (1280, 800), (1600, 1000), (1920, 1200), (2240, 1400), (2560, 1600), (3200, 2000) };
    private static readonly float[] UiScales = { 0f, 1f, 0.9f, 0.8f, 0.7f, 0.6f };
    private static readonly float[] RenderScales = { 1f, 0.85f, 0.75f, 0.67f, 0.5f };

    public override void _Ready()
    {
        Layer = 20;
        ProcessMode = ProcessModeEnum.Always;
        var shade = new ColorRect { Color = new Color(0.015f, .025f, .028f, .86f), MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(shade);
        _shade = shade;
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _shade.AddChild(_canvas);
        GetViewport().SizeChanged += Build;
        Build();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= Build;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("cancel")) { GetViewport().SetInputAsHandled(); Close(); }
    }

    private void Close()
    {
        GameSettings.Save();
        Closed?.Invoke();
        QueueFree();
    }

    private void Apply(bool window = false)
    {
        GameSettings.ApplyAll(GetWindow(), window);
    }

    private void Build()
    {
        if (!IsInsideTree()) return;
        var (_, height) = Ui.Fit(_canvas, GetViewport().GetVisibleRect().Size);
        Ui.Clear(_canvas);
        const float w = 860, h = 640;
        var p = Ui.Panel(_canvas, (Ui.DesignWidth - w) / 2, (height - h) / 2, w, h, CommandTheme.Panel, true);
        Ui.Text(p, "SETTINGS", 40, 22, 400, 44, 36, CommandTheme.Text, true);
        Ui.Text(p, "CHANGES APPLY IMMEDIATELY", 40, 66, 400, 18, 12, CommandTheme.Muted, true);
        for (var i = 0; i < Tabs.Length; i++)
        {
            var index = i;
            Ui.Button(p, Tabs[i], 430 + i * 132, 30, 124, 40, () => { _tab = index; Build(); }, _tab == i, 16);
        }
        Ui.Rule(p, 40, 96, w - 80, CommandTheme.Line.Darkened(.25f));
        var body = new Control { Position = new Vector2(40, 112), MouseFilter = Control.MouseFilterEnum.Ignore };
        p.AddChild(body);
        switch (_tab)
        {
            case 0: BuildDisplay(body); break;
            case 1: BuildAudio(body); break;
            default: BuildControls(body); break;
        }
        Ui.Button(p, "DONE", w - 240, h - 70, 200, 46, Close, true, 20);
        Ui.Button(p, "RESTORE DEFAULTS", 40, h - 66, 200, 38, () =>
        {
            GameSettings.UiScale = 0f; GameSettings.RenderScale = 1f; GameSettings.Msaa = 1; GameSettings.VSync = true; GameSettings.ShowFps = false;
            GameSettings.Master = 0.8f; GameSettings.Music = 0.6f; GameSettings.Effects = 0.8f; GameSettings.Voice = 0.8f; GameSettings.Interface = 0.7f;
            GameSettings.EdgeScroll = true; GameSettings.ScrollSpeed = 1f; GameSettings.GroupTags = true;
            Apply();
            Build();
        }, false, 14);
    }

    private static void Row(Control body, int row, string title, string note)
    {
        var y = row * 66f;
        Ui.Text(body, title, 0, y + 4, 300, 24, 19, CommandTheme.Text, true);
        Ui.Text(body, note, 0, y + 29, 400, 20, 13, CommandTheme.Muted);
    }

    private void Choice(Control body, int row, string title, string note, string[] items, int selected, Action<int> set)
    {
        Row(body, row, title, note);
        var pick = Ui.Picker(body, items, Math.Max(0, selected), 420, row * 66f + 4, 360, 44, 17);
        pick.ItemSelected += i => set((int)i);
    }

    private void Slider(Control body, int row, string title, string note, float value, float min, float max, Action<float> set, Func<float, string> show)
    {
        Row(body, row, title, note);
        var y = row * 66f;
        var label = Ui.Text(body, show(value), 700, y + 12, 80, 24, 18, CommandTheme.Gold, true, HorizontalAlignment.Right);
        var slider = new HSlider { Position = new Vector2(420, y + 12), Size = new Vector2(270, 26), MinValue = min, MaxValue = max, Step = 0.01, Value = value };
        var track = new StyleBoxFlat { BgColor = CommandTheme.Ink, ContentMarginTop = 4, ContentMarginBottom = 4 };
        var fill = new StyleBoxFlat { BgColor = CommandTheme.Gold.Darkened(.15f), ContentMarginTop = 4, ContentMarginBottom = 4 };
        slider.AddThemeStyleboxOverride("slider", track);
        slider.AddThemeStyleboxOverride("grabber_area", fill);
        slider.AddThemeStyleboxOverride("grabber_area_highlight", fill);
        body.AddChild(slider);
        slider.ValueChanged += v => { set((float)v); label.Text = show((float)v); };
    }

    private void BuildDisplay(Control body)
    {
        var screen = DisplayServer.ScreenGetSize();
        var sizes = Sizes.Where(s => s.W <= screen.X && s.H <= screen.Y).ToList();
        if (!sizes.Contains((GameSettings.WindowWidth, GameSettings.WindowHeight))) sizes.Add((GameSettings.WindowWidth, GameSettings.WindowHeight));
        if (sizes.Count == 0) sizes.Add(Sizes[0]);
        Choice(body, 0, "WINDOW", $"This display is {screen.X} × {screen.Y} pixels.", new[] { "Windowed", "Full screen" }, GameSettings.WindowMode,
            i => { GameSettings.WindowMode = i; Apply(true); });
        Choice(body, 1, "WINDOW SIZE", "Used when windowed. Sizes are in pixels.",
            sizes.Select(s => $"{s.W} × {s.H}").ToArray(), sizes.FindIndex(s => s.W == GameSettings.WindowWidth),
            i => { GameSettings.WindowWidth = sizes[i].W; GameSettings.WindowHeight = sizes[i].H; Apply(true); });
        Choice(body, 2, "INTERFACE SIZE", "In-game console size. Smaller shows more battlefield.",
            UiScales.Select(s => s == 0f ? "Automatic" : $"{s * 100:0}%").ToArray(), Array.IndexOf(UiScales, UiScales.OrderBy(s => Math.Abs(s - GameSettings.UiScale)).First()),
            i => { GameSettings.UiScale = UiScales[i]; Apply(); });
        Choice(body, 3, "3D RESOLUTION", "Lower it if the frame rate drops. The interface stays sharp.",
            RenderScales.Select(s => $"{s * 100:0}%").ToArray(), Array.IndexOf(RenderScales, RenderScales.OrderBy(s => Math.Abs(s - GameSettings.RenderScale)).First()),
            i => { GameSettings.RenderScale = RenderScales[i]; Apply(); });
        Choice(body, 4, "ANTI-ALIASING", "Smooths edges of models.", new[] { "Off", "2×", "4×" }, GameSettings.Msaa, i => { GameSettings.Msaa = i; Apply(); });
        Choice(body, 5, "V-SYNC  /  FPS COUNTER", "Sync to the display, and show the frame rate under the clock.",
            new[] { "V-sync on", "V-sync on, show FPS", "V-sync off", "V-sync off, show FPS" }, (GameSettings.VSync ? 0 : 2) + (GameSettings.ShowFps ? 1 : 0),
            i => { GameSettings.VSync = i < 2; GameSettings.ShowFps = i % 2 == 1; Apply(); });
    }

    private void BuildAudio(Control body)
    {
        static string Pct(float v) => v <= 0.001f ? "OFF" : $"{v * 100:0}%";
        Slider(body, 0, "MASTER", "Everything.", GameSettings.Master, 0, 1, v => { GameSettings.Master = v; GameSettings.ApplyAudio(); }, Pct);
        Slider(body, 1, "MUSIC", "M toggles it in a match.", GameSettings.Music, 0, 1, v => { GameSettings.Music = v; GameSettings.ApplyAudio(); }, Pct);
        Slider(body, 2, "EFFECTS", "Weapons, explosions, engines.", GameSettings.Effects, 0, 1, v => { GameSettings.Effects = v; GameSettings.ApplyAudio(); }, Pct);
        Slider(body, 3, "VOICES", "Unit responses and the announcer.", GameSettings.Voice, 0, 1, v => { GameSettings.Voice = v; GameSettings.ApplyAudio(); }, Pct);
        Slider(body, 4, "INTERFACE", "Clicks, cash and alerts.", GameSettings.Interface, 0, 1, v => { GameSettings.Interface = v; GameSettings.ApplyAudio(); }, Pct);
    }

    private void BuildControls(Control body)
    {
        Choice(body, 0, "EDGE SCROLLING", "Move the view by pushing the pointer against the screen edge.", new[] { "On", "Off" }, GameSettings.EdgeScroll ? 0 : 1,
            i => { GameSettings.EdgeScroll = i == 0; });
        Slider(body, 1, "SCROLL SPEED", "Arrow keys and edge scrolling.", GameSettings.ScrollSpeed, 0.4f, 2.5f, v => GameSettings.ScrollSpeed = v, v => $"{v * 100:0}%");
        Choice(body, 2, "GROUP NUMBER TAGS", "Show each unit's control group number beside it.", new[] { "On", "Off" }, GameSettings.GroupTags ? 0 : 1,
            i => { GameSettings.GroupTags = i == 0; });
        Ui.Rule(body, 0, 204, 780, CommandTheme.Line.Darkened(.25f));
        Ui.Text(body, "KEYS", 0, 212, 200, 22, 16, CommandTheme.Text, true);
        var half = (Hotkeys.Reference.Length + 1) / 2;
        for (var i = 0; i < Hotkeys.Reference.Length; i++)
        {
            var (keys, does) = Hotkeys.Reference[i];
            var x = i < half ? 0f : 395f;
            var y = 238f + (i < half ? i : i - half) * 17.5f;
            Ui.Text(body, keys, x, y, 112, 17, 11, CommandTheme.Gold, true);
            Ui.Text(body, does, x + 114, y - 1, 275, 17, 12, CommandTheme.Muted);
        }
    }
}
