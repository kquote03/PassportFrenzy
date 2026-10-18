"""Procedural placeholder SFX for Passport Frenzy (stdlib only, deterministic).

Writes 16-bit mono 44.1 kHz WAVs, one per SoundIds hook, to Assets/_Project/Placeholder/Audio/SFX/<id>.wav.
Run from the repo root:  python Tools/sfx/generate_sfx.py
"""
import math
import os
import random
import struct
import wave

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Placeholder", "Audio", "SFX")
TAU = 2.0 * math.pi


# ---------------------------------------------------------------- building blocks

def silence(seconds):
    return [0.0] * int(seconds * SR)


def mix(*tracks):
    n = max(len(t) for t in tracks)
    out = [0.0] * n
    for t in tracks:
        for i, v in enumerate(t):
            out[i] += v
    return out


def offset(track, seconds):
    return silence(seconds) + track


def concat(*tracks):
    out = []
    for t in tracks:
        out.extend(t)
    return out


def env_adsr(n, a=0.005, d=0.05, s=0.6, r=0.1):
    """Attack/decay/sustain/release envelope over n samples (times in seconds)."""
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    s_n = max(0, n - a_n - d_n - r_n)
    e = []
    for i in range(a_n):
        e.append(i / max(1, a_n))
    for i in range(d_n):
        e.append(1.0 - (1.0 - s) * i / max(1, d_n))
    e.extend([s] * s_n)
    for i in range(r_n):
        e.append(s * (1.0 - i / max(1, r_n)))
    return (e + [0.0] * n)[:n]


def env_exp(n, decay, attack=0.002):
    """Fast attack, exponential decay (decay = seconds to ~1/e)."""
    a_n = max(1, int(attack * SR))
    return [(i / a_n if i < a_n else 1.0) * math.exp(-(i / SR) / decay) for i in range(n)]


def tone(freq, seconds, wave_type="sine", freq_end=None, vibrato=0.0, vib_rate=6.0):
    """Oscillator with optional linear glide (freq -> freq_end) and vibrato (fraction of freq)."""
    n = int(seconds * SR)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / n
        f = freq if freq_end is None else freq + (freq_end - freq) * t
        if vibrato:
            f *= 1.0 + vibrato * math.sin(TAU * vib_rate * i / SR)
        phase += TAU * f / SR
        if wave_type == "sine":
            v = math.sin(phase)
        elif wave_type == "square":
            v = 1.0 if math.sin(phase) >= 0 else -1.0
        elif wave_type == "saw":
            v = 2.0 * ((phase / TAU) % 1.0) - 1.0
        elif wave_type == "tri":
            v = 2.0 * abs(2.0 * ((phase / TAU) % 1.0) - 1.0) - 1.0
        else:
            raise ValueError(wave_type)
        out.append(v)
    return out


def noise(seconds, rng):
    return [rng.uniform(-1.0, 1.0) for _ in range(int(seconds * SR))]


def lowpass(track, cutoff, cutoff_end=None):
    """One-pole low-pass; cutoff may glide linearly."""
    out = []
    y = 0.0
    n = len(track)
    for i, x in enumerate(track):
        c = cutoff if cutoff_end is None else cutoff + (cutoff_end - cutoff) * i / max(1, n - 1)
        a = 1.0 - math.exp(-TAU * c / SR)
        y += a * (x - y)
        out.append(y)
    return out


def highpass(track, cutoff):
    lp = lowpass(track, cutoff)
    return [x - l for x, l in zip(track, lp)]


def bandpass(track, low, high):
    return lowpass(highpass(track, low), high)


def apply(track, envelope, gain=1.0):
    return [x * e * gain for x, e in zip(track, envelope)]


def bell(freq, seconds, decay, partials=((1, 1.0), (2.0, 0.35), (3.01, 0.18), (4.2, 0.08))):
    """Struck chime: inharmonic-ish partials with their own decays."""
    n = int(seconds * SR)
    out = [0.0] * n
    for mult, amp in partials:
        part = apply(tone(freq * mult, seconds), env_exp(n, decay / (0.7 + mult * 0.3)), amp)
        for i in range(n):
            out[i] += part[i]
    return out


def note(freq, seconds, wave_type="tri", decay=None, a=0.004, r=0.04, gain=1.0):
    n = int(seconds * SR)
    e = env_exp(n, decay, a) if decay else env_adsr(n, a, 0.03, 0.7, r)
    return apply(tone(freq, seconds, wave_type), e, gain)


def finish(track, peak_db=-3.0, fade=0.003):
    """Remove DC, fade ends (no clicks), normalize to peak_db dBFS."""
    if not track:
        return track
    mean = sum(track) / len(track)
    track = [x - mean for x in track]
    f = max(1, int(fade * SR))
    for i in range(min(f, len(track))):
        g = i / f
        track[i] *= g
        track[-1 - i] *= g
    peak = max(abs(x) for x in track) or 1.0
    target = 10 ** (peak_db / 20.0)
    return [x * target / peak for x in track]


def write(name, track, peak_db=-3.0):
    track = finish(track, peak_db)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, x)) * 32767)) for x in track))
    print(f"{name:14s} {len(track) / SR:5.2f}s  {path}")


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


# ---------------------------------------------------------------- sounds

def jump(rng):
    # Quick upward square blip, softened by a low-pass.
    t = tone(330, 0.14, "square", freq_end=880)
    return lowpass(apply(t, env_adsr(len(t), 0.003, 0.04, 0.5, 0.07)), 4000)


def hit(rng):
    # Soft body thud (falling sine) + short filtered noise smack.
    thud = apply(tone(160, 0.22, "sine", freq_end=60), env_exp(int(0.22 * SR), 0.06))
    smack = apply(lowpass(noise(0.08, rng), 2500), env_exp(int(0.08 * SR), 0.015), 0.6)
    return mix(thud, smack)


def block(rng):
    # Dull wooden thunk: damped low partials + knock transient.
    n = int(0.2 * SR)
    body = mix(apply(tone(190, 0.2), env_exp(n, 0.035)),
               apply(tone(410, 0.2), env_exp(n, 0.02), 0.5),
               apply(tone(780, 0.2), env_exp(n, 0.01), 0.25))
    knock = apply(bandpass(noise(0.03, rng), 600, 3000), env_exp(int(0.03 * SR), 0.006), 0.7)
    return mix(body, knock)


def shield_break(rng):
    # Glassy shatter: bright noise bursts + scattered high bell partials.
    out = apply(highpass(noise(0.5, rng), 3000), env_exp(int(0.5 * SR), 0.09), 0.6)
    for _ in range(9):
        f = rng.uniform(2500, 6500)
        shard = bell(f, 0.35, 0.08, ((1, 1.0), (1.51, 0.4), (2.37, 0.2)))
        out = mix(out, offset(apply(shard, [0.35] * len(shard)), rng.uniform(0.0, 0.12)))
    return out


def photo(rng):
    # Camera: shutter click-clack + tiny film-advance whir.
    def click(gain):
        return apply(bandpass(noise(0.02, rng), 1500, 7000), env_exp(int(0.02 * SR), 0.004), gain)
    whir_t = mix(apply(tone(900, 0.16, "saw", freq_end=1300), [0.12] * int(0.16 * SR)),
                 apply(lowpass(noise(0.16, rng), 3000), [0.15] * int(0.16 * SR)))
    whir = apply(lowpass(whir_t, 3500), env_adsr(len(whir_t), 0.01, 0.02, 0.8, 0.05))
    return mix(click(1.0), offset(click(0.8), 0.055), offset(whir, 0.1))


def pickup(rng):
    # Bright two-note arpeggio (E6 -> B6) with a sparkle.
    a = note(midi(88), 0.09, "square", decay=0.05, gain=0.5)
    b = note(midi(95), 0.22, "square", decay=0.09, gain=0.5)
    sparkle = bell(midi(100), 0.25, 0.06)
    return lowpass(mix(a, offset(b, 0.07), offset(apply(sparkle, [0.3] * len(sparkle)), 0.07)), 7000)


def use_item(rng):
    # Whoosh: noise through a sweeping low-pass with a swell envelope.
    n = noise(0.38, rng)
    sweep = lowpass(n, 400, 5000)
    e = [math.sin(math.pi * i / len(sweep)) ** 2 for i in range(len(sweep))]
    rise = apply(tone(300, 0.38, "sine", freq_end=900), [0.15] * len(sweep))
    return mix(apply(sweep, e), apply(rise, e))


def booking(rng):
    # Airport PA chime: ding-dong-ding (C6, A5, F5... friendly major third steps).
    notes = [(midi(84), 0.0), (midi(81), 0.22), (midi(77), 0.44)]
    out = silence(1.2)
    for f, t in notes:
        out = mix(out, offset(bell(f, 0.75, 0.35), t))
    return out


def phone_open(rng):
    # Soft UI slide-in: airy upward swoosh + gentle tap.
    sw = lowpass(noise(0.18, rng), 1500, 6000)
    e = [math.sin(math.pi * i / len(sw)) for i in range(len(sw))]
    tap = note(midi(84), 0.08, "sine", decay=0.03, gain=0.6)
    return mix(apply(sw, e, 0.5), offset(tap, 0.12))


def mash(rng):
    # Very short keyboard/pen tick (~40 ms), slightly randomized pitch is done per id below.
    t = apply(bandpass(noise(0.04, rng), 1800, 6000), env_exp(int(0.04 * SR), 0.006))
    blip = apply(tone(1700, 0.04), env_exp(int(0.04 * SR), 0.008), 0.4)
    return mix(t, blip)


def stamp(rng):
    # Punchy rubber stamp: low thump + paper slap.
    n = int(0.3 * SR)
    thump = apply(tone(120, 0.3, "sine", freq_end=45), env_exp(n, 0.07))
    slap = apply(lowpass(noise(0.06, rng), 3500), env_exp(int(0.06 * SR), 0.012), 0.8)
    knock = apply(tone(260, 0.1), env_exp(int(0.1 * SR), 0.02), 0.4)
    return mix(thump, slap, knock)


def rejected(rng):
    # Low buzzer "eh-eh": two short square buzzes with a slight detune.
    def buzz():
        t = mix(tone(150, 0.17, "square"), apply(tone(153, 0.17, "saw"), [0.6] * int(0.17 * SR)))
        return apply(lowpass(t, 1800), env_adsr(len(t), 0.005, 0.02, 0.8, 0.03))
    return concat(buzz(), silence(0.06), buzz())


def buy(rng):
    # Ka-ching: register clunk + bright coin bell pair.
    clunk = apply(bandpass(noise(0.05, rng), 300, 2500), env_exp(int(0.05 * SR), 0.012))
    ring1 = bell(midi(96), 0.6, 0.18, ((1, 1.0), (2.76, 0.4), (5.4, 0.2)))
    ring2 = bell(midi(100), 0.6, 0.2, ((1, 1.0), (2.76, 0.4), (5.4, 0.2)))
    return mix(clunk, offset(apply(ring1, [0.6] * len(ring1)), 0.05), offset(apply(ring2, [0.6] * len(ring2)), 0.12))


def takeoff(rng):
    # Jet engine spool-up: rumble + rising whine + air noise opening up (~2.2 s).
    dur = 2.2
    n = int(dur * SR)
    rumble = lowpass(noise(dur, rng), 120, 400)
    air = lowpass(noise(dur, rng), 600, 6000)
    whine = mix(tone(500, dur, "sine", freq_end=1600), apply(tone(750, dur, "sine", freq_end=2400), [0.4] * n))
    swell = [min(1.0, (i / n) * 1.6) for i in range(n)]
    tail = env_adsr(n, 0.3, 0.1, 1.0, 0.5)
    out = mix(apply(rumble, swell, 1.6), apply(air, swell, 0.5), apply(whine, swell, 0.08))
    return apply(out, tail)


def flight_missed(rng):
    # Sad descending "wah-wah-wah-waaah" (muted brassy saw with vibrato on the last note).
    out = []
    for i, m in enumerate([67, 66, 65]):
        t = tone(midi(m), 0.3, "saw")
        out.extend(apply(lowpass(t, 1400), env_adsr(len(t), 0.02, 0.05, 0.75, 0.06)))
    last = tone(midi(64), 0.9, "saw", vibrato=0.012, vib_rate=5.0)
    out.extend(apply(lowpass(last, 1200, 500), env_adsr(len(last), 0.02, 0.1, 0.7, 0.35)))
    return out


def visa_overstay(rng):
    # Two-tone alarm (hi-lo-hi-lo), slightly harsh but short (~0.8 s).
    out = []
    for f in (988, 740, 988, 740):
        t = tone(f, 0.2, "square")
        out.extend(apply(lowpass(t, 3000), env_adsr(len(t), 0.004, 0.02, 0.8, 0.02), 0.8))
    return out


def win(rng):
    # Short fanfare: C-E-G arpeggio then a held C chord with sparkle.
    out = silence(1.4)
    for i, m in enumerate([72, 76, 79]):
        out = mix(out, offset(note(midi(m), 0.16, "square", decay=0.12, gain=0.35), i * 0.12))
    chord = mix(*(note(midi(m), 0.9, "saw", a=0.01, r=0.4, gain=0.2) for m in (72, 76, 79, 84)))
    out = mix(out, offset(lowpass(chord, 3500), 0.36))
    out = mix(out, offset(apply(bell(midi(96), 0.6, 0.15), [0.25] * int(0.6 * SR)), 0.36))
    return out


SOUNDS = {
    "jump": jump,
    "hit": hit,
    "block": block,
    "shield_break": shield_break,
    "photo": photo,
    "pickup": pickup,
    "use_item": use_item,
    "booking": booking,
    "phone_open": phone_open,
    "mash": mash,
    "stamp": stamp,
    "rejected": rejected,
    "buy": buy,
    "takeoff": takeoff,
    "flight_missed": flight_missed,
    "visa_overstay": visa_overstay,
    "win": win,
}

# Per-sound loudness (dBFS peak) so short UI ticks don't out-shout big moments.
PEAKS = {"mash": -9.0, "phone_open": -8.0, "takeoff": -4.0, "jump": -9.0, "rejected": -7.0, "visa_overstay": -8.0,
         "flight_missed": -5.0}


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in SOUNDS.items():
        rng = random.Random(sum(map(ord, name)))  # deterministic per sound
        write(name, fn(rng), PEAKS.get(name, -3.0))


if __name__ == "__main__":
    main()
