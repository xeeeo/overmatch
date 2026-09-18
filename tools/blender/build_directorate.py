"""Directorate art: angular heavy machinery and reinforced industrial buildings.

    blender -b -P tools/blender/build_directorate.py [-- name ...]

Regenerates only this faction; selectors and model paths are unchanged.
Authored geometry and local helpers live in directorate_design.py.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from directorate_design import UNITS, BUILDINGS

MODELS = {**BUILDINGS, **UNITS}
# Preserve the callable named builders as well as the command-line selectors.
globals().update(MODELS)

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for name in argv or list(MODELS):
        MODELS[name]()
        print(f"[models] built {name}")
