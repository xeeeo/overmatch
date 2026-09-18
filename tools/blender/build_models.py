"""Generates every Overmatch model as GLB into game/assets/models/.

    blender -b -P tools/blender/build_models.py            # all
    blender -b -P tools/blender/build_models.py -- bulwark # one
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import omlib as om  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "models")

DARK = (0.10, 0.10, 0.11)
STEEL = (0.35, 0.37, 0.38)
YELLOW = (0.90, 0.68, 0.14)
GLASS = (0.55, 0.75, 0.85)


def coalition_bulwark():
    """Regenerate the detailed Bulwark without changing the other unit builders."""
    from build_bulwark import build
    build(os.path.join(OUT, "coalition", "bulwark.glb"))


def coalition_dozer():
    from coalition_units import MODELS as detailed
    detailed["dozer"]()


def coalition_warden():
    from coalition_units import MODELS as detailed
    detailed["warden"]()


def infantry(name, body_rgb, weapon):
    """Compatibility entry point; Coalition palette is now shared."""
    from coalition_units import soldier
    soldier(name, weapon)


def coalition_rifleman():
    from coalition_units import MODELS as detailed
    detailed["rifleman"]()


def coalition_rocket_trooper():
    from coalition_units import MODELS as detailed
    detailed["rocket_trooper"]()


def coalition_pathfinder():
    from coalition_units import MODELS as detailed
    detailed["pathfinder"]()


def coalition_lancer():
    from coalition_units import MODELS as detailed
    detailed["lancer"]()


def coalition_tiltrotor():
    from coalition_units import MODELS as detailed
    detailed["tiltrotor"]()


def coalition_kestrel():
    from coalition_units import MODELS as detailed
    detailed["kestrel"]()


MODELS = {
    "bulwark": coalition_bulwark,
    "dozer": coalition_dozer,
    "warden": coalition_warden,
    "rifleman": coalition_rifleman,
    "rocket_trooper": coalition_rocket_trooper,
    "pathfinder": coalition_pathfinder,
    "lancer": coalition_lancer,
    "tiltrotor": coalition_tiltrotor,
    "kestrel": coalition_kestrel,
}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    names = argv or list(MODELS)
    for n in names:
        os.makedirs(os.path.join(OUT, "coalition"), exist_ok=True)
        MODELS[n]()
        print(f"[models] built {n}")
