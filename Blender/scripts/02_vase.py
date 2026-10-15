"""02. 요청문 2 — 도자기 정리와 파편 제작 (Vase_Intact / Vase_Broken_Root / Vase_Shard_NN).

blender --background Purrcelain_Work.blend --python 02_vase.py
"""
import math
import os
import random
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import (apply_transform, clean_mesh, decimate, load_stats, log, mesh_report,
                       origin_to_bottom_center, save_stats, scale_to_height, select_only, world_bounds)

TARGET_HEIGHT = 0.30       # m
DECIMATE_RATIO = 0.3       # 20k -> 6k tris
SEED_LAYOUT = [(0.14, 3), (0.38, 3), (0.62, 2), (0.86, 2)]  # (높이 비율, 개수) -> 10조각
MIN_SHARD_RATIO = 0.03     # 전체 부피의 3% 미만 조각은 이웃에 병합
random.seed(7)

intact = bpy.data.objects["Vase_Intact"]
coll = intact.users_collection[0]
stats = load_stats()
vase = stats.setdefault("vase", {})
vase["before"] = mesh_report(intact)

# ------------------------------------------------------------ 1. 원본 정리
vase["clean"] = clean_mesh(intact)
decimate(intact, DECIMATE_RATIO)
scale_to_height(intact, TARGET_HEIGHT)
origin_to_bottom_center(intact)
intact.data.shade_smooth()
vase["intact"] = mesh_report(intact)
mat = intact.material_slots[0].material

# ------------------------------------------------------------ 2. 단면용 흰 도자기 UV 찾기
img = next(n.image for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE')
w, h = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
uv_layer = intact.data.uv_layers.active
uvs = np.array([d.uv[:] for d in uv_layer.data], dtype=np.float32)
ix = np.clip((uvs[:, 0] % 1) * (w - 1), 0, w - 1).astype(int)
iy = np.clip((uvs[:, 1] % 1) * (h - 1), 0, h - 1).astype(int)
rgb = px[iy, ix, :3]
score = rgb.min(axis=1) - (rgb.max(axis=1) - rgb.min(axis=1)) * 2   # 밝고 무채색일수록 높음
white_uv = tuple(uvs[int(score.argmax())])
vase["cut_face_uv"] = [round(float(v), 4) for v in white_uv]
vase["cut_face_color"] = [round(float(v), 3) for v in rgb[int(score.argmax())]]

# ------------------------------------------------------------ 3. 시드 배치 (층별 링 + 지터)
lo, hi = world_bounds(intact)
verts = [intact.matrix_world @ v.co for v in intact.data.vertices]


def radius_at(z, band=0.02):
    r = [math.hypot(v.x, v.y) for v in verts if abs(v.z - z) < band]
    return max(r) if r else 0.05


seeds = []
for level, (zf, count) in enumerate(SEED_LAYOUT):
    z = lo.z + (hi.z - lo.z) * zf
    r = radius_at(z) * 0.55
    for k in range(count):
        a = 2 * math.pi * k / count + level * 0.9 + random.uniform(-0.25, 0.25)
        seeds.append(Vector((r * math.cos(a), r * math.sin(a), z + random.uniform(-0.01, 0.01))))


def make_cell(i):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    size = (hi - lo) * 1.5
    center = (hi + lo) / 2
    bmesh.ops.transform(bm, verts=bm.verts, matrix=Matrix.Translation(center) @ Matrix.Diagonal((*size, 1)))
    for j, s in enumerate(seeds):
        if j == i:
            continue
        n = (s - seeds[i]).normalized()
        mid = (s + seeds[i]) / 2
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=mid, plane_no=n, clear_outer=True)
        edges = [e for e in bm.edges if e.is_boundary]
        if edges:
            bmesh.ops.holes_fill(bm, edges=edges, sides=0)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(f"Cell_{i}")
    bm.to_mesh(me)
    bm.free()
    uv = me.uv_layers.new(name=uv_layer.name)
    for d in uv.data:
        d.uv = white_uv
    me.materials.append(mat)
    cell = bpy.data.objects.new(f"Cell_{i}", me)
    coll.objects.link(cell)
    return cell


# ------------------------------------------------------------ 4. Boolean 교차로 파편 생성
def volume(o):
    bm = bmesh.new()
    bm.from_mesh(o.data)
    v = bm.calc_volume(signed=False)
    bm.free()
    return v


def centroid(o):
    return sum((v.co for v in o.data.vertices), Vector()) / len(o.data.vertices)


def cut_shards():
    out = []
    for i in range(len(seeds)):
        cell = make_cell(i)
        shard = intact.copy()
        shard.data = intact.data.copy()
        coll.objects.link(shard)
        mod = shard.modifiers.new("Cut", 'BOOLEAN')
        mod.operation = 'INTERSECT'
        mod.solver = 'EXACT'
        mod.object = cell
        mod.material_mode = 'TRANSFER'
        select_only(shard)
        bpy.ops.object.modifier_apply(modifier=mod.name)
        me_cell = cell.data
        bpy.data.objects.remove(cell)
        bpy.data.meshes.remove(me_cell)
        if len(shard.data.polygons) == 0:
            bpy.data.objects.remove(shard)
            continue
        clean_mesh(shard, min_island_ratio=0.05, fill_holes=False)
        out.append((i, shard))
    return out


# ------------------------------------------------------------ 5. 너무 작은 조각은 시드를 빼고 다시 자른다
total = volume(intact)
dropped_seeds = 0
while True:
    pairs = cut_shards()
    vols = {i: volume(s) for i, s in pairs}
    small = min(vols, key=vols.get)
    if vols[small] >= total * MIN_SHARD_RATIO or len(pairs) <= 8:
        break
    for _, s in pairs:
        bpy.data.objects.remove(s)
    seeds.pop(small)
    dropped_seeds += 1
shards = [s for _, s in pairs]

# ------------------------------------------------------------ 6. 이름/원점/계층/머티리얼 정리
root = bpy.data.objects.get("Vase_Broken_Root") or bpy.data.objects.new("Vase_Broken_Root", None)
if root.name not in coll.objects:
    coll.objects.link(root)
root.empty_display_type = 'PLAIN_AXES'
root.empty_display_size = 0.1
root.matrix_world = intact.matrix_world.copy()  # 원본과 같은 위치

shards.sort(key=lambda s: (round(centroid(s).z, 2), math.atan2(centroid(s).y, centroid(s).x)))
vase["shards"] = {}
for n, s in enumerate(shards, 1):
    s.name = s.data.name = f"Vase_Shard_{n:02d}"
    s.data.materials.clear()
    s.data.materials.append(mat)
    for p in s.data.polygons:
        p.material_index = 0
    s.data.shade_smooth()
    s.data.set_sharp_from_angle(angle=math.radians(35))   # 깨진 단면은 각지게
    select_only(s)
    bpy.ops.object.origin_set(type='ORIGIN_CENTER_OF_VOLUME', center='MEDIAN')
    apply_transform(s, location=False, rotation=True, scale=True)
    s.parent = root
    s.matrix_parent_inverse = root.matrix_world.inverted()
    d = s.dimensions
    vase["shards"][s.name] = {**mesh_report(s), "volume_pct": round(volume(s) / total * 100, 1),
                              "min_dim_cm": round(min(d) * 100, 1)}

vase["shard_count"] = len(shards)
vase["dropped_small_seeds"] = dropped_seeds
vase["size_m"] = [round(v, 3) for v in intact.dimensions]
save_stats(stats)
bpy.ops.wm.save_mainfile()
log(f"intact: {vase['intact']}  size={vase['size_m']}")
log(f"cut uv={vase['cut_face_uv']} color={vase['cut_face_color']}")
log(f"shards={len(shards)} dropped_seeds={dropped_seeds}")
for k, v in vase["shards"].items():
    log(f"{k}: {v}")
