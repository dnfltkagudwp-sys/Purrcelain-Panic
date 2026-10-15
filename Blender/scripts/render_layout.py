"""정리된 세 에셋을 게임 배치로 임시 배치해 렌더한다 (파일은 저장하지 않음).

blender --background Purrcelain_Work.blend --python render_layout.py -- <out.png> [action] [frame] [explode]
"""
import math
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
out = args[0]
action = args[1] if len(args) > 1 else "Idle_Lean"
frame = int(args[2]) if len(args) > 2 else 1
explode = float(args[3]) if len(args) > 3 else 0.0

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x, scene.render.resolution_y = 1280, 720
scene.view_settings.view_transform = 'Standard'
world = scene.world or bpy.data.worlds.new("W")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.82, 0.86, 0.9, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9

rig = bpy.data.objects["CAT_Rig"]
rig.location = (0, 0.55, 0)   # 배가 식탁 뒷면(y=0.30)에 닿는 위치
rig.animation_data.action = bpy.data.actions[action]
scene.frame_set(frame)

top = bpy.data.objects["TABLE_Main"].dimensions.z
bpy.data.objects["Vase_Intact"].location = (0.12, 0.08, top)
root = bpy.data.objects["Vase_Broken_Root"]
root.location = (-0.45, 0.0, top)
for s in root.children:
    c = s.matrix_local.translation
    d = Vector((c.x, c.y, 0)).normalized() if c.length > 1e-4 else Vector()
    s.location = c + d * explode + Vector((0, 0, 0.0))

floor = bpy.data.meshes.new("Floor")
floor.from_pydata([(-4, -4, 0), (4, -4, 0), (4, 4, 0), (-4, 4, 0)], [], [(0, 1, 2, 3)])
fo = bpy.data.objects.new("Floor", floor)
scene.collection.objects.link(fo)

sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-30))
scene.collection.objects.link(sun)

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.lens = 40
cam.location = (0.9, -2.6, 1.55)
cam.rotation_euler = (math.radians(72), 0, math.radians(18))
scene.collection.objects.link(cam)
scene.camera = cam
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
