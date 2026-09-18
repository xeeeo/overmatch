using Godot;
using Overmatch.Game.UiReview;

namespace Overmatch.Game;

/// <summary>Player preferences, saved to user://settings.cfg and applied to the window, renderer and audio buses.</summary>
public static class GameSettings
{
    private const string Path = "user://settings.cfg";

    // Display
    public static int WindowMode = 0;            // 0 windowed, 1 fullscreen
    public static int WindowWidth = 1600, WindowHeight = 1000;
    /// <summary>In-game interface size as a fraction of the largest that fits. 0 = automatic.</summary>
    public static float UiScale = 0f;
    public static float RenderScale = 1f;
    public static int Msaa = 1;                  // 0 off, 1 2x, 2 4x
    public static bool VSync = true;
    public static bool ShowFps;
    // Audio, 0..1
    public static float Master = 0.8f, Music = 0.6f, Effects = 0.8f, Voice = 0.8f, Interface = 0.7f;
    // Controls
    public static bool EdgeScroll = true;
    public static float ScrollSpeed = 1f;
    public static bool GroupTags = true;

    public static readonly string[] Buses = { "Music", "Effects", "Voice", "Interface" };
    public static event Action? Changed;

    public static void Load()
    {
        var c = new ConfigFile();
        if (c.Load(Path) != Error.Ok) return;
        WindowMode = (int)c.GetValue("display", "mode", WindowMode);
        WindowWidth = (int)c.GetValue("display", "width", WindowWidth);
        WindowHeight = (int)c.GetValue("display", "height", WindowHeight);
        UiScale = (float)c.GetValue("display", "ui_scale", UiScale);
        RenderScale = (float)c.GetValue("display", "render_scale", RenderScale);
        Msaa = (int)c.GetValue("display", "msaa", Msaa);
        VSync = (bool)c.GetValue("display", "vsync", VSync);
        ShowFps = (bool)c.GetValue("display", "show_fps", ShowFps);
        Master = (float)c.GetValue("audio", "master", Master);
        Music = (float)c.GetValue("audio", "music", Music);
        Effects = (float)c.GetValue("audio", "effects", Effects);
        Voice = (float)c.GetValue("audio", "voice", Voice);
        Interface = (float)c.GetValue("audio", "interface", Interface);
        EdgeScroll = (bool)c.GetValue("controls", "edge_scroll", EdgeScroll);
        ScrollSpeed = (float)c.GetValue("controls", "scroll_speed", ScrollSpeed);
        GroupTags = (bool)c.GetValue("controls", "group_tags", GroupTags);
    }

    public static void Save()
    {
        var c = new ConfigFile();
        c.SetValue("display", "mode", WindowMode);
        c.SetValue("display", "width", WindowWidth);
        c.SetValue("display", "height", WindowHeight);
        c.SetValue("display", "ui_scale", UiScale);
        c.SetValue("display", "render_scale", RenderScale);
        c.SetValue("display", "msaa", Msaa);
        c.SetValue("display", "vsync", VSync);
        c.SetValue("display", "show_fps", ShowFps);
        c.SetValue("audio", "master", Master);
        c.SetValue("audio", "music", Music);
        c.SetValue("audio", "effects", Effects);
        c.SetValue("audio", "voice", Voice);
        c.SetValue("audio", "interface", Interface);
        c.SetValue("controls", "edge_scroll", EdgeScroll);
        c.SetValue("controls", "scroll_speed", ScrollSpeed);
        c.SetValue("controls", "group_tags", GroupTags);
        c.Save(Path);
    }

    public static void EnsureBuses()
    {
        foreach (var name in Buses)
        {
            if (AudioServer.GetBusIndex(name) >= 0) continue;
            AudioServer.AddBus();
            var i = AudioServer.BusCount - 1;
            AudioServer.SetBusName(i, name);
            AudioServer.SetBusSend(i, "Master");
        }
    }

    public static void ApplyAudio()
    {
        EnsureBuses();
        Set("Master", Master); Set("Music", Music); Set("Effects", Effects); Set("Voice", Voice); Set("Interface", Interface);
        static void Set(string bus, float v)
        {
            var i = AudioServer.GetBusIndex(bus);
            AudioServer.SetBusMute(i, v <= 0.001f);
            AudioServer.SetBusVolumeDb(i, Mathf.LinearToDb(Mathf.Max(v, 0.001f)));
        }
    }

    /// <summary>Window mode and size. Skipped for scripted runs, which set their own resolution on the command line.</summary>
    public static void ApplyWindow(Window win)
    {
        var full = WindowMode == 1;
        var mode = full ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        if (DisplayServer.WindowGetMode() != mode) DisplayServer.WindowSetMode(mode);
        if (!full)
        {
            var screen = DisplayServer.ScreenGetUsableRect();
            var size = new Vector2I(Math.Min(WindowWidth, screen.Size.X), Math.Min(WindowHeight, screen.Size.Y));
            if (win.Size != size)
            {
                win.Size = size;
                win.Position = screen.Position + (screen.Size - size) / 2;
            }
        }
    }

    public static void ApplyRendering(Window win)
    {
        DisplayServer.WindowSetVsyncMode(VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        win.Msaa3D = Msaa switch { 0 => Viewport.Msaa.Disabled, 2 => Viewport.Msaa.Msaa4X, _ => Viewport.Msaa.Msaa2X };
        win.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
        win.Scaling3DScale = Mathf.Clamp(RenderScale, 0.5f, 1f);
    }

    public static void ApplyAll(Window win, bool window)
    {
        if (window) ApplyWindow(win);
        ApplyRendering(win);
        ApplyAudio();
        UiScaler.Apply(win);
        Changed?.Invoke();
    }

    public static void NotifyChanged() => Changed?.Invoke();
}

/// <summary>
/// Interface scaling through the window's content scale factor, so text and vector art are rasterised at the final
/// size instead of being drawn small and stretched. The 2D coordinate space becomes design units: at least 1600 x 900.
/// </summary>
public static class UiScaler
{
    public static bool InGame { get; set; }
    public static float Factor { get; private set; } = 1f;

    public static float Fit(Window win) => Math.Max(0.25f, Math.Min(win.Size.X / Ui.DesignWidth, win.Size.Y / Ui.MinDesignHeight));

    /// <summary>Automatic: full size on small windows, smaller on big screens where the full-size console eats the battlefield.</summary>
    public static float GameFraction(Window win)
    {
        if (GameSettings.UiScale > 0.01f) return Mathf.Clamp(GameSettings.UiScale, 0.5f, 1f);
        return win.Size.X >= 3000 ? 0.7f : win.Size.X >= 2200 ? 0.8f : 1f;
    }

    public static void Apply(Window win)
    {
        var f = Fit(win) * (InGame ? GameFraction(win) : 1f);
        Factor = f;
        if (Mathf.Abs(win.ContentScaleFactor - f) > 0.0005f) win.ContentScaleFactor = f;
    }
}
