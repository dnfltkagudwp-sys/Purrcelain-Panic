"""GLB/blend 미리보기 렌더 (Workbench, 정면 -Y / 측면 +X).

blender --background --python preview_render.py -- <input.glb|none> <out_prefix>
"""
import bpy
import sys
import math
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, out_prefix = argv[0], argv[1]

if src != "none":
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    for o in list(bpy.data.objects):
        if o.users_collection and o.users_collection[0].name == "glTF_not_exported":
            bpy.data.objects.remove(o)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'TEXTURE'
scene.render.resolution_x = scene.render.resolution_y = 768
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("World")

meshes = [o for o in scene.objects if o.type == 'MESH' and o.visible_get()]
pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
center, size = (lo + hi) / 2, max(hi - lo)
print(f"BOUNDS lo={tuple(round(v, 3) for v in lo)} hi={tuple(round(v, 3) for v in hi)}")

cam_data = bpy.data.cameras.new("PreviewCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = size * 1.25
cam = bpy.data.objects.new("PreviewCam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

views = {"front": (Vector((0, -1, 0)), (math.radians(90), 0, 0)),
         "side": (Vector((1, 0, 0)), (math.radians(90), 0, math.radians(90)))}
for name, (direction, rot) in views.items():
    cam.location = center + direction * size * 3
    cam.rotation_euler = rot
    scene.render.filepath = f"{out_prefix}_{name}.png"
    bpy.ops.render.render(write_still=True)

bpy.data.objects.remove(cam)
