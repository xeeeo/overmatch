using Godot;
using Overmatch.Sim.Data;

namespace Overmatch.Game.UiReview;

/// <summary>
/// Isolated, interactive art-direction review. All numbers and queues are fixtures;
/// no commands are submitted to the game. Launch this scene explicitly to review.
/// </summary>
public partial class CommandReview : Node
{
    private ReviewWorld _world = null!;
    private Control _canvas = null!;
    private Control _hud = null!;
    private Control? _modal;
    private Control? _tooltip;
    private Label? _notice;
    private TacticalRadar? _radar;
    private float _height = 1000;
    private string _screen = "hud";
    private string _state = "construction";
    private string? _capture;
    private int _frames;
    private string _order = "Awaiting orders";
    private readonly List<string> _queue = new() { "coalition_bulwark", "coalition_warden" };
    private float _queueProgress = .42f;
    private double _noticeTimer;
    private bool _interactiveQueue;
    private string? _selectedCard;
    private Label? _queueCaption;
    private ProgressBar? _queueMeter;

    public override void _Ready()
    {
        GetWindow().Title = "Overmatch — UI review";
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--ui-screen=")) _screen = arg[12..];
            if (arg.StartsWith("--ui-state=")) _state = arg[11..];
            if (arg.StartsWith("--ui-capture=")) _capture = arg[13..];
        }
        _world = new ReviewWorld(); AddChild(_world);
        var layer = new CanvasLayer { Layer = 10 }; AddChild(layer);
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_canvas);
        GetViewport().SizeChanged += Resize;
        Resize();
        GD.Print("[UI review] Isolated scene ready. F1 construction, F2 production, F3 low power, F4 menu, Esc pause.");
    }

    private void Resize()
    {
        var size = GetViewport().GetVisibleRect().Size;
        var scale = Math.Min(size.X / 1600f, size.Y / 900f);
        _height = Math.Max(900, size.Y / scale);
        _canvas.Scale = Vector2.One * scale;
        _canvas.Size = new Vector2(1600, _height);
        _canvas.Position = new Vector2((size.X - 1600 * scale) / 2, 0);
        if (_modal is ReviewFrontEnd front) { front.Size = _canvas.Size; return; }
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (var child in _canvas.GetChildren()) { _canvas.RemoveChild(child); child.QueueFree(); }
        _modal = null; _tooltip = null; _notice = null; _queueCaption = null; _queueMeter = null;
        _hud = new Control { Size = _canvas.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(_hud);
        _world.Select(_state == "production" ? "coalition_motor_pool" : "coalition_dozer");
        BuildHeader(); BuildField(); BuildConsole(); BuildReviewStrip();
        if (_screen is "menu" or "setup") ShowFrontEnd(_screen == "setup");
        if (_screen == "pause") ShowPause();
    }

    private void BuildHeader()
    {
        Panel(_hud, 0, 0, 1600, 60, new Color("11191bf5"), false, 0);
        Rule(_hud, 0, 59, 1600, CommandTheme.Line);
        Icon(_hud, "coalition", 22, 9, 42, CommandTheme.Gold);
        Text(_hud, "OVERMATCH", 76, 8, 205, 35, 30, CommandTheme.Text, true);
        Text(_hud, "COALITION COMMAND", 262, 22, 210, 20, 14, CommandTheme.Muted, true);
        Rule(_hud, 480, 12, 1, CommandTheme.Line, 36);
        Text(_hud, "CREDITS", 506, 7, 90, 18, 12, CommandTheme.Muted, true);
        Text(_hud, "$ 12,500", 506, 23, 155, 32, 27, CommandTheme.Gold, true);
        Icon(_hud, "reactor", 670, 15, 28, _state == "lowpower" ? CommandTheme.Red : CommandTheme.Green);
        Text(_hud, "POWER GRID", 712, 7, 140, 18, 12, CommandTheme.Muted, true);
        Text(_hud, _state == "lowpower" ? "40 / 20   OVERLOAD" : "60 / 80   STABLE", 712, 25, 170, 25, 20, _state == "lowpower" ? CommandTheme.Red : CommandTheme.Green, true);
        Meter(_hud, 903, 25, 126, 9, _state == "lowpower" ? 1 : .75f, _state == "lowpower" ? CommandTheme.Red : CommandTheme.Green, 16);
        Icon(_hud, "chevron", 1070, 13, 32, CommandTheme.Gold);
        Text(_hud, "FIELD COMMANDER", 1112, 7, 170, 18, 12, CommandTheme.Muted, true);
        Text(_hud, "RANK 3  /  1 POINT", 1112, 25, 177, 24, 20, CommandTheme.Text, true);
        Text(_hud, "08:42", 1322, 15, 88, 30, 25, CommandTheme.Muted, true);
        Button(_hud, "MENU   ESC", 1440, 12, 138, 36, ShowPause);
    }

    private void BuildField()
    {
        Panel(_hud, 20, 80, 260, 62, new Color("111a1bdd"));
        Text(_hud, "DRY PLAIN", 36, 86, 205, 27, 24, CommandTheme.Text, true);
        Text(_hud, "SKIRMISH  /  COALITION SECTOR", 36, 116, 226, 18, 12, CommandTheme.Muted, true);

        Panel(_hud, 1320, 84, 260, 154, new Color("111a1be8"), true);
        Text(_hud, "SUPPORT POWERS", 1338, 96, 216, 22, 17, CommandTheme.Muted, true);
        Power("satellite", "SPY SATELLITE", "READY", 1336, 126, true);
        Power("attack", "PRECISION STRIKE", "01:24", 1336, 176, false);
        Panel(_hud, 1338, 252, 242, 48, new Color("111a1bdc"));
        Icon(_hud, "satellite", 1348, 261, 26, CommandTheme.Gold);
        Text(_hud, "ORBITAL STRIKE", 1385, 257, 145, 18, 14, CommandTheme.Muted, true);
        Text(_hud, "CHARGING   03:12", 1385, 275, 170, 20, 17, CommandTheme.Gold, true);

        if (_state == "lowpower")
        {
            Panel(_hud, 560, 138, 470, 67, new Color("39251ff0"), true);
            Icon(_hud, "reactor", 574, 152, 36, CommandTheme.Red);
            Text(_hud, "LOW POWER", 626, 145, 365, 24, 22, CommandTheme.Red, true);
            Text(_hud, "Production slowed. Restore power to bring defenses online.", 626, 174, 385, 22, 14, CommandTheme.Text);
        }
        _notice = Text(_hud, "", 420, 220, 760, 40, 20, CommandTheme.Gold, true);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
    }

    private void Power(string glyph, string title, string value, float x, float y, bool ready)
    {
        var b = Button(_hud, "", x, y, 228, 44, () => Notify("Spy Satellite selected — target selection preview"));
        b.Disabled = !ready;
        Icon(b, glyph, 9, 7, 29, ready ? CommandTheme.Gold : CommandTheme.Muted);
        Text(b, title, 48, 4, 165, 19, 16, ready ? CommandTheme.Text : CommandTheme.Muted, true);
        Text(b, value, 48, 24, 163, 16, 12, ready ? CommandTheme.Green : CommandTheme.Gold, true);
    }

    private void BuildConsole()
    {
        var y = _height - 285;
        Panel(_hud, 0, y - 10, 1600, 295, new Color("131c1e"), false, 0);
        Rule(_hud, 0, y - 10, 1600, CommandTheme.Line.Lightened(.15f), 2);
        Rule(_hud, 0, y - 7, 1600, CommandTheme.Ink, 4);

        // Radar in a dedicated physical housing.
        Panel(_hud, 18, y + 10, 254, 241, CommandTheme.Raised.Darkened(.17f), true);
        Text(_hud, "TACTICAL RADAR", 36, y + 17, 175, 24, 18, CommandTheme.Text, true);
        Text(_hud, "N", 237, y + 20, 18, 20, 15, CommandTheme.Gold, true);
        _radar = new TacticalRadar { Position = new Vector2(36, y + 47), Size = new Vector2(218, 182), World = _world.World, FocusNormalized = _world.MapFocus };
        _radar.Navigated += _world.FocusMap;
        _hud.AddChild(_radar);
        Text(_hud, "96 × 96", 35, y + 229, 98, 17, 11, CommandTheme.Muted, true);
        Button(_hud, "CENTER BASE", 145, y + 229, 110, 18, () => { _world.FocusBase(); _radar.FocusNormalized = _world.MapFocus; }, small: true);

        // Selection dossier.
        var production = _state == "production";
        Panel(_hud, 286, y + 10, 300, 241, CommandTheme.Panel, true);
        Text(_hud, production ? "MOTOR POOL" : "DOZER", 302, y + 16, 205, 34, 28, CommandTheme.Text, true);
        Text(_hud, production ? "VEHICLE PRODUCTION" : "CONSTRUCTION VEHICLE", 303, y + 50, 251, 19, 12, CommandTheme.Muted, true);
        Text(_hud, "01", 542, y + 20, 30, 25, 22, CommandTheme.Gold, true);
        Panel(_hud, 302, y + 77, 148, 98, CommandTheme.Ink);
        AddPortrait(_hud, production ? "coalition/motor_pool" : "coalition/dozer", 304, y + 75, 144, 101, production ? 13 : 6.5f);
        Text(_hud, "CONDITION", 467, y + 79, 95, 17, 11, CommandTheme.Muted, true);
        Text(_hud, production ? "2,500 / 2,500" : "300 / 300", 467, y + 101, 113, 23, 18, CommandTheme.Text, true);
        Meter(_hud, 467, y + 129, 100, 6, 1, CommandTheme.Green, 12);
        Text(_hud, production ? "OPERATIONAL" : "READY", 467, y + 146, 110, 18, 12, CommandTheme.Green, true);
        Text(_hud, production ? "Armored vehicles & field upgrades." : "Builds and repairs your base.", 303, y + 183, 265, 22, 15, CommandTheme.Muted);
        MiniOrder("stop", "STOP", 302, y + 213, () => { _order = "Awaiting orders"; Notify("Stop order preview"); });
        MiniOrder(production ? "rally" : "repair", production ? "RALLY" : "REPAIR", 392, y + 213, () => Notify(production ? "Rally point selection preview" : "Repair target selection preview"));
        MiniOrder(production ? "sell" : "attack", production ? "SELL" : "MOVE", 483, y + 213, () => Notify(production ? "Sell action preview — no structure sold" : "Move order preview"));

        // Contextual command cards, with a consistent location for costs and hotkeys.
        Text(_hud, production ? "PRODUCTION" : "CONSTRUCTION", 607, y + 8, 320, 24, 20, CommandTheme.Text, true);
        Text(_hud, production ? "SELECT A UNIT TO QUEUE" : "SELECT A STRUCTURE", 984, y + 12, 257, 20, 12, CommandTheme.Muted, true).HorizontalAlignment = HorizontalAlignment.Right;
        var buildings = new[] { "power_plant", "barracks", "supply_center", "motor_pool", "airfield", "sentry_battery", "strategy_center", "drop_zone", "orbital_uplink" };
        var units = new[] { "warden", "bulwark", "lancer", "jammer", "hive" };
        if (production)
        {
            for (int i = 0; i < units.Length; i++)
            {
                var def = _world.World.Rules.Unit("coalition_" + units[i]);
                BuildCard(def, 605 + i * 127, y + 42, i + 1, i >= 3, "Requires Strategy Center");
            }
            UpgradeCard("coalition_composite_armour", "research", 605, y + 148);
            UpgradeCard("coalition_guided_rounds", "attack", 859, y + 148);
        }
        else
        {
            for (int i = 0; i < buildings.Length; i++)
            {
                var def = _world.World.Rules.Building("coalition_" + buildings[i]);
                BuildCard(def, 605 + (i % 5) * 127, y + 42 + (i / 5) * 106, i + 1, i == 8, "Requires Strategy Center");
            }
            var empty = Panel(_hud, 1113, y + 148, 119, 98, CommandTheme.Ink);
            Icon(empty, "coalition", 38, 21, 43, CommandTheme.Line);
            Text(empty, "COALITION", 13, 73, 94, 15, 11, CommandTheme.Line, true).HorizontalAlignment = HorizontalAlignment.Center;
        }
        BuildQueue(y, production);
    }

    private void MiniOrder(string icon, string title, float x, float y, Action action)
    {
        var b = Button(_hud, "", x, y, 83, 27, action);
        Icon(b, icon, 5, 5, 17, CommandTheme.Muted);
        Text(b, title, 28, 4, 52, 20, 12, CommandTheme.Text, true);
    }

    private void BuildCard(ObjectDef def, float x, float y, int key, bool locked, string reason)
    {
        var b = Button(_hud, "", x, y, 119, 98, () =>
        {
            _selectedCard = def.Id;
            if (_state == "production")
            {
                if (_queue.Count >= 5) { Notify("Production queue full — cancel a queued unit to make room"); return; }
                _queue.Add(def.Id); _interactiveQueue = true;
            }
            else _order = def.Name;
            Rebuild();
            if (_state == "production") Notify($"{def.Name} added to the preview queue");
        }, _selectedCard == def.Id);
        b.Disabled = locked;
        b.MouseEntered += () => ShowTooltip(def.Name, locked ? reason : def.Description, $"$ {def.Cost:N0}   /   {def.BuildTime:0} SEC", x);
        b.MouseExited += HideTooltip;
        Panel(b, 4, 4, 111, 55, locked ? CommandTheme.Ink : new Color("202e31"));
        var portrait = AddPortrait(b, def.Model, 10, -3, 101, 67, def.IsBuilding ? def.Id.EndsWith("airfield") ? 17 : 12 : 7);
        if (locked) portrait.Modulate = new Color(.4f, .48f, .5f, .55f);
        Text(b, key.ToString("00"), 8, 5, 29, 18, 11, locked ? CommandTheme.Line : CommandTheme.Muted, true);
        Text(b, def.Name, 8, 62, 106, 19, 15, locked ? CommandTheme.Muted.Darkened(.25f) : CommandTheme.Text, true);
        Text(b, locked ? "LOCKED" : $"$ {def.Cost:N0}", 8, 81, 104, 16, 12, locked ? CommandTheme.Muted.Darkened(.25f) : CommandTheme.Gold, true);
        if (locked) Icon(b, "research", 84, 7, 24, CommandTheme.Muted.Darkened(.3f));
    }

    private void UpgradeCard(string id, string icon, float x, float y)
    {
        var def = _world.World.Rules.Upgrade(id);
        var b = Button(_hud, "", x, y, 246, 98, () => Notify($"{def.Name} research preview"));
        Icon(b, icon, 14, 20, 39, CommandTheme.Gold);
        Text(b, "RESEARCH", 66, 14, 170, 18, 11, CommandTheme.Muted, true);
        Text(b, def.Name, 66, 36, 174, 23, 19, CommandTheme.Text, true);
        Text(b, $"$ {def.Cost:N0}", 66, 68, 170, 20, 14, CommandTheme.Gold, true);
        b.MouseEntered += () => ShowTooltip(def.Name, def.Description, $"$ {def.Cost:N0}", x);
        b.MouseExited += HideTooltip;
    }

    private void BuildQueue(float y, bool production)
    {
        Panel(_hud, 1254, y + 10, 326, 241, CommandTheme.Panel, true);
        Text(_hud, production ? "PRODUCTION QUEUE" : "FIELD ORDERS", 1270, y + 18, 275, 24, 19, CommandTheme.Text, true);
        Rule(_hud, 1270, y + 51, 291, CommandTheme.Line.Darkened(.25f));
        if (production)
        {
            if (_queue.Count == 0)
            {
                Text(_hud, "QUEUE EMPTY", 1270, y + 69, 280, 30, 23, CommandTheme.Muted, true);
                Text(_hud, "Select a unit to begin production.", 1270, y + 110, 281, 25, 16, CommandTheme.Muted);
                return;
            }
            var first = _world.World.Rules.Unit(_queue[0]);
            Text(_hud, first.Name.ToUpperInvariant(), 1270, y + 64, 255, 25, 23, CommandTheme.Text, true);
            _queueCaption = Text(_hud, $"ASSEMBLING   {_queueProgress * 100:0}%", 1270, y + 96, 262, 20, 13, CommandTheme.Gold, true);
            _queueMeter = new ProgressBar { Position = new Vector2(1270, y + 125), Size = new Vector2(291, 8), MaxValue = 1, Value = _queueProgress, ShowPercentage = false, MouseFilter = Control.MouseFilterEnum.Ignore };
            var track = CommandTheme.Box(CommandTheme.Ink, CommandTheme.Line, 0);
            var fill = CommandTheme.Box(CommandTheme.Gold, CommandTheme.Gold, 0);
            foreach (var style in new[] { track, fill })
            {
                style.ContentMarginLeft = 0; style.ContentMarginRight = 0;
                style.ContentMarginTop = 0; style.ContentMarginBottom = 0;
            }
            _queueMeter.AddThemeStyleboxOverride("background", track);
            _queueMeter.AddThemeStyleboxOverride("fill", fill);
            _hud.AddChild(_queueMeter);
            for (int i = 0; i < _queue.Count; i++)
            {
                var index = i; var unit = _world.World.Rules.Unit(_queue[i]);
                var b = Button(_hud, "", 1270 + i * 59, y + 152, 53, 53, () => { _queue.RemoveAt(index); if (index == 0) _queueProgress = 0; Rebuild(); });
                AddPortrait(b, unit.Model, 2, 1, 49, 46, 7);
                Text(b, (i + 1).ToString(), 4, 2, 18, 15, 11, CommandTheme.Gold, true);
                b.TooltipText = $"Cancel {unit.Name} (preview)";
            }
            Text(_hud, "CLICK A QUEUED UNIT TO CANCEL", 1270, y + 221, 288, 16, 11, CommandTheme.Muted, true);
        }
        else
        {
            Icon(_hud, _order == "Awaiting orders" ? "dozer" : "command", 1273, y + 70, 43, CommandTheme.Gold);
            Text(_hud, _order == "Awaiting orders" ? "READY TO BUILD" : _order.ToUpperInvariant(), 1330, y + 71, 221, 30, 23, CommandTheme.Text, true);
            Text(_hud, _order == "Awaiting orders" ? "No active construction" : "Structure selected", 1331, y + 103, 222, 22, 15, CommandTheme.Muted);
            Text(_hud, "Choose a structure from the command grid.\nHover a card for costs and requirements.", 1270, y + 147, 282, 49, 16, CommandTheme.Muted);
            Text(_hud, "RIGHT CLICK", 1270, y + 220, 105, 18, 12, CommandTheme.Gold, true);
            Text(_hud, "Cancel selection", 1374, y + 219, 180, 20, 14, CommandTheme.Muted);
        }
    }

    private void BuildReviewStrip()
    {
        Text(_hud, "UI REVIEW  /  STAGED SCENE", 24, _height - 23, 300, 19, 11, CommandTheme.Muted, true);
        Text(_hud, "F1  BUILD    F2  PRODUCE    F3  LOW POWER    F4  MENU", 388, _height - 23, 780, 19, 11, CommandTheme.Muted, true);
        Text(_hud, "OVERMATCH  /  COMMAND SYSTEM 01", 1220, _height - 23, 360, 19, 11, CommandTheme.Muted, true).HorizontalAlignment = HorizontalAlignment.Right;
        var panel = Panel(_hud, 510, 78, 580, 42, new Color("111a1be8"));
        Button(panel, "CONSTRUCTION", 6, 5, 148, 32, () => SetState("construction"), _state == "construction", true);
        Button(panel, "PRODUCTION", 162, 5, 145, 32, () => SetState("production"), _state == "production", true);
        Button(panel, "LOW POWER", 315, 5, 135, 32, () => SetState("lowpower"), _state == "lowpower", true);
        Button(panel, "FRONT END", 458, 5, 115, 32, () => { _screen = "menu"; Rebuild(); }, small: true);
    }

    private void SetState(string state) { _state = state; _screen = "hud"; _selectedCard = null; Rebuild(); }

    private void ShowTooltip(string title, string detail, string cost, float x)
    {
        HideTooltip();
        _tooltip = Panel(_hud, Math.Min(x, 1120), _height - 399, 442, 100, new Color("152022f5"), true);
        Text(_tooltip, title.ToUpperInvariant(), 17, 10, 390, 28, 23, CommandTheme.Text, true);
        Text(_tooltip, detail, 17, 43, 401, 31, 14, CommandTheme.Muted).AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Text(_tooltip, cost, 17, 77, 406, 18, 13, CommandTheme.Gold, true);
    }

    private void HideTooltip() { _tooltip?.QueueFree(); _tooltip = null; }

    private void ShowFrontEnd(bool setup)
    {
        _hud.Visible = false;
        var front = new ReviewFrontEnd { Size = _canvas.Size, SetupInitially = setup };
        front.DeployRequested = () => { _screen = "hud"; _world.FocusBase(); Rebuild(); };
        front.HudRequested = () => { _screen = "hud"; Rebuild(); };
        _canvas.AddChild(front);
        _modal = front;
    }

    private void ShowPause()
    {
        if (_modal is not null) return;
        _screen = "pause";
        var shade = new ColorRect { Color = new Color(0.015f, .025f, .028f, .8f), Size = _canvas.Size, MouseFilter = Control.MouseFilterEnum.Stop };
        _canvas.AddChild(shade); _modal = shade;
        var x = 560f; var y = (_height - 430) / 2;
        var p = Panel(shade, x, y, 480, 430, CommandTheme.Panel, true);
        Icon(p, "coalition", 205, 30, 70, CommandTheme.Gold);
        Text(p, "OPERATIONS PAUSED", 40, 118, 400, 47, 40, CommandTheme.Text, true).HorizontalAlignment = HorizontalAlignment.Center;
        Text(p, "Stand by for orders, Commander.", 40, 178, 400, 25, 18, CommandTheme.Muted).HorizontalAlignment = HorizontalAlignment.Center;
        Button(p, "RESUME OPERATIONS", 55, 238, 370, 52, () => { _screen = "hud"; Rebuild(); }, true);
        Button(p, "RETURN TO COMMAND", 55, 304, 370, 48, () => { _screen = "menu"; Rebuild(); });
        Text(p, "UI REVIEW  /  ESC TO RESUME", 40, 381, 400, 22, 12, CommandTheme.Muted, true).HorizontalAlignment = HorizontalAlignment.Center;
    }

    private void Notify(string message) { if (_notice is not null) _notice.Text = message; _noticeTimer = 4; }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } && _screen == "hud")
        { _order = "Awaiting orders"; _selectedCard = null; Rebuild(); }
        if (ev is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.F1: SetState("construction"); break;
            case Key.F2: SetState("production"); break;
            case Key.F3: SetState("lowpower"); break;
            case Key.F4: _screen = "menu"; Rebuild(); break;
            case Key.Escape:
                if (_screen != "hud") { _screen = "hud"; Rebuild(); }
                else ShowPause();
                break;
        }
    }

    public override void _Process(double delta)
    {
        _frames++;
        if (_noticeTimer > 0) { _noticeTimer -= delta; if (_noticeTimer <= 0 && _notice is not null) _notice.Text = ""; }
        // The production progress is a UI-only animation; it never creates a sim entity.
        if (_interactiveQueue && _queue.Count > 0 && _screen == "hud" && _state == "production")
        {
            _queueProgress += (float)delta / 25;
            if (_queueCaption is not null) _queueCaption.Text = $"ASSEMBLING   {_queueProgress * 100:0}%";
            if (_queueMeter is not null) _queueMeter.Value = _queueProgress;
            if (_queueProgress >= 1) { _queue.RemoveAt(0); _queueProgress = 0; Rebuild(); }
        }
        if (_capture is not null && _frames == 60)
        {
            var error = GetViewport().GetTexture().GetImage().SavePng(_capture);
            GD.Print($"[UI review] capture {_capture}: {error}");
            GetTree().Quit(error == Error.Ok ? 0 : 1);
        }
    }

    private static ConsolePanel Panel(Node parent, float x, float y, float width, float height, Color fill, bool accent = false, float cut = 8)
    {
        var p = new ConsolePanel { Position = new Vector2(x, y), Size = new Vector2(width, height), Fill = fill, Accent = accent, Cut = cut };
        parent.AddChild(p); return p;
    }

    private static Label Text(Node parent, string text, float x, float y, float width, float height, int size, Color color, bool display = false)
    {
        var label = new Label { Text = text, Position = new Vector2(x, y), Size = new Vector2(width, height), MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", display ? CommandTheme.Display : CommandTheme.Body);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label); return label;
    }

    private static Button Button(Node parent, string text, float x, float y, float width, float height, Action pressed, bool accent = false, bool small = false)
    {
        var b = new Button { Text = text, Position = new Vector2(x, y), Size = new Vector2(width, height), MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        CommandTheme.Style(b);
        if (accent) CommandTheme.Style(b, true);
        if (small) { b.AddThemeFontSizeOverride("font_size", 13); b.AddThemeStyleboxOverride("normal", CommandTheme.Box(accent ? new Color("34382e") : CommandTheme.Ink, accent ? CommandTheme.Gold.Darkened(.25f) : CommandTheme.Line, 1)); }
        b.Pressed += pressed; parent.AddChild(b); return b;
    }

    private static void Icon(Node parent, string name, float x, float y, float size, Color color)
    {
        parent.AddChild(new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = CommandTheme.Icon(name), Position = new Vector2(x, y), Size = new Vector2(size, size), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Modulate = color, MouseFilter = Control.MouseFilterEnum.Ignore });
    }

    private static ModelPortrait AddPortrait(Node parent, string model, float x, float y, float width, float height, float distance)
    {
        var p = new ModelPortrait { ModelId = model, Distance = distance, Position = new Vector2(x, y), Size = new Vector2(width, height) };
        parent.AddChild(p); return p;
    }

    private static void Rule(Node parent, float x, float y, float width, Color color, float height = 1)
    { parent.AddChild(new ColorRect { Position = new Vector2(x, y), Size = new Vector2(width, height), Color = color, MouseFilter = Control.MouseFilterEnum.Ignore }); }

    private static void Meter(Node parent, float x, float y, float width, float height, float fraction, Color color, int segments)
    {
        var step = width / segments;
        for (int i = 0; i < segments; i++) Rule(parent, x + step * i, y, step - 2, i < fraction * segments ? color.Darkened(.1f) : CommandTheme.Line.Darkened(.5f), height);
    }
}
