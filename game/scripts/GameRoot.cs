using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>Owns the sim World, ticks it at a fixed rate, and keeps one view per entity and supply pile.</summary>
public partial class GameRoot : Node3D
{
    public World World { get; private set; } = null!;
    public RtsCamera Camera { get; private set; } = null!;
    public SelectionController Selection { get; private set; } = null!;
    public PlacementController Placement { get; private set; } = null!;
    public Hud Hud { get; private set; } = null!;
    public Dictionary<int, EntityView> Views { get; } = new();
    public int LocalPlayer => 0;
    public Color LocalColour { get; private set; } = new(0.3f, 0.5f, 0.9f);
    /// <summary>Sim ticks per real-time tick; 1 is normal speed.</summary>
    public int Speed { get; set; } = 1;

    private Node3D _entitiesRoot = null!;
    private Node3D _pilesRoot = null!;
    private readonly List<SupplyPileView> _pileViews = new();
    private Vfx _vfx = null!;
    private MeshInstance3D _marker = null!;
    private StandardMaterial3D _markerMat = null!;
    private float _markerTtl;
    private float _accumulator;
    private string? _smokePath;
    private int _smokeStage;

    private static readonly Color EnemyColour = new(0.85f, 0.25f, 0.25f);

    public override void _Ready()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--smoke=")) _smokePath = arg["--smoke=".Length..];
            else if (arg.StartsWith("--speed=")) Speed = int.Parse(arg["--speed=".Length..]);
        }

        var rules = DataLoader.LoadRules();
        var map = rules.Map("plain");
        World = new World(rules, map, new[] { "coalition", "coalition" });
        LocalColour = Color.FromHtml(World.Player(LocalPlayer).Faction.Colour);

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
        _pilesRoot = new Node3D { Name = "Piles" };
        AddChild(_pilesRoot);
        foreach (var pile in World.Piles)
        {
            var v = SupplyPileView.Create(pile);
            _pilesRoot.AddChild(v);
            _pileViews.Add(v);
        }
        _vfx = new Vfx { Name = "Vfx" };
        AddChild(_vfx);
        Placement = new PlacementController { Name = "Placement", Root = this };
        AddChild(Placement);

        BuildMarker();
        SetUpStartPositions(map);
        BuildUi();
    }

    /// <summary>Generals-style start: an HQ, a builder, and a few units per player. The enemy gets a small fortified base.</summary>
    private void SetUpStartPositions(MapDef map)
    {
        var s0 = new Vec2(map.Spawns[0].X, map.Spawns[0].Y);
        var hq = World.PlaceBuilding("coalition_command_post", LocalPlayer, (int)s0.X - 2, (int)s0.Y - 2);
        hq.Rally = new Vec2(s0.X, s0.Y - 6);
        World.Spawn("coalition_dozer", LocalPlayer, s0 + new Vec2(-5, -3), Angles.DegToRad(-90));
        World.Spawn("coalition_dozer", LocalPlayer, s0 + new Vec2(5, -3), Angles.DegToRad(-90));
        for (var i = 0; i < 4; i++) World.Spawn("coalition_bulwark", LocalPlayer, s0 + new Vec2(-7 + i * 2.7f, -6), Angles.DegToRad(45));
        for (var i = 0; i < 2; i++) World.Spawn("coalition_warden", LocalPlayer, s0 + new Vec2(-8 + i * 2.4f, -9), Angles.DegToRad(45));

        var s1 = new Vec2(map.Spawns[1].X, map.Spawns[1].Y);
        World.PlaceBuilding("coalition_command_post", 1, (int)s1.X - 2, (int)s1.Y - 2);
        World.PlaceBuilding("coalition_power_plant", 1, (int)s1.X + 5, (int)s1.Y - 1);
        World.PlaceBuilding("coalition_barracks", 1, (int)s1.X - 8, (int)s1.Y - 1);
        World.PlaceBuilding("coalition_sentry_battery", 1, (int)s1.X - 3, (int)s1.Y - 8);
        World.PlaceBuilding("coalition_sentry_battery", 1, (int)s1.X + 3, (int)s1.Y - 8);
        for (var i = 0; i < 4; i++) World.Spawn("coalition_bulwark", 1, s1 + new Vec2(-4 + i * 2.7f, -11), Angles.DegToRad(-135));
        for (var i = 0; i < 4; i++) World.Spawn("coalition_rifleman", 1, s1 + new Vec2(-6 + i * 1.2f, -5), Angles.DegToRad(-135));
    }

    private void SmokeStep()
    {
        if (_smokePath is null) return;
        var w = World;
        var dozers = w.Entities.Where(e => e.Owner == LocalPlayer && e.IsBuilder).ToList();
        var s0 = new Vec2(w.MapDef.Spawns[0].X, w.MapDef.Spawns[0].Y);
        int Building(string id) => w.Entities.FirstOrDefault(e => e.Owner == LocalPlayer && e.Building?.Id == id && e.Operational)?.Id ?? 0;

        switch (_smokeStage)
        {
            case 0 when w.Tick >= 5:
                w.Submit(new BuildCommand(LocalPlayer, dozers[0].Id, "coalition_power_plant", (int)s0.X + 6, (int)s0.Y + 2));
                w.Submit(new BuildCommand(LocalPlayer, dozers[1].Id, "coalition_supply_center", (int)s0.X + 4, (int)s0.Y + 7));
                _smokeStage = 1;
                GD.Print("[Smoke] ordered power plant + supply center");
                break;
            case 1 when Building("coalition_supply_center") != 0:
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_supply_center"), "coalition_tiltrotor"));
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_supply_center"), "coalition_tiltrotor"));
                var free = dozers.FirstOrDefault(d => d.BuildTargetId == 0) ?? dozers[0];
                w.Submit(new BuildCommand(LocalPlayer, free.Id, "coalition_barracks", (int)s0.X - 9, (int)s0.Y + 2));
                _smokeStage = 2;
                GD.Print("[Smoke] supply center up; queued tiltrotors, ordered barracks");
                break;
            case 2 when Building("coalition_barracks") != 0 && Building("coalition_power_plant") != 0:
                var d0 = dozers.FirstOrDefault(d => d.BuildTargetId == 0) ?? dozers[0];
                w.Submit(new BuildCommand(LocalPlayer, d0.Id, "coalition_motor_pool", (int)s0.X + 10, (int)s0.Y + 8));
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_barracks"), "coalition_rifleman"));
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_barracks"), "coalition_rifleman"));
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_barracks"), "coalition_rocket_trooper"));
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_power_plant"), "coalition_turbine_overdrive"));
                _smokeStage = 3;
                GD.Print("[Smoke] ordered motor pool, infantry, overdrive");
                break;
            case 3 when Building("coalition_motor_pool") != 0:
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_motor_pool"), "coalition_bulwark"));
                w.Submit(new ProduceCommand(LocalPlayer, Building("coalition_motor_pool"), "coalition_warden"));
                var d1 = dozers.FirstOrDefault(d => d.BuildTargetId == 0) ?? dozers[0];
                w.Submit(new BuildCommand(LocalPlayer, d1.Id, "coalition_sentry_battery", (int)s0.X - 2, (int)s0.Y - 10));
                _smokeStage = 4;
                _smokeTick = w.Tick;
                GD.Print("[Smoke] motor pool up; queued vehicles, ordered sentry");
                break;
            case 4 when w.Tick >= _smokeTick + 20 * 30:
                Camera.Position = MapView.ToWorld(s0 + new Vec2(2, 0));
                foreach (var e in w.Entities.Where(e => e.Owner == LocalPlayer && e.Building?.Id == "coalition_supply_center"))
                    Views[e.Id].Selected = true;
                Selection.SelectOnly(Building("coalition_supply_center"));
                _smokeStage = 5;
                _smokeTick = w.Tick;
                break;
            case 5 when w.Tick >= _smokeTick + 3:
                GetViewport().GetTexture().GetImage().SavePng(_smokePath!);
                GD.Print($"[Smoke] saved {_smokePath} at tick {w.Tick}; cash {w.Player(LocalPlayer).Cash}; buildings {w.Entities.Count(e => e.Owner == 0 && e.IsBuilding)}; units {w.Entities.Count(e => e.Owner == 0 && !e.IsBuilding)}");
                GetTree().Quit();
                _smokeStage = 6;
                break;
        }
    }

    private int _smokeTick;

    public override void _Process(double delta)
    {
        _accumulator += (float)delta;
        var steps = 0;
        var maxSteps = 5 * Speed;
        while (_accumulator >= World.Dt / Speed && steps < maxSteps)
        {
            World.Step();
            _accumulator -= World.Dt / Speed;
            steps++;
            HandleEvents();
        }
        var alpha = Mathf.Clamp(_accumulator * Speed / World.Dt, 0f, 1f);
        SmokeStep();

        foreach (var e in World.Entities)
        {
            if (!Views.TryGetValue(e.Id, out var view))
            {
                view = EntityView.Create(e, PlayerColour(e.Owner));
                view.SetTeamColour(PlayerColour(e.Owner));
                _entitiesRoot.AddChild(view);
                Views[e.Id] = view;
            }
            view.Sync(alpha);
            if (e.Owner != LocalPlayer) view.Visible = World.Vision.IsVisible(LocalPlayer, e.Pos) || (e.IsBuilding && World.Vision.Get(LocalPlayer, e.Pos) != Visibility.Shroud);
        }
        foreach (var pv in _pileViews) pv.Refresh();
        _vfx.SyncProjectiles(World, alpha);

        if (_markerTtl > 0)
        {
            _markerTtl -= (float)delta;
            _marker.Visible = _markerTtl > 0;
            _marker.Scale = Vector3.One * (0.6f + _markerTtl);
        }
    }

    private void HandleEvents()
    {
        _vfx.Consume(World, PlayerColour);
        foreach (var ev in World.Events)
        {
            switch (ev)
            {
                case DiedEvent d:
                    RemoveView(d.EntityId);
                    break;
                case SoldEvent s:
                    RemoveView(s.BuildingId);
                    break;
                case OrderRejectedEvent r when r.Player == LocalPlayer:
                    Hud.Say(r.Reason switch
                    {
                        "insufficient funds" => "Insufficient funds",
                        "occupied" => "Cannot build there",
                        "terrain" => "Cannot build on that terrain",
                        _ => r.Reason,
                    });
                    break;
                case ConstructionCompletedEvent c when c.Owner == LocalPlayer && World.Tick > 1:
                    Hud.Say($"{World.Get(c.BuildingId)?.Def.Name} complete");
                    break;
                case UpgradeCompletedEvent u when u.Owner == LocalPlayer:
                    Hud.Say($"{World.Rules.Upgrade(u.UpgradeId).Name} researched");
                    break;
            }
        }
    }

    private void RemoveView(int id)
    {
        if (!Views.TryGetValue(id, out var view)) return;
        view.QueueFree();
        Views.Remove(id);
    }

    public Color PlayerColour(int owner) => owner == LocalPlayer ? LocalColour : EnemyColour;

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
        Selection = new SelectionController { Player = LocalPlayer, Root = this };
        layer.AddChild(Selection);
        layer.AddChild(new UnitOverlay { Root = this });
        Hud = new Hud { Root = this, Layer = 2 };
        AddChild(Hud);
    }
}
