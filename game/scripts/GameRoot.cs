using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Owns the sim World, ticks it at a fixed rate, and keeps one EntityView per entity.</summary>
public partial class GameRoot : Node3D
{
    public World World { get; private set; } = null!;
    public RtsCamera Camera { get; private set; } = null!;
    public Dictionary<int, EntityView> Views { get; } = new();

    private Node3D _entitiesRoot = null!;
    private Label _hud = null!;
    private MeshInstance3D _moveMarker = null!;
    private float _markerTtl;
    private float _accumulator;
    private const int LocalPlayer = 0;
    private string? _smokePath;
    private bool _smokeMoved;

    public override void _Ready()
    {
        var rules = DataLoader.LoadRules();
        World = new World(rules);

        BuildGround();
        BuildLighting();

        Camera = new RtsCamera { Position = new Vector3(0, 0, 10) };
        AddChild(Camera);

        _entitiesRoot = new Node3D { Name = "Entities" };
        AddChild(_entitiesRoot);

        SpawnTestArmy();
        BuildUi();

        // `--smoke=/path/out.png` (after `--`) drives a scripted move order, saves a screenshot and quits. Used by Claude/CI for visual checks.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--smoke=")) _smokePath = arg["--smoke=".Length..];
    }

    private void SmokeStep()
    {
        if (_smokePath is null) return;
        if (!_smokeMoved && World.Tick >= 10)
        {
            _smokeMoved = true;
            var ids = World.Entities.Where(e => e.Owner == LocalPlayer).Select(e => e.Id).ToArray();
            foreach (var id in ids) Views[id].Selected = true;
            World.Submit(new MoveCommand(LocalPlayer, ids, new Vec2(12, 8)));
            ShowMoveMarker(new Vector3(12, 0, -8));
            GD.Print("[Smoke] issued move order");
        }
        if (World.Tick >= 70)
        {
            var img = GetViewport().GetTexture().GetImage();
            img.SavePng(_smokePath);
            GD.Print($"[Smoke] saved {_smokePath}; quitting");
            _smokePath = null;
            GetTree().Quit();
        }
    }

    private void SpawnTestArmy()
    {
        var rng = new Random(7);
        for (var i = 0; i < 12; i++)
        {
            var col = i % 4;
            var row = i / 4;
            World.Spawn("coalition_bulwark", LocalPlayer, new Vec2(-8 + col * 2.6f, -6 + row * 2.6f), Angles.DegToRad(90));
        }
        World.Spawn("coalition_dozer", LocalPlayer, new Vec2(6, -2), Angles.DegToRad(90));
        World.Spawn("coalition_dozer", LocalPlayer, new Vec2(8, -2), Angles.DegToRad(90));
        // A few enemy tanks parked on the far side, not selectable by the local player.
        for (var i = 0; i < 4; i++)
            World.Spawn("coalition_bulwark", 1, new Vec2(20 + i * 2.2f + (float)rng.NextDouble(), 25), Angles.DegToRad(-90));
    }

    public override void _Process(double delta)
    {
        _accumulator += (float)delta;
        var steps = 0;
        while (_accumulator >= World.Dt && steps < 5)
        {
            World.Step();
            _accumulator -= World.Dt;
            steps++;
        }
        var alpha = Mathf.Clamp(_accumulator / World.Dt, 0f, 1f);
        SmokeStep();

        foreach (var e in World.Entities)
        {
            if (!Views.TryGetValue(e.Id, out var view))
            {
                var faction = World.Rules.Factions.TryGetValue(e.Def.Faction, out var f) ? f : null;
                var colour = PlayerColour(e.Owner, faction);
                view = EntityView.Create(e, colour);
                _entitiesRoot.AddChild(view);
                Views[e.Id] = view;
            }
            view.Sync(alpha);
        }

        if (_markerTtl > 0)
        {
            _markerTtl -= (float)delta;
            _moveMarker.Visible = _markerTtl > 0;
            _moveMarker.Scale = Vector3.One * (0.6f + _markerTtl);
        }

        _hud.Text = $"OVERMATCH  pre-alpha M0   tick {World.Tick}   {Engine.GetFramesPerSecond()} fps\n" +
                    "Left-drag: select   Shift: add   Right-click: move   Ctrl+A: all   S: stop   WASD/edges: pan   Q/E: rotate   Wheel / two-finger scroll / +-: zoom";
    }

    private static Color PlayerColour(int owner, FactionDef? faction)
    {
        if (owner == 1) return new Color(0.85f, 0.25f, 0.25f);
        if (faction is not null) return Color.FromHtml(faction.Colour);
        return new Color(0.5f, 0.5f, 0.5f);
    }

    public void ShowMoveMarker(Vector3 at)
    {
        _moveMarker.Position = new Vector3(at.X, 0.06f, at.Z);
        _moveMarker.Visible = true;
        _markerTtl = 0.6f;
    }

    private void BuildGround()
    {
        const int texSize = 64;
        var img = Image.CreateEmpty(texSize, texSize, false, Image.Format.Rgb8);
        var a = new Color(0.36f, 0.42f, 0.24f);
        var b = new Color(0.32f, 0.38f, 0.21f);
        for (var y = 0; y < texSize; y++)
            for (var x = 0; x < texSize; x++)
                img.SetPixel(x, y, ((x / 32 + y / 32) % 2 == 0) ? a : b);
        var tex = ImageTexture.CreateFromImage(img);

        var ground = new MeshInstance3D
        {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = new Vector2(200, 200), SubdivideDepth = 1, SubdivideWidth = 1 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = tex,
                Uv1Scale = new Vector3(50, 50, 1),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Roughness = 1f,
            },
        };
        AddChild(ground);

        _moveMarker = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.5f, OuterRadius = 0.6f, Rings = 24, RingSegments = 6 },
            Visible = false,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.4f, 1f, 0.4f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
        };
        AddChild(_moveMarker);
    }

    private void BuildLighting()
    {
        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, 35, 0),
            LightEnergy = 1.3f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 150f,
        };
        AddChild(sun);

        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.55f, 0.65f, 0.75f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.7f, 0.75f, 0.8f),
            AmbientLightEnergy = 0.6f,
            FogEnabled = true,
            FogLightColor = new Color(0.6f, 0.68f, 0.75f),
            FogDensity = 0.002f,
        };
        AddChild(new WorldEnvironment { Environment = env });
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        var selection = new SelectionController { Player = LocalPlayer, Root = this };
        layer.AddChild(selection);

        _hud = new Label
        {
            Position = new Vector2(12, 8),
            Modulate = new Color(1, 1, 1, 0.9f),
        };
        _hud.AddThemeFontSizeOverride("font_size", 14);
        layer.AddChild(_hud);
    }
}
