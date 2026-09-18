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
    om.reset_scene()
    body = om.material("Body", BODY); tyre = om.material("Tyre", DARK); steel = om.material("Steel", STEEL)
    om.box("Chassis", (2.2, 1.1, 0.4), (0.0, 0.0, 0.58), body)
    om.box("Cab", (0.8, 1.05, 0.7), (0.65, 0.0, 1.1), om.team())
    om.box("Rack", (1.2, 1.0, 0.7), (-0.4, 0.0, 1.1), steel)
    for x in (0.7, -0.7):
        for side in (1, -1):
            om.cylinder(f"Wheel{x}{side}", 0.36, 0.3, (x, side * 0.62, 0.36), tyre, verts=14, rot=(90, 0, 0), bevel=0.03)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.cylinder("Mast", 0.06, 1.6, (-0.4, 0.0, 2.2), steel, verts=6, bevel=0.0)
    om.box("Dish", (0.9, 0.12, 0.7), (-0.4, 0.0, 3.0), steel, bevel=0.02)
    om.box("Emitter", (0.5, 0.5, 0.15), (-0.4, 0.0, 1.5), om.team(), bevel=0.02)
    t = om.join("Turret", om.parts_since(snap), origin=(-0.4, 0.0, 1.45)); om.parent(t, hull)
    export("coalition", "jammer")


def hive():
    om.reset_scene()
    body = om.material("Body", BODY); track = om.material("Track", DARK); steel = om.material("Steel", STEEL)
    om.box("Hull", (2.3, 1.3, 0.6), (0.0, 0.0, 0.6), body)
    for side in (1, -1):
        om.box(f"Track{side}", (2.4, 0.36, 0.42), (0.0, side * 0.75, 0.3), track, bevel=0.04)
    om.box("Bay", (1.6, 1.1, 0.5), (-0.2, 0.0, 1.15), om.team())
    for i, x in enumerate((-0.6, -0.2, 0.2, 0.6)):
        om.box(f"Cell{i}", (0.3, 0.3, 0.12), (x, 0.0, 1.46), steel, bevel=0.02)
    om.box("Cabin", (0.5, 1.1, 0.5), (0.85, 0.0, 1.15), body)
    om.join_all("coalition_hive")
    export("coalition", "hive")


def drone():
    om.reset_scene()
    steel = om.material("Steel", STEEL)
    om.box("Body", (0.5, 0.2, 0.12), (0.0, 0.0, 0.2), om.team(), bevel=0.02)
    om.box("Wing", (0.15, 0.9, 0.03), (0.0, 0.0, 0.22), steel, bevel=0.0)
    om.box("Tail", (0.12, 0.3, 0.03), (-0.25, 0.0, 0.25), steel, bevel=0.0)
    om.join_all("drone")
    export("coalition", "drone")


def vantage():
    om.reset_scene()
    body = om.material("Body", (0.55, 0.58, 0.60)); glass = om.material("Glass", GLASS, roughness=0.2); dark = om.material("Dark", DARK)
    om.wedge("Fuselage", (3.0, 0.7, 0.6), (0.2, 0.0, 0.8), body)
    om.box("Canopy", (0.8, 0.5, 0.3), (0.9, 0.0, 1.05), glass, bevel=0.03)
    om.box("Wing", (1.0, 3.4, 0.08), (-0.2, 0.0, 0.75), om.team(), bevel=0.02)
    om.box("Tail", (0.6, 1.4, 0.06), (-1.5, 0.0, 0.85), body, bevel=0.02)
    om.box("Fin", (0.6, 0.06, 0.7), (-1.5, 0.0, 1.2), om.team(), bevel=0.01)
    om.cylinder("Engine", 0.22, 0.9, (-1.4, 0.0, 0.8), dark, verts=10, rot=(0, 90, 0))
    for side in (1, -1):
        om.cylinder("Missile", 0.07, 0.8, (-0.1, side * 1.1, 0.62), dark, verts=8, rot=(0, 90, 0), bevel=0.0)
    om.join_all("coalition_vantage")
    export("coalition", "vantage")


def spectre():
    om.reset_scene()
    body = om.material("Body", (0.16, 0.17, 0.19)); dark = om.material("Dark", DARK)
    om.wedge("Body", (2.4, 1.0, 0.45), (0.3, 0.0, 0.8), body)
    om.box("WingL", (1.6, 2.4, 0.1), (-0.5, 1.6, 0.8), body, bevel=0.02, rot=(0, 0, 25))
    om.box("WingR", (1.6, 2.4, 0.1), (-0.5, -1.6, 0.8), body, bevel=0.02, rot=(0, 0, -25))
    om.box("Stripe", (0.6, 0.5, 0.47), (0.2, 0.0, 0.8), om.team(), bevel=0.02)
    om.cylinder("Intake", 0.16, 0.6, (-0.3, 0.55, 1.02), dark, verts=8, rot=(0, 90, 0))
    om.cylinder("Intake2", 0.16, 0.6, (-0.3, -0.55, 1.02), dark, verts=8, rot=(0, 90, 0))
    om.join_all("coalition_spectre")
    export("coalition", "spectre")


def hole():
    om.reset_scene()
    dirt = om.material("Dirt", (0.32, 0.24, 0.16)); dark = om.material("Dark", (0.08, 0.06, 0.05))
    om.cylinder("Rim", 0.55, 0.18, (0.0, 0.0, 0.09), dirt, verts=12, bevel=0.03)
    om.cylinder("Pit", 0.38, 0.1, (0.0, 0.0, 0.16), dark, verts=12, bevel=0.0)
    om.box("Plank", (0.9, 0.12, 0.05), (0.1, 0.3, 0.2), om.material("Wood", (0.45, 0.32, 0.18)), bevel=0.0, rot=(0, 0, 20))
    om.join_all("hole")
    export("neutral", "hole")


def civilian_house():
    om.reset_scene()
    wall = om.material("Wall", (0.78, 0.72, 0.62)); roof = om.material("Roof", (0.55, 0.30, 0.22)); dark = om.material("Dark", DARK)
    om.box("Walls", (2.4, 2.2, 1.4), (0.0, 0.0, 0.7), wall)
    om.wedge("Roof", (2.6, 2.4, 0.7), (0.0, 0.0, 1.75), roof, bevel=0.02)
    om.box("Door", (0.05, 0.5, 0.9), (1.22, -0.5, 0.45), dark, bevel=0.0)
    for y in (0.5,):
        om.box("Window", (0.05, 0.5, 0.4), (1.22, y, 0.85), om.material("Glass", GLASS, roughness=0.2), bevel=0.0)
    om.box("Chimney", (0.3, 0.3, 0.6), (-0.7, 0.6, 2.1), wall, bevel=0.01)
    om.join_all("civilian_house")
    export("neutral", "civilian_house")


def civilian_block():
    om.reset_scene()
    wall = om.material("Wall", (0.66, 0.64, 0.60)); dark = om.material("Dark", DARK); glass = om.material("Glass", GLASS, roughness=0.2)
    om.box("Block", (3.4, 3.2, 3.2), (0.0, 0.0, 1.6), wall)
    om.box("Roof", (3.5, 3.3, 0.15), (0.0, 0.0, 3.25), om.material("Roof", (0.35, 0.35, 0.36)), bevel=0.01)
    for z in (0.9, 1.7, 2.5):
        for y in (-1.0, 0.0, 1.0):
            om.box("Win", (0.05, 0.5, 0.45), (1.72, y, z), glass, bevel=0.0)
            om.box("Win2", (0.5, 0.05, 0.45), (y, 1.62, z), glass, bevel=0.0)
    om.box("Door", (0.05, 0.8, 1.0), (1.72, 0.0, 0.5), dark, bevel=0.0)
    om.join_all("civilian_block")
    export("neutral", "civilian_block")


def oil_derrick():
    om.reset_scene()
    steel = om.material("Steel", (0.30, 0.30, 0.32)); rust = om.material("Rust", (0.50, 0.30, 0.18)); tank = om.material("Tank", (0.55, 0.55, 0.50))
    om.box("Base", (2.6, 2.6, 0.2), (0.0, 0.0, 0.1), om.material("Concrete", (0.5, 0.49, 0.46)), bevel=0.02)
    om.box("Post", (0.25, 0.25, 2.2), (0.3, 0.0, 1.3), steel, bevel=0.02)
    om.box("Beam", (2.0, 0.2, 0.2), (0.0, 0.0, 2.4), rust, bevel=0.02, rot=(0, 12, 0))
    om.box("Head", (0.5, 0.3, 0.5), (0.95, 0.0, 2.2), rust, bevel=0.03)
    om.cylinder("Tank", 0.55, 1.2, (-0.8, 0.8, 0.8), tank, verts=12)
    om.cylinder("Pipe", 0.08, 1.6, (-0.4, -0.8, 0.3), steel, verts=6, rot=(0, 90, 0), bevel=0.0)
    om.join_all("oil_derrick")
    export("neutral", "oil_derrick")


MODELS = {"jammer": jammer, "hive": hive, "drone": drone, "vantage": vantage, "spectre": spectre,
          "hole": hole, "civilian_house": civilian_house, "civilian_block": civilian_block, "oil_derrick": oil_derrick}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for n in argv or list(MODELS):
        MODELS[n]()
        print(f"[models] built {n}")
