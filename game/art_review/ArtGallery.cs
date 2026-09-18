using Godot;
using Overmatch.Game.UiReview;
using Overmatch.Sim.Data;

namespace Overmatch.Game.ArtReview;

/// <summary>Isolated roster inspection using production GLBs and player materials.</summary>
public partial class ArtGallery : Node
{
    private GameRules _rules = null!;
    private Control _canvas = null!;
    private string _faction = "coalition";
    private string _kind = "units";
    private string? _capture;
    private string? _baseline;
    private int _frames;
    private int _team = -1;
    private bool _turned;
    private bool _rts;
    private readonly List<Node3D> _models = new();
    private const float Width = 1920, Height = 1200;

    public override void _Ready()
    {
        GetWindow().Title = "Overmatch — faction art gallery";
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--gallery-faction=")) _faction = arg[18..];
            if (arg.StartsWith("--gallery-kind=")) _kind = arg[15..];
            if (arg.StartsWith("--gallery-capture=")) _capture = arg[18..];
            if (arg.StartsWith("--gallery-baseline=")) _baseline = arg[19..];
            if (arg.StartsWith("--gallery-team=")) _team = int.Parse(arg[15..]);
            if (arg == "--gallery-turned") _turned = true;
            if (arg == "--gallery-rts") _rts = true;
        }
        _rules = DataLoader.LoadRules();
        var layer = new CanvasLayer();
        AddChild(layer);
        _canvas = new Control();
        layer.AddChild(_canvas);
        GetViewport().SizeChanged += Resize;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (var child in _canvas.GetChildren()) { _canvas.RemoveChild(child); child.QueueFree(); }
        _models.Clear();
        Solid(new Rect2(0, 0, Width, Height), CommandTheme.Ink);
        Text("OVERMATCH  /  ART DIRECTION", 38, 20, 1000, 26, 18, CommandTheme.Gold, true);
        Text($"{_faction.ToUpperInvariant()}  /  {_kind.ToUpperInvariant()}", 36, 48, 1340, 63, 49, CommandTheme.Text, true);
        Text(_baseline is null ? "PRODUCTION GAME ASSETS" : "BASELINE GAME ASSETS", 1410, 60, 470, 30, 19, CommandTheme.Gold, true);
        var factionX = 38f;
        foreach (var faction in new[] { "coalition", "directorate", "network", "neutral" })
        {
            var choice = faction;
            Button(faction.ToUpperInvariant(), factionX, 120, 175, faction == _faction, () => { _faction = choice; Rebuild(); });
            factionX += 185;
        }
        Button("UNITS", 880, 120, 140, _kind == "units", () => { _kind = "units"; Rebuild(); });
        Button("BUILDINGS", 1030, 120, 155, _kind == "buildings", () => { _kind = "buildings"; Rebuild(); });
        Button(_rts ? "RTS SCALE" : "DETAIL VIEW", 1545, 120, 180, _rts, () => { _rts = !_rts; Rebuild(); });
        Button("TURRET 90°", 1735, 120, 147, _turned, () => { _turned = !_turned; Rebuild(); });

        IEnumerable<ObjectDef> definitions = _kind == "buildings" ? _rules.Buildings.Values : _rules.Units.Values;
        var entries = definitions.Where(d => d.Model.StartsWith(_faction + "/"))
            .OrderBy(d => d.IsAir ? 2 : d.IsInfantry ? 0 : 1).ThenBy(d => d.Name)
            .Select(d => (d.Model, d.Name, Role(d))).ToList();
        if (_faction == "neutral")
            entries = new[] { "civilian_house", "civilian_block", "oil_derrick", "hole" }
                .Select(n => ("neutral/" + n, n.Replace('_', ' ').ToUpperInvariant(), "BATTLEFIELD PROP")).ToList();
        if (entries.Count == 0) throw new InvalidOperationException($"No art entries for {_faction}/{_kind}");
        var columns = entries.Count <= 4 ? 4 : 5;
        var rows = (entries.Count + columns - 1) / columns;
        var cellW = (Width - 76 - (columns - 1) * 14) / columns;
        var cellH = Math.Min(420, (924f - (rows - 1) * 14) / rows);
        var colour = MatchSettings.Palette[_team >= 0 ? _team % MatchSettings.Palette.Length : _faction switch { "directorate" => 1, "network" => 3, _ => 0 }];
        for (var i = 0; i < entries.Count; i++)
        {
            var (model, name, role) = entries[i];
            AddCard(model, name, role, new Rect2(38 + i % columns * (cellW + 14), 188 + i / columns * (cellH + 14), cellW, cellH), colour);
        }
        Solid(new Rect2(38, 1140, 1844, 1), CommandTheme.Line);
        Text("1 / 2 / 3  TEAM COLOUR     T  TURRET     Z  DETAIL / RTS SCALE", 38, 1152, 1100, 29, 17, CommandTheme.Muted, true);
        Text(_rts ? "RTS CAMERA SCALE · 1200 PX REFERENCE" : "CAMERAS FIT EACH MODEL · SHARED LIGHTING", 1190, 1152, 700, 29, 17, CommandTheme.Muted, true);
        Resize();
        _frames = 0;
        GD.Print($"[Art gallery] {_faction}/{_kind}: {entries.Count} production models; baseline={_baseline ?? "none"}");
    }

    private static string Role(ObjectDef d) => d.IsBuilding ? "STRUCTURE" : d.IsAir ? "AIRCRAFT" : d.IsInfantry ? "INFANTRY" : d.Tags.Contains("builder") ? "CONSTRUCTION" : "VEHICLE / EQUIPMENT";

    private void AddCard(string id, string title, string role, Rect2 rect, Color colour)
    {
        Solid(rect, CommandTheme.Panel);
        var viewportHeight = rect.Size.Y - 57;
        var container = new SubViewportContainer { Position = rect.Position, Size = new Vector2(rect.Size.X, viewportHeight), Stretch = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.AddChild(container);
        var viewport = new SubViewport { Size = new Vector2I((int)rect.Size.X, (int)viewportHeight), OwnWorld3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, Msaa3D = Viewport.Msaa.Msaa4X };
        container.AddChild(viewport);
        var stage = new Node3D();
        viewport.AddChild(stage);
        ArtStage.Light(stage);
        var current = EntityView.LoadModel(id, colour);
        stage.AddChild(current);
        if (_turned && EntityView.FindNamed(current, "Turret") is { } currentTurret) currentTurret.Rotation = new Vector3(0, Mathf.Pi / 2, 0);
        var bounds = ArtStage.Bounds(current);
        Node3D model = current;
        if (_baseline is not null)
        {
            var doc = new GltfDocument();
            var state = new GltfState();
            var err = doc.AppendFromFile(_baseline.PathJoin(id + ".glb"), state);
            if (err != Error.Ok) throw new InvalidOperationException($"Baseline {id}: {err}");
            model = (Node3D)doc.GenerateScene(state);
            stage.AddChild(model);
            ArtStage.Recolour(model, colour);
            if (_turned && EntityView.FindNamed(model, "Turret") is { } baselineTurret) baselineTurret.Rotation = new Vector3(0, Mathf.Pi / 2, 0);
            bounds = bounds.Merge(ArtStage.Bounds(model));
            current.Visible = false;
        }
        _models.Add(model);
        var target = bounds.GetCenter();
        var camera = new Camera3D { Current = true, Near = .01f, Far = 200, Projection = Camera3D.ProjectionType.Orthogonal, KeepAspect = Camera3D.KeepAspectEnum.Height };
        stage.AddChild(camera);
        camera.Position = target + new Vector3(6, 6.7f, 8);
        camera.LookAt(target);
        var inverse = camera.GlobalTransform.AffineInverse();
        var projected = Enumerable.Range(0, 8).Select(i => inverse * bounds.GetEndpoint(i)).ToArray();
        var height = projected.Max(v => v.Y) - projected.Min(v => v.Y);
        var width = projected.Max(v => v.X) - projected.Min(v => v.X);
        camera.Size = Math.Max(height, width * viewportHeight / rect.Size.X) * 1.25f;
        if (_rts)
        {
            camera.Projection = Camera3D.ProjectionType.Perspective;
            camera.Fov = Mathf.RadToDeg(2 * Mathf.Atan(Mathf.Tan(Mathf.DegToRad(45) / 2) * viewportHeight / Height));
            camera.Position = target + new Vector3(.35f, .788f, .506f).Normalized() * 32;
            camera.LookAt(target);
        }
        Text(title.ToUpperInvariant(), rect.Position.X + 13, rect.End.Y - 55, rect.Size.X - 23, 28, 23, CommandTheme.Text, true);
        Text(role, rect.Position.X + 14, rect.End.Y - 27, rect.Size.X - 24, 19, 12, CommandTheme.Muted, true);
    }

    private void Resize()
    {
        var size = GetViewport().GetVisibleRect().Size;
        _canvas.Scale = Vector2.One * Math.Min(size.X / Width, size.Y / Height);
        _canvas.Position = (size - new Vector2(Width, Height) * _canvas.Scale) / 2;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Key1: _team = 0; break;
            case Key.Key2: _team = 1; break;
            case Key.Key3: _team = 3; break;
            case Key.T: _turned = !_turned; break;
            case Key.Z: _rts = !_rts; break;
            default: return;
        }
        Rebuild();
    }

    public override void _Process(double delta)
    {
        if (_capture is null || ++_frames != 30) return;
        var error = GetViewport().GetTexture().GetImage().SavePng(_capture);
        GD.Print($"[Art gallery] capture {_capture}: {error}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    private void Button(string caption, float x, float y, float w, bool active, Action action)
    {
        var button = new Button { Text = caption, Position = new Vector2(x, y), Size = new Vector2(w, 42) };
        CommandTheme.Style(button, active); button.Pressed += action; _canvas.AddChild(button);
    }
    private void Text(string value, float x, float y, float w, float h, int size, Color colour, bool display = false)
    {
        var label = new Label { Text = value, Position = new Vector2(x, y), Size = new Vector2(w, h), VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", display ? CommandTheme.Display : CommandTheme.Body);
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", colour); _canvas.AddChild(label);
    }
    private void Solid(Rect2 rect, Color colour) => _canvas.AddChild(new ColorRect { Position = rect.Position, Size = rect.Size, Color = colour, MouseFilter = Control.MouseFilterEnum.Ignore });
}

internal static class ArtStage
{
    public static void Light(Node3D stage)
    {
        stage.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("202b2d"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("d7e3e8"), AmbientLightEnergy = .32f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        }});
        stage.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-48, -28, -8), LightColor = new Color("fff0d8"), LightEnergy = 1.1f, ShadowEnabled = true });
        stage.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-25, 140, 0), LightColor = new Color("a8c9df"), LightEnergy = .28f });
        stage.AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = Vector2.One * 200 }, Position = new Vector3(0, -.02f, 0), MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("172224"), Roughness = 1 } });
    }
    public static Aabb Bounds(Node3D root)
    {
        Aabb? result = null;
        void Walk(Node node)
        {
            if (node is MeshInstance3D mesh)
            {
                var aabb = mesh.GlobalTransform * mesh.GetAabb();
                result = result is { } previous ? previous.Merge(aabb) : aabb;
            }
            foreach (var child in node.GetChildren()) Walk(child);
        }
        Walk(root);
        return result ?? new Aabb(Vector3.Zero, Vector3.One);
    }
    public static void Recolour(Node node, Color colour)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is { } geometry)
            for (var i = 0; i < geometry.GetSurfaceCount(); i++)
                if (mesh.GetActiveMaterial(i) is BaseMaterial3D { ResourceName: "TeamColour" } mat)
                {
                    var copy = (BaseMaterial3D)mat.Duplicate(); copy.AlbedoColor = colour; mesh.SetSurfaceOverrideMaterial(i, copy);
                }
        foreach (var child in node.GetChildren()) Recolour(child, colour);
    }
}
