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
        }
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

    public void ShowMenu()
    {
        _game?.QueueFree();
        _game = null;
        _menu = new MainMenu { App = this };
        AddChild(_menu);
    }

    public void StartMatch(MatchSettings settings)
    {
        _menu?.QueueFree();
        _menu = null;
        _game = new GameRoot { Settings = settings, App = this, Speed = _speed, SmokePath = _smokePath };
        AddChild(_game);
    }
}
