"""A generated ambient loop: low drone, slow pulse, sparse bell tones, distant hits. Seamless at the loop point.

    python3 tools/audio/build_music.py
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dsp import SR, lowpass, write_wav  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "audio", "music")

BPM = 80
BARS = 16
BEAT = 60.0 / BPM
LENGTH = BARS * 4 * BEAT  # 48 s
N = int(LENGTH * SR)

# D minor-ish: root motion over four 4-bar phrases.
ROOTS = [73.42, 58.27, 65.41, 55.0]  # D2, Bb1, C2, A1
BELLS = [293.66, 349.23, 440.0, 523.25, 587.33]


def main():
    rng = random.Random(7)
    out = [0.0] * N
    two_pi = 2 * math.pi

    # Drone pad: detuned saw-ish partials, slow swell per phrase. Frequencies chosen so every partial completes whole cycles per loop.
    for phrase, root in enumerate(ROOTS):
        start = int(phrase * 4 * 4 * BEAT * SR)
        end = int((phrase + 1) * 4 * 4 * BEAT * SR)
        span = end - start
        for detune, amp in ((1.0, 0.30), (1.004, 0.22), (0.5, 0.25), (1.5, 0.12), (2.0, 0.08)):
            f = root * detune
            for i in range(span):
                t = i / SR
                swell = math.sin(math.pi * i / span) ** 0.7
                out[start + i] += amp * swell * (math.sin(two_pi * f * t) + 0.3 * math.sin(two_pi * f * 2 * t) + 0.15 * math.sin(two_pi * f * 3 * t))

    # Pulse bass on every beat, accent on one.
    for beat in range(BARS * 4):
        root = ROOTS[(beat // 16) % 4]
        start = int(beat * BEAT * SR)
        dur = int(0.35 * SR)
        accent = 1.0 if beat % 4 == 0 else 0.55
        for i in range(min(dur, N - start)):
            t = i / SR
            out[start + i] += 0.55 * accent * math.exp(-t / 0.12) * math.sin(two_pi * root * t)

    # Distant hit every two bars (filtered noise burst).
    for bar in range(0, BARS, 2):
        start = int((bar * 4 + 2) * BEAT * SR)
        dur = int(0.9 * SR)
        y = 0.0
        for i in range(min(dur, N - start)):
            t = i / SR
            y += 0.06 * (rng.uniform(-1, 1) - y)
            out[start + i] += 1.4 * math.exp(-t / 0.25) * y

    # Sparse bell tones.
    for bar in range(BARS):
        if rng.random() < 0.6:
            f = rng.choice(BELLS)
            start = int((bar * 4 + rng.choice([0, 1.5, 2.5, 3])) * BEAT * SR)
            dur = int(2.2 * SR)
            for i in range(dur):
                t = i / SR
                idx = (start + i) % N  # wraps, so tails carry over the loop point
                out[idx] += 0.10 * math.exp(-t / 0.7) * (math.sin(two_pi * f * t) + 0.4 * math.sin(two_pi * f * 2.76 * t))

    out = lowpass(out, 5000)
    write_wav(os.path.join(OUT, "ambient_loop.wav"), out)
    print(f"[music] wrote {LENGTH:.0f}s loop")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    main()
