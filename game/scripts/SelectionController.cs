using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Click, shift-click and drag-box selection; right-click move/attack; A + click attack-move. Draws the selection box.</summary>
public partial class SelectionController : Control
{
    private const float ClickTolerance = 6f;
    private const float PickRadiusPx = 24f;

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

    public IReadOnlyCollection<int> Selected => _selected;
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
            case "superweapon":
                Root.World.Submit(new FireSuperweaponCommand(Player, p.BuildingId, target.Value));
                break;
        }
        Root.ShowMarker(MapView.ToWorld(target.Value), new Color(1f, 0.5f, 0.2f));
        Disarm();
    }

    /// <summary>Nearest entity to a screen point, filtered; null if none within the pick radius.</summary>
    private Entity? Pick(Vector2 screen, Func<Entity, bool> filter)
    {
        var cam = Root.Camera.Camera;
        Entity? best = null;
        var bestD = PickRadiusPx;
        foreach (var e in Root.World.Entities)
        {
            if (!e.Alive || e.IsInside || !filter(e)) continue;
            if (!Root.Views.TryGetValue(e.Id, out var view) || !view.Visible) continue;
            var world = MapView.ToWorld(e.Pos, 0.7f + (e.Unit?.FlightHeight ?? 0f));
            if (cam.IsPositionBehind(world)) continue;
            var d = (cam.UnprojectPosition(world) - screen).Length();
            var slack = e.IsBuilding ? e.Radius * 12f : 0f; // buildings are big targets
            if (d - slack < bestD) { bestD = d - slack; best = e; }
        }
        return best;
    }

    private void ClickSelect(Vector2 screen, bool additive)
    {
        var hit = Pick(screen, e => e.Owner == Player);
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
        }
        // Infantry right-clicking a garrisonable building/transport enter it; capture-capable infantry capture neutral/enemy tech buildings.
        var infantry = _selected.Where(id => Root.World.Get(id) is { Def.IsInfantry: true }).ToArray();
        if (infantry.Length > 0)
        {
            var container = Pick(screen, e => e.Def.GarrisonSlots > 0 && (e.Owner == Player || e.Owner < 0) && !e.Building!.IsHole);
            if (container is not null && container.Building is not { Capturable: true })
            {
                Root.World.Submit(new GarrisonCommand(Player, infantry, container.Id));
                Root.ShowMarker(MapView.ToWorld(container.Pos), new Color(0.4f, 0.8f, 1f));
                return;
            }
            var capturable = Pick(screen, e => e.Building is { Capturable: true } && e.Owner != Player);
            var capturers = infantry.Where(id => Root.World.Get(id)!.Unit!.CanCapture).ToArray();
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
            return;
        }
        var ground = Root.Camera.GroundPoint(screen);
        if (ground is not { } g) return;
        Root.World.Submit(new MoveCommand(Player, _selected.ToArray(), MapView.ToSim(g)));
        Root.ShowMarker(g, new Color(0.4f, 1f, 0.4f));
    }

    private void IssueAttackMove(Vector2 screen)
    {
        PruneSelection();
        if (_selected.Count == 0) return;
        var ground = Root.Camera.GroundPoint(screen);
        if (ground is not { } g) return;
        Root.World.Submit(new AttackMoveCommand(Player, _selected.ToArray(), MapView.ToSim(g)));
        Root.ShowMarker(g, new Color(1f, 0.6f, 0.2f));
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
