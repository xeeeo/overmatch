using Godot;
using Overmatch.Sim.Data;
using System.Text.Json;

namespace Overmatch.Game.ArtReview;

/// <summary>
/// Bounded art-only A/B smoke scene. One viewport, 120 units and 12 buildings;
/// no simulation, UI, selection rings or map generation. Baseline and current
/// share the same roster, resource caching, positions, camera, light and colour.
/// </summary>
public partial class ArtBenchmark : Node3D
{
    private const int WarmupFrames = 60;
    private const int SampleFrames = 180;
    private const int UnitCount = 120;
    private const int BuildingCount = 12;
    private readonly Dictionary<string, PackedScene> _sceneCache = new();
    private readonly List<Node3D> _turrets = new();
    private readonly List<Node3D> _rotors = new();
    private readonly List<double> _processMs = new();
    private readonly List<double> _frameMs = new();
    private readonly List<double> _fpsMonitor = new();
    private readonly List<double> _drawCalls = new();
    private readonly List<double> _primitives = new();
    private readonly SortedDictionary<string, int> _roster = new(StringComparer.Ordinal);
    private string? _baseline;
    private string? _output;
    private string? _capture;
    private int _frame;
    private ulong _previousTicks;
    private ulong _sampleStart;
    private bool _finished;

    public override void _Ready()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith("--benchmark-baseline=")) _baseline = argument[21..];
            if (argument.StartsWith("--benchmark-output=")) _output = argument[19..];
            if (argument.StartsWith("--benchmark-capture=")) _capture = argument[20..];
        }
        GetWindow().Title = $"Overmatch — art benchmark ({(_baseline is null ? "current" : "baseline")})";
        GetWindow().Size = new Vector2I(1600, 1000);
        Engine.MaxFps = 0;
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
        BuildLighting();
        BuildRoster();

        var camera = new Camera3D { Current = true, Fov = 50, Near = .1f, Far = 600 };
        AddChild(camera);
        var target = new Vector3(0, 0, -6);
        camera.Position = target + new Vector3(37, 58, 57);
        camera.LookAt(target);
        GD.Print($"[Art benchmark] mode={(_baseline is null ? "current" : "baseline")}; units={UnitCount}; buildings={BuildingCount}; warmup={WarmupFrames}; samples={SampleFrames}; vsync={DisplayServer.WindowGetVsyncMode()}");
    }

    private void BuildLighting()
    {
        // Same light, shadow distance and environment values as GameRoot.BuildLighting.
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, 35, 0), LightEnergy = 1.3f,
            ShadowEnabled = true, DirectionalShadowMaxDistance = 150,
        });
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(.55f, .65f, .75f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(.7f, .75f, .8f), AmbientLightEnergy = .6f,
                FogEnabled = true, FogLightColor = new Color(.6f, .68f, .75f), FogDensity = .002f,
            },
        });
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(90, 90) }, Position = new Vector3(0, -.025f, -5),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(.20f, .25f, .18f), Roughness = 1 },
        });
    }

    private void BuildRoster()
    {
        var rules = DataLoader.LoadRules();
        var factions = new[] { "coalition", "directorate", "network" };
        var colours = new[] { MatchSettings.Palette[0], MatchSettings.Palette[1], MatchSettings.Palette[3] };
        for (var faction = 0; faction < factions.Length; faction++)
        {
            var prefix = factions[faction] + "/";
            var units = rules.Units.Values.Where(d => d.Model.StartsWith(prefix))
                .OrderBy(d => d.Model, StringComparer.Ordinal).ToArray();
            if (units.Length == 0) throw new InvalidOperationException($"No benchmark units for {prefix}");
            for (var index = 0; index < 40; index++)
            {
                var definition = units[index % units.Length];
                var gridIndex = index * 3 + faction;
                var position = new Vector3((gridIndex % 12 - 5.5f) * 4.8f,
                    definition.IsAir ? 2.0f : 0, (gridIndex / 12 - 4.5f) * 4.3f);
                AddModel(definition.Model, colours[faction], position, gridIndex % 2 == 0 ? .18f : -.18f);
            }
            // Command, production, defensive and technology silhouettes from each faction.
            var buildings = factions[faction] switch
            {
                "coalition" => new[] { "command_post", "motor_pool", "sentry_battery", "orbital_uplink" },
                "directorate" => new[] { "command_bunker", "vehicle_plant", "flak_tower", "missile_silo" },
                _ => new[] { "command_cell", "arms_dealer", "stinger_site", "launch_site" },
            };
            for (var index = 0; index < buildings.Length; index++)
            {
                var gridIndex = index * 3 + faction;
                AddModel(prefix + buildings[index], colours[faction],
                    new Vector3((gridIndex % 6 - 2.5f) * 10, 0, -27 - gridIndex / 6 * 8), 0);
            }
        }
    }

    private void AddModel(string id, Color colour, Vector3 position, float heading)
    {
        var model = LoadScene(id).Instantiate<Node3D>();
        FreezeBodies(model);
        Recolour(model, colour);
        AddChild(model);
        model.Position = position;
        model.Rotation = new Vector3(0, heading, 0);
        CollectArticulation(model);
        _roster[id] = _roster.GetValueOrDefault(id) + 1;
    }

    private static void FreezeBodies(Node node)
    {
        // Some historical glTF root names trigger Godot's VehicleBody import
        // hint. This is a geometry benchmark: neither roster may fall or move.
        if (node is RigidBody3D body) body.Freeze = true;
        foreach (var child in node.GetChildren()) FreezeBodies(child);
    }

    private PackedScene LoadScene(string id)
    {
        if (_sceneCache.TryGetValue(id, out var existing)) return existing;
        PackedScene scene;
        if (_baseline is null)
        {
            scene = ResourceLoader.Load<PackedScene>($"res://assets/models/{id}.glb")
                ?? throw new InvalidOperationException($"Missing production GLB: {id}");
        }
        else
        {
            var document = new GltfDocument();
            var state = new GltfState();
            var result = document.AppendFromFile(_baseline.PathJoin(id + ".glb"), state);
            if (result != Error.Ok) throw new InvalidOperationException($"Cannot load baseline {id}: {result}");
            var root = document.GenerateScene(state);
            void OwnChildren(Node node)
            {
                foreach (var child in node.GetChildren()) { child.Owner = root; OwnChildren(child); }
            }
            OwnChildren(root);
            scene = new PackedScene();
            result = scene.Pack(root);
            root.Free();
            document.Dispose();
            state.Dispose();
            if (result != Error.Ok) throw new InvalidOperationException($"Cannot cache baseline {id}: {result}");
        }
        _sceneCache.Add(id, scene);
        return scene;
    }

    private static void Recolour(Node node, Color colour)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is { } geometry)
            for (var index = 0; index < geometry.GetSurfaceCount(); index++)
                if (mesh.GetActiveMaterial(index) is BaseMaterial3D { ResourceName: "TeamColour" } original)
                {
                    var copy = (BaseMaterial3D)original.Duplicate();
                    copy.AlbedoColor = colour;
                    mesh.SetSurfaceOverrideMaterial(index, copy);
                }
        foreach (var child in node.GetChildren()) Recolour(child, colour);
    }

    private void CollectArticulation(Node node)
    {
        if (node is Node3D spatial)
        {
            if (node.Name == "Turret") _turrets.Add(spatial);
            if (node.Name.ToString().StartsWith("Rotor")) _rotors.Add(spatial);
        }
        foreach (var child in node.GetChildren()) CollectArticulation(child);
    }

    public override void _Process(double delta)
    {
        if (_finished) return;
        _frame++;
        for (var index = 0; index < _turrets.Count; index++)
            _turrets[index].Rotation = new Vector3(0, _frame * .012f + index * .17f, 0);
        foreach (var rotor in _rotors) rotor.Rotation = new Vector3(0, _frame * .6f, 0);

        var ticks = Time.GetTicksUsec();
        if (_frame <= WarmupFrames)
        {
            _previousTicks = ticks;
            _sampleStart = ticks;
            return;
        }
        _frameMs.Add((ticks - _previousTicks) / 1000.0);
        _previousTicks = ticks;
        _processMs.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000);
        _fpsMonitor.Add(Performance.GetMonitor(Performance.Monitor.TimeFps));
        _drawCalls.Add(Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame));
        _primitives.Add(Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame));
        if (_processMs.Count == SampleFrames) Finish(ticks);
    }

    private void Finish(ulong ticks)
    {
        _finished = true;
        var wallSeconds = (ticks - _sampleStart) / 1_000_000.0;
        var size = GetViewport().GetVisibleRect().Size;
        var report = new
        {
            benchmark = "art-roster-smoke-v1",
            mode = _baseline is null ? "current" : "baseline",
            baseline_directory = _baseline,
            resolution = new[] { (int)size.X, (int)size.Y },
            renderer = ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
            vsync = DisplayServer.WindowGetVsyncMode().ToString(),
            max_fps = Engine.MaxFps,
            msaa = GetViewport().Msaa3D.ToString(),
            warmup_frames = WarmupFrames,
            sample_frames = _processMs.Count,
            units = UnitCount,
            buildings = BuildingCount,
            unique_models = _sceneCache.Count,
            turrets = _turrets.Count,
            rotors = _rotors.Count,
            wall_seconds = Math.Round(wallSeconds, 4),
            wall_fps = Math.Round(SampleFrames / wallSeconds, 2),
            frame_wall_ms = Statistics(_frameMs),
            process_ms = Statistics(_processMs),
            process_monitor_distinct_values = _processMs.Distinct().Count(),
            fps_monitor = Statistics(_fpsMonitor),
            fps_monitor_distinct_values = _fpsMonitor.Distinct().Count(),
            draw_calls = Statistics(_drawCalls),
            rendered_primitives = Statistics(_primitives),
            roster = _roster,
            scope = "One bounded native art-only run, not GPU profiling or full-match performance. Wall timing is primary; engine monitors can update slower than sampling.",
        };
        var json = JsonSerializer.Serialize(report);
        if (_output is not null)
        {
            var path = System.IO.Path.GetFullPath(_output);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
        var exitCode = 0;
        if (_capture is not null)
        {
            var path = System.IO.Path.GetFullPath(_capture);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var result = GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"[Art benchmark] capture={path}; result={result}");
            if (result != Error.Ok) exitCode = 1;
        }
        GD.Print("ART_BENCHMARK_JSON=" + json);
        GetTree().Quit(exitCode);
    }

    private static object Statistics(List<double> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        return new
        {
            mean = Math.Round(values.Average(), 3),
            median = Math.Round(sorted[sorted.Length / 2], 3),
            p95 = Math.Round(sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], 3),
            min = Math.Round(sorted[0], 3),
            max = Math.Round(sorted[^1], 3),
        };
    }
}
