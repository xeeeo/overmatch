"""Tiny pure-Python DSP toolkit for Overmatch audio. No numpy: everything is lists of floats in [-1, 1]."""
import math
import random
import struct
import wave

SR = 22050


def silence(seconds, sr=SR):
    return [0.0] * int(seconds * sr)


def sine(freq, seconds, sr=SR, phase=0.0):
    n = int(seconds * sr)
    w = 2 * math.pi * freq / sr
    return [math.sin(phase + w * i) for i in range(n)]


def sweep(f0, f1, seconds, sr=SR, shape="sine"):
    """Frequency glide from f0 to f1 (exponential)."""
    n = int(seconds * sr)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / max(1, n - 1)
        f = f0 * (f1 / f0) ** t if f0 > 0 and f1 > 0 else f0 + (f1 - f0) * t
        phase += 2 * math.pi * f / sr
        s = math.sin(phase)
        if shape == "square":
            s = 1.0 if s >= 0 else -1.0
        elif shape == "saw":
            s = (phase / math.pi) % 2 - 1
        out.append(s)
    return out


def noise(seconds, sr=SR, seed=1):
    rng = random.Random(seed)
    return [rng.uniform(-1, 1) for _ in range(int(seconds * sr))]


def lowpass(x, cutoff, sr=SR):
    rc = 1.0 / (2 * math.pi * cutoff)
    a = (1.0 / sr) / (rc + 1.0 / sr)
    out = []
    y = 0.0
    for s in x:
        y += a * (s - y)
        out.append(y)
    return out


def highpass(x, cutoff, sr=SR):
    rc = 1.0 / (2 * math.pi * cutoff)
    a = rc / (rc + 1.0 / sr)
    out = []
    y = 0.0
    prev = 0.0
    for s in x:
        y = a * (y + s - prev)
        prev = s
        out.append(y)
    return out


def bandpass(x, lo, hi, sr=SR):
    return highpass(lowpass(x, hi, sr), lo, sr)


def sweep_lowpass(x, c0, c1, sr=SR):
    """Low-pass whose cutoff glides from c0 to c1 over the sound."""
    out = []
    y = 0.0
    n = len(x)
    for i, s in enumerate(x):
        c = c0 * (c1 / c0) ** (i / max(1, n - 1))
        rc = 1.0 / (2 * math.pi * c)
        a = (1.0 / sr) / (rc + 1.0 / sr)
        y += a * (s - y)
        out.append(y)
    return out


def env_exp(x, decay, attack=0.002, sr=SR):
    """Fast attack, exponential decay (decay = seconds to fall to ~37%)."""
    out = []
    na = max(1, int(attack * sr))
    for i, s in enumerate(x):
        a = min(1.0, i / na)
        out.append(s * a * math.exp(-(i / sr) / decay))
    return out


def env_adsr(x, a, d, s_level, r, sr=SR):
    n = len(x)
    na, nd, nr = int(a * sr), int(d * sr), int(r * sr)
    out = []
    for i, v in enumerate(x):
        if i < na:
            g = i / max(1, na)
        elif i < na + nd:
            g = 1 - (1 - s_level) * (i - na) / max(1, nd)
        elif i < n - nr:
            g = s_level
        else:
            g = s_level * max(0.0, (n - i) / max(1, nr))
        out.append(v * g)
    return out


def gain(x, g):
    return [s * g for s in x]


def mix(*tracks):
    n = max(len(t) for t in tracks)
    out = [0.0] * n
    for t in tracks:
        for i, s in enumerate(t):
            out[i] += s
    return out


def concat(*parts):
    out = []
    for p in parts:
        out.extend(p)
    return out


def delay(x, seconds, sr=SR):
    return silence(seconds, sr) + list(x)


def clip(x, drive=1.0):
    """Soft clipping (tanh). drive > 1 adds grit."""
    return [math.tanh(s * drive) for s in x]


def echo(x, seconds, feedback=0.35, repeats=3, sr=SR):
    out = list(x) + [0.0] * int(seconds * repeats * sr)
    d = int(seconds * sr)
    for r in range(1, repeats + 1):
        g = feedback ** r
        for i, s in enumerate(x):
            out[i + d * r] += s * g
    return out


def normalise(x, peak=0.89):
    m = max(1e-9, max(abs(s) for s in x))
    return [s * peak / m for s in x]


def fade(x, fade_in=0.003, fade_out=0.01, sr=SR):
    n = len(x)
    ni, no = int(fade_in * sr), int(fade_out * sr)
    out = list(x)
    for i in range(min(ni, n)):
        out[i] *= i / ni
    for i in range(min(no, n)):
        out[n - 1 - i] *= i / no
    return out


def write_wav(path, x, sr=SR):
    x = fade(normalise(x), sr=sr)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in x))


def read_wav(path):
    with wave.open(path, "rb") as w:
        sr = w.getframerate()
        n = w.getnframes()
        ch = w.getnchannels()
        width = w.getsampwidth()
        raw = w.readframes(n)
    if width != 2:
        raise ValueError("16-bit WAV expected")
    vals = struct.unpack("<" + "h" * (len(raw) // 2), raw)
    if ch > 1:
        vals = vals[::ch]
    return [v / 32768.0 for v in vals], sr


def resample(x, sr_from, sr_to):
    if sr_from == sr_to:
        return list(x)
    ratio = sr_from / sr_to
    n = int(len(x) / ratio)
    out = []
    for i in range(n):
        p = i * ratio
        i0 = int(p)
        f = p - i0
        a = x[i0]
        b = x[min(i0 + 1, len(x) - 1)]
        out.append(a + (b - a) * f)
    return out
