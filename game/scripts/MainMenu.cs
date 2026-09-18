using Godot;
using Overmatch.Game.UiReview;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Title screen and skirmish setup in the command-console style, bound to the real maps, factions and match settings.</summary>
public partial class MainMenu : CanvasLayer
{
    public App App { get; set; } = null!;
    /// <summary>Open straight on the skirmish setup page (used for screenshots).</summary>
    public bool StartOnSetup { get; set; }

    private static readonly string[] FactionIds = { "coalition", "directorate", "network", "random" };
    private static readonly string[] Difficulties = { "easy", "medium", "hard", "brutal" };
    private static readonly int[] CashOptions = { 5000, 10000, 20000, 50000 };
    private static readonly (string id, string name, string tag, string blurb, (string icon, string label)[] doctrine)[] Intel =
    {
        ("coalition", "COALITION", "PRECISION. SUPERIORITY. CONTROL.", "An elite force built around air power, drones,\nprecision fires and electronic warfare.", new[] { ("airfield", "AIR POWER"), ("attack", "PRECISION"), ("satellite", "INTELLIGENCE") }),
        ("directorate", "DIRECTORATE", "MASS. ARMOUR. INEVITABILITY.", "Hordes that fight harder together, heavy tanks,\nrocket artillery, cyber warfare and the bomb.", new[] { ("factory", "INDUSTRY"), ("defense", "ARMOUR"), ("reactor", "ATOMICS") }),
        ("network", "NETWORK", "UNSEEN. UNPOWERED. UNENDING.", "Cheap, hidden and everywhere: tunnels, salvage,\nimprovised drones and buildings that grow back.", new[] { ("command", "AMBUSH"), ("repair", "SALVAGE"), ("supply", "ATTRITION") }),
    };

    private Backdrop _backdrop = null!;
    private Control _canvas = null!;
    private Control _page = null!;
    private List<MapDef> _maps = new();
    private bool _setup;
    private int _intel;
    private double _intelTimer;
    private int _mapIndex;
    private int _cash = 1;
    private readonly int[] _controllers = { 0, 1, 2, 2 }; // 0 human, 1 AI, 2 closed
    private readonly int[] _factions = { 0, 3, 3, 3 };
    private readonly int[] _difficulty = { 1, 1, 1, 1 };

    public override void _Ready()
    {
        _maps = DataLoader.LoadRules().Maps.Values.OrderBy(m => m.Spawns.Count).ThenBy(m => m.Name).ToList();
        _mapIndex = Math.Max(0, _maps.FindIndex(m => m.Id == "plain"));
        _backdrop = new Backdrop();
        AddChild(_backdrop);
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_canvas);
        GetViewport().SizeChanged += Layout;
        _setup = StartOnSetup;
        Layout();
    }

    private void Layout()
    {
        var size = GetViewport().GetVisibleRect().Size;
        _backdrop.Size = size;
        Ui.Fit(_canvas, size);
        Show(_setup);
    }

    private void Show(bool setup)
    {
        _setup = setup;
        _backdrop.Setup = setup;
        _backdrop.QueueRedraw();
        if (_page is not null) { _canvas.RemoveChild(_page); _page.QueueFree(); }
        _page = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Size = _canvas.Size };
        _canvas.AddChild(_page);
        if (setup) BuildSetup(); else BuildTitle();
        var y = _canvas.Size.Y - 52;
        Ui.Text(_page, "OVERMATCH  /  COMMAND SYSTEMS", 65, 18, 720, 27, 16, CommandTheme.Muted, true);
        Ui.Text(_page, "PRE-ALPHA  /  OPEN SOURCE  /  GPL-3.0", 1030, 18, 505, 27, 16, CommandTheme.Gold, true, HorizontalAlignment.Right);
        Ui.Text(_page, "A FAN PROJECT, NOT AFFILIATED WITH ELECTRONIC ARTS", 65, y, 700, 27, 15, CommandTheme.Muted, true);
        Ui.Text(_page, "GITHUB.COM/XEEEO/OVERMATCH", 893, y, 643, 27, 15, CommandTheme.Muted, true, HorizontalAlignment.Right);
    }

    public override void _Process(double delta)
    {
        if (_setup) return;
        _intelTimer += delta;
        if (_intelTimer < 7) return;
        _intelTimer = 0;
        _intel = (_intel + 1) % Intel.Length;
        Show(false);
    }

    // ------------------------------------------------------------------ title

    private void BuildTitle()
    {
        Ui.Text(_page, "REAL-TIME STRATEGY  /  2030s", 70, 108, 670, 30, 19, CommandTheme.Gold, true);
        Ui.Text(_page, "OVERMATCH", 60, 141, 735, 154, 132, CommandTheme.Text, true);
        Ui.Text(_page, "THE NEXT WAR IS ALREADY HERE.", 70, 298, 680, 36, 27, CommandTheme.Muted, true);
        Ui.Text(_page, "Three doctrines. One battlefield.\nEstablish your command and decide what survives.", 72, 352, 630, 74, 21, CommandTheme.Muted, false, HorizontalAlignment.Left, wrap: true);

        var skirmish = Ui.Button(_page, "", 72, 481, 598, 76, () => Show(true), true);
        Ui.Text(skirmish, "01    SKIRMISH", 31, 8, 470, 33, 25, CommandTheme.Gold, true);
        Ui.Text(skirmish, "Fight computer commanders on six battlefields", 31, 42, 480, 24, 16, CommandTheme.Muted);
        Ui.Text(skirmish, "›", 545, 8, 32, 45, 34, CommandTheme.Gold, true);

        var quit = Ui.Button(_page, "", 72, 573, 598, 76, () => GetTree().Quit());
        Ui.Text(quit, "02    STAND DOWN", 31, 8, 480, 33, 25, CommandTheme.Text, true);
        Ui.Text(quit, "Quit to desktop", 31, 42, 482, 24, 16, CommandTheme.Muted);
        Ui.Text(quit, "›", 545, 8, 32, 45, 34, CommandTheme.Muted, true);

        var f = Intel[_intel];
        Ui.Text(_page, "FACTION INTELLIGENCE", 940, 111, 492, 30, 19, CommandTheme.Muted, true);
        Ui.Text(_page, $"0{_intel + 1} / 0{Intel.Length}", 1383, 114, 80, 25, 17, CommandTheme.Gold, true, HorizontalAlignment.Right);
        var emblem = Ui.Icon(_page, f.id, 1083, 193, 206, CommandTheme.Text);
        emblem.Size = new Vector2(206, 238);
        Ui.Text(_page, f.name, 932, 499, 524, 60, 56, CommandTheme.Text, true, HorizontalAlignment.Center);
        Ui.Text(_page, f.tag, 912, 567, 564, 30, 20, CommandTheme.Blue, true, HorizontalAlignment.Center);
        Ui.Text(_page, f.blurb, 930, 615, 532, 69, 21, CommandTheme.Muted, false, HorizontalAlignment.Center, wrap: true);
        for (var i = 0; i < f.doctrine.Length; i++)
        {
            var x = 918 + i * 192;
            Ui.Icon(_page, f.doctrine[i].icon, x + 63, 710, 30, CommandTheme.Gold.Darkened(.1f));
            Ui.Text(_page, f.doctrine[i].label, x, 754, 158, 27, 16, CommandTheme.Muted, true, HorizontalAlignment.Center);
        }
        var next = Ui.Button(_page, "NEXT  ›", 1396, 790, 140, 36, () => { _intel = (_intel + 1) % Intel.Length; _intelTimer = 0; Show(false); }, false, 14);
    }

    // ------------------------------------------------------------------ skirmish setup

    private void BuildSetup()
    {
        var map = _maps[_mapIndex];
        Ui.Text(_page, "OPERATIONS / LOCAL ENGAGEMENT", 68, 86, 990, 28, 18, CommandTheme.Gold, true);
        Ui.Text(_page, "SKIRMISH", 63, 113, 1120, 82, 70, CommandTheme.Text, true);
        Ui.Text(_page, "Set the battlefield. Choose your doctrine.", 68, 198, 1000, 31, 22, CommandTheme.Muted);

        var left = new ConsolePanel { Position = new Vector2(64, 255), Size = new Vector2(500, 475), Fill = new Color("172124"), Edge = CommandTheme.Line.Darkened(.25f), Accent = true };
        var right = new ConsolePanel { Position = new Vector2(592, 255), Size = new Vector2(944, 475), Fill = new Color("172124"), Edge = CommandTheme.Line.Darkened(.25f) };
        _page.AddChild(left);
        _page.AddChild(right);
        Ui.Text(_page, "01 / BATTLEFIELD", 90, 273, 430, 28, 19, CommandTheme.Gold, true);
        Ui.Text(_page, "02 / COMMANDERS", 618, 273, 640, 28, 19, CommandTheme.Gold, true);

        var active = Enumerable.Range(0, 4).Where(i => i < map.Spawns.Count && _controllers[i] != 2).ToList();
        _page.AddChild(new MapSchematic { Position = new Vector2(90, 312), Size = new Vector2(446, 232), Map = map, ActiveSlots = active });
        var picker = Ui.Picker(_page, _maps.Select(m => m.Name).ToArray(), _mapIndex, 90, 556, 446, 44, 20);
        picker.ItemSelected += n => { _mapIndex = (int)n; Show(true); };
        Ui.Text(_page, $"{map.Spawns.Count} POSITIONS   /   {map.Width} × {map.Height}   /   {map.Supplies.Count} SUPPLY PILES   /   {map.Neutrals.Count(n => n.Id == "oil_derrick")} DERRICKS", 92, 612, 440, 27, 14, CommandTheme.Muted, true);
        Ui.Text(_page, map.Description, 92, 644, 440, 70, 17, CommandTheme.Muted, false, HorizontalAlignment.Left, wrap: true);

        Ui.Text(_page, "SLOT", 619, 322, 67, 27, 15, CommandTheme.Muted, true);
        Ui.Text(_page, "COMMAND", 701, 322, 190, 27, 15, CommandTheme.Muted, true);
        Ui.Text(_page, "FACTION", 929, 322, 230, 27, 15, CommandTheme.Muted, true);
        Ui.Text(_page, "DIFFICULTY", 1190, 322, 270, 27, 15, CommandTheme.Muted, true);
        for (var i = 0; i < 4; i++) BuildSlot(i, 359 + i * 70, map);

        Ui.Text(_page, "STARTING FUNDS", 619, 663, 203, 28, 16, CommandTheme.Muted, true);
        var cash = Ui.Picker(_page, CashOptions.Select(c => $"$ {c:N0}").ToArray(), _cash, 809, 653, 194);
        cash.ItemSelected += n => _cash = (int)n;
        Ui.Text(_page, "EASY IS HANDICAPPED  ·  BRUTAL CHEATS ON INCOME", 1030, 663, 478, 27, 13, CommandTheme.Muted.Darkened(.1f), true, HorizontalAlignment.Right);

        Ui.Button(_page, "‹    BACK", 64, 761, 212, 64, () => Show(false), false, 24);
        var ais = active.Count(i => _controllers[i] == 1);
        Ui.Text(_page, ais > 0 ? "READY TO DEPLOY" : "NO OPPONENT", 626, 765, 490, 27, 18, ais > 0 ? CommandTheme.Text : CommandTheme.Red, true);
        Ui.Text(_page, ais > 0 ? $"You against {ais} computer commander{(ais == 1 ? "" : "s")} on {map.Name}." : "Open at least one AI Commander slot.", 626, 793, 540, 28, 17, CommandTheme.Muted);
        var deploy = Ui.Button(_page, "DEPLOY    ›", 1194, 761, 342, 64, Start, true, 24);
        deploy.Disabled = ais == 0;
    }

    private void BuildSlot(int index, float y, MapDef map)
    {
        var available = index < map.Spawns.Count;
        if (!available) _controllers[index] = 2;
        var active = _controllers[index] != 2;
        var colour = MatchSettings.Palette[index % MatchSettings.Palette.Length];
        _page.AddChild(new ColorRect { Position = new Vector2(619, y + 9), Size = new Vector2(5, 34), Color = active ? colour : colour.Darkened(.6f), MouseFilter = Control.MouseFilterEnum.Ignore });
        Ui.Text(_page, $"0{index + 1}", 640, y + 5, 50, 40, 24, active ? CommandTheme.Text : CommandTheme.Muted.Darkened(.3f), true);

        var command = Ui.Picker(_page, new[] { "Human", "AI Commander", "Closed" }, _controllers[index], 701, y, 203);
        if (index == 0 || !available) command.Disabled = true; else command.SetItemDisabled(0, true);
        if (!available) command.Text = "No start position";
        command.ItemSelected += n => { _controllers[index] = (int)n; Show(true); };

        var faction = Ui.Picker(_page, new[] { "Coalition", "Directorate", "Network", "Random" }, _factions[index], 929, y, 236);
        faction.Disabled = !active;
        faction.ItemSelected += n => _factions[index] = (int)n;

        var difficulty = Ui.Picker(_page, new[] { "Easy", "Medium", "Hard", "Brutal" }, _difficulty[index], 1190, y, 318);
        difficulty.Disabled = _controllers[index] != 1;
        if (_controllers[index] == 0) difficulty.Text = "PLAYER CONTROL";
        if (!active) difficulty.Text = "—";
        difficulty.ItemSelected += n => _difficulty[index] = (int)n;
    }

    private void Start()
    {
        var map = _maps[_mapIndex];
        var rng = new Random();
        string Pick(int idx) => FactionIds[idx] == "random" ? FactionIds[rng.Next(3)] : FactionIds[idx];
        var s = new MatchSettings { StartingCash = CashOptions[_cash], MapId = map.Id };
        var n = 0;
        for (var i = 0; i < 4 && i < map.Spawns.Count; i++)
        {
            if (_controllers[i] == 2) continue;
            var fac = Pick(_factions[i]);
            var facName = char.ToUpper(fac[0]) + fac[1..];
            if (_controllers[i] == 0) s.Players.Add(new PlayerSlot { Name = "You", Faction = fac, Colour = MatchSettings.Palette[0] });
            else
            {
                n++;
                var diff = Difficulties[_difficulty[i]];
                s.Players.Add(new PlayerSlot { Name = $"{facName} AI ({diff})", Faction = fac, IsAi = true, Difficulty = diff, Colour = MatchSettings.Palette[n % MatchSettings.Palette.Length] });
            }
        }
        if (s.Players.Count(p => p.IsAi) == 0) return;
        App.StartMatch(s);
    }
}

/// <summary>Graphite backdrop with a faint plotting grid and contour lines, drawn as vectors.</summary>
public partial class Backdrop : Control
{
    public bool Setup { get; set; }

    public override void _Ready() => MouseFilter = MouseFilterEnum.Stop;

    public override void _Draw()
    {
        float w = Size.X, h = Size.Y;
        var k = Math.Min(w / Ui.DesignWidth, h / Ui.MinDesignHeight);
        DrawRect(new Rect2(0, 0, w, h), new Color("0d1417"));
        for (var x = 0f; x < w; x += 8)
        {
            var glow = Mathf.Sin(x / w * Mathf.Pi) * .035f;
            DrawRect(new Rect2(x, 0, 8, h), new Color(.045f + glow, .064f + glow, .07f + glow));
        }
        for (var x = 40f * k; x < w; x += 80 * k) DrawLine(new Vector2(x, 0), new Vector2(x, h), new Color(.65f, .75f, .75f, .025f));
        for (var y = 20f * k; y < h; y += 80 * k) DrawLine(new Vector2(0, y), new Vector2(w, y), new Color(.65f, .75f, .75f, .025f));
        var ox = (w - Ui.DesignWidth * k) / 2f;
        for (var i = 0; i < 7; i++)
        {
            var d = i * 29f;
            Vector2[] contour = { new(570 + d, 1000), new(640 + d, 700), new(770 + d, 640), new(740 + d, 480), new(850 + d, 360), new(920 + d, 140), new(1100 + d, 0) };
            DrawPolyline(contour.Select(p => new Vector2(ox + p.X * k, p.Y * k)).ToArray(), new Color(.5f, .61f, .61f, .045f), 1, true);
        }
        Vector2 P(float x, float y) => new(ox + x * k, y * k);
        DrawLine(P(64, 54), P(1536, 54), CommandTheme.Line.Darkened(.48f), 1);
        DrawLine(P(64, 54), P(146, 54), CommandTheme.Gold, 3);
        if (Setup) return;
        DrawLine(P(825, 126), P(825, 774), CommandTheme.Line.Darkened(.45f), 1);
        DrawCircle(P(1186, 315), 165 * k, new Color(.45f, .65f, .72f, .025f));
        DrawArc(P(1186, 315), 175 * k, 0, Mathf.Tau, 96, new Color(.45f, .65f, .72f, .1f), 1, true);
        DrawArc(P(1186, 315), 183 * k, -.35f, .9f, 24, new Color(.91f, .74f, .4f, .55f), 2, true);
    }
}

/// <summary>Schematic of any map definition: terrain, supplies, derricks and numbered start positions.</summary>
public partial class MapSchematic : Control
{
    public MapDef Map { get; set; } = null!;
    public List<int> ActiveSlots { get; set; } = new();

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("101a1e"));
        var scale = Math.Min((Size.X - 60) / Map.Width, (Size.Y - 24) / Map.Height);
        var origin = new Vector2((Size.X - Map.Width * scale) * .5f, (Size.Y - Map.Height * scale) * .5f);
        Vector2 P(float x, float y) => origin + new Vector2(x, Map.Height - y) * scale;
        void Block(RectDef r, Color c) => DrawRect(new Rect2(P(r.X, r.Y + r.H), new Vector2(r.W, r.H) * scale), c);
        DrawRect(new Rect2(origin, new Vector2(Map.Width, Map.Height) * scale), new Color("293330"));
        foreach (var r in Map.Road) Block(r, new Color("687365"));
        foreach (var r in Map.Rough) Block(r, new Color("43483b"));
        foreach (var r in Map.Water) Block(r, new Color("264650"));
        foreach (var r in Map.Blocked) Block(r, new Color("758076"));
        for (var c = 0; c <= Map.Width; c += 16) DrawLine(P(c, 0), P(c, Map.Height), new Color(1, 1, 1, .055f));
        for (var c = 0; c <= Map.Height; c += 16) DrawLine(P(0, c), P(Map.Width, c), new Color(1, 1, 1, .055f));
        foreach (var s in Map.Supplies) DrawRect(new Rect2(P(s.X, s.Y) - new Vector2(2, 2), new Vector2(4, 4)), CommandTheme.Gold);
        foreach (var n in Map.Neutrals)
            if (n.Id == "oil_derrick") DrawArc(P(n.X + 1.5f, n.Y + 1.5f), 4, 0, Mathf.Tau, 12, CommandTheme.Blue, 1.5f, true);
            else DrawRect(new Rect2(P(n.X, n.Y + 3), new Vector2(3, 3) * scale), new Color("5a6562"));
        for (var i = 0; i < Map.Spawns.Count; i++)
        {
            var on = ActiveSlots.Contains(i);
            var colour = on ? MatchSettings.Palette[i % MatchSettings.Palette.Length].Lightened(.2f) : CommandTheme.Muted.Darkened(.3f);
            var at = P(Map.Spawns[i].X, Map.Spawns[i].Y);
            DrawCircle(at, 11, CommandTheme.Ink);
            DrawArc(at, 11, 0, Mathf.Tau, 24, colour, on ? 2 : 1, true);
            DrawString(CommandTheme.Display, at + new Vector2(-4, 6), $"{i + 1}", HorizontalAlignment.Left, -1, 17, colour);
        }
        DrawRect(new Rect2(origin, new Vector2(Map.Width, Map.Height) * scale), CommandTheme.Line, false);
        DrawString(CommandTheme.Display, new Vector2(10, 20), "N", HorizontalAlignment.Left, -1, 15, CommandTheme.Muted);
        DrawLine(new Vector2(14, 27), new Vector2(14, 46), CommandTheme.Muted);
        DrawLine(new Vector2(14, 27), new Vector2(10, 34), CommandTheme.Muted);
        DrawLine(new Vector2(14, 27), new Vector2(18, 34), CommandTheme.Muted);
    }
}
