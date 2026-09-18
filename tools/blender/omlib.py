"""Shared helpers for Overmatch model generators. Run inside Blender: blender -b -P build_models.py

Conventions
- Units: 1 Blender unit = 1 world unit (a tank hull is ~2 units long).
- Forward is +X, up is +Z (Blender). The glTF exporter turns that into +X forward, +Y up in Godot.
- Origin at ground level under the model's centre.
- Material named "TeamColour" is recoloured per player by the game. Everything else is baked flat colour.
- Meshes are flat-shaded, low-poly, with a small bevel for that chunky look.
"""
import bpy
import math

_materials = {}


def reset_scene():
    global TEAM
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _materials.clear()
    TEAM = None


def material(name, rgb, roughness=0.75, metallic=0.0):
    if name in _materials:
        return _materials[name]
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    _materials[name] = mat
    return mat


TEAM = None


def team():
    global TEAM
    if TEAM is None:
        TEAM = material("TeamColour", (0.23, 0.49, 0.85))
    return TEAM


def _finish(obj, mat, bevel):
    obj.data.materials.append(mat)
    for p in obj.data.polygons:
        p.use_smooth = False
    if bevel:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
    return obj


def box(name, size, loc, mat, bevel=0.03, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    obj.rotation_euler = tuple(math.radians(r) for r in rot)
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    return _finish(obj, mat, bevel)


def cylinder(name, radius, depth, loc, mat, verts=12, rot=(0, 0, 0), bevel=0.02):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = tuple(math.radians(r) for r in rot)
    bpy.ops.object.transform_apply(rotation=True)
    return _finish(obj, mat, bevel)


def wedge(name, size, loc, mat, bevel=0.03):
    """A box with the top-front edge pulled back: sloped glacis plates."""
    obj = box(name, size, loc, mat, bevel)
    sx, sy, sz = size
    # Move top-front vertices back by 35% of the length.
    for v in obj.data.vertices:
        if v.co.z > 0 and v.co.x > 0:
            v.co.x -= sx * 0.35
    return obj


def join_all(name):
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    root = bpy.context.active_object
    root.name = name
    return root


def export_glb(path):
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format="GLB",
        export_apply=True,
        export_yup=True,
        export_materials="EXPORT",
        export_animations=False,
        export_skins=False,
        export_lights=False,
        export_cameras=False,
    )
