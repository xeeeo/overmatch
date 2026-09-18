"""Coalition buildings. Origin = centre of the footprint at ground level. Models stay inside the footprint with a small margin.

    blender -b -P tools/blender/build_buildings.py [-- name ...]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import omlib as om  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "models", "coalition")

CONCRETE = (0.62, 0.60, 0.56)
CONCRETE_DARK = (0.48, 0.47, 0.44)
ROOF = (0.36, 0.38, 0.36)
STEEL = (0.35, 0.37, 0.38)
DARK = (0.10, 0.10, 0.11)
GLASS = (0.55, 0.75, 0.85)
WARN = (0.90, 0.68, 0.14)


def mats():
    return dict(
        concrete=om.material("Concrete", CONCRETE),
        concrete_dark=om.material("ConcreteDark", CONCRETE_DARK),
        roof=om.material("Roof", ROOF),
        steel=om.material("Steel", STEEL),
        dark=om.material("Dark", DARK),
        glass=om.material("Glass", GLASS, roughness=0.2),
        warn=om.material("Warn", WARN),
    )


def slab(m, w, h, z=0.18):
    om.box("Slab", (w - 0.3, h - 0.3, z), (0, 0, z / 2), m["concrete_dark"], bevel=0.02)


def antenna(m, x, y, z, height=1.6):
    om.cylinder("Mast", 0.05, height, (x, y, z + height / 2), m["steel"], verts=6, bevel=0.0)
    om.box("Dish", (0.12, 0.5, 0.5), (x, y, z + height), m["steel"], bevel=0.01)


def export(name):
    om.join_all(name)
    om.export_glb(os.path.join(OUT, f"{name}.glb"))


def command_post():
    om.reset_scene(); m = mats()
    slab(m, 5, 5)
    om.box("Main", (3.6, 3.0, 1.6), (0.2, 0.0, 0.18 + 0.8), m["concrete"])
    om.box("Roof", (3.7, 3.1, 0.15), (0.2, 0.0, 1.85), m["roof"], bevel=0.01)
    om.box("Band", (3.62, 3.02, 0.22), (0.2, 0.0, 1.2), om.team(), bevel=0.01)
    om.box("Wing", (1.4, 1.4, 1.0), (-1.6, 1.2, 0.68), m["concrete"])
    om.box("Tower", (1.0, 1.0, 2.6), (-1.5, -1.1, 1.48), m["concrete_dark"])
    om.box("TowerGlass", (1.02, 1.02, 0.5), (-1.5, -1.1, 2.4), m["glass"], bevel=0.0)
    om.cylinder("RadarBase", 0.3, 0.3, (1.0, 1.0, 2.1), m["steel"], verts=10)
    om.box("Radar", (0.15, 1.6, 0.8), (1.0, 1.0, 2.7), m["steel"], bevel=0.01)
    om.box("Door", (0.1, 0.9, 1.0), (2.0, -0.6, 0.68), m["dark"], bevel=0.0)
    export("command_post")


def power_plant():
    om.reset_scene(); m = mats()
    slab(m, 3, 3)
    om.box("Hall", (2.0, 2.2, 1.3), (-0.25, 0.0, 0.83), m["concrete"])
    om.box("Roof", (2.1, 2.3, 0.12), (-0.25, 0.0, 1.55), m["roof"], bevel=0.01)
    om.box("Stripe", (2.02, 2.22, 0.18), (-0.25, 0.0, 1.0), om.team(), bevel=0.01)
    for y in (-0.6, 0.6):
        om.cylinder("Turbine", 0.28, 2.0, (1.0, y, 0.6), m["steel"], verts=12, rot=(0, 90, 0))
        om.cylinder("Stack", 0.18, 1.2, (1.15, y, 1.4), m["dark"], verts=10)
    om.box("Transformer", (0.6, 0.6, 0.7), (-1.05, -0.95, 0.53), m["dark"], bevel=0.02)
    export("power_plant")


def barracks():
    om.reset_scene(); m = mats()
    slab(m, 3, 3)
    om.box("Hut", (2.4, 1.6, 1.0), (0.0, 0.3, 0.68), m["concrete"])
    om.box("Roof", (2.5, 1.7, 0.14), (0.0, 0.3, 1.25), om.team(), bevel=0.01)
    om.box("Porch", (2.4, 0.7, 0.05), (0.0, -0.9, 0.2), m["concrete_dark"], bevel=0.0)
    for x in (-0.9, 0.0, 0.9):
        om.box("Post", (0.08, 0.08, 0.9), (x, -1.2, 0.65), m["steel"], bevel=0.0)
        om.box("Window", (0.5, 0.05, 0.35), (x, -0.5, 0.85), m["dark"], bevel=0.0)
    om.box("Awning", (2.4, 0.8, 0.06), (0.0, -0.85, 1.1), m["roof"], bevel=0.0)
    om.box("Flagpole", (0.05, 0.05, 1.8), (1.2, 1.1, 1.1), m["steel"], bevel=0.0)
    om.box("Flag", (0.5, 0.03, 0.3), (0.95, 1.1, 1.85), om.team(), bevel=0.0)
    export("barracks")


def supply_center():
    om.reset_scene(); m = mats()
    slab(m, 4, 4)
    om.box("Warehouse", (2.2, 3.2, 1.5), (-0.7, 0.0, 0.93), m["concrete"])
    om.box("Roof", (2.3, 3.3, 0.12), (-0.7, 0.0, 1.74), m["roof"], bevel=0.01)
    om.box("Stripe", (2.22, 3.22, 0.2), (-0.7, 0.0, 1.1), om.team(), bevel=0.01)
    om.box("BayDoor", (0.05, 1.4, 1.1), (0.4, 0.5, 0.73), m["dark"], bevel=0.0)
    om.cylinder("Pad", 1.05, 0.06, (1.05, 0.0, 0.21), m["concrete_dark"], verts=20)
    om.cylinder("PadRing", 0.9, 0.02, (1.05, 0.0, 0.25), m["warn"], verts=20, bevel=0.0)
    for i, (x, y) in enumerate(((-1.4, -1.5), (-0.9, -1.5), (-1.4, -1.0))):
        om.box(f"Crate{i}", (0.45, 0.45, 0.45), (x, y, 0.4), m["warn"], bevel=0.03)
    export("supply_center")


def motor_pool():
    om.reset_scene(); m = mats()
    slab(m, 4, 4)
    om.box("Garage", (3.2, 2.6, 1.6), (0.0, 0.4, 0.98), m["concrete"])
    om.box("Roof", (3.3, 2.7, 0.14), (0.0, 0.4, 1.85), m["roof"], bevel=0.01)
    om.box("Stripe", (3.22, 2.62, 0.2), (0.0, 0.4, 1.3), om.team(), bevel=0.01)
    om.box("Door", (2.2, 0.05, 1.2), (0.0, -0.92, 0.78), m["dark"], bevel=0.0)
    om.box("Ramp", (2.2, 1.0, 0.1), (0.0, -1.45, 0.2), m["concrete_dark"], bevel=0.0)
    om.cylinder("Tank", 0.35, 1.2, (1.7, 1.4, 0.55), m["steel"], verts=10)
    om.box("Crane", (0.1, 2.0, 0.1), (-1.5, 0.3, 2.1), m["steel"], bevel=0.0)
    om.box("CranePost", (0.12, 0.12, 2.2), (-1.5, 1.3, 1.1), m["steel"], bevel=0.0)
    export("motor_pool")


def airfield():
    om.reset_scene(); m = mats()
    slab(m, 6, 4, z=0.14)
    om.box("Runway", (5.4, 1.4, 0.04), (0.0, -0.9, 0.16), m["concrete_dark"], bevel=0.0)
    for x in (-2.0, -0.7, 0.7, 2.0):
        om.box("Stripe", (0.6, 0.12, 0.02), (x, -0.9, 0.19), m["warn"], bevel=0.0)
    for x in (-1.6, 1.6):
        om.cylinder("Pad", 0.8, 0.04, (x, 1.0, 0.16), m["concrete_dark"], verts=16)
        om.cylinder("PadRing", 0.65, 0.02, (x, 1.0, 0.19), m["warn"], verts=16, bevel=0.0)
    om.box("Tower", (0.9, 0.9, 2.4), (0.0, 1.3, 1.34), m["concrete"])
    om.box("TowerTop", (1.2, 1.2, 0.5), (0.0, 1.3, 2.75), m["glass"], bevel=0.02)
    om.box("TowerRoof", (1.3, 1.3, 0.1), (0.0, 1.3, 3.05), om.team(), bevel=0.01)
    om.box("Hangar", (1.4, 1.2, 0.9), (-2.2, 1.2, 0.59), m["concrete"])
    export("airfield")


def strategy_center():
    om.reset_scene(); m = mats()
    slab(m, 4, 4)
    om.box("Base", (3.0, 3.0, 0.9), (0.0, 0.0, 0.63), m["concrete"])
    om.box("Band", (3.02, 3.02, 0.2), (0.0, 0.0, 0.9), om.team(), bevel=0.01)
    om.cylinder("Dome", 1.1, 1.0, (0.0, 0.0, 1.55), m["concrete_dark"], verts=16)
    om.cylinder("DomeTop", 0.8, 0.4, (0.0, 0.0, 2.2), m["roof"], verts=16)
    antenna(m, 1.2, 1.2, 1.1, 1.8)
    antenna(m, -1.2, -1.2, 1.1, 1.4)
    om.box("Screen", (0.05, 1.2, 0.6), (1.52, 0.0, 0.6), m["glass"], bevel=0.0)
    export("strategy_center")


def drop_zone():
    om.reset_scene(); m = mats()
    slab(m, 3, 3, z=0.14)
    om.cylinder("Pad", 1.2, 0.05, (0.0, 0.0, 0.16), m["concrete_dark"], verts=20)
    om.box("H1", (0.15, 0.9, 0.02), (-0.3, 0.0, 0.2), m["warn"], bevel=0.0)
    om.box("H2", (0.15, 0.9, 0.02), (0.3, 0.0, 0.2), m["warn"], bevel=0.0)
    om.box("H3", (0.6, 0.15, 0.02), (0.0, 0.0, 0.2), m["warn"], bevel=0.0)
    om.box("Shed", (0.8, 0.8, 0.8), (1.0, 1.0, 0.54), m["concrete"])
    om.box("ShedRoof", (0.9, 0.9, 0.08), (1.0, 1.0, 0.98), om.team(), bevel=0.0)
    om.cylinder("Beacon", 0.06, 1.4, (-1.1, 1.1, 0.84), m["steel"], verts=6, bevel=0.0)
    om.box("BeaconLight", (0.2, 0.2, 0.2), (-1.1, 1.1, 1.6), m["warn"], bevel=0.02)
    for i, (x, y) in enumerate(((-1.0, -1.0), (-0.5, -1.05))):
        om.box(f"Crate{i}", (0.4, 0.4, 0.4), (x, y, 0.34), m["warn"], bevel=0.03)
    export("drop_zone")


def sentry_battery():
    om.reset_scene(); m = mats()
    om.box("Slab", (1.8, 1.8, 0.2), (0, 0, 0.1), m["concrete_dark"], bevel=0.02)
    om.cylinder("Base", 0.7, 0.6, (0.0, 0.0, 0.5), m["concrete"], verts=12)
    om.box("Band", (1.42, 1.42, 0.12), (0.0, 0.0, 0.5), om.team(), bevel=0.01)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.cylinder("Ring", 0.5, 0.15, (0.0, 0.0, 0.88), m["steel"], verts=12)
    om.box("Head", (0.7, 0.6, 0.4), (0.0, 0.0, 1.15), m["steel"], bevel=0.02)
    om.cylinder("Gun", 0.06, 0.9, (0.6, 0.12, 1.15), m["dark"], verts=8, rot=(0, 90, 0), bevel=0.0)
    om.cylinder("Gun2", 0.06, 0.9, (0.6, -0.12, 1.15), m["dark"], verts=8, rot=(0, 90, 0), bevel=0.0)
    om.box("Pod", (0.4, 0.25, 0.25), (0.0, 0.45, 1.3), m["dark"], bevel=0.02)
    om.box("Pod2", (0.4, 0.25, 0.25), (0.0, -0.45, 1.3), m["dark"], bevel=0.02)
    turret = om.join("Turret", om.parts_since(snap), origin=(0.0, 0.0, 0.85))
    om.parent(turret, hull)
    om.export_glb(os.path.join(OUT, "sentry_battery.glb"))


def orbital_uplink():
    om.reset_scene(); m = mats()
    slab(m, 4, 4)
    om.box("Bunker", (2.6, 2.6, 1.0), (0.0, 0.0, 0.68), m["concrete"])
    om.box("Band", (2.62, 2.62, 0.2), (0.0, 0.0, 0.95), om.team(), bevel=0.01)
    om.cylinder("Mount", 0.5, 0.8, (0.0, 0.0, 1.55), m["steel"], verts=12)
    om.cylinder("Dish", 1.5, 0.15, (0.0, 0.0, 2.2), m["concrete_dark"], verts=20, rot=(25, 0, 0))
    om.cylinder("Feed", 0.05, 1.2, (0.0, 0.25, 2.7), m["steel"], verts=6, rot=(25, 0, 0), bevel=0.0)
    om.box("FeedHead", (0.25, 0.25, 0.25), (0.0, 0.5, 3.25), m["dark"], bevel=0.02)
    for x, y in ((1.5, 1.5), (-1.5, 1.5), (1.5, -1.5), (-1.5, -1.5)):
        om.box("Pylon", (0.3, 0.3, 1.4), (x, y, 0.88), m["concrete_dark"], bevel=0.02)
        om.box("Light", (0.2, 0.2, 0.15), (x, y, 1.65), m["warn"], bevel=0.02)
    export("orbital_uplink")


MODELS = {
    "command_post": command_post, "power_plant": power_plant, "barracks": barracks, "supply_center": supply_center,
    "motor_pool": motor_pool, "airfield": airfield, "strategy_center": strategy_center, "drop_zone": drop_zone,
    "sentry_battery": sentry_battery, "orbital_uplink": orbital_uplink,
}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    os.makedirs(OUT, exist_ok=True)
    for n in argv or list(MODELS):
        MODELS[n]()
        print(f"[models] built {n}")
