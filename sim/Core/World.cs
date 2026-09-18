using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>The whole simulation. Deterministic fixed-tick; no engine types anywhere.</summary>
public sealed class World
{
    public const int TicksPerSecond = 20;
    public const float Dt = 1f / TicksPerSecond;

    public GameRules Rules { get; }
    public MapDef MapDef { get; }
    public MapGrid Grid { get; }
    public VisionMap Vision { get; }
    public FlowFieldCache Fields { get; }
    public int PlayerCount { get; }
    public int Tick { get; private set; }
    public float Time => Tick * Dt;

    private readonly List<Entity> _entities = new();
    private readonly Dictionary<int, Entity> _byId = new();
    private readonly List<Projectile> _projectiles = new();
    private readonly Queue<Command> _pending = new();
    private readonly List<GameEvent> _events = new();
    private int _nextId = 1;

    public IReadOnlyList<Entity> Entities => _entities;
    public IReadOnlyList<Projectile> Projectiles => _projectiles;
    /// <summary>Events raised during the most recent Step.</summary>
    public IReadOnlyList<GameEvent> Events => _events;
    public SpatialHash Spatial { get; } = new();

    public World(GameRules rules, MapDef map, int playerCount = 2)
    {
        Rules = rules;
        MapDef = map;
        Grid = MapGrid.FromDef(map);
        PlayerCount = playerCount;
        Vision = new VisionMap(Grid.Width, Grid.Height, playerCount);
        Fields = new FlowFieldCache(Grid);
    }

    /// <summary>Convenience for tests: an open map of the given size.</summary>
    public World(GameRules rules, int width = 64, int height = 64, int playerCount = 2)
        : this(rules, new MapDef { Id = "blank", Width = width, Height = height }, playerCount)
    {
    }

    public Entity? Get(int id) => _byId.TryGetValue(id, out var e) && e.Alive ? e : null;

    public Entity Spawn(string unitId, int owner, Vec2 pos, float facing = 0f)
    {
        var def = Rules.Unit(unitId);
        var e = new Entity
        {
            Id = _nextId++,
            Owner = owner,
            Def = def,
            Pos = pos,
            PrevPos = pos,
            Facing = facing,
            PrevFacing = facing,
            TurretFacing = facing,
            PrevTurretFacing = facing,
            Hp = def.Hp,
            Cooldowns = new float[def.Weapons.Count],
        };
        _entities.Add(e);
        _byId[e.Id] = e;
        _events.Add(new SpawnedEvent(e.Id));
        return e;
    }

    public void Submit(Command command) => _pending.Enqueue(command);

    internal void Emit(GameEvent ev) => _events.Add(ev);
    internal int NextId() => _nextId++;
    internal void AddProjectile(Projectile p) => _projectiles.Add(p);

    /// <summary>Advance the world by one tick.</summary>
    public void Step()
    {
        Tick++;
        _events.Clear();

        foreach (var e in _entities)
        {
            e.PrevPos = e.Pos;
            e.PrevFacing = e.Facing;
            e.PrevTurretFacing = e.TurretFacing;
        }
        foreach (var p in _projectiles) p.PrevPos = p.Pos;

        while (_pending.Count > 0) Apply(_pending.Dequeue());

        Spatial.Rebuild(_entities);
        if (Tick % VisionMap.UpdateInterval == 1) Vision.Recompute(_entities);

        Combat.Update(this);

        foreach (var e in _entities)
        {
            if (!e.Alive) continue;
            Movement.Update(e, this);
        }
        Movement.Separate(this);

        Combat.UpdateProjectiles(this);

        // Remove the dead.
        for (var i = _entities.Count - 1; i >= 0; i--)
        {
            if (_entities[i].Alive) continue;
            _byId.Remove(_entities[i].Id);
            _entities.RemoveAt(i);
        }
        _projectiles.RemoveAll(p => !p.Alive);
    }

    private void Apply(Command command)
    {
        switch (command)
        {
            case MoveCommand m:
                IssueMove(OwnedAlive(m.Player, m.Units), m.Target, MoveKind.Move);
                break;
            case AttackMoveCommand am:
                IssueMove(OwnedAlive(am.Player, am.Units), am.Target, MoveKind.AttackMove);
                break;
            case AttackCommand a:
            {
                var target = Get(a.TargetId);
                foreach (var u in OwnedAlive(a.Player, a.Units))
                {
                    if (target is null || target.Owner == u.Owner || !u.HasWeapons) continue;
                    u.TargetId = target.Id;
                    u.ExplicitTarget = true;
                    u.Move = null;
                    u.SuspendedMove = null;
                }
                break;
            }
            case StopCommand s:
                foreach (var u in OwnedAlive(s.Player, s.Units))
                {
                    u.Move = null;
                    u.SuspendedMove = null;
                    u.TargetId = 0;
                    u.ExplicitTarget = false;
                }
                break;
        }
    }

    private void IssueMove(List<Entity> units, Vec2 target, MoveKind kind)
    {
        if (units.Count == 0) return;
        var targets = Formation.Spread(units, target);
        for (var i = 0; i < units.Count; i++)
        {
            var u = units[i];
            u.Move = new MoveOrder { Target = ClampToMap(targets[i]), Kind = kind };
            u.SuspendedMove = null;
            if (kind == MoveKind.Move)
            {
                u.TargetId = 0;
                u.ExplicitTarget = false;
            }
        }
    }

    public Vec2 ClampToMap(Vec2 p) => new(
        Math.Clamp(p.X, 0.5f, Grid.Width - 0.5f),
        Math.Clamp(p.Y, 0.5f, Grid.Height - 0.5f));

    private List<Entity> OwnedAlive(int player, int[] ids)
    {
        var list = new List<Entity>(ids.Length);
        foreach (var id in ids)
            if (_byId.TryGetValue(id, out var e) && e.Alive && e.Owner == player) list.Add(e);
        return list;
    }

    public bool AreEnemies(Entity a, Entity b) => a.Owner != b.Owner;
}
