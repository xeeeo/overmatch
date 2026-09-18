using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Top bar, powers panel, and the bottom command bar with selection info, hover descriptions, queue and command buttons.</summary>
public partial class Hud : CanvasLayer
{
    public GameRoot Root { get; set; } = null!;

    private sealed record ButtonSpec(string Key, Func<string> Text, Func<bool> Enabled, string Info, Action OnPress);

    private Label _cash = null!;
    private Label _power = null!;
    private Label _info = null!;
    private Label _debug = null!;
    private Label _message = null!;
    private Label _hint = null!;
    private float _messageTtl;
    private HBoxContainer _queueBox = null!;
    private GridContainer _buttons = null!;
    private PanelContainer _commandBar = null!;
    private VBoxContainer _powersBox = null!;
    private Label _rank = null!;
    private string _lastPowersSig = "";
    private string _lastButtonsSig = "";
    private string _lastQueueSig = "";
    private readonly List<(Button button, ButtonSpec spec)> _live = new();
    private string? _hoverInfo;
    private string _selectionInfo = "";

    public const float BarHeight = 176f;
    public Control MinimapSlot { get; private set; } = null!;

    public override void _Ready()
    {
        BuildTopBar();
        BuildCommandBar();
        BuildPowersPanel();

        _message = new Label { Modulate = new Color(1f, 0.85f, 0.4f), HorizontalAlignment = HorizontalAlignment.Center };
        _message.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_message);
        _message.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _message.OffsetTop = 60;

        _hint = new Label { Modulate = new Color(0.8f, 0.95f, 1f, 0.95f), HorizontalAlignment = HorizontalAlignment.Center };
        _hint.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_hint);
        _hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _hint.OffsetTop = -BarHeight - 30;
        _hint.OffsetBottom = -BarHeight - 6;
    }

    private void BuildTopBar()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        panel.CustomMinimumSize = new Vector2(0, 30);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        panel.AddChild(row);
        _cash = MakeLabel("$ 0", 16);
        _power = MakeLabel("Power 0 / 0", 16);
        _debug = MakeLabel("", 12);
        _debug.Modulate = new Color(1, 1, 1, 0.6f);
        row.AddChild(MakeLabel(" OVERMATCH", 16));
        row.AddChild(_cash);
        row.AddChild(_power);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        row.AddChild(_debug);
        AddChild(panel);
    }

    private void BuildCommandBar()
    {
        _commandBar = new PanelContainer();
        _commandBar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _commandBar.OffsetTop = -BarHeight;
        _commandBar.CustomMinimumSize = new Vector2(0, BarHeight);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _commandBar.AddChild(row);

        MinimapSlot = new Control { CustomMinimumSize = new Vector2(BarHeight - 8, BarHeight - 8) };
        row.AddChild(MinimapSlot);

        var infoBox = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
        _info = MakeLabel("", 13);
        _info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _info.CustomMinimumSize = new Vector2(380, 0);
        infoBox.AddChild(_info);
        row.AddChild(infoBox);

        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _queueBox = new HBoxContainer();
        right.AddChild(_queueBox);
        _buttons = new GridContainer { Columns = 6 };
        right.AddChild(_buttons);
        row.AddChild(right);
        AddChild(_commandBar);
    }

    private void BuildPowersPanel()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        panel.OffsetTop = 36;
        panel.OffsetLeft = -236;
        panel.OffsetRight = -6;
        _powersBox = new VBoxContainer { CustomMinimumSize = new Vector2(226, 0) };
        _powersBox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(_powersBox);
        _rank = MakeLabel("Rank 1", 13);
        _powersBox.AddChild(_rank);
        AddChild(panel);
    }

    private static Label MakeLabel(string text, int size)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", size);
        return l;
    }

    public void Say(string text)
    {
        _message.Text = text;
        _messageTtl = 2.5f;
    }

    public bool IsMouseOverBar(Vector2 pos) => _commandBar.GetGlobalRect().HasPoint(pos) || _powersBox.GetGlobalRect().HasPoint(pos);

    // ------------------------------------------------------------------ per frame

    public override void _Process(double delta)
    {
        var w = Root.World;
        var p = w.Player(Root.LocalPlayer);
        _cash.Text = $"$ {p.Cash:N0}";
        _power.Text = !p.Faction.NeedsPower ? "No power needed" : p.LowPower ? $"Power {p.PowerSupply} / {p.PowerDemand}  LOW POWER" : $"Power {p.PowerSupply} / {p.PowerDemand}";
        _power.Modulate = p.LowPower ? new Color(1f, 0.4f, 0.3f) : Colors.White;
        _debug.Text = $"tick {w.Tick}   {Engine.GetFramesPerSecond()} fps   speed {Root.Speed}x ";

        if (_messageTtl > 0) { _messageTtl -= (float)delta; if (_messageTtl <= 0) _message.Text = ""; }
        if (Root.Selection.PendingTarget is { } pt) _hint.Text = $"{pt.Label}: click a target (right-click cancels)";
        else if (Root.Selection.AttackMoveArmed) _hint.Text = "Attack-move: click a destination (right-click cancels)";
        else if (Root.Placement.Active) _hint.Text = "Click to place, Shift+click to place several, right-click to cancel";
        else _hint.Text = Root.Selection.HoverHint(GetViewport().GetMousePosition());

        RefreshPowers();
        RefreshCommandBar();
        _info.Text = _hoverInfo ?? _selectionInfo;
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
        // As a purchase preview the name and price lead; for a selected object the caller already printed its name.
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
        lines.Add(string.Join("   ", traits));
        if (d.Abilities.Count > 0) lines.Add("Abilities: " + string.Join(", ", d.Abilities.Select(a => a.Name)));
        return string.Join("\n", lines);
    }

    private string NameOf(string id)
    {
        var r = Root.World.Rules;
        if (r.Buildings.TryGetValue(id, out var b)) return b.Name;
        // A tech tag: name the building that provides it.
        var provider = r.Buildings.Values.FirstOrDefault(x => x.Provides.Contains(id));
        return provider?.Name ?? id.Replace('_', ' ');
    }

    // ------------------------------------------------------------------ command bar

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

    private void RefreshCommandBar()
    {
        var w = Root.World;
        var player = w.Player(Root.LocalPlayer);
        var me = Root.LocalPlayer;
        var e = Primary();
        var specs = new List<ButtonSpec>();

        if (e is null)
        {
            var inspected = w.Get(Root.Selection.InspectId);
            if (inspected is not null)
            {
                var owner = inspected.Owner < 0 ? "Neutral" : w.Player(inspected.Owner).Name;
                _selectionInfo = $"{inspected.Def.Name}  [{owner}]   HP {inspected.Hp:0} / {inspected.MaxHp:0}\n{Describe(inspected.Def, false)}";
            }
            else _selectionInfo = "Nothing selected\nLeft-drag: select   Right-click: move / attack / enter / capture\nA: attack-move   S: stop   Ctrl+1-9: set group, 1-9: recall\nH: headquarters   Space: last alert   Click anything to inspect it";
        }
        else
        {
            var count = Root.Selection.Selected.Count;
            var title = count > 1 && !e.IsBuilding ? $"{count} units  ({e.Def.Name})" : e.Def.Name;
            var state = e.UnderConstruction ? $"  constructing {e.BuildProgress * 100:0}%" : "";
            var extra = "";
            if (e.IsHarvester) extra += $"   carrying {e.Carried}";
            if (e.Level > 0) extra += $"   veteran {new string('*', e.Level)}";
            if (e.SalvageLevel > 0) extra += $"   salvage {e.SalvageLevel}";
            if (e.Statuses.Count > 0) extra += "   " + string.Join(" ", e.Statuses.Select(st => st.Type));
            if (e.Unit is { Ammo: > 0 } au) extra += $"   ammo {e.Ammo}/{au.Ammo}";
            if (e.Passengers.Count > 0) extra += $"   {e.Passengers.Count} inside";
            _selectionInfo = $"{title}{state}   HP {e.Hp:0} / {e.MaxHp:0}{extra}\n{Describe(e.Def, false)}";

            if (e.IsBuilding) BuildingButtons(e, specs);
            else UnitButtons(e, specs);
        }

        // Rebuild controls only when the set of buttons changes; texts and enabled state update in place so hover survives.
        var sig = string.Join("|", specs.Select(s => s.Key));
        if (sig != _lastButtonsSig)
        {
            _lastButtonsSig = sig;
            _hoverInfo = null;
            foreach (var c in _buttons.GetChildren()) c.QueueFree();
            _live.Clear();
            foreach (var spec in specs)
            {
                var btn = new Button { CustomMinimumSize = new Vector2(124, 52), FocusMode = Control.FocusModeEnum.None };
                btn.AddThemeFontSizeOverride("font_size", 12);
                var captured = spec;
                btn.Pressed += () => { Root.Audio.PlayUi("click"); captured.OnPress(); };
                btn.MouseEntered += () => _hoverInfo = captured.Info;
                btn.MouseExited += () => { if (_hoverInfo == captured.Info) _hoverInfo = null; };
                _buttons.AddChild(btn);
                _live.Add((btn, spec));
            }
        }
        foreach (var (button, spec) in _live)
        {
            button.Text = spec.Text();
            button.Disabled = !spec.Enabled();
        }
        RefreshQueue(e);
    }

    private void BuildingButtons(Entity e, List<ButtonSpec> specs)
    {
        var w = Root.World;
        var me = Root.LocalPlayer;
        var player = w.Player(me);
        if (e.UnderConstruction)
        {
            specs.Add(new ButtonSpec("cancel-site", () => "Cancel\n(full refund)", () => true, "Cancel construction and get the full cost back.", () => w.Submit(new SellCommand(me, e.Id))));
            return;
        }
        var b = e.Building!;
        foreach (var uid in b.Produces)
        {
            var u = w.Rules.Unit(uid);
            specs.Add(new ButtonSpec("unit:" + uid, () => $"{u.Name}\n${(int)(u.Cost * player.DiscountMult)}",
                () => w.HasPrereqs(me, u.Prereqs, out _) && player.Cash >= u.Cost * player.DiscountMult, Describe(u),
                () => w.Submit(new ProduceCommand(me, e.Id, uid))));
        }
        foreach (var upid in b.Upgrades)
        {
            var up = w.Rules.Upgrade(upid);
            var info = $"{up.Name}   ${up.Cost}   {up.Time:0}s\n{up.Description}" + (up.Prereqs.Count > 0 ? "\nRequires: " + string.Join(", ", up.Prereqs.Select(NameOf)) : "");
            specs.Add(new ButtonSpec("up:" + upid, () => player.Has(upid) ? $"{up.Name}\n(done)" : $"{up.Name}\n${up.Cost}",
                () => !player.Has(upid) && w.HasPrereqs(me, up.Prereqs, out _) && player.Cash >= up.Cost, info,
                () => w.Submit(new ProduceCommand(me, e.Id, upid))));
        }
        if (e.Def.GarrisonSlots > 0)
            specs.Add(new ButtonSpec("evacuate", () => $"Evacuate\n({(b.TunnelHub ? player.TunnelPool.Count : e.Passengers.Count)} inside)",
                () => (b.TunnelHub ? player.TunnelPool.Count : e.Passengers.Count) > 0, b.TunnelHub ? "Everyone in the tunnel network comes out here." : "Everyone out.",
                () => w.Submit(new UngarrisonCommand(me, e.Id))));
        if (b.Superweapon is { } sw)
            specs.Add(new ButtonSpec("sw", () => e.SuperweaponCharge >= sw.ChargeTime ? $"FIRE\n{sw.Name}" : $"{sw.Name}\n{(sw.ChargeTime - e.SuperweaponCharge) / 60f:0}:{(sw.ChargeTime - e.SuperweaponCharge) % 60f:00}",
                () => e.SuperweaponCharge >= sw.ChargeTime, $"{sw.Name}: click, then pick a target anywhere on the map.",
                () => Root.Selection.Arm(new SelectionController.Pending("superweapon", "", e.Id, sw.Name, false))));
        if (e.Def.Cost > 0)
            specs.Add(new ButtonSpec("sell", () => $"Sell\n+${e.Def.Cost / 2}", () => true, "Sell this building for half its cost.", () => w.Submit(new SellCommand(me, e.Id))));
    }

    private void UnitButtons(Entity e, List<ButtonSpec> specs)
    {
        var w = Root.World;
        var me = Root.LocalPlayer;
        var player = w.Player(me);
        int[] Sel() => Root.Selection.Selected.ToArray();

        if (e.IsBuilder)
        {
            foreach (var bd in w.Rules.Buildings.Values.Where(x => x.Faction == player.Faction.Id).OrderBy(x => x.Cost))
            {
                var def = bd;
                if (def.Hq && w.Entities.Any(x => x.Owner == me && x.Building?.Hq == true)) continue;
                specs.Add(new ButtonSpec("build:" + def.Id, () => $"{def.Name}\n${def.Cost}",
                    () => w.HasPrereqs(me, def.Prereqs, out _) && player.Cash >= def.Cost, Describe(def),
                    () => Root.Placement.Begin(def, e.Id)));
            }
        }
        if (e.IsHarvester)
            specs.Add(new ButtonSpec("harvest", () => "Harvest\nnearest", () => true, "Go to the nearest supply pile. Right-click a pile to choose one.", () => w.Submit(new HarvestCommand(me, Sel(), 0))));
        for (var i = 0; i < e.Def.Abilities.Count; i++)
        {
            var ab = e.Def.Abilities[i];
            var idx = i;
            specs.Add(new ButtonSpec("ab:" + ab.Id, () => e.AbilityCooldowns.Length > idx && e.AbilityCooldowns[idx] > 0f ? $"{ab.Name}\n{e.AbilityCooldowns[idx]:0}s" : ab.Name,
                () => e.AbilityCooldowns.Length > idx && e.AbilityCooldowns[idx] <= 0f, $"{ab.Name}\n{ab.Description}",
                () =>
                {
                    if (ab.Target == "none") w.Submit(new AbilityCommand(me, Sel(), ab.Id, Vec2.Zero, 0));
                    else Root.Selection.Arm(new SelectionController.Pending("ability", ab.Id, 0, ab.Name, ab.Target is "unit" or "building"));
                }));
        }
        if (e.Unit is { CanCapture: true })
            specs.Add(new ButtonSpec("capture", () => "Capture", () => true, "Capture an oil derrick or other tech building: click this, then the building. Right-clicking the building does the same. Takes 12 seconds next to it.",
                () => Root.Selection.Arm(new SelectionController.Pending("capture", "", 0, "Capture", true))));
        if (e.Def.IsInfantry)
            specs.Add(new ButtonSpec("enter", () => "Enter\nbuilding", () => true, "Garrison a civilian building, bunker, transport or tunnel: click this, then the target. Right-clicking it does the same.",
                () => Root.Selection.Arm(new SelectionController.Pending("garrison", "", 0, "Enter", true))));
        if (e.Passengers.Count > 0 || e.Def.GarrisonSlots > 0)
            specs.Add(new ButtonSpec("unload", () => $"Unload\n({e.Passengers.Count})", () => e.Passengers.Count > 0, "Passengers out.", () => w.Submit(new UngarrisonCommand(me, e.Id))));
        if (e.HasWeapons)
            specs.Add(new ButtonSpec("amove", () => "Attack-move\n(A)", () => true, "Move and fight anything on the way: click this, then a destination.", () => Root.Selection.ArmAttackMove()));
        specs.Add(new ButtonSpec("stop", () => "Stop\n(S)", () => true, "Halt and drop current orders.", () => w.Submit(new StopCommand(me, Sel()))));
    }

    private void RefreshQueue(Entity? e)
    {
        var items = e?.Queue?.Items;
        var sig = items is null ? "" : e!.Id + ":" + string.Join(",", items.Select(i => i.Id));
        if (sig != _lastQueueSig)
        {
            _lastQueueSig = sig;
            foreach (var c in _queueBox.GetChildren()) c.QueueFree();
            if (items is not null)
                for (var i = 0; i < items.Count; i++)
                {
                    var idx = i;
                    var btn = new Button { CustomMinimumSize = new Vector2(92, 40), TooltipText = "Click to cancel (refund)", FocusMode = Control.FocusModeEnum.None };
                    btn.AddThemeFontSizeOverride("font_size", 11);
                    btn.Pressed += () => Root.World.Submit(new CancelProduceCommand(Root.LocalPlayer, e!.Id, idx));
                    _queueBox.AddChild(btn);
                }
        }
        if (items is null) return;
        var children = _queueBox.GetChildren();
        for (var i = 0; i < children.Count && i < items.Count; i++)
        {
            var item = items[i];
            var name = item.IsUpgrade ? Root.World.Rules.Upgrade(item.Id).Name : Root.World.Rules.Unit(item.Id).Name;
            if (children[i] is Button b) b.Text = i == 0 ? $"{name}\n{item.Progress * 100:0}%" : name;
        }
    }

    // ------------------------------------------------------------------ powers

    private void RefreshPowers()
    {
        var w = Root.World;
        var p = w.Player(Root.LocalPlayer);
        var sig = $"{p.Rank}/{p.Points}/{string.Join(",", p.PowersOwned)}/{(int)w.Time}/{string.Join(",", w.Entities.Where(e => e.Owner == Root.LocalPlayer && e.Building?.Superweapon is not null).Select(e => e.Id))}";
        if (sig == _lastPowersSig) return;
        _lastPowersSig = sig;
        foreach (var c in _powersBox.GetChildren()) if (c != _rank) c.QueueFree();
        _rank.Text = $"Rank {p.Rank}   {p.Points} point{(p.Points == 1 ? "" : "s")}   {p.Xp} xp";
        foreach (var pid in p.Faction.Powers)
        {
            var def = w.Rules.Power(pid);
            Button btn;
            if (!p.HasPower(pid))
            {
                var can = p.Points > 0 && p.Rank >= def.Rank;
                btn = new Button { Text = $"Buy: {def.Name}  (rank {def.Rank})", Disabled = !can, TooltipText = def.Description, Alignment = HorizontalAlignment.Left };
                btn.Pressed += () => w.Submit(new BuyPowerCommand(Root.LocalPlayer, pid));
            }
            else
            {
                var left = p.PowerReadyAt(pid) - w.Time;
                var passive = def.Effect.Type == "bounty";
                var ready = left <= 0f && !passive;
                btn = new Button
                {
                    Text = passive ? $"{def.Name}  (active)" : ready ? $"USE: {def.Name}" : $"{def.Name}  {left:0}s",
                    Disabled = !ready, TooltipText = def.Description, Alignment = HorizontalAlignment.Left,
                };
                btn.Pressed += () =>
                {
                    if (def.Target == "none") w.Submit(new UsePowerCommand(Root.LocalPlayer, pid, Vec2.Zero));
                    else Root.Selection.Arm(new SelectionController.Pending("power", pid, 0, def.Name, false));
                };
            }
            btn.FocusMode = Control.FocusModeEnum.None;
            btn.AddThemeFontSizeOverride("font_size", 12);
            var info = $"{def.Name}  (rank {def.Rank}, recharge {def.Cooldown:0}s)\n{def.Description}";
            btn.MouseEntered += () => _hoverInfo = info;
            btn.MouseExited += () => { if (_hoverInfo == info) _hoverInfo = null; };
            _powersBox.AddChild(btn);
        }
        foreach (var b in w.Entities.Where(e => e.Owner == Root.LocalPlayer && e.Operational && e.Building?.Superweapon is not null))
        {
            var sw = b.Building!.Superweapon!;
            var left = sw.ChargeTime - b.SuperweaponCharge;
            var btn = new Button { Text = left <= 0f ? $"FIRE {sw.Name}" : $"{sw.Name}  {(int)left / 60}:{(int)left % 60:00}", Disabled = left > 0f, Alignment = HorizontalAlignment.Left, FocusMode = Control.FocusModeEnum.None };
            btn.Modulate = left <= 0f ? new Color(1f, 0.6f, 0.3f) : Colors.White;
            var id = b.Id;
            btn.Pressed += () => Root.Selection.Arm(new SelectionController.Pending("superweapon", "", id, sw.Name, false));
            btn.AddThemeFontSizeOverride("font_size", 12);
            _powersBox.AddChild(btn);
        }
    }
}
