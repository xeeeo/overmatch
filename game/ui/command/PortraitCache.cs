using Godot;

namespace Overmatch.Game.UiReview;

/// <summary>
/// Renders each model once into an off-screen viewport and hands out the texture, so command cards can be rebuilt
/// freely without re-rendering 3D portraits. Same framing logic as <see cref="ModelPortrait"/>.
/// </summary>
public partial class PortraitCache : Node
{
    private readonly Dictionary<string, SubViewport> _viewports = new();
    private static readonly Vector2I RenderSize = new(288, 180);

    public Texture2D Get(string modelId, Color team)
    {
        var key = modelId + "|" + team.ToHtml(false);
        if (_viewports.TryGetValue(key, out var existing)) return existing.GetTexture();

        var viewport = new SubViewport
        {
            Size = RenderSize, TransparentBg = true, OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once, Msaa3D = Viewport.Msaa.Msaa2X,
        };
        AddChild(viewport);
        var model = EntityView.LoadModel(modelId, team);
        viewport.AddChild(model);
        viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-42, -28, 0), LightEnergy = 1.6f });
        viewport.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("bfccd1"), AmbientLightEnergy = .7f,
                BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = Colors.Transparent,
            },
        });

        var bounds = Bounds(model);
        var centre = bounds.Position + bounds.Size / 2;
        var reach = Math.Max(6f, bounds.Size.Length() * 1.5f);
        var camera = new Camera3D { Position = centre + new Vector3(reach * .8f, reach * .65f, reach), Projection = Camera3D.ProjectionType.Orthogonal, Current = true, Far = 200f };
        viewport.AddChild(camera);
        camera.LookAt(centre);
        float halfW = 0, halfH = 0;
        for (var i = 0; i < 8; i++)
        {
            var p = camera.Basis.Inverse() * (bounds.GetEndpoint(i) - centre);
            halfW = Math.Max(halfW, Math.Abs(p.X));
            halfH = Math.Max(halfH, Math.Abs(p.Y));
        }
        camera.Size = Math.Max(halfH * 2, halfW * 2 / ((float)RenderSize.X / RenderSize.Y)) * 1.12f;

        _viewports[key] = viewport;
        return viewport.GetTexture();
    }

    private static Aabb Bounds(Node3D root)
    {
        Aabb? combined = null;
        void Visit(Node node)
        {
            if (node is MeshInstance3D mesh)
            {
                var b = mesh.GlobalTransform * mesh.GetAabb();
                combined = combined is { } c ? c.Merge(b) : b;
            }
            foreach (var child in node.GetChildren()) Visit(child);
        }
        Visit(root);
        return combined ?? new Aabb(-Vector3.One, Vector3.One * 2);
    }
}
