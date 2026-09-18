// Headless AI-vs-AI match runner for balance work.
//
//   dotnet run --project tools/harness -c Release -- [--difficulty medium] [--maps plain,highlands] [--seeds 3] [--minutes 25] [--verbose]
//
// Every faction pairing is played on every map, from both sides, for each seed. Prints a win-rate table.
using System.Collections.Concurrent;
using System.Diagnostics;
using Overmatch.Sim;
using Overmatch.Sim.Data;

var opts = new Dictionary<string, string>();
for (var i = 0; i < args.Length; i++)
    if (args[i].StartsWith("--")) opts[args[i][2..]] = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true";

var dataDir = FindData();
var rules = GameRules.Load(Directory.EnumerateFiles(dataDir, "*.json", SearchOption.AllDirectories)
    .Select(f => new DataFile(Path.GetRelativePath(dataDir, f).Replace('\\', '/'), File.ReadAllText(f))));

var difficulty = opts.GetValueOrDefault("difficulty", "medium");
var maps = opts.GetValueOrDefault("maps", "plain,highlands,open_steppe").Split(',');
var seeds = int.Parse(opts.GetValueOrDefault("seeds", "2"));
var minutes = int.Parse(opts.GetValueOrDefault("minutes", "25"));
var verbose = opts.ContainsKey("verbose");
var factions = rules.Factions.Keys.OrderBy(k => k).ToArray();

if (opts.TryGetValue("ffa", out var ffaMap))
{
    // --ffa twilight_frost  every start position gets an AI (factions rotate); reports the speed of the simulation.
    var fmap = rules.Map(ffaMap);
    var fw = new World(rules, fmap, fmap.Spawns.Select((_, i) => factions[i % factions.Length]), 1);
    for (var p = 0; p < fmap.Spawns.Count; p++)
    {
        var s = fmap.Spawns[p];
        var fd = fw.Player(p).Faction;
        var hq = rules.Building(fd.Hq);
        fw.PlaceBuilding(fd.Hq, p, (int)s.X - hq.Width / 2, (int)s.Y - hq.Height / 2);
        fw.Spawn(fd.Builder, p, new Vec2(s.X, s.Y - 5));
        fw.AddAi(p, rules.Ai(fd.Id, difficulty));
    }
    var clock = System.Diagnostics.Stopwatch.StartNew();
    double worst = 0; long lastMs = 0;
    while (!fw.Finished && fw.Tick < World.TicksPerSecond * 60 * minutes)
    {
        var t0 = clock.Elapsed.TotalMilliseconds;
        fw.Step();
        worst = Math.Max(worst, clock.Elapsed.TotalMilliseconds - t0);
        if (fw.Tick % (World.TicksPerSecond * 300) != 0) continue;
        var alive = Enumerable.Range(0, fw.PlayerCount).Where(p => !fw.Player(p).Eliminated).ToList();
        Console.WriteLine($"{fw.Time / 60,4:0}m  entities={fw.Entities.Count,4}  alive={alive.Count}  avg tick={(clock.ElapsedMilliseconds - lastMs) / (double)(World.TicksPerSecond * 300):0.00} ms  worst tick={worst:0.0} ms  " +
            string.Join(" ", alive.Select(p => $"{fw.Player(p).Faction.Id[..3]}{p}:{fw.Entities.Count(e => e.Owner == p && !e.IsBuilding)}")));
        lastMs = clock.ElapsedMilliseconds; worst = 0;
    }
    Console.WriteLine($"winner: {(fw.Winner >= 0 ? $"player {fw.Winner} ({fw.Player(fw.Winner).Faction.Id})" : "timeout")} at {fw.Time / 60:0.0} min, {clock.Elapsed.TotalSeconds:0.0}s real (tick budget is 50 ms)");
    return;
}

if (opts.TryGetValue("timeline", out var tl))
{
    // --timeline coalition,directorate,plain[,seed]  prints a two-minute timeline of one match.
    var parts = tl.Split(',');
    var tmap = rules.Map(parts[2]);
    var tw = new World(rules, tmap, new[] { parts[0], parts[1] }, parts.Length > 3 ? int.Parse(parts[3]) : 0);
    for (var p = 0; p < 2; p++)
    {
        var s = tmap.Spawns[p];
        var fd = tw.Player(p).Faction;
        var hq = rules.Building(fd.Hq);
        tw.PlaceBuilding(fd.Hq, p, (int)s.X - hq.Width / 2, (int)s.Y - hq.Height / 2);
        tw.Spawn(fd.Builder, p, new Vec2(s.X, s.Y - 5));
        tw.AddAi(p, rules.Ai(fd.Id, difficulty));
    }
    while (!tw.Finished && tw.Tick < World.TicksPerSecond * 60 * minutes)
    {
        tw.Step();
        if (tw.Tick % (World.TicksPerSecond * 120) != 0) continue;
        string S(int pl)
        {
            var mine = tw.Entities.Where(e => e.Owner == pl).ToList();
            var army = mine.Where(e => !e.IsBuilding && e.HasWeapons && !e.IsHarvester && e.FollowId == 0 && e.Unit is { Speed: > 0f }).ToList();
            var hstates = string.Join(",", mine.Where(e => e.IsHarvester).GroupBy(e => e.HarvestState).Select(g => $"{g.Key}:{g.Count()}"));
            var top = string.Join(" ", army.GroupBy(e => e.Def.Id.Split('_', 2)[1]).OrderByDescending(g => g.Count()).Take(4).Select(g => $"{g.Key}x{g.Count()}"));
            return $"{tw.Player(pl).Faction.Id[..3]} earned={tw.Player(pl).TotalEarned,6} cash={tw.Player(pl).Cash,6} bld={mine.Count(e => e.IsBuilding),2} harv={mine.Count(e => e.IsHarvester),2}({hstates}) army={army.Count,2}(${army.Sum(e => e.Def.Cost),5}) lost={tw.Player(pl).UnitsLost,3} kills={tw.Player(pl).UnitsKilled,3} [{top}] '{tw.Ais[pl].Status}'";
        }
        Console.WriteLine($"{tw.Time / 60,4:0}m  {S(0)}\n       {S(1)}");
    }
    Console.WriteLine($"winner: {(tw.Winner >= 0 ? tw.Player(tw.Winner).Faction.Id : "timeout")} at {tw.Time / 60:0.0} min");
    return;
}

var jobs = new List<(string a, string b, string map, int seed)>();
foreach (var a in factions)
    foreach (var b in factions)
        foreach (var map in maps)
            for (var s = 0; s < seeds; s++)
                jobs.Add((a, b, map, s));

var results = new ConcurrentBag<(string a, string b, string map, int seed, int winner, float minutes, int valueA, int valueB)>();
var sw = Stopwatch.StartNew();
Parallel.ForEach(jobs, job =>
{
    var map = rules.Map(job.map);
    var w = new World(rules, map, new[] { job.a, job.b }, job.seed);
    for (var p = 0; p < 2; p++)
    {
        var s = map.Spawns[p];
        var fd = w.Player(p).Faction;
        var hq = rules.Building(fd.Hq);
        w.PlaceBuilding(fd.Hq, p, (int)s.X - hq.Width / 2, (int)s.Y - hq.Height / 2);
        w.Spawn(fd.Builder, p, new Vec2(s.X, s.Y - 5));
        w.AddAi(p, rules.Ai(fd.Id, difficulty));
    }
    var limit = World.TicksPerSecond * 60 * minutes;
    while (!w.Finished && w.Tick < limit) w.Step();
    int Value(int pl) => w.Entities.Where(e => e.Owner == pl && e.Alive).Sum(e => e.Def.Cost);
    results.Add((job.a, job.b, job.map, job.seed, w.Winner, w.Time / 60f, Value(0), Value(1)));
});

Console.WriteLine($"{jobs.Count} matches, difficulty {difficulty}, maps {string.Join(",", maps)}, {seeds} seeds, limit {minutes} min — {sw.Elapsed.TotalSeconds:0}s");
if (verbose)
    foreach (var r in results.OrderBy(r => r.a).ThenBy(r => r.b).ThenBy(r => r.map).ThenBy(r => r.seed))
        Console.WriteLine($"  {r.a,-12} vs {r.b,-12} {r.map,-12} seed {r.seed}: {(r.winner == 0 ? r.a : r.winner == 1 ? r.b : "timeout"),-12} {r.minutes,5:0.0} min   value {r.valueA}/{r.valueB}");

// Win rate of row faction against column faction (mirror matches excluded from totals). A timeout is scored by army+base value.
Console.WriteLine();
Console.WriteLine($"{"win % vs",-13}" + string.Join("", factions.Select(f => $"{f,13}")) + $"{"overall",10}{"avg min",9}{"timeouts",10}");
foreach (var a in factions)
{
    var row = $"{a,-13}";
    double total = 0, wins = 0, mins = 0; var timeouts = 0; var games = 0;
    foreach (var b in factions)
    {
        double w = 0, n = 0;
        foreach (var r in results)
        {
            double Score(int side) => r.winner == side ? 1 : r.winner >= 0 ? 0 : (side == 0 ? r.valueA : r.valueB) > (side == 0 ? r.valueB : r.valueA) * 1.25 ? 0.75 : (side == 0 ? r.valueB : r.valueA) > (side == 0 ? r.valueA : r.valueB) * 1.25 ? 0.25 : 0.5;
            if (r.a == a && r.b == b) { w += Score(0); n++; if (a != b) { mins += r.minutes; games++; if (r.winner < 0) timeouts++; } }
            else if (r.a == b && r.b == a && a != b) { w += Score(1); n++; mins += r.minutes; games++; if (r.winner < 0) timeouts++; }
        }
        row += a == b ? $"{"-",13}" : $"{(n > 0 ? 100 * w / n : 0),12:0}%";
        if (a != b) { total += n; wins += w; }
    }
    Console.WriteLine(row + $"{(total > 0 ? 100 * wins / total : 0),9:0}%{(games > 0 ? mins / games : 0),9:0.0}{timeouts,10}");
}

static string FindData()
{
    var dir = AppContext.BaseDirectory;
    for (var i = 0; i < 10 && dir is not null; i++)
    {
        var c = Path.Combine(dir, "game", "data");
        if (Directory.Exists(c)) return c;
        dir = Path.GetDirectoryName(dir);
    }
    throw new DirectoryNotFoundException("game/data not found");
}
