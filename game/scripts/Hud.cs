using Godot;
using Overmatch.Game.UiReview;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>
/// The live command console: header, support powers, and a bottom console of radar, selection dossier, command cards
/// and production queue. Visual language from game/ui/command; every value and button here is bound to the running match.
/// </summary>
public partial class Hud : CanvasLayer
{
    public GameRoot Root { get; set; } = null!;

    private sealed record Card(string Key, string Title, Func<string> Sub, Func<bool> Enabled, Func<bool> Locked, string Info, Action OnPress, string? Model, string? Icon);

    public const float ConsoleHeight = 295f;
    private const float HeaderHeight = 60f;

    private Control _canvas = null!;
    private Control _console = null!;
    private Control _dossier = null!;
    private Control _cards = null!;
    private Control _queue = null!;
    private Control _powers = null!;
    private Control _banner = null!;
    private Control? _tooltip;
    private Label _notice = null!;
    private Label _hint = null!;
    private PortraitCache _portraits = null!;
    private float _width = Ui.DesignWidth;
    private float _height = 900f;
    private readonly List<Control> _fullWidth = new();                      // bars that span the screen
    private readonly List<(Control Node, float X)> _right = new();          // anchored to the right edge
    private readonly List<(Control Node, float X)> _centre = new();         // centred
    private float _noticeTtl;

    private readonly List<Action> _live = new();          // per-frame text/state updates for the header
    private readonly List<Action> _liveSelection = new(); // … for the dossier, cards and queue
    private readonly List<Action> _livePowers = new();
    private readonly List<Action> _liveQueue = new();     // … rebuilt whenever the queue contents change
    private string _selectionSig = "";
    private string _queueSig = "";
    private string _powersSig = "";
    private string? _hoverKey;

    /// <summary>The radar housing; GameRoot parents the live minimap here.</summary>
    public Control MinimapSlot { get; private set; } = null!;
    /// <summary>Screen-space Y where the console begins (for the minimap's camera outline and input tests).</summary>
    public float ConsoleTopScreenY => _height - ConsoleHeight;

    public override void _Ready()
    {
        _portraits = new PortraitCache { Name = "Portraits" };
        AddChild(_portraits);
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_canvas);
        BuildHeader();
        BuildField();
        BuildConsole();
        GetViewport().SizeChanged += Layout;
        Layout();
    }

    private void Layout()
    {
        // The viewport is already in design units (see UiScaler). On a big screen with a reduced interface it is wider
        // than the 1600 the console was drawn for: bars stretch, the left cluster stays put, the right cluster follows the edge.
        var size = GetViewport().GetVisibleRect().Size;
        _width = Math.Max(Ui.DesignWidth, size.X);
        _height = Math.Max(Ui.MinDesignHeight, size.Y);
        _canvas.Scale = Vector2.One;
        _canvas.Position = Vector2.Zero;
        _canvas.Size = new Vector2(_width, _height);
        var extra = _width - Ui.DesignWidth;
        foreach (var bar in _fullWidth) bar.Size = new Vector2(_width, bar.Size.Y);
        foreach (var (node, x) in _right) node.Position = new Vector2(x + extra, node.Position.Y);
        foreach (var (node, x) in _centre) node.Position = new Vector2(x + Mathf.Round(extra / 2f), node.Position.Y);
        _console.Position = new Vector2(0, _height - ConsoleHeight);
        _hint.Position = new Vector2(_hint.Position.X, _height - ConsoleHeight - 34);
        HideTooltip();
    }

    // ------------------------------------------------------------------ static chrome

    private void BuildHeader()
    {
        var w = Root.World;
        var p = w.Player(Root.LocalPlayer);
        var h = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(h);
        _fullWidth.Add(Ui.Panel(h, 0, 0, 1600, HeaderHeight, new Color("11191bf5"), false, 0, blockMouse: true));
        _fullWidth.Add(Ui.Rule(h, 0, HeaderHeight - 1, 1600, CommandTheme.Line));
        var hr = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        h.AddChild(hr);
        _right.Add((hr, 0));
        Ui.Icon(h, Ui.FactionIcon(p.Faction.Id), 22, 9, 42, CommandTheme.Gold);
        Ui.Text(h, "OVERMATCH", 76, 8, 205, 35, 30, CommandTheme.Text, true);
        Ui.Text(h, $"{p.Faction.Name.ToUpperInvariant()} COMMAND", 262, 22, 210, 20, 14, CommandTheme.Muted, true);
        Ui.Rule(h, 480, 12, 1, CommandTheme.Line, 36);
        Ui.Text(h, "CREDITS", 506, 7, 90, 18, 12, CommandTheme.Muted, true);
        var cash = Ui.Text(h, "$ 0", 506, 23, 160, 32, 27, CommandTheme.Gold, true);
        var reactor = Ui.Icon(h, "reactor", 670, 15, 28, CommandTheme.Green);
        Ui.Text(h, "POWER GRID", 712, 7, 140, 18, 12, CommandTheme.Muted, true);
        var power = Ui.Text(h, "", 712, 25, 190, 25, 20, CommandTheme.Green, true);
        var meter = Ui.Meter(h, 903, 25, 126, 9, 16, CommandTheme.Green);
        Ui.Icon(hr, "chevron", 1070, 13, 32, CommandTheme.Gold);
        Ui.Text(hr, "FIELD COMMANDER", 1112, 7, 170, 18, 12, CommandTheme.Muted, true);
        var rank = Ui.Text(hr, "", 1112, 25, 200, 24, 20, CommandTheme.Text, true);
        var clock = Ui.Text(hr, "00:00", 1322, 15, 88, 30, 25, CommandTheme.Muted, true);
        Ui.Button(hr, "MENU   ESC", 1440, 12, 138, 36, () => Root.Result.TogglePause());
        var fps = Ui.Text(hr, "", 1322, 44, 88, 14, 10, CommandTheme.Muted, true);
        _live.Add(() => fps.Text = GameSettings.ShowFps ? $"{Engine.GetFramesPerSecond():0} FPS" : "");

        _live.Add(() =>
        {
            cash.Text = $"$ {p.Cash:N0}";
            var colour = p.LowPower ? CommandTheme.Red : CommandTheme.Green;
            if (!p.Faction.NeedsPower) { power.Text = "SELF-SUFFICIENT"; meter.Fraction = 0f; }
            else
            {
                power.Text = $"{p.PowerDemand} / {p.PowerSupply}   {(p.LowPower ? "OVERLOAD" : "STABLE")}";
                meter.Fraction = p.PowerSupply <= 0 ? (p.PowerDemand > 0 ? 1f : 0f) : (float)p.PowerDemand / p.PowerSupply;
            }
            power.AddThemeColorOverride("font_color", colour);
            reactor.Modulate = colour;
            meter.SetColour(colour);
            rank.Text = $"RANK {p.Rank}  /  {p.Points} POINT{(p.Points == 1 ? "" : "S")}";
            clock.Text = Ui.Clock(w.Time);
        });
    }

    private void BuildField()
    {
        var map = Root.World.MapDef;
        var plate = Ui.Panel(_canvas, 20, 80, 300, 62, new Color("111a1bdd"));
        Ui.Text(plate, map.Name.ToUpperInvariant(), 16, 6, 270, 27, 24, CommandTheme.Text, true);
        Ui.Text(plate, $"SKIRMISH  /  {Root.World.PlayerCount} COMMANDERS  /  {Root.Speed}X", 16, 36, 270, 18, 12, CommandTheme.Muted, true);

        _powers = new Control { Position = new Vector2(1320, 84), MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(_powers);
        _right.Add((_powers, 1320));

        _banner = Ui.Panel(_canvas, 560, 138, 480, 67, new Color("39251ff0"), true);
        Ui.Icon(_banner, "reactor", 14, 14, 36, CommandTheme.Red);
        Ui.Text(_banner, "LOW POWER", 66, 7, 365, 24, 22, CommandTheme.Red, true);
        Ui.Text(_banner, "Production slowed and defences offline. Build more power.", 66, 36, 400, 22, 14, CommandTheme.Text);
        _banner.Visible = false;
        _centre.Add((_banner, 560));

        _notice = Ui.Text(_canvas, "", 420, 220, 760, 40, 22, CommandTheme.Gold, true, HorizontalAlignment.Center);
        _hint = Ui.Text(_canvas, "", 300, 560, 1000, 26, 16, CommandTheme.Blue, true, HorizontalAlignment.Center);
        _centre.Add((_notice, 420));
        _centre.Add((_hint, 300));
    }

    private void BuildConsole()
    {
        _console = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(_console);
        _fullWidth.Add(Ui.Panel(_console, 0, 0, 1600, ConsoleHeight, new Color("131c1e"), false, 0, blockMouse: true));
        _fullWidth.Add(Ui.Rule(_console, 0, 0, 1600, CommandTheme.Line.Lightened(.15f), 2));
        _fullWidth.Add(Ui.Rule(_console, 0, 3, 1600, CommandTheme.Ink, 4));

        // Radar housing. The live minimap (with M5 fog rules) is parented into the slot by GameRoot.
        Ui.Panel(_console, 18, 20, 254, 251, CommandTheme.Raised.Darkened(.17f), true);
        Ui.Text(_console, "TACTICAL RADAR", 36, 27, 175, 24, 18, CommandTheme.Text, true);
        Ui.Text(_console, "N", 237, 30, 18, 20, 15, CommandTheme.Gold, true);
        MinimapSlot = new Control { Position = new Vector2(36, 57), Size = new Vector2(218, 182), ClipContents = true };
        _console.AddChild(MinimapSlot);
        var grid = Root.World.Grid;
        Ui.Text(_console, $"{grid.Width} × {grid.Height}", 35, 243, 98, 17, 11, CommandTheme.Muted, true);
        Ui.Button(_console, "CENTER BASE", 145, 242, 110, 20, () => Root.Selection.JumpToHq(), false, 12);

        _dossier = new Control { Position = new Vector2(286, 20), MouseFilter = Control.MouseFilterEnum.Ignore };
        _cards = new Control { Position = new Vector2(605, 18), MouseFilter = Control.MouseFilterEnum.Ignore };
        _queue = new Control { Position = new Vector2(1254, 20), MouseFilter = Control.MouseFilterEnum.Ignore };
        _console.AddChild(_dossier);
        _console.AddChild(_cards);
        _console.AddChild(_queue);
        _right.Add((_queue, 1254));
    }

    // ------------------------------------------------------------------ public API used by the rest of the game

    public void Say(string text)
    {
        _notice.Text = text.ToUpperInvariant();
        _noticeTtl = 2.8f;
    }

    /// <summary>True when the pointer is over interface chrome rather than the battlefield.</summary>
    public bool IsMouseOverBar(Vector2 screen)
    {
        if (!Visible) return false;
        var p = screen;
        if (p.Y <= HeaderHeight || p.Y >= _height - ConsoleHeight) return true;
        var pr = new Rect2(_powers.Position, _powers.Size);
        return pr.HasPoint(p);
    }

    // ------------------------------------------------------------------ per frame

    public override void _Process(double delta)
    {
        var w = Root.World;
        var p = w.Player(Root.LocalPlayer);
        foreach (var a in _live) a();
        _banner.Visible = p.LowPower;

        if (_noticeTtl > 0) { _noticeTtl -= (float)delta; if (_noticeTtl <= 0) _notice.Text = ""; }
        if (Root.Selection.PendingTarget is { } pt) _hint.Text = $"{pt.Label.ToUpperInvariant()}  —  CLICK A TARGET   /   RIGHT-CLICK CANCELS";
        else if (Root.Selection.AttackMoveArmed) _hint.Text = "ATTACK-MOVE  —  CLICK A DESTINATION   /   RIGHT-CLICK CANCELS";
        else if (Root.Placement.Active) _hint.Text = "CLICK TO PLACE   /   SHIFT+CLICK PLACES SEVERAL   /   RIGHT-CLICK CANCELS";
        else _hint.Text = Root.Selection.HoverHint(GetViewport().GetMousePosition()).ToUpperInvariant();

        RefreshPowers();
        RefreshSelection();
        foreach (var a in _livePowers) a();
        foreach (var a in _liveSelection) a();
        foreach (var a in _liveQueue) a();
    }

    // ------------------------------------------------------------------ descriptions

    private static readonly Dictionary<string, string> GoodAgainst = new()
    {
        ["small_arms"] = "infantry", ["armour_piercing"] = "vehicles and tanks", ["explosive"] = "infantry, light vehicles and buildings",
        ["flame"] = "infantry and garrisons", ["toxin"] = "infantry and garrisons", ["sniper"] = "infantry", ["demolition"] = "buildings", ["laser"] = "everything",
    };

    /// <summary>Everything a player needs before buying: cost, requirements, what it does, and crucially what it can shoot at.</summary>
    public string Describe(ObjectDef d, bool withCost = true)
    {
        var w = Root.World;
        var lines = new List<string>();
        if (withCost) lines.Add(d.Cost > 0 ? $"{d.Name}   ${d.Cost}   {d.BuildTime:0}s" : d.Name);
        if (d.Prereqs.Count > 0 && !w.HasPrereqs(Root.LocalPlayer, d.Prereqs, out _))
            lines.Add("Requires: " + string.Join(", ", d.Prereqs.Select(NameOf)));
        if (d.Description != "") lines.Add(d.Description);

        var weapons = d.Weapons.Select(id => w.Rules.Weapon(id)).ToList();
        if (weapons.Count > 0)
        {
            var air = weapons.Any(x => x.CanTargetAir);
            var ground = weapons.Any(x => x.CanTargetGround);
            lines.Add("Attacks: " + (ground && air ? "ground and AIR" : air ? "AIR only" : "ground only") + $"   range {weapons.Max(x => x.Range):0}");
            var good = weapons.Select(x => GoodAgainst.GetValueOrDefault(x.DamageType, "")).Where(x => x != "").Distinct().ToList();
            if (good.Count > 0) lines.Add("Good against: " + string.Join("; ", good));
        }
        else if (d is UnitDef { Builder: false, Harvest: null } && d.Auras.Count == 0 && d.Spawner is null) lines.Add("Unarmed");

        var traits = new List<string> { $"HP {d.Hp:0}", d.Armour.Replace('_', ' ') + " armour" };
        if (d is UnitDef u) traits.Add(u.IsAir ? "aircraft" : $"speed {u.Speed:0.#}");
        if (d.Stealth) traits.Add("stealth");
        if (d.Detector > 0) traits.Add("detects stealth");
        if (d.GarrisonSlots > 0) traits.Add($"carries {d.GarrisonSlots}");
        if (d is UnitDef { CanCapture: true }) traits.Add("can capture");
        if (d is BuildingDef b && b.Power != 0) traits.Add(b.Power > 0 ? $"+{b.Power} power" : $"{b.Power} power");
if (d is BuildingDef { Trickle: { } inc } ib)
            lines.Add($"Income: ${inc.Amount} every {inc.Interval:0} s" + (ib.TricklePerPassenger > 0 ? $", plus ${ib.TricklePerPassenger} per infantry inside" : "")
                + $"   (${inc.Amount * 60f / inc.Interval:0} a minute)");
        lines.Add(string.Join("   ", traits));
        if (d.Abilities.Count > 0) lines.Add("Abilities: " + string.Join(", ", d.Abilities.Select(a => a.Name)));
        return string.Join("\n", lines);
    }

    private string NameOf(string id)
    {
        var r = Root.World.Rules;
        if (r.Buildings.TryGetValue(id, out var b)) return b.Name;
        var provider = r.Buildings.Values.FirstOrDefault(x => x.Provides.Contains(id));
        return provider?.Name ?? id.Replace('_', ' ');
    }

    private void ShowTooltip(string key, string title, string body, float x)
    {
        HideTooltip();
        _hoverKey = key;
        var lines = body.Split('\n').Length;
        var h = 58 + lines * 21;
        _tooltip = Ui.Panel(_canvas, Math.Clamp(x, 286, _width - 500), _height - ConsoleHeight - h - 44, 480, h, new Color("152022f7"), true);
        Ui.Text(_tooltip, title.ToUpperInvariant(), 17, 10, 440, 28, 23, CommandTheme.Text, true);
        Ui.Text(_tooltip, body, 17, 44, 446, h - 50, 15, CommandTheme.Muted, false, HorizontalAlignment.Left, wrap: true);
    }

    private void HideTooltip()
    {
        _tooltip?.QueueFree();
        _tooltip = null;
        _hoverKey = null;
    }

    // ------------------------------------------------------------------ selection: dossier, cards, queue

    private Entity? Primary()
    {
        Entity? first = null;
        foreach (var id in Root.Selection.Selected)
        {
            var e = Root.World.Get(id);
            if (e is null) continue;
            if (e.IsBuilding) return e;
            first ??= e;
        }
        return first;
    }

    private void RefreshSelection()
    {
        var w = Root.World;
        var e = Primary();
        var inspected = e is null ? w.Get(Root.Selection.InspectId) : null;
        var subject = e ?? inspected;

        var cards = new List<Card>();
        if (e is not null) { if (e.IsBuilding) BuildingCards(e, cards); else UnitCards(e, cards); }

        var sig = $"{subject?.Id}|{Root.Selection.Selected.Count}|{e?.UnderConstruction}|{string.Join(",", cards.Select(c => c.Key))}";
        if (sig != _selectionSig)
        {
            _selectionSig = sig;
            _queueSig = "";
            _liveSelection.Clear();
            _liveQueue.Clear();
            HideTooltip();
            Ui.Clear(_dossier);
            Ui.Clear(_cards);
            BuildDossier(subject, e is not null);
            BuildCards(e, cards);
        }

        var items = e?.Queue?.Items;
        var qsig = items is null ? "none:" + (e?.Id ?? 0) : e!.Id + ":" + string.Join(",", items.Select(i => i.Id));
        if (qsig != _queueSig)
        {
            _queueSig = qsig;
            _liveQueue.Clear();
            Ui.Clear(_queue);
            BuildQueue(e);
        }
    }

    private void BuildDossier(Entity? s, bool mine)
    {
        var w = Root.World;
        Ui.Panel(_dossier, 0, 0, 300, 251, CommandTheme.Panel, true);
        if (s is null)
        {
            var f = w.Player(Root.LocalPlayer).Faction;
            Ui.Icon(_dossier, Ui.FactionIcon(f.Id), 110, 40, 80, CommandTheme.Line);
            Ui.Text(_dossier, "NO SELECTION", 16, 140, 268, 30, 24, CommandTheme.Muted, true, HorizontalAlignment.Center);
            Ui.Text(_dossier, "Drag to select units. Click anything to inspect it.", 16, 176, 268, 44, 14, CommandTheme.Muted, false, HorizontalAlignment.Center, wrap: true);
            return;
        }

        var count = Root.Selection.Selected.Count;
        var owner = s.Owner < 0 ? "NEUTRAL" : w.Player(s.Owner).Name.ToUpperInvariant();
        var role = s.IsBuilding ? "STRUCTURE" : s.Def.IsInfantry ? "INFANTRY" : s.Def.IsAir ? "AIRCRAFT" : "VEHICLE";
        Ui.Text(_dossier, s.Def.Name.ToUpperInvariant(), 16, 6, 230, 34, 28, CommandTheme.Text, true);
        Ui.Text(_dossier, mine ? (count > 1 && !s.IsBuilding ? $"{count} UNITS SELECTED" : role) : $"{owner}  /  {role}", 17, 40, 251, 19, 12, mine ? CommandTheme.Muted : CommandTheme.Blue, true);
        if (mine && count > 1 && !s.IsBuilding) Ui.Text(_dossier, count.ToString("00"), 250, 10, 40, 25, 22, CommandTheme.Gold, true);

        Ui.Panel(_dossier, 16, 67, 148, 98, CommandTheme.Ink);
        var colour = Root.PlayerColour(s.Owner);
        Ui.Picture(_dossier, _portraits.Get(s.Def.Model, colour), 18, 66, 144, 100);

        Ui.Text(_dossier, "CONDITION", 181, 69, 95, 17, 11, CommandTheme.Muted, true);
        var hp = Ui.Text(_dossier, "", 181, 91, 113, 23, 18, CommandTheme.Text, true);
        var meter = Ui.Meter(_dossier, 181, 119, 100, 6, 12, CommandTheme.Green);
        var status = Ui.Text(_dossier, "", 181, 136, 115, 18, 12, CommandTheme.Green, true);
        var extra = Ui.Text(_dossier, "", 17, 168, 268, 16, 11, CommandTheme.Gold, true);
        Ui.Text(_dossier, s.Def.Description, 17, 183, 268, 34, 12, CommandTheme.Muted, false, HorizontalAlignment.Left, wrap: true);

        _liveSelection.Add(() =>
        {
            if (!s.Alive) return;
            hp.Text = $"{s.Hp:N0} / {s.MaxHp:N0}";
            var f = s.HpFraction;
            meter.Fraction = f;
            var c = f > 0.6f ? CommandTheme.Green : f > 0.3f ? CommandTheme.Gold : CommandTheme.Red;
            meter.SetColour(c);
            status.Text = s.UnderConstruction ? $"BUILDING {s.BuildProgress * 100:0}%" : s.Disabled ? "DISABLED" : s.Rearming ? "REARMING" : s.BeingRepaired ? "REPAIRING" : s.ReturningToBase ? "RETURNING" : f < 0.3f ? "CRITICAL" : s.IsBuilding ? "OPERATIONAL" : "READY";
            status.AddThemeColorOverride("font_color", s.Disabled ? CommandTheme.Blue : c);
            var bits = new List<string>();
            if (s.Level > 0) bits.Add("VETERAN " + new string('★', s.Level));
            if (s.SalvageLevel > 0) bits.Add($"SALVAGE {s.SalvageLevel}");
if (mine && s.Building is { Trickle: { } tr } sb && !s.UnderConstruction)
            {
                var pay = (int)((tr.Amount + sb.TricklePerPassenger * s.Passengers.Count) * Root.World.Player(s.Owner).IncomeMult);
                // Low power halves the rate, so the honest countdown is twice as long.
                var slowed = sb.NeedsPower && Root.World.Player(s.Owner).LowPower;
                var left = Mathf.CeilToInt(Mathf.Max(0f, s.TrickleTimer) * (slowed ? 2f : 1f));
                bits.Add(!s.Operational ? $"INCOME PAUSED  +${pay}" : $"NEXT +${pay} IN {left / 60}:{left % 60:00}" + (slowed ? "  LOW POWER, HALF SPEED" : ""));
            }
            if (s.IsHarvester) bits.Add($"CARRYING {s.Carried}");
            if (s.Unit is { Ammo: > 0 } au) bits.Add($"AMMO {s.Ammo}/{au.Ammo}");
            if (s.Passengers.Count > 0) bits.Add($"{s.Passengers.Count} INSIDE");
            if (s.Kills > 0) bits.Add($"{s.Kills} KILLS");
            extra.Text = string.Join("   ", bits);
        });

        if (!mine) return;
        var me = Root.LocalPlayer;
        int[] Sel() => Root.Selection.Selected.ToArray();
        var x = 16f;
        void Mini(string icon, string title, Action act, string info)
        {
            var b = Ui.Button(_dossier, "", x, 217, 86, 27, () => { Root.Audio.PlayUi("click"); act(); }, false, 12);
            Ui.Icon(b, icon, 5, 5, 17, CommandTheme.Muted);
            Ui.Text(b, title, 27, 4, 58, 20, 12, CommandTheme.Text, true);
            var bx = 286 + x;
            b.MouseEntered += () => ShowTooltip("mini:" + title, title, info, bx);
            b.MouseExited += () => { if (_hoverKey == "mini:" + title) HideTooltip(); };
            x += 91;
        }
        if (s.IsBuilding)
        {
            if (s.UnderConstruction) Mini("sell", "CANCEL", () => w.Submit(new SellCommand(me, s.Id)), "Cancel construction for a full refund.");
            else if (s.Def.Cost > 0) Mini("sell", "SELL", () => w.Submit(new SellCommand(me, s.Id)), $"Sell this building for ${s.Def.Cost / 2}.");
            Mini("rally", "RALLY", () => Say("Right-click the ground to set the rally point"), "With this building selected, right-click the ground to set where new units gather.");
        }
        else
        {
            Mini("stop", "STOP", () => w.Submit(new StopCommand(me, Sel())), "Halt and drop current orders. Hotkey S.");
            if (s.HasWeapons) Mini("attack", "A-MOVE", () => Root.Selection.ArmAttackMove(), "Attack-move: advance and fight anything on the way. Hotkey A, then click.");
            if (s.Unit is { IsAir: true } && !s.Def.Tags.Contains("drone"))
                Mini("repair", "TO BASE", () => w.Submit(new ReturnToBaseCommand(me, Sel())), "Fly to the nearest airfield. Aircraft holding over an airfield are repaired, and jets rearm. Right-clicking an airfield does the same.");
            if (s.Passengers.Count > 0 || (s.Def.GarrisonSlots > 0 && !s.Def.IsInfantry)) Mini("rally", "UNLOAD", () => w.Submit(new UngarrisonCommand(me, s.Id)), "Passengers out.");
        }
    }

    private void BuildCards(Entity? e, List<Card> cards)
    {
        if (e is null) return;
        var heading = e.IsBuilding ? (e.Building!.Produces.Count > 0 ? "PRODUCTION" : e.Building.Upgrades.Count > 0 ? "RESEARCH" : "STRUCTURE") : e.IsBuilder ? "CONSTRUCTION" : "ORDERS";
        Ui.Text(_cards, heading, 2, -10, 320, 24, 20, CommandTheme.Text, true);
        if (cards.Count == 0)
        {
            Ui.Text(_cards, "No commands for this selection.", 2, 40, 400, 24, 15, CommandTheme.Muted);
            return;
        }
        var colour = Root.LocalColour;
        for (var i = 0; i < cards.Count && i < 10; i++)
        {
            var card = cards[i];
            var cx = (i % 5) * 127f;
            var cy = 24f + (i / 5) * 106f;
            var b = Ui.Button(_cards, "", cx, cy, 119, 98, () => { Root.Audio.PlayUi("click"); card.OnPress(); });
            Ui.Panel(b, 4, 4, 111, 55, new Color("202e31"));
            Control art;
            if (card.Model is not null) art = Ui.Picture(b, _portraits.Get(card.Model, colour), 6, 2, 107, 58);
            else art = Ui.Icon(b, card.Icon ?? "command", 40, 10, 40, CommandTheme.Gold);
            Ui.Text(b, (i + 1 == 10 ? 0 : i + 1).ToString("00"), 8, 5, 29, 18, 11, CommandTheme.Muted, true);
            var title = Ui.Text(b, card.Title, 8, 62, 106, 19, 15, CommandTheme.Text, true);
            var sub = Ui.Text(b, "", 8, 81, 104, 16, 12, CommandTheme.Gold, true);
            var lockIcon = Ui.Icon(b, "research", 88, 7, 22, CommandTheme.Muted.Darkened(.2f));
            var absX = 605 + cx;
            b.MouseEntered += () => ShowTooltip(card.Key, card.Title, card.Info, absX);
            b.MouseExited += () => { if (_hoverKey == card.Key) HideTooltip(); };
            _liveSelection.Add(() =>
            {
                var locked = card.Locked();
                b.Disabled = locked || !card.Enabled();
                sub.Text = locked ? "LOCKED" : card.Sub();
                lockIcon.Visible = locked;
                art.Modulate = card.Model is not null ? (locked ? new Color(.4f, .48f, .5f, .55f) : b.Disabled ? new Color(.7f, .7f, .7f, .8f) : Colors.White) : (b.Disabled ? CommandTheme.Muted.Darkened(.3f) : CommandTheme.Gold);
                title.AddThemeColorOverride("font_color", b.Disabled ? CommandTheme.Muted.Darkened(.2f) : CommandTheme.Text);
                // Red only means "you cannot afford this"; anything else that is unavailable is just muted.
                var unaffordable = b.Disabled && sub.Text.StartsWith('$');
                sub.AddThemeColorOverride("font_color", locked ? CommandTheme.Muted.Darkened(.25f) : unaffordable ? CommandTheme.Red.Darkened(.1f) : b.Disabled ? CommandTheme.Muted : CommandTheme.Gold);
            });
        }
    }

    private void BuildingCards(Entity e, List<Card> cards)
    {
        var w = Root.World;
        var me = Root.LocalPlayer;
        var player = w.Player(me);
        if (e.UnderConstruction) return;
        var b = e.Building!;
        foreach (var uid in b.Produces)
        {
            var u = w.Rules.Unit(uid);
            cards.Add(new Card("unit:" + uid, u.Name, () => $"$ {(int)(u.Cost * player.DiscountMult):N0}", () => player.Cash >= u.Cost * player.DiscountMult,
                () => !w.HasPrereqs(me, u.Prereqs, out _), Describe(u), () => w.Submit(new ProduceCommand(me, e.Id, uid)), u.Model, null));
        }
        foreach (var upid in b.Upgrades)
        {
            var up = w.Rules.Upgrade(upid);
            var info = $"{up.Name}   ${up.Cost}   {up.Time:0}s\n{up.Description}" + (up.Prereqs.Count > 0 ? "\nRequires: " + string.Join(", ", up.Prereqs.Select(NameOf)) : "");
            cards.Add(new Card("up:" + upid, up.Name, () => player.Has(upid) ? "RESEARCHED" : $"$ {up.Cost:N0}", () => !player.Has(upid) && player.Cash >= up.Cost,
                () => !player.Has(upid) && !w.HasPrereqs(me, up.Prereqs, out _), info, () => w.Submit(new ProduceCommand(me, e.Id, upid)), null, "research"));
        }
        if (e.Def.GarrisonSlots > 0)
            cards.Add(new Card("evacuate", "Evacuate", () => $"{(b.TunnelHub ? player.TunnelPool.Count : e.Passengers.Count)} INSIDE", () => (b.TunnelHub ? player.TunnelPool.Count : e.Passengers.Count) > 0,
                () => false, b.TunnelHub ? "Everyone in the tunnel network comes out here." : "Everyone out.", () => w.Submit(new UngarrisonCommand(me, e.Id)), null, "rally"));
        if (b.Superweapon is { } sw)
            cards.Add(new Card("sw", sw.Name, () => e.SuperweaponCharge >= sw.ChargeTime ? "READY — FIRE" : "CHARGING  " + Ui.Clock(sw.ChargeTime - e.SuperweaponCharge),
                () => e.SuperweaponCharge >= sw.ChargeTime, () => false, $"{sw.Name}: click, then pick a target anywhere on the map.",
                () => Root.Selection.Arm(new SelectionController.Pending("superweapon", "", e.Id, sw.Name, false)), null, "satellite"));
    }

    private void UnitCards(Entity e, List<Card> cards)
    {
        var w = Root.World;
        var me = Root.LocalPlayer;
        var player = w.Player(me);
        int[] Sel() => Root.Selection.Selected.ToArray();

        if (e.IsBuilder)
            foreach (var bd in w.Rules.Buildings.Values.Where(x => x.Faction == player.Faction.Id).OrderBy(x => x.Cost))
            {
                var def = bd;
                if (def.Hq && w.Entities.Any(x => x.Owner == me && x.Building?.Hq == true)) continue;
                cards.Add(new Card("build:" + def.Id, def.Name, () => $"$ {def.Cost:N0}", () => player.Cash >= def.Cost, () => !w.HasPrereqs(me, def.Prereqs, out _),
                    Describe(def), () => Root.Placement.Begin(def, e.Id), def.Model, null));
            }
        if (e.IsHarvester)
            cards.Add(new Card("harvest", "Harvest", () => "NEAREST PILE", () => true, () => false, "Go to the nearest supply pile. Right-click a pile to choose one.",
                () => w.Submit(new HarvestCommand(me, Sel(), 0)), null, "supply"));
        for (var i = 0; i < e.Def.Abilities.Count; i++)
        {
            var ab = e.Def.Abilities[i];
            var idx = i;
            var icon = ab.Effect.Type switch { "reveal" => "satellite", "heal" => "repair", _ => "attack" };
            cards.Add(new Card("ab:" + ab.Id, ab.Name, () => e.AbilityCooldowns.Length > idx && e.AbilityCooldowns[idx] > 0f ? "READY IN " + Ui.Clock(e.AbilityCooldowns[idx]) : "READY",
                () => e.AbilityCooldowns.Length > idx && e.AbilityCooldowns[idx] <= 0f, () => false, ab.Description,
                () =>
                {
                    if (ab.Target == "none") w.Submit(new AbilityCommand(me, Sel(), ab.Id, Vec2.Zero, 0));
                    else Root.Selection.Arm(new SelectionController.Pending("ability", ab.Id, 0, ab.Name, ab.Target is "unit" or "building"));
                }, null, icon));
        }
        if (e.Unit is { CanCapture: true })
            cards.Add(new Card("capture", "Capture", () => "TECH BUILDING", () => true, () => false,
                "Capture an oil derrick or other tech building: click this, then the building. Right-clicking the building does the same. Takes 12 seconds next to it.",
                () => Root.Selection.Arm(new SelectionController.Pending("capture", "", 0, "Capture", true)), null, "command"));
        if (e.Def.IsInfantry)
            cards.Add(new Card("enter", "Enter", () => "GARRISON", () => true, () => false,
                "Garrison a civilian building, bunker, transport or tunnel: click this, then the target. Right-clicking it does the same.",
                () => Root.Selection.Arm(new SelectionController.Pending("garrison", "", 0, "Enter", true)), null, "barracks"));
    }

    private void BuildQueue(Entity? e)
    {
        var w = Root.World;
        Ui.Panel(_queue, 0, 0, 326, 251, CommandTheme.Panel, true);
        var items = e?.Queue?.Items;
        if (items is null)
        {
            Ui.Text(_queue, "FIELD ORDERS", 16, 8, 275, 24, 19, CommandTheme.Text, true);
            Ui.Rule(_queue, 16, 41, 291, CommandTheme.Line.Darkened(.25f));
            Ui.Text(_queue, "RIGHT-CLICK", 16, 52, 110, 18, 12, CommandTheme.Gold, true);
            Ui.Text(_queue, "move · attack · enter · capture", 112, 51, 205, 20, 13, CommandTheme.Muted);
            Ui.Text(_queue, "A  /  G + CLICK", 16, 76, 110, 18, 12, CommandTheme.Gold, true);
            Ui.Text(_queue, "attack-move · guard an area", 112, 75, 205, 20, 13, CommandTheme.Muted);
            Ui.Text(_queue, "CTRL + 1–9", 16, 100, 110, 18, 12, CommandTheme.Gold, true);
            Ui.Text(_queue, "set group · 1–9 recall · twice to jump", 112, 99, 205, 20, 13, CommandTheme.Muted);
            Ui.Text(_queue, "DOUBLE-CLICK", 16, 124, 110, 18, 12, CommandTheme.Gold, true);
            Ui.Text(_queue, "select all of a type on screen", 112, 123, 205, 20, 13, CommandTheme.Muted);
            Ui.Text(_queue, "H  /  SPACE", 16, 148, 110, 18, 12, CommandTheme.Gold, true);
            Ui.Text(_queue, "headquarters · last alert", 112, 147, 205, 20, 13, CommandTheme.Muted);
            Ui.Text(_queue, "S  /  X  /  Q  /  E", 16, 172, 110, 18, 12, CommandTheme.Gold, true);
            Ui.Text(_queue, "stop · scatter · all army · matching", 112, 171, 205, 20, 13, CommandTheme.Muted);
            Ui.Text(_queue, "ESC", 16, 196, 110, 18, 12, CommandTheme.Gold, true);
            Ui.Text(_queue, "menu, settings and the full key list", 112, 195, 205, 20, 13, CommandTheme.Muted);
            Ui.Text(_queue, "HOVER A CARD FOR COSTS, REQUIREMENTS AND TARGETS", 16, 222, 300, 16, 11, CommandTheme.Muted, true);
            return;
        }

        Ui.Text(_queue, "PRODUCTION QUEUE", 16, 8, 275, 24, 19, CommandTheme.Text, true);
        Ui.Rule(_queue, 16, 41, 291, CommandTheme.Line.Darkened(.25f));
        if (items.Count == 0)
        {
            Ui.Text(_queue, "QUEUE EMPTY", 16, 59, 280, 30, 23, CommandTheme.Muted, true);
            Ui.Text(_queue, "Select a card to begin production.", 16, 100, 281, 25, 16, CommandTheme.Muted);
            return;
        }
        string NameFor(QueueItem it) => it.IsUpgrade ? w.Rules.Upgrade(it.Id).Name : w.Rules.Unit(it.Id).Name;
        Ui.Text(_queue, NameFor(items[0]).ToUpperInvariant(), 16, 54, 290, 25, 23, CommandTheme.Text, true);
        var caption = Ui.Text(_queue, "", 16, 86, 290, 20, 13, CommandTheme.Gold, true);
        var bar = Ui.Meter(_queue, 16, 114, 291, 9, 30, CommandTheme.Gold);
        var head = items[0];
        var building = e!;
        _liveQueue.Add(() =>
        {
            bar.Fraction = head.Progress;
            var slow = w.Player(Root.LocalPlayer).LowPower;
            caption.Text = $"{(head.IsUpgrade ? "RESEARCHING" : "ASSEMBLING")}   {head.Progress * 100:0}%{(slow ? "   —   LOW POWER, HALF SPEED" : "")}";
        });
        for (var i = 0; i < items.Count && i < 5; i++)
        {
            var index = i;
            var it = items[i];
            var b = Ui.Button(_queue, "", 16 + i * 59, 142, 53, 53, () => w.Submit(new CancelProduceCommand(Root.LocalPlayer, building.Id, index)));
            if (it.IsUpgrade) Ui.Icon(b, "research", 11, 10, 31, CommandTheme.Gold);
            else Ui.Picture(b, _portraits.Get(w.Rules.Unit(it.Id).Model, Root.LocalColour), 2, 2, 49, 46);
            Ui.Text(b, (i + 1).ToString(), 4, 2, 18, 15, 11, CommandTheme.Gold, true);
            b.TooltipText = $"Cancel {NameFor(it)} (refund)";
        }
        if (items.Count > 5) Ui.Text(_queue, $"+{items.Count - 5}", 296, 158, 28, 22, 16, CommandTheme.Muted, true);
        Ui.Text(_queue, "CLICK A QUEUED ITEM TO CANCEL IT FOR A REFUND", 16, 222, 300, 16, 11, CommandTheme.Muted, true);
    }

    // ------------------------------------------------------------------ support powers

    private void RefreshPowers()
    {
        var w = Root.World;
        var p = w.Player(Root.LocalPlayer);
        var supers = w.Entities.Where(e => e.Owner == Root.LocalPlayer && e.Operational && e.Building?.Superweapon is not null).ToList();
        var sig = $"{p.Rank}/{p.Points}/{string.Join(",", p.PowersOwned)}/{string.Join(",", supers.Select(e => e.Id))}";
        if (sig == _powersSig) return;
        _powersSig = sig;
        _livePowers.Clear();
        Ui.Clear(_powers);

        var rows = p.Faction.Powers.Count + supers.Count;
        var height = 46 + rows * 50;
        _powers.Size = new Vector2(260, height);
        Ui.Panel(_powers, 0, 0, 260, height, new Color("111a1be8"), true, 8, blockMouse: true);
        Ui.Text(_powers, "SUPPORT POWERS", 18, 12, 216, 22, 17, CommandTheme.Muted, true);
        var y = 42f;
        foreach (var pid in p.Faction.Powers)
        {
            var def = w.Rules.Power(pid);
            var owned = p.HasPower(pid);
            var passive = def.Effect.Type == "bounty";
            var icon = def.Effect.Type switch { "reveal" => "satellite", "heal" => "repair", "spawn" => "command", "discount" => "factory", "bounty" => "supply", "status" => "research", _ => "attack" };
            var b = Ui.Button(_powers, "", 16, y, 228, 44, () =>
            {
                Root.Audio.PlayUi("click");
                if (!owned) w.Submit(new BuyPowerCommand(Root.LocalPlayer, pid));
                else if (def.Target == "none") w.Submit(new UsePowerCommand(Root.LocalPlayer, pid, Vec2.Zero));
                else Root.Selection.Arm(new SelectionController.Pending("power", pid, 0, def.Name, false));
            });
            var glyph = Ui.Icon(b, icon, 9, 7, 29, CommandTheme.Muted);
            var title = Ui.Text(b, def.Name.ToUpperInvariant(), 48, 4, 172, 19, 16, CommandTheme.Text, true);
            var value = Ui.Text(b, "", 48, 24, 172, 16, 12, CommandTheme.Gold, true);
            var info = $"Rank {def.Rank}   recharge {def.Cooldown:0}s\n{def.Description}";
            b.MouseEntered += () => ShowTooltip("power:" + pid, def.Name, info, 1100);
            b.MouseExited += () => { if (_hoverKey == "power:" + pid) HideTooltip(); };
            _livePowers.Add(() =>
            {
                bool ready;
                if (!owned)
                {
                    ready = p.Points > 0 && p.Rank >= def.Rank;
                    value.Text = p.Rank < def.Rank ? $"LOCKED  ·  RANK {def.Rank}" : ready ? "AVAILABLE  ·  SPEND 1 POINT" : "NO POINTS";
                }
                else if (passive) { ready = false; value.Text = "ACTIVE"; }
                else
                {
                    var left = p.PowerReadyAt(pid) - w.Time;
                    ready = left <= 0f;
                    value.Text = ready ? "READY" : Ui.Clock(left);
                }
                b.Disabled = !ready;
                glyph.Modulate = ready ? CommandTheme.Gold : CommandTheme.Muted;
                title.AddThemeColorOverride("font_color", ready || (owned && passive) ? CommandTheme.Text : CommandTheme.Muted);
                value.AddThemeColorOverride("font_color", owned && ready ? CommandTheme.Green : owned && passive ? CommandTheme.Green : CommandTheme.Gold);
            });
            y += 50;
        }
        foreach (var s in supers)
        {
            var sw = s.Building!.Superweapon!;
            var id = s.Id;
            var b = Ui.Button(_powers, "", 16, y, 228, 44, () => Root.Selection.Arm(new SelectionController.Pending("superweapon", "", id, sw.Name, false)), true);
            Ui.Icon(b, "satellite", 9, 7, 29, CommandTheme.Gold);
            Ui.Text(b, sw.Name.ToUpperInvariant(), 48, 4, 172, 19, 15, CommandTheme.Text, true);
            var value = Ui.Text(b, "", 48, 24, 172, 16, 12, CommandTheme.Gold, true);
            _livePowers.Add(() =>
            {
                var left = sw.ChargeTime - s.SuperweaponCharge;
                b.Disabled = left > 0f;
                value.Text = left > 0f ? "CHARGING   " + Ui.Clock(left) : "READY — SELECT TARGET";
                value.AddThemeColorOverride("font_color", left > 0f ? CommandTheme.Gold : CommandTheme.Red);
            });
            y += 50;
        }
    }
}
