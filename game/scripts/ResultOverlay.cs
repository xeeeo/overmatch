using Godot;

namespace Overmatch.Game;

/// <summary>Victory/defeat banner and the Esc pause menu.</summary>
public partial class ResultOverlay : CanvasLayer
{
    public GameRoot Root { get; set; } = null!;

    private Control _panel = null!;
    private Label _title = null!;
    private Label _sub = null!;
    private Button _continue = null!;
    private bool _paused;
    private bool _ended;

    public override void _Ready()
    {
        Layer = 5;
        var shade = new ColorRect { Color = new Color(0, 0, 0, 0.55f), Visible = false };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(shade);
        _panel = shade;

        var centre = new CenterContainer();
        centre.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        shade.AddChild(centre);
        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 12);
        centre.AddChild(box);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 56);
        box.AddChild(_title);
        _sub = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.75f) };
        box.AddChild(_sub);
        box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
        _continue = MakeButton("Continue", () => Hide());
        box.AddChild(_continue);
        box.AddChild(MakeButton("Quit to menu", () => Root.App.ShowMenu()));
        box.AddChild(MakeButton("Quit game", () => GetTree().Quit()));
    }

    private static Button MakeButton(string text, Action onPress)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(220, 40) };
        b.Pressed += onPress;
        return b;
    }

    public void ShowResult(bool won, string detail)
    {
        _ended = true;
        _title.Text = won ? "VICTORY" : "DEFEAT";
        _title.Modulate = won ? new Color(0.95f, 0.85f, 0.45f) : new Color(0.9f, 0.35f, 0.3f);
        _sub.Text = detail;
        _continue.Text = "Keep watching";
        _panel.Visible = true;
    }

    public void TogglePause()
    {
        if (_ended && _panel.Visible) { Hide(); return; }
        if (_panel.Visible) { Hide(); return; }
        _paused = true;
        Root.Paused = true;
        _title.Text = "PAUSED";
        _title.Modulate = Colors.White;
        _sub.Text = "";
        _continue.Text = "Resume";
        _panel.Visible = true;
    }

    private void Hide()
    {
        _panel.Visible = false;
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
