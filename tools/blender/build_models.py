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
    hull_obj = om.join_all("Hull")

    snap = om.new_parts()
    om.wedge("TurretBody", (1.1, 0.95, 0.38), (-0.2, 0.0, 1.25), om.team())
    om.cylinder("Gun", 0.07, 1.5, (0.75, 0.0, 1.27), steel, verts=10, rot=(0, 90, 0))
    om.cylinder("Muzzle", 0.10, 0.2, (1.42, 0.0, 1.27), steel, verts=10, rot=(0, 90, 0))
    om.box("Hatch", (0.3, 0.3, 0.08), (-0.45, 0.25, 1.47), steel, bevel=0.01)
    om.box("Sensor", (0.18, 0.18, 0.22), (-0.2, -0.3, 1.5), steel, bevel=0.01)
    turret = om.join("Turret", om.parts_since(snap), origin=(-0.15, 0.0, 1.06))
    om.parent(turret, hull_obj)
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


def coalition_warden():
    """Light armoured vehicle. Tall boxy cab, angled bonnet, roof-mounted remote gun as the turret."""
    om.reset_scene()
    body = om.material("Body", (0.30, 0.34, 0.30))
    tyre = om.material("Tyre", DARK)
    steel = om.material("Steel", STEEL)
    glass = om.material("Glass", GLASS, roughness=0.2)
    om.box("Chassis", (2.2, 1.1, 0.35), (0.0, 0.0, 0.55), body)
    om.wedge("Bonnet", (0.9, 1.05, 0.45), (0.75, 0.0, 0.95), body)
    om.box("Cab", (1.15, 1.1, 0.75), (-0.2, 0.0, 1.1), om.team())
    om.box("Windscreen", (0.1, 0.9, 0.35), (0.38, 0.0, 1.2), glass, bevel=0.0)
    om.box("Bumper", (0.15, 1.15, 0.2), (1.2, 0.0, 0.6), steel, bevel=0.02)
    for x in (0.7, -0.7):
        for side in (1, -1):
            om.cylinder(f"Wheel{x}{side}", 0.36, 0.3, (x, side * 0.62, 0.36), tyre, verts=14, rot=(90, 0, 0), bevel=0.03)
    hull_obj = om.join_all("Hull")

    snap = om.new_parts()
    om.cylinder("Mount", 0.18, 0.1, (-0.2, 0.0, 1.52), steel, verts=10)
    om.box("GunBody", (0.5, 0.22, 0.16), (-0.05, 0.0, 1.62), steel, bevel=0.02)
    om.cylinder("Barrel", 0.035, 0.6, (0.45, 0.0, 1.62), steel, verts=8, rot=(0, 90, 0), bevel=0.0)
    turret = om.join("Turret", om.parts_since(snap), origin=(-0.2, 0.0, 1.5))
    om.parent(turret, hull_obj)
    om.export_glb(os.path.join(OUT, "coalition", "warden.glb"))


def infantry(name, body_rgb, weapon):
    """Blocky soldier ~1.4 tall. Torso in team colour, helmet, backpack, and a weapon held forward."""
    om.reset_scene()
    skin = om.material("Skin", (0.80, 0.62, 0.50))
    boots = om.material("Boots", DARK)
    kit = om.material("Kit", body_rgb)
    steel = om.material("Steel", STEEL)
    for side in (1, -1):
        om.box(f"Leg{side}", (0.22, 0.2, 0.55), (0.0, side * 0.13, 0.28), kit, bevel=0.02)
        om.box(f"Boot{side}", (0.3, 0.22, 0.1), (0.04, side * 0.13, 0.05), boots, bevel=0.01)
    om.box("Torso", (0.34, 0.5, 0.5), (0.0, 0.0, 0.82), om.team(), bevel=0.03)
    om.box("Vest", (0.36, 0.36, 0.3), (0.0, 0.0, 0.85), kit, bevel=0.02)
    om.box("Pack", (0.15, 0.36, 0.36), (-0.24, 0.0, 0.85), kit, bevel=0.02)
    om.box("Head", (0.24, 0.24, 0.22), (0.0, 0.0, 1.2), skin, bevel=0.03)
    om.box("Helmet", (0.3, 0.3, 0.14), (0.0, 0.0, 1.33), kit, bevel=0.04)
    for side in (1, -1):
        om.box(f"Arm{side}", (0.5, 0.14, 0.14), (0.2, side * 0.3, 0.9), kit, bevel=0.02)
    if weapon == "rifle":
        om.box("Rifle", (0.7, 0.07, 0.1), (0.4, 0.15, 0.92), steel, bevel=0.01)
    elif weapon == "rocket":
        om.cylinder("Tube", 0.08, 1.0, (0.2, 0.32, 1.05), steel, verts=8, rot=(0, 90, 0), bevel=0.0)
    elif weapon == "sniper":
        om.box("Rifle", (1.0, 0.06, 0.08), (0.5, 0.15, 0.92), steel, bevel=0.01)
        om.box("Scope", (0.25, 0.05, 0.06), (0.35, 0.15, 0.99), steel, bevel=0.0)
    om.join_all(name)
    om.export_glb(os.path.join(OUT, "coalition", f"{name}.glb"))


def coalition_rifleman():
    infantry("rifleman", (0.30, 0.34, 0.30), "rifle")


def coalition_rocket_trooper():
    infantry("rocket_trooper", (0.36, 0.34, 0.28), "rocket")


def coalition_pathfinder():
    infantry("pathfinder", (0.26, 0.30, 0.26), "sniper")


def coalition_lancer():
    """HIMARS-style: 6-wheel truck with a raised rocket pod on the back (the pod is the turret)."""
    om.reset_scene()
    body = om.material("Body", (0.30, 0.34, 0.30))
    tyre = om.material("Tyre", DARK)
    steel = om.material("Steel", STEEL)
    glass = om.material("Glass", GLASS, roughness=0.2)
    om.box("Chassis", (2.8, 1.2, 0.35), (0.0, 0.0, 0.6), body)
    om.box("Cab", (0.9, 1.2, 0.8), (1.0, 0.0, 1.15), om.team())
    om.box("Windscreen", (0.08, 1.0, 0.35), (1.46, 0.0, 1.25), glass, bevel=0.0)
    for x in (1.0, 0.0, -1.0):
        for side in (1, -1):
            om.cylinder(f"Wheel{x}{side}", 0.36, 0.3, (x, side * 0.66, 0.36), tyre, verts=14, rot=(90, 0, 0), bevel=0.03)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.box("PodBase", (1.2, 0.9, 0.2), (-0.7, 0.0, 0.88), steel, bevel=0.02)
    om.box("Pod", (1.4, 0.9, 0.6), (-0.6, 0.0, 1.35), steel, bevel=0.03, rot=(0, -25, 0))
    for y in (-0.25, 0.0, 0.25):
        for z in (1.25, 1.5):
            om.cylinder("Tube", 0.09, 0.2, (0.1, y, z), steel, verts=8, rot=(0, 65, 0), bevel=0.0)
    turret = om.join("Turret", om.parts_since(snap), origin=(-0.7, 0.0, 0.85))
    om.parent(turret, hull)
    om.export_glb(os.path.join(OUT, "coalition", "lancer.glb"))


def coalition_tiltrotor():
    """Cargo tiltrotor: fuselage, stub wings, two big rotors (nodes named Rotor* spin in-game)."""
    om.reset_scene()
    body = om.material("Body", (0.30, 0.34, 0.30))
    steel = om.material("Steel", STEEL)
    glass = om.material("Glass", GLASS, roughness=0.2)
    om.box("Fuselage", (2.6, 1.0, 0.9), (0.0, 0.0, 0.9), body)
    om.wedge("Nose", (0.8, 0.9, 0.8), (1.6, 0.0, 0.9), om.team())
    om.box("Cockpit", (0.4, 0.8, 0.3), (1.35, 0.0, 1.2), glass, bevel=0.0)
    om.box("Tail", (1.0, 0.12, 0.8), (-1.6, 0.0, 1.4), body, bevel=0.02)
    om.box("Wing", (0.6, 4.0, 0.12), (-0.2, 0.0, 1.35), om.team(), bevel=0.02)
    om.box("Cargo", (1.2, 0.9, 0.3), (-0.3, 0.0, 0.35), steel, bevel=0.02)
    hull = om.join_all("Hull")
    for side in (1, -1):
        snap = om.new_parts()
        om.cylinder("Hub", 0.15, 0.3, (-0.2, side * 2.0, 1.55), steel, verts=8)
        om.box("Blade1", (2.2, 0.16, 0.03), (-0.2, side * 2.0, 1.7), steel, bevel=0.0)
        om.box("Blade2", (0.16, 2.2, 0.03), (-0.2, side * 2.0, 1.7), steel, bevel=0.0)
        rotor = om.join(f"Rotor{'L' if side > 0 else 'R'}", om.parts_since(snap), origin=(-0.2, side * 2.0, 1.55))
        om.parent(rotor, hull)
        om.cylinder("Nacelle", 0.25, 0.7, (-0.2, side * 2.0, 1.2), body, verts=10)
    om.export_glb(os.path.join(OUT, "coalition", "tiltrotor.glb"))


def coalition_kestrel():
    """Attack helicopter: slim fuselage, stub wings with pods, main rotor + tail rotor."""
    om.reset_scene()
    body = om.material("Body", (0.26, 0.30, 0.28))
    steel = om.material("Steel", STEEL)
    glass = om.material("Glass", GLASS, roughness=0.2)
    om.wedge("Fuselage", (2.2, 0.7, 0.8), (0.3, 0.0, 0.9), body)
    om.box("Canopy", (0.8, 0.6, 0.35), (0.9, 0.0, 1.3), glass, bevel=0.03)
    om.box("Spine", (1.0, 0.5, 0.3), (-0.4, 0.0, 1.35), om.team(), bevel=0.02)
    om.box("TailBoom", (1.8, 0.25, 0.25), (-1.7, 0.0, 1.1), body, bevel=0.02)
    om.box("Fin", (0.4, 0.08, 0.6), (-2.5, 0.0, 1.45), om.team(), bevel=0.01)
    om.box("Wing", (0.5, 2.2, 0.1), (0.2, 0.0, 0.9), body, bevel=0.02)
    for side in (1, -1):
        om.cylinder("Pod", 0.12, 0.7, (0.3, side * 0.9, 0.8), steel, verts=8, rot=(0, 90, 0))
    for side in (1, -1):
        om.box("Skid", (1.4, 0.06, 0.06), (0.3, side * 0.4, 0.35), steel, bevel=0.0)
    hull = om.join_all("Hull")
    snap = om.new_parts()
    om.cylinder("Hub", 0.12, 0.2, (0.2, 0.0, 1.6), steel, verts=8)
    om.box("Blade1", (3.4, 0.14, 0.03), (0.2, 0.0, 1.68), steel, bevel=0.0)
    om.box("Blade2", (0.14, 3.4, 0.03), (0.2, 0.0, 1.68), steel, bevel=0.0)
    rotor = om.join("RotorMain", om.parts_since(snap), origin=(0.2, 0.0, 1.55))
    om.parent(rotor, hull)
    snap = om.new_parts()
    om.box("TailBlade", (0.06, 0.6, 0.06), (-2.55, 0.16, 1.1), steel, bevel=0.0)
    tail = om.join("RotorTail", om.parts_since(snap), origin=(-2.55, 0.16, 1.1))
    om.parent(tail, hull)
    snap = om.new_parts()
    om.cylinder("Gun", 0.05, 0.6, (1.5, 0.0, 0.55), steel, verts=8, rot=(0, 90, 0), bevel=0.0)
    turret = om.join("Turret", om.parts_since(snap), origin=(1.2, 0.0, 0.55))
    om.parent(turret, hull)
    om.export_glb(os.path.join(OUT, "coalition", "kestrel.glb"))


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
