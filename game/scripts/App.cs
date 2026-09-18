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

    public override void _Ready()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--smoke=")) _smokePath = arg["--smoke=".Length..];
            else if (arg.StartsWith("--speed=")) _speed = int.Parse(arg["--speed=".Length..]);
            else if (arg.StartsWith("--ai=")) _autoDifficulty = arg["--ai=".Length..];
        }
        if (_smokePath is not null || _autoDifficulty is not null)
            StartMatch(MatchSettings.Default(1, _autoDifficulty ?? "medium"));
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
