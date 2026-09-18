using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Shows a footprint ghost under the cursor and places a building on click.</summary>
public partial class PlacementController : Node3D
{
    public GameRoot Root { get; set; } = null!;
    public bool Active => _def is not null;

    private BuildingDef? _def;
    private int _builderId;
    private MeshInstance3D _ghost = null!;
    private StandardMaterial3D _ghostMat = null!;
    private Node3D? _preview;
    private int _cellX, _cellY;
    private bool _valid;

    public override void _Ready()
    {
        _ghostMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.3f, 1f, 0.3f, 0.35f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        _ghost = new MeshInstance3D { Mesh = new BoxMesh(), MaterialOverride = _ghostMat, Visible = false };
        AddChild(_ghost);
    }

    public void Begin(BuildingDef def, int builderId)
    {
        _def = def;
        _builderId = builderId;
        _ghost.Mesh = new BoxMesh { Size = new Vector3(def.Width, 0.3f, def.Height) };
        _ghost.Visible = true;
        _preview?.QueueFree();
        _preview = EntityView.LoadModel(def.Model, Root.LocalColour);
        var tint = new StandardMaterial3D { AlbedoColor = new Color(1f, 1f, 1f, 0.45f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        Tint(_preview, tint);
        AddChild(_preview);
        UpdateGhost(GetViewport().GetMousePosition());
    }

    private static void Tint(Node n, Material m)
    {
        if (n is MeshInstance3D mi && mi.Mesh is { } mesh)
            for (var i = 0; i < mesh.GetSurfaceCount(); i++) mi.SetSurfaceOverrideMaterial(i, m);
        foreach (var c in n.GetChildren()) Tint(c, m);
    }

    public void Cancel()
    {
        _def = null;
        _ghost.Visible = false;
        _preview?.QueueFree();
        _preview = null;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_def is null) return;
        switch (@event)
        {
            case InputEventMouseMotion mm:
                UpdateGhost(mm.Position);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mb:
                UpdateGhost(mb.Position);
                if (_valid)
                {
                    Root.World.Submit(new BuildCommand(Root.LocalPlayer, _builderId, _def.Id, _cellX, _cellY));
                    if (!mb.ShiftPressed) Cancel();
                }
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                Cancel();
                GetViewport().SetInputAsHandled();
                break;
            default:
                if (@event.IsActionPressed("cancel")) { Cancel(); GetViewport().SetInputAsHandled(); }
                break;
        }
    }

    private void UpdateGhost(Vector2 screen)
    {
        if (_def is null) return;
        var ground = Root.Camera.GroundPoint(screen);
        if (ground is not { } g) return;
        var sim = MapView.ToSim(g);
        _cellX = Mathf.FloorToInt(sim.X - _def.Width * 0.5f + 0.5f);
        _cellY = Mathf.FloorToInt(sim.Y - _def.Height * 0.5f + 0.5f);
        var centre = new Vec2(_cellX + _def.Width * 0.5f, _cellY + _def.Height * 0.5f);
        _valid = Root.World.CanPlace(_def, _cellX, _cellY, out _) && Root.World.Player(Root.LocalPlayer).Cash >= _def.Cost;
        _ghostMat.AlbedoColor = _valid ? new Color(0.3f, 1f, 0.3f, 0.35f) : new Color(1f, 0.3f, 0.3f, 0.4f);
        _ghost.Position = MapView.ToWorld(centre, 0.15f);
        if (_preview is not null) _preview.Position = MapView.ToWorld(centre, 0.02f);
    }
}
