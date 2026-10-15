"""03b. 요청문 7 — 도자기 변형체 2종 (모양·색이 조금씩 다른 버전).

원본 도자기(Vase_Intact)와 파편 9조각을 같은 변형으로 늘리거나 눌러서, 깨지는 구조까지 그대로 가진 변형체를 만든다.
- Vase_Tall_*  : 가늘고 길게 (청자 느낌: 민트빛 유약 + 초록 무늬)
- Vase_Squat_* : 낮고 넓게   (분홍 느낌: 크림빛 유약 + 장미색 무늬)
blender --background Purrcelain_Work.blend --python 03b_vase_variants.py
"""
import os
import sys

import bpy
import numpy as np
from mathutils import Matrix

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import (TEX_DIR, ensure_collection, hsv_to_rgb, load_stats, log, mesh_report, rgb_to_hsv,
                       save_stats, select_only)

BASE_HUE = 0.58   # 원본 무늬의 파랑
VARIANTS = {
    "Tall":  {"scale": (0.90, 0.90, 1.22), "pattern_hue": 0.40, "glaze_hue": 0.42, "glaze_sat": 0.07},
    "Squat": {"scale": (1.16, 1.16, 0.78), "pattern_hue": 0.96, "glaze_hue": 0.09, "glaze_sat": 0.08},
}

stats = load_stats()
src_intact = bpy.data.objects["Vase_Intact"]
src_root = bpy.data.objects["Vase_Broken_Root"]
src_shards = sorted(src_root.children, key=lambda o: o.name)
src_mat = src_intact.material_slots[0].material
src_img = next(n.image for n in src_mat.node_tree.nodes if n.type == 'TEX_IMAGE')
w, h = src_img.size
base_px = np.array(src_img.pixels[:], dtype=np.float32).reshape(h, w, 4)
base_hsv = rgb_to_hsv(base_px[..., :3])


def recolor(v):
    """무늬(채도 있는 픽셀)는 색상만 옮기고, 흰 유약(채도 낮은 픽셀)은 살짝 물들인다."""
    hsv = base_hsv.copy()
    pattern = hsv[..., 1] >= 0.15
    hsv[..., 0] = np.where(pattern, (v["pattern_hue"] + (hsv[..., 0] - BASE_HUE)) % 1.0, v["glaze_hue"])
    hsv[..., 1] = np.where(pattern, hsv[..., 1], np.clip(hsv[..., 1] + v["glaze_sat"], 0, 1))
    px = base_px.copy()
    px[..., :3] = hsv_to_rgb(hsv)
    return px


def make_material(name, tex_name, px):
    mat = src_mat.copy()
    mat.name = name
    img = bpy.data.images.new(tex_name, w, h, alpha=True)
    img.pixels.foreach_set(px.ravel())
    img.filepath_raw = os.path.join(TEX_DIR, tex_name + ".png")
    img.file_format = 'PNG'
    img.save()
    next(n for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE').image = img
    mean = rgb_to_hsv(px[..., :3]).reshape(-1, 3).mean(0)
    mat.diffuse_color = (*hsv_to_rgb(mean[None, :])[0], 1.0)
    return mat


def copy_deformed(src, name, scale_m, mat, coll):
    """월드 좌표로 구운 뒤 변형을 적용한 독립 복사본 (원점은 호출하는 쪽에서 정한다)."""
    ob = src.copy()
    ob.data = src.data.copy()
    ob.data.name = name
    ob.name = name
    ob.parent = None
    ob.data.transform(src.matrix_world)
    ob.data.transform(scale_m)
    ob.matrix_world = Matrix.Identity(4)
    ob.data.materials.clear()
    ob.data.materials.append(mat)
    for p in ob.data.polygons:
        p.material_index = 0
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    coll.objects.link(ob)
    ob.data.update()
    return ob


report = {}
for key, v in VARIANTS.items():
    for o in [o for o in bpy.data.objects if o.name.startswith(f"Vase_{key}_")]:   # 재실행 대비
        bpy.data.objects.remove(o)
    coll = ensure_collection(f"VASE_{key}")
    scale_m = Matrix.Diagonal((*v["scale"], 1.0))   # 원점이 바닥 중앙이므로 바닥은 z=0에 그대로 남는다
    mat = make_material(f"M_Vase_{key}", f"T_Vase_{key}_BaseColor", recolor(v))

    intact = copy_deformed(src_intact, f"Vase_{key}_Intact", scale_m, mat, coll)
    root = bpy.data.objects.new(f"Vase_{key}_Broken_Root", None)
    root.empty_display_type = 'PLAIN_AXES'
    root.empty_display_size = 0.1
    coll.objects.link(root)
    root.matrix_world = intact.matrix_world.copy()

    shard_info = {}
    for i, s in enumerate(src_shards, 1):
        sh = copy_deformed(s, f"Vase_{key}_Shard_{i:02d}", scale_m, mat, coll)
        select_only(sh)
        bpy.ops.object.origin_set(type='ORIGIN_CENTER_OF_VOLUME', center='MEDIAN')
        sh.parent = root
        sh.matrix_parent_inverse = root.matrix_world.inverted()
        r = mesh_report(sh)
        shard_info[sh.name] = {k: r[k] for k in ("tris", "hole_edges", "non_manifold_edges", "flipped_faces")}

    d = intact.dimensions
    report[key] = {
        "scale_xyz": list(v["scale"]),
        "size_m": [round(d.x, 3), round(d.y, 3), round(d.z, 3)],
        "intact": mesh_report(intact),
        "material": mat.name,
        "texture": f"T_Vase_{key}_BaseColor.png",
        "shards": len(shard_info),
        "shard_tris_total": sum(x["tris"] for x in shard_info.values()),
        "shard_problems": {n: x for n, x in shard_info.items()
                           if x["hole_edges"] or x["non_manifold_edges"] or x["flipped_faces"]},
    }

stats["vase_variants"] = report
save_stats(stats)
bpy.ops.wm.save_mainfile()
for k, r in report.items():
    log(f"{k}: {r}")
