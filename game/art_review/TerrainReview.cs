using Godot;
using Overmatch.Game.UiReview;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game.ArtReview;

/// <summary>Frozen map presentation fixture. Map definitions and simulation are read-only.</summary>
public partial class TerrainReview : Node3D
{
    private static readonly string[] Maps = { "plain", "twin_rivers", "crossroads", "highlands", "oil_rush", "open_steppe" };
    private string _id = "twin_rivers";
    private string? _capture;
    private bool _overview;
    private bool _fog;
    private int _frames;
    private Node3D _stage = null!;
    private Camera3D _camera = null!;
    private Vector3 _target;
    private float _distance = 34;
    private float _yaw = .15f;
    private GameRules _rules = null!;
    private Label _title = null!;
    private Label _subtitle = null!;
    private double _processTotal;
    private int _processSamples;

    public override void _Ready()
    {
        GetWindow().Title = "Overmatch — battlefield art review";
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--terrain-map=")) _id = arg[14..];
            if (arg.StartsWith("--terrain-capture=")) _capture = arg[18..];
            if (arg == "--terrain-overview") _overview = true;
            if (arg == "--terrain-fog") _fog = true;
        }
        _rules = DataLoader.LoadRules();
        var layer = new CanvasLayer(); AddChild(layer);
        var header = new ColorRect { Color = new Color("0e181bf2"), Position = Vector2.Zero, Size = new Vector2(1920, 105), MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(header);
        _title = Label("",new Vector2(34,19),34,CommandTheme.Text); layer.AddChild(_title);
        _subtitle = Label("",new Vector2(35,65),17,CommandTheme.Gold); layer.AddChild(_subtitle);
        var footer = Label("F1–F6  MAP    O  OVERVIEW    ARROWS / WASD  PAN    WHEEL  ZOOM    Q / E  ROTATE",new Vector2(35,1050),17,CommandTheme.Text);
        layer.AddChild(footer);
        GetViewport().SizeChanged += () => footer.Position = new Vector2(35,GetViewport().GetVisibleRect().Size.Y-39);
        footer.Position = new Vector2(35,GetViewport().GetVisibleRect().Size.Y-39);
        Build();
    }

    private void Build()
    {
        if (_stage is not null) { RemoveChild(_stage); _stage.QueueFree(); }
        _stage = new Node3D(); AddChild(_stage);
        var map = _rules.Map(_id);
        var world = new World(_rules,map,new[] { "coalition", "directorate" });
        var view = new MapView(); _stage.AddChild(view); view.Build(world,0);
        var focus = _id switch
        {
            "twin_rivers" => new Vec2(33,26), "crossroads" => new Vec2(42,39),
            "highlands" => new Vec2(31,37), "oil_rush" => new Vec2(42,43),
            "open_steppe" => new Vec2(45,42), _ => new Vec2(28,28),
        };
        var vehicles = new[] { "coalition_bulwark", "coalition_warden", "coalition_rifleman" };
        for (var i = 0; i < vehicles.Length; i++)
        {
            for (var radius = 1; radius < 14; radius++)
            {
                var p = focus + new Vec2(-4-i*2, -radius-2);
                if (!world.Grid.IsPassable(p,Locomotor.Tracked)) continue;
                world.Spawn(vehicles[i],0,p,.45f); break;
            }
        }
        foreach (var entity in world.Entities)
        {
            var colour = entity.Owner >= 0 ? MatchSettings.Palette[entity.Owner] : new Color("a29c85");
            var entityView = EntityView.Create(entity,colour); entityView.SetTeamColour(colour); _stage.AddChild(entityView);
        }
        foreach (var pile in world.Piles) _stage.AddChild(SupplyPileView.Create(pile));
        if (!_fog) world.Vision.RevealAll(0);
        else world.Vision.Recompute(world.Entities);
        _stage.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55,35,0), LightEnergy = 1.3f, ShadowEnabled = true, DirectionalShadowMaxDistance = 150 });
        _stage.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(.55f,.65f,.75f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color(.7f,.75f,.8f), AmbientLightEnergy = .6f,
            FogEnabled = true, FogLightColor = new Color(.6f,.68f,.75f), FogDensity = .002f,
        }});
        _camera = new Camera3D { Fov = 45, Far = 600, Current = true };
        _stage.AddChild(_camera);
        _target = _overview ? new Vector3(map.Width/2f,0,-map.Height/2f) : MapView.ToWorld(focus);
        _distance = _overview ? Math.Max(map.Width,map.Height)*1.75f : 34;
        _yaw = _overview ? 0 : .15f;
        UpdateCamera();
        _title.Text = "OVERMATCH  /  " + map.Name.ToUpperInvariant();
        _subtitle.Text = (_overview ? "BATTLEFIELD OVERVIEW" : "NORMAL RTS CAMERA") + "    ·    LIVE MAP GEOMETRY / FROZEN REVIEW    ·    " + map.Description;
        _frames = 0; _processTotal = 0; _processSamples = 0;
        GD.Print($"[Terrain review] {_id}: grid {map.Width}x{map.Height}, {map.Blocked.Count} blocked regions, {map.Road.Count} roads, {map.Water.Count} water regions");
    }

    private void UpdateCamera()
    {
        var pitch = Mathf.DegToRad(_overview ? 66 : 52);
        _camera.Position = _target + new Vector3(Mathf.Sin(_yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),Mathf.Cos(_yaw)*Mathf.Cos(pitch))*_distance;
        _camera.LookAt(_target);
    }

    public override void _Process(double delta)
    {
        if (_capture is null)
        {
            var v = new Vector3((Input.IsPhysicalKeyPressed(Key.D)||Input.IsPhysicalKeyPressed(Key.Right)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)||Input.IsPhysicalKeyPressed(Key.Left)?1:0),0,
                (Input.IsPhysicalKeyPressed(Key.S)||Input.IsPhysicalKeyPressed(Key.Down)?1:0)-(Input.IsPhysicalKeyPressed(Key.W)||Input.IsPhysicalKeyPressed(Key.Up)?1:0));
            _target += v*(float)delta*_distance*.6f;
            if (Input.IsPhysicalKeyPressed(Key.Q)) _yaw += (float)delta;
            if (Input.IsPhysicalKeyPressed(Key.E)) _yaw -= (float)delta;
            UpdateCamera(); return;
        }
        _frames++;
        if (_frames > 30) { _processTotal += Performance.GetMonitor(Performance.Monitor.TimeProcess); _processSamples++; }
        if (_frames != 90) return;
        var error = GetViewport().GetTexture().GetImage().SavePng(_capture);
        GD.Print($"[Terrain review] capture {_capture}: {error}; average process {1000*_processTotal/Math.Max(1,_processSamples):F2}ms; rendered triangles {Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode >= Key.F1 && key.Keycode <= Key.F6) { _id = Maps[(int)key.Keycode-(int)Key.F1]; Build(); }
            if (key.Keycode == Key.O) { _overview = !_overview; Build(); }
        }
        if (@event is InputEventMouseButton { Pressed: true } mouse)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp) _distance = Math.Max(15,_distance-4);
            if (mouse.ButtonIndex == MouseButton.WheelDown) _distance = Math.Min(220,_distance+4);
            UpdateCamera();
        }
    }

    private static Label Label(string text, Vector2 position, int size, Color colour)
    {
        var label = new Label { Text = text, Position = position, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font",CommandTheme.Display); label.AddThemeFontSizeOverride("font_size",size); label.AddThemeColorOverride("font_color",colour); return label;
    }
}
