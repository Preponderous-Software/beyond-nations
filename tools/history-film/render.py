"""Render a Beyond Nations history stage (from tools/history-to-usd.py) as an orbiting timelapse.

Runs inside Blender, headless:

    blender -b --threads 4 --python tools/history-film/render.py -- \\
        out/history_flat.usdc out/frames [--size 1280x720] [--engine cycles|workbench]

Cycles (the default) renders on the CPU anywhere; Workbench needs a working OpenGL driver
(it rendered blank frames under Mesa llvmpipe). Import the *flattened* stage
(``history-to-usd.py --flatten``): Blender does not resolve value clips. Writes one PNG per
time code to the frames directory; tools/history-film/finish.py adds the overlay and encodes
the film.

What Blender's USD importer does not carry over is put back here from the stage itself, read
with the ``pxr`` module Blender bundles:

* time-sampled ``visibility`` (entities before they exist and pawns inside a settlement,
  ground chunks before they are generated) becomes ``hide_render`` keyframes;
* ``primvars:displayColor`` is shown through a material (and Workbench's attribute colour).

Pawns are one unit across in a world thousands of units wide, so pawns and settlements are
drawn larger than life (``--pawn-scale``, ``--settlement-scale``), grown from their base so
they still stand on the ground. Nothing else is scaled.
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from pxr import Usd, UsdGeom


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(prog="render.py")
    parser.add_argument("stage", help="flattened history stage (.usdc/.usda)")
    parser.add_argument("frames", help="directory to write NNNN.png frames into")
    parser.add_argument("--size", default="1280x720")
    parser.add_argument("--engine", choices=("cycles", "workbench"), default="cycles")
    parser.add_argument("--samples", type=int, default=16, help="Cycles samples")
    parser.add_argument("--pawn-scale", type=float, default=20.0)
    parser.add_argument("--settlement-scale", type=float, default=5.0)
    parser.add_argument("--turns", type=float, default=1.0, help="orbits over the whole film")
    parser.add_argument("--elevation", type=float, default=52.0, help="camera pitch, degrees")
    parser.add_argument("--only", default=None, help="render only these frames, e.g. 1,40,80")
    return parser.parse_args(argv)


def prim_path_of(obj: bpy.types.Object) -> str | None:
    for constraint in obj.constraints:
        if constraint.type == "TRANSFORM_CACHE" and constraint.object_path:
            return constraint.object_path
    for modifier in obj.modifiers:
        if modifier.type == "MESH_SEQUENCE_CACHE" and modifier.object_path:
            return modifier.object_path
    return None


def visibility_attr(stage: Usd.Stage, path: str) -> Usd.Attribute | None:
    """The time-sampled visibility governing ``path``: its own, or its parent Xform's (an
    entity's visibility is on e_<id>, its geometry on e_<id>/geom)."""
    prim = stage.GetPrimAtPath(path)
    while prim and prim.GetPath() != "/":
        attr = UsdGeom.Imageable(prim).GetVisibilityAttr()
        if attr and attr.GetNumTimeSamples() > 0:
            return attr
        prim = prim.GetParent()
    return None


def keyframe_visibility(obj, attr: Usd.Attribute, start: int, end: int) -> None:
    times = attr.GetTimeSamples()
    previous = None
    for frame in range(start, end + 1):
        # Only frames where it might change need checking; samples are sparse.
        if previous is not None and frame not in times and frame - 1 not in times:
            continue
        hidden = attr.Get(frame) == UsdGeom.Tokens.invisible
        if hidden != previous:
            obj.hide_render = hidden
            obj.hide_viewport = hidden
            obj.keyframe_insert("hide_render", frame=frame)
            obj.keyframe_insert("hide_viewport", frame=frame)
            previous = hidden


def grow_from_base(obj, factor: float) -> None:
    """Scale an entity's primitive by ``factor`` about the bottom of the primitive (local
    y = -1 for Core's capsules and cylinders, which are two units tall), after the USD cache
    has produced the mesh for the frame."""
    group = bpy.data.node_groups.get(f"grow_{factor}")
    if group is None:
        group = bpy.data.node_groups.new(f"grow_{factor}", "GeometryNodeTree")
        group.interface.new_socket("Geometry", in_out="INPUT", socket_type="NodeSocketGeometry")
        group.interface.new_socket("Geometry", in_out="OUTPUT", socket_type="NodeSocketGeometry")
        nodes = group.nodes
        inp, out = nodes.new("NodeGroupInput"), nodes.new("NodeGroupOutput")
        transform = nodes.new("GeometryNodeTransform")
        transform.inputs["Scale"].default_value = (factor, factor, factor)
        transform.inputs["Translation"].default_value = (0.0, factor - 1.0, 0.0)
        group.links.new(inp.outputs[0], transform.inputs["Geometry"])
        group.links.new(transform.outputs["Geometry"], out.inputs[0])
    modifier = obj.modifiers.new("grow", "NODES")
    modifier.node_group = group


def ground_track(stage: Usd.Stage, start: int, end: int, step: int) -> list[tuple[int, tuple]]:
    """(frame, (centre x, centre y, radius)) in Blender's Z-up space every ``step`` frames,
    framing the ground chunks visible at that frame. The stage is Y-up, which Blender's
    importer turns into (x, -z, y). Radius never shrinks and the centre is eased."""
    chunks = []
    ground = stage.GetPrimAtPath("/World/Ground")
    for prim in ground.GetChildren() if ground else []:
        lo, hi = UsdGeom.Boundable(prim).GetExtentAttr().Get()
        chunks.append((UsdGeom.Imageable(prim).GetVisibilityAttr(), lo, hi))
    frames = list(range(start, end + 1, step))
    if frames[-1] != end:
        frames.append(end)
    track, centre, radius = [], None, 0.0
    for frame in frames:
        boxes = [(lo, hi) for vis, lo, hi in chunks if vis.Get(frame) != UsdGeom.Tokens.invisible]
        if not boxes:
            boxes = [(lo, hi) for _, lo, hi in chunks] or [((-100, 0, -100), (100, 0, 100))]
        x0 = min(lo[0] for lo, _ in boxes)
        x1 = max(hi[0] for _, hi in boxes)
        z0 = min(lo[2] for lo, _ in boxes)
        z1 = max(hi[2] for _, hi in boxes)
        target = ((x0 + x1) / 2, -(z0 + z1) / 2)
        centre = target if centre is None else tuple(c + (t - c) * 0.15 for c, t in zip(centre, target))
        need = math.hypot(x1 - x0, z1 - z0) / 2 + math.hypot(target[0] - centre[0], target[1] - centre[1])
        radius = max(radius, need)
        track.append((frame, (centre[0], centre[1], radius)))
    return track


def display_color_material(name: str, glow: float = 0.0) -> bpy.types.Material:
    """A material coloured by the mesh's displayColor; ``glow`` adds that colour as emission
    so small things (settlements) stay legible from far away."""
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    attribute = nodes.new("ShaderNodeAttribute")
    attribute.attribute_name = "displayColor"
    material.node_tree.links.new(attribute.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.85
    if glow:
        material.node_tree.links.new(attribute.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = glow
    return material


def main() -> None:
    args = parse_args()
    width, height = (int(v) for v in args.size.lower().split("x"))
    stage_path = str(Path(args.stage).resolve())
    frames_dir = Path(args.frames).resolve()
    frames_dir.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.edit.keyframe_new_interpolation_type = "CONSTANT"
    bpy.ops.wm.usd_import(filepath=stage_path, import_visible_only=False, set_frame_range=True)
    scene = bpy.context.scene

    stage = Usd.Stage.Open(stage_path)
    start, end = int(stage.GetStartTimeCode()), int(stage.GetEndTimeCode())
    scene.frame_start, scene.frame_end = start, end

    material = display_color_material("displayColor")
    beacon = display_color_material("settlement", glow=1.5)
    counts = {"entities": 0, "ground": 0, "unmapped": 0}
    for obj in list(scene.objects):
        if obj.type != "MESH":
            continue
        path = prim_path_of(obj)
        if path is None:
            # Static meshes have no cache to name their prim; ground chunks are uniquely named.
            path = f"/World/Ground/{obj.name}" if stage.GetPrimAtPath(f"/World/Ground/{obj.name}") else None
        if path is None:
            counts["unmapped"] += 1
            continue
        obj.data.materials.clear()
        obj.data.materials.append(material)
        if "displayColor" in obj.data.color_attributes:
            obj.data.color_attributes.active_color = obj.data.color_attributes["displayColor"]
        attr = visibility_attr(stage, path)
        if attr is not None:
            keyframe_visibility(obj, attr, start, end)
        if path.startswith("/World/Ground/"):
            counts["ground"] += 1
            continue
        counts["entities"] += 1
        entity = stage.GetPrimAtPath(path).GetParent()
        kind = entity.GetAttribute("extras:kind").Get(end)
        if kind == "pawn":
            grow_from_base(obj, args.pawn_scale)
        elif kind == "settlement":
            grow_from_base(obj, args.settlement_scale)
            obj.data.materials[0] = beacon
    print(f"render.py: {counts}", flush=True)

    # Camera: orbit the world as it grows. Every few frames, frame the ground generated so
    # far (eased, and never zooming back in), and turn once around it over the film.
    track = ground_track(stage, start, end, step=5)
    pivot = bpy.data.objects.new("orbit", None)
    scene.collection.objects.link(pivot)
    camera = bpy.data.objects.new("camera", bpy.data.cameras.new("camera"))
    scene.collection.objects.link(camera)
    camera.parent = pivot
    camera.data.lens = 35
    camera.data.clip_start = 5
    camera.data.clip_end = 100000
    pitch = math.radians(args.elevation)
    camera.rotation_euler = (math.pi / 2 - pitch, 0.0, 0.0)
    scene.camera = camera
    aspect = width / height
    half_fov = math.atan(18.0 / aspect / camera.data.lens)  # vertical, 36 mm sensor fitted across
    for frame, (cx, cy, radius) in track:
        distance = (radius + 120.0) / math.tan(half_fov)
        pivot.location = (cx, cy, 0.0)
        pivot.rotation_euler = (0.0, 0.0, 2 * math.pi * args.turns * (frame - start) / max(1, end - start))
        camera.location = (0.0, -distance * math.cos(pitch), distance * math.sin(pitch))
        pivot.keyframe_insert("location", frame=frame)
        pivot.keyframe_insert("rotation_euler", index=2, frame=frame)
        camera.keyframe_insert("location", frame=frame)
    for obj in (pivot, camera):
        for fcurve in _fcurves(obj):
            for point in fcurve.keyframe_points:
                point.interpolation = "LINEAR"

    world = bpy.data.worlds.new("world")
    scene.world = world
    world.color = (0.03, 0.04, 0.06)
    render = scene.render
    render.resolution_x, render.resolution_y = width, height
    render.resolution_percentage = 100
    render.image_settings.file_format = "PNG"
    render.film_transparent = False
    if args.engine == "cycles":
        render.engine = "CYCLES"
        scene.cycles.samples = args.samples
        scene.cycles.use_denoising = True
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.03, 0.04, 0.06, 1)
        world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy = 3.5
        sun.rotation_euler = (math.radians(40), 0, math.radians(30))
        scene.collection.objects.link(sun)
    else:
        render.engine = "BLENDER_WORKBENCH"
        shading = scene.display.shading
        shading.light = "STUDIO"
        shading.color_type = "VERTEX"  # the active colour attribute, i.e. displayColor
        shading.show_shadows = True
        shading.shadow_intensity = 0.35
        shading.show_cavity = True
        shading.cavity_type = "WORLD"
        shading.background_type = "WORLD"
        scene.display.render_aa = "8"
        scene.display.light_direction = (0.4, -0.35, 0.85)

    frames = range(start, end + 1)
    if args.only:
        frames = [int(f) for f in args.only.split(",")]
    for frame in frames:
        scene.frame_set(frame)
        render.filepath = str(frames_dir / f"{frame:04d}.png")
        bpy.ops.render.render(write_still=True)
        print(f"render.py: frame {frame}/{end}", flush=True)


def _fcurves(obj):
    action = obj.animation_data.action if obj.animation_data else None
    if action is None:
        return []
    if hasattr(action, "fcurves") and len(getattr(action, "fcurves", [])):
        return list(action.fcurves)
    curves = []  # layered actions (Blender 4.4+)
    for layer in getattr(action, "layers", []):
        for strip in layer.strips:
            for bag in strip.channelbags:
                curves.extend(bag.fcurves)
    return curves


if __name__ == "__main__":
    main()
