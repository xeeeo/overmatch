using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>The whole simulation. Deterministic fixed-tick; no engine types anywhere.</summary>
public sealed class World
{
    public const int TicksPerSecond = 20;
    public const float Dt = 1f / TicksPerSecond;

    public GameRules Rules { get; }
    public int Tick { get; private set; }
    public float Time => Tick * Dt;

    private readonly List<Entity> _entities = new();
    private readonly Dictionary<int, Entity> _byId = new();
    private readonly Queue<Command> _pending = new();
    private int _nextId = 1;

    public IReadOnlyList<Entity> Entities => _entities;

    public World(GameRules rules)
    {
        Rules = rules;
    }

    public Entity? Get(int id) => _byId.TryGetValue(id, out var e) ? e : null;

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
            Hp = def.Hp,
        };
        _entities.Add(e);
        _byId[e.Id] = e;
        return e;
    }

    public void Submit(Command command) => _pending.Enqueue(command);

    /// <summary>Advance the world by one tick.</summary>
    public void Step()
    {
        Tick++;
        foreach (var e in _entities)
        {
            e.PrevPos = e.Pos;
            e.PrevFacing = e.Facing;
        }

        while (_pending.Count > 0) Apply(_pending.Dequeue());

        foreach (var e in _entities)
        {
            if (!e.Alive) continue;
            Movement.Update(e, Dt);
        }
        Movement.Separate(_entities);
    }

    private void Apply(Command command)
    {
        switch (command)
        {
            case MoveCommand m:
            {
                var units = OwnedAlive(m.Player, m.Units);
                if (units.Count == 0) return;
                var targets = Formation.Spread(units, m.Target);
                for (var i = 0; i < units.Count; i++)
                    units[i].Move = new MoveOrder { Target = targets[i] };
                break;
            }
            case StopCommand s:
                foreach (var u in OwnedAlive(s.Player, s.Units)) u.Move = null;
                break;
        }
    }

    private List<Entity> OwnedAlive(int player, int[] ids)
    {
        var list = new List<Entity>(ids.Length);
        foreach (var id in ids)
            if (_byId.TryGetValue(id, out var e) && e.Alive && e.Owner == player) list.Add(e);
        return list;
    }
}
