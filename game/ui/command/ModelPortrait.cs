using Godot;

namespace Overmatch.Game.UiReview;

/// <summary>UI portrait of an unmodified game model, rendered once and reused on the card.</summary>
public partial class ModelPortrait : SubViewportContainer
{
    public string ModelId { get; set; } = "coalition/dozer";
    public float Distance { get; set; } = 8;
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Stretch = true;
        var viewport = new SubViewport
        {
            Size = new Vector2I(Math.Max(1, (int)Size.X * 2), Math.Max(1, (int)Size.Y * 2)),
            TransparentBg = true, OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
            Msaa3D = Viewport.Msaa.Msaa2X,
        };
        AddChild(viewport);
        var model = EntityView.LoadModel(ModelId, new Color("648a9d"));
        viewport.AddChild(model);
        viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-42, -28, 0), LightEnergy = 1.6f });
        viewport.AddChild(new WorldEnvironment { Environment = new Godot.Environment
        {
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("bfccd1"), AmbientLightEnergy = .7f,
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = Colors.Transparent,
        }});
        var bounds = Bounds(model);
        var center = bounds.Position + bounds.Size / 2;
        var camera = new Camera3D { Position = center + new Vector3(Distance * .8f, Distance * .65f, Distance), Projection = Camera3D.ProjectionType.Orthogonal, Current = true };
        viewport.AddChild(camera);
        camera.LookAt(center);
        // Fit every existing asset to its card instead of hand-tuning model scale.
        float halfWidth = 0, halfHeight = 0;
        for (int i = 0; i < 8; i++)
        {
            var point = camera.Basis.Inverse() * (bounds.GetEndpoint(i) - center);
            halfWidth = Math.Max(halfWidth, Math.Abs(point.X));
            halfHeight = Math.Max(halfHeight, Math.Abs(point.Y));
        }
        camera.Size = Math.Max(halfHeight * 2, halfWidth * 2 / (Size.X / Size.Y)) * 1.12f;
    }

    private static Aabb Bounds(Node3D root)
    {
        Aabb? combined = null;
        void Visit(Node node)
        {
            if (node is MeshInstance3D mesh)
            {
                var bounds = mesh.GlobalTransform * mesh.GetAabb();
                combined = combined is { } current ? current.Merge(bounds) : bounds;
            }
            foreach (var child in node.GetChildren()) Visit(child);
        }
        Visit(root);
        return combined ?? new Aabb(-Vector3.One, Vector3.One * 2);
    }
}
