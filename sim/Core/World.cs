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
    public IReadOnlyList<Player> Players => _players;
    public int PlayerCount => _players.Count;
    public int Tick { get; private set; }
    public float Time => Tick * Dt;
    /// <summary>-1 while the match runs; the winning player id once it ends (-2 for a draw).</summary>
    public int Winner { get; private set; } = -1;
    public bool Finished => Winner != -1;

    private readonly List<Player> _players = new();
    private readonly List<Entity> _entities = new();
    private readonly Dictionary<int, Entity> _byId = new();
    private readonly List<Projectile> _projectiles = new();
    private readonly List<SupplyPile> _piles = new();
    private readonly Queue<Command> _pending = new();
    private readonly List<GameEvent> _events = new();
    private readonly List<AiController> _ais = new();
    internal readonly List<Hazard> HazardList = new();
    internal readonly List<Strike> StrikeList = new();
    internal readonly List<Crate> CrateList = new();
    internal readonly List<Reveal> RevealList = new();
    public IReadOnlyList<Hazard> Hazards => HazardList;
    public IReadOnlyList<Crate> Crates => CrateList;
    public IReadOnlyList<Reveal> Reveals => RevealList;
    internal readonly List<(string defId, int owner, int cx, int cy)> PendingHoles = new();
    /// <summary>Deterministic randomness for scatter and the like.</summary>
    public Random Rng { get; }
    public int Seed { get; }
    /// <summary>Building entity id occupying each cell, 0 for none.</summary>
    private readonly int[] _cellBuilding;
    private int _nextId = 1;

    public IReadOnlyList<Entity> Entities => _entities;
    public IReadOnlyList<Projectile> Projectiles => _projectiles;
    public IReadOnlyList<SupplyPile> Piles => _piles;
    /// <summary>Events raised during the most recent Step.</summary>
    public IReadOnlyList<GameEvent> Events => _events;
    public SpatialHash Spatial { get; } = new();

    public World(GameRules rules, MapDef map, IEnumerable<string> playerFactions, int seed = 0)
    {
        Rules = rules;
        MapDef = map;
        Grid = MapGrid.FromDef(map);
        _cellBuilding = new int[Grid.Width * Grid.Height];
        foreach (var f in playerFactions)
        {
            var def = rules.Factions.TryGetValue(f, out var fd) ? fd : new FactionDef { Id = f, Name = f };
            _players.Add(new Player(_players.Count, def));
        }
        Vision = new VisionMap(Grid.Width, Grid.Height, _players.Count);
        Fields = new FlowFieldCache(Grid);
        Rng = new Random(map.Width * 7919 + map.Height * 31 + _players.Count + seed * 104729);
        Seed = seed;
        foreach (var s in map.Supplies)
            _piles.Add(new SupplyPile { Id = _nextId++, Pos = new Vec2(s.X, s.Y), Initial = s.Amount, Remaining = s.Amount });
        foreach (var n in map.Neutrals)
            if (rules.Buildings.ContainsKey(n.Id)) PlaceBuilding(n.Id, -1, n.X, n.Y);
    }

    /// <summary>HP multiplier from a player's upgrades; neutral objects get none.</summary>
    public float HpMultFor(int owner, ObjectDef def) => owner >= 0 && owner < _players.Count ? _players[owner].HpMult(def) : 1f;

    /// <summary>Can this player currently see the entity? Fog, stealth and detection.</summary>
    public bool CanSee(int player, Entity e)
    {
        if (e.Owner == player) return true;
        if (e.IsInside) return false;
        if (!Vision.IsVisible(player, e.Pos)) return false;
        var stealthy = e.Def.Stealth || (e.Owner >= 0 && Player(e.Owner).StealthFor(e.Def));
        if (!stealthy || e.Has("revealed")) return true;
        foreach (var d in Spatial.Query(e.Pos, 14f))
            if (d.Owner == player && d.Alive && d.Def.Detector > 0f && (d.Pos - e.Pos).Length <= d.Def.Detector) return true;
        return false;
    }

    public World(GameRules rules, MapDef map, int playerCount = 2)
        : this(rules, map, Enumerable.Repeat(rules.Factions.Keys.FirstOrDefault() ?? "none", playerCount))
    {
    }

    /// <summary>Convenience for tests: an open map of the given size.</summary>
    public World(GameRules rules, int width = 64, int height = 64, int playerCount = 2)
        : this(rules, new MapDef { Id = "blank", Width = width, Height = height }, playerCount)
    {
    }

    public Player Player(int id) => _players[id];
    public IReadOnlyList<AiController> Ais => _ais;

    /// <summary>Hand a player to the computer.</summary>
    public AiController AddAi(int player, AiProfile profile)
    {
        var p = Player(player);
        p.IsAi = true;
        p.IncomeMult = profile.IncomeMult;
        var ai = new AiController(this, player, profile);
        _ais.Add(ai);
        return ai;
    }
    public Entity? Get(int id) => _byId.TryGetValue(id, out var e) && e.Alive ? e : null;
    public SupplyPile? Pile(int id) => _piles.FirstOrDefault(p => p.Id == id);

    public Entity Spawn(string unitId, int owner, Vec2 pos, float facing = 0f)
    {
        var def = Rules.Unit(unitId);
        var e = Create(def, owner, ClampToMap(pos), facing);
        e.MaxHp = def.Hp * HpMultFor(owner, def);
        e.Hp = e.MaxHp;
        e.Ammo = def.Ammo;
        e.LifetimeLeft = def.Lifetime;
        _events.Add(new SpawnedEvent(e.Id));
        return e;
    }

    private Entity Create(ObjectDef def, int owner, Vec2 pos, float facing)
    {
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
            Cooldowns = new float[def.Weapons.Count],
            AbilityCooldowns = new float[def.Abilities.Count],
        };
        _entities.Add(e);
        _byId[e.Id] = e;
        return e;
    }

    // ---------------------------------------------------------------- buildings

    public int BuildingAt(int x, int y) => Grid.InBounds(x, y) ? _cellBuilding[y * Grid.Width + x] : 0;

    /// <summary>Can this building go here? Cells must be buildable terrain, in bounds, and not under another building.</summary>
    public bool CanPlace(BuildingDef def, int cx, int cy, out string reason)
    {
        for (var y = cy; y < cy + def.Height; y++)
            for (var x = cx; x < cx + def.Width; x++)
            {
                if (!Grid.InBounds(x, y)) { reason = "off map"; return false; }
                var t = Grid.Get(x, y);
                if (t is CellType.Water or CellType.Cliff) { reason = "terrain"; return false; }
                if (_cellBuilding[y * Grid.Width + x] != 0) { reason = "occupied"; return false; }
            }
        // Keep supply piles reachable.
        foreach (var pile in _piles)
        {
            if (pile.Depleted) continue;
            if (pile.Pos.X > cx - 2f && pile.Pos.X < cx + def.Width + 2f && pile.Pos.Y > cy - 2f && pile.Pos.Y < cy + def.Height + 2f)
            { reason = "too close to supplies"; return false; }
        }
        reason = "";
        return true;
    }

    /// <summary>Does the player own a finished building satisfying every prereq?</summary>
    public bool HasPrereqs(int player, IEnumerable<string> prereqs, out string missing)
    {
        foreach (var p in prereqs)
        {
            var ok = false;
            foreach (var e in _entities)
            {
                if (e.Owner != player || !e.Operational || e.Building is not { } b) continue;
                if (b.Id == p || b.Provides.Contains(p)) { ok = true; break; }
            }
            if (!ok) { missing = p; return false; }
        }
        missing = "";
        return true;
    }

    /// <summary>Place a building instantly (complete). Used for start positions and tests.</summary>
    public Entity PlaceBuilding(string buildingId, int owner, int cx, int cy, bool complete = true)
    {
        var def = Rules.Building(buildingId);
        var e = Create(def, owner, new Vec2(cx + def.Width * 0.5f, cy + def.Height * 0.5f), 0f);
        e.CellX = cx;
        e.CellY = cy;
        e.MaxHp = def.Hp * HpMultFor(owner, def);
        if (complete)
        {
            e.Hp = e.MaxHp;
        }
        else
        {
            e.UnderConstruction = true;
            e.Hp = e.MaxHp * 0.1f;
        }
        if (def.Produces.Count > 0 || def.Upgrades.Count > 0) e.Queue = new ProductionQueue();
        if (def.Trickle is { } t) e.TrickleTimer = t.Interval;
        if (def.Spawner is { } sp) e.SpawnTimer = sp.Interval;
        for (var y = cy; y < cy + def.Height; y++)
            for (var x = cx; x < cx + def.Width; x++)
            {
                _cellBuilding[y * Grid.Width + x] = e.Id;
                Grid.Set(x, y, CellType.Structure);
            }
        Economy.RecomputePower(this);
        _events.Add(new SpawnedEvent(e.Id));
        if (complete) _events.Add(new ConstructionCompletedEvent(e.Id, owner));
        return e;
    }

    internal void FreeFootprint(Entity building)
    {
        var b = building.Building!;
        for (var y = building.CellY; y < building.CellY + b.Height; y++)
            for (var x = building.CellX; x < building.CellX + b.Width; x++)
            {
                if (_cellBuilding[y * Grid.Width + x] != building.Id) continue;
                _cellBuilding[y * Grid.Width + x] = 0;
                Grid.Set(x, y, CellType.Ground);
            }
    }

    /// <summary>Closest point on the (slightly expanded) footprint to p: where a unit stands to work on it.</summary>
    public static Vec2 ApproachPoint(Entity building, Vec2 from, float margin)
    {
        var (x0, y0, x1, y1) = building.Bounds;
        var x = Math.Clamp(from.X, x0 - margin, x1 + margin);
        var y = Math.Clamp(from.Y, y0 - margin, y1 + margin);
        // Push onto the expanded boundary if we clamped to the inside.
        if (x > x0 && x < x1 && y > y0 && y < y1)
        {
            var dl = x - (x0 - margin); var dr = (x1 + margin) - x; var dd = y - (y0 - margin); var du = (y1 + margin) - y;
            var m = MathF.Min(MathF.Min(dl, dr), MathF.Min(dd, du));
            if (m == dl) x = x0 - margin; else if (m == dr) x = x1 + margin; else if (m == dd) y = y0 - margin; else y = y1 + margin;
        }
        return new Vec2(x, y);
    }

    public static float DistanceToBounds(Entity building, Vec2 p)
    {
        var (x0, y0, x1, y1) = building.Bounds;
        var dx = MathF.Max(MathF.Max(x0 - p.X, 0f), p.X - x1);
        var dy = MathF.Max(MathF.Max(y0 - p.Y, 0f), p.Y - y1);
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Where a produced unit appears: just outside the south edge, nearest free cell.</summary>
    public Vec2 ExitPoint(Entity building, Locomotor loco)
    {
        var (x0, y0, x1, _) = building.Bounds;
        var want = ClampToMap(new Vec2((x0 + x1) * 0.5f, y0 - 1.0f));
        if (loco == Locomotor.Air) return want;
        var (cx, cy) = MapGrid.CellOf(want);
        var free = Grid.NearestPassable(cx, cy, loco, 8);
        return free is null ? want : MapGrid.Centre(free.Value.x, free.Value.y);
    }

    // ---------------------------------------------------------------- stepping

    public void Submit(Command command) => _pending.Enqueue(command);

    internal void Emit(GameEvent ev) => _events.Add(ev);
    internal int NextId() => _nextId++;
    internal void AddProjectile(Projectile p) => _projectiles.Add(p);
    internal Entity SpawnProduced(UnitDef def, int owner, Vec2 pos, float facing) => Spawn(def.Id, owner, pos, facing);

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

        if (!Finished)
            foreach (var ai in _ais) ai.Think();

        while (_pending.Count > 0) Apply(_pending.Dequeue());

        Spatial.Rebuild(_entities);
        if (Tick % VisionMap.UpdateInterval == 1) Vision.Recompute(_entities, RevealList);

        Effects.Update(this);
        Economy.Update(this);
        Construction.Update(this);
        Production.Update(this);
        Garrison.Update(this);
        Powers.UpdateSuperweapons(this);
        Combat.Update(this);

        foreach (var e in _entities)
        {
            if (!e.Alive || e.IsBuilding || e.IsInside) continue;
            if (e.FollowId != 0) Movement.Follow(e, this);
            Movement.Update(e, this);
        }
        Movement.Separate(this);

        Combat.UpdateProjectiles(this);

        // Remove the dead.
        var removed = false;
        for (var i = _entities.Count - 1; i >= 0; i--)
        {
            var e = _entities[i];
            if (e.Alive) continue;
            if (e.IsBuilding) { FreeFootprint(e); removed = true; }
            if (e.IsInside && Get(e.InsideId) is { } c) { c.Passengers.Remove(e.Id); if (e.Owner >= 0) Player(e.Owner).TunnelPool.Remove(e.Id); }
            _byId.Remove(e.Id);
            _entities.RemoveAt(i);
        }
        if (removed) Economy.RecomputePower(this);
        foreach (var (defId, owner, cx, cy) in PendingHoles)
        {
            var def = Rules.Building(defId);
            var holeDef = Rules.Building("hole");
            var hx = cx + (def.Width - holeDef.Width) / 2;
            var hy = cy + (def.Height - holeDef.Height) / 2;
            if (!CanPlace(holeDef, hx, hy, out _)) continue;
            var hole = PlaceBuilding("hole", owner, hx, hy, complete: true);
            hole.HoleDefId = defId;
            hole.HoleCellX = cx;
            hole.HoleCellY = cy;
            _events.Add(new HoleEvent(hole.Id, defId, false));
        }
        PendingHoles.Clear();
        // Discounts expire.
        foreach (var p in _players) if (p.DiscountUntil > 0f && Time >= p.DiscountUntil) { p.DiscountMult = 1f; p.DiscountUntil = 0f; }
        _projectiles.RemoveAll(p => !p.Alive);

        if (Tick % TicksPerSecond == 0) CheckElimination();
    }

    /// <summary>A player is out when they have no buildings and no builders. Last one standing wins.</summary>
    private void CheckElimination()
    {
        if (Finished) return;
        foreach (var p in _players)
        {
            if (p.Eliminated) continue;
            var alive = false;
            foreach (var e in _entities)
                if (e.Alive && e.Owner == p.Id && ((e.IsBuilding && !e.Building!.IsHole) || e.IsBuilder)) { alive = true; break; }
            if (alive) continue;
            p.Eliminated = true;
            _events.Add(new PlayerEliminatedEvent(p.Id));
        }
        var remaining = _players.Where(p => !p.Eliminated).ToList();
        if (remaining.Count == 1 && _players.Count > 1) { Winner = remaining[0].Id; _events.Add(new MatchEndedEvent(Winner)); }
        else if (remaining.Count == 0) { Winner = -2; _events.Add(new MatchEndedEvent(-2)); }
    }

    private void Apply(Command command)
    {
        switch (command)
        {
            case MoveCommand m:
                IssueMove(OwnedUnits(m.Player, m.Units), m.Target, MoveKind.Move);
                break;
            case AttackMoveCommand am:
                IssueMove(OwnedUnits(am.Player, am.Units), am.Target, MoveKind.AttackMove);
                break;
            case AttackCommand a:
            {
                var target = Get(a.TargetId);
                foreach (var u in OwnedUnits(a.Player, a.Units))
                {
                    if (target is null || target.Owner == u.Owner || !u.HasWeapons) continue;
                    ClearJobs(u);
                    u.TargetId = target.Id;
                    u.ExplicitTarget = true;
                    u.Move = null;
                    u.SuspendedMove = null;
                }
                break;
            }
            case StopCommand s:
                foreach (var u in OwnedUnits(s.Player, s.Units))
                {
                    ClearJobs(u);
                    u.Move = null;
                    u.SuspendedMove = null;
                    u.TargetId = 0;
                    u.ExplicitTarget = false;
                }
                break;
            case BuildCommand b:
                Construction.Begin(this, b);
                break;
            case AssistBuildCommand ab:
            {
                var site = Get(ab.BuildingId);
                if (site is null || site.Owner != ab.Player || !site.UnderConstruction) break;
                foreach (var u in OwnedUnits(ab.Player, ab.Units))
                    if (u.IsBuilder) { ClearJobs(u); u.BuildTargetId = site.Id; u.Move = null; }
                break;
            }
            case RepairCommand rp:
            {
                var target = Get(rp.BuildingId);
                if (target is null || target.Owner != rp.Player || !target.IsBuilding || target.UnderConstruction || target.Hp >= target.MaxHp) break;
                foreach (var u in OwnedUnits(rp.Player, rp.Units))
                    if (u.IsBuilder) { ClearJobs(u); u.RepairTargetId = target.Id; u.Move = null; }
                break;
            }
            case ReturnToBaseCommand rtb:
            {
                var any = false;
                foreach (var u in OwnedUnits(rtb.Player, rtb.Units))
                {
                    if (u.Unit is not { IsAir: true } || u.Def.Tags.Contains("drone")) continue;
                    if (Effects.NearestPad(this, u) is null) { if (!any) Emit(new OrderRejectedEvent(rtb.Player, "no_airfield")); any = true; continue; }
                    ClearJobs(u); u.TargetId = 0; u.Move = null;
                    u.ReturningToBase = true;
                    if (u.Unit.Ammo > 0 && u.Ammo < u.Unit.Ammo) { u.Rearming = true; u.RearmTimer = u.Unit.RearmTime; }
                }
                break;
            }
            case ProduceCommand p:
                Production.Enqueue(this, p);
                break;
            case CancelProduceCommand c:
                Production.Cancel(this, c);
                break;
            case RallyCommand r:
            {
                var b = Get(r.BuildingId);
                if (b is not null && b.Owner == r.Player && b.IsBuilding) b.Rally = ClampToMap(r.Target);
                break;
            }
            case SellCommand s:
                Economy.Sell(this, s);
                break;
            case AbilityCommand ab:
                Powers.UseAbility(this, ab);
                break;
            case GarrisonCommand g:
            {
                var c = Get(g.ContainerId);
                if (c is null) break;
                foreach (var u in OwnedUnits(g.Player, g.Units))
                {
                    if (!Garrison.CanEnter(u, c)) continue;
                    ClearJobs(u);
                    u.TargetId = 0;
                    u.EnterTargetId = c.Id;
                    u.Move = null;
                }
                break;
            }
            case UngarrisonCommand ug:
            {
                var c = Get(ug.ContainerId);
                if (c is not null && (c.Owner == ug.Player || c.Owner < 0)) Garrison.Exit(this, c, ug.Player);
                break;
            }
            case CaptureCommand cap:
            {
                var b = Get(cap.BuildingId);
                if (b is null || b.Building is not { Capturable: true } || b.Owner == cap.Player) break;
                foreach (var u in OwnedUnits(cap.Player, cap.Units))
                {
                    if (u.Unit is not { CanCapture: true }) continue;
                    ClearJobs(u);
                    u.TargetId = 0;
                    u.CaptureTargetId = b.Id;
                    u.CaptureProgress = 0f;
                    u.Move = null;
                }
                break;
            }
            case BuyPowerCommand bp:
                Powers.Buy(this, bp);
                break;
            case UsePowerCommand up:
                Powers.Use(this, up);
                break;
            case FireSuperweaponCommand fs:
                Powers.Fire(this, fs);
                break;
            case HarvestCommand h:
                foreach (var u in OwnedUnits(h.Player, h.Units))
                {
                    if (!u.IsHarvester) continue;
                    var pile = h.PileId != 0 ? Pile(h.PileId) : Economy.NearestPile(this, u.Pos, float.MaxValue);
                    if (pile is null || pile.Depleted) continue;
                    ClearJobs(u);
                    u.Move = null;
                    u.PileId = pile.Id;
                    u.HarvestState = HarvestState.ToPile;
                }
                break;
        }
    }

    private static void ClearJobs(Entity u)
    {
        u.BuildTargetId = 0;
        u.RepairTargetId = 0;
        u.ReturningToBase = false;
        u.EnterTargetId = 0;
        u.CaptureTargetId = 0;
        u.CaptureProgress = 0f;
        if (u.HarvestState != HarvestState.Idle && u.HarvestState != HarvestState.Loading && u.HarvestState != HarvestState.Unloading)
            u.HarvestState = HarvestState.Idle;
    }

    private void IssueMove(List<Entity> units, Vec2 target, MoveKind kind)
    {
        if (units.Count == 0) return;
        var targets = Formation.Spread(units, target);
        for (var i = 0; i < units.Count; i++)
        {
            var u = units[i];
            ClearJobs(u);
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

    private List<Entity> OwnedUnits(int player, int[] ids)
    {
        var list = new List<Entity>(ids.Length);
        foreach (var id in ids)
            if (_byId.TryGetValue(id, out var e) && e.Alive && e.Owner == player && !e.IsBuilding && !e.IsInside) list.Add(e);
        return list;
    }

    public bool AreEnemies(Entity a, Entity b) => a.Owner != b.Owner && a.Owner >= 0 && b.Owner >= 0;

    internal void Reject(int player, string reason) => _events.Add(new OrderRejectedEvent(player, reason));
}
