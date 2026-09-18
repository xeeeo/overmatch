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

    public IReadOnlyCollection<int> Selected => _selected;
    public bool AttackMoveArmed => _attackMoveArmed;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
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
                _attackMoveArmed = false;
                IssueContextOrder(mb.Position);
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
                if (e.Alive && e.Owner == Player) _selected.Add(e.Id);
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
            _attackMoveArmed = false;
        }
    }

    /// <summary>Nearest entity to a screen point, filtered; null if none within the pick radius.</summary>
    private Entity? Pick(Vector2 screen, Func<Entity, bool> filter)
    {
        var cam = Root.Camera.Camera;
        Entity? best = null;
        var bestD = PickRadiusPx;
        foreach (var e in Root.World.Entities)
        {
            if (!e.Alive || !filter(e)) continue;
            if (!Root.Views.TryGetValue(e.Id, out var view) || !view.Visible) continue;
            var world = MapView.ToWorld(e.Pos, 0.7f);
            if (cam.IsPositionBehind(world)) continue;
            var d = (cam.UnprojectPosition(world) - screen).Length();
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    private void ClickSelect(Vector2 screen, bool additive)
    {
        var hit = Pick(screen, e => e.Owner == Player);
        if (!additive) _selected.Clear();
        if (hit is not null)
        {
            if (additive && !_selected.Add(hit.Id)) _selected.Remove(hit.Id);
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
            if (!e.Alive || e.Owner != Player) continue;
            var world = MapView.ToWorld(e.Pos, 0.5f);
            if (cam.IsPositionBehind(world)) continue;
            if (rect.HasPoint(cam.UnprojectPosition(world))) _selected.Add(e.Id);
        }
        ApplySelectionVisuals();
    }

    /// <summary>Right-click: attack an enemy under the cursor, otherwise move.</summary>
    private void IssueContextOrder(Vector2 screen)
    {
        PruneSelection();
        if (_selected.Count == 0) return;
        var enemy = Pick(screen, e => e.Owner != Player);
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
