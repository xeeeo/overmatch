"""Coalition M4 additions and neutral map objects.

    blender -b -P tools/blender/build_extras.py [-- name ...]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import omlib as om  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "models")
DARK = (0.10, 0.10, 0.11)
STEEL = (0.35, 0.37, 0.38)
GLASS = (0.55, 0.75, 0.85)
BODY = (0.30, 0.34, 0.30)


def export(sub, name):
    om.export_glb(os.path.join(OUT, sub, f"{name}.glb"))


def jammer():
    from coalition_units import MODELS as detailed
    detailed["jammer"]()


def hive():
    from coalition_units import MODELS as detailed
    detailed["hive"]()


def drone():
    from coalition_units import MODELS as detailed
    detailed["drone"]()


def vantage():
    from coalition_units import MODELS as detailed
    detailed["vantage"]()


def spectre():
    from coalition_units import MODELS as detailed
    detailed["spectre"]()


def hole():
    from neutral_art import hole as build
    build()


def civilian_house():
    from neutral_art import civilian_house as build
    build()


def civilian_block():
    from neutral_art import civilian_block as build
    build()


def oil_derrick():
    from neutral_art import oil_derrick as build
    build()


MODELS = {"jammer": jammer, "hive": hive, "drone": drone, "vantage": vantage, "spectre": spectre,
          "hole": hole, "civilian_house": civilian_house, "civilian_block": civilian_block, "oil_derrick": oil_derrick}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for n in argv or list(MODELS):
        MODELS[n]()
        print(f"[models] built {n}")
