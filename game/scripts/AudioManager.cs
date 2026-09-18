using Godot;
using Overmatch.Sim;
using Overmatch.Sim.Data;

namespace Overmatch.Game;

/// <summary>All sound: positional battle effects, interface clicks, unit voice responses, the announcer and the music loop.</summary>
public partial class AudioManager : Node
{
    public GameRoot Root { get; set; } = null!;

    private readonly Dictionary<string, AudioStream?> _cache = new();
    private readonly List<AudioStreamPlayer3D> _pool3d = new();
    private readonly Dictionary<string, ulong> _lastPlayed = new();
    private AudioStreamPlayer _ui = null!;
    private AudioStreamPlayer _voice = null!;
    private AudioStreamPlayer _announcer = null!;
    private AudioStreamPlayer _music = null!;
    private readonly Queue<string> _announceQueue = new();
    private ulong _lastVoice;
    private readonly Random _rng = new();
    private int _next3d;

    public bool MusicOn { get; private set; } = true;

    public override void _Ready()
    {
        for (var i = 0; i < 20; i++)
        {
            var p = new AudioStreamPlayer3D { MaxDistance = 140f, UnitSize = 22f, AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance };
            AddChild(p);
            _pool3d.Add(p);
        }
        _ui = new AudioStreamPlayer { VolumeDb = -10f };
        _voice = new AudioStreamPlayer { VolumeDb = -4f };
        _announcer = new AudioStreamPlayer { VolumeDb = -2f };
        _music = new AudioStreamPlayer { VolumeDb = -16f };
        AddChild(_ui); AddChild(_voice); AddChild(_announcer); AddChild(_music);

        if (Load("music/ambient_loop") is AudioStreamWav loop)
        {
            loop.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            loop.LoopBegin = 0;
            loop.LoopEnd = (int)(loop.GetLength() * loop.MixRate);
            _music.Stream = loop;
            _music.Play();
        }
    }

    private AudioStream? Load(string name)
    {
        if (_cache.TryGetValue(name, out var s)) return s;
        var path = $"res://assets/audio/{name}.wav";
        s = ResourceLoader.Exists(path) ? ResourceLoader.Load<AudioStream>(path) : null;
        _cache[name] = s;
        return s;
    }

    private bool Throttled(string key, ulong minGapMs)
    {
        var now = Time.GetTicksMsec();
        if (_lastPlayed.TryGetValue(key, out var last) && now - last < minGapMs) return true;
        _lastPlayed[key] = now;
        return false;
    }

    public void ToggleMusic()
    {
        MusicOn = !MusicOn;
        _music.StreamPaused = !MusicOn;
    }

    public void PlayUi(string name)
    {
        if (Load("sfx/" + name) is not { } s || Throttled("ui:" + name, 40)) return;
        _ui.Stream = s;
        _ui.Play();
    }

    public void PlayAt(string name, Vec2 simPos, float height = 1f, float volumeDb = 0f, ulong minGapMs = 70)
    {
        if (Load("sfx/" + name) is not { } s || Throttled(name, minGapMs)) return;
        // Only what the player could plausibly hear: inside their vision or near the camera.
        var world = MapView.ToWorld(simPos, height);
        if ((world - Root.Camera.GlobalPosition).Length() > 120f) return;
        var p = _pool3d[_next3d++ % _pool3d.Count];
        p.Stream = s;
        p.GlobalPosition = world;
        p.VolumeDb = volumeDb;
        p.PitchScale = 0.94f + (float)_rng.NextDouble() * 0.12f;
        p.Play();
    }

    /// <summary>Queue an announcer line; repeats of the same line are rate-limited.</summary>
    public void Announce(string key, ulong minGapMs = 6000)
    {
        if (Throttled("ann:" + key, minGapMs) || _announceQueue.Contains(key) || _announceQueue.Count > 2) return;
        _announceQueue.Enqueue(key);
    }

    public override void _Process(double delta)
    {
        if (_announcer.Playing || _announceQueue.Count == 0) return;
        if (Load("voice/announcer/" + _announceQueue.Dequeue()) is { } s)
        {
            _announcer.Stream = s;
            _announcer.Play();
        }
    }

    public static string VoiceClass(ObjectDef d)
    {
        if (d is UnitDef { Builder: true, Harvest: null }) return "builder";
        if (d is UnitDef { Harvest: not null }) return "harvester";
        if (d.Tags.Contains("drone") || d.Tags.Contains("aircraft")) return "aircraft";
        if (d.IsInfantry) return "infantry";
        if (d.Crusher || d.Tags.Contains("heavy") || d.Spawner is not null || d.Auras.Any(a => a.Type == "jam") || d.Weapons.Any(w => w.Contains("bomb"))) return "heavy";
        return "vehicle";
    }

    /// <summary>A unit answers: "select", "move" or "attack".</summary>
    public void UnitResponse(Entity e, string ev)
    {
        if (e.IsBuilding) { if (ev == "select") PlayUi("click"); return; }
        var now = Time.GetTicksMsec();
        if (now - _lastVoice < 900) return;
        var name = $"voice/{e.Def.Faction}/{VoiceClass(e.Def)}_{ev}_{_rng.Next(1, 4)}";
        if (Load(name) is not { } s) return;
        _lastVoice = now;
        _voice.Stream = s;
        _voice.Play();
    }

    private static string WeaponSound(WeaponDef w)
    {
        var id = w.Id;
        if (w.Suicide) return "explosion_large";
        if (id.Contains("sniper")) return "sniper";
        if (id.Contains("flak")) return "flak";
        if (id.Contains("spray") || w.DamageType == "toxin") return "toxin";
        if (id.Contains("twin") || id.Contains("spectre")) return "heavy_cannon";
        if (id.Contains("cannon") || id.Contains("sentry_gun")) return "cannon";
        if (id.Contains("typhoon") || id.Contains("lancer") || id.Contains("buggy") || id.Contains("bomb") || id.Contains("molotov")) return "artillery";
        if (w.Projectile.Kind == "missile" || id.Contains("rocket") || id.Contains("rpg")) return "rocket";
        if (id.Contains("rifle")) return "rifle";
        return "mg_burst";
    }

    /// <summary>Turn this tick's sim events into sound.</summary>
    public void Consume(World world)
    {
        var me = Root.LocalPlayer;
        foreach (var ev in world.Events)
        {
            switch (ev)
            {
                case WeaponFiredEvent f when world.Rules.Weapons.TryGetValue(f.WeaponId, out var wd):
                    PlayAt(WeaponSound(wd), f.From, 1.2f, -3f, WeaponSound(wd) == "mg_burst" ? 140ul : 80ul);
                    break;
                case HitEvent h when world.Rules.Weapons.TryGetValue(h.WeaponId, out var hw):
                    if (hw.Splash is { Radius: > 1.2f }) PlayAt("explosion_small", h.Pos, 0.5f, -4f, 90);
                    else if (hw.Projectile.Kind != "instant") PlayAt("impact", h.Pos, 0.5f, -8f, 120);
                    break;
                case DiedEvent d:
                    PlayAt(d.WasBuilding ? "building_collapse" : world.Rules.Units.TryGetValue(d.DefId, out var ud) && ud.IsInfantry ? "impact" : "explosion_small", d.Pos, 0.8f, d.WasBuilding ? 2f : -2f, 60);
                    if (d.Owner == me && d.WasBuilding) Announce("building_lost");
                    break;
                case StrikeImpactEvent s:
                    PlayAt(s.Radius >= 6f ? "explosion_huge" : "explosion_large", s.Pos, 1f, s.Radius >= 6f ? 8f : 2f, 50);
                    break;
                case HazardEvent hz:
                    PlayAt("toxin", hz.Pos, 0.3f, -6f, 300);
                    break;
                case SuperweaponFiredEvent sw:
                    PlayUi("siren");
                    Announce("superweapon_launch", 1000);
                    break;
                case SuperweaponReadyEvent sr when sr.Player == me:
                    Announce("superweapon_ready");
                    break;
                case ConstructionStartedEvent cs when cs.Owner == me:
                    PlayUi("place_building");
                    if (world.Get(cs.BuildingId)?.Building?.Superweapon is not null) { }
                    break;
                case ConstructionStartedEvent cs2 when cs2.Owner != me && world.Get(cs2.BuildingId)?.Building?.Superweapon is not null:
                    Announce("enemy_superweapon", 20000);
                    break;
                case ConstructionCompletedEvent cc when cc.Owner == me && world.Tick > 5:
                    PlayUi("construction_complete");
                    Announce("construction_complete");
                    break;
                case ProductionCompletedEvent pc when pc.Owner == me:
                    PlayUi("unit_ready");
                    Announce("unit_ready", 9000);
                    break;
                case UpgradeCompletedEvent uc when uc.Owner == me:
                    Announce("upgrade_complete");
                    break;
                case RankUpEvent ru when ru.Player == me:
                    PlayUi("promotion");
                    Announce("promotion");
                    break;
                case PowerUsedEvent pu:
                    PlayAt("power_use", pu.Target, 1f, 0f, 200);
                    break;
                case CapturedEvent ce:
                    if (ce.NewOwner == me) { PlayUi("capture"); Announce("building_captured"); }
                    else if (ce.OldOwner == me) Announce("building_stolen");
                    break;
                case GarrisonEvent ge when world.Get(ge.UnitId)?.Owner == me:
                    PlayUi("garrison");
                    break;
                case SupplyDeliveredEvent sd when sd.Owner == me:
                    PlayUi("cash");
                    break;
                case AbilityUsedEvent au:
                    PlayAt("ew_pulse", au.Target, 1f, -4f, 200);
                    break;
                case PlayerEliminatedEvent pe when pe.Player != me:
                    Announce("player_eliminated", 1000);
                    break;
                case OrderRejectedEvent r when r.Player == me:
                    PlayUi("error");
                    if (r.Reason == "insufficient funds") Announce("insufficient_funds", 8000);
                    else if (r.Reason is "occupied" or "terrain" or "too close to supplies" or "off map") Announce("cannot_build", 8000);
                    break;
                case MatchEndedEvent m:
                    PlayUi(m.Winner == me ? "victory" : "defeat");
                    Announce(m.Winner == me ? "victory" : "defeat", 1000);
                    break;
            }
        }
    }
}
