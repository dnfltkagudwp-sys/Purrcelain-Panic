"""00. VARCO 3D 원본 GLB를 불러와 작업 파일을 만든다.

- 원본은 SOURCE_Backup 컬렉션에 복사본으로 보존 (뷰포트/렌더 숨김, 출력 제외)
- 작업용 오브젝트는 CAT / TABLE / VASE 컬렉션에 배치
blender --background --factory-startup --python 00_import.py
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import (PROJECT, WORK_BLEND, ensure_collection, log, mesh_report,
                       move_to_collection, save_stats, world_bounds)

SRC = os.path.join(PROJECT, "Source")
ASSETS = {
    "CAT": "cat_rigged.glb",
    "TABLE": "table_raw.glb",
    "VASE": "vase_raw.glb",
}

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

backup = ensure_collection("SOURCE_Backup")
stats = {"raw": {}}
removed = []

for key, fname in ASSETS.items():
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(SRC, fname))
    new = [o for o in bpy.data.objects if o not in before]

    # glTF 임포터가 만든 불필요한 오브젝트(예: Icosphere 표시용 메시) 제거
    for o in list(new):
        if any(c.name.startswith("glTF_not_exported") for c in o.users_collection):
            removed.append(o.name)
            new.remove(o)
            bpy.data.objects.remove(o)
    for c in list(bpy.data.collections):
        if c.name.startswith("glTF_not_exported"):
            bpy.data.collections.remove(c)

    coll = ensure_collection(key)
    for o in new:
        move_to_collection(o, coll)

    # 작업 이름 부여
    mesh = next(o for o in new if o.type == 'MESH')
    arm = next((o for o in new if o.type == 'ARMATURE'), None)
    names = {"CAT": "CAT_Body", "TABLE": "TABLE_Main", "VASE": "Vase_Intact"}
    mesh.name = mesh.data.name = names[key]
    if arm:
        arm.name = "CAT_Rig"
        arm.data.name = "CAT_Rig_Data"

    lo, hi = world_bounds(mesh)
    stats["raw"][mesh.name] = {"file": fname, "size_m": [round(v, 3) for v in (hi - lo)], **mesh_report(mesh)}

    # 원본 보존: 데이터까지 복사한 백업본
    dup_map = {}
    for o in new:
        d = o.copy()
        if o.data:
            d.data = o.data.copy()
        if o.type == 'MESH':  # 머티리얼도 복사해야 이후 색 보정이 백업에 번지지 않는다
            for i, m in enumerate(d.data.materials):
                if m:
                    d.data.materials[i] = m.copy()
                    d.data.materials[i].name = "SRC_" + m.name
        d.name = "SRC_" + o.name
        backup.objects.link(d)
        dup_map[o] = d
    for o, d in dup_map.items():
        if o.parent in dup_map:
            d.parent = dup_map[o.parent]
        for m in d.modifiers:
            if m.type == 'ARMATURE' and m.object in dup_map:
                m.object = dup_map[m.object]

# 백업 컬렉션 숨김 + 뷰 레이어에서 제외
backup.hide_viewport = True
backup.hide_render = True
bpy.context.view_layer.layer_collection.children["SOURCE_Backup"].exclude = True

stats["removed_on_import"] = removed
save_stats(stats)
os.makedirs(os.path.dirname(WORK_BLEND), exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=WORK_BLEND)
log(f"removed junk: {removed}")
for k, v in stats["raw"].items():
    log(f"{k}: {v}")
log(f"saved {WORK_BLEND}")
