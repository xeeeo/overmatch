"""Render the actual Bulwark GLBs in a reproducible, neutral studio.

Run from any directory (Blender 4.x/5.x, no extra Python dependencies):
    blender -b -P tools/art/render_bulwark.py
    blender -b -P tools/art/render_bulwark.py -- --mode hero
    blender -b -P tools/art/render_bulwark.py -- --mode comparison --samples 32

The GLBs are read-only. Geometry, shading and material names are preserved.
TeamColour alone uses the runtime player's blue, converted from sRGB to scene
linear; other material values are untouched. A common ground alignment and
display translation are applied. Both comparison subjects use one
orthographic camera at 1:1 scale.
All text, lighting and staging are native Blender objects, not raster edits.
"""

import argparse
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
VIEW_DIRECTION = Vector((7.0, 9.0, 7.2)).normalized()


def arguments():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mode", choices=("all", "hero", "comparison", "views"), default="all")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "artifacts/unit-review/bulwark")
    parser.add_argument("--model", type=Path, default=ROOT / "game/assets/models/coalition/bulwark.glb")
    parser.add_argument("--before", type=Path, default=ROOT / "game/unit_review/assets/bulwark_before.glb")
    parser.add_argument("--samples", type=int, default=64)
    return parser.parse_args(argv)


def reset(width, height, samples):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.seed = 17
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 6
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.color_depth = "8"
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = -0.35
    scene.world = bpy.data.worlds.new("Review studio atmosphere")
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.11, 0.14, 0.19, 1)
    background.inputs["Strength"].default_value = 0.32
    return scene


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner)
              for obj in objects if obj.type == "MESH" for corner in obj.bound_box]
    if not points:
        raise ValueError("The imported GLB contains no mesh geometry")
    lo = Vector(tuple(min(point[axis] for point in points) for axis in range(3)))
    hi = Vector(tuple(max(point[axis] for point in points) for axis in range(3)))
    return lo, hi, points


def import_model(path, name):
    if not path.is_file():
        raise FileNotFoundError(f"Model is not available: {path}")
    old_objects = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
    objects = [obj for obj in bpy.data.objects if obj not in old_objects]
    # Godot recolours this named surface from an sRGB player colour at runtime.
    # Blender shader inputs are scene-linear, so reproduce that visible colour
    # instead of displaying the placeholder linear blue stored in the GLB.
    def linear(channel):
        return channel / 12.92 if channel <= 0.04045 else ((channel + 0.055) / 1.055) ** 2.4

    runtime_blue = tuple(linear(channel) for channel in (0.23, 0.49, 0.85)) + (1.0,)
    imported_materials = {slot.material for obj in objects if obj.type == "MESH"
                          for slot in obj.material_slots if slot.material}
    for mat in imported_materials:
        if mat.name.split(".")[0] == "TeamColour" and mat.use_nodes:
            shader = mat.node_tree.nodes.get("Principled BSDF")
            if shader is not None:
                shader.inputs["Base Color"].default_value = runtime_blue
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    # Parent only original top-level nodes, retaining any turret hierarchy.
    for obj in objects:
        if obj.parent is None:
            world = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = world
    bpy.context.view_layer.update()
    lo, hi, _ = bounds(objects)
    root.location = (-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z)
    bpy.context.view_layer.update()
    return root, objects


def material(name, color, roughness=0.75, emission=False):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    shader = nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        shader.inputs["Emission Color"].default_value = (*color, 1)
        shader.inputs["Emission Strength"].default_value = 1.0
    return mat


def studio(spread=1.0):
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.025))
    floor = bpy.context.object
    floor.name = "Review only / charcoal studio floor"
    floor.data.materials.append(material("Review only / floor", (0.023, 0.029, 0.036), 0.82))

    def area(name, position, energy, size, color):
        lamp = bpy.data.lights.new(name, "AREA")
        lamp.energy = energy * spread
        lamp.shape = "DISK"
        lamp.size = size * math.sqrt(spread)
        lamp.color = color
        obj = bpy.data.objects.new(name, lamp)
        bpy.context.collection.objects.link(obj)
        obj.location = position
        obj.rotation_euler = (Vector((0, 0, 0.5)) - obj.location).to_track_quat("-Z", "Y").to_euler()

    area("Review only / warm soft key", (4.5, -3.5, 8.0), 1150, 5.0, (1.0, 0.89, 0.77))
    area("Review only / broad fill", (1.0, 6.5, 5.5), 600, 5.0, (0.83, 0.90, 1.0))
    area("Review only / cool rim", (-5.0, -2.0, 5.5), 1250, 4.0, (0.66, 0.80, 1.0))


def stage_outline(center, radius):
    """A subdued geometric stage edge; this is never part of the unit asset."""
    curve = bpy.data.curves.new("Review only / stage edge", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 1
    curve.bevel_depth = 0.003
    curve.bevel_resolution = 1
    spline = curve.splines.new("POLY")
    spline.points.add(95)
    for index, point in enumerate(spline.points):
        angle = index * 2 * math.pi / 96
        point.co = (center.x + radius * math.cos(angle), center.y + radius * math.sin(angle), -0.017, 1)
    spline.use_cyclic_u = True
    obj = bpy.data.objects.new("Review only / stage edge", curve)
    bpy.context.collection.objects.link(obj)
    curve.materials.append(material("Review only / stage edge", (0.065, 0.079, 0.087), 0.8))


def camera(target, scale, direction=VIEW_DIRECTION):
    data = bpy.data.cameras.new("Review orthographic camera")
    data.type = "ORTHO"
    data.ortho_scale = scale
    data.lens = 50
    data.clip_end = 500
    obj = bpy.data.objects.new("Review orthographic camera", data)
    bpy.context.collection.objects.link(obj)
    obj.location = Vector(target) + Vector(direction).normalized() * 30
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = obj
    return obj


def frame_info(objects, direction=VIEW_DIRECTION):
    rotation = (-Vector(direction)).to_track_quat("-Z", "Y")
    right = rotation @ Vector((1, 0, 0))
    up = rotation @ Vector((0, 1, 0))
    lo, hi, points = bounds(objects)
    horizontal = [point.dot(right) for point in points]
    vertical = [point.dot(up) for point in points]
    return right, up, max(horizontal) - min(horizontal), max(vertical) - min(vertical), (lo + hi) / 2


def label(cam, body, x, y, size, color=(0.64, 0.71, 0.77), align="LEFT"):
    """Place native text at normalized image coordinates (0,0 is bottom left)."""
    scene = bpy.context.scene
    aspect = scene.render.resolution_x / scene.render.resolution_y
    width = cam.data.ortho_scale
    height = width / aspect
    rotation = cam.rotation_euler.to_quaternion()
    right = rotation @ Vector((1, 0, 0))
    up = rotation @ Vector((0, 1, 0))
    forward = rotation @ Vector((0, 0, -1))
    curve = bpy.data.curves.new("Review only / caption", "FONT")
    curve.body = body
    curve.align_x = align
    curve.size = size * height
    curve.space_character = 1.12
    curve.resolution_u = 8
    obj = bpy.data.objects.new("Review only / " + body, curve)
    bpy.context.collection.objects.link(obj)
    obj.location = cam.location + forward * 3 + right * ((x - 0.5) * width) + up * ((y - 0.5) * height)
    obj.rotation_euler = cam.rotation_euler
    curve.materials.append(material("Review only / caption ink", color, emission=True))
    # Floating review labels cannot cast shadows onto the actual model.
    obj.visible_shadow = False


def render(path):
    bpy.context.scene.render.filepath = str(path.resolve())
    bpy.ops.render.render(write_still=True)
    print(f"Review render written: {path.resolve()}")


def hero(args):
    reset(1600, 1000, args.samples)
    _, objects = import_model(args.model, "Proposed Bulwark")
    right, up, width, height, center = frame_info(objects)
    # ortho_scale is image width, including for landscape frames.
    scale = max(width * 1.32, height * 1.6 * 1.55)
    cam = camera(center + up * 0.04, scale)
    studio()
    stage_outline(Vector((0, 0, 0)), max(width * 0.58, 1.85))
    label(cam, "COALITION   /   MAIN BATTLE TANK", 0.065, 0.92, 0.016, (0.46, 0.57, 0.66))
    label(cam, "BULWARK", 0.065, 0.855, 0.051, (0.79, 0.85, 0.89))
    label(cam, "UNIT DESIGN STUDY   /   01", 0.065, 0.075, 0.016)
    label(cam, "ACTUAL GAME ASSET", 0.935, 0.075, 0.016, align="RIGHT")
    render(args.output_dir / "01-bulwark-hero.png")


def comparison(args):
    reset(1600, 900, args.samples)
    old_root, old_objects = import_model(args.before, "Existing Bulwark")
    new_root, new_objects = import_model(args.model, "Proposed Bulwark")
    right, up, old_width, old_height, old_center = frame_info(old_objects)
    _, _, new_width, new_height, new_center = frame_info(new_objects)
    common_width = max(old_width, new_width)
    common_height = max(old_height, new_height)
    separation = common_width * 1.32
    old_root.location -= right * separation / 2
    new_root.location += right * separation / 2
    bpy.context.view_layer.update()
    scale = max(common_width * 2.8, common_height * (1600 / 900) * 1.65)
    center = (old_center + new_center) / 2 + up * 0.06
    cam = camera(center, scale)
    studio(1.35)
    stage_outline(-right * separation / 2, max(common_width * 0.52, 1.8))
    stage_outline(right * separation / 2, max(common_width * 0.52, 1.8))
    label(cam, "BULWARK   /   DESIGN COMPARISON", 0.055, 0.92, 0.027, (0.79, 0.85, 0.89))
    label(cam, "01   EXISTING", 0.25, 0.81, 0.026, align="CENTER")
    label(cam, "02   PROPOSED", 0.75, 0.81, 0.026, (0.79, 0.85, 0.89), align="CENTER")
    label(cam, "SAME CAMERA   /   SAME WORLD SCALE   /   RUNTIME TEAM COLOUR", 0.055, 0.07, 0.017)
    render(args.output_dir / "02-bulwark-comparison.png")


def views(args):
    # Separate full-size orthographic captures make silhouette inspection easy.
    # Scale is shared across these three images; no asset geometry is changed.
    directions = (("front", Vector((1, 0, 0))), ("side", Vector((0, 1, 0))),
                  ("top", Vector((0, 0, 1))))
    reset(1200, 900, args.samples)
    _, objects = import_model(args.model, "Proposed Bulwark")
    lo, hi, _ = bounds(objects)
    common_scale = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z) * 1.7
    for index, (name, direction) in enumerate(directions, 3):
        reset(1200, 900, args.samples)
        _, objects = import_model(args.model, "Proposed Bulwark")
        _, _, _, _, center = frame_info(objects, direction)
        cam = camera(center, common_scale, direction)
        studio()
        label(cam, "BULWARK   /   " + name.upper(), 0.065, 0.91, 0.027, (0.79, 0.85, 0.89))
        label(cam, "ORTHOGRAPHIC   /   ACTUAL GAME ASSET", 0.065, 0.075, 0.017)
        render(args.output_dir / f"{index:02d}-bulwark-{name}.png")


def main():
    args = arguments()
    if args.samples < 1:
        raise ValueError("--samples must be positive")
    if args.mode in ("all", "comparison") and not args.before.is_file():
        raise FileNotFoundError(f"Comparison requires the baseline GLB: {args.before}")
    args.output_dir.mkdir(parents=True, exist_ok=True)
    if args.mode in ("all", "hero"):
        hero(args)
    if args.mode in ("all", "comparison"):
        comparison(args)
    if args.mode == "views":
        views(args)


if __name__ == "__main__":
    main()
