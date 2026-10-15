"""Purrcelain Panic 공용 Blender 헬퍼."""
import json
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Quaternion, Vector

PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
WORK_BLEND = os.path.join(PROJECT, "Blender", "Purrcelain_Work.blend")
STATS_JSON = os.path.join(PROJECT, "docs", "stats.json")
TEX_DIR = os.path.join(PROJECT, "Blender", "Textures")
TABLE_SIZE = (1.80, 0.60, 0.62)   # 식탁 폭(X) × 깊이(Y) × 높이(Z), m — 고양이 팔 각도도 이 높이에 맞춘다


# ---------------------------------------------------------------- 기록
def load_stats():
    if os.path.exists(STATS_JSON):
        with open(STATS_JSON, encoding="utf-8") as f:
            return json.load(f)
    return {}


def save_stats(stats):
    os.makedirs(os.path.dirname(STATS_JSON), exist_ok=True)
    with open(STATS_JSON, "w", encoding="utf-8") as f:
        json.dump(stats, f, ensure_ascii=False, indent=2)


def log(msg):
    print(f"[PP] {msg}")


# ---------------------------------------------------------------- 선택/컨텍스트
def select_only(*objs, active=None):
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = active or objs[0]


def apply_transform(*objs, location=False, rotation=True, scale=True):
    select_only(*objs)
    bpy.ops.object.transform_apply(location=location, rotation=rotation, scale=scale)


def move_to_collection(obj, coll):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    coll.objects.link(obj)


def ensure_collection(name, parent=None):
    coll = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    parent = parent or bpy.context.scene.collection
    if coll.name not in parent.children:
        parent.children.link(coll)
    return coll


# ---------------------------------------------------------------- 메시 검사/정리
def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def world_bounds(*objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


def mesh_report(obj):
    """노멀 방향/구멍/Non-Manifold/떨어진 조각 검사 결과."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    seam_split = sum(1 for e in bm.edges if e.is_boundary)
    # UV 심에서 분리된 정점은 병합한 임시 복사본으로 검사 (원본 메시는 그대로)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
    bm.verts.index_update()
    boundary = sum(1 for e in bm.edges if e.is_boundary)
    non_manifold = sum(1 for e in bm.edges if not e.is_manifold)
    # 일관성 재계산 후 방향이 바뀌는 면 수 = 뒤집힌 면
    before = [f.normal.copy() for f in bm.faces]
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    flipped = sum(1 for f, n in zip(bm.faces, before) if f.normal.dot(n) < 0)
    islands = len(_islands(bm))
    vol = bm.calc_volume(signed=False)
    bm.free()
    return {"tris": tri_count(obj), "seam_split_edges": seam_split,
            "hole_edges": boundary, "non_manifold_edges": non_manifold,
            "flipped_faces": flipped, "islands": islands, "volume": round(vol, 6)}


def _islands(bm):
    seen, out = set(), []
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, isl = [v], []
        seen.add(v.index)
        while stack:
            cur = stack.pop()
            isl.append(cur)
            for e in cur.link_edges:
                o = e.other_vert(cur)
                if o.index not in seen:
                    seen.add(o.index)
                    stack.append(o)
        out.append(isl)
    return out


def clean_mesh(obj, merge_dist=1e-5, min_island_ratio=0.005, fill_holes=True):
    """중복 정점 병합, 작은 조각 제거, 구멍 채우기, 노멀 바깥쪽 통일."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    v0 = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=merge_dist)
    merged = v0 - len(bm.verts)

    bm.verts.index_update()
    islands = _islands(bm)
    total = len(bm.verts)
    removed_islands = 0
    for isl in islands:
        if len(isl) < total * min_island_ratio:
            bmesh.ops.delete(bm, geom=isl, context='VERTS')
            removed_islands += 1

    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context='VERTS')

    filled = 0
    if fill_holes:
        boundary = [e for e in bm.edges if e.is_boundary]
        if boundary:
            res = bmesh.ops.holes_fill(bm, edges=boundary, sides=0)
            filled = len(res["faces"])
            bmesh.ops.triangulate(bm, faces=res["faces"])

    before = [f.normal.copy() for f in bm.faces]
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    flipped = sum(1 for f, n in zip(bm.faces, before) if f.normal.dot(n) < 0)

    bm.to_mesh(me)
    bm.free()
    if me.has_custom_normals:
        select_only(obj)
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    me.update()
    return {"merged_verts": merged, "removed_islands": removed_islands,
            "removed_loose_verts": len(loose), "filled_hole_faces": filled, "fixed_flipped_faces": flipped}


def decimate(obj, ratio):
    """Decimate(Collapse)를 모디파이어 스택 맨 앞에 넣고 적용 (Armature 앞)."""
    select_only(obj)
    mod = obj.modifiers.new("Decimate", 'DECIMATE')
    mod.ratio = ratio
    mod.use_collapse_triangulate = True
    while obj.modifiers[0] != mod:
        bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)


def origin_to_bottom_center(obj):
    """메시 데이터를 옮겨 원점을 바닥 중앙(바운딩 박스 XY 중심, 최저 Z)에 맞추고 오브젝트는 월드 원점에 둔다."""
    lo, hi = world_bounds(obj)
    pivot = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    obj.data.transform(Matrix.Translation(obj.matrix_world.inverted() @ pivot * -1))
    obj.matrix_world = Matrix.Identity(4)
    obj.data.update()
    bpy.context.view_layer.update()  # bound_box 갱신


def scale_to_height(obj, height):
    lo, hi = world_bounds(obj)
    f = height / (hi.z - lo.z)
    obj.scale = obj.scale * f
    bpy.context.view_layer.update()
    apply_transform(obj, location=False)
    return f


# ---------------------------------------------------------------- 애니메이션
def action_fcurves(action, slot=None):
    """Blender 4.4+ 슬롯 액션 / 구버전 모두에서 F-Curve 목록을 돌려준다."""
    try:
        from bpy_extras import anim_utils
        slot = slot or (action.slots[0] if action.slots else None)
        if slot is not None:
            cb = anim_utils.action_get_channelbag_for_slot(action, slot)
            if cb is not None:
                return list(cb.fcurves)
    except (ImportError, AttributeError):
        pass
    return list(getattr(action, "fcurves", []))


def arm_space_rot(bone, axis_angles):
    """아마추어 공간 기준 회전(축, 도) 목록을 본 로컬 쿼터니언으로 변환한다."""
    r = Matrix.Identity(3)
    for axis, deg in axis_angles:
        r = Matrix.Rotation(math.radians(deg), 3, axis) @ r
    m = bone.bone.matrix_local.to_3x3().normalized()
    return (m.inverted() @ r @ m).to_quaternion()
