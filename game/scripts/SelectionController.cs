using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Click, shift-click and drag-box selection; right-click move orders. Draws the selection box as a 2D overlay.</summary>
public partial class SelectionController : Control
{
    private const float ClickTolerance = 6f;
    private const float PickRadiusPx = 22f;

    public int Player { get; set; }
    public GameRoot Root { get; set; } = null!;

    private readonly HashSet<int> _selected = new();
    private Vector2 _dragStart;
    private bool _dragging;
    private bool _boxActive;

    public IReadOnlyCollection<int> Selected => _selected;

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
                IssueMove(mb.Position);
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
            if (_selected.Count > 0) Root.World.Submit(new StopCommand(Player, _selected.ToArray()));
        }
    }

    private void ClickSelect(Vector2 screen, bool additive)
    {
        var cam = Root.Camera.Camera;
        int best = -1;
        var bestD = PickRadiusPx;
        foreach (var e in Root.World.Entities)
        {
            if (!e.Alive || e.Owner != Player) continue;
            var world = new Vector3(e.Pos.X, 0.5f, -e.Pos.Y);
            if (cam.IsPositionBehind(world)) continue;
            var d = (cam.UnprojectPosition(world) - screen).Length();
            if (d < bestD) { bestD = d; best = e.Id; }
        }
        if (!additive) _selected.Clear();
        if (best >= 0)
        {
            if (additive && !_selected.Add(best)) _selected.Remove(best);
            else _selected.Add(best);
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
            var world = new Vector3(e.Pos.X, 0.5f, -e.Pos.Y);
            if (cam.IsPositionBehind(world)) continue;
            if (rect.HasPoint(cam.UnprojectPosition(world))) _selected.Add(e.Id);
        }
        ApplySelectionVisuals();
    }

    private void IssueMove(Vector2 screen)
    {
        if (_selected.Count == 0) return;
        var ground = Root.Camera.GroundPoint(screen);
        if (ground is not { } g) return;
        Root.World.Submit(new MoveCommand(Player, _selected.ToArray(), new Vec2(g.X, -g.Z)));
        Root.ShowMoveMarker(g);
    }

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
