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
    private Vfx _vfx = null!;
    private SelectionController _selection = null!;
    private Label _hud = null!;
    private MeshInstance3D _marker = null!;
    private StandardMaterial3D _markerMat = null!;
    private float _markerTtl;
    private float _accumulator;
    private const int LocalPlayer = 0;
    private string? _smokePath;
    private int _smokeStage;

    private static readonly Color EnemyColour = new(0.85f, 0.25f, 0.25f);

    public override void _Ready()
    {
        var rules = DataLoader.LoadRules();
        var map = rules.Map("plain");
        World = new World(rules, map, 2);

        var mapView = new MapView { Name = "Map" };
        AddChild(mapView);
        mapView.Build(World, LocalPlayer);
        BuildLighting();

        var spawn = map.Spawns.Count > 0 ? new Vec2(map.Spawns[0].X, map.Spawns[0].Y) : new Vec2(map.Width / 2f, map.Height / 2f);
        Camera = new RtsCamera
        {
            Position = MapView.ToWorld(spawn),
            MinBounds = new Vector2(0, -map.Height),
            MaxBounds = new Vector2(map.Width, 0),
        };
        AddChild(Camera);

        _entitiesRoot = new Node3D { Name = "Entities" };
        AddChild(_entitiesRoot);
        _vfx = new Vfx { Name = "Vfx" };
        AddChild(_vfx);

        BuildMarker();
        SpawnTestArmies(spawn);
        BuildUi();

        // `--smoke=/path/out.png` (after `--`) drives a scripted attack-move, saves screenshots and quits. Used by Claude/CI for visual checks.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--smoke=")) _smokePath = arg["--smoke=".Length..];
    }

    private void SpawnTestArmies(Vec2 spawn)
    {
        for (var i = 0; i < 12; i++)
            World.Spawn("coalition_bulwark", LocalPlayer, spawn + new Vec2(-4 + i % 4 * 2.6f, -4 + i / 4 * 2.6f), Angles.DegToRad(45));
        for (var i = 0; i < 4; i++)
            World.Spawn("coalition_warden", LocalPlayer, spawn + new Vec2(6 + i * 2.2f, -4), Angles.DegToRad(45));
        World.Spawn("coalition_dozer", LocalPlayer, spawn + new Vec2(-6, 6), Angles.DegToRad(45));
        World.Spawn("coalition_dozer", LocalPlayer, spawn + new Vec2(-8, 6), Angles.DegToRad(45));

        // An enemy picket beyond the rocks, facing us.
        var enemy = new Vec2(52, 50);
        for (var i = 0; i < 6; i++)
            World.Spawn("coalition_bulwark", 1, enemy + new Vec2(i % 3 * 2.6f, i / 3 * 2.6f), Angles.DegToRad(-135));
        for (var i = 0; i < 3; i++)
            World.Spawn("coalition_warden", 1, enemy + new Vec2(-3 + i * 2.2f, 6), Angles.DegToRad(-135));
    }

    private void SmokeStep()
    {
        if (_smokePath is null) return;
        if (_smokeStage == 0 && World.Tick >= 10)
        {
            _smokeStage = 1;
            var ids = World.Entities.Where(e => e.Owner == LocalPlayer && e.HasWeapons).Select(e => e.Id).ToArray();
            foreach (var id in ids) Views[id].Selected = true;
            World.Submit(new AttackMoveCommand(LocalPlayer, ids, new Vec2(54, 52)));
            ShowMarker(MapView.ToWorld(new Vec2(54, 52)), new Color(1f, 0.6f, 0.2f));
            GD.Print("[Smoke] issued attack-move");
        }
        if (_smokeStage == 1 && World.Tick >= 120)
        {
            _smokeStage = 2;
            Save(_smokePath.Replace(".png", "_a.png"));
        }
        if (_smokeStage == 2 && World.Tick >= 200)
        {
            _smokeStage = 3;
            Camera.Position = MapView.ToWorld(new Vec2(46, 44));
        }
        if (_smokeStage == 3 && World.Tick >= 290)
        {
            _smokeStage = 4;
            Save(_smokePath);
            GD.Print("[Smoke] done; quitting");
            GetTree().Quit();
        }

        void Save(string path)
        {
            GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"[Smoke] saved {path} at tick {World.Tick}");
        }
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
            _vfx.Consume(World, PlayerColour);
            RemoveDeadViews();
        }
        var alpha = Mathf.Clamp(_accumulator / World.Dt, 0f, 1f);
        SmokeStep();

        foreach (var e in World.Entities)
        {
            if (!Views.TryGetValue(e.Id, out var view))
            {
                view = EntityView.Create(e, PlayerColour(e.Owner));
                _entitiesRoot.AddChild(view);
                Views[e.Id] = view;
            }
            view.Sync(alpha);
            if (e.Owner != LocalPlayer) view.Visible = World.Vision.IsVisible(LocalPlayer, e.Pos);
        }
        _vfx.SyncProjectiles(World, alpha);

        if (_markerTtl > 0)
        {
            _markerTtl -= (float)delta;
            _marker.Visible = _markerTtl > 0;
            _marker.Scale = Vector3.One * (0.6f + _markerTtl);
        }

        var mine = World.Entities.Count(e => e.Owner == LocalPlayer);
        var theirs = World.Entities.Count(e => e.Owner != LocalPlayer);
        var mode = _selection.AttackMoveArmed ? "   [ATTACK-MOVE: click target]" : "";
        _hud.Text = $"OVERMATCH  pre-alpha M1   tick {World.Tick}   {Engine.GetFramesPerSecond()} fps   units {mine} vs {theirs}{mode}\n" +
                    "Left-drag: select   Shift: add   Right-click: move / attack   A + click: attack-move   Ctrl+A: all   S: stop   Esc: cancel\n" +
                    "WASD / edges / middle-drag: pan   Q/E: rotate   Wheel / two-finger scroll / + -: zoom";
    }

    private void RemoveDeadViews()
    {
        foreach (var ev in World.Events)
        {
            if (ev is not DiedEvent d || !Views.TryGetValue(d.EntityId, out var view)) continue;
            view.QueueFree();
            Views.Remove(d.EntityId);
        }
    }

    private Color PlayerColour(int owner)
    {
        if (owner == LocalPlayer)
        {
            var faction = World.Rules.Factions.TryGetValue("coalition", out var f) ? f : null;
            return faction is not null ? Color.FromHtml(faction.Colour) : new Color(0.3f, 0.5f, 0.9f);
        }
        return EnemyColour;
    }

    public void ShowMarker(Vector3 at, Color colour)
    {
        _marker.Position = new Vector3(at.X, 0.06f, at.Z);
        _markerMat.AlbedoColor = colour;
        _marker.Visible = true;
        _markerTtl = 0.6f;
    }

    private void BuildMarker()
    {
        _markerMat = new StandardMaterial3D { AlbedoColor = new Color(0.4f, 1f, 0.4f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        _marker = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.5f, OuterRadius = 0.6f, Rings = 24, RingSegments = 6 },
            Visible = false,
            MaterialOverride = _markerMat,
        };
        AddChild(_marker);
    }

    private void BuildLighting()
    {
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, 35, 0),
            LightEnergy = 1.3f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 150f,
        });
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.55f, 0.65f, 0.75f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.7f, 0.75f, 0.8f),
                AmbientLightEnergy = 0.6f,
                FogEnabled = true,
                FogLightColor = new Color(0.6f, 0.68f, 0.75f),
                FogDensity = 0.002f,
            },
        });
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        _selection = new SelectionController { Player = LocalPlayer, Root = this };
        layer.AddChild(_selection);
        layer.AddChild(new UnitOverlay { Root = this });

        _hud = new Label { Position = new Vector2(12, 8), Modulate = new Color(1, 1, 1, 0.9f) };
        _hud.AddThemeFontSizeOverride("font_size", 14);
        layer.AddChild(_hud);
    }
}
