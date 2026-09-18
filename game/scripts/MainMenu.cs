using Godot;

namespace Overmatch.Game;

/// <summary>Title screen and skirmish setup, built from Controls in code.</summary>
public partial class MainMenu : CanvasLayer
{
    public App App { get; set; } = null!;

    private Control _root = null!;
    private VBoxContainer _setup = null!;
    private VBoxContainer _title = null!;
    private readonly List<(OptionButton kind, OptionButton difficulty)> _slots = new();
    private OptionButton _cash = null!;

    public override void _Ready()
    {
        _root = new ColorRect { Color = new Color(0.08f, 0.10f, 0.09f) };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        var centre = new CenterContainer();
        centre.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(centre);

        _title = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _title.AddThemeConstantOverride("separation", 14);
        centre.AddChild(_title);
        var h = new Label { Text = "OVERMATCH", HorizontalAlignment = HorizontalAlignment.Center };
        h.AddThemeFontSizeOverride("font_size", 64);
        h.Modulate = new Color(0.95f, 0.85f, 0.45f);
        _title.AddChild(h);
        var sub = new Label { Text = "an open-source RTS in the spirit of Generals: Zero Hour\npre-alpha", HorizontalAlignment = HorizontalAlignment.Center };
        sub.Modulate = new Color(1, 1, 1, 0.6f);
        _title.AddChild(sub);
        _title.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });
        _title.AddChild(Button("Skirmish", () => { _title.Visible = false; _setup.Visible = true; }));
        _title.AddChild(Button("Quit", () => GetTree().Quit()));

        _setup = new VBoxContainer { Visible = false };
        _setup.AddThemeConstantOverride("separation", 10);
        centre.AddChild(_setup);
        var st = new Label { Text = "SKIRMISH", HorizontalAlignment = HorizontalAlignment.Center };
        st.AddThemeFontSizeOverride("font_size", 36);
        _setup.AddChild(st);
        _setup.AddChild(new Label { Text = "Map: Dry Plain (4 players)   Faction: Coalition (others arrive in M4)", Modulate = new Color(1, 1, 1, 0.7f) });

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 6);
        _setup.AddChild(grid);
        for (var i = 0; i < 4; i++)
        {
            var slotLabel = new Label { Text = i == 0 ? "Slot 1  (you)" : $"Slot {i + 1}", CustomMinimumSize = new Vector2(120, 0) };
            grid.AddChild(slotLabel);
            var kind = new OptionButton();
            if (i == 0) { kind.AddItem("Human"); kind.Disabled = true; }
            else
            {
                kind.AddItem("Closed");
                kind.AddItem("AI");
                kind.Selected = i == 1 ? 1 : 0;
            }
            grid.AddChild(kind);
            var diff = new OptionButton();
            foreach (var d in new[] { "Easy", "Medium", "Hard", "Brutal" }) diff.AddItem(d);
            diff.Selected = 1;
            diff.Disabled = i == 0;
            grid.AddChild(diff);
            _slots.Add((kind, diff));
        }
        var cashRow = new HBoxContainer();
        cashRow.AddChild(new Label { Text = "Starting cash" });
        _cash = new OptionButton();
        foreach (var c in new[] { 5000, 10000, 20000, 50000 }) _cash.AddItem($"${c:N0}");
        _cash.Selected = 1;
        cashRow.AddChild(_cash);
        _setup.AddChild(cashRow);
        _setup.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
        _setup.AddChild(Button("Start", Start));
        _setup.AddChild(Button("Back", () => { _setup.Visible = false; _title.Visible = true; }));
    }

    private static Button Button(string text, Action onPress)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(240, 44) };
        b.AddThemeFontSizeOverride("font_size", 18);
        b.Pressed += onPress;
        return b;
    }

    private void Start()
    {
        var s = new MatchSettings { StartingCash = new[] { 5000, 10000, 20000, 50000 }[_cash.Selected] };
        s.Players.Add(new PlayerSlot { Name = "You", Colour = MatchSettings.Palette[0] });
        var n = 1;
        for (var i = 1; i < _slots.Count; i++)
        {
            if (_slots[i].kind.Selected != 1) continue;
            var diff = new[] { "easy", "medium", "hard", "brutal" }[_slots[i].difficulty.Selected];
            s.Players.Add(new PlayerSlot { Name = $"AI {n} ({diff})", IsAi = true, Difficulty = diff, Colour = MatchSettings.Palette[n % 4] });
            n++;
        }
        if (s.Players.Count < 2) s.Players.Add(new PlayerSlot { Name = "AI 1 (medium)", IsAi = true, Colour = MatchSettings.Palette[1] });
        App.StartMatch(s);
    }
}
