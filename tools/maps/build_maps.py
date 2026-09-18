"""Generates Overmatch skirmish maps as JSON into game/data/maps/.

Layouts are authored for one corner/side and mirrored so every start is fair.

    python3 tools/maps/build_maps.py
"""
import json
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "game", "data", "maps")


def rect(x, y, w, h):
    return {"x": int(x), "y": int(y), "w": int(w), "h": int(h)}


def mirror_rects(rects, size, mode):
    """mode: 'point' (180° rotation), 'quad' (4-way), 'none'."""
    W, H = size
    out = []
    for r in rects:
        variants = [r]
        if mode in ("point", "quad"):
            variants.append(rect(W - r["x"] - r["w"], H - r["y"] - r["h"], r["w"], r["h"]))
        if mode == "quad":
            variants.append(rect(W - r["x"] - r["w"], r["y"], r["w"], r["h"]))
            variants.append(rect(r["x"], H - r["y"] - r["h"], r["w"], r["h"]))
        for v in variants:
            if v not in out:
                out.append(v)
    return out


def mirror_points(points, size, mode, keys=("x", "y")):
    W, H = size
    out = []
    for p in points:
        variants = [dict(p)]
        if mode in ("point", "quad"):
            q = dict(p); q["x"] = W - p["x"]; q["y"] = H - p["y"]; variants.append(q)
        if mode == "quad":
            q = dict(p); q["x"] = W - p["x"]; variants.append(q)
            q = dict(p); q["y"] = H - p["y"]; variants.append(q)
        for v in variants:
            if v not in out:
                out.append(v)
    return out


def mirror_neutrals(neutrals, size, mode, sizes):
    """Neutral buildings are placed by footprint origin, so mirror using their footprint."""
    W, H = size
    out = []
    for n in neutrals:
        w, h = sizes[n["id"]]
        variants = [dict(n)]
        if mode in ("point", "quad"):
            variants.append({"id": n["id"], "x": W - n["x"] - w, "y": H - n["y"] - h})
        if mode == "quad":
            variants.append({"id": n["id"], "x": W - n["x"] - w, "y": n["y"]})
            variants.append({"id": n["id"], "x": n["x"], "y": H - n["y"] - h})
        for v in variants:
            if v not in out:
                out.append(v)
    return out


SIZES = {"oil_derrick": (3, 3), "civilian_house": (3, 3), "civilian_block": (4, 4)}


def build(map_id, name, size, mode, spawns, supplies, blocked=(), water=(), rough=(), road=(), neutrals=(), description=""):
    m = {
        "id": map_id, "name": name, "description": description, "width": size[0], "height": size[1],
        "spawns": mirror_points(spawns, size, mode),
        "supplies": mirror_points(supplies, size, mode),
        "blocked": mirror_rects(list(blocked), size, mode),
        "water": mirror_rects(list(water), size, mode),
        "rough": mirror_rects(list(rough), size, mode),
        "road": mirror_rects(list(road), size, mode),
        "neutrals": mirror_neutrals(list(neutrals), size, mode, SIZES),
    }
    # Spawn order: keep opposite corners as players 1 and 2.
    json.dump(m, open(os.path.join(OUT, f"{map_id}.json"), "w"), indent=2)
    print(f"[maps] {map_id}: {size[0]}x{size[1]}, {len(m['spawns'])} spawns, {len(m['supplies'])} piles, {len(m['neutrals'])} neutrals")


def main():
    os.makedirs(OUT, exist_ok=True)

    # 1. Twin Rivers — 2 players, two rivers with fords, a contested town in the middle.
    build("twin_rivers", "Twin Rivers (2)", (96, 80), "point",
          spawns=[{"x": 14, "y": 14}],
          supplies=[{"x": 24, "y": 8, "amount": 30000}, {"x": 8, "y": 26, "amount": 30000}, {"x": 40, "y": 18, "amount": 20000}, {"x": 20, "y": 60, "amount": 20000}],
          water=[rect(30, 0, 4, 22), rect(30, 28, 4, 22), rect(30, 56, 4, 24)],   # river with two fords (gaps at y 22-28 and 50-56)
          road=[rect(0, 23, 96, 3)],
          rough=[rect(36, 56, 12, 12)],
          blocked=[rect(10, 40, 8, 3), rect(22, 34, 3, 8), rect(40, 8, 3, 6)],
          neutrals=[{"id": "oil_derrick", "x": 40, "y": 30}, {"id": "civilian_house", "x": 42, "y": 40}, {"id": "civilian_block", "x": 46, "y": 34}],
          description="Two rivers, four fords. Hold the town between them.")

    # 2. Crossroads — 4 players, roads meet at a town with derricks.
    build("crossroads", "Crossroads (4)", (112, 112), "quad",
          spawns=[{"x": 16, "y": 16}],
          supplies=[{"x": 28, "y": 8, "amount": 30000}, {"x": 8, "y": 28, "amount": 30000}, {"x": 34, "y": 34, "amount": 20000}],
          road=[rect(0, 54, 112, 4), rect(54, 0, 4, 112)],
          blocked=[rect(24, 40, 10, 3), rect(40, 24, 3, 10), rect(44, 44, 4, 4), rect(14, 50, 6, 2)],
          rough=[rect(30, 14, 10, 8)],
          neutrals=[{"id": "oil_derrick", "x": 48, "y": 40}, {"id": "civilian_house", "x": 46, "y": 50}, {"id": "civilian_house", "x": 50, "y": 46}],
          description="Four corners, two highways, one town everyone wants.")

    # 3. Highlands — 2 players, rock ridges and chokepoints.
    build("highlands", "Highlands (2)", (88, 88), "point",
          spawns=[{"x": 14, "y": 14}],
          supplies=[{"x": 26, "y": 8, "amount": 30000}, {"x": 8, "y": 26, "amount": 30000}, {"x": 60, "y": 22, "amount": 25000}],
          blocked=[rect(0, 34, 26, 4), rect(32, 34, 8, 4), rect(34, 0, 4, 22), rect(34, 28, 4, 6), rect(46, 38, 4, 12), rect(20, 46, 14, 3), rect(56, 10, 3, 10)],
          rough=[rect(40, 40, 8, 8)],
          neutrals=[{"id": "oil_derrick", "x": 40, "y": 20}, {"id": "civilian_house", "x": 28, "y": 40}],
          description="Ridges and passes. Whoever holds the gaps holds the map.")

    # 4. Oil Rush — 4 players, thin supplies, rich in derricks.
    build("oil_rush", "Oil Rush (4)", (104, 104), "quad",
          spawns=[{"x": 15, "y": 15}],
          supplies=[{"x": 26, "y": 8, "amount": 18000}, {"x": 8, "y": 26, "amount": 18000}],
          blocked=[rect(30, 30, 6, 6), rect(18, 42, 3, 8), rect(42, 18, 8, 3)],
          water=[rect(46, 46, 6, 6)],
          road=[rect(0, 50, 104, 3)],
          neutrals=[{"id": "oil_derrick", "x": 30, "y": 16}, {"id": "oil_derrick", "x": 16, "y": 30}, {"id": "oil_derrick", "x": 40, "y": 40}, {"id": "civilian_house", "x": 38, "y": 46}],
          description="The piles run dry fast. The derricks never do.")

    # 5. Open Steppe — 2 players, big and empty: a tank map.
    build("open_steppe", "Open Steppe (2)", (120, 72), "point",
          spawns=[{"x": 14, "y": 36}],
          supplies=[{"x": 8, "y": 22, "amount": 30000}, {"x": 8, "y": 50, "amount": 30000}, {"x": 36, "y": 12, "amount": 25000}, {"x": 36, "y": 60, "amount": 25000}],
          blocked=[rect(28, 34, 3, 5), rect(48, 20, 5, 3), rect(48, 50, 5, 3)],
          rough=[rect(54, 30, 12, 12)],
          road=[rect(0, 35, 120, 3)],
          neutrals=[{"id": "oil_derrick", "x": 56, "y": 10}, {"id": "civilian_house", "x": 58, "y": 44}],
          description="Nowhere to hide. Bring armour.")


if __name__ == "__main__":
    main()
