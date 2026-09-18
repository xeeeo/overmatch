using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Renders the cell grid: ground texture, rock props for blocked cells, and the fog-of-war overlay.</summary>
public partial class MapView : Node3D
{
    private World _world = null!;
    private int _localPlayer;
    private ImageTexture _fogTex = null!;
    private Image _fogImg = null!;
    private byte[] _fogBytes = Array.Empty<byte>();
    private int _fogVersion = -1;

    public static Vector3 ToWorld(Vec2 p, float y = 0f) => new(p.X, y, -p.Y);
    public static Vec2 ToSim(Vector3 v) => new(v.X, -v.Z);

    public void Build(World world, int localPlayer)
    {
        _world = world;
        _localPlayer = localPlayer;
        var w = world.Grid.Width;
        var h = world.Grid.Height;

        AddChild(new MeshInstance3D
        {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = new Vector2(w, h) },
            Position = new Vector3(w / 2f, 0f, -h / 2f),
            MaterialOverride = TerrainArt.Ground(world.MapDef),
        });

        TerrainArt.AddDetails(this, world.MapDef);
        BuildFog(w, h);
    }

    private void BuildFog(int w, int h)
    {
        _fogBytes = new byte[w * h];
        Array.Fill(_fogBytes, (byte)255);
        _fogImg = Image.CreateFromData(w, h, false, Image.Format.R8, _fogBytes);
        _fogTex = ImageTexture.CreateFromImage(_fogImg);

        var mat = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/fog.gdshader"), RenderPriority = 100 };
        mat.SetShaderParameter("fog_tex", _fogTex);
        AddChild(new MeshInstance3D
        {
            Name = "Fog",
            Mesh = new PlaneMesh { Size = new Vector2(w, h) },
            Position = new Vector3(w / 2f, 0.4f, -h / 2f),
            MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    public override void _Process(double delta)
    {
        if (_world is null || _world.Vision.Version == _fogVersion) return;
        _fogVersion = _world.Vision.Version;
        var w = _world.Grid.Width;
        var h = _world.Grid.Height;
        var raw = _world.Vision.Raw(_localPlayer);
        for (var y = 0; y < h; y++)
        {
            var row = (h - 1 - y) * w;
            var src = y * w;
            for (var x = 0; x < w; x++)
            {
                _fogBytes[row + x] = (Visibility)raw[src + x] switch
                {
                    Visibility.Visible => 0,
                    Visibility.Explored => 150,
                    _ => 255,
                };
            }
        }
        _fogImg.SetData(w, h, false, Image.Format.R8, _fogBytes);
        _fogTex.Update(_fogImg);
    }
}
