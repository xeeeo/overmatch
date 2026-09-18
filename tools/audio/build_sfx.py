"""Synthesises every Overmatch sound effect into game/assets/audio/sfx/. Reproducible: no samples, only maths.

    python3 tools/audio/build_sfx.py [name ...]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dsp import *  # noqa: F401,F403

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "audio", "sfx")


def boom(seconds, body=70, crack=2500, tail=0.35, seed=1):
    """Generic explosion: a sub thump, a noise body that darkens over time, and a bright crack at the start."""
    thump = env_exp(sweep(body * 2.2, body * 0.6, seconds), tail * 0.8)
    body_n = env_exp(sweep_lowpass(noise(seconds, seed=seed), crack, 180), tail)
    crack_n = env_exp(highpass(noise(0.08, seed=seed + 7), 1800), 0.02)
    return clip(mix(gain(thump, 0.9), gain(body_n, 1.0), gain(crack_n, 0.7)), 1.6)


def cannon():
    return boom(0.9, body=85, crack=3200, tail=0.22, seed=3)


def heavy_cannon():
    return boom(1.2, body=60, crack=2600, tail=0.32, seed=4)


def artillery():
    launch = env_exp(sweep_lowpass(noise(1.0, seed=5), 1800, 300), 0.3)
    return clip(mix(boom(1.1, body=55, crack=2000, tail=0.3, seed=6), gain(launch, 0.5)), 1.4)


def mg_burst():
    shots = []
    for i in range(4):
        click = env_exp(bandpass(noise(0.07, seed=20 + i), 500, 4500), 0.018)
        pop = env_exp(sweep(320, 120, 0.07), 0.02)
        shots.append(mix(click, gain(pop, 0.6)))
        shots.append(silence(0.025))
    return clip(concat(*shots), 1.8)


def rifle():
    click = env_exp(bandpass(noise(0.18, seed=31), 600, 5000), 0.03)
    pop = env_exp(sweep(400, 140, 0.12), 0.03)
    return echo(clip(mix(click, gain(pop, 0.5)), 1.5), 0.09, 0.25, 2)


def sniper():
    crack = env_exp(highpass(noise(0.25, seed=33), 1200), 0.035)
    thump = env_exp(sweep(260, 70, 0.3), 0.06)
    return echo(clip(mix(crack, gain(thump, 0.8)), 1.8), 0.16, 0.35, 3)


def rocket():
    whoosh = env_adsr(sweep_lowpass(noise(1.1, seed=40), 500, 3500), 0.05, 0.2, 0.6, 0.6)
    tone = env_adsr(sweep(180, 420, 1.1, shape="saw"), 0.03, 0.2, 0.25, 0.6)
    ignite = env_exp(noise(0.12, seed=41), 0.03)
    return mix(gain(whoosh, 0.9), gain(lowpass(tone, 900), 0.25), gain(ignite, 0.6))


def flak():
    return concat(boom(0.35, body=130, crack=4000, tail=0.08, seed=44), silence(0.04), boom(0.4, body=120, crack=3800, tail=0.1, seed=45))


def explosion_small():
    return boom(1.0, body=75, crack=2800, tail=0.28, seed=50)


def explosion_large():
    return echo(boom(2.0, body=48, crack=2200, tail=0.6, seed=51), 0.22, 0.3, 2)


def explosion_huge():
    """Superweapon impact: long sub rumble with a rolling tail."""
    rumble = env_exp(lowpass(noise(4.5, seed=52), 120), 1.6)
    return echo(mix(boom(3.0, body=36, crack=1800, tail=1.1, seed=53), gain(rumble, 1.2)), 0.35, 0.45, 3)


def impact():
    return env_exp(bandpass(noise(0.25, seed=55), 300, 3000), 0.05)


def building_collapse():
    crumble = env_adsr(sweep_lowpass(noise(2.2, seed=57), 2500, 200), 0.02, 0.4, 0.5, 1.2)
    return mix(boom(1.6, body=50, crack=1800, tail=0.5, seed=58), gain(crumble, 0.8))


def toxin():
    hiss = env_adsr(bandpass(noise(1.2, seed=60), 2500, 8000), 0.08, 0.3, 0.5, 0.6)
    bubble = env_adsr(sweep(140, 90, 1.2), 0.1, 0.3, 0.3, 0.5)
    return mix(hiss, gain(bubble, 0.5))


def place_building():
    thud = env_exp(sweep(160, 60, 0.35), 0.09)
    clank = env_exp(bandpass(noise(0.2, seed=62), 800, 3000), 0.04)
    return mix(thud, gain(clank, 0.5))


def construction_complete():
    a = env_exp(sine(660, 0.18), 0.08)
    b = env_exp(sine(880, 0.3), 0.12)
    return concat(a, gain(b, 0.9))


def unit_ready():
    return concat(env_exp(sine(520, 0.12), 0.05), env_exp(sine(780, 0.22), 0.09))


def click():
    return env_exp(bandpass(noise(0.05, seed=70), 1500, 6000), 0.008)


def error():
    return concat(env_exp(sweep(220, 200, 0.14, shape="square"), 0.07), silence(0.04), env_exp(sweep(180, 160, 0.2, shape="square"), 0.09))


def alert():
    beep = env_adsr(sine(880, 0.16), 0.005, 0.03, 0.8, 0.05)
    return concat(beep, silence(0.07), beep, silence(0.07), beep)


def siren():
    """Superweapon launch warning."""
    up = env_adsr(lowpass(sweep(320, 760, 1.2, shape="saw"), 2200), 0.05, 0.1, 0.9, 0.1)
    down = env_adsr(lowpass(sweep(760, 320, 1.2, shape="saw"), 2200), 0.05, 0.1, 0.9, 0.2)
    return echo(concat(up, down), 0.2, 0.25, 2)


def promotion():
    notes = [523, 659, 784, 1047]
    return concat(*[env_exp(mix(sine(f, 0.22), gain(sine(f * 2, 0.22), 0.3)), 0.12) for f in notes])


def power_use():
    return mix(env_adsr(sweep(200, 1200, 0.6), 0.02, 0.1, 0.7, 0.3), gain(env_exp(noise(0.6, seed=80), 0.2), 0.2))


def cash():
    return concat(env_exp(sine(1320, 0.07), 0.03), env_exp(sine(1760, 0.16), 0.07))


def move_ack():
    return env_exp(sine(440, 0.09), 0.04)


def capture():
    return concat(env_exp(sine(392, 0.15), 0.07), env_exp(sine(523, 0.15), 0.07), env_exp(sine(659, 0.3), 0.14))


def garrison():
    return mix(env_exp(sweep(180, 90, 0.25), 0.07), gain(env_exp(bandpass(noise(0.15, seed=85), 400, 2000), 0.03), 0.6))


def drone():
    buzz = env_adsr(lowpass(sweep(240, 300, 0.9, shape="saw"), 1800), 0.1, 0.2, 0.7, 0.3)
    trem = [s * (0.7 + 0.3 * math.sin(i * 2 * math.pi * 38 / SR)) for i, s in enumerate(buzz)]
    return trem


def jet_flyby():
    n = env_adsr(sweep_lowpass(noise(2.0, seed=90), 600, 5000), 0.7, 0.2, 0.8, 1.0)
    return gain(n, 1.0)


def ew_pulse():
    return echo(env_exp(sweep(2400, 120, 0.7, shape="square"), 0.25), 0.11, 0.4, 3)


def victory():
    notes = [(392, 0.25), (523, 0.25), (659, 0.25), (784, 0.7)]
    return concat(*[env_adsr(mix(sine(f, d), gain(sine(f * 1.5, d), 0.35), gain(sine(f / 2, d), 0.4)), 0.01, 0.05, 0.8, 0.12) for f, d in notes])


def defeat():
    notes = [(392, 0.35), (349, 0.35), (311, 0.35), (233, 0.9)]
    return concat(*[env_adsr(mix(sine(f, d), gain(sine(f / 2, d), 0.5)), 0.01, 0.05, 0.8, 0.2) for f, d in notes])


SOUNDS = {
    "cannon": cannon, "heavy_cannon": heavy_cannon, "artillery": artillery, "mg_burst": mg_burst, "rifle": rifle, "sniper": sniper,
    "rocket": rocket, "flak": flak, "explosion_small": explosion_small, "explosion_large": explosion_large, "explosion_huge": explosion_huge,
    "impact": impact, "building_collapse": building_collapse, "toxin": toxin, "place_building": place_building,
    "construction_complete": construction_complete, "unit_ready": unit_ready, "click": click, "error": error, "alert": alert,
    "siren": siren, "promotion": promotion, "power_use": power_use, "cash": cash, "move_ack": move_ack, "capture": capture,
    "garrison": garrison, "drone": drone, "jet_flyby": jet_flyby, "ew_pulse": ew_pulse, "victory": victory, "defeat": defeat,
}

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    names = sys.argv[1:] or list(SOUNDS)
    for n in names:
        write_wav(os.path.join(OUT, f"{n}.wav"), SOUNDS[n]())
    print(f"[sfx] wrote {len(names)} sounds to {OUT}")
