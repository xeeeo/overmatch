using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Top bar (cash, power) and the bottom command bar (selection info, production queue, build/produce buttons).</summary>
public partial class Hud : CanvasLayer
{
    public GameRoot Root { get; set; } = null!;

    private Label _cash = null!;
    private Label _power = null!;
    private Label _info = null!;
    private Label _debug = null!;
    private Label _message = null!;
    private float _messageTtl;
    private VBoxContainer _selectionBox = null!;
    private HBoxContainer _queueBox = null!;
    private GridContainer _buttons = null!;
    private PanelContainer _commandBar = null!;
    private VBoxContainer _powersBox = null!;
    private Label _rank = null!;
    private string _lastPowersSig = "";
    private string _lastSignature = "";
    private int _lastCash = -1;
    private double _refreshTimer;

    public const float BarHeight = 168f;

    public override void _Ready()
    {
        BuildTopBar();
        BuildCommandBar();
        BuildPowersPanel();
        _message = new Label { Modulate = new Color(1f, 0.85f, 0.4f), HorizontalAlignment = HorizontalAlignment.Center };
        _message.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _message.Position = new Vector2(0, 70);
        _message.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_message);
        _message.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _message.OffsetTop = 60;
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
        row.AddChild(MakeLabel("OVERMATCH", 16));
        row.AddChild(_cash);
        row.AddChild(_power);
        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(spacer);
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

        _selectionBox = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
        _info = MakeLabel("Nothing selected", 14);
        _info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _selectionBox.AddChild(_info);
        row.AddChild(_selectionBox);

        var mid = new VBoxContainer { CustomMinimumSize = new Vector2(360, 0) };
        mid.AddChild(MakeLabel("Queue", 12));
        _queueBox = new HBoxContainer();
        mid.AddChild(_queueBox);
        row.AddChild(mid);

        _buttons = new GridContainer { Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(_buttons);
        AddChild(_commandBar);
    }

    private void BuildPowersPanel()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        panel.OffsetTop = 36;
        panel.OffsetLeft = -230;
        panel.OffsetRight = -6;
        _powersBox = new VBoxContainer { CustomMinimumSize = new Vector2(220, 0) };
        _powersBox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(_powersBox);
        _rank = MakeLabel("Rank 1", 13);
        _powersBox.AddChild(_rank);
        AddChild(panel);
    }

    private void RefreshPowers()
    {
        var w = Root.World;
        var p = w.Player(Root.LocalPlayer);
        var sig = $"{p.Rank}/{p.Points}/{string.Join(",", p.PowersOwned)}/{(int)(w.Time / 5)}/{string.Join(",", w.Entities.Where(e => e.Owner == Root.LocalPlayer && e.Building?.Superweapon is not null).Select(e => e.Id + ":" + (int)e.SuperweaponCharge))}";
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
                var ready = left <= 0f;
                var passive = def.Target == "none" && def.Effect.Type == "bounty";
                btn = new Button
                {
                    Text = passive ? $"{def.Name}  (active)" : ready ? def.Name : $"{def.Name}  {left:0}s",
                    Disabled = !ready || passive, TooltipText = def.Description, Alignment = HorizontalAlignment.Left,
                };
                btn.Pressed += () =>
                {
                    if (def.Target == "none") w.Submit(new UsePowerCommand(Root.LocalPlayer, pid, Vec2.Zero));
                    else Root.Selection.Arm(new SelectionController.Pending("power", pid, 0, def.Name, false));
                };
            }
            btn.AddThemeFontSizeOverride("font_size", 12);
            _powersBox.AddChild(btn);
        }
        foreach (var b in w.Entities.Where(e => e.Owner == Root.LocalPlayer && e.Operational && e.Building?.Superweapon is not null))
        {
            var sw = b.Building!.Superweapon!;
            var left = sw.ChargeTime - b.SuperweaponCharge;
            var btn = new Button
            {
                Text = left <= 0f ? $"FIRE {sw.Name}" : $"{sw.Name}  {left / 60f:0}:{left % 60f:00}",
                Disabled = left > 0f, Alignment = HorizontalAlignment.Left,
            };
            btn.Modulate = left <= 0f ? new Color(1f, 0.6f, 0.3f) : Colors.White;
            var id = b.Id;
            btn.Pressed += () => Root.Selection.Arm(new SelectionController.Pending("superweapon", "", id, sw.Name, false));
            btn.AddThemeFontSizeOverride("font_size", 12);
            _powersBox.AddChild(btn);
        }
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

    public bool IsMouseOverBar(Vector2 pos) => _commandBar.GetGlobalRect().HasPoint(pos);

    public override void _Process(double delta)
    {
        var w = Root.World;
        var p = w.Player(Root.LocalPlayer);
        _cash.Text = $"$ {p.Cash:N0}";
        _power.Text = p.LowPower ? $"Power {p.PowerSupply} / {p.PowerDemand}  LOW POWER" : $"Power {p.PowerSupply} / {p.PowerDemand}";
        _power.Modulate = p.LowPower ? new Color(1f, 0.4f, 0.3f) : Colors.White;
        var mine = w.Entities.Count(e => e.Owner == Root.LocalPlayer && !e.IsBuilding);
        _debug.Text = $"units {mine}   tick {w.Tick}   {Engine.GetFramesPerSecond()} fps   speed {Root.Speed}x";

        if (_messageTtl > 0) { _messageTtl -= (float)delta; if (_messageTtl <= 0) _message.Text = ""; }
        RefreshPowers();
        if (Root.Selection.PendingTarget is { } pt && _messageTtl <= 0) _message.Text = $"{pt.Label}: click a target (right-click cancels)";

        _refreshTimer += delta;
        var sig = Root.Selection.Signature();
        if (sig != _lastSignature || p.Cash != _lastCash || _refreshTimer > 0.5)
        {
            _lastSignature = sig;
            _lastCash = p.Cash;
            _refreshTimer = 0;
            Refresh();
        }
        else RefreshQueueProgress();
    }

    private Entity? Primary()
    {
        var ids = Root.Selection.Selected;
        if (ids.Count == 0) return null;
        // Buildings first so a single building selection drives the bar.
        Entity? first = null;
        foreach (var id in ids)
        {
            var e = Root.World.Get(id);
            if (e is null) continue;
            if (e.IsBuilding) return e;
            first ??= e;
        }
        return first;
    }

    private void Refresh()
    {
        foreach (var c in _buttons.GetChildren()) c.QueueFree();
        foreach (var c in _queueBox.GetChildren()) c.QueueFree();
        var e = Primary();
        var w = Root.World;
        var player = w.Player(Root.LocalPlayer);

        if (e is null)
        {
            _info.Text = "Nothing selected\n\nLeft-drag: select   Right-click: move / attack\nA + click: attack-move   S: stop   Ctrl+A: all units";
            return;
        }

        var count = Root.Selection.Selected.Count;
        var title = count > 1 && !e.IsBuilding ? $"{count} units" : e.Def.Name;
        var state = e.UnderConstruction ? $"  constructing {e.BuildProgress * 100:0}%" : "";
        var extra = e.IsHarvester ? $"\ncarrying {e.Carried}" : "";
        if (e.Level > 0) extra += $"   veteran {new string('*', e.Level)}";
        if (e.SalvageLevel > 0) extra += $"   salvage {e.SalvageLevel}";
        if (e.Statuses.Count > 0) extra += "   " + string.Join(" ", e.Statuses.Select(st => st.Type));
        if (e.Unit is { Ammo: > 0 } au) extra += $"   ammo {e.Ammo}/{au.Ammo}";
        _info.Text = $"{title}{state}\nHP {e.Hp:0} / {e.MaxHp:0}{extra}\n{e.Def.Description}";

        if (e.IsBuilding)
        {
            if (e.UnderConstruction)
            {
                AddButton("Cancel\n(full refund)", true, "", () => w.Submit(new SellCommand(Root.LocalPlayer, e.Id)));
                return;
            }
            var b = e.Building!;
            foreach (var uid in b.Produces)
            {
                var u = w.Rules.Unit(uid);
                var ok = w.HasPrereqs(Root.LocalPlayer, u.Prereqs, out var missing);
                var afford = player.Cash >= u.Cost;
                var reason = !ok ? $"Requires {Name(missing)}" : !afford ? "Not enough cash" : u.Description;
                AddButton($"{u.Name}\n${u.Cost}", ok && afford, reason, () => w.Submit(new ProduceCommand(Root.LocalPlayer, e.Id, uid)));
            }
            foreach (var upid in b.Upgrades)
            {
                var up = w.Rules.Upgrade(upid);
                if (player.Has(upid)) { AddButton($"{up.Name}\n(done)", false, up.Description, () => { }); continue; }
                var ok = w.HasPrereqs(Root.LocalPlayer, up.Prereqs, out var missing);
                var afford = player.Cash >= up.Cost;
                var reason = !ok ? $"Requires {Name(missing)}" : !afford ? "Not enough cash" : up.Description;
                AddButton($"{up.Name}\n${up.Cost}", ok && afford, reason, () => w.Submit(new ProduceCommand(Root.LocalPlayer, e.Id, upid)));
            }
            if (e.Passengers.Count > 0 || (e.Building!.TunnelHub && player.TunnelPool.Count > 0))
            {
                var n = e.Building.TunnelHub ? player.TunnelPool.Count : e.Passengers.Count;
                AddButton($"Evacuate\n({n} inside)", true, "Everyone out", () => w.Submit(new UngarrisonCommand(Root.LocalPlayer, e.Id)));
            }
            if (e.Building!.Superweapon is { } sw)
                AddButton(e.SuperweaponCharge >= sw.ChargeTime ? $"FIRE\n{sw.Name}" : $"{sw.Name}\ncharging", e.SuperweaponCharge >= sw.ChargeTime, sw.Effect.Type,
                    () => Root.Selection.Arm(new SelectionController.Pending("superweapon", "", e.Id, sw.Name, false)));
            if (e.Owner == Root.LocalPlayer && e.Def.Cost > 0)
                AddButton($"Sell\n+${e.Def.Cost / 2}", true, "Sell this building for half its cost", () => w.Submit(new SellCommand(Root.LocalPlayer, e.Id)));
            if (e.Queue is not null)
            {
                for (var i = 0; i < e.Queue.Items.Count; i++)
                {
                    var idx = i;
                    var item = e.Queue.Items[i];
                    var name = item.IsUpgrade ? w.Rules.Upgrade(item.Id).Name : w.Rules.Unit(item.Id).Name;
                    var btn = new Button { Text = $"{name}\n{item.Progress * 100:0}%", CustomMinimumSize = new Vector2(84, 48), TooltipText = "Click to cancel" };
                    btn.Pressed += () => w.Submit(new CancelProduceCommand(Root.LocalPlayer, e.Id, idx));
                    _queueBox.AddChild(btn);
                }
            }
            return;
        }

        if (e.IsBuilder)
        {
            foreach (var bd in w.Rules.Buildings.Values.Where(x => x.Faction == player.Faction.Id).OrderBy(x => x.Cost))
            {
                if (bd.Hq && w.Entities.Any(x => x.Owner == Root.LocalPlayer && x.Building?.Hq == true)) continue;
                var ok = w.HasPrereqs(Root.LocalPlayer, bd.Prereqs, out var missing);
                var afford = player.Cash >= bd.Cost;
                var reason = !ok ? $"Requires {Name(missing)}" : !afford ? "Not enough cash" : bd.Description;
                var def = bd;
                AddButton($"{bd.Name}\n${bd.Cost}", ok && afford, reason, () => Root.Placement.Begin(def, e.Id));
            }
        }
        if (e.IsHarvester)
            AddButton("Harvest\nnearest", true, "Go to the nearest supply pile", () => w.Submit(new HarvestCommand(Root.LocalPlayer, Root.Selection.Selected.ToArray(), 0)));
        for (var i = 0; i < e.Def.Abilities.Count; i++)
        {
            var ab = e.Def.Abilities[i];
            var cd = e.AbilityCooldowns.Length > i ? e.AbilityCooldowns[i] : 0f;
            var label = cd > 0f ? $"{ab.Name}\n{cd:0}s" : ab.Name;
            AddButton(label, cd <= 0f, ab.Description, () =>
            {
                if (ab.Target == "none") w.Submit(new AbilityCommand(Root.LocalPlayer, Root.Selection.Selected.ToArray(), ab.Id, Vec2.Zero, 0));
                else Root.Selection.Arm(new SelectionController.Pending("ability", ab.Id, 0, ab.Name, ab.Target is "unit" or "building"));
            });
        }
        if (e.Passengers.Count > 0)
            AddButton($"Unload\n({e.Passengers.Count})", true, "Passengers out", () => w.Submit(new UngarrisonCommand(Root.LocalPlayer, e.Id)));
        AddButton("Stop\n(S)", true, "", () => w.Submit(new StopCommand(Root.LocalPlayer, Root.Selection.Selected.ToArray())));
        if (e.HasWeapons) AddButton("Attack-move\n(A)", true, "Then click a destination", () => Root.Selection.ArmAttackMove());
    }

    private void RefreshQueueProgress()
    {
        var e = Primary();
        if (e?.Queue is null) return;
        var children = _queueBox.GetChildren();
        for (var i = 0; i < children.Count && i < e.Queue.Items.Count; i++)
        {
            var item = e.Queue.Items[i];
            var name = item.IsUpgrade ? Root.World.Rules.Upgrade(item.Id).Name : Root.World.Rules.Unit(item.Id).Name;
            if (children[i] is Button b) b.Text = $"{name}\n{item.Progress * 100:0}%";
        }
    }

    private string Name(string id) =>
        Root.World.Rules.Buildings.TryGetValue(id, out var b) ? b.Name : id.Replace("coalition_", "").Replace('_', ' ');

    private void AddButton(string text, bool enabled, string tooltip, Action onPress)
    {
        var btn = new Button { Text = text, Disabled = !enabled, TooltipText = tooltip, CustomMinimumSize = new Vector2(118, 52) };
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.Pressed += onPress;
        _buttons.AddChild(btn);
    }
}
