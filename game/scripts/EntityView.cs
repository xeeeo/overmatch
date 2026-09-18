using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Presentation of one sim entity. Interpolates between the last two sim ticks.</summary>
public partial class EntityView : Node3D
{
    public Entity Entity { get; private set; } = null!;

    private Node3D _model = null!;
    private MeshInstance3D _ring = null!;
    private Node3D? _turret;
    private readonly List<Node3D> _rotors = new();
    private bool _selected;
    private float _rotorAngle;
    private bool _wasUnderConstruction;
    private StandardMaterial3D? _buildTint;

    public static EntityView Create(Entity entity, Color team)
    {
        var view = new EntityView { Entity = entity, Name = $"E{entity.Id}_{entity.Def.Id}" };
        var model = LoadModel(entity.Def.Model, team);
        view._model = model;
        view.AddChild(model);
        view._turret = FindNamed(model, "Turret");
        CollectRotors(model, view._rotors);

        var ringR = entity.Radius * (entity.IsBuilding ? 1.1f : 1.5f);
        view._ring = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = ringR, OuterRadius = ringR + 0.08f, Rings = 32, RingSegments = 6 },
            Position = new Vector3(0, 0.05f, 0),
            Visible = false,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.4f, 1f, 0.4f),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
        view.AddChild(view._ring);
        if (entity.UnderConstruction) view.BeginConstructionLook();
        view.Sync(1f);
        return view;
    }

    /// <summary>Loads res://assets/models/{id}.glb if it exists, recolouring any material named "TeamColour"; otherwise a primitive placeholder.</summary>
    public static Node3D LoadModel(string modelId, Color team)
    {
        var path = $"res://assets/models/{modelId}.glb";
        if (!ResourceLoader.Exists(path)) return PlaceholderModels.Build(modelId, team);
        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene is null) return PlaceholderModels.Build(modelId, team);
        var node = scene.Instantiate<Node3D>();
        ApplyTeamColour(node, team);
        return node;
    }

    private static void ApplyTeamColour(Node node, Color team)
    {
        if (node is MeshInstance3D mi && mi.Mesh is { } mesh)
        {
            for (var i = 0; i < mesh.GetSurfaceCount(); i++)
            {
                var mat = mi.GetActiveMaterial(i);
                if (mat is BaseMaterial3D bm && bm.ResourceName == "TeamColour")
                {
                    var copy = (BaseMaterial3D)bm.Duplicate();
                    copy.AlbedoColor = team;
                    mi.SetSurfaceOverrideMaterial(i, copy);
                }
            }
        }
        foreach (var child in node.GetChildren()) ApplyTeamColour(child, team);
    }

    /// <summary>Replaces every material with a dark, burnt one. Used for wrecks.</summary>
    public static void ApplyWreckLook(Node node)
    {
        var burnt = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.11f, 0.10f), Roughness = 1f };
        ApplyOverride(node, burnt);
    }

    private static void ApplyOverride(Node node, Material? mat)
    {
        if (node is MeshInstance3D mi && mi.Mesh is { } mesh)
            for (var i = 0; i < mesh.GetSurfaceCount(); i++) mi.SetSurfaceOverrideMaterial(i, mat);
        foreach (var child in node.GetChildren()) ApplyOverride(child, mat);
    }

    public static Node3D? FindNamed(Node node, string name)
    {
        if (node.Name == name && node is Node3D n3) return n3;
        foreach (var child in node.GetChildren())
        {
            var found = FindNamed(child, name);
            if (found is not null) return found;
        }
        return null;
    }

    private static void CollectRotors(Node node, List<Node3D> into)
    {
        if (node is Node3D n3 && node.Name.ToString().StartsWith("Rotor")) into.Add(n3);
        foreach (var child in node.GetChildren()) CollectRotors(child, into);
    }

    public bool Selected
    {
        get => _selected;
        set { _selected = value; _ring.Visible = value; }
    }

    private void BeginConstructionLook()
    {
        _wasUnderConstruction = true;
        _buildTint = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.7f, 0.85f, 1f, 0.55f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        ApplyOverride(_model, _buildTint);
    }

    private void EndConstructionLook()
    {
        _wasUnderConstruction = false;
        // Drop the tint but keep team-colour overrides: reload is simplest.
        var team = ((StandardMaterial3D?)null)?.AlbedoColor ?? Colors.White;
        ApplyOverride(_model, null);
        _model.Scale = Vector3.One;
        // Re-apply team colour lost by the override reset.
        if (_teamColour is { } tc) ApplyTeamColour(_model, tc);
    }

    private Color? _teamColour;
    public void SetTeamColour(Color c) => _teamColour = c;

    public void Sync(float alpha)
    {
        var e = Entity;
        if (e.IsBuilding)
        {
            Position = MapView.ToWorld(e.Pos);
            if (e.UnderConstruction)
            {
                if (!_wasUnderConstruction) BeginConstructionLook();
                _model.Scale = new Vector3(1f, 0.08f + 0.92f * e.BuildProgress, 1f);
            }
            else if (_wasUnderConstruction) EndConstructionLook();
        }
        else
        {
            var p = Vec2.Lerp(e.PrevPos, e.Pos, alpha);
            var f = Angles.LerpAngle(e.PrevFacing, e.Facing, alpha);
            var h = e.Unit?.FlightHeight ?? 0f;
            // Sim: X east, Y north. Godot: X east, -Z north. Facing 0 = +X; rotate about Y so that +X model forward points along the heading.
            Position = new Vector3(p.X, h, -p.Y);
            Rotation = new Vector3(0f, f, 0f);
            if (h > 0f) _ring.Position = new Vector3(0, -h + 0.05f, 0);
        }

        if (_turret is not null)
        {
            var t = Angles.LerpAngle(e.PrevTurretFacing, e.TurretFacing, alpha);
            _turret.Rotation = new Vector3(0f, Angles.Wrap(t - Rotation.Y), 0f);
        }
        if (_rotors.Count > 0)
        {
            _rotorAngle += 0.6f;
            foreach (var r in _rotors) r.Rotation = new Vector3(0f, _rotorAngle, 0f);
        }
    }
}
