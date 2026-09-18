"""Audit exported glTF geometry and the names/transforms consumed by EntityView.

python3 tools/art/audit_models.py --baseline-dir /tmp/overmatch-art-baseline-3bf70dd
The baseline manifest is also committed so repeat audits need no historical GLBs.
"""
import argparse
import json
import math
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[2]
IDENTITY = [[float(i == j) for j in range(4)] for i in range(4)]


def mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def transform(node):
    if "matrix" in node:
        return [[node["matrix"][4*j+i] for j in range(4)] for i in range(4)]
    x, y, z, w = node.get("rotation", [0, 0, 0, 1])
    s = node.get("scale", [1, 1, 1])
    t = node.get("translation", [0, 0, 0])
    r = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
         [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
         [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]
    return [[r[i][j]*s[j] for j in range(3)] + [t[i]] for i in range(3)] + [[0, 0, 0, 1]]


def inspect(path):
    raw = path.read_bytes()
    magic, version, size = struct.unpack_from("<III", raw)
    assert magic == 0x46546C67 and version == 2 and size == len(raw), path
    length, kind = struct.unpack_from("<II", raw, 12)
    assert kind == 0x4E4F534A, path
    doc = json.loads(raw[20:20+length])
    binary_start = 28 + length
    nodes = doc.get("nodes", [])
    accessors = doc.get("accessors", [])
    parent = {child: i for i, node in enumerate(nodes) for child in node.get("children", [])}
    world = {}
    def pose(i):
        if i not in world:
            world[i] = mul(pose(parent[i]) if i in parent else IDENTITY, transform(nodes[i]))
        return world[i]
    points = []
    articulated = {}
    for i, node in enumerate(nodes):
        mat = pose(i)
        name = node.get("name", "")
        if name == "Turret" or name.startswith("Rotor"):
            articulated[name] = {
                "parent": nodes[parent[i]].get("name") if i in parent else None,
                "world_matrix": [round(v, 6) for row in mat for v in row],
                "local_matrix": [round(v, 6) for row in transform(node) for v in row],
            }
        if "mesh" not in node:
            continue
        for primitive in doc["meshes"][node["mesh"]]["primitives"]:
            a = accessors[primitive["attributes"]["POSITION"]]
            assert "min" in a and "max" in a, f"Missing position bounds: {path}"
            # Read vertices, not the eight corners of their local AABB: an
            # articulated/rotated mesh's transformed AABB overstates its extent.
            assert a["componentType"] == 5126 and a["type"] == "VEC3", path
            view = doc["bufferViews"][a["bufferView"]]
            start = binary_start + view.get("byteOffset", 0) + a.get("byteOffset", 0)
            stride = view.get("byteStride", 12)
            for vertex in range(a["count"]):
                p = struct.unpack_from("<fff", raw, start + vertex*stride)
                points.append([sum(mat[j][k]*p[k] for k in range(3))+mat[j][3] for j in range(3)])
    assert points and all(math.isfinite(v) for p in points for v in p), path
    primitives = [p for m in doc["meshes"] for p in m["primitives"]]
    assert all(p.get("mode", 4) == 4 for p in primitives), f"Non-triangle primitives: {path}"
    return {
        "bytes": len(raw),
        "triangles": sum(accessors[p["indices"] if "indices" in p else p["attributes"]["POSITION"]]["count"] // 3 for p in primitives),
        "meshes": len(doc["meshes"]), "surfaces": len(primitives),
        "materials": [m.get("name", "") for m in doc.get("materials", [])],
        "bounds_min": [round(min(p[j] for p in points), 5) for j in range(3)],
        "bounds_max": [round(max(p[j] for p in points), 5) for j in range(3)],
        "articulation": articulated, "animations": len(doc.get("animations", [])),
    }


def inventory(directory):
    return {p.relative_to(directory).as_posix()[:-4]: inspect(p) for p in sorted(directory.rglob("*.glb"))}


def audit(current, baseline):
    errors, notes = [], []
    for name, old in baseline.items():
        if name not in current:
            errors.append(f"Missing model {name}")
            continue
        new = current[name]
        if "TeamColour" in old["materials"] and "TeamColour" not in new["materials"]:
            errors.append(f"{name}: missing TeamColour")
        if new["animations"]:
            errors.append(f"{name}: unexpected exported animation")
        if set(old["articulation"]) != set(new["articulation"]):
            errors.append(f"{name}: changed articulated node names")
        for node, before in old["articulation"].items():
            after = new["articulation"].get(node)
            if after is None:
                continue
            if before["parent"] != after["parent"]:
                errors.append(f"{name}/{node}: changed parent")
            for key in ("world_matrix", "local_matrix"):
                if max(abs(a-b) for a, b in zip(before[key], after[key])) > .00002:
                    errors.append(f"{name}/{node}: changed {key}")
        if new["triangles"] > 10000 or new["surfaces"] > 20:
            errors.append(f"{name}: exceeds review budget ({new['triangles']} triangles, {new['surfaces']} surfaces)")
        for axis in range(3):
            extent = old["bounds_max"][axis]-old["bounds_min"][axis]
            tolerance = max(.15, extent*(.2 if axis == 1 else .1))
            if new["bounds_min"][axis] < old["bounds_min"][axis]-tolerance or new["bounds_max"][axis] > old["bounds_max"][axis]+tolerance:
                notes.append(f"{name}: bounds expand on {'XYZ'[axis]} (review silhouette/footprint)")
    return errors, notes


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline-dir", type=Path)
    parser.add_argument("--baseline", type=Path, default=ROOT/"artifacts/art-pass/baseline-models.json")
    parser.add_argument("--output", type=Path, default=ROOT/"artifacts/art-pass/model-audit.json")
    args = parser.parse_args()
    if args.baseline_dir:
        baseline = inventory(args.baseline_dir)
        args.baseline.parent.mkdir(parents=True, exist_ok=True)
        args.baseline.write_text(json.dumps(baseline, indent=2)+"\n")
    else:
        baseline = json.loads(args.baseline.read_text())
    current = inventory(ROOT/"game/assets/models")
    errors, notes = audit(current, baseline)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps({"baseline_commit": "3bf70dd", "models": current, "errors": errors, "notes": notes}, indent=2)+"\n")
    print(f"Audited {len(current)} models: {sum(m['triangles'] for m in current.values()):,} triangles; {len(errors)} contract errors; {len(notes)} bounds notes")
    for message in errors + notes:
        print(message)
    raise SystemExit(bool(errors))


if __name__ == "__main__":
    main()
