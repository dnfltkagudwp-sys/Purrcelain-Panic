"""03. 요청문 3 — 식탁 정리(TABLE_Main)와 세 에셋 머티리얼 스타일 통일.

색 보정은 셰이더 노드가 아니라 텍스처 픽셀에 직접 적용해 새 PNG로 저장한다
(FBX → Unity로는 노드 보정이 넘어가지 않기 때문).
blender --background Purrcelain_Work.blend --python 03_table_style.py
"""
import math
import os
import sys

import bpy
import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import (TABLE_SIZE, TEX_DIR, clean_mesh, decimate, load_stats, log, mesh_report,
                       origin_to_bottom_center, save_stats, scale_to_height, world_bounds)

TABLE_HEIGHT = 0.72          # VARCO 원본 비율 그대로 키웠을 때의 높이 (리사이즈 전 기준)
TABLE_DECIMATE = 0.25      # 20k -> 5k tris

stats = load_stats()

# ------------------------------------------------------------ 1. 식탁 정리
table = bpy.data.objects["TABLE_Main"]
t = stats.setdefault("table", {})
t["before"] = mesh_report(table)
t["clean"] = clean_mesh(table)
scale_to_height(table, TABLE_HEIGHT)
origin_to_bottom_center(table)


def resize_table(obj, size):
    """다리·모서리는 그대로 두고 가운데만 늘리고 줄인다 (균일 스케일이면 다리가 굵어짐)."""
    W, D, H = obj.dimensions
    for v in obj.data.vertices:
        co = v.co
        for axis, (half, new_half, inner) in enumerate(((W / 2, size[0] / 2, 0.55), (D / 2, size[1] / 2, 0.5))):
            c = half * inner
            d = new_half - half
            x = co[axis]
            if x > c:
                co[axis] = x + d
            elif x < -c:
                co[axis] = x - d
            else:
                co[axis] = x * (c + d) / c
        z_lo, z_hi = H * 0.10, H * 0.55        # 다리 중간 구간만 압축
        dz = size[2] - H
        if co.z > z_hi:
            co.z += dz
        elif co.z > z_lo:
            co.z = z_lo + (co.z - z_lo) * (z_hi - z_lo + dz) / (z_hi - z_lo)
    obj.data.update()
    bpy.context.view_layer.update()


t["size_before_resize_m"] = [round(v, 3) for v in table.dimensions]
resize_table(table, TABLE_SIZE)
decimate(table, TABLE_DECIMATE)
table.data.shade_smooth()
table.data.set_sharp_from_angle(angle=math.radians(40))
t["after"] = mesh_report(table)
t["size_m"] = [round(v, 3) for v in table.dimensions]
t["origin"] = "bottom center (0,0,0)"
t["front"] = "-Y (긴 면이 정면, Unity +Z)"

# ------------------------------------------------------------ 2. 머티리얼 통일
# 고양이: 가장 눈에 띄게(채도·명도 ↑) / 식탁: 중간 명도로 가라앉힘 / 도자기: 밝게
STYLE = {
    "CAT_Body":    {"mat": "M_Cat",   "tex": "T_Cat_BaseColor",   "sat": 1.20, "val_mul": 1.08, "val_lift": 0.00, "rough": 0.65},
    "TABLE_Main":  {"mat": "M_Table", "tex": "T_Table_BaseColor", "sat": 0.80, "val_target": 0.50, "val_lift": 0.00, "rough": 0.65},
    "Vase_Intact": {"mat": "M_Vase",  "tex": "T_Vase_BaseColor",  "sat": 1.10, "val_mul": 0.88, "val_lift": 0.14, "rough": 0.35},
}


def rgb_to_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-6
    rm = m & (mx == r)
    gm = m & (mx == g) & ~rm
    bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    h /= 6
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    return np.stack([h, s, mx], -1)


def hsv_to_rgb(hsv):
    h, s, v = hsv[..., 0] * 6, hsv[..., 1], hsv[..., 2]
    i = np.floor(h).astype(int) % 6
    f = h - np.floor(h)
    p, q, tt = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    choices = [(v, tt, p), (q, v, p), (p, v, tt), (p, q, v), (tt, p, v), (v, p, q)]
    out = np.zeros(h.shape + (3,), dtype=np.float32)
    for k, (a, b, c) in enumerate(choices):
        sel = i == k
        out[sel] = np.stack([a[sel], b[sel], c[sel]], -1)
    return out


os.makedirs(TEX_DIR, exist_ok=True)
style_log = {}
done_mats = {}
for obj_name, st in STYLE.items():
    obj = bpy.data.objects[obj_name]
    mat = obj.material_slots[0].material
    old_name = mat.name
    mat.name = st["mat"]
    nodes = mat.node_tree.nodes
    bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
    tex = next(n for n in nodes if n.type == 'TEX_IMAGE')
    src = tex.image
    w, h = src.size
    px = np.array(src.pixels[:], dtype=np.float32).reshape(h, w, 4)
    hsv = rgb_to_hsv(px[..., :3])
    before = hsv.reshape(-1, 3).mean(0)

    hsv[..., 1] = np.clip(hsv[..., 1] * st["sat"], 0, 1)
    if "val_target" in st:
        st["val_mul"] = float(st["val_target"] / max(before[2], 1e-3))
    hsv[..., 2] = np.clip(hsv[..., 2] * st["val_mul"] + st["val_lift"], 0, 1)
    after = hsv.reshape(-1, 3).mean(0)
    px[..., :3] = hsv_to_rgb(hsv)

    img = bpy.data.images.new(st["tex"], w, h, alpha=True)
    img.pixels.foreach_set(px.ravel())
    img.filepath_raw = os.path.join(TEX_DIR, st["tex"] + ".png")
    img.file_format = 'PNG'
    img.save()
    img.colorspace_settings.name = 'sRGB'
    tex.image = img

    # 거칠기/금속성 통일: 텍스처 연결 해제 후 값 지정
    for name, val in (("Roughness", st["rough"]), ("Metallic", 0.0)):
        sock = bsdf.inputs[name]
        for link in list(sock.links):
            mat.node_tree.links.remove(link)
        sock.default_value = val
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.5
    mat.roughness = st["rough"]
    mat.diffuse_color = (*hsv_to_rgb(after[None, :])[0], 1.0)

    style_log[obj_name] = {
        "material": f"{old_name} -> {st['mat']}", "texture": st["tex"] + ".png",
        "mean_hsv_before": [round(float(v), 3) for v in before],
        "mean_hsv_after": [round(float(v), 3) for v in after],
        "saturation_x": st["sat"], "value_x": round(st["val_mul"], 3), "value_lift": st["val_lift"],
        "roughness": st["rough"], "metallic": 0.0,
    }
    done_mats[obj_name] = mat

# 파편도 같은 머티리얼인지 확인
vase_mat = done_mats["Vase_Intact"]
for o in bpy.data.objects:
    if o.name.startswith("Vase_Shard_"):
        assert all(s.material == vase_mat for s in o.material_slots), o.name

stats["materials"] = style_log
save_stats(stats)
bpy.ops.wm.save_mainfile()
log(f"table: {t}")
for k, v in style_log.items():
    log(f"{k}: {v}")
