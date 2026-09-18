using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Presentation of one sim entity. Interpolates between the last two sim ticks.</summary>
public partial class EntityView : Node3D
{
    public Entity Entity { get; private set; } = null!;

    private MeshInstance3D _ring = null!;
    private bool _selected;

    public static EntityView Create(Entity entity, Color team)
    {
        var view = new EntityView { Entity = entity, Name = $"E{entity.Id}_{entity.Def.Id}" };
        view.AddChild(LoadModel(entity.Def.Model, team));

        view._ring = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = entity.Radius * 1.5f, OuterRadius = entity.Radius * 1.5f + 0.08f, Rings = 24, RingSegments = 6 },
            Position = new Vector3(0, 0.05f, 0),
            Visible = false,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.4f, 1f, 0.4f),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
        view.AddChild(view._ring);
        view.Sync(1f);
        return view;
    }

    /// <summary>Loads res://assets/models/{id}.glb if it exists, recolouring any material named "TeamColour"; otherwise a primitive placeholder.</summary>
    private static Node3D LoadModel(string modelId, Color team)
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

    public bool Selected
    {
        get => _selected;
        set { _selected = value; _ring.Visible = value; }
    }

    public void Sync(float alpha)
    {
        var p = Vec2.Lerp(Entity.PrevPos, Entity.Pos, alpha);
        var f = Angles.LerpAngle(Entity.PrevFacing, Entity.Facing, alpha);
        // Sim: X east, Y north. Godot: X east, -Z north. Facing 0 = +X; rotate about Y so that +X model forward points along the heading.
        Position = new Vector3(p.X, 0f, -p.Y);
        Rotation = new Vector3(0f, f, 0f);
    }
}
