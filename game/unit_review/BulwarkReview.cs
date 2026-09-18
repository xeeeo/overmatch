using Godot;
using Overmatch.Game.UiReview;

namespace Overmatch.Game.UnitReview;

/// <summary>Standalone visual comparison. This scene never creates or ticks a game world.</summary>
public partial class BulwarkReview : Node
{
    private readonly List<Node3D> _models = new();
    private readonly List<Camera3D> _cameras = new();
    private readonly Dictionary<string, Button> _teamButtons = new();
    private Control _canvas = null!;
    private Button _turretButton = null!;
    private Button _scaleButton = null!;
    private Label _viewLabel = null!;
    private string _team = "blue";
    private bool _turned;
    private bool _rts;
    private string? _capture;
    private int _frames;
    private const float ReferenceWidth = 1600;
    private const float ReferenceHeight = 900;
    private const float StageHeight = 442;

    public override void _Ready()
    {
        GetWindow().Title = "Overmatch — Bulwark design review";
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--unit-capture=")) _capture = arg[15..];
            if (arg.StartsWith("--unit-team=")) _team = arg[12..];
            if (arg == "--unit-turret=90") _turned = true;
            if (arg == "--unit-scale=rts") _rts = true;
        }
        if (_team is not ("red" or "gold")) _team = "blue";

        var layer = new CanvasLayer();
        AddChild(layer);
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_canvas);
        Solid(_canvas, new Rect2(0, 0, 1600, 900), CommandTheme.Ink);
        Text("OVERMATCH  /  COALITION", 48, 29, 650, 24, 18, CommandTheme.Gold, true);
        Text("BULWARK", 46, 63, 920, 68, 60, CommandTheme.Text, true);
        Text("MAIN BATTLE TANK", 50, 138, 840, 26, 20, CommandTheme.Muted, true);
        Text("UNIT DESIGN  /  01", 1200, 47, 350, 28, 22, CommandTheme.Gold, true, HorizontalAlignment.Right);
        Text("Silhouette. Material. Identity.", 1150, 83, 400, 25, 18, CommandTheme.Muted, false, HorizontalAlignment.Right);
        Solid(_canvas, new Rect2(48, 185, 1504, 1), CommandTheme.Line);

        BuildPanel(48, "EXISTING", "CURRENT PRODUCTION ASSET", "res://unit_review/assets/bulwark_before.glb", false);
        BuildPanel(816, "BULWARK · DESIGN 01", "PROPOSED UNIT ART", "res://assets/models/coalition/bulwark.glb", true);
        Text("BASELINE", 49, 729, 150, 24, 16, CommandTheme.Muted, true);
        Text("Original geometry and materials for direct comparison.", 49, 757, 715, 27, 18, CommandTheme.Text);
        Text("FIRST UNIT PASS", 817, 729, 220, 24, 16, CommandTheme.Gold, true);
        Text("Same world scale. Shared team colour. Independent turret.", 817, 757, 715, 27, 18, CommandTheme.Text);

        Solid(_canvas, new Rect2(48, 809, 1504, 1), CommandTheme.Line);
        Text("TEAM COLOUR", 49, 831, 141, 24, 15, CommandTheme.Muted, true);
        var x = 193f;
        foreach (var choice in new[] { "blue", "red", "gold" })
        {
            var selected = choice;
            var button = MakeButton(choice.ToUpperInvariant(), x, 824, 96, () => { _team = selected; UpdateState(); });
            _teamButtons[choice] = button;
            x += 106;
        }
        _turretButton = MakeButton("TURRET  0°", 553, 824, 180, () => { _turned = !_turned; UpdateState(); });
        _scaleButton = MakeButton("VIEW  CLOSE-UP", 747, 824, 203, () => { _rts = !_rts; UpdateState(); });
        _viewLabel = Text("", 988, 824, 563, 40, 17, CommandTheme.Muted, true, HorizontalAlignment.Right);
        Text("1 / 2 / 3  TEAM     T  TURRET     Z  ZOOM", 49, 875, 940, 18, 12, CommandTheme.Muted, true);
        Text("VISUAL REVIEW  ·  BULWARK ONLY", 1100, 875, 451, 18, 12, CommandTheme.Muted, true, HorizontalAlignment.Right);
        GetViewport().SizeChanged += Resize;
        Resize();
        UpdateState();
        GD.Print("[Unit review] Bulwark comparison ready. 1/2/3 team colour, T turret, Z camera scale.");
    }

    private void BuildPanel(float x, string title, string subtitle, string path, bool proposed)
    {
        Solid(_canvas, new Rect2(x, 206, 736, 500), CommandTheme.Panel);
        Solid(_canvas, new Rect2(x, 206, 3, 54), proposed ? CommandTheme.Gold : CommandTheme.Line);
        Text(title, x + 19, 213, 690, 30, 25, proposed ? CommandTheme.Text : CommandTheme.Muted, true);
        Text(subtitle, x + 20, 247, 690, 20, 12, CommandTheme.Muted, true);
        var container = new SubViewportContainer
        {
            Position = new Vector2(x + 1, 270), Size = new Vector2(734, StageHeight),
            Stretch = true, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _canvas.AddChild(container);
        var viewport = new SubViewport
        {
            Size = new Vector2I(734, (int)StageHeight), OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always, Msaa3D = Viewport.Msaa.Msaa4X,
        };
        container.AddChild(viewport);
        var stage = new Node3D();
        viewport.AddChild(stage);
        stage.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color("202b2d"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color("d7e3e8"), AmbientLightEnergy = .32f,
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
            },
        });
        stage.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-48, -28, -8), LightColor = new Color("fff0d8"),
            LightEnergy = 1.1f, ShadowEnabled = true,
        });
        stage.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-25, 140, 0), LightColor = new Color("a8c9df"), LightEnergy = .28f,
        });
        stage.AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(200, 200) }, Position = new Vector3(0, -.015f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("172224"), Roughness = 1 },
        });
        stage.AddChild(new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 1.9f, OuterRadius = 1.907f, Rings = 96, RingSegments = 4 },
            Position = new Vector3(0, -.007f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("667273"), Roughness = 1 },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        var model = GD.Load<PackedScene>(path)?.Instantiate<Node3D>();
        if (model is null) throw new InvalidOperationException($"Unit review model is unavailable: {path}");
        stage.AddChild(model);
        _models.Add(model);
        var camera = new Camera3D { Current = true, Near = .1f, Far = 200, KeepAspect = Camera3D.KeepAspectEnum.Height };
        stage.AddChild(camera);
        _cameras.Add(camera);
    }

    private void Resize()
    {
        var size = GetViewport().GetVisibleRect().Size;
        var scale = Math.Min(size.X / ReferenceWidth, size.Y / ReferenceHeight);
        _canvas.Scale = Vector2.One * scale;
        _canvas.Size = new Vector2(ReferenceWidth, ReferenceHeight);
        _canvas.Position = (size - _canvas.Size * scale) * .5f;
    }

    private void UpdateState()
    {
        var colour = MatchSettings.Palette[_team switch { "red" => 1, "gold" => 3, _ => 0 }];
        foreach (var model in _models)
        {
            Recolour(model, colour);
            if (EntityView.FindNamed(model, "Turret") is { } turret)
                turret.Rotation = new Vector3(0, _turned ? Mathf.Pi / 2 : 0, 0);
        }
        foreach (var (name, button) in _teamButtons) CommandTheme.Style(button, name == _team);
        _turretButton.Text = _turned ? "TURRET  90°" : "TURRET  0°";
        CommandTheme.Style(_turretButton, _turned);
        _scaleButton.Text = _rts ? "VIEW  RTS SCALE" : "VIEW  CLOSE-UP";
        CommandTheme.Style(_scaleButton, _rts);
        _viewLabel.Text = _rts ? "NORMAL GAME ZOOM  /  MATCHED 1:1 AT 1600 × 900" : "STUDIO VIEW  /  IDENTICAL CAMERA & LIGHTING";
        foreach (var camera in _cameras)
        {
            if (_rts)
            {
                // Crop the central part of the live camera's 45° / 32 m view to this
                // 442 px panel: a unit keeps its pixel size at the 900 px reference height.
                camera.Projection = Camera3D.ProjectionType.Perspective;
                camera.Fov = Mathf.RadToDeg(2 * Mathf.Atan(Mathf.Tan(Mathf.DegToRad(45f) / 2) * StageHeight / ReferenceHeight));
                var pitch = Mathf.DegToRad(52f);
                var yaw = Mathf.DegToRad(38f);
                camera.Position = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch)) * 32;
                camera.LookAt(Vector3.Zero);
            }
            else
            {
                camera.Projection = Camera3D.ProjectionType.Orthogonal;
                camera.Size = 4.25f;
                var target = new Vector3(.08f, .65f, 0);
                camera.Position = target + new Vector3(5.4f, 4.8f, 6.5f);
                camera.LookAt(target);
            }
        }
    }

    private static void Recolour(Node node, Color colour)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is { } geometry)
        {
            for (var i = 0; i < geometry.GetSurfaceCount(); i++)
            {
                if (mesh.GetActiveMaterial(i) is not BaseMaterial3D { ResourceName: "TeamColour" } material) continue;
                var copy = (BaseMaterial3D)material.Duplicate();
                copy.AlbedoColor = colour;
                mesh.SetSurfaceOverrideMaterial(i, copy);
            }
        }
        foreach (var child in node.GetChildren()) Recolour(child, colour);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Key1: _team = "blue"; break;
            case Key.Key2: _team = "red"; break;
            case Key.Key3: _team = "gold"; break;
            case Key.T: _turned = !_turned; break;
            case Key.Z: _rts = !_rts; break;
            default: return;
        }
        UpdateState();
    }

    public override void _Process(double delta)
    {
        if (_capture is null || ++_frames != 60) return;
        var error = GetViewport().GetTexture().GetImage().SavePng(_capture);
        GD.Print($"[Unit review] capture {_capture}: {error}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    private Button MakeButton(string caption, float x, float y, float width, Action action)
    {
        var button = new Button { Text = caption, Position = new Vector2(x, y), Size = new Vector2(width, 40) };
        CommandTheme.Style(button);
        button.Pressed += action;
        _canvas.AddChild(button);
        return button;
    }

    private Label Text(string value, float x, float y, float width, float height, int size, Color colour, bool display = false, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = value, Position = new Vector2(x, y), Size = new Vector2(width, height),
            MouseFilter = Control.MouseFilterEnum.Ignore, HorizontalAlignment = align,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontOverride("font", display ? CommandTheme.Display : CommandTheme.Body);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);
        _canvas.AddChild(label);
        return label;
    }

    private static void Solid(Node parent, Rect2 rect, Color colour) => parent.AddChild(new ColorRect
    {
        Position = rect.Position, Size = rect.Size, Color = colour, MouseFilter = Control.MouseFilterEnum.Ignore,
    });
}
