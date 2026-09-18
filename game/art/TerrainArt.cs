using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Visual terrain only. Reads map definitions; never modifies the collision grid.</summary>
public static class TerrainArt
{
    public readonly record struct Palette(string Meadow, string Light, string Earth, string Stone, string Water, float Aridity, float RockHeight);

    public static Palette Colours(string id) => id switch
    {
        "highlands" => new("505a43", "777953", "69634b", "777c77", "355b5e", .18f, 2.0f),
        "oil_rush" => new("817454", "a09068", "907651", "80765f", "455c50", .9f, 1.25f),
        "open_steppe" => new("74724b", "9a8d5c", "84704d", "807965", "3b645c", .65f, 1.15f),
        "twin_rivers" => new("4c633c", "73794e", "726245", "76776b", "275a60", .1f, 1.35f),
        "crossroads" => new("566447", "7c805b", "75644b", "777b72", "315b5d", .23f, 1.5f),
        _ => new("666b46", "86845a", "806c4b", "79796b", "315b5a", .38f, 1.35f),
    };

    public static ShaderMaterial Ground(MapDef map)
    {
        var grid = MapGrid.FromDef(map);
        var pixels = new byte[grid.Width * grid.Height * 4];
        for (var row = 0; row < grid.Height; row++)
            for (var x = 0; x < grid.Width; x++)
            {
                var channel = grid.Get(x, grid.Height - 1 - row) switch
                {
                    CellType.Road => 0, CellType.Rough => 1, CellType.Water => 2,
                    CellType.Cliff or CellType.Structure => 3, _ => -1,
                };
                if (channel >= 0) pixels[(row * grid.Width + x) * 4 + channel] = 255;
            }
        var image = Image.CreateFromData(grid.Width, grid.Height, false, Image.Format.Rgba8, pixels);
        var palette = Colours(map.Id);
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/terrain.gdshader") };
        material.SetShaderParameter("terrain_mask", ImageTexture.CreateFromImage(image));
        material.SetShaderParameter("map_size", new Vector2(map.Width, map.Height));
        material.SetShaderParameter("meadow", new Color(palette.Meadow));
        material.SetShaderParameter("meadow_light", new Color(palette.Light));
        material.SetShaderParameter("earth", new Color(palette.Earth));
        material.SetShaderParameter("stone", new Color(palette.Stone));
        material.SetShaderParameter("water_deep", new Color(palette.Water));
        material.SetShaderParameter("aridity", palette.Aridity);
        material.SetShaderParameter("terrain_seed", (float)(Seed(map.Id) % 1000));
        return material;
    }

    public static void AddDetails(Node3D parent, MapDef map)
    {
        var palette = Colours(map.Id);
        var random = new Random(Seed(map.Id));
        var stone = new Color(palette.Stone);
        using var rocks = new SurfaceTool();
        rocks.Begin(Mesh.PrimitiveType.Triangles);
        var rockVertices = 0;
        foreach (var rect in map.Blocked)
        {
            var nx = Math.Max(1, (int)Math.Ceiling(rect.W / 2.7f));
            var ny = Math.Max(1, (int)Math.Ceiling(rect.H / 2.7f));
            for (var iy = 0; iy < ny; iy++)
                for (var ix = 0; ix < nx; ix++)
                {
                    var width = rect.W / (float)nx;
                    var depth = rect.H / (float)ny;
                    var x = rect.X + width * (ix + .5f);
                    var z = -(rect.Y + depth * (iy + .5f));
                    var height = palette.RockHeight * (.60f + (float)random.NextDouble() * .65f);
                    rockVertices += Rock(rocks, new Vector3(x, 0, z), width - .045f, depth - .045f, height, stone, random);
                }
        }
        if (rockVertices > 0)
        {
            rocks.GenerateNormals();
            parent.AddChild(new MeshInstance3D { Name = "RockFormations", Mesh = rocks.Commit(), MaterialOverride = new StandardMaterial3D {
                VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness = .95f,
            }});
        }
        RoadMarkings(parent, map);
        GroundCover(parent, map, palette, random);
    }

    private static int Rock(SurfaceTool tool, Vector3 center, float w, float d, float h, Color stone, Random rng)
    {
        // Chamfered rings keep the whole formation inside the impassable rectangle.
        var outline = new[] { new Vector2(-.35f,-.5f), new Vector2(.35f,-.5f), new Vector2(.5f,-.34f), new Vector2(.5f,.34f),
            new Vector2(.35f,.5f), new Vector2(-.35f,.5f), new Vector2(-.5f,.34f), new Vector2(-.5f,-.34f) };
        var rings = new Vector3[4, 8];
        var scale = new[] { 1f, .96f, .76f, .60f };
        var height = new[] { -.035f, h*.31f, h*.83f, h };
        for (var k = 0; k < 8; k++)
        {
            var jitter = .90f + (float)rng.NextDouble() * .10f;
            for (var r = 0; r < 4; r++)
                rings[r, k] = center + new Vector3(outline[k].X*w*scale[r]*jitter, height[r] * (.94f + .06f*MathF.Sin(k*2.1f)), outline[k].Y*d*scale[r]*jitter);
        }
        for (var r = 0; r < 3; r++)
            for (var i = 0; i < 8; i++)
            {
                var j = (i+1)%8;
                var tint = r switch { 0 => .67f, 1 => .88f, _ => 1.04f };
                var colour = Shade(stone, tint + (float)rng.NextDouble()*.08f);
                Triangle(tool, rings[r,i], rings[r+1,i], rings[r+1,j], colour);
                Triangle(tool, rings[r,i], rings[r+1,j], rings[r,j], colour);
            }
        for (var i = 0; i < 8; i++) Triangle(tool, center + Vector3.Up*h*.985f, rings[3,(i+1)%8], rings[3,i], Shade(stone,1.10f));
        return 56*3;
    }

    private static void RoadMarkings(Node3D parent, MapDef map)
    {
        using var lines = new SurfaceTool();
        lines.Begin(Mesh.PrimitiveType.Triangles);
        var grid = MapGrid.FromDef(map);
        var count = 0;
        foreach (var road in map.Road)
        {
            var horizontal = road.W >= road.H;
            var length = horizontal ? road.W : road.H;
            if (length < 6) continue;
            for (float pos = 1; pos < length - .8f; pos += 2.5f)
            {
                var x = road.X + (horizontal ? pos : road.W*.5f);
                var y = road.Y + (horizontal ? road.H*.5f : pos);
                if (grid.Get((int)x,(int)y) != CellType.Road) continue;
                var dx = horizontal ? .60f : .045f;
                var dz = horizontal ? .045f : .60f;
                // A stripe never bridges water or an impassable cell at a crossing.
                if (grid.Get((int)(x+dx),(int)(y+dz)) != CellType.Road || grid.Get((int)(x-dx),(int)(y-dz)) != CellType.Road) continue;
                var a = new Vector3(x-dx,.012f,-y-dz); var b = new Vector3(x+dx,.012f,-y-dz);
                var c = new Vector3(x+dx,.012f,-y+dz); var d = new Vector3(x-dx,.012f,-y+dz);
                var colour = new Color("b0a67c");
                Triangle(lines,a,d,c,colour); Triangle(lines,a,c,b,colour); count++;
            }
        }
        if (count == 0) return;
        lines.GenerateNormals();
        parent.AddChild(new MeshInstance3D { Name = "WornRoadMarkings", Mesh = lines.Commit(), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness = 1f } });
    }

    private static void GroundCover(Node3D parent, MapDef map, Palette palette, Random rng)
    {
        using var blades = new SurfaceTool(); blades.Begin(Mesh.PrimitiveType.Triangles);
        for (var i = 0; i < 3; i++)
        {
            var angle = i*Mathf.Pi/3;
            var a = new Vector3(MathF.Cos(angle)*.12f,0,MathF.Sin(angle)*.12f);
            Triangle(blades,-a,a,new Vector3(.035f,.23f,0),Colors.White);
        }
        blades.GenerateNormals();
        var clump = blades.Commit();
        var grid = MapGrid.FromDef(map);
        var transforms = new List<Transform3D>();
        var colours = new List<Color>();
        for (var i = 0; i < map.Width*map.Height/7; i++)
        {
            var x = (float)rng.NextDouble()*map.Width; var y = (float)rng.NextDouble()*map.Height;
            if (grid.Get((int)x,(int)y) != CellType.Ground) continue;
            // Keep starting pads, supply deposits and the approaches to neutral buildings visually clear.
            if (map.Spawns.Any(s => MathF.Abs(s.X-x) < 8 && MathF.Abs(s.Y-y) < 8)
                || map.Supplies.Any(s => MathF.Abs(s.X-x) < 4 && MathF.Abs(s.Y-y) < 4)
                || map.Neutrals.Any(s => MathF.Abs(s.X-x) < 4 && MathF.Abs(s.Y-y) < 4)) continue;
            var size = .45f + (float)rng.NextDouble()*.65f;
            var basis = new Basis(Vector3.Up,(float)rng.NextDouble()*Mathf.Tau).Scaled(Vector3.One*size);
            transforms.Add(new Transform3D(basis,new Vector3(x,.006f,-y)));
            colours.Add(Shade(new Color(palette.Light),.84f + (float)rng.NextDouble()*.12f));
        }
        if (transforms.Count == 0) return;
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = clump, InstanceCount = transforms.Count };
        for (var i = 0; i < transforms.Count; i++) { mm.SetInstanceTransform(i,transforms[i]); mm.SetInstanceColor(i,colours[i]); }
        parent.AddChild(new MultiMeshInstance3D { Name = "LowGroundCover", Multimesh = mm, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness = 1, CullMode = BaseMaterial3D.CullModeEnum.Disabled } });
    }

    private static void Triangle(SurfaceTool mesh, Vector3 a, Vector3 b, Vector3 c, Color colour)
    {
        // Godot front faces use clockwise winding. Keep every triangle flat shaded.
        mesh.SetSmoothGroup(uint.MaxValue); mesh.SetColor(colour);
        mesh.AddVertex(a); mesh.AddVertex(c); mesh.AddVertex(b);
    }
    private static Color Shade(Color colour, float value) => new(colour.R*value,colour.G*value,colour.B*value,1);
    private static int Seed(string id) { uint hash = 2166136261; foreach (var c in id) hash = (hash^c)*16777619; return (int)(hash & 0x7fffffff); }
}
