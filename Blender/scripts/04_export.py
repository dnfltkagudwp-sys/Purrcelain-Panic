"""04. 요청문 4 — 최종 검수와 Unity용 FBX 3종 출력 + 재임포트 검증.

출력: Export/Cat_Player.fbx, Export/Table_Main.fbx, Export/Vase_Set.fbx (+ 텍스처 복사)
blender --background Purrcelain_Work.blend --python 04_export.py
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import PROJECT, load_stats, log, mesh_report, save_stats, select_only, world_bounds

EXPORT_DIR = os.path.join(PROJECT, "Export")
os.makedirs(EXPORT_DIR, exist_ok=True)

COMMON = dict(
    use_selection=True,
    axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', global_scale=1.0,
    bake_space_transform=True,             # Apply Transform
    mesh_smooth_type='FACE', use_mesh_modifiers=True, use_tspace=False,
    add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X',
    use_armature_deform_only=True,
    path_mode='COPY', embed_textures=False,
)

ASSETS = {
    "Cat_Player.fbx": {"objects": ["CAT_Rig", "CAT_Body"], "types": {'ARMATURE', 'MESH'}, "anim": True},
    "Table_Main.fbx": {"objects": ["TABLE_Main"], "types": {'MESH'}, "anim": False},
    "Vase_Set.fbx": {"objects": ["Vase_Intact", "Vase_Broken_Root"] +
                     sorted(o.name for o in bpy.data.objects if o.name.startswith("Vase_Shard_")),
                     "types": {'MESH', 'EMPTY'}, "anim": False},
}
for _v in ("Tall", "Squat"):   # 요청문 7: 도자기 변형체
    ASSETS[f"Vase_{_v}_Set.fbx"] = {
        "objects": [f"Vase_{_v}_Intact", f"Vase_{_v}_Broken_Root"] +
                   sorted(o.name for o in bpy.data.objects if o.name.startswith(f"Vase_{_v}_Shard_")),
        "types": {'MESH', 'EMPTY'}, "anim": False}

stats = load_stats()
report = {}
scene = bpy.context.scene
rig = bpy.data.objects["CAT_Rig"]

# 애니메이션 평가 기준값(원본) — 재임포트 결과와 비교용
reference = {}
for act_name, frame in (("Idle_Lean", 16), ("Push_R", 8), ("Push_L", 8)):
    rig.animation_data.action = bpy.data.actions[act_name]
    scene.frame_set(frame)
    hand = rig.matrix_world @ rig.pose.bones["LeftHand" if act_name == "Push_L" else "RightHand"].head
    reference[act_name] = (frame, hand.copy())

# 출력 전 휴지 자세로 + 액션마다 NLA 스트립(스트립 이름 = Unity 클립 이름)
rig.animation_data.action = None
for tr in list(rig.animation_data.nla_tracks):
    rig.animation_data.nla_tracks.remove(tr)
for act_name in ("Idle_Lean", "Push_R", "Push_L"):
    act = bpy.data.actions[act_name]
    tr = rig.animation_data.nla_tracks.new()
    tr.name = act_name
    strip = tr.strips.new(act_name, int(act.frame_range[0]), act)
    strip.name = act_name
for p in rig.pose.bones:
    p.location = (0, 0, 0)
    p.rotation_quaternion = (1, 0, 0, 0)
    p.scale = (1, 1, 1)
rig.animation_data.use_nla = False   # 검수 측정은 휴지(T) 자세 기준
scene.frame_set(1)

for fname, spec in ASSETS.items():
    objs = [bpy.data.objects[n] for n in spec["objects"]]
    issues = []
    for o in objs:
        if o.type != 'MESH':
            continue
        if any(s.material is None for s in o.material_slots) or not o.material_slots:
            issues.append(f"{o.name}: 빈 머티리얼 슬롯")
        r = mesh_report(o)
        if r["flipped_faces"] or r["hole_edges"] or r["non_manifold_edges"]:
            issues.append(f"{o.name}: {r}")
        if any(abs(s - 1) > 1e-6 for s in o.scale) or any(abs(a) > 1e-6 for a in o.rotation_euler):
            issues.append(f"{o.name}: transform 미적용 scale={tuple(o.scale)} rot={tuple(o.rotation_euler)}")
    meshes = [o for o in objs if o.type == 'MESH']
    lo, hi = world_bounds(*meshes)
    report[fname] = {
        "objects": [f"{o.name}" + (f" (parent: {o.parent.name})" if o.parent else "") for o in objs],
        "tris": {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes},
        "size_m": [round(v, 3) for v in (hi - lo)],
        "bottom_z": round(lo.z, 4),
        "materials": sorted({s.material.name for o in meshes for s in o.material_slots if s.material}),
        "issues": issues,
    }

    rig.animation_data.use_nla = True
    select_only(*objs)
    kwargs = dict(COMMON, object_types=spec["types"], bake_anim=spec["anim"])
    if spec["anim"]:
        kwargs.update(bake_anim_use_all_actions=False, bake_anim_use_nla_strips=True,
                      bake_anim_use_all_bones=True, bake_anim_force_startend_keying=True,
                      bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
    bpy.ops.export_scene.fbx(filepath=os.path.join(EXPORT_DIR, fname), **kwargs)
    rig.animation_data.use_nla = False
    scene.frame_set(1)
    report[fname]["file_kb"] = round(os.path.getsize(os.path.join(EXPORT_DIR, fname)) / 1024)

# ------------------------------------------------------------ 재임포트 검증
verify = {}
for fname in ASSETS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(EXPORT_DIR, fname))
    objs = list(bpy.data.objects)
    meshes = [o for o in objs if o.type == 'MESH']
    lo, hi = world_bounds(*meshes)
    v = {"objects": sorted(o.name for o in objs), "size_m": [round(x, 3) for x in (hi - lo)],
         "bottom_z": round(lo.z, 4), "actions": sorted(a.name for a in bpy.data.actions)}
    arm = next((o for o in objs if o.type == 'ARMATURE'), None)
    if arm:
        for act_name, (frame, ref) in reference.items():
            act = next(a for a in bpy.data.actions if a.name.endswith(act_name))
            arm.animation_data.action = act
            if hasattr(arm.animation_data, "action_slot") and act.slots:
                arm.animation_data.action_slot = act.slots[0]
            bpy.context.scene.frame_set(frame)
            hand = arm.matrix_world @ arm.pose.bones["LeftHand" if act_name == "Push_L" else "RightHand"].head
            v[f"{act_name}_hand_err_mm"] = round((hand - ref).length * 1000, 2)
            v[f"{act_name}_frames"] = list(act.frame_range)
    verify[fname] = v

stats["export"] = report
stats["export_verify"] = verify
save_stats(stats)
for k in ASSETS:
    log(f"{k}: {report[k]}")
    log(f"  reimport: {verify[k]}")
