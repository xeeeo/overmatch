using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Cheap effects: flashes, sparks, smoke, tracers and wrecks. Everything is primitives; no textures.</summary>
public partial class Vfx : Node3D
{
    private sealed class Flash
    {
        public MeshInstance3D Node = null!;
        public StandardMaterial3D Mat = null!;
        public float Life, MaxLife, StartSize, EndSize;
        public Color Colour;
    }

    private readonly List<Flash> _flashes = new();
    private readonly Dictionary<int, MeshInstance3D> _projectiles = new();
    private readonly Dictionary<int, double> _wrecks = new();
    private readonly Dictionary<int, Node3D> _wreckNodes = new();
    private readonly SphereMesh _sphere = new() { Radius = 0.5f, Height = 1f, RadialSegments = 10, Rings = 5 };
    private readonly BoxMesh _tracer = new() { Size = new Vector3(0.6f, 0.08f, 0.08f) };
    private readonly StandardMaterial3D _tracerMat = new()
    {
        AlbedoColor = new Color(1f, 0.85f, 0.4f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
    };

    public void Consume(World world, Func<int, Color> team)
    {
        foreach (var ev in world.Events)
        {
            switch (ev)
            {
                case WeaponFiredEvent f:
                    if (world.Get(f.SourceId) is { } src)
                        Spawn(MapView.ToWorld(src.Pos, 1.25f), new Color(1f, 0.8f, 0.4f), 0.35f, 0.9f, 0.08f);
                    if (f.ProjectileId == 0) Beam(MapView.ToWorld(f.From, 1.25f), MapView.ToWorld(f.To, 0.9f));
                    break;
                case HitEvent h:
                    Spawn(MapView.ToWorld(h.Pos, 0.8f), new Color(1f, 0.6f, 0.2f), 0.5f, 1.4f, 0.18f);
                    Sparks(MapView.ToWorld(h.Pos, 0.8f), 5, new Color(1f, 0.7f, 0.3f), 3f, 0.5f);
                    break;
                case StrikeImpactEvent si:
                {
                    var at = MapView.ToWorld(si.Pos, 0.5f);
                    var big = si.Radius >= 6f;
                    Spawn(at, new Color(1f, 0.9f, 0.6f), si.Radius * 0.3f, si.Radius * 1.6f, big ? 0.6f : 0.3f);
                    Spawn(at + Vector3.Up * si.Radius * 0.4f, new Color(0.3f, 0.27f, 0.24f, 0.7f), si.Radius * 0.5f, si.Radius * 1.3f, big ? 3.5f : 1.2f);
                    Sparks(at, big ? 40 : 10, new Color(1f, 0.6f, 0.2f), si.Radius * 1.5f, big ? 1.8f : 0.8f);
                    if (big) Sparks(at, 30, new Color(0.25f, 0.22f, 0.2f), si.Radius, 3f);
                    break;
                }
                case SuperweaponFiredEvent sf:
                    Spawn(MapView.ToWorld(sf.Target, 0.2f), new Color(1f, 0.3f, 0.2f, 0.6f), 1f, 6f, sf.Delay);
                    break;
                case AbilityUsedEvent au:
                    Spawn(MapView.ToWorld(au.Target, 0.6f), new Color(0.5f, 0.8f, 1f), 0.4f, 2.5f, 0.35f);
                    break;
                case DiedEvent d:
                {
                    var at = MapView.ToWorld(d.Pos, 0.6f);
                    Spawn(at, new Color(1f, 0.5f, 0.15f), 1.0f, 3.2f, 0.3f);
                    Spawn(at + Vector3.Up * 0.8f, new Color(0.25f, 0.23f, 0.21f, 0.6f), 0.8f, 2.6f, 1.1f);
                    Sparks(at, 14, new Color(0.25f, 0.22f, 0.2f), 5f, 1.6f);
                    Sparks(at, 8, new Color(1f, 0.6f, 0.2f), 7f, 0.6f);
                    if (d.WasBuilding)
                    {
                        Spawn(at, new Color(1f, 0.55f, 0.2f), 2f, 7f, 0.5f);
                        Sparks(at, 24, new Color(0.3f, 0.28f, 0.26f), 8f, 2.2f);
                        if (world.Rules.Buildings.TryGetValue(d.DefId, out var bdef)) Rubble(d, bdef.Width, bdef.Height);
                    }
                    else if (world.Rules.Units.TryGetValue(d.DefId, out var def) && def.Tags.Contains("vehicle"))
                        Wreck(d, def.Model, team(d.Owner));
                    break;
                }
            }
        }
    }

    private void Wreck(DiedEvent d, string model, Color team)
    {
        var node = EntityView.LoadModel(model, team);
        EntityView.ApplyWreckLook(node);
        var wreck = new Node3D
        {
            Position = MapView.ToWorld(d.Pos, -0.08f),
            Rotation = new Vector3(Mathf.DegToRad(3f), d.Facing, Mathf.DegToRad(-4f)),
        };
        wreck.AddChild(node);
        if (EntityView.FindNamed(node, "Turret") is { } turret)
            turret.Rotation = new Vector3(Mathf.DegToRad(8f), Angles.Wrap(d.TurretFacing - d.Facing) + 0.3f, 0f);
        AddChild(wreck);
        _wreckNodes[d.EntityId] = wreck;
        _wrecks[d.EntityId] = 30.0;
    }

    private void Rubble(DiedEvent d, int w, int h)
    {
        var rubble = new Node3D { Position = MapView.ToWorld(d.Pos) };
        var mat = new StandardMaterial3D { AlbedoColor = new Color(0.16f, 0.15f, 0.14f), Roughness = 1f };
        var rng = new Random(d.EntityId);
        rubble.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(w - 0.3f, 0.25f, h - 0.3f) }, Position = new Vector3(0, 0.12f, 0), MaterialOverride = mat });
        for (var i = 0; i < w * h / 2; i++)
        {
            var s = 0.4f + (float)rng.NextDouble() * 0.7f;
            rubble.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(s, s * 0.6f, s * 0.8f) },
                Position = new Vector3(((float)rng.NextDouble() - 0.5f) * (w - 1f), 0.25f + s * 0.3f, ((float)rng.NextDouble() - 0.5f) * (h - 1f)),
                RotationDegrees = new Vector3(0, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 15f),
                MaterialOverride = mat,
            });
        }
        AddChild(rubble);
        _wreckNodes[d.EntityId] = rubble;
        _wrecks[d.EntityId] = 45.0;
    }

    private void Beam(Vector3 from, Vector3 to)
    {
        var dir = to - from;
        var len = dir.Length();
        if (len < 0.01f) return;
        var beam = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(len, 0.05f, 0.05f) },
            MaterialOverride = _tracerMat,
            Position = from + dir * 0.5f,
        };
        beam.LookAtFromPosition(beam.Position, to, Vector3.Up);
        beam.RotateObjectLocal(Vector3.Up, Mathf.Pi / 2f);
        AddChild(beam);
        var f = new Flash { Node = beam, Mat = _tracerMat, Life = 0.06f, MaxLife = 0.06f, StartSize = 1f, EndSize = 1f, Colour = _tracerMat.AlbedoColor };
        _flashes.Add(f);
    }

    private void Spawn(Vector3 at, Color colour, float startSize, float endSize, float life)
    {
        var mat = new StandardMaterial3D
        {
            AlbedoColor = colour,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        var node = new MeshInstance3D { Mesh = _sphere, MaterialOverride = mat, Position = at, Scale = Vector3.One * startSize };
        AddChild(node);
        _flashes.Add(new Flash { Node = node, Mat = mat, Life = life, MaxLife = life, StartSize = startSize, EndSize = endSize, Colour = colour });
    }

    private void Sparks(Vector3 at, int amount, Color colour, float speed, float life)
    {
        var p = new CpuParticles3D
        {
            Amount = amount,
            OneShot = true,
            Explosiveness = 1f,
            Lifetime = life,
            Position = at,
            Direction = Vector3.Up,
            Spread = 70f,
            InitialVelocityMin = speed * 0.4f,
            InitialVelocityMax = speed,
            Gravity = new Vector3(0, -6f, 0),
            ScaleAmountMin = 0.12f,
            ScaleAmountMax = 0.3f,
            Mesh = _sphere,
            Emitting = true,
        };
        p.MaterialOverride = new StandardMaterial3D { AlbedoColor = colour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        AddChild(p);
        GetTree().CreateTimer(life + 0.2f).Timeout += p.QueueFree;
    }

    private readonly Dictionary<int, MeshInstance3D> _hazards = new();
    private readonly Dictionary<int, MeshInstance3D> _crates = new();

    /// <summary>Hazard discs and salvage crates mirror the sim lists.</summary>
    public void SyncWorldObjects(World world)
    {
        var seen = new HashSet<int>();
        foreach (var h in world.Hazards)
        {
            seen.Add(h.Id);
            if (!_hazards.TryGetValue(h.Id, out var node))
            {
                var toxin = h.DamageType == "toxin";
                node = new MeshInstance3D
                {
                    Mesh = new CylinderMesh { TopRadius = h.Radius, BottomRadius = h.Radius, Height = 0.08f, RadialSegments = 20 },
                    Position = MapView.ToWorld(h.Pos, 0.08f),
                    MaterialOverride = new StandardMaterial3D
                    {
                        AlbedoColor = toxin ? new Color(0.45f, 0.9f, 0.2f, 0.45f) : new Color(0.9f, 0.85f, 0.2f, 0.4f),
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    },
                };
                AddChild(node);
                _hazards[h.Id] = node;
            }
            var f = h.Duration > 0 ? h.Remaining / h.Duration : 1f;
            ((StandardMaterial3D)node.MaterialOverride).AlbedoColor = new Color(((StandardMaterial3D)node.MaterialOverride).AlbedoColor, 0.15f + 0.35f * f);
        }
        foreach (var (id, node) in _hazards.ToArray()) if (!seen.Contains(id)) { node.QueueFree(); _hazards.Remove(id); }

        seen.Clear();
        foreach (var c in world.Crates)
        {
            seen.Add(c.Id);
            if (_crates.ContainsKey(c.Id)) continue;
            var node = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.7f, 0.5f, 0.7f) },
                Position = MapView.ToWorld(c.Pos, 0.25f),
                RotationDegrees = new Vector3(0, c.Id * 37 % 90, 0),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.55f, 0.45f, 0.2f) },
            };
            AddChild(node);
            _crates[c.Id] = node;
        }
        foreach (var (id, node) in _crates.ToArray()) if (!seen.Contains(id)) { node.QueueFree(); _crates.Remove(id); }
    }

    public void SyncProjectiles(World world, float alpha)
    {
        var seen = new HashSet<int>();
        foreach (var p in world.Projectiles)
        {
            seen.Add(p.Id);
            if (!_projectiles.TryGetValue(p.Id, out var node))
            {
                node = new MeshInstance3D { Mesh = _tracer, MaterialOverride = _tracerMat };
                AddChild(node);
                _projectiles[p.Id] = node;
            }
            var pos = Vec2.Lerp(p.PrevPos, p.Pos, alpha);
            var arc = p.Weapon.Projectile.Arc ? Mathf.Sin(Mathf.Clamp(p.Progress, 0, 1) * Mathf.Pi) * p.TotalDistance * 0.15f : 0f;
            node.Position = MapView.ToWorld(pos, 1.2f + arc);
            var dir = p.Aim - p.Pos;
            if (dir.LengthSq > 1e-4f) node.Rotation = new Vector3(0, dir.Angle, 0);
        }
        foreach (var (id, node) in _projectiles.ToArray())
        {
            if (seen.Contains(id)) continue;
            node.QueueFree();
            _projectiles.Remove(id);
        }
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        for (var i = _flashes.Count - 1; i >= 0; i--)
        {
            var f = _flashes[i];
            f.Life -= dt;
            if (f.Life <= 0f)
            {
                f.Node.QueueFree();
                _flashes.RemoveAt(i);
                continue;
            }
            var t = 1f - f.Life / f.MaxLife;
            if (f.Node.Mesh == _sphere)
            {
                f.Node.Scale = Vector3.One * Mathf.Lerp(f.StartSize, f.EndSize, Mathf.Sqrt(t));
                f.Mat.AlbedoColor = new Color(f.Colour, f.Colour.A * (1f - t));
            }
        }
        foreach (var (id, ttl) in _wrecks.ToArray())
        {
            var left = ttl - delta;
            if (left <= 0)
            {
                _wreckNodes[id].QueueFree();
                _wreckNodes.Remove(id);
                _wrecks.Remove(id);
            }
            else
            {
                _wrecks[id] = left;
                if (left < 3.0) _wreckNodes[id].Position += new Vector3(0, -(float)delta * 0.3f, 0); // sink away
            }
        }
    }
}
