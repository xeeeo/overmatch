using Godot;

namespace Overmatch.Game;

/// <summary>Title screen and skirmish setup, built from Controls in code.</summary>
public partial class MainMenu : CanvasLayer
{
    public App App { get; set; } = null!;

    private Control _root = null!;
    private VBoxContainer _setup = null!;
    private VBoxContainer _title = null!;
    private readonly List<(OptionButton kind, OptionButton faction, OptionButton difficulty)> _slots = new();
    private static readonly string[] FactionIds = { "coalition", "directorate", "network", "random" };
    private OptionButton _cash = null!;
    private OptionButton _map = null!;
    private Label _mapInfo = null!;
    private List<Overmatch.Sim.Data.MapDef> _maps = new();

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
        _maps = DataLoader.LoadRules().Maps.Values.OrderBy(m => m.Spawns.Count).ThenBy(m => m.Name).ToList();
        var mapRow = new HBoxContainer();
        mapRow.AddChild(new Label { Text = "Map", CustomMinimumSize = new Vector2(120, 0) });
        _map = new OptionButton();
        foreach (var m in _maps) _map.AddItem(m.Name);
        _map.Selected = Math.Max(0, _maps.FindIndex(m => m.Id == "plain"));
        _map.ItemSelected += _ => MapChanged();
        mapRow.AddChild(_map);
        _setup.AddChild(mapRow);
        _mapInfo = new Label { Modulate = new Color(1, 1, 1, 0.7f) };
        _setup.AddChild(_mapInfo);

        var grid = new GridContainer { Columns = 4 };
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
            var fac = new OptionButton();
            foreach (var f in new[] { "Coalition", "Directorate", "Network", "Random" }) fac.AddItem(f);
            fac.Selected = i == 0 ? 0 : 3;
            grid.AddChild(fac);
            var diff = new OptionButton();
            foreach (var d in new[] { "Easy", "Medium", "Hard", "Brutal" }) diff.AddItem(d);
            diff.Selected = 1;
            diff.Disabled = i == 0;
            grid.AddChild(diff);
            _slots.Add((kind, fac, diff));
        }
        var cashRow = new HBoxContainer();
        cashRow.AddChild(new Label { Text = "Starting cash" });
        _cash = new OptionButton();
        foreach (var c in new[] { 5000, 10000, 20000, 50000 }) _cash.AddItem($"${c:N0}");
        _cash.Selected = 1;
        cashRow.AddChild(_cash);
        _setup.AddChild(cashRow);
        _setup.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
        MapChanged();
        _setup.AddChild(Button("Start", Start));
        _setup.AddChild(Button("Back", () => { _setup.Visible = false; _title.Visible = true; }));
    }

    private void MapChanged()
    {
        var m = _maps[_map.Selected];
        _mapInfo.Text = $"{m.Width}x{m.Height}, up to {m.Spawns.Count} players. {m.Description}";
        for (var i = 1; i < _slots.Count; i++)
        {
            var allowed = i < m.Spawns.Count;
            _slots[i].kind.Disabled = !allowed;
            if (!allowed) _slots[i].kind.Selected = 0;
        }
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
        var s = new MatchSettings { StartingCash = new[] { 5000, 10000, 20000, 50000 }[_cash.Selected], MapId = _maps[_map.Selected].Id };
        var rng = new Random();
        string Pick(int idx) => FactionIds[idx] == "random" ? FactionIds[rng.Next(3)] : FactionIds[idx];
        s.Players.Add(new PlayerSlot { Name = "You", Faction = Pick(_slots[0].faction.Selected), Colour = MatchSettings.Palette[0] });
        var n = 1;
        for (var i = 1; i < _slots.Count; i++)
        {
            if (_slots[i].kind.Selected != 1) continue;
            var diff = new[] { "easy", "medium", "hard", "brutal" }[_slots[i].difficulty.Selected];
            var fac = Pick(_slots[i].faction.Selected);
            s.Players.Add(new PlayerSlot { Name = $"AI {n} ({fac} {diff})", Faction = fac, IsAi = true, Difficulty = diff, Colour = MatchSettings.Palette[n % 4] });
            n++;
        }
        if (s.Players.Count < 2) s.Players.Add(new PlayerSlot { Name = "AI 1 (coalition medium)", IsAi = true, Colour = MatchSettings.Palette[1] });
        App.StartMatch(s);
    }
}
