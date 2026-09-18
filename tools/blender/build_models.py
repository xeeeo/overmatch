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
    """Main battle tank. Low, wide, sloped front, big turret, long gun."""
    om.reset_scene()
    hull = om.material("Hull", (0.30, 0.34, 0.30))
    track = om.material("Track", DARK)
    steel = om.material("Steel", STEEL)
    om.wedge("Hull", (2.1, 1.25, 0.45), (0.0, 0.0, 0.55), hull)
    om.box("HullTop", (1.4, 1.0, 0.18), (-0.1, 0.0, 0.85), om.team(), bevel=0.02)
    for side in (1, -1):
        om.box(f"Track{side}", (2.3, 0.36, 0.42), (0.0, side * 0.72, 0.30), track, bevel=0.04)
        om.box(f"Skirt{side}", (2.0, 0.08, 0.25), (0.0, side * 0.92, 0.45), hull, bevel=0.01)
    om.cylinder("TurretBase", 0.55, 0.12, (-0.15, 0.0, 1.0), steel, verts=16)
    om.wedge("Turret", (1.1, 0.95, 0.38), (-0.2, 0.0, 1.25), om.team())
    om.cylinder("Gun", 0.07, 1.5, (0.75, 0.0, 1.27), steel, verts=10, rot=(0, 90, 0))
    om.cylinder("Muzzle", 0.10, 0.2, (1.42, 0.0, 1.27), steel, verts=10, rot=(0, 90, 0))
    om.box("Hatch", (0.3, 0.3, 0.08), (-0.45, 0.25, 1.47), steel, bevel=0.01)
    om.box("Sensor", (0.18, 0.18, 0.22), (-0.2, -0.3, 1.5), steel, bevel=0.01)
    om.join_all("coalition_bulwark")
    om.export_glb(os.path.join(OUT, "coalition", "bulwark.glb"))


def coalition_dozer():
    """Construction dozer. Boxy body, cab in team colour, big front blade."""
    om.reset_scene()
    body = om.material("Body", YELLOW)
    blade = om.material("Blade", (0.65, 0.48, 0.10))
    track = om.material("Track", DARK)
    glass = om.material("Glass", GLASS, roughness=0.2)
    om.box("Body", (1.6, 1.1, 0.55), (-0.15, 0.0, 0.55), body)
    om.box("Cab", (0.8, 0.85, 0.6), (-0.35, 0.0, 1.12), om.team())
    om.box("Window", (0.82, 0.6, 0.3), (-0.35, 0.0, 1.2), glass, bevel=0.0)
    om.box("Exhaust", (0.12, 0.12, 0.45), (0.3, 0.35, 1.0), om.material("Steel", STEEL), bevel=0.01)
    om.box("Blade", (0.15, 1.6, 0.7), (0.95, 0.0, 0.42), blade, bevel=0.02)
    for side in (1, -1):
        om.box(f"Arm{side}", (0.9, 0.1, 0.12), (0.5, side * 0.62, 0.45), blade, bevel=0.01)
        om.box(f"Track{side}", (1.7, 0.34, 0.40), (-0.1, side * 0.62, 0.28), track, bevel=0.04)
    om.join_all("coalition_dozer")
    om.export_glb(os.path.join(OUT, "coalition", "dozer.glb"))


MODELS = {
    "bulwark": coalition_bulwark,
    "dozer": coalition_dozer,
}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    names = argv or list(MODELS)
    for n in names:
        os.makedirs(os.path.join(OUT, "coalition"), exist_ok=True)
        MODELS[n]()
        print(f"[models] built {n}")
