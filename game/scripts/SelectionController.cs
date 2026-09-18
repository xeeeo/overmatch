using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Click, shift-click and drag-box selection; right-click move/attack; A + click attack-move. Draws the selection box.</summary>
public partial class SelectionController : Control
{
    private const float ClickTolerance = 6f;

    public int Player { get; set; }
    public GameRoot Root { get; set; } = null!;

    private readonly HashSet<int> _selected = new();
    private Vector2 _dragStart;
    private bool _dragging;
    private bool _boxActive;
    private bool _attackMoveArmed;

    /// <summary>A pending click-to-target action: attack-move, a power, an ability or a superweapon.</summary>
    public sealed record Pending(string Kind, string Id, int BuildingId, string Label, bool NeedsEntity);
    public Pending? PendingTarget { get; private set; }

    private readonly Dictionary<int, HashSet<int>> _groups = new();
    private int _lastGroup = -1;
    private ulong _lastGroupTime;

    public IReadOnlyCollection<int> Selected => _selected;

    /// <summary>Right-click on the minimap: move (or attack-move if armed) to a map position.</summary>
    public void OrderMoveTo(Vec2 target)
    {
        PruneSelection();
        var units = _selected.Where(id => Root.World.Get(id) is { IsBuilding: false }).ToArray();
        if (units.Length == 0) return;
        if (_attackMoveArmed) Root.World.Submit(new AttackMoveCommand(Player, units, target));
        else Root.World.Submit(new MoveCommand(Player, units, target));
        _attackMoveArmed = false;
        Respond("move");
    }

    public void JumpToHq()
    {
        var hq = Root.World.Entities.FirstOrDefault(e => e.Owner == Player && e.Building is { Hq: true }) ?? Root.World.Entities.FirstOrDefault(e => e.Owner == Player && e.IsBuilding);
        if (hq is null) return;
        Root.Camera.Position = MapView.ToWorld(hq.Pos);
        SelectOnly(hq.Id);
    }

    private void Respond(string ev)
    {
        var first = _selected.Select(id => Root.World.Get(id)).FirstOrDefault(e => e is not null);
        if (first is not null) Root.Audio.UnitResponse(first, ev);
    }

    private bool HandleKey(InputEventKey key)
    {
        if (!key.Pressed || key.Echo) return false;
        var digit = key.Keycode >= Key.Key0 && key.Keycode <= Key.Key9 ? (int)(key.Keycode - Key.Key0) : -1;
        if (digit >= 0)
        {
            if (key.CtrlPressed || key.MetaPressed)
            {
                var members = new HashSet<int>(_selected.Where(id => Root.World.Get(id) is { IsBuilding: false }));
                // A unit wears one number: joining a group leaves any other.
                foreach (var other in _groups.Values) other.ExceptWith(members);
                _groups[digit] = members;
                Root.Hud.Say($"Group {digit} set ({_groups[digit].Count})");
            }
            else if (_groups.TryGetValue(digit, out var g))
            {
                g.RemoveWhere(id => Root.World.Get(id) is null);
                if (g.Count == 0) return true;
                if (key.AltPressed)
                {
                    var seen = g.Select(id => Root.World.Get(id)!).ToList();
                    Root.Camera.Position = MapView.ToWorld(new Vec2(seen.Average(e => e.Pos.X), seen.Average(e => e.Pos.Y)));
                    return true;
                }
                if (key.ShiftPressed)
                {
                    foreach (var id in g) _selected.Add(id);
                    InspectId = 0;
                    ApplySelectionVisuals();
                    return true;
                }
                var now = Time.GetTicksMsec();
                var again = _lastGroup == digit && now - _lastGroupTime < 400;
                _lastGroup = digit;
                _lastGroupTime = now;
                _selected.Clear();
                foreach (var id in g) _selected.Add(id);
                InspectId = 0;
                ApplySelectionVisuals();
                Respond("select");
                if (again)
                {
                    // Double-tap: centre the camera on the group.
                    var alive = g.Select(id => Root.World.Get(id)!).ToList();
                    var c = new Vec2(alive.Average(e => e.Pos.X), alive.Average(e => e.Pos.Y));
                    Root.Camera.Position = MapView.ToWorld(c);
                }
            }
            return true;
        }
        if (key.CtrlPressed || key.MetaPressed || key.AltPressed) return false;
        switch (key.Keycode)
        {
            case Key.G:
                if (_selected.Any(id => Root.World.Get(id) is { HasWeapons: true, IsBuilding: false })) { Arm(new Pending("guard", "", 0, "Guard", false)); Root.Hud.Say("Guard: click the area to hold"); }
                return true;
            case Key.X:
                if (_selected.Count > 0) Root.World.Submit(new ScatterCommand(Player, _selected.ToArray()));
                return true;
            case Key.R:
            {
                var air = _selected.Where(id => Root.World.Get(id) is { Unit.IsAir: true }).ToArray();
                if (air.Length > 0) Root.World.Submit(new ReturnToBaseCommand(Player, air));
                return true;
            }
            case Key.V:
                foreach (var id in _selected.Where(id => Root.World.Get(id) is { Passengers.Count: > 0 })) Root.World.Submit(new UngarrisonCommand(Player, id));
                return true;
            case Key.Q:
                SelectWhere(e => e.HasWeapons && !e.IsHarvester && !e.IsBuilder, false);
                return true;
            case Key.W:
                SelectWhere(e => e.Unit is { IsAir: true } && e.HasWeapons, false);
                return true;
            case Key.E:
            {
                var like = _selected.Select(id => Root.World.Get(id)).FirstOrDefault(e => e is { IsBuilding: false });
                if (like is null) return true;
                var now = Time.GetTicksMsec();
                var everywhere = now - _lastMatchTime < 400;
                _lastMatchTime = now;
                var ids = _selected.Select(id => Root.World.Get(id)?.Def.Id).Where(d => d is not null).ToHashSet();
                SelectWhere(e => ids.Contains(e.Def.Id), !everywhere);
                return true;
            }
            case Key.I:
            {
                var idle = Root.World.Entities.Where(e => e.Alive && e.Owner == Player && e.IsBuilder && !e.IsInside && e.Move is null
                    && e.BuildTargetId == 0 && e.RepairTargetId == 0 && e.HarvestState == HarvestState.Idle).OrderBy(e => e.Id).ToList();
                if (idle.Count == 0) { Root.Hud.Say("No idle builders"); return true; }
                var next = idle.FirstOrDefault(e => e.Id > _lastIdleId) ?? idle[0];
                _lastIdleId = next.Id;
                SelectOnly(next.Id);
                Root.Camera.Position = MapView.ToWorld(next.Pos);
                return true;
            }
            case Key.F9:
                Root.Hud.Visible = !Root.Hud.Visible;
                return true;
            case Key.H:
                JumpToHq();
                return true;
            case Key.Space:
                if (Root.LastAlert is { } a) Root.Camera.Position = MapView.ToWorld(a);
                return true;
            case Key.M:
                Root.Audio.ToggleMusic();
                Root.Hud.Say(Root.Audio.MusicOn ? "Music on" : "Music off");
                return true;
        }
        return false;
    }

    private ulong _lastMatchTime;
    private int _lastIdleId;

    /// <summary>The control group a unit belongs to, or -1. Drawn as a number tag by the overlay.</summary>
    public int GroupOf(int entityId)
    {
        foreach (var (n, g) in _groups) if (g.Contains(entityId)) return n;
        return -1;
    }

    /// <summary>Scripted runs only: assign a control group without the keyboard.</summary>
    public void DebugSetGroup(int n, IEnumerable<int> ids) => _groups[n] = new HashSet<int>(ids);

    private void SelectWhere(Func<Entity, bool> match, bool onScreenOnly)
    {
        var cam = Root.Camera.Camera;
        var rect = GetViewport().GetVisibleRect();
        _selected.Clear();
        InspectId = 0;
        foreach (var e in Root.World.Entities)
        {
            if (!e.Alive || e.Owner != Player || e.IsBuilding || e.IsInside || !match(e)) continue;
            if (onScreenOnly)
            {
                var world = MapView.ToWorld(e.Pos, 0.5f + (e.Unit?.FlightHeight ?? 0f));
                if (cam.IsPositionBehind(world) || !rect.HasPoint(cam.UnprojectPosition(world))) continue;
            }
            _selected.Add(e.Id);
        }
        ApplySelectionVisuals();
        if (_selected.Count > 0) Respond("select");
    }

    /// <summary>Double-click: every unit of the same type on screen.</summary>
    private void SelectSameTypeOnScreen(Entity like)
    {
        var cam = Root.Camera.Camera;
        var rect = GetViewport().GetVisibleRect();
        _selected.Clear();
        foreach (var e in Root.World.Entities)
        {
            if (!e.Alive || e.Owner != Player || e.IsBuilding || e.IsInside || e.Def.Id != like.Def.Id) continue;
            var world = MapView.ToWorld(e.Pos, 0.5f + (e.Unit?.FlightHeight ?? 0f));
            if (!cam.IsPositionBehind(world) && rect.HasPoint(cam.UnprojectPosition(world))) _selected.Add(e.Id);
        }
        ApplySelectionVisuals();
    }
    public bool AttackMoveArmed => _attackMoveArmed || PendingTarget is not null;
    public void ArmAttackMove() => _attackMoveArmed = _selected.Count > 0;
    public void Arm(Pending p) { PendingTarget = p; _attackMoveArmed = false; }
    public void Disarm() { PendingTarget = null; _attackMoveArmed = false; }
    public void SelectOnly(int id)
    {
        _selected.Clear();
        if (id != 0) _selected.Add(id);
        ApplySelectionVisuals();
    }
    public string Signature() => string.Join(",", _selected.OrderBy(i => i));

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Root.Placement.Active) return;
        if (@event is InputEventKey k && HandleKey(k)) { GetViewport().SetInputAsHandled(); return; }
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true } dc && !Root.Hud.IsMouseOverBar(dc.Position) && PendingTarget is null)
        {
            var like = Pick(dc.Position, e => e.Owner == Player && !e.IsBuilding);
            if (like is not null) { SelectSameTypeOnScreen(like); _dragging = false; return; }
        }
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    if (Root.Hud.IsMouseOverBar(mb.Position)) return;
                    if (PendingTarget is not null)
                    {
                        ApplyPending(mb.Position);
                        return;
                    }
                    if (_attackMoveArmed)
                    {
                        IssueAttackMove(mb.Position);
                        _attackMoveArmed = false;
                        return;
                    }
                    _dragStart = mb.Position;
                    _dragging = true;
                    _boxActive = false;
                }
                else if (_dragging)
                {
                    _dragging = false;
                    var additive = mb.ShiftPressed;
                    if (_boxActive) BoxSelect(_dragStart, mb.Position, additive);
                    else ClickSelect(mb.Position, additive);
                    _boxActive = false;
                    if (_selected.Count > 0) Respond("select");
                    QueueRedraw();
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
            {
                if (PendingTarget is not null || _attackMoveArmed) { Disarm(); return; }
                if (!Root.Hud.IsMouseOverBar(mb.Position)) IssueContextOrder(mb.Position);
            }
        }
        else if (@event is InputEventMouseMotion mm && _dragging)
        {
            if (!_boxActive && (mm.Position - _dragStart).Length() > ClickTolerance) _boxActive = true;
            if (_boxActive) QueueRedraw();
        }
        else if (@event.IsActionPressed("select_all"))
        {
            _selected.Clear();
            foreach (var e in Root.World.Entities)
                if (e.Alive && e.Owner == Player && !e.IsBuilding && e.HasWeapons) _selected.Add(e.Id);
            ApplySelectionVisuals();
        }
        else if (@event.IsActionPressed("stop"))
        {
            _attackMoveArmed = false;
            if (_selected.Count > 0) Root.World.Submit(new StopCommand(Player, _selected.ToArray()));
        }
        else if (@event.IsActionPressed("attack_move"))
        {
            if (_selected.Count > 0) _attackMoveArmed = true;
        }
        else if (@event.IsActionPressed("cancel"))
        {
            Disarm();
        }
    }

    private void ApplyPending(Vector2 screen)
    {
        var p = PendingTarget!;
        var ground = Root.Camera.GroundPoint(screen);
        var entity = p.NeedsEntity ? Pick(screen, e => true) : null;
        if (p.NeedsEntity && entity is null) { Root.Hud.Say("Pick a target"); return; }
        var target = entity is not null ? entity.Pos : ground is { } g ? MapView.ToSim(g) : (Vec2?)null;
        if (target is null) return;
        switch (p.Kind)
        {
            case "power":
                Root.World.Submit(new UsePowerCommand(Player, p.Id, target.Value));
                break;
            case "ability":
                Root.World.Submit(new AbilityCommand(Player, _selected.ToArray(), p.Id, target.Value, entity?.Id ?? 0));
                break;
            case "capture":
                if (entity?.Building is { Capturable: true } && entity.Owner != Player)
                    Root.World.Submit(new CaptureCommand(Player, _selected.Where(id => Root.World.Get(id)?.Unit?.CanCapture == true).ToArray(), entity.Id));
                else { Root.Hud.Say("That cannot be captured"); return; }
                break;
            case "garrison":
                if (entity is not null && entity.Def.GarrisonSlots > 0 && (entity.Owner == Player || entity.Owner < 0))
                    Root.World.Submit(new GarrisonCommand(Player, _selected.Where(id => Root.World.Get(id) is { Def.IsInfantry: true }).ToArray(), entity.Id));
                else { Root.Hud.Say("Infantry cannot enter that"); return; }
                break;
            case "guard":
                Root.World.Submit(new GuardCommand(Player, _selected.ToArray(), target.Value));
                Respond("attack");
                break;
            case "superweapon":
                Root.World.Submit(new FireSuperweaponCommand(Player, p.BuildingId, target.Value));
                break;
        }
        Root.ShowMarker(MapView.ToWorld(target.Value), new Color(1f, 0.5f, 0.2f));
        Disarm();
    }

    private Entity? Pick(Vector2 screen, Func<Entity, bool> filter) => Picking.PickAt(Root, screen, filter);

    /// <summary>A neutral or enemy object the player clicked to read about. No commands apply to it.</summary>
    public int InspectId { get; private set; }

    /// <summary>What a right-click would do at this point, for the hover hint.</summary>
    public string HoverHint(Vector2 screen)
    {
        if (_selected.Count == 0 || Root.Hud.IsMouseOverBar(screen)) return "";
        var infantry = _selected.Any(id => Root.World.Get(id) is { Def.IsInfantry: true });
        var target = Pick(screen, e => !_selected.Contains(e.Id));
        if (target is null) return "";
        if (target.Building is { Capturable: true } && target.Owner != Player)
            return _selected.Any(id => Root.World.Get(id)?.Unit?.CanCapture == true) ? $"Right-click: capture {target.Def.Name}" : $"{target.Def.Name}: capture it with basic infantry";
        if (infantry && target.Def.GarrisonSlots > 0 && (target.Owner == Player || target.Owner < 0) && target.Building is not { IsHole: true })
            return $"Right-click: enter {target.Def.Name}";
        if (target.Owner != Player && target.Owner >= 0) return $"Right-click: attack {target.Def.Name}";
        if (target.UnderConstruction && target.Owner == Player && _selected.Any(id => Root.World.Get(id) is { IsBuilder: true })) return "Right-click: help build";
        if (target.Owner == Player && target.IsBuilding && target.Hp < target.MaxHp && _selected.Any(id => Root.World.Get(id) is { IsBuilder: true })) return $"Right-click: repair {target.Def.Name}";
        if (target.Owner == Player && target.Building is { Pads: > 0 } && target.Operational && _selected.Any(id => Root.World.Get(id) is { Unit.IsAir: true })) return "Right-click: return to base for repair and rearm";
        if (target.Owner == Player && target.Operational && target.Def.Auras.FirstOrDefault(a => a.Type == "heal" && a.Targets != "all") is { } bay
            && _selected.Any(id => Root.World.Get(id) is { } u && u.Hp < u.MaxHp && bay.Affects(u.Def))) return $"{target.Def.Name}: damaged units beside it are repaired";
        return "";
    }

    private void ClickSelect(Vector2 screen, bool additive)
    {
        var hit = Pick(screen, e => e.Owner == Player);
        InspectId = 0;
        if (hit is null && !additive)
        {
            // Nothing of ours under the cursor: let the player read about whatever is there.
            var other = Pick(screen, e => e.Owner != Player);
            if (other is not null) { _selected.Clear(); InspectId = other.Id; ApplySelectionVisuals(); return; }
        }
        if (!additive || hit is { IsBuilding: true }) _selected.Clear();
        if (hit is not null)
        {
            if (additive && !hit.IsBuilding && !_selected.Add(hit.Id)) _selected.Remove(hit.Id);
            else _selected.Add(hit.Id);
        }
        ApplySelectionVisuals();
    }

    private void BoxSelect(Vector2 a, Vector2 b, bool additive)
    {
        var rect = new Rect2(new Vector2(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y)), (b - a).Abs());
        var cam = Root.Camera.Camera;
        if (!additive) _selected.Clear();
        foreach (var e in Root.World.Entities)
        {
            if (!e.Alive || e.Owner != Player || e.IsBuilding) continue;
            var world = MapView.ToWorld(e.Pos, 0.5f + (e.Unit?.FlightHeight ?? 0f));
            if (cam.IsPositionBehind(world)) continue;
            if (rect.HasPoint(cam.UnprojectPosition(world))) _selected.Add(e.Id);
        }
        // A box that caught units drops any building from the selection.
        if (_selected.Any(id => Root.World.Get(id) is { IsBuilding: false })) _selected.RemoveWhere(id => Root.World.Get(id) is { IsBuilding: true });
        ApplySelectionVisuals();
    }

    /// <summary>Right-click: attack an enemy under the cursor, otherwise move.</summary>
    private void IssueContextOrder(Vector2 screen)
    {
        PruneSelection();
        if (_selected.Count == 0) return;
        var ground0 = Root.Camera.GroundPoint(screen);
        // A selected building: right-click sets its rally point.
        var building = _selected.Select(id => Root.World.Get(id)).FirstOrDefault(e => e is { IsBuilding: true });
        if (building is not null)
        {
            if (ground0 is { } rg)
            {
                Root.World.Submit(new RallyCommand(Player, building.Id, MapView.ToSim(rg)));
                Root.ShowMarker(rg, new Color(0.4f, 0.8f, 1f));
            }
            return;
        }
        // Harvesters right-clicked on a pile go harvest it; on a site, builders help.
        if (ground0 is { } g0)
        {
            var sim = MapView.ToSim(g0);
            var pile = Root.World.Piles.FirstOrDefault(p => !p.Depleted && Vec2.Distance(p.Pos, sim) < 2.5f);
            var harvesters = _selected.Where(id => Root.World.Get(id) is { IsHarvester: true }).ToArray();
            if (pile is not null && harvesters.Length > 0)
            {
                Root.World.Submit(new HarvestCommand(Player, harvesters, pile.Id));
                Root.ShowMarker(g0, new Color(1f, 0.85f, 0.3f));
                return;
            }
            var site = Pick(screen, e => e.Owner == Player && e.UnderConstruction);
            var builders = _selected.Where(id => Root.World.Get(id) is { IsBuilder: true }).ToArray();
            if (site is not null && builders.Length > 0)
            {
                Root.World.Submit(new AssistBuildCommand(Player, builders, site.Id));
                Root.ShowMarker(g0, new Color(0.4f, 0.8f, 1f));
                return;
            }
            var hurt = Pick(screen, e => e.Owner == Player && e.IsBuilding && !e.UnderConstruction && e.Hp < e.MaxHp);
            if (hurt is not null && builders.Length > 0)
            {
                Root.World.Submit(new RepairCommand(Player, builders, hurt.Id));
                Root.ShowMarker(g0, new Color(0.3f, 0.9f, 0.3f));
                return;
            }
            var airfield = Pick(screen, e => e.Owner == Player && e.Operational && e.Building is { Pads: > 0 });
            var aircraft = _selected.Where(id => Root.World.Get(id) is { Unit.IsAir: true }).ToArray();
            if (airfield is not null && aircraft.Length > 0)
            {
                Root.World.Submit(new ReturnToBaseCommand(Player, aircraft));
                Root.ShowMarker(g0, new Color(0.3f, 0.9f, 0.3f));
                return;
            }
        }
        // Infantry right-clicking a garrisonable building/transport enter it; capture-capable infantry capture neutral/enemy tech buildings.
        var infantry = _selected.Where(id => Root.World.Get(id) is { Def.IsInfantry: true }).ToArray();
        if (infantry.Length > 0)
        {
            var container = Pick(screen, e => e.Def.GarrisonSlots > 0 && (e.Owner == Player || e.Owner < 0) && e.Building is not { IsHole: true } && !_selected.Contains(e.Id));
            if (container is not null && container.Building is not { Capturable: true })
            {
                Root.World.Submit(new GarrisonCommand(Player, infantry, container.Id));
                Root.ShowMarker(MapView.ToWorld(container.Pos), new Color(0.4f, 0.8f, 1f));
                return;
            }
            var capturable = Pick(screen, e => e.Building is { Capturable: true } && e.Owner != Player);
            var capturers = infantry.Where(id => Root.World.Get(id)?.Unit?.CanCapture == true).ToArray();
            if (capturable is not null && capturers.Length > 0)
            {
                Root.World.Submit(new CaptureCommand(Player, capturers, capturable.Id));
                Root.ShowMarker(MapView.ToWorld(capturable.Pos), new Color(0.4f, 0.8f, 1f));
                return;
            }
        }
        var enemy = Pick(screen, e => e.Owner != Player && e.Owner >= 0);
        if (enemy is not null)
        {
            Root.World.Submit(new AttackCommand(Player, _selected.ToArray(), enemy.Id));
            Root.ShowMarker(MapView.ToWorld(enemy.Pos), new Color(1f, 0.35f, 0.3f));
            Respond("attack");
            return;
        }
        var ground = Root.Camera.GroundPoint(screen);
        if (ground is not { } g) return;
        Root.World.Submit(new MoveCommand(Player, _selected.ToArray(), MapView.ToSim(g)));
        Root.ShowMarker(g, new Color(0.4f, 1f, 0.4f));
        Respond("move");
    }

    private void IssueAttackMove(Vector2 screen)
    {
        PruneSelection();
        if (_selected.Count == 0) return;
        var ground = Root.Camera.GroundPoint(screen);
        if (ground is not { } g) return;
        Root.World.Submit(new AttackMoveCommand(Player, _selected.ToArray(), MapView.ToSim(g)));
        Root.ShowMarker(g, new Color(1f, 0.6f, 0.2f));
        Respond("attack");
    }

    private void PruneSelection() => _selected.RemoveWhere(id => Root.World.Get(id) is null);

    private void ApplySelectionVisuals()
    {
        foreach (var (id, view) in Root.Views) view.Selected = _selected.Contains(id);
    }

    public override void _Draw()
    {
        if (!_boxActive) return;
        var mouse = GetViewport().GetMousePosition();
        var rect = new Rect2(new Vector2(Mathf.Min(_dragStart.X, mouse.X), Mathf.Min(_dragStart.Y, mouse.Y)), (mouse - _dragStart).Abs());
        DrawRect(rect, new Color(0.4f, 1f, 0.4f, 0.12f));
        DrawRect(rect, new Color(0.4f, 1f, 0.4f, 0.9f), false, 1.5f);
    }

    public override void _Process(double delta)
    {
        if (_boxActive) QueueRedraw();
    }
}
