using Godot;

namespace Overmatch.Game;

/// <summary>Scene root: shows the menu, starts and ends matches.</summary>
public partial class App : Node
{
    private MainMenu? _menu;
    private GameRoot? _game;
    private string? _smokePath;
    private int _speed = 1;
    private string? _autoDifficulty;
    private string _faction = "coalition";
    private string _enemy = "coalition";
    private string _mapId = "plain";
    private string? _menuShot;
    private int _frames;

    public override void _Ready()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--smoke=")) _smokePath = arg["--smoke=".Length..];
            else if (arg.StartsWith("--speed=")) _speed = int.Parse(arg["--speed=".Length..]);
            else if (arg.StartsWith("--ai=")) _autoDifficulty = arg["--ai=".Length..];
            else if (arg.StartsWith("--faction=")) _faction = arg["--faction=".Length..];
            else if (arg.StartsWith("--enemy=")) _enemy = arg["--enemy=".Length..];
            else if (arg.StartsWith("--map=")) _mapId = arg["--map=".Length..];
            else if (arg.StartsWith("--menu-shot=")) _menuShot = arg["--menu-shot=".Length..];
        }
        var scripted = _smokePath is not null || _menuShot is not null;
        GameSettings.Load();
        if (scripted) { GameSettings.UiScale = 0f; GameSettings.Music = GameSettings.Master = 0f; }
        Hotkeys.Install();
        var win = GetWindow();
        win.MinSize = new Vector2I(1024, 640);
        win.SizeChanged += () => UiScaler.Apply(win);
        GameSettings.ApplyAll(win, window: !scripted && !OS.GetCmdlineArgs().Contains("--resolution"));
        if (_smokePath is not null || _autoDifficulty is not null)
        {
            var s = MatchSettings.Default(1, _autoDifficulty ?? "medium");
            s.Players[0].Faction = _faction;
            s.Players[1].Faction = _enemy;
            s.MapId = _mapId;
            StartMatch(s);
        }
        else
            ShowMenu();
    }

    /// <summary>`--menu-shot=/path.png` photographs the title screen and the skirmish setup, then quits.</summary>
    public override void _Process(double delta)
    {
        if (_menuShot is null) return;
        _frames++;
        if (_frames == 40) GetViewport().GetTexture().GetImage().SavePng(_menuShot.Replace(".png", "_title.png"));
        if (_frames == 41) { _menu?.QueueFree(); _menu = new MainMenu { App = this, StartOnSetup = true }; AddChild(_menu); }
        if (_frames == 80) _menu?.OpenSettings();
        if (_frames == 110) GetViewport().GetTexture().GetImage().SavePng(_menuShot.Replace(".png", "_settings.png"));
        if (_frames == 79) GetViewport().GetTexture().GetImage().SavePng(_menuShot);
        if (_frames == 111) { GD.Print("[Menu] screenshots saved"); GetTree().Quit(); }
    }

    public void ShowMenu()
    {
        _game?.QueueFree();
        _game = null;
        UiScaler.InGame = false;
        UiScaler.Apply(GetWindow());
        _menu = new MainMenu { App = this };
        AddChild(_menu);
    }

    public void StartMatch(MatchSettings settings)
    {
        _menu?.QueueFree();
        _menu = null;
        UiScaler.InGame = true;
        UiScaler.Apply(GetWindow());
        _game = new GameRoot { Settings = settings, App = this, Speed = _speed, SmokePath = _smokePath };
        AddChild(_game);
    }
}
