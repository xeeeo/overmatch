using Overmatch.Sim.Data;

namespace Overmatch.Sim;

/// <summary>A computer player. Runs inside the sim, issues ordinary commands, and reads the world directly (it does not respect fog for base decisions, like the original's AI).</summary>
public sealed class AiController
{
    private readonly World _w;
    private readonly AiProfile _p;
    public int Player { get; }
    public AiProfile Profile => _p;

    private int _nextThink;
    private readonly Dictionary<string, int> _produced = new();
    private readonly List<int> _wave = new();
    private int _waveStartCount;
    private int _waveTargetId;
    private float _lastWaveTime = -1f;
    private float _lastDefenceTime = -100f;
    private Vec2 _muster;
    private Vec2 _enemyDir = new(1, 0);
    private int _defenceIndex;
    /// <summary>Units given a job by a command submitted this think (commands apply next tick, so the entity does not show it yet).</summary>
    private readonly HashSet<int> _assigned = new();

    public string Status { get; private set; } = "";

    public AiController(World world, int player, AiProfile profile)
    {
        _w = world;
        _p = profile;
        Player = player;
        _nextThink = world.Tick + player * 7 + 5;
    }

    private Player Me => _w.Player(Player);
    private IEnumerable<Entity> Mine => _w.Entities.Where(e => e.Alive && e.Owner == Player);
    private Entity? Hq => Mine.FirstOrDefault(e => e.Building is { Hq: true }) ?? Mine.FirstOrDefault(e => e.IsBuilding);

    public void Think()
    {
        if (_w.Tick < _nextThink || Me.Eliminated) return;
        _nextThink = _w.Tick + Math.Max(1, (int)(_p.ReactionDelay * World.TicksPerSecond));
        var hq = Hq;
        if (hq is null) { Status = "no base"; return; }
        UpdateEnemyDirection(hq);
        _wave.RemoveAll(id => _w.Get(id) is null);
        _assigned.Clear();

        ManageBuilders(hq);
        ManageBase(hq);
        ManageHarvesters();
        ManageProduction();
        ManageUpgrades();
        ManageDefence(hq);
        ManageAttack(hq);
        ManagePowers(hq);
    }

    /// <summary>Buy powers in the faction's order; use targeted powers on the enemy; fire superweapons at the enemy HQ.</summary>
    private void ManagePowers(Entity hq)
    {
        var me = Me;
        if (me.Points > 0)
            foreach (var pid in me.Faction.Powers)
            {
                if (me.HasPower(pid)) continue;
                var def = _w.Rules.Power(pid);
                if (me.Rank < def.Rank) continue;
                _w.Submit(new BuyPowerCommand(Player, pid));
                break;
            }

        Entity? enemyHq = null;
        foreach (var e in _w.Entities)
            if (e.Alive && e.Owner != Player && e.Owner >= 0 && e.Building is { Hq: true } && !_w.Player(e.Owner).Eliminated) { enemyHq = e; break; }
        var target = _wave.Count > 0 && _w.Get(_waveTargetId) is { } wt ? wt : enemyHq ?? NearestEnemyBuilding(hq.Pos);
        if (target is null) return;

        foreach (var pid in me.PowersOwned)
        {
            var def = _w.Rules.Power(pid);
            if (def.Target == "none" || _w.Time < me.PowerReadyAt(pid)) continue;
            var at = def.Effect.Type switch
            {
                "heal" => DamagedCluster() ?? target.Pos,
                "reveal" => target.Pos,
                "spawn" when def.Effect.Unit.Contains("drone") || def.Effect.Unit.Contains("ied") => target.Pos,
                "spawn" => _wave.Count > 0 ? target.Pos : _muster,
                _ => target.Pos,
            };
            if (def.Effect.Type == "heal" && DamagedCluster() is null) continue;
            _w.Submit(new UsePowerCommand(Player, pid, at));
            break;
        }

        foreach (var b in Mine)
        {
            if (b.Building?.Superweapon is not { } sw || !b.Operational || b.SuperweaponCharge < sw.ChargeTime) continue;
            _w.Submit(new FireSuperweaponCommand(Player, b.Id, (enemyHq ?? target).Pos));
            Status = "superweapon fired";
        }
    }

    private Vec2? DamagedCluster()
    {
        Entity? worst = null;
        foreach (var e in Mine)
            if (!e.IsBuilding && e.HasWeapons && e.HpFraction < 0.5f && (worst is null || e.HpFraction < worst.HpFraction)) worst = e;
        return worst?.Pos;
    }

    // ------------------------------------------------------------ helpers

    private void UpdateEnemyDirection(Entity hq)
    {
        var enemy = NearestEnemyBuilding(hq.Pos);
        if (enemy is not null) _enemyDir = (enemy.Pos - hq.Pos).Normalized;
        _muster = _w.ClampToMap(hq.Pos + _enemyDir * 14f);
    }

    private Entity? NearestEnemyBuilding(Vec2 from)
    {
        Entity? best = null;
        var bestD = float.MaxValue;
        foreach (var e in _w.Entities)
        {
            if (!e.Alive || e.Owner == Player || e.Owner < 0 || !e.IsBuilding || e.Building!.IsHole || _w.Player(e.Owner).Eliminated) continue;
            var d = (e.Pos - from).LengthSq;
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    private int Count(string defId) => Mine.Count(e => e.Def.Id == defId);
    private int CountIncludingQueued(string unitId) => Count(unitId) + Mine.Sum(e => e.Queue?.Items.Count(i => !i.IsUpgrade && i.Id == unitId) ?? 0);
    private bool Owns(string buildingId) => Mine.Any(e => e.Building?.Id == buildingId);
    private bool Constructing(string buildingId) => Mine.Any(e => e.Building?.Id == buildingId && e.UnderConstruction);
    /// <summary>A builder not already on a site. Prefers one that is idle over one that is harvesting; never one carrying cargo home.</summary>
    private Entity? IdleBuilder() =>
        Mine.FirstOrDefault(e => e.IsBuilder && e.BuildTargetId == 0 && !e.IsInside && !_assigned.Contains(e.Id) && e.Move is null && e.HarvestState == HarvestState.Idle)
        ?? Mine.FirstOrDefault(e => e.IsBuilder && e.BuildTargetId == 0 && !e.IsInside && !_assigned.Contains(e.Id) && e.HarvestState is HarvestState.Idle or HarvestState.ToPile or HarvestState.Loading);
    private bool CanAfford(int cost) => Me.Cash >= cost + _p.CashReserve;

    private IEnumerable<Entity> Producers(string unitId) => Mine.Where(e => e.Operational && e.Building?.Produces.Contains(unitId) == true);

    /// <summary>Spiral search for a footprint origin around an anchor. Keeps a one-cell margin so units can pass between buildings.</summary>
    private (int x, int y)? FindPlacement(BuildingDef def, Vec2 anchor, float minDist, float maxDist, Vec2? bias = null)
    {
        for (var r = minDist; r <= maxDist; r += 1.5f)
        {
            var steps = Math.Max(8, (int)(r * 2f));
            for (var i = 0; i < steps; i++)
            {
                var ang = i * (MathF.PI * 2f / steps);
                if (bias is { } b) ang = b.Angle + (i % 2 == 0 ? 1 : -1) * (i / 2) * (MathF.PI * 2f / steps);
                var centre = anchor + Vec2.FromAngle(ang) * r;
                var cx = (int)MathF.Round(centre.X - def.Width * 0.5f);
                var cy = (int)MathF.Round(centre.Y - def.Height * 0.5f);
                if (CanPlaceWithMargin(def, cx, cy)) return (cx, cy);
            }
        }
        return null;
    }

    private bool CanPlaceWithMargin(BuildingDef def, int cx, int cy)
    {
        if (!_w.CanPlace(def, cx, cy, out _)) return false;
        for (var y = cy - 1; y <= cy + def.Height; y++)
            for (var x = cx - 1; x <= cx + def.Width; x++)
            {
                if (!_w.Grid.InBounds(x, y)) return false;
                if (_w.BuildingAt(x, y) != 0) return false;
                var t = _w.Grid.Get(x, y);
                if (t is CellType.Water or CellType.Cliff) return false;
            }
        return true;
    }

    private bool TryBuild(string buildingId, Vec2 anchor, float minDist, float maxDist, Vec2? bias = null)
    {
        if (!_w.Rules.Buildings.TryGetValue(buildingId, out var def)) return false;
        if (Constructing(buildingId)) return false;
        if (!_w.HasPrereqs(Player, def.Prereqs, out _)) return false;
        if (!CanAfford(def.Cost)) return false;
        var builder = IdleBuilder();
        if (builder is null) return false;
        var spot = FindPlacement(def, anchor, minDist, maxDist, bias);
        if (spot is null) return false;
        _w.Submit(new BuildCommand(Player, builder.Id, buildingId, spot.Value.x, spot.Value.y));
        _assigned.Add(builder.Id);
        Status = $"building {def.Name}";
        return true;
    }

    // ------------------------------------------------------------ managers

    private void ManageBuilders(Entity hq)
    {
        if (_p.BuilderUnit == "" || CountIncludingQueued(_p.BuilderUnit) >= _p.TargetBuilders) return;
        var producer = Producers(_p.BuilderUnit).FirstOrDefault(e => e.Queue is { Items.Count: 0 });
        if (producer is null || Me.Cash < _w.Rules.Unit(_p.BuilderUnit).Cost) return;
        _w.Submit(new ProduceCommand(Player, producer.Id, _p.BuilderUnit));
    }

    private void ManageBase(Entity hq)
    {
        var me = Me;
        // Power first: keep headroom for the next building, and never start something else while short.
        if (_p.PowerBuilding != "" && me.Faction.NeedsPower && Owns(_p.SupplyBuilding))
        {
            var next = _p.BuildOrder.FirstOrDefault(b => !Owns(b));
            var nextDemand = next is not null && _w.Rules.Buildings.TryGetValue(next, out var nd) && nd.Power < 0 ? -nd.Power : 0;
            if (me.PowerDemand + nextDemand + 2 > me.PowerSupply)
            {
                if (!Constructing(_p.PowerBuilding)) TryBuild(_p.PowerBuilding, hq.Pos, 6f, 20f, -_enemyDir);
                if (me.LowPower || Constructing(_p.PowerBuilding)) return;
            }
        }

        // The build order.
        foreach (var b in _p.BuildOrder)
        {
            if (Owns(b)) continue;
            if (b == _p.SupplyBuilding)
            {
                var pile = Economy.NearestPile(_w, hq.Pos, 60f);
                var anchor = pile?.Pos ?? hq.Pos;
                if (TryBuild(b, anchor, 4f, 12f, pile is null ? null : (hq.Pos - pile.Pos).Normalized)) return;
            }
            else if (TryBuild(b, hq.Pos, 6f, 22f, -_enemyDir)) return;
            return; // wait for this one (cash or prereqs) before moving on
        }

        // Defences facing the enemy.
        if (_p.DefenceBuilding != "" && Count(_p.DefenceBuilding) < _p.Defences)
        {
            var side = (_defenceIndex++ % 2 == 0 ? 1f : -1f);
            var perp = new Vec2(-_enemyDir.Y, _enemyDir.X) * side * 5f;
            if (TryBuild(_p.DefenceBuilding, hq.Pos + _enemyDir * 10f + perp, 0.5f, 6f, _enemyDir)) return;
        }

        // Expansion: all piles near our supply centres empty → new centre at another pile.
        if (_p.Expand && _p.SupplyBuilding != "" && !Constructing(_p.SupplyBuilding))
        {
            var centres = Mine.Where(e => e.Operational && e.Building is { SupplyCenter: true }).ToList();
            if (centres.Count > 0 && !centres.Any(c => Economy.NearestPile(_w, c.Pos, 30f) is not null))
            {
                var pile = Economy.NearestPile(_w, hq.Pos, 200f);
                if (pile is not null && TryBuild(_p.SupplyBuilding, pile.Pos, 4f, 12f, (hq.Pos - pile.Pos).Normalized)) return;
            }
        }
    }

    private void ManageHarvesters()
    {
        if (_p.HarvesterUnit == "") return;
        var have = CountIncludingQueued(_p.HarvesterUnit);
        var centres = Mine.Count(e => e.Operational && e.Building is { SupplyCenter: true });
        var target = Math.Min(_p.MaxHarvesters, _p.TargetHarvesters + Math.Max(0, centres - 1) * 2);
        // Idle harvesters go back to work, but only when there is somewhere to unload, and never a builder on (or just sent to) a site.
        var hasCenter = Mine.Any(e => e.Operational && e.Building is { SupplyCenter: true });
        if (hasCenter)
            foreach (var h in Mine.Where(e => e.IsHarvester && e.HarvestState == HarvestState.Idle && e.Move is null && e.BuildTargetId == 0 && !e.IsInside && !_assigned.Contains(e.Id)))
                if (Economy.NearestPile(_w, h.Pos, 60f) is not null) { _w.Submit(new HarvestCommand(Player, new[] { h.Id }, 0)); _assigned.Add(h.Id); }
        if (have >= target || _w.Piles.All(p => p.Depleted)) return;
        var producer = Producers(_p.HarvesterUnit).FirstOrDefault(e => e.Queue is { Items.Count: 0 });
        if (producer is null || !CanAfford(_w.Rules.Unit(_p.HarvesterUnit).Cost)) return;
        _w.Submit(new ProduceCommand(Player, producer.Id, _p.HarvesterUnit));
    }

    private void ManageProduction()
    {
        var army = Mine.Count(e => !e.IsBuilding && e.HasWeapons);
        if (army >= _p.MaxArmy || _p.Composition.Count == 0) return;
        foreach (var producer in Mine.Where(e => e.Operational && e.Queue is not null && e.Building!.Produces.Count > 0))
        {
            if (producer.Queue!.Items.Count >= 2) continue;
            if (producer.Rally is null || (producer.Rally.Value - _muster).LengthSq > 4f)
                _w.Submit(new RallyCommand(Player, producer.Id, _muster));
            // Weighted round robin: the unit furthest below its share.
            string? pick = null;
            var bestScore = float.MaxValue;
            foreach (var (unitId, weight) in _p.Composition)
            {
                if (weight <= 0 || !producer.Building!.Produces.Contains(unitId)) continue;
                var def = _w.Rules.Unit(unitId);
                if (!_w.HasPrereqs(Player, def.Prereqs, out _)) continue;
                var score = _produced.GetValueOrDefault(unitId) / (float)weight;
                if (score < bestScore) { bestScore = score; pick = unitId; }
            }
            if (pick is null) continue;
            var cost = _w.Rules.Unit(pick).Cost;
            if (!CanAfford(cost)) continue;
            _w.Submit(new ProduceCommand(Player, producer.Id, pick));
            _produced[pick] = _produced.GetValueOrDefault(pick) + 1;
        }
    }

    private void ManageUpgrades()
    {
        if (_w.Time < _p.FirstWaveAt * 0.5f) return;
        foreach (var upId in _p.UpgradeOrder)
        {
            if (Me.Has(upId)) continue;
            if (Mine.Any(e => e.Queue?.Items.Any(i => i.IsUpgrade && i.Id == upId) == true)) return;
            var lab = Mine.FirstOrDefault(e => e.Operational && e.Building?.Upgrades.Contains(upId) == true && e.Queue is { Items.Count: 0 });
            if (lab is null) return;
            var up = _w.Rules.Upgrade(upId);
            if (!_w.HasPrereqs(Player, up.Prereqs, out _) || !CanAfford(up.Cost)) return;
            _w.Submit(new ProduceCommand(Player, lab.Id, upId));
            return;
        }
    }

    private void ManageDefence(Entity hq)
    {
        if (_w.Time - _lastDefenceTime < 4f) return;
        // Any visible enemy combat unit near one of our buildings?
        Entity? threat = null;
        var bestD = _p.DefenceRadius;
        foreach (var b in Mine.Where(e => e.IsBuilding))
        {
            foreach (var c in _w.Spatial.Query(b.Pos, _p.DefenceRadius))
            {
                if (c.Owner == Player || c.Owner < 0 || c.IsBuilding || !c.HasWeapons) continue;
                var d = (c.Pos - b.Pos).Length;
                if (d < bestD) { bestD = d; threat = c; }
            }
        }
        if (threat is null) return;
        _lastDefenceTime = _w.Time;
        var defenders = Mine.Where(e => !e.IsBuilding && e.HasWeapons && !_wave.Contains(e.Id) && !e.IsHarvester && !e.IsBuilder)
            .Select(e => e.Id).ToArray();
        if (defenders.Length == 0) return;
        _w.Submit(new AttackMoveCommand(Player, defenders, threat.Pos));
        Status = "defending";
    }

    private void ManageAttack(Entity hq)
    {
        var now = _w.Time;
        if (_wave.Count > 0)
        {
            // Wave in progress: retarget when the target dies; retreat when mauled.
            if (_wave.Count <= _waveStartCount * _p.RetreatFraction)
            {
                _w.Submit(new MoveCommand(Player, _wave.ToArray(), _muster));
                _wave.Clear();
                Status = "retreating";
                return;
            }
            if (_w.Get(_waveTargetId) is null || (_w.Tick % 100 == 0))
            {
                var lead = _w.Get(_wave[0])!;
                var target = PickTarget(lead.Pos);
                if (target is null) { _wave.Clear(); return; }
                if (target.Id != _waveTargetId || _w.Tick % 100 == 0)
                {
                    _waveTargetId = target.Id;
                    _w.Submit(new AttackMoveCommand(Player, _wave.ToArray(), target.Pos));
                }
            }
            return;
        }

        var army = Mine.Where(e => !e.IsBuilding && e.HasWeapons && !e.IsHarvester && !e.IsBuilder).ToList();
        var interval = _lastWaveTime < 0 ? _p.FirstWaveAt : _p.WaveInterval;
        var since = now - (_lastWaveTime < 0 ? 0f : _lastWaveTime);
        var ready = army.Count >= _p.WaveSize || (since >= interval * 1.5f && army.Count >= Math.Max(3, _p.WaveSize / 2));
        if (now < _p.FirstWaveAt || since < interval || !ready) return;

        var tgt = PickTarget(_muster);
        if (tgt is null) return;
        _wave.AddRange(army.Select(a => a.Id));
        _waveStartCount = _wave.Count;
        _waveTargetId = tgt.Id;
        _lastWaveTime = now;
        _w.Submit(new AttackMoveCommand(Player, _wave.ToArray(), tgt.Pos));
        Status = $"attacking with {_wave.Count}";
    }

    private Entity? PickTarget(Vec2 from)
    {
        if (_p.Harass)
        {
            Entity? h = null;
            var hd = float.MaxValue;
            foreach (var e in _w.Entities)
            {
                if (!e.Alive || e.Owner == Player || e.Owner < 0 || _w.Player(e.Owner).Eliminated) continue;
                if (!e.IsHarvester && e.Building is not { SupplyCenter: true }) continue;
                var d = (e.Pos - from).LengthSq;
                if (d < hd) { hd = d; h = e; }
            }
            if (h is not null) return h;
        }
        return NearestEnemyBuilding(from);
    }
}
