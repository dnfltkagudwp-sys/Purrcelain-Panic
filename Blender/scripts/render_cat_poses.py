"""고양이 포즈 확인 렌더: 임시 상판(높이 0.72m)을 앞에 두고 Idle/Push 주요 프레임을 측면·정면으로 찍는다.

blender --background Purrcelain_Work.blend --python render_cat_poses.py -- <out_dir>
(파일은 저장하지 않음)
"""
import math
import os
import sys

import bpy
from mathutils import Vector

out_dir = sys.argv[sys.argv.index("--") + 1]
os.makedirs(out_dir, exist_ok=True)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'TEXTURE'
scene.render.resolution_x = scene.render.resolution_y = 640

for o in scene.objects:
    if o.type == 'MESH' and o.name != "CAT_Body":
        o.hide_render = True

bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -0.65, 0.70))
top = bpy.context.object
top.scale = (1.2, 0.8, 0.04)

cam_data = bpy.data.cameras.new("C")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.6
cam = bpy.data.objects.new("C", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
rig = bpy.data.objects["CAT_Rig"]
views = {"side": (Vector((3, -0.3, 0.6)), (math.radians(90), 0, math.radians(90))),
         "front": (Vector((0.0, -3, 0.75)), (math.radians(90), 0, 0)),
         "persp": (Vector((1.6, -2.2, 1.4)), (math.radians(68), 0, math.radians(36)))}
shots = [("Idle_Lean", 1), ("Idle_Lean", 16), ("Push_R", 4), ("Push_R", 8)]
for act_name, frame in shots:
    rig.animation_data.action = bpy.data.actions[act_name]
    scene.frame_set(frame)
    for v, (loc, rot) in views.items():
        cam.location, cam.rotation_euler = loc, rot
        scene.render.filepath = os.path.join(out_dir, f"{act_name}_f{frame:02d}_{v}.png")
        bpy.ops.render.render(write_still=True)
