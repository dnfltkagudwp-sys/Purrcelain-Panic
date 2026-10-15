"""05. Unity 비교용: SOURCE_Backup(정리 전 원본)을 같은 FBX 설정으로 출력한다.

출력: Export/Before/Before_Cat.fbx, Before_Table.fbx, Before_Vase.fbx (원본 크기·폴리곤·머티리얼 그대로)
blender --background Purrcelain_Work.blend --python 05_export_before.py
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import PROJECT, log, select_only

OUT = os.path.join(PROJECT, "Export", "Before")
os.makedirs(OUT, exist_ok=True)
bpy.context.view_layer.layer_collection.children["SOURCE_Backup"].exclude = False
bk = bpy.data.collections["SOURCE_Backup"]
bk.hide_viewport = bk.hide_render = False

SETS = {
    "Before_Cat.fbx": (["SRC_CAT_Rig", "SRC_CAT_Body"], {'ARMATURE', 'MESH'}),
    "Before_Table.fbx": (["SRC_TABLE_Main"], {'MESH'}),
    "Before_Vase.fbx": (["SRC_Vase_Intact"], {'MESH'}),
}
# 원본 텍스처(.blend 안에 packed)를 PNG 파일로 꺼내 FBX가 파일 경로로 참조하게 한다
for o in bk.objects:
    if o.type != 'MESH':
        continue
    for slot in o.material_slots:
        for node in slot.material.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image and node.image.packed_file:
                img = node.image
                base = o.name.replace("SRC_", "T_Raw_")
                img.name = f"{base}_{node.name.replace(' ', '')}"
                img.filepath_raw = os.path.join(OUT, img.name + ".png")
                img.file_format = 'PNG'
                img.save()
                img.unpack(method='REMOVE')
                img.filepath = os.path.join(OUT, img.name + ".png")
                img.reload()

for fname, (names, types) in SETS.items():
    objs = [bpy.data.objects[n] for n in names]
    for o in objs:
        o.hide_set(False)
    select_only(*objs)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT, fname), use_selection=True, object_types=types,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, mesh_smooth_type='FACE', add_leaf_bones=False,
        bake_anim=False, path_mode='RELATIVE', embed_textures=False)
    log(f"{fname}: {names}")
