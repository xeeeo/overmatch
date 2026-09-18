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
    from coalition_buildings import MODELS as detailed
    detailed["command_post"]()


def power_plant():
    from coalition_buildings import MODELS as detailed
    detailed["power_plant"]()


def barracks():
    from coalition_buildings import MODELS as detailed
    detailed["barracks"]()


def supply_center():
    from coalition_buildings import MODELS as detailed
    detailed["supply_center"]()


def motor_pool():
    from coalition_buildings import MODELS as detailed
    detailed["motor_pool"]()


def airfield():
    from coalition_buildings import MODELS as detailed
    detailed["airfield"]()


def strategy_center():
    from coalition_buildings import MODELS as detailed
    detailed["strategy_center"]()


def drop_zone():
    from coalition_buildings import MODELS as detailed
    detailed["drop_zone"]()


def sentry_battery():
    from coalition_buildings import MODELS as detailed
    detailed["sentry_battery"]()


def orbital_uplink():
    from coalition_buildings import MODELS as detailed
    detailed["orbital_uplink"]()


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
