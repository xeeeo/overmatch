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
    public bool Paused { get; set; }
    public MatchSettings Settings { get; set; } = MatchSettings.Default();
    public App App { get; set; } = null!;
    public string? SmokePath { get; set; }
    public ResultOverlay Result { get; private set; } = null!;
    public AudioManager Audio { get; private set; } = null!;
    public Minimap Minimap { get; private set; } = null!;
    /// <summary>Where the last "under attack" alert happened (Space jumps there).</summary>
    public Vec2? LastAlert { get; private set; }
    private bool _resultShown;
    private float _attackAlertCooldown;

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

    public override void _Ready()
    {
        _smokePath = SmokePath;
        var rules = DataLoader.LoadRules();
        var map = rules.Map(Settings.MapId);
        World = new World(rules, map, Settings.Players.Select(p => p.Faction));
        for (var i = 0; i < Settings.Players.Count; i++)
        {
            var slot = Settings.Players[i];
            var player = World.Player(i);
            player.Name = slot.Name;
            player.Cash = Settings.StartingCash;
            if (slot.IsAi) World.AddAi(i, rules.Ai(slot.Faction, slot.Difficulty));
        }
        LocalColour = Settings.Players[LocalPlayer].Colour;

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
        _vfx = new Vfx { Name = "Vfx", Camera = Camera };
        AddChild(_vfx);
        Audio = new AudioManager { Name = "Audio", Root = this };
        AddChild(Audio);
        Placement = new PlacementController { Name = "Placement", Root = this };
        AddChild(Placement);

        BuildMarker();
        SetUpStartPositions(map);
        BuildUi();
    }

    /// <summary>Generals-style start: every player gets a Command Post and one Dozer at their spawn.</summary>
    private void SetUpStartPositions(MapDef map)
    {
        for (var p = 0; p < World.PlayerCount; p++)
        {
            var spawn = map.Spawns[p % map.Spawns.Count];
            var s = new Vec2(spawn.X, spawn.Y);
            var faction = World.Player(p).Faction;
            var hqId = faction.Hq != "" ? faction.Hq : "coalition_command_post";
            var hqDef = World.Rules.Building(hqId);
            var hq = World.PlaceBuilding(hqId, p, (int)s.X - hqDef.Width / 2, (int)s.Y - hqDef.Height / 2);
            var toCentre = (new Vec2(map.Width / 2f, map.Height / 2f) - s).Normalized;
            hq.Rally = s + toCentre * 7f;
            var builder = faction.Builder != "" ? faction.Builder : "coalition_dozer";
            World.Spawn(builder, p, s + toCentre * 5f, toCentre.Angle);
        }
    }

    private void SmokeStep()
    {
        if (_smokePath is null) return;
        var w = World;
        if (Settings.Players.Any(p => p.IsAi))
        {
            // Skirmish smoke: watch the AI for six minutes of game time, then photograph its base.
            var minutes = w.Time / 60f;
            // Exercise the interface as a player would, so HUD exceptions surface here and not in a play test.
            if (_uiStage == 0 && minutes >= 0.5f)
            {
                _uiStage = 1;
                var hq = w.Entities.FirstOrDefault(e => e.Owner == LocalPlayer && e.IsBuilding);
                if (hq is not null) Selection.SelectOnly(hq.Id);
            }
            else if (_uiStage == 1 && minutes >= 1f)
            {
                _uiStage = 2;
                var builder = w.Entities.FirstOrDefault(e => e.Owner == LocalPlayer && e.IsBuilder);
                if (builder is not null) Selection.SelectOnly(builder.Id);
                var size = GetViewport().GetVisibleRect().Size;
                for (var x = 0.1f; x < 1f; x += 0.2f)
                    for (var y = 0.1f; y < 0.8f; y += 0.2f)
                        Selection.HoverHint(new Vector2(size.X * x, size.Y * y));
                foreach (var d in w.Rules.Units.Values.Cast<Overmatch.Sim.Data.ObjectDef>().Concat(w.Rules.Buildings.Values)) Hud.Describe(d);
                GD.Print("[Smoke] UI paths exercised");
            }
            else if (_uiStage == 2 && minutes >= 1.2f)
            {
                _uiStage = 3;
                GetViewport().GetTexture().GetImage().SavePng(_smokePath!.Replace(".png", "_ui.png"));
                // Now a production building with a live queue.
                var hq = w.Entities.FirstOrDefault(e => e.Owner == LocalPlayer && e.Building is { Hq: true });
                if (hq is not null)
                {
                    Selection.SelectOnly(hq.Id);
                    var unit = hq.Building!.Produces.FirstOrDefault();
                    if (unit is not null) for (var q = 0; q < 3; q++) w.Submit(new ProduceCommand(LocalPlayer, hq.Id, unit));
                }
            }
            else if (_uiStage == 3 && minutes >= 1.35f)
            {
                _uiStage = 4;
                GetViewport().GetTexture().GetImage().SavePng(_smokePath!.Replace(".png", "_queue.png"));
                Result.TogglePause();
            }
            else if (_uiStage == 4)
            {
                _uiStage = 5;
                _pauseShotFrames = 8;
            }
            if (_pauseShotFrames > 0 && --_pauseShotFrames == 0)
            {
                GetViewport().GetTexture().GetImage().SavePng(_smokePath!.Replace(".png", "_pause.png"));
                Result.TogglePause();
            }
            if (w.Tick / 600 != _lastLogged)
            {
                _lastLogged = w.Tick / 600;
                GD.Print($"[Smoke] real={Time.GetTicksMsec() / 1000f:0.0}s t={minutes:0.0}min tick={w.Tick} fps={Engine.GetFramesPerSecond()} ai='{w.Ais[0].Status}' aiBuildings={w.Entities.Count(e => e.Owner == 1 && e.IsBuilding)} aiUnits={w.Entities.Count(e => e.Owner == 1 && !e.IsBuilding)} cash={w.Player(1).Cash} entities={w.Entities.Count}");
            }
            if (_smokeStage == 0 && minutes >= 6f)
            {
                var ai = w.Entities.FirstOrDefault(e => e.Owner == 1 && e.Building is { Hq: true });
                if (ai is not null) Camera.Position = MapView.ToWorld(ai.Pos + new Vec2(-4, -6));
                w.Vision.RevealAll(LocalPlayer); // photograph the AI base without fog
                _smokeStage = 101;
                _smokeTick = w.Tick;
            }
            else if (_smokeStage == 101 && w.Tick >= _smokeTick + 3)
            {
                GetViewport().GetTexture().GetImage().SavePng(_smokePath!);
                var ai = w.Ais[0];
                GD.Print($"[Smoke] saved {_smokePath} at {w.Time / 60f:0.0} min; AI cash {w.Player(1).Cash}; buildings {w.Entities.Count(e => e.Owner == 1 && e.IsBuilding)}; units {w.Entities.Count(e => e.Owner == 1 && !e.IsBuilding)}; status '{ai.Status}'; finished {w.Finished} winner {w.Winner}");
                GetTree().Quit();
                _smokeStage = 102;
            }
            return;
        }
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
    private int _lastLogged = -1;
    private int _uiStage;
    private int _pauseShotFrames;

    public override void _Process(double delta)
    {
        if (!Paused) _accumulator += (float)delta;
        var steps = 0;
        var maxSteps = 5 * Speed;
        while (!Paused && _accumulator >= World.Dt / Speed && steps < maxSteps)
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
            if (e.IsInside) view.Visible = false;
            else if (e.Owner != LocalPlayer)
                view.Visible = World.CanSee(LocalPlayer, e) || (e.IsBuilding && !e.Def.Stealth && World.Vision.Get(LocalPlayer, e.Pos) != Visibility.Shroud);
            else view.Visible = true;
            view.SetLook(e.Def.Stealth || World.Player(LocalPlayer).StealthFor(e.Def) ? (e.Owner == LocalPlayer ? 0.55f : 1f) : 1f, e.Disabled);
        }
        foreach (var pv in _pileViews) pv.Refresh();
        _vfx.SyncProjectiles(World, alpha);
        _vfx.SyncWorldObjects(World);

        if (_markerTtl > 0)
        {
            _markerTtl -= (float)delta;
            _marker.Visible = _markerTtl > 0;
            _marker.Scale = Vector3.One * (0.6f + _markerTtl);
        }
        if (_attackAlertCooldown > 0) _attackAlertCooldown -= (float)delta;
    }

    private void HandleEvents()
    {
        _vfx.Consume(World, PlayerColour);
        Audio.Consume(World);
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
                case DamagedEvent dmg when _attackAlertCooldown <= 0 && dmg.AttackerId != 0 && World.Get(dmg.EntityId) is { } hurt && hurt.Owner == LocalPlayer:
                    // Only alert for things happening off screen.
                    var onScreen = !Camera.Camera.IsPositionBehind(MapView.ToWorld(hurt.Pos)) && GetViewport().GetVisibleRect().HasPoint(Camera.Camera.UnprojectPosition(MapView.ToWorld(hurt.Pos)));
                    LastAlert = hurt.Pos;
                    Minimap.Ping(hurt.Pos);
                    if (!onScreen)
                    {
                        Hud.Say(hurt.IsBuilding ? "Base under attack  (Space to look)" : "Units under attack  (Space to look)");
                        Audio.PlayUi("alert");
                        Audio.Announce(hurt.IsBuilding ? "base_under_attack" : "unit_under_attack", 15000);
                    }
                    _attackAlertCooldown = 8f;
                    break;
                case PlayerEliminatedEvent pe when pe.Player != LocalPlayer:
                    Hud.Say($"{World.Player(pe.Player).Name} eliminated");
                    break;
                case MatchEndedEvent m when !_resultShown:
                    _resultShown = true;
                    var won = m.Winner == LocalPlayer;
                    var detail = m.Winner >= 0 ? $"{World.Player(m.Winner).Name} wins after {World.Time / 60f:0} minutes" : "Draw";
                    Result.ShowResult(won, detail);
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

    public Color PlayerColour(int owner) => owner >= 0 && owner < Settings.Players.Count ? Settings.Players[owner].Colour : new Color(0.5f, 0.5f, 0.5f);

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
        Result = new ResultOverlay { Root = this };
        AddChild(Result);
        Minimap = new Minimap { Root = this };
        Hud.MinimapSlot.AddChild(Minimap);
        if (_smokePath is null) Audio.Announce("welcome", 1000);
    }
}
