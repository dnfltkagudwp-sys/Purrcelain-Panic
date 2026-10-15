"""도자기 3종(원본·Tall·Squat)과 파편 묶음을 나란히 렌더한다 (파일은 저장하지 않음).

blender --background Purrcelain_Work.blend --python render_vase_variants.py -- <out.png>
"""
import math
import sys

import bpy
from mathutils import Vector

out = sys.argv[sys.argv.index("--") + 1]
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
scene.view_settings.view_transform = 'Standard'
world = scene.world or bpy.data.worlds.new("W")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.80, 0.83, 0.87, 1)

for o in scene.objects:
    o.hide_render = True

sets = [("Vase_Intact", "Vase_Broken_Root"), ("Vase_Tall_Intact", "Vase_Tall_Broken_Root"),
        ("Vase_Squat_Intact", "Vase_Squat_Broken_Root")]
for i, (intact_name, root_name) in enumerate(sets):
    x = (i - 1) * 0.6
    intact = bpy.data.objects[intact_name]
    intact.location = (x - 0.13, 0, 0)
    intact.hide_render = False
    root = bpy.data.objects[root_name]
    root.location = (x + 0.14, 0, 0)
    for s in root.children:
        c = s.matrix_local.translation
        d = Vector((c.x, c.y, (c.z - 0.12) * 0.5))
        s.location = c + (d.normalized() * 0.035 if d.length > 1e-5 else Vector())
        s.hide_render = False

floor = bpy.data.meshes.new("Floor")
floor.from_pydata([(-3, -3, 0), (3, -3, 0), (3, 3, 0), (-3, 3, 0)], [], [(0, 1, 2, 3)])
fo = bpy.data.objects.new("Floor", floor)
scene.collection.objects.link(fo)
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-30))
scene.collection.objects.link(sun)

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 2.0
direction = Vector((0.15, -1.0, 0.35)).normalized()
target = Vector((0, 0, 0.17))
cam.location = target + direction * 6
cam.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
scene.collection.objects.link(cam)
scene.camera = cam
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
