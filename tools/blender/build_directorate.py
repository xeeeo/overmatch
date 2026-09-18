"""Directorate: angular, heavy, red-banded, industrial. Origin = footprint centre (buildings) / ground under centre (units).

    blender -b -P tools/blender/build_directorate.py [-- name ...]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import omlib as om  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "assets", "models", "directorate")
DARK = (0.10, 0.10, 0.11)
STEEL = (0.32, 0.33, 0.35)
GLASS = (0.55, 0.75, 0.85)
HULL = (0.36, 0.34, 0.30)
CONCRETE = (0.52, 0.50, 0.47)
CONCRETE_DARK = (0.40, 0.39, 0.37)
ROOF = (0.30, 0.30, 0.31)


def M():
    return dict(dark=om.material("Dark", DARK), steel=om.material("Steel", STEEL), glass=om.material("Glass", GLASS, roughness=0.2),
                hull=om.material("Hull", HULL), concrete=om.material("Concrete", CONCRETE), cdark=om.material("ConcreteDark", CONCRETE_DARK),
                roof=om.material("Roof", ROOF), track=om.material("Track", DARK), tyre=om.material("Tyre", DARK))


def export(name):
    om.export_glb(os.path.join(OUT, f"{name}.glb"))


def slab(m, w, h, z=0.2):
    om.box("Slab", (w - 0.3, h - 0.3, z), (0, 0, z / 2), m["cdark"], bevel=0.02)


def tracks(m, length, half_width, height=0.42):
    for side in (1, -1):
        om.box(f"Track{side}", (length, 0.4, height), (0.0, side * half_width, height / 2 + 0.08), m["track"], bevel=0.04)


def wheels(m, xs, half_width, r=0.36):
    for x in xs:
        for side in (1, -1):
            om.cylinder(f"Wheel{x}{side}", r, 0.3, (x, side * half_width, r), m["tyre"], verts=14, rot=(90, 0, 0), bevel=0.03)


# ---------------- buildings ----------------
def command_bunker():
    om.reset_scene(); m = M(); slab(m, 5, 5)
    om.box("Bunker", (3.8, 3.4, 1.3), (0, 0, 0.85), m["concrete"])
    om.wedge("Glacis", (1.0, 3.4, 1.3), (2.2, 0, 0.85), m["cdark"])
    om.box("Band", (3.82, 3.42, 0.25), (0, 0, 1.2), om.team(), bevel=0.01)
    om.box("Upper", (2.2, 2.0, 0.9), (-0.4, 0, 2.0), m["concrete"])
    om.box("Roof", (2.3, 2.1, 0.12), (-0.4, 0, 2.5), m["roof"], bevel=0.01)
    om.cylinder("Mast", 0.07, 2.2, (-1.2, -1.0, 3.5), m["steel"], verts=6, bevel=0.0)
    om.box("Flag", (0.7, 0.03, 0.4), (-0.85, -1.0, 4.3), om.team(), bevel=0.0)
    om.box("Dish", (0.15, 1.2, 0.8), (0.6, 1.0, 2.9), m["steel"], bevel=0.01)
    om.join_all("command_bunker"); export("command_bunker")


def reactor():
    om.reset_scene(); m = M(); slab(m, 3, 3)
    om.box("Hall", (1.6, 2.2, 1.1), (-0.5, 0, 0.75), m["concrete"])
    om.cylinder("Dome", 0.9, 1.5, (0.5, 0, 0.95), m["cdark"], verts=16)
    om.cylinder("DomeTop", 0.6, 0.4, (0.5, 0, 1.9), m["concrete"], verts=16)
    om.box("Band", (1.62, 2.22, 0.2), (-0.5, 0, 1.0), om.team(), bevel=0.01)
    om.cylinder("Tower", 0.3, 2.2, (-1.0, -0.9, 1.3), m["steel"], verts=10)
    om.box("Warning", (0.4, 0.4, 0.4), (0.5, 1.1, 0.4), om.material("Warn", (0.9, 0.7, 0.1)), bevel=0.02)
    om.join_all("reactor"); export("reactor")


def barracks():
    om.reset_scene(); m = M(); slab(m, 3, 3)
    om.box("Hall", (2.5, 1.8, 1.1), (0, 0.2, 0.75), m["concrete"])
    om.wedge("Roof", (2.6, 1.9, 0.5), (0, 0.2, 1.55), m["roof"], bevel=0.02)
    om.box("Band", (2.52, 1.82, 0.18), (0, 0.2, 0.9), om.team(), bevel=0.01)
    om.box("Yard", (2.4, 0.7, 0.05), (0, -1.0, 0.22), m["cdark"], bevel=0.0)
    om.box("Sandbags", (2.4, 0.3, 0.35), (0, -1.3, 0.38), om.material("Bag", (0.55, 0.5, 0.38)), bevel=0.05)
    om.box("Door", (0.05, 0.8, 0.9), (1.26, -0.2, 0.65), m["dark"], bevel=0.0)
    om.join_all("barracks"); export("barracks")


def supply_depot():
    om.reset_scene(); m = M(); slab(m, 4, 4)
    om.box("Shed", (2.4, 3.2, 1.6), (-0.6, 0, 1.0), m["concrete"])
    om.wedge("Roof", (2.6, 3.4, 0.6), (-0.6, 0, 1.9), m["roof"], bevel=0.02)
    om.box("Band", (2.42, 3.22, 0.22), (-0.6, 0, 1.2), om.team(), bevel=0.01)
    om.box("Dock", (1.2, 3.0, 0.3), (1.1, 0, 0.35), m["cdark"], bevel=0.02)
    for i, y in enumerate((-1.0, -0.3, 0.4)):
        om.box(f"Crate{i}", (0.5, 0.5, 0.5), (1.1, y, 0.75), om.material("Crate", (0.55, 0.45, 0.25)), bevel=0.03)
    om.join_all("supply_depot"); export("supply_depot")


def vehicle_plant():
    om.reset_scene(); m = M(); slab(m, 4, 4)
    om.box("Hall", (3.2, 3.0, 1.7), (0, 0.3, 1.05), m["concrete"])
    om.box("Roof", (3.3, 3.1, 0.14), (0, 0.3, 1.95), m["roof"], bevel=0.01)
    om.box("Band", (3.22, 3.02, 0.22), (0, 0.3, 1.4), om.team(), bevel=0.01)
    om.box("Door", (2.2, 0.05, 1.3), (0, -1.22, 0.85), m["dark"], bevel=0.0)
    for x in (-1.0, 0.0, 1.0):
        om.cylinder(f"Stack{x}", 0.15, 1.0, (x, 1.4, 2.4), m["dark"], verts=8)
    om.box("Crane", (0.1, 3.0, 0.1), (1.7, 0.3, 2.3), m["steel"], bevel=0.0)
    om.join_all("vehicle_plant"); export("vehicle_plant")


def airfield():
    om.reset_scene(); m = M(); slab(m, 6, 4, z=0.14)
    om.box("Runway", (5.4, 1.4, 0.04), (0, -0.9, 0.16), m["cdark"], bevel=0.0)
    for x in (-2.0, -0.7, 0.7, 2.0):
        om.box("Stripe", (0.6, 0.12, 0.02), (x, -0.9, 0.19), om.material("Warn", (0.9, 0.7, 0.1)), bevel=0.0)
    om.box("Hangar", (2.4, 1.4, 1.1), (-1.5, 1.1, 0.7), m["concrete"])
    om.wedge("HangarRoof", (2.5, 1.5, 0.5), (-1.5, 1.1, 1.5), m["roof"], bevel=0.02)
    om.box("Tower", (0.8, 0.8, 2.2), (1.6, 1.3, 1.25), m["concrete"])
    om.box("TowerTop", (1.1, 1.1, 0.45), (1.6, 1.3, 2.55), m["glass"], bevel=0.02)
    om.box("TowerRoof", (1.2, 1.2, 0.1), (1.6, 1.3, 2.85), om.team(), bevel=0.01)
    om.join_all("airfield"); export("airfield")


def propaganda_center():
    om.reset_scene(); m = M(); slab(m, 4, 4)
    om.box("Base", (3.0, 3.0, 1.0), (0, 0, 0.7), m["concrete"])
    om.box("Band", (3.02, 3.02, 0.25), (0, 0, 1.0), om.team(), bevel=0.01)
    om.box("Screen", (0.12, 2.4, 1.4), (1.2, 0, 2.0), om.team(), bevel=0.02)
    om.box("ScreenFrame", (0.2, 2.6, 1.6), (1.1, 0, 2.0), m["steel"], bevel=0.02)
    om.cylinder("Mast", 0.08, 2.4, (-1.0, 1.0, 2.4), m["steel"], verts=6, bevel=0.0)
    for z in (2.6, 3.2):
        om.box(f"Speaker{z}", (0.4, 0.4, 0.3), (-1.0, 1.0, z), m["dark"], bevel=0.03)
    om.join_all("propaganda_center"); export("propaganda_center")


def cyber_center():
    om.reset_scene(); m = M(); slab(m, 3, 3)
    om.box("Box", (2.2, 2.2, 1.6), (0, 0, 1.0), m["cdark"])
    om.box("Band", (2.22, 2.22, 0.2), (0, 0, 1.5), om.team(), bevel=0.01)
    for x in (-0.6, 0.0, 0.6):
        om.box(f"Vent{x}", (0.35, 2.3, 0.08), (x, 0, 1.85), m["steel"], bevel=0.0)
    om.cylinder("Dish", 0.6, 0.1, (0.5, -0.5, 2.3), m["steel"], verts=16, rot=(30, 0, 0))
    om.box("AC", (0.6, 0.5, 0.4), (-0.7, 0.7, 2.0), m["steel"], bevel=0.02)
    om.join_all("cyber_center"); export("cyber_center")


def flak_tower():
    om.reset_scene(); m = M()
    om.box("Slab", (1.8, 1.8, 0.2), (0, 0, 0.1), m["cdark"], bevel=0.02)
    om.box("Tower", (1.3, 1.3, 1.6), (0, 0, 1.0), m["concrete"])
    om.box("Band", (1.32, 1.32, 0.15), (0, 0, 1.2), om.team(), bevel=0.01)
    for side in (1, -1):
        om.box(f"Slit{side}", (0.05, 0.7, 0.15), (side * 0.66, 0, 1.1), m["dark"], bevel=0.0)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.cylinder("Ring", 0.5, 0.14, (0, 0, 1.87), m["steel"], verts=12)
    om.box("Mount", (0.6, 0.5, 0.35), (0, 0, 2.1), m["steel"], bevel=0.02)
    for side in (1, -1):
        om.cylinder(f"Flak{side}", 0.06, 1.1, (0.55, side * 0.15, 2.2), m["dark"], verts=8, rot=(0, 60, 0), bevel=0.0)
    om.cylinder("Gatling", 0.1, 0.7, (0.5, 0, 1.95), m["dark"], verts=8, rot=(0, 90, 0), bevel=0.0)
    t = om.join("Turret", om.parts_since(snap), origin=(0, 0, 1.85)); om.parent(t, hull)
    export("flak_tower")


def missile_silo():
    om.reset_scene(); m = M(); slab(m, 4, 4)
    om.box("Bunker", (3.0, 3.0, 0.8), (0, 0, 0.6), m["concrete"])
    om.box("Band", (3.02, 3.02, 0.2), (0, 0, 0.8), om.team(), bevel=0.01)
    om.cylinder("Silo", 1.0, 0.3, (0.3, 0, 1.15), m["cdark"], verts=16)
    om.box("DoorL", (1.0, 1.0, 0.1), (0.3, 0.55, 1.35), m["steel"], bevel=0.02, rot=(35, 0, 0))
    om.box("DoorR", (1.0, 1.0, 0.1), (0.3, -0.55, 1.35), m["steel"], bevel=0.02, rot=(-35, 0, 0))
    om.cylinder("Missile", 0.28, 1.6, (0.3, 0, 1.6), m["steel"], verts=12)
    om.cylinder("Nose", 0.28, 0.5, (0.3, 0, 2.6), om.material("Warn", (0.9, 0.7, 0.1)), verts=12)
    om.box("Control", (0.9, 0.9, 1.2), (-1.2, -1.1, 1.0), m["concrete"])
    om.join_all("missile_silo"); export("missile_silo")


# ---------------- units ----------------
def engineering_vehicle():
    om.reset_scene(); m = M()
    om.box("Body", (1.7, 1.2, 0.55), (-0.1, 0, 0.6), m["hull"])
    om.box("Cab", (0.7, 1.0, 0.6), (-0.4, 0, 1.15), om.team())
    om.box("Blade", (0.15, 1.7, 0.7), (1.0, 0, 0.45), m["steel"], bevel=0.02)
    om.box("Arm", (0.8, 0.12, 0.12), (0.55, 0.65, 0.5), m["steel"], bevel=0.01)
    om.box("Arm2", (0.8, 0.12, 0.12), (0.55, -0.65, 0.5), m["steel"], bevel=0.01)
    om.box("Crane", (1.4, 0.15, 0.15), (-0.6, 0.4, 1.7), m["steel"], bevel=0.01, rot=(0, -20, 0))
    tracks(m, 1.9, 0.66)
    om.join_all("engineering_vehicle"); export("engineering_vehicle")


def supply_truck():
    om.reset_scene(); m = M()
    om.box("Chassis", (2.6, 1.1, 0.3), (0, 0, 0.55), m["hull"])
    om.box("Cab", (0.8, 1.1, 0.8), (1.0, 0, 1.15), om.team())
    om.box("Windscreen", (0.06, 0.9, 0.35), (1.42, 0, 1.25), m["glass"], bevel=0.0)
    om.box("Bed", (1.6, 1.2, 0.9), (-0.5, 0, 1.15), m["steel"], bevel=0.03)
    om.box("Tarp", (1.5, 1.1, 0.2), (-0.5, 0, 1.7), om.material("Tarp", (0.32, 0.36, 0.28)), bevel=0.04)
    wheels(m, (0.9, -0.2, -1.0), 0.62)
    om.join_all("supply_truck"); export("supply_truck")


def infantry(name, kit_rgb, weapon):
    om.reset_scene()
    skin = om.material("Skin", (0.80, 0.62, 0.50)); boots = om.material("Boots", DARK); kit = om.material("Kit", kit_rgb); steel = om.material("Steel", STEEL)
    for side in (1, -1):
        om.box(f"Leg{side}", (0.22, 0.2, 0.55), (0.0, side * 0.13, 0.28), kit, bevel=0.02)
        om.box(f"Boot{side}", (0.3, 0.22, 0.1), (0.04, side * 0.13, 0.05), boots, bevel=0.01)
    om.box("Torso", (0.34, 0.5, 0.5), (0.0, 0.0, 0.82), om.team(), bevel=0.03)
    om.box("Coat", (0.36, 0.52, 0.28), (0.0, 0.0, 0.7), kit, bevel=0.02)
    om.box("Head", (0.24, 0.24, 0.22), (0.0, 0.0, 1.2), skin, bevel=0.03)
    om.box("Cap", (0.3, 0.3, 0.1), (0.0, 0.0, 1.36), kit, bevel=0.03)
    for side in (1, -1):
        om.box(f"Arm{side}", (0.5, 0.14, 0.14), (0.2, side * 0.3, 0.9), kit, bevel=0.02)
    if weapon == "rifle":
        om.box("Rifle", (0.7, 0.07, 0.1), (0.4, 0.15, 0.92), steel, bevel=0.01)
    elif weapon == "rocket":
        om.cylinder("Tube", 0.08, 1.0, (0.2, 0.32, 1.05), steel, verts=8, rot=(0, 90, 0), bevel=0.0)
    elif weapon == "laptop":
        om.box("Laptop", (0.3, 0.4, 0.03), (0.4, 0.0, 0.95), steel, bevel=0.0)
        om.box("Screen", (0.03, 0.4, 0.25), (0.55, 0.0, 1.08), om.material("Glass", GLASS, roughness=0.2), bevel=0.0)
    om.join_all(name); export(name)


def conscript(): infantry("conscript", (0.42, 0.36, 0.26), "rifle")
def rocket_squad(): infantry("rocket_squad", (0.38, 0.34, 0.26), "rocket")
def hacker(): infantry("hacker", (0.22, 0.22, 0.26), "laptop")


def tank(name, size, turret_size, gun_len, twin=False, heavy=False):
    om.reset_scene(); m = M()
    L, W = size
    om.box("Hull", (L, W, 0.5), (0, 0, 0.55), m["hull"])
    om.wedge("Front", (0.6, W, 0.5), (L / 2 + 0.1, 0, 0.55), m["hull"])
    om.box("Band", (L * 0.7, W + 0.02, 0.16), (-0.1, 0, 0.75), om.team(), bevel=0.01)
    tracks(m, L + 0.3, W / 2 + 0.15, 0.48 if heavy else 0.42)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    tl, tw = turret_size
    om.box("TurretBody", (tl, tw, 0.4), (-0.15, 0, 1.05), m["hull"], bevel=0.03)
    om.box("TurretBand", (tl * 0.6, tw + 0.02, 0.12), (-0.2, 0, 1.05), om.team(), bevel=0.01)
    if twin:
        for side in (1, -1):
            om.cylinder(f"Gun{side}", 0.08, gun_len, (tl / 2 + gun_len / 2 - 0.2, side * 0.22, 1.08), m["steel"], verts=10, rot=(0, 90, 0))
    else:
        om.cylinder("Gun", 0.07, gun_len, (tl / 2 + gun_len / 2 - 0.2, 0, 1.08), m["steel"], verts=10, rot=(0, 90, 0))
    om.box("Hatch", (0.3, 0.3, 0.08), (-0.4, 0.2, 1.28), m["steel"], bevel=0.01)
    t = om.join("Turret", om.parts_since(snap), origin=(-0.15, 0, 0.85)); om.parent(t, hull)
    export(name)


def vanguard(): tank("vanguard", (2.0, 1.2), (1.0, 0.9), 1.3)
def colossus(): tank("colossus", (3.0, 1.8), (1.6, 1.4), 1.8, twin=True, heavy=True)


def typhoon():
    om.reset_scene(); m = M()
    om.box("Hull", (2.4, 1.3, 0.45), (0, 0, 0.55), m["hull"])
    om.box("Cab", (0.7, 1.2, 0.6), (0.85, 0, 1.05), om.team())
    tracks(m, 2.6, 0.8)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.box("PodBase", (1.0, 1.0, 0.2), (-0.5, 0, 0.9), m["steel"], bevel=0.02)
    om.box("Pod", (1.6, 1.1, 0.7), (-0.5, 0, 1.4), m["steel"], bevel=0.03, rot=(0, -30, 0))
    for y in (-0.35, 0.0, 0.35):
        for z in (1.2, 1.5, 1.8):
            om.cylinder("Tube", 0.09, 0.2, (0.2, y, z), m["dark"], verts=8, rot=(0, 60, 0), bevel=0.0)
    t = om.join("Turret", om.parts_since(snap), origin=(-0.5, 0, 0.85)); om.parent(t, hull)
    export("typhoon")


def hailstorm():
    om.reset_scene(); m = M()
    om.box("Hull", (2.1, 1.3, 0.5), (0, 0, 0.55), m["hull"])
    om.box("Band", (1.5, 1.32, 0.15), (-0.1, 0, 0.75), om.team(), bevel=0.01)
    tracks(m, 2.3, 0.8)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.box("Turret", (0.9, 1.0, 0.45), (-0.1, 0, 1.05), m["hull"], bevel=0.03)
    for side in (1, -1):
        om.cylinder(f"Flak{side}", 0.07, 1.4, (0.6, side * 0.3, 1.2), m["steel"], verts=8, rot=(0, 70, 0), bevel=0.0)
    om.box("Radar", (0.3, 0.6, 0.35), (-0.4, 0, 1.4), m["steel"], bevel=0.02)
    t = om.join("Turret", om.parts_since(snap), origin=(-0.1, 0, 0.85)); om.parent(t, hull)
    export("hailstorm")


def speaker():
    om.reset_scene(); m = M()
    om.box("Chassis", (2.2, 1.1, 0.35), (0, 0, 0.55), m["hull"])
    om.box("Cab", (0.8, 1.05, 0.75), (0.7, 0, 1.1), om.team())
    om.box("Box", (1.2, 1.05, 0.9), (-0.4, 0, 1.15), m["steel"], bevel=0.03)
    for side in (1, -1):
        om.box(f"Horn{side}", (0.5, 0.35, 0.35), (-0.4, side * 0.55, 1.75), m["dark"], bevel=0.04)
    om.box("Banner", (0.9, 0.03, 0.5), (-0.4, 0, 1.95), om.team(), bevel=0.0)
    wheels(m, (0.7, -0.7), 0.6)
    om.join_all("speaker"); export("speaker")


def ghost_van():
    om.reset_scene(); m = M()
    om.box("Van", (2.3, 1.1, 1.0), (0, 0, 0.9), m["steel"])
    om.box("Windscreen", (0.06, 0.9, 0.4), (1.16, 0, 1.15), m["glass"], bevel=0.0)
    om.box("Stripe", (2.32, 1.12, 0.15), (0, 0, 0.95), om.team(), bevel=0.01)
    om.cylinder("Dish", 0.45, 0.08, (-0.5, 0, 1.6), m["dark"], verts=14, rot=(20, 0, 0))
    om.cylinder("Antenna", 0.03, 1.0, (0.5, 0.3, 1.9), m["dark"], verts=6, bevel=0.0)
    wheels(m, (0.75, -0.75), 0.6)
    om.join_all("ghost_van"); export("ghost_van")


def troop_crawler():
    om.reset_scene(); m = M()
    om.box("Hull", (2.6, 1.4, 0.6), (0, 0, 0.62), m["hull"])
    om.box("Cabin", (2.0, 1.3, 0.7), (-0.2, 0, 1.25), m["hull"])
    om.box("Band", (2.02, 1.32, 0.15), (-0.2, 0, 1.3), om.team(), bevel=0.01)
    om.box("Slits", (1.6, 1.34, 0.12), (-0.2, 0, 1.45), m["dark"], bevel=0.0)
    om.box("Ramp", (0.1, 1.2, 0.8), (-1.32, 0, 1.0), m["steel"], bevel=0.02)
    om.cylinder("Mg", 0.05, 0.6, (0.9, 0.3, 1.7), m["dark"], verts=6, rot=(0, 90, 0), bevel=0.0)
    tracks(m, 2.8, 0.85)
    om.join_all("troop_crawler"); export("troop_crawler")


def talon():
    om.reset_scene(); m = M()
    body = om.material("Body", (0.42, 0.44, 0.46))
    om.wedge("Fuselage", (2.8, 0.8, 0.6), (0.2, 0, 0.8), body)
    om.box("Canopy", (0.7, 0.5, 0.3), (0.9, 0, 1.05), m["glass"], bevel=0.03)
    om.box("WingL", (1.1, 1.6, 0.08), (-0.3, 1.1, 0.75), om.team(), bevel=0.02, rot=(0, 0, 30))
    om.box("WingR", (1.1, 1.6, 0.08), (-0.3, -1.1, 0.75), om.team(), bevel=0.02, rot=(0, 0, -30))
    om.box("Fin", (0.6, 0.06, 0.6), (-1.3, 0, 1.15), body, bevel=0.01)
    om.cylinder("Engine", 0.22, 1.0, (-1.2, 0, 0.8), m["dark"], verts=10, rot=(0, 90, 0))
    for side in (1, -1):
        om.box(f"Bomb{side}", (0.7, 0.18, 0.18), (-0.1, side * 0.5, 0.5), m["dark"], bevel=0.04)
    om.join_all("talon"); export("talon")


MODELS = {
    "command_bunker": command_bunker, "reactor": reactor, "barracks": barracks, "supply_depot": supply_depot, "vehicle_plant": vehicle_plant,
    "airfield": airfield, "propaganda_center": propaganda_center, "cyber_center": cyber_center, "flak_tower": flak_tower, "missile_silo": missile_silo,
    "engineering_vehicle": engineering_vehicle, "supply_truck": supply_truck, "conscript": conscript, "rocket_squad": rocket_squad, "hacker": hacker,
    "vanguard": vanguard, "colossus": colossus, "typhoon": typhoon, "hailstorm": hailstorm, "speaker": speaker, "ghost_van": ghost_van,
    "troop_crawler": troop_crawler, "talon": talon,
}

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    os.makedirs(OUT, exist_ok=True)
    for n in argv or list(MODELS):
        MODELS[n]()
        print(f"[models] built {n}")
