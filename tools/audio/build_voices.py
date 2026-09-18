"""Unit voice lines and the announcer, synthesised with espeak-ng and pushed through a radio filter.

Needs espeak-ng (`brew install espeak-ng`). Output speech carries no licence restrictions.

    python3 tools/audio/build_voices.py
"""
import os
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dsp import bandpass, clip, concat, env_exp, gain, mix, noise, read_wav, resample, silence, write_wav  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "audio", "voice")
SR = 11025

# espeak-ng voice, pitch (0-99), speed (wpm), radio grit
FACTIONS = {
    "coalition": dict(voice="en-us+m3", pitch=42, speed=178, drive=2.2),
    "directorate": dict(voice="en+m7", pitch=26, speed=152, drive=2.8),
    "network": dict(voice="en-gb+m1", pitch=55, speed=188, drive=3.4),
}
ANNOUNCER = dict(voice="en-us+f3", pitch=52, speed=165, drive=1.6)

LINES = {
    "coalition": {
        "infantry": {"select": ["Squad ready.", "Go ahead, command.", "Standing by."], "move": ["Moving out.", "On our way.", "Copy that."], "attack": ["Engaging.", "Weapons free.", "Contact. Firing."]},
        "vehicle": {"select": ["Armour ready.", "Systems green.", "Awaiting orders."], "move": ["Rolling.", "En route.", "Affirmative."], "attack": ["Target acquired.", "Firing solution locked.", "Sending it."]},
        "aircraft": {"select": ["On station.", "Altitude steady.", "Go for tasking."], "move": ["Vectoring.", "New heading.", "Wilco."], "attack": ["Tally target.", "Rifle.", "Cleared hot."]},
        "builder": {"select": ["Dozer here.", "Blueprints loaded.", "What are we building?"], "move": ["Relocating.", "Moving the rig.", "Okay."], "attack": ["I only build.", "Not my department.", "Seriously?"]},
        "harvester": {"select": ["Cargo flight ready.", "Hold is empty.", "Logistics here."], "move": ["Lifting off.", "Supply run.", "On the way."], "attack": ["We are unarmed.", "Negative, cargo only.", "Not happening."]},
        "heavy": {"select": ["Drones are hungry.", "Swarm online.", "Electronic warfare ready."], "move": ["Repositioning.", "Moving the package.", "Copy."], "attack": ["Release the swarm.", "Jamming now.", "Let them try."]},
    },
    "directorate": {
        "infantry": {"select": ["For the Directorate.", "Conscripts ready.", "We are many."], "move": ["We march.", "As ordered.", "Forward."], "attack": ["Overwhelm them.", "Charge.", "They will break."]},
        "vehicle": {"select": ["Steel is ready.", "Tank crew here.", "Orders."], "move": ["Advancing.", "Tracks turning.", "It is done."], "attack": ["Crush them.", "Fire the main gun.", "No mercy."]},
        "aircraft": {"select": ["Bomber ready.", "Payload armed.", "Awaiting target."], "move": ["Changing course.", "Flying.", "Understood."], "attack": ["Bombs away.", "Beginning the run.", "They will burn."]},
        "builder": {"select": ["Engineers ready.", "The plan is approved.", "We build for the state."], "move": ["Moving.", "Yes, director.", "At once."], "attack": ["We are engineers.", "That is not our task.", "No."]},
        "harvester": {"select": ["Supply truck.", "The quota must be met.", "Ready to haul."], "move": ["Driving.", "On the road.", "Yes."], "attack": ["This is a truck.", "We carry crates.", "Impossible."]},
        "heavy": {"select": ["Colossus stands ready.", "Nothing stops us.", "The ground shakes."], "move": ["Slow and certain.", "Forward, always.", "Rolling over."], "attack": ["Flatten them.", "Both barrels.", "Make rubble."]},
    },
    "network": {
        "infantry": {"select": ["We are everywhere.", "Ready, brother.", "Speak."], "move": ["Quietly.", "Through the back streets.", "Going."], "attack": ["Ambush!", "Take them!", "For the cause!"]},
        "vehicle": {"select": ["It still runs.", "Bolted it together myself.", "What do you need?"], "move": ["Flooring it.", "Hold on.", "Driving."], "attack": ["Light them up!", "They have nice parts.", "Shoot!"]},
        "aircraft": {"select": ["Drone is up.", "Signal is good.", "Watching."], "move": ["Flying low.", "Moving the drone.", "Okay."], "attack": ["Diving!", "One way trip.", "Say goodbye."]},
        "builder": {"select": ["Work, work.", "I dig, I build.", "Yes?"], "move": ["Walking.", "I go.", "Fine."], "attack": ["With a shovel?", "I am a worker.", "No, no."]},
        "harvester": {"select": ["Work, work.", "I carry.", "Yes?"], "move": ["Walking.", "I go.", "Fine."], "attack": ["With a shovel?", "I am a worker.", "No, no."]},
        "heavy": {"select": ["Handle with care.", "It is very full.", "Do not bump me."], "move": ["Driving gently.", "Nice and slow.", "On my way."], "attack": ["This is my stop.", "Special delivery.", "Goodbye, everyone."]},
    },
}

ANNOUNCE = {
    "construction_complete": "Construction complete.", "unit_ready": "Unit ready.", "upgrade_complete": "Upgrade complete.",
    "base_under_attack": "Our base is under attack.", "unit_under_attack": "Units under attack.", "insufficient_funds": "Insufficient funds.",
    "low_power": "Low power.", "cannot_build": "Cannot build there.", "building_lost": "Building lost.",
    "superweapon_ready": "Superweapon ready.", "superweapon_launch": "Warning. Superweapon launch detected.",
    "enemy_superweapon": "Warning. Enemy superweapon under construction.", "promotion": "Promotion available.", "power_ready": "General's power ready.",
    "building_captured": "Building captured.", "building_stolen": "We have lost a building to the enemy.", "player_eliminated": "Enemy commander eliminated.",
    "victory": "Victory. The field is yours.", "defeat": "You have been defeated.", "welcome": "Welcome back, commander.",
}


def speak(text, voice, pitch, speed):
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
        path = f.name
    subprocess.run(["espeak-ng", "-v", voice, "-p", str(pitch), "-s", str(speed), "-a", "180", "-w", path, text], check=True)
    samples, sr = read_wav(path)
    os.unlink(path)
    return resample(samples, sr, SR)


def radio(x, drive, squelch=True, seed=1):
    """Band-limit to a field radio, overdrive, and bracket with squelch clicks."""
    y = bandpass(x, 350, 3200, SR)
    y = clip(gain(y, 3.0), drive)
    hiss = gain(bandpass(noise(len(y) / SR, SR, seed), 800, 3500, SR), 0.035)
    y = mix(y, hiss)
    if not squelch:
        return y
    click = env_exp(bandpass(noise(0.05, SR, seed + 5), 900, 3500, SR), 0.012, sr=SR)
    tail = env_exp(bandpass(noise(0.09, SR, seed + 9), 700, 3000, SR), 0.03, sr=SR)
    return concat(gain(click, 0.5), silence(0.015, SR), y, gain(tail, 0.35))


def main():
    count = 0
    for faction, cfg in FACTIONS.items():
        os.makedirs(os.path.join(OUT, faction), exist_ok=True)
        for klass, events in LINES[faction].items():
            for event, lines in events.items():
                for i, text in enumerate(lines, 1):
                    x = speak(text, cfg["voice"], cfg["pitch"], cfg["speed"])
                    write_wav(os.path.join(OUT, faction, f"{klass}_{event}_{i}.wav"), radio(x, cfg["drive"], seed=count), SR)
                    count += 1
    os.makedirs(os.path.join(OUT, "announcer"), exist_ok=True)
    for key, text in ANNOUNCE.items():
        x = speak(text, ANNOUNCER["voice"], ANNOUNCER["pitch"], ANNOUNCER["speed"])
        write_wav(os.path.join(OUT, "announcer", f"{key}.wav"), radio(x, ANNOUNCER["drive"], squelch=False, seed=count), SR)
        count += 1
    print(f"[voices] wrote {count} lines to {OUT}")


if __name__ == "__main__":
    main()
