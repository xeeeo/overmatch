"""Bulwark MBT, design 01. Original stylised Coalition vehicle, generated in Blender.

Regenerate only this unit:
    blender -b -P tools/blender/build_bulwark.py
or through the standard entry point:
    blender -b -P tools/blender/build_models.py -- bulwark

Keep the existing ground origin, +X forward, TeamColour material and Turret node.
All details are visual; this generator does not change game stats or abilities.
"""
import math
import os
import sys

import bpy
import bmesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import omlib as om

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DEFAULT_OUT = os.path.join(ROOT, "game", "assets", "models", "coalition", "bulwark.glb")
PIVOT = (-0.15, 0.0, 1.06)


def mesh(name, vertices, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(material)
    return obj


def prism(name, outline, bottom, top, mat, inset=0):
    """Chamfered plan silhouette; an inset roof gives the armour a clear slope."""
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    vertices = [(x, y, bottom) for x, y in outline]
    vertices += [(cx + (x-cx)*(1-inset), cy + (y-cy)*(1-inset), top) for x, y in outline]
    n = len(outline)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    return mesh(name, vertices, faces, mat)


def loft(name, sections, mat):
    """Eight-sided cross-sections: distinct belly, vertical sides and sloping shoulders."""
    vertices = []
    for x, width, low, high in sections:
        vertices += [(x, -width*.78, low), (x, width*.78, low),
                     (x, width, low+.065), (x, width, high-.08),
                     (x, width*.80, high), (x, -width*.80, high),
                     (x, -width, high-.08), (x, -width, low+.065)]
    faces = [tuple(reversed(range(8))), tuple(range(len(vertices)-8, len(vertices)))]
    for ring in range(len(sections)-1):
        for i in range(8):
            a = ring*8+i; b = ring*8+(i+1) % 8
            faces.append((a, b, b+8, a+8))
    return mesh(name, vertices, faces, mat)


def bake(objects):
    # Apply each piece's own bevel before joining. Joining unbaked pieces would
    # otherwise keep only the active mesh's modifiers and lose the edge treatment.
    for obj in objects:
        bpy.context.view_layer.objects.active = obj
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)


def join_at_origin(name, parts, origin):
    bake(parts)
    return om.join(name, parts, origin=origin)


def track_run(side, track, steel, recess):
    y = side*.735
    # A continuous hollow belt with deliberately broad links; not a solid block.
    outer = []; inner = []
    for center, angles in ((.88, range(-90, 91, 30)), (-.88, range(90, 271, 30))):
        for angle in angles:
            rad = math.radians(angle)
            outer.append((center + .245*math.cos(rad), .32 + .245*math.sin(rad)))
            inner.append((center + .165*math.cos(rad), .32 + .165*math.sin(rad)))
    n = len(outer)
    vertices = [(x, yy, z) for yy in (y-.16, y+.16) for ring in (outer, inner) for x, z in ring]
    faces = []
    for i in range(n):
        j = (i+1) % n
        faces += [(i, j, 2*n+j, 2*n+i),
                  (n+i, 3*n+i, 3*n+j, n+j),
                  (i, n+i, n+j, j),
                  (2*n+i, 2*n+j, 3*n+j, 3*n+i)]
    mesh(f"TrackBelt{side}", vertices, faces, track)
    om.box("TrackWell", (1.86, .17, .28), (0, y, .32), recess, bevel=0)

    for i, x in enumerate((-.83, -.42, 0, .42, .83)):
        # Front-facing wheel discs remain visible below the skirts at game scale.
        om.cylinder(f"RoadWheel{side}_{i}", .185, .29, (x, y, .30), recess,
                    verts=12, rot=(90, 0, 0), bevel=0)
        om.cylinder(f"Hub{side}_{i}", .093, .026, (x, y+side*.161, .30), steel,
                    verts=8, rot=(90, 0, 0), bevel=0)
    for x in (-1.015, 1.015):
        om.cylinder("Idler", .145, .025, (x, y+side*.163, .32), steel,
                    verts=10, rot=(90, 0, 0), bevel=0)

    # Top/bottom links plus links wrapping the ends. Flat boxes keep cost modest.
    for x in (-.81, -.57, -.33, -.09, .15, .39, .63, .84):
        for z in (.067, .573):
            om.box("Tread", (.135, .34, .024), (x, y, z), track, bevel=0)
    for center, angles in ((.88, (-60, -25, 10, 45, 80)), (-.88, (100, 135, 170, 205, 240))):
        for angle in angles:
            a = math.radians(angle)
            om.box("TreadCurve", (.125, .34, .027),
                   (center+.25*math.cos(a), y, .32+.25*math.sin(a)), track,
                   bevel=0, rot=(0, 90-angle, 0))


def build(output=DEFAULT_OUT):
    om.reset_scene()
    armour = om.material("BulwarkArmour", (.19, .235, .205), roughness=.8)
    shoulder = om.material("BulwarkEdge", (.30, .335, .28), roughness=.74)
    track = om.material("Track", (.038, .049, .046), roughness=.95)
    recess = om.material("Recess", (.018, .029, .029), roughness=.87)
    steel = om.material("Steel", (.20, .235, .235), roughness=.55, metallic=.4)
    marking = om.material("Identification", (.75, .77, .63), roughness=.76)
    lens = om.material("Optics", (.045, .37, .44), roughness=.24, metallic=.15)
    bsdf = lens.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Emission Color"].default_value = (.015, .19, .23, 1)
    bsdf.inputs["Emission Strength"].default_value = .3
    team = om.team()

    # Broad, low hull. The tapered glacis and exposed rear deck keep forward clear.
    loft("LowerHull", [(-1.06, .52, .22, .54), (-.78, .64, .23, .57),
                       (.77, .64, .23, .57), (1.08, .52, .31, .49)], recess)
    loft("CompositeHull", [(-1.075, .61, .43, .77), (-.76, .64, .43, .83),
                           (.50, .65, .41, .87), (1.075, .59, .36, .60)], armour)
    # Front glacis cap is one large plane, readable from the RTS camera.
    prism("GlacisCap", [(.39,-.43),(.9,-.44),(1.07,-.30),(1.07,.30),(.9,.44),(.39,.43)],
          .585, .615, shoulder)
    # Shoulder strips and segmented side armour expose a dark track silhouette.
    for side in (-1, 1):
        track_run(side, track, steel, recess)
        om.box("Fender", (2.13, .35, .065), (-.035, side*.727, .625), armour, bevel=.018)
        om.box("TeamShoulder", (.73, .205, .035), (.14, side*.756, .674), team, bevel=.012)
        for i, x in enumerate((-.79, -.38, .03, .44, .83)):
            # Three visible levels: hull shoulder, slab armour, exposed moving gear.
            om.box(f"Skirt{side}_{i}", (.367, .075, .24), (x, side*.919, .536),
                   team if i == 2 else armour, bevel=.018)
            om.box("SkirtEdge", (.285, .016, .025), (x, side*.960, .453), shoulder, bevel=0)
        om.box("FrontLampHousing", (.095, .15, .095), (1.083, side*.43, .547), recess, bevel=.012)
        om.box("FrontLamp", (.012, .102, .035), (1.136, side*.43, .55), marking, bevel=0)
        om.box("RearExhaust", (.12, .24, .16), (-1.085, side*.48, .575), steel, bevel=.015)
        for yy in (-.065, 0, .065):
            om.box("ExhaustSlot", (.014, .025, .10), (-1.151, side*.48+yy, .58), recess, bevel=0)

    om.box("EngineRecess", (.41, .79, .025), (-.84, 0, .80), recess, bevel=.012)
    for x in (-1.00, -.936, -.872, -.808, -.744, -.68):
        om.box("CoolingSlat", (.032, .70, .022), (x, 0, .827), steel, bevel=0)
    om.cylinder("TurretRing", .515, .12, (-.15, 0, .87), recess, verts=20, bevel=0)
    om.cylinder("TurretCollar", .47, .085, (-.15, 0, .957), steel, verts=16, bevel=.015)
    # Tiny identification marks, modelled rather than a new texture dependency.
    for x in (.74, .80, .86):
        om.box("HullMark", (.025, .17, .01), (x, -.02, .634), marking, bevel=0)
    hull = join_at_origin("Hull", [o for o in bpy.context.scene.objects if o.type == "MESH"], (0, 0, 0))

    snap = om.new_parts()
    outline = [(.52,-.25),(.25,-.62),(-.66,-.60),(-.91,-.35),
               (-.91,.35),(-.66,.60),(.25,.62),(.52,.25)]
    prism("TurretCore", outline, 1.00, 1.33, armour, inset=.15)
    # Layered composite cheeks form the recognisable forward-pointing silhouette.
    for side in (-1, 1):
        cheek = [(.49,side*.265),(.23,side*.635),(-.34,side*.60),(-.32,side*.38),(.22,side*.24)]
        prism("CompositeCheek", cheek, 1.075, 1.36, shoulder, inset=.07)
        panel = [(.22,side*.345),(.095,side*.508),(-.345,side*.50),(-.39,side*.375)]
        prism("FactionCheek", panel, 1.345, 1.366, team, inset=.03)
        om.box("CheekSeam", (.32, .018, .018), (-.07, side*.568, 1.18), recess, bevel=0)
        # Optical/APS blisters belong to the rotating turret, not the static hull.
        om.box("ProtectionPod", (.205, .11, .15), (-.53, side*.577, 1.34), armour, bevel=.018)
        om.box("PodFace", (.13, .016, .058), (-.51, side*.638, 1.36), recess, bevel=0)
        for x in (-.78, -.64):
            om.cylinder("SmokeCanister", .039, .18, (x, side*.46, 1.16), steel,
                        verts=8, rot=(side*25, 35, 0), bevel=0)

    om.box("Bustle", (.34, .81, .23), (-.835, 0, 1.20), armour, bevel=.028)
    om.box("BustleCap", (.26, .71, .032), (-.84, 0, 1.337), team, bevel=.012)
    om.box("BustleRecess", (.024, .58, .105), (-1.018, 0, 1.20), recess, bevel=0)
    for yy in (-.20, -.1, 0, .1, .2):
        om.box("BustleVent", (.032, .025, .095), (-1.036, yy, 1.20), steel, bevel=0)

    # Single main gun retains the old muzzle envelope and elevation; no VFX edits.
    om.box("MantletSeal", (.22, .39, .24), (.405, 0, 1.18), recess, bevel=.022)
    om.box("Mantlet", (.31, .30, .205), (.52, 0, 1.22), armour, bevel=.035)
    om.cylinder("GunShroud", .078, .31, (.725, 0, 1.235), armour,
                verts=10, rot=(0,90,0), bevel=0)
    om.cylinder("Barrel", .052, .59, (1.10, 0, 1.235), steel,
                verts=10, rot=(0,90,0), bevel=0)
    om.cylinder("FumeExtractor", .073, .19, (1.01, 0, 1.235), armour,
                verts=10, rot=(0,90,0), bevel=.006)
    om.cylinder("MuzzleCollar", .071, .135, (1.435, 0, 1.235), steel,
                verts=10, rot=(0,90,0), bevel=0)
    om.cylinder("Bore", .049, .008, (1.507, 0, 1.235), recess,
                verts=10, rot=(0,90,0), bevel=0)

    om.cylinder("HatchRim", .176, .047, (-.37, .19, 1.355), recess, verts=12, bevel=0)
    om.cylinder("Hatch", .148, .04, (-.37, .19, 1.393), shoulder, verts=10, bevel=.009)
    om.box("HatchGrip", (.11, .035, .027), (-.385, .185, 1.427), steel, bevel=.007)
    om.box("SightPedestal", (.195, .195, .08), (-.37, -.205, 1.369), recess, bevel=.012)
    om.box("ArmouredSight", (.23, .19, .14), (-.35, -.205, 1.47), armour, bevel=.022)
    om.box("SightGlass", (.012, .125, .065), (-.224, -.205, 1.481), lens, bevel=0)
    om.box("SightHood", (.075, .21, .025), (-.245, -.205, 1.56), shoulder, bevel=.006)
    # Short protected aerial avoids a noisy needle silhouette at long distance.
    om.cylinder("AerialBase", .045, .065, (-.79, .23, 1.399), steel, verts=8, bevel=0)
    om.cylinder("Aerial", .013, .165, (-.79, .23, 1.506), recess, verts=6, bevel=0)
    for yy in (-.08, .025):
        om.box("TurretID", (.105, .025, .013), (-.555, yy, 1.342), marking, bevel=0)
    turret = join_at_origin("Turret", om.parts_since(snap), PIVOT)
    om.parent(turret, hull)

    # Enforce the runtime articulation/material contract and keep export reproducible.
    assert hull.name == "Hull" and turret.name == "Turret"
    assert turret.parent == hull
    assert all(abs(v) < 1e-6 for v in turret.rotation_euler), "Turret must start with identity rotation"
    assert any(mat.name == "TeamColour" for mat in bpy.data.materials)
    bpy.context.view_layer.update()
    triangles = sum(len(p.vertices)-2 for obj in (hull, turret) for p in obj.data.polygons)
    assert triangles <= 5000, f"Bulwark exceeds 5k triangle budget: {triangles}"
    os.makedirs(os.path.dirname(os.path.abspath(output)), exist_ok=True)
    om.export_glb(output)
    print(f"[bulwark] exported {output}; {triangles} triangles; Hull + Turret; TeamColour preserved")


if __name__ == "__main__":
    build()
