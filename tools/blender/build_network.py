"""Network: improvised, ragged, sand-coloured, tarps and sandbags. Origin = footprint centre (buildings) / ground under centre (units).

    blender -b -P tools/blender/build_network.py [-- name ...]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import omlib as om  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "models", "network")
DARK = (0.10, 0.10, 0.11)
STEEL = (0.40, 0.38, 0.34)
RUST = (0.48, 0.30, 0.18)
SAND = (0.72, 0.64, 0.48)
MUD = (0.55, 0.47, 0.34)
TARP = (0.36, 0.42, 0.30)
WOOD = (0.45, 0.32, 0.18)
GLASS = (0.55, 0.75, 0.85)


def M():
    return dict(dark=om.material("Dark", DARK), steel=om.material("Steel", STEEL), rust=om.material("Rust", RUST), sand=om.material("Sand", SAND),
                mud=om.material("Mud", MUD), tarp=om.material("Tarp", TARP), wood=om.material("Wood", WOOD), glass=om.material("Glass", GLASS, roughness=0.2),
                tyre=om.material("Tyre", DARK), track=om.material("Track", DARK), bag=om.material("Bag", (0.62, 0.56, 0.42)))


def export(name):
    om.export_glb(os.path.join(OUT, f"{name}.glb"))


def sandbags(m, w, h):
    for (x, y, sx, sy) in ((0, -h / 2 + 0.25, w - 0.6, 0.3), (0, h / 2 - 0.25, w - 0.6, 0.3), (-w / 2 + 0.25, 0, 0.3, h - 0.6), (w / 2 - 0.25, 0, 0.3, h - 0.6)):
        om.box("Bags", (sx, sy, 0.35), (x, y, 0.18), m["bag"], bevel=0.06)


def wheels(m, xs, half_width, r=0.34):
    for x in xs:
        for side in (1, -1):
            om.cylinder(f"Wheel{x}{side}", r, 0.28, (x, side * half_width, r), m["tyre"], verts=12, rot=(90, 0, 0), bevel=0.03)


# ---------------- buildings ----------------
def command_cell():
    om.reset_scene(); m = M(); sandbags(m, 5, 5)
    om.box("Main", (3.2, 2.8, 1.5), (0, 0, 0.75), m["sand"])
    om.box("Roof", (3.3, 2.9, 0.12), (0, 0, 1.55), m["mud"], bevel=0.01)
    om.box("Band", (3.22, 2.82, 0.2), (0, 0, 1.1), om.team(), bevel=0.01)
    om.box("Wing", (1.4, 1.6, 1.0), (-1.4, 1.4, 0.5), m["sand"])
    om.box("WingRoof", (1.6, 1.8, 0.08), (-1.4, 1.4, 1.04), m["tarp"], bevel=0.0)
    om.cylinder("Mast", 0.06, 2.6, (1.2, -1.2, 2.5), m["rust"], verts=6, bevel=0.0)
    om.box("Flag", (0.7, 0.03, 0.4), (0.85, -1.2, 3.5), om.team(), bevel=0.0)
    om.box("Dish", (0.12, 0.9, 0.7), (1.0, 1.0, 2.0), m["rust"], bevel=0.01)
    om.box("Door", (0.05, 0.8, 1.0), (1.62, -0.4, 0.5), m["dark"], bevel=0.0)
    om.join_all("command_cell"); export("command_cell")


def safehouse():
    om.reset_scene(); m = M()
    om.box("House", (2.2, 2.0, 1.3), (0, 0.2, 0.65), m["sand"])
    om.box("Roof", (2.4, 2.2, 0.1), (0, 0.2, 1.35), m["mud"], bevel=0.01)
    om.box("Tarp", (1.6, 1.4, 0.06), (0.4, -1.0, 1.1), m["tarp"], bevel=0.0, rot=(0, 8, 0))
    om.box("Pole", (0.06, 0.06, 1.1), (1.1, -1.6, 0.55), m["wood"], bevel=0.0)
    om.box("Pole2", (0.06, 0.06, 1.1), (-0.3, -1.6, 0.55), m["wood"], bevel=0.0)
    om.box("Door", (0.05, 0.6, 0.9), (1.12, -0.2, 0.45), m["dark"], bevel=0.0)
    om.box("Band", (2.22, 2.02, 0.15), (0, 0.2, 1.0), om.team(), bevel=0.01)
    om.join_all("safehouse"); export("safehouse")


def supply_stash():
    om.reset_scene(); m = M(); sandbags(m, 4, 4)
    om.box("Shed", (2.0, 2.8, 1.2), (-0.7, 0, 0.6), m["mud"])
    om.box("ShedRoof", (2.3, 3.0, 0.08), (-0.7, 0, 1.24), m["rust"], bevel=0.0, rot=(0, 0, 3))
    om.box("Band", (2.02, 2.82, 0.15), (-0.7, 0, 0.9), om.team(), bevel=0.01)
    for i, (x, y) in enumerate(((0.9, -0.9), (1.2, -0.2), (0.7, 0.6), (1.3, 1.0))):
        om.box(f"Crate{i}", (0.5, 0.5, 0.5), (x, y, 0.25), om.material("Crate", (0.55, 0.45, 0.25)), bevel=0.03, rot=(0, 0, i * 17))
    om.box("Tarp", (1.4, 1.2, 0.06), (1.0, 0.4, 0.75), m["tarp"], bevel=0.0, rot=(5, 0, 10))
    om.join_all("supply_stash"); export("supply_stash")


def arms_dealer():
    om.reset_scene(); m = M(); sandbags(m, 4, 4)
    om.box("Garage", (3.0, 2.6, 1.5), (0, 0.3, 0.75), m["sand"])
    om.box("Roof", (3.2, 2.8, 0.1), (0, 0.3, 1.55), m["rust"], bevel=0.01)
    om.box("Band", (3.02, 2.62, 0.18), (0, 0.3, 1.1), om.team(), bevel=0.01)
    om.box("Door", (2.0, 0.05, 1.2), (0, -1.02, 0.7), m["dark"], bevel=0.0)
    om.cylinder("Barrel", 0.3, 0.8, (1.2, 1.5, 0.4), m["rust"], verts=10)
    om.cylinder("Barrel2", 0.3, 0.8, (-1.3, 1.5, 0.4), m["dark"], verts=10)
    om.box("Crane", (0.1, 1.6, 0.1), (1.7, 0.2, 1.9), m["rust"], bevel=0.0)
    om.join_all("arms_dealer"); export("arms_dealer")


def tunnel_network():
    om.reset_scene(); m = M()
    om.box("Bags", (1.8, 1.8, 0.4), (0, 0, 0.2), m["bag"], bevel=0.06)
    om.cylinder("Mouth", 0.55, 0.5, (0, 0, 0.45), m["dark"], verts=12, bevel=0.0)
    om.cylinder("Rim", 0.65, 0.12, (0, 0, 0.6), m["mud"], verts=12, bevel=0.02)
    om.box("Beam", (1.4, 0.15, 0.15), (0, 0, 0.95), m["wood"], bevel=0.0)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.box("GunBox", (0.4, 0.3, 0.25), (0, 0, 1.05), m["rust"], bevel=0.02)
    om.cylinder("Gun", 0.05, 0.6, (0.4, 0, 1.08), m["dark"], verts=6, rot=(0, 90, 0), bevel=0.0)
    t = om.join("Turret", om.parts_since(snap), origin=(0, 0, 0.95)); om.parent(t, hull)
    export("tunnel_network")


def black_market():
    om.reset_scene(); m = M()
    om.box("Stall", (2.2, 2.0, 1.0), (0, 0.2, 0.5), m["mud"])
    om.box("Awning", (2.6, 1.0, 0.05), (0, -1.2, 1.15), om.team(), bevel=0.0, rot=(10, 0, 0))
    om.box("Roof", (2.4, 2.2, 0.08), (0, 0.2, 1.05), m["rust"], bevel=0.0)
    for x in (-0.9, 0.9):
        om.box(f"Pole{x}", (0.06, 0.06, 1.0), (x, -1.6, 0.5), m["wood"], bevel=0.0)
    om.box("Counter", (2.0, 0.3, 0.6), (0, -0.9, 0.3), m["wood"], bevel=0.02)
    om.box("Sign", (0.6, 0.03, 0.4), (0.6, 0.2 - 1.02, 0.95), om.material("Sign", (0.9, 0.7, 0.1)), bevel=0.0)
    om.cylinder("Dish", 0.4, 0.06, (-0.6, 0.6, 1.4), m["rust"], verts=12, rot=(30, 0, 0))
    om.join_all("black_market"); export("black_market")


def compound():
    om.reset_scene(); m = M()
    om.box("Wall", (3.4, 3.4, 1.2), (0, 0, 0.6), m["sand"])
    om.box("Inner", (2.4, 2.4, 1.6), (0, 0, 0.8), m["mud"])
    om.box("Band", (2.42, 2.42, 0.2), (0, 0, 1.3), om.team(), bevel=0.01)
    om.box("Roof", (2.6, 2.6, 0.1), (0, 0, 1.65), m["rust"], bevel=0.01)
    for x, y in ((1.5, 1.5), (-1.5, 1.5), (1.5, -1.5), (-1.5, -1.5)):
        om.box("Corner", (0.5, 0.5, 1.6), (x, y, 0.8), m["sand"], bevel=0.02)
    om.box("Gate", (0.05, 1.0, 1.0), (1.72, 0, 0.5), m["dark"], bevel=0.0)
    om.join_all("compound"); export("compound")


def stinger_site():
    om.reset_scene(); m = M()
    om.box("Bags", (1.8, 1.8, 0.45), (0, 0, 0.22), m["bag"], bevel=0.06)
    om.cylinder("Pit", 0.5, 0.2, (0, 0, 0.5), m["mud"], verts=12, bevel=0.0)
    om.box("Net", (1.9, 1.9, 0.04), (0, 0, 1.4), m["tarp"], bevel=0.0, rot=(0, 6, 0))
    for x, y in ((0.9, 0.9), (-0.9, -0.9)):
        om.box("Pole", (0.05, 0.05, 1.4), (x, y, 0.7), m["wood"], bevel=0.0)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.box("Mount", (0.4, 0.4, 0.3), (0, 0, 0.75), m["rust"], bevel=0.02)
    for side in (1, -1):
        om.cylinder(f"Tube{side}", 0.07, 0.9, (0.3, side * 0.14, 0.95), m["dark"], verts=8, rot=(0, 70, 0), bevel=0.0)
    t = om.join("Turret", om.parts_since(snap), origin=(0, 0, 0.6)); om.parent(t, hull)
    export("stinger_site")


def drone_workshop():
    om.reset_scene(); m = M()
    om.box("Shed", (2.2, 2.0, 1.1), (0, 0.2, 0.55), m["mud"])
    om.box("Roof", (2.4, 2.2, 0.08), (0, 0.2, 1.15), m["rust"], bevel=0.0)
    om.box("Band", (2.22, 2.02, 0.15), (0, 0.2, 0.85), om.team(), bevel=0.01)
    om.box("Bench", (1.6, 0.5, 0.5), (0, -1.1, 0.25), m["wood"], bevel=0.02)
    for i, x in enumerate((-0.5, 0.0, 0.5)):
        om.box(f"Drone{i}", (0.35, 0.35, 0.06), (x, -1.1, 0.55), m["dark"], bevel=0.0)
    om.cylinder("Antenna", 0.03, 1.6, (0.8, 0.9, 1.9), m["dark"], verts=6, bevel=0.0)
    om.join_all("drone_workshop"); export("drone_workshop")


def launch_site():
    om.reset_scene(); m = M(); sandbags(m, 4, 4)
    om.box("Pad", (2.6, 2.6, 0.25), (0, 0, 0.12), m["mud"], bevel=0.02)
    om.box("Rack", (1.6, 1.6, 0.4), (0, 0, 0.45), m["rust"], bevel=0.03, rot=(0, -20, 0))
    for y in (-0.5, 0.0, 0.5):
        for z in (0.45, 0.85, 1.25):
            om.cylinder("Rocket", 0.12, 1.6, (0.3, y, z), m["steel"], verts=8, rot=(0, 70, 0), bevel=0.0)
    om.box("Band", (1.62, 1.62, 0.12), (0, 0, 0.5), om.team(), bevel=0.01, rot=(0, -20, 0))
    om.box("Hut", (0.9, 0.9, 0.9), (-1.2, -1.2, 0.45), m["sand"])
    om.join_all("launch_site"); export("launch_site")


# ---------------- units ----------------
def infantry(name, kit_rgb, weapon, scarf=True):
    om.reset_scene()
    skin = om.material("Skin", (0.72, 0.55, 0.42)); boots = om.material("Boots", DARK); kit = om.material("Kit", kit_rgb); steel = om.material("Steel", STEEL)
    for side in (1, -1):
        om.box(f"Leg{side}", (0.22, 0.2, 0.55), (0.0, side * 0.13, 0.28), kit, bevel=0.02)
        om.box(f"Boot{side}", (0.3, 0.22, 0.1), (0.04, side * 0.13, 0.05), boots, bevel=0.01)
    om.box("Torso", (0.34, 0.5, 0.5), (0.0, 0.0, 0.82), kit, bevel=0.03)
    om.box("Sash", (0.36, 0.14, 0.52), (0.0, 0.1, 0.82), om.team(), bevel=0.02)
    om.box("Head", (0.24, 0.24, 0.22), (0.0, 0.0, 1.2), skin, bevel=0.03)
    if scarf:
        om.box("Scarf", (0.28, 0.28, 0.14), (0.0, 0.0, 1.3), om.team(), bevel=0.03)
    for side in (1, -1):
        om.box(f"Arm{side}", (0.5, 0.14, 0.14), (0.2, side * 0.3, 0.9), kit, bevel=0.02)
    if weapon == "rifle":
        om.box("Rifle", (0.7, 0.07, 0.1), (0.4, 0.15, 0.92), steel, bevel=0.01)
    elif weapon == "rocket":
        om.cylinder("Tube", 0.08, 1.0, (0.2, 0.32, 1.05), steel, verts=8, rot=(0, 90, 0), bevel=0.0)
    elif weapon == "charge":
        om.box("Pack", (0.2, 0.4, 0.4), (-0.25, 0.0, 0.85), om.material("Warn", (0.9, 0.7, 0.1)), bevel=0.02)
    elif weapon == "controller":
        om.box("Ctrl", (0.3, 0.35, 0.06), (0.4, 0.0, 0.95), steel, bevel=0.0)
        om.cylinder("Ant", 0.02, 0.5, (0.3, 0.15, 1.2), steel, verts=6, bevel=0.0)
    elif weapon == "shovel":
        om.box("Shovel", (0.06, 0.06, 1.0), (0.3, 0.3, 0.7), om.material("Wood", WOOD), bevel=0.0)
        om.box("Blade", (0.2, 0.05, 0.25), (0.3, 0.3, 0.2), steel, bevel=0.0)
    om.join_all(name); export(name)


def worker(): infantry("worker", (0.52, 0.46, 0.34), "shovel", scarf=False)
def rebel(): infantry("rebel", (0.42, 0.38, 0.28), "rifle")
def rpg_trooper(): infantry("rpg_trooper", (0.40, 0.36, 0.26), "rocket")
def saboteur(): infantry("saboteur", (0.22, 0.22, 0.22), "charge")
def fpv_operator(): infantry("fpv_operator", (0.38, 0.40, 0.30), "controller")


def angry_mob():
    om.reset_scene()
    skin = om.material("Skin", (0.72, 0.55, 0.42)); steel = om.material("Steel", STEEL)
    cols = [(0.5, 0.4, 0.3), (0.35, 0.4, 0.5), (0.55, 0.5, 0.3), (0.3, 0.35, 0.3), (0.6, 0.3, 0.3)]
    for i, (dx, dy) in enumerate(((0, 0), (0.6, 0.4), (-0.5, 0.5), (0.5, -0.5), (-0.6, -0.4))):
        kit = om.material(f"Kit{i}", cols[i])
        om.box(f"Body{i}", (0.3, 0.42, 0.9), (dx, dy, 0.55), kit if i else om.team(), bevel=0.03)
        om.box(f"Head{i}", (0.22, 0.22, 0.22), (dx, dy, 1.12), skin, bevel=0.03)
        om.box(f"Arm{i}", (0.5, 0.12, 0.12), (dx + 0.25, dy + 0.25, 0.9), kit, bevel=0.02, rot=(0, -30 + i * 10, 0))
    om.box("Rifle", (0.7, 0.07, 0.1), (0.9, 0.5, 1.0), steel, bevel=0.01)
    om.box("Bottle", (0.08, 0.08, 0.3), (-0.4, 0.75, 1.25), om.material("Glass", GLASS, roughness=0.2), bevel=0.0)
    om.join_all("angry_mob"); export("angry_mob")


def technical():
    om.reset_scene(); m = M()
    om.box("Chassis", (2.2, 1.0, 0.3), (0, 0, 0.5), m["rust"])
    om.box("Cab", (0.8, 1.0, 0.7), (0.6, 0, 1.0), om.team())
    om.box("Windscreen", (0.06, 0.8, 0.3), (1.02, 0, 1.1), m["glass"], bevel=0.0)
    om.box("Bed", (1.1, 1.0, 0.35), (-0.5, 0, 0.8), m["sand"], bevel=0.02)
    wheels(m, (0.7, -0.7), 0.55)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.cylinder("Post", 0.05, 0.5, (-0.5, 0, 1.2), m["steel"], verts=6, bevel=0.0)
    om.box("GunBody", (0.5, 0.15, 0.15), (-0.3, 0, 1.45), m["dark"], bevel=0.02)
    om.cylinder("Barrel", 0.04, 0.6, (0.2, 0, 1.45), m["dark"], verts=6, rot=(0, 90, 0), bevel=0.0)
    t = om.join("Turret", om.parts_since(snap), origin=(-0.5, 0, 1.0)); om.parent(t, hull)
    export("technical")


def raider_quad():
    om.reset_scene(); m = M()
    om.box("Body", (1.2, 0.6, 0.3), (0, 0, 0.45), m["rust"])
    om.box("Seat", (0.5, 0.4, 0.2), (-0.2, 0, 0.7), om.team(), bevel=0.03)
    om.box("Rider", (0.3, 0.3, 0.6), (-0.1, 0, 1.05), om.material("Kit", (0.4, 0.36, 0.28)), bevel=0.03)
    om.box("Head", (0.2, 0.2, 0.2), (-0.1, 0, 1.45), om.material("Skin", (0.72, 0.55, 0.42)), bevel=0.03)
    om.box("Bars", (0.1, 0.7, 0.05), (0.5, 0, 0.85), m["steel"], bevel=0.0)
    om.box("Gun", (0.7, 0.06, 0.08), (0.7, 0.2, 0.85), m["dark"], bevel=0.0)
    wheels(m, (0.45, -0.45), 0.42, r=0.26)
    om.join_all("raider_quad"); export("raider_quad")


def marauder():
    om.reset_scene(); m = M()
    om.box("Hull", (2.0, 1.2, 0.5), (0, 0, 0.55), m["rust"])
    om.box("Plate", (0.9, 1.25, 0.55), (0.4, 0, 0.6), m["steel"], bevel=0.02)
    om.box("Band", (1.2, 1.22, 0.15), (-0.3, 0, 0.75), om.team(), bevel=0.01)
    for side in (1, -1):
        om.box(f"Track{side}", (2.2, 0.36, 0.42), (0, side * 0.7, 0.3), m["track"], bevel=0.04)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.box("Turret", (0.9, 0.8, 0.4), (-0.1, 0, 1.05), m["rust"], bevel=0.03)
    om.cylinder("Gun", 0.07, 1.2, (0.7, 0, 1.08), m["dark"], verts=8, rot=(0, 90, 0))
    om.box("Scrap", (0.3, 0.4, 0.2), (-0.4, 0.2, 1.3), m["steel"], bevel=0.02, rot=(0, 0, 20))
    t = om.join("Turret", om.parts_since(snap), origin=(-0.1, 0, 0.85)); om.parent(t, hull)
    export("marauder")


def sprayer():
    om.reset_scene(); m = M()
    om.box("Body", (1.6, 1.1, 0.6), (-0.2, 0, 0.7), om.material("Green", (0.30, 0.45, 0.25)))
    om.box("Cab", (0.6, 0.9, 0.6), (0.5, 0, 1.2), om.team())
    om.cylinder("Tank", 0.5, 1.0, (-0.6, 0, 1.25), om.material("Warn", (0.75, 0.75, 0.2)), verts=12, rot=(0, 90, 0))
    om.box("Boom", (0.08, 2.4, 0.08), (-1.0, 0, 0.7), m["steel"], bevel=0.0)
    for y in (-1.0, -0.5, 0.5, 1.0):
        om.cylinder(f"Nozzle{y}", 0.04, 0.2, (-1.0, y, 0.6), m["dark"], verts=6, bevel=0.0)
    for side in (1, -1):
        om.box(f"Track{side}", (1.8, 0.34, 0.4), (-0.1, side * 0.65, 0.28), m["track"], bevel=0.04)
    om.join_all("sprayer"); export("sprayer")


def rocket_buggy():
    om.reset_scene(); m = M()
    om.box("Frame", (1.8, 0.9, 0.25), (0, 0, 0.5), m["rust"])
    om.box("Cage", (0.8, 0.8, 0.5), (0.4, 0, 0.9), m["steel"], bevel=0.02)
    om.box("Seat", (0.4, 0.5, 0.3), (0.4, 0, 0.8), om.team(), bevel=0.02)
    wheels(m, (0.65, -0.65), 0.55, r=0.32)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.box("Pod", (0.7, 0.8, 0.5), (-0.5, 0, 1.0), m["dark"], bevel=0.03, rot=(0, -25, 0))
    for y in (-0.25, 0.0, 0.25):
        for z in (0.9, 1.15):
            om.cylinder("Tube", 0.07, 0.15, (-0.15, y, z), m["rust"], verts=6, rot=(0, 65, 0), bevel=0.0)
    t = om.join("Turret", om.parts_since(snap), origin=(-0.5, 0, 0.7)); om.parent(t, hull)
    export("rocket_buggy")


def bomb_truck():
    om.reset_scene(); m = M()
    om.box("Chassis", (2.4, 1.1, 0.3), (0, 0, 0.5), m["rust"])
    om.box("Cab", (0.8, 1.05, 0.8), (0.9, 0, 1.05), om.team())
    om.box("Windscreen", (0.06, 0.85, 0.35), (1.32, 0, 1.15), m["glass"], bevel=0.0)
    om.box("Load", (1.4, 1.1, 0.9), (-0.4, 0, 1.1), m["sand"], bevel=0.03)
    om.box("Tarp", (1.5, 1.2, 0.1), (-0.4, 0, 1.6), m["tarp"], bevel=0.03)
    for i in range(3):
        om.cylinder(f"Barrel{i}", 0.2, 0.5, (-0.8 + i * 0.4, 0.62, 0.85), om.material("Warn", (0.9, 0.7, 0.1)), verts=8)
    wheels(m, (0.8, -0.8), 0.6)
    om.join_all("bomb_truck"); export("bomb_truck")


def radar_van():
    om.reset_scene(); m = M()
    om.box("Van", (2.2, 1.05, 1.0), (0, 0, 0.85), m["sand"])
    om.box("Windscreen", (0.06, 0.85, 0.4), (1.12, 0, 1.1), m["glass"], bevel=0.0)
    om.box("Stripe", (2.22, 1.07, 0.12), (0, 0, 0.9), om.team(), bevel=0.01)
    om.cylinder("Mast", 0.05, 1.6, (-0.4, 0, 2.1), m["steel"], verts=6, bevel=0.0)
    om.box("Dish", (0.12, 1.2, 0.5), (-0.4, 0, 2.9), m["rust"], bevel=0.02)
    wheels(m, (0.7, -0.7), 0.58)
    om.join_all("radar_van"); export("radar_van")


def fpv_drone():
    om.reset_scene(); m = M()
    om.box("Body", (0.3, 0.3, 0.08), (0, 0, 0.2), m["dark"], bevel=0.01)
    om.box("Charge", (0.18, 0.18, 0.12), (0, 0, 0.12), om.material("Warn", (0.9, 0.7, 0.1)), bevel=0.01)
    for x, y in ((0.25, 0.25), (-0.25, 0.25), (0.25, -0.25), (-0.25, -0.25)):
        om.box("Arm", (0.3, 0.04, 0.03), (x / 2, y / 2, 0.22), m["steel"], bevel=0.0, rot=(0, 0, 45 if x * y > 0 else -45))
        om.cylinder("Rotor", 0.14, 0.02, (x, y, 0.26), om.team(), verts=8, bevel=0.0)
    om.join_all("fpv_drone"); export("fpv_drone")


def ied():
    om.reset_scene(); m = M()
    om.cylinder("Dirt", 0.35, 0.08, (0, 0, 0.04), m["mud"], verts=10, bevel=0.02)
    om.box("Wire", (0.4, 0.03, 0.03), (0.1, 0.1, 0.09), m["dark"], bevel=0.0, rot=(0, 0, 30))
    om.join_all("ied"); export("ied")


MODELS = {
    "command_cell": command_cell, "safehouse": safehouse, "supply_stash": supply_stash, "arms_dealer": arms_dealer, "tunnel_network": tunnel_network,
    "black_market": black_market, "compound": compound, "stinger_site": stinger_site, "drone_workshop": drone_workshop, "launch_site": launch_site,
    "worker": worker, "rebel": rebel, "rpg_trooper": rpg_trooper, "saboteur": saboteur, "angry_mob": angry_mob, "technical": technical,
    "raider_quad": raider_quad, "marauder": marauder, "sprayer": sprayer, "rocket_buggy": rocket_buggy, "bomb_truck": bomb_truck, "radar_van": radar_van,
    "fpv_operator": fpv_operator, "fpv_drone": fpv_drone, "ied": ied,
}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    os.makedirs(OUT, exist_ok=True)
    for n in argv or list(MODELS):
        MODELS[n]()
        print(f"[models] built {n}")
