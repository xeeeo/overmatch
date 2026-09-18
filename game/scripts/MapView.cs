using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Renders the cell grid: ground texture, rock props for blocked cells, and the fog-of-war overlay.</summary>
public partial class MapView : Node3D
{
    private const int TexScale = 1;

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
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = BuildGroundTexture(world.Grid),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
                Roughness = 1f,
            },
        });

        BuildRocks(world.MapDef);
        BuildFog(w, h);
    }

    private static ImageTexture BuildGroundTexture(MapGrid grid)
    {
        var w = grid.Width * TexScale;
        var h = grid.Height * TexScale;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);
        var rng = new Random(1234);
        for (var py = 0; py < h; py++)
        {
            // Image row 0 is the far (north) edge, which is the highest sim y.
            var cy = grid.Height - 1 - py / TexScale;
            for (var px = 0; px < w; px++)
            {
                var cx = px / TexScale;
                // Gentle large-scale mottling plus a touch of per-cell grain.
                var n = 0.025f * Mathf.Sin(cx * 0.35f + cy * 0.21f) * Mathf.Cos(cy * 0.27f - cx * 0.13f) + (float)rng.NextDouble() * 0.02f - 0.01f;
                var c = grid.Get(cx, cy) switch
                {
                    CellType.Road => new Color(0.42f + n, 0.40f + n, 0.36f + n),
                    CellType.Rough => new Color(0.42f + n, 0.36f + n, 0.24f + n),
                    CellType.Water => new Color(0.18f + n * 0.5f, 0.36f + n * 0.5f, 0.52f),
                    CellType.Cliff or CellType.Structure => new Color(0.30f + n, 0.30f + n, 0.29f + n),
                    _ => new Color(0.36f + n, 0.42f + n, 0.24f + n),
                };
                img.SetPixel(px, py, c);
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    private void BuildRocks(MapDef map)
    {
        var rng = new Random(99);
        var rockMat = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.44f, 0.42f), Roughness = 0.95f };
        foreach (var r in map.Blocked)
        {
            // One slab per rect plus a few boulders on top so it reads as a rock formation, not a box.
            var height = 1.0f + (float)rng.NextDouble() * 0.6f;
            AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(r.W - 0.15f, height, r.H - 0.15f) },
                Position = new Vector3(r.X + r.W / 2f, height / 2f, -(r.Y + r.H / 2f)),
                MaterialOverride = rockMat,
            });
            var boulders = Math.Max(1, r.W * r.H / 6);
            for (var i = 0; i < boulders; i++)
            {
                var bx = r.X + 0.5f + (float)rng.NextDouble() * (r.W - 1f);
                var by = r.Y + 0.5f + (float)rng.NextDouble() * (r.H - 1f);
                var s = 0.6f + (float)rng.NextDouble() * 0.9f;
                AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(s, s * 0.7f, s * 0.8f) },
                    Position = new Vector3(bx, height + s * 0.25f, -by),
                    RotationDegrees = new Vector3((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 20f),
                    MaterialOverride = rockMat,
                });
            }
        }
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
