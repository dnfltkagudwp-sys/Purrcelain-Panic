"""01. 요청문 1 — 고양이 정리와 애니메이션 (CAT_Body / CAT_Rig / Idle_Lean / Push_R / Push_L / Swipe_R / Swipe_L).

blender --background Purrcelain_Work.blend --python 01_cat.py
"""
import os
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import (TABLE_SIZE, WORK_BLEND, action_fcurves, apply_transform, arm_space_rot, clean_mesh,
                       decimate, load_stats, log, mesh_report, save_stats, select_only, world_bounds)

TARGET_HEIGHT = 1.15      # m
DECIMATE_RATIO = 0.4      # 50k -> 20k tris
X, Y, Z = 'X', 'Y', 'Z'   # 아마추어 공간 축: +X = 고양이 왼쪽, -Y = 정면, +Z = 위

body = bpy.data.objects["CAT_Body"]
rig = bpy.data.objects["CAT_Rig"]
stats = load_stats()
cat = stats.setdefault("cat", {})
cat["before"] = mesh_report(body)

# ------------------------------------------------------------ 1. 메시 정리
cat["clean"] = clean_mesh(body)
decimate(body, DECIMATE_RATIO)
body.data.shade_smooth()

# ------------------------------------------------------------ 2. 크기 / 원점(발밑 중앙) / Scale 1
lo, hi = world_bounds(body)
factor = TARGET_HEIGHT / (hi.z - lo.z)
feet = [rig.data.bones[n].head_local for n in ("LeftFoot", "RightFoot")]
feet_center = Vector(((feet[0].x + feet[1].x) / 2, (feet[0].y + feet[1].y) / 2, lo.z))

rig.matrix_world = Matrix.Scale(factor, 4) @ Matrix.Translation(-feet_center)
bpy.context.view_layer.update()
apply_transform(rig, body, location=True, rotation=True, scale=True)

# ------------------------------------------------------------ 3. Weight 검사/정리 (Unity 4본 제한)
deform = {b.name for b in rig.data.bones if b.use_deform}
idx_ok = {g.index for g in body.vertex_groups if g.name in deform}
unweighted = sum(1 for v in body.data.vertices
                 if sum(g.weight for g in v.groups if g.group in idx_ok) < 1e-4)
over4 = sum(1 for v in body.data.vertices if sum(1 for g in v.groups if g.weight > 1e-4) > 4)
select_only(body)
bpy.ops.object.vertex_group_clean(group_select_mode='ALL', limit=0.01)
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL', limit=4)
bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)
cat["weights"] = {"unweighted_verts": unweighted, "verts_over_4_bones_before": over4,
                  "fix": "clean<0.01, limit 4, normalize all"}

# ------------------------------------------------------------ 4. 애니메이션
scene = bpy.context.scene
scene.render.fps = 30
pb = rig.pose.bones
for p in pb:
    p.rotation_mode = 'QUATERNION'

POSE_BONES = ["Spine", "Spine1", "Spine2", "Neck", "Head",
              "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
              "RightShoulder", "RightArm", "RightForeArm", "RightHand"]

# 식탁에 기대는 기본 자세 (아마추어 공간 회전, 도). 부모 회전 위에 누적된다.
BASE = {
    "Spine":         [(X, 3)],
    "Spine1":        [(X, 3)],
    "Spine2":        [(X, 3)],
    "Neck":          [(X, -3)],
    "Head":          [(X, -5)],
    "LeftShoulder":  [(Z, -6)],
    "LeftArm":       [(Z, -70), (X, -28)],    # 앞으로 + 위로: 앞발을 상판 위에 올림
    "LeftForeArm":   [(Z, -35), (X, 18)],
    "LeftHand":      [(X, 8)],
    "RightShoulder": [(Z, 6)],
    "RightArm":      [(Z, 70), (X, -28)],
    "RightForeArm":  [(Z, 35), (X, 18)],
    "RightHand":     [(X, 8)],
}


def pose(overrides=None):
    d = {k: list(v) for k, v in BASE.items()}
    for k, v in (overrides or {}).items():
        d[k] = v
    return d


def new_action(name):
    act = bpy.data.actions.get(name)
    if act:
        bpy.data.actions.remove(act)
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data_create()
    rig.animation_data.action = act
    return act


ARM_DELTA = 0.0   # 식탁 높이에 맞춘 팔 들어올림 보정(도, X축)


def set_pose(rotations):
    for name in POSE_BONES:
        rots = rotations.get(name, [])
        if name in ("LeftArm", "RightArm"):
            rots = [(a, d + ARM_DELTA if a == X else d) for a, d in rots]
        pb[name].rotation_quaternion = arm_space_rot(pb[name], rots)


def key_pose(frame, rotations):
    set_pose(rotations)
    for name in POSE_BONES:
        pb[name].keyframe_insert("rotation_quaternion", frame=frame, group=name)
    root = pb["Root"]  # Root 고정: 위치/회전 0으로 키
    root.location = (0, 0, 0)
    root.rotation_quaternion = (1, 0, 0, 0)
    root.keyframe_insert("location", frame=frame, group="Root")
    root.keyframe_insert("rotation_quaternion", frame=frame, group="Root")


def finish_action(act, start, end, cyclic):
    act.use_frame_range = True
    act.frame_start, act.frame_end = start, end
    act.use_cyclic = cyclic
    for fc in action_fcurves(act):
        if cyclic:
            fc.modifiers.new('CYCLES')
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'
            kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
        fc.update()


# --- 식탁 높이에 맞춰 팔 각도 보정: 기본 자세에서 손목이 상판 위 5cm에 오도록 이분 탐색
def wrist_z():
    bpy.context.view_layer.update()
    return (rig.matrix_world @ pb["RightHand"].head).z


target_z = TABLE_SIZE[2] + 0.05
lo_d, hi_d = -40.0, 40.0          # X 음수 = 팔을 위로
for _ in range(30):
    ARM_DELTA = (lo_d + hi_d) / 2
    set_pose(pose())
    if wrist_z() > target_z:
        lo_d = ARM_DELTA
    else:
        hi_d = ARM_DELTA
cat["arm_pitch_delta_deg"] = round(ARM_DELTA, 2)
cat["wrist_z_base_pose"] = round(wrist_z(), 3)

# --- Idle_Lean: 30프레임 루프 (1 == 31), 작은 호흡 + 머리 움직임
idle = new_action("Idle_Lean")
breath_in = pose({
    "Spine1": [(X, 1.5)], "Spine2": [(X, 1.5)],                    # 가슴이 살짝 펴짐
    "LeftShoulder": [(Z, -6), (Y, 2)], "RightShoulder": [(Z, 6), (Y, -2)],
})
key_pose(1, pose({"Head": [(X, -5), (Z, -4)]}))
key_pose(8, pose({"Head": [(X, -7), (Y, 3)]}))
key_pose(16, {**breath_in, "Head": [(X, -6), (Z, 4)]})
key_pose(24, pose({"Head": [(X, -4), (Y, -3)]}))
key_pose(31, pose({"Head": [(X, -5), (Z, -4)]}))
finish_action(idle, 1, 31, cyclic=True)

# --- Push_R: 16프레임 1회 동작
PUSH_R_KEYS = {
    4: {                                                            # 준비: 오른팔 뒤로
        "Spine2": [(X, 3), (Z, -8)],
        "RightShoulder": [(Z, -14)],
        "RightArm": [(Z, 42), (X, -24)],
        "RightForeArm": [(Z, 72), (X, 16)],
        "RightHand": [(X, 10)],
    },
    8: {                                                            # 접촉: 손을 앞으로 쭉
        "Spine1": [(X, 10)],
        "Spine2": [(X, 4), (Z, 14)],
        "RightShoulder": [(Z, 24)],
        "RightArm": [(Z, 88), (X, -30)],
        "RightForeArm": [(Z, 4), (X, 6)],
        "RightHand": [(X, -30)],
    },
}


def mirror_keys(keys):
    """좌우 반전: Right↔Left 본 이름을 바꾸고, YZ 평면 대칭이므로 Y·Z축 회전 부호를 뒤집는다."""
    out = {}
    for frame, bones in keys.items():
        m = {}
        for name, rots in bones.items():
            name = name.replace("Right", "#TMP#").replace("Left", "Right").replace("#TMP#", "Left")
            m[name] = [(axis, deg if axis == X else -deg) for axis, deg in rots]
        out[frame] = m
    return out


def make_push(name, keys, linear_from=(4,)):
    act = new_action(name)
    key_pose(1, pose())
    for frame, overrides in keys.items():
        key_pose(frame, pose(overrides))
    key_pose(16, pose())
    finish_action(act, 1, 16, cyclic=False)
    for fc in action_fcurves(act):                                  # 준비 → 접촉 구간은 빠르게
        for kp in fc.keyframe_points:
            if int(kp.co.x) in linear_from:
                kp.interpolation = 'LINEAR'
    return act


push = make_push("Push_R", PUSH_R_KEYS)
push_l = make_push("Push_L", mirror_keys(PUSH_R_KEYS))   # 요청문 6: 왼손 밀기 (Push_R 좌우 반전)

# --- Swipe_R: 16프레임 1회 동작 (요청문 9). 앞발을 가슴 앞 안쪽으로 당겼다가 앞-오른쪽으로 곡선을 그리며 쓸어낸다
SWIPE_R_KEYS = {
    4: {                                                            # 준비: 앞발을 가슴 앞 왼쪽으로 당기고 상체를 왼쪽으로 비틂
        "Spine2": [(X, 3), (Z, 10)],
        "RightShoulder": [(Z, 12)],
        "RightArm": [(Z, 105), (X, -36)],
        "RightForeArm": [(Z, 40), (X, 10)],
        "RightHand": [(X, 0), (Z, 20)],
    },
    7: {                                                            # 타격: 앞-오른쪽으로 가장 멀리 뻗음
        "Spine1": [(X, 10)],
        "Spine2": [(X, 4), (Z, 3)],
        "RightShoulder": [(Z, 18)],
        "RightArm": [(Z, 78), (X, -35)],
        "RightForeArm": [(Z, 4), (X, 7)],
        "RightHand": [(X, -22), (Z, -5)],
    },
    9: {                                                            # 마무리: 바깥쪽으로 쓸고 나감, 상체는 오른쪽으로 풀림
        "Spine1": [(X, 6)],
        "Spine2": [(X, 3), (Z, -10)],
        "RightShoulder": [(Z, 0)],
        "RightArm": [(Z, 58), (X, -32)],
        "RightForeArm": [(Z, 5), (X, 10)],
        "RightHand": [(X, -10), (Z, -25)],
    },
}
SWIPE_HIT_FRAMES = (5, 9)   # Unity 타격 구간(손 궤적 Sphere Cast)

swipe = make_push("Swipe_R", SWIPE_R_KEYS, linear_from=(4, 7))
swipe_l = make_push("Swipe_L", mirror_keys(SWIPE_R_KEYS), linear_from=(4, 7))


# 손이 닿는 범위 측정 (고양이 원점 기준, -Y = 앞)
def paw_tip(frame, act, side="Right"):
    rig.animation_data.action = act
    scene.frame_set(frame)
    return rig.matrix_world @ pb[f"{side}Hand"].tail


cat["reach"] = {
    "idle_paw_forward_m": round(-paw_tip(1, idle).y, 3),
    "push_contact_forward_m": round(-paw_tip(8, push).y, 3),
    "push_contact_x_m": round(paw_tip(8, push).x, 3),
    "push_contact_z_m": round(paw_tip(8, push).z, 3),
    "belly_front_m": round(-min(v.co.y for v in body.data.vertices if v.co.z < 0.6), 3),
    "table_top_z_m": TABLE_SIZE[2],
}
# Push_L이 Push_R의 거울상인지 확인: 각 키 프레임에서 손끝 위치를 X축 대칭으로 비교
cat["push_mirror_check"] = {}
for f in (1, 4, 8, 16):
    r = paw_tip(f, push, "Right")
    l = paw_tip(f, push_l, "Left")
    cat["push_mirror_check"][f"F{f}"] = {
        "right_tip": [round(v, 3) for v in r], "left_tip": [round(v, 3) for v in l],
        "mirror_err_mm": round((Vector((-l.x, l.y, l.z)) - r).length * 1000, 1)}


# Swipe_R 궤적: 프레임마다 손끝 위치(식탁 기준)와 앞발 메시의 가장 낮은 점이 상판보다 위인지
def paw_lowest_z(side="Right"):
    tip = rig.matrix_world @ pb[f"{side}Hand"].tail
    mesh = body.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
    z = min((body.matrix_world @ v.co).z for v in mesh.vertices
            if ((body.matrix_world @ v.co) - tip).length < 0.08)
    body.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh_clear()
    return z


belly = cat["reach"]["belly_front_m"]
traj = {}
for f in range(1, 17):
    t = paw_tip(f, swipe)
    traj[f"F{f}"] = {"x": round(t.x, 3), "into_table": round(-t.y - belly, 3), "z": round(t.z, 3),
                     "paw_low_above_top_cm": round((paw_lowest_z() - TABLE_SIZE[2]) * 100, 1)}
cat["swipe_r_trajectory"] = traj
cat["swipe_hit_frames"] = list(SWIPE_HIT_FRAMES)
cat["swipe_mirror_check"] = {}
for f in (1, 4, 7, 9, 16):
    r = paw_tip(f, swipe, "Right")
    l = paw_tip(f, swipe_l, "Left")
    cat["swipe_mirror_check"][f"F{f}"] = round((Vector((-l.x, l.y, l.z)) - r).length * 1000, 1)

rig.animation_data.action = idle
scene.frame_start, scene.frame_end = 1, 31
scene.frame_set(1)

# ------------------------------------------------------------ 5. 결과 기록
lo, hi = world_bounds(body)
cat["after"] = mesh_report(body)
cat["size_m"] = [round(v, 3) for v in (hi - lo)]
cat["origin"] = "feet center (0,0,0)"
cat["scale_factor"] = round(factor, 4)
cat["shoulder_height_m"] = round((rig.matrix_world @ rig.data.bones["RightArm"].head_local).z, 3)
cat["rig_scale"] = list(rig.scale)
cat["body_scale"] = list(body.scale)
cat["actions"] = {a.name: list(a.frame_range) for a in (idle, push, push_l, swipe, swipe_l)}
save_stats(stats)
bpy.ops.wm.save_mainfile()
for k, v in cat.items():
    log(f"{k}: {v}")
