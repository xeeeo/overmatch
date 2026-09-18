using Godot;
using Overmatch.Game.UiReview;

namespace Overmatch.Game;

/// <summary>Pause menu and the victory/defeat debrief, in the command-console style.</summary>
public partial class ResultOverlay : CanvasLayer
{
    public GameRoot Root { get; set; } = null!;

    private ColorRect _shade = null!;
    private Control _canvas = null!;
    private bool _paused;
    private bool _ended;
    private bool _won;
    private string _detail = "";

    public bool IsOpen => _shade.Visible;

    public override void _Ready()
    {
        Layer = 5;
        _shade = new ColorRect { Color = new Color(0.015f, .025f, .028f, .8f), Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_shade);
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _shade.AddChild(_canvas);
        GetViewport().SizeChanged += () => { if (_shade.Visible) Build(); };
    }

    private void Build()
    {
        var (_, height) = Ui.Fit(_canvas, GetViewport().GetVisibleRect().Size);
        Ui.Clear(_canvas);
        var w = Root.World;
        var faction = w.Player(Root.LocalPlayer).Faction.Id;
        var tall = _ended ? 548 : 472;
        var p = Ui.Panel(_canvas, 560, (height - tall) / 2, 480, tall, CommandTheme.Panel, true);
        var accent = !_ended ? CommandTheme.Gold : _won ? CommandTheme.Gold : CommandTheme.Red;
        Ui.Icon(p, Ui.FactionIcon(faction), 205, 30, 70, accent);
        Ui.Text(p, !_ended ? "OPERATIONS PAUSED" : _won ? "VICTORY" : "DEFEAT", 40, 112, 400, 56, _ended ? 52 : 40, _ended ? accent : CommandTheme.Text, true, HorizontalAlignment.Center);
        Ui.Text(p, _ended ? _detail : "Stand by for orders, Commander.", 30, 178, 420, 25, 18, CommandTheme.Muted, false, HorizontalAlignment.Center);
        var y = 226f;
        if (_ended)
        {
            var me = w.Player(Root.LocalPlayer);
            Ui.Rule(p, 55, y, 370, CommandTheme.Line.Darkened(.25f));
            Ui.Text(p, "DESTROYED", 55, y + 10, 120, 18, 12, CommandTheme.Muted, true);
            Ui.Text(p, me.UnitsKilled.ToString("N0"), 55, y + 28, 120, 28, 24, CommandTheme.Text, true);
            Ui.Text(p, "LOST", 180, y + 10, 120, 18, 12, CommandTheme.Muted, true);
            Ui.Text(p, me.UnitsLost.ToString("N0"), 180, y + 28, 120, 28, 24, CommandTheme.Text, true);
            Ui.Text(p, "INCOME", 305, y + 10, 120, 18, 12, CommandTheme.Muted, true);
            Ui.Text(p, $"$ {me.TotalEarned:N0}", 305, y + 28, 130, 28, 24, CommandTheme.Gold, true);
            y += 76;
        }
        Ui.Button(p, _ended ? "KEEP WATCHING" : "RESUME OPERATIONS", 55, y + 12, 370, 52, Close, true, 22);
        Ui.Button(p, "RETURN TO COMMAND", 55, y + 78, 370, 48, () => Root.App.ShowMenu(), false, 20);
        Ui.Button(p, "QUIT TO DESKTOP", 55, y + 136, 370, 40, () => GetTree().Quit(), false, 16);
        Ui.Text(p, _ended ? Ui.Clock(w.Time) + "  ELAPSED" : "ESC TO RESUME", 40, tall - 40, 400, 22, 12, CommandTheme.Muted, true, HorizontalAlignment.Center);
    }

    public void ShowResult(bool won, string detail)
    {
        _ended = true;
        _won = won;
        _detail = detail;
        _shade.Visible = true;
        Build();
    }

    public void TogglePause()
    {
        if (_shade.Visible) { Close(); return; }
        if (!_ended) { _paused = true; Root.Paused = true; }
        _shade.Visible = true;
        Build();
    }

    private void Close()
    {
        _shade.Visible = false;
        if (_paused) { _paused = false; Root.Paused = false; }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("cancel") && !Root.Placement.Active && !Root.Selection.AttackMoveArmed)
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
    }
}
