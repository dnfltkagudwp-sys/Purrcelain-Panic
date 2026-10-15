"""포트폴리오용 과정 이미지: SOURCE_Backup(정리 전)과 작업본(정리 후)을 같은 조건으로 렌더해 항목별로 묶는다.

blender --background --factory-startup --python render_process.py
결과: Captures/process/P*.png  (작업 파일은 저장하지 않음)
"""
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(__file__))
from pp_common import PROJECT, WORK_BLEND, tri_count, world_bounds

OUT = os.path.join(PROJECT, "Captures", "process")
PANEL_DIR = os.path.join(OUT, "_panels")
os.makedirs(PANEL_DIR, exist_ok=True)
FONT_PATH = "C:/Windows/Fonts/malgunbd.ttf"
VIEW = Vector((0.55, -1.0, 0.38)).normalized()   # 3/4 정면 (Blender 정면 = -Y)

# ------------------------------------------------------------------ 장면 준비

def reset():
    bpy.ops.wm.open_mainfile(filepath=WORK_BLEND)
    scene = bpy.context.scene
    bpy.context.view_layer.layer_collection.children["SOURCE_Backup"].exclude = False
    bk = bpy.data.collections["SOURCE_Backup"]
    bk.hide_render = bk.hide_viewport = False
    for rig_name in ("CAT_Rig", "SRC_CAT_Rig"):
        rig = bpy.data.objects[rig_name]
        if rig.animation_data:
            rig.animation_data.use_nla = False
            rig.animation_data.action = None
        for p in rig.pose.bones:
            p.location = (0, 0, 0)
            p.rotation_quaternion = (1, 0, 0, 0)
            p.rotation_euler = (0, 0, 0)
    for o in scene.objects:
        o.hide_render = True
    scene.render.resolution_x = scene.render.resolution_y = 800
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    world = scene.world or bpy.data.worlds.new("W")
    scene.world = world
    world.color = (0.80, 0.83, 0.87)
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.80, 0.83, 0.87, 1)
    bg.inputs["Strength"].default_value = 1.0
    return scene


def workbench(scene, color_type='OBJECT'):
    scene.render.engine = 'BLENDER_WORKBENCH'
    sh = scene.display.shading
    sh.light = 'STUDIO'
    sh.color_type = color_type
    sh.background_type = 'WORLD'
    sh.show_cavity = False


def eevee(scene):
    scene.render.engine = 'BLENDER_EEVEE'
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
    sun.data.energy = 3.0
    sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-30))
    scene.collection.objects.link(sun)


def show(*objs):
    for o in objs:
        o.hide_render = False


def label_mat():
    m = bpy.data.materials.get("_Label") or bpy.data.materials.new("_Label")
    m.diffuse_color = (0.08, 0.08, 0.1, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.02, 0.02, 0.03, 1)
    bsdf.inputs["Roughness"].default_value = 1.0
    return m


def frame_camera(scene, objs, view=VIEW, pad=1.25, ortho=True, ortho_scale=None, target=None):
    lo, hi = world_bounds(*objs)
    center = target if target is not None else (lo + hi) / 2
    size = max(hi - lo)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scene.collection.objects.link(cam)
    cam.data.type = 'ORTHO' if ortho else 'PERSP'
    cam.data.ortho_scale = ortho_scale or size * pad
    cam.data.lens = 40
    cam.location = center + view * size * 4
    cam.rotation_euler = (-view).to_track_quat('-Z', 'Y').to_euler()
    cam.data.clip_end = 100
    scene.camera = cam
    return cam


def add_label(scene, cam, text, line=0, size=0.055):
    """카메라에 붙인 글자 (화면 왼쪽 위)."""
    font = bpy.data.fonts.load(FONT_PATH, check_existing=True)
    cu = bpy.data.curves.new("Label", 'FONT')
    cu.body = text
    cu.font = font
    ob = bpy.data.objects.new("Label", cu)
    scene.collection.objects.link(ob)
    ob.data.materials.append(label_mat())
    ob.color = (0.05, 0.05, 0.07, 1)
    ob.parent = cam
    if cam.data.type == 'ORTHO':
        s, z = cam.data.ortho_scale, -1.0
    else:
        z = -1.0
        s = 36 / cam.data.lens
    aspect = scene.render.resolution_y / scene.render.resolution_x
    cu.size = s * size
    ob.location = (-s * 0.46, s * aspect * 0.5 - s * size * 1.25 - line * s * size * 1.35, z)
    return ob


def render(scene, name):
    path = os.path.join(PANEL_DIR, name + ".png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


def montage(paths, out_name, cols):
    imgs = []
    for p in paths:
        im = bpy.data.images.load(p, check_existing=False)
        w, h = im.size
        imgs.append(np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4))
        bpy.data.images.remove(im)
    h = max(i.shape[0] for i in imgs)
    w = max(i.shape[1] for i in imgs)
    gap = 8
    rows = []
    for r in range(0, len(imgs), cols):
        row = []
        for i in imgs[r:r + cols]:
            pad = np.ones((h, w, 4), np.float32)
            pad[:i.shape[0], :i.shape[1]] = i
            row.append(pad)
            row.append(np.ones((h, gap, 4), np.float32))
        while len(row) < cols * 2:
            row += [np.ones((h, w, 4), np.float32), np.ones((h, gap, 4), np.float32)]
        rows.append(np.concatenate(row[:-1], axis=1))
        rows.append(np.ones((gap, rows[-1].shape[1], 4), np.float32))
    full = np.concatenate(rows[:-1][::-1], axis=0)   # Blender 이미지는 아래쪽이 원점
    H, W = full.shape[:2]
    img = bpy.data.images.new(out_name, W, H, alpha=True)
    img.pixels.foreach_set(full.ravel())
    img.filepath_raw = os.path.join(OUT, out_name + ".png")
    img.file_format = 'PNG'
    img.save()
    print(f"[PP] saved {img.filepath_raw}")


def meshes_of(name):
    o = bpy.data.objects[name]
    if o.type == 'ARMATURE':
        return [c for c in o.children if c.type == 'MESH']
    if o.type == 'EMPTY':
        return [c for c in o.children if c.type == 'MESH']
    return [o]


def match_height(src_root, ref_objs):
    """비교용: 원본을 정리 후 높이에 맞춰 임시로 키우고 같은 자리로 옮긴다 (저장하지 않음)."""
    lo_r, hi_r = world_bounds(*ref_objs)
    meshes = meshes_of(src_root.name)
    lo, hi = world_bounds(*meshes)
    f = (hi_r.z - lo_r.z) / (hi.z - lo.z)
    src_root.scale = src_root.scale * f
    bpy.context.view_layer.update()
    lo, hi = world_bounds(*meshes)
    src_root.location += Vector(((lo_r.x + hi_r.x) / 2 - (lo.x + hi.x) / 2,
                                 (lo_r.y + hi_r.y) / 2 - (lo.y + hi.y) / 2, lo_r.z - lo.z))
    bpy.context.view_layer.update()


PAIRS = [  # (정리 전 루트, 정리 후 루트, 표시 이름)
    ("SRC_CAT_Rig", "CAT_Rig", "고양이"),
    ("SRC_TABLE_Main", "TABLE_Main", "식탁"),
    ("SRC_Vase_Intact", "Vase_Intact", "도자기"),
]

# ------------------------------------------------------------------ P1 최적화: 와이어프레임 전후

def wire_overlay(scene, mesh_obj, thickness):
    w = mesh_obj.copy()
    w.data = mesh_obj.data.copy()
    w.modifiers.clear()
    w.parent = None
    w.matrix_world = mesh_obj.matrix_world.copy()
    if mesh_obj.find_armature():
        w.data.transform(Matrix.Identity(4))
    scene.collection.objects.link(w)
    mod = w.modifiers.new("Wire", 'WIREFRAME')
    mod.thickness = thickness
    mod.use_relative_offset = False
    mod.use_replace = True
    w.color = (0.12, 0.13, 0.16, 1)
    w.hide_render = False
    mesh_obj.color = (0.86, 0.87, 0.9, 1)
    return w


panels = []
for src_name, dst_name, label in PAIRS:
    for which, root_name in (("before", src_name), ("after", dst_name)):
        scene = reset()
        workbench(scene, 'OBJECT')
        root = bpy.data.objects[root_name]
        if which == "before":
            match_height(root, meshes_of(dst_name))
        ms = meshes_of(root_name)
        show(*ms)
        lo, hi = world_bounds(*ms)
        for m in ms:
            wire_overlay(scene, m, (hi.z - lo.z) * 0.0012)
        cam = frame_camera(scene, ms)
        tris = sum(tri_count(m) for m in ms)
        add_label(scene, cam, f"{label} · {'정리 전' if which == 'before' else '정리 후'}")
        add_label(scene, cam, f"{tris:,} tris", line=1)
        panels.append(render(scene, f"wire_{dst_name}_{which}"))
montage(panels, "P1_최적화_와이어프레임_전후", cols=2)

# ------------------------------------------------------------------ P2 열린 엣지(UV 심 분리) → 병합 후 0

def boundary_curve(scene, mesh_obj, radius):
    bm = bmesh.new()
    bm.from_mesh(mesh_obj.data)
    mw = mesh_obj.matrix_world
    cu = bpy.data.curves.new("Open", 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = radius
    n = 0
    for e in bm.edges:
        if e.is_boundary:
            sp = cu.splines.new('POLY')
            sp.points.add(1)
            for i, v in enumerate(e.verts):
                sp.points[i].co = (*(mw @ v.co), 1)
            n += 1
    bm.free()
    ob = bpy.data.objects.new("Open", cu)
    scene.collection.objects.link(ob)
    ob.color = (0.9, 0.12, 0.1, 1)
    return n


panels = []
for which in ("before", "after"):
    for src_name, dst_name, label in PAIRS:
        scene = reset()
        workbench(scene, 'OBJECT')
        root_name = src_name if which == "before" else dst_name
        root = bpy.data.objects[root_name]
        if which == "before":
            match_height(root, meshes_of(dst_name))
        ms = meshes_of(root_name)
        show(*ms)
        lo, hi = world_bounds(*ms)
        n = 0
        for m in ms:
            m.color = (0.86, 0.87, 0.9, 1)
            n += boundary_curve(scene, m, (hi.z - lo.z) * 0.0025)
        cam = frame_camera(scene, ms)
        add_label(scene, cam, f"{label} · {'원본' if which == 'before' else 'Merge by Distance 후'}")
        add_label(scene, cam, f"열린 엣지 {n:,}개", line=1)
        panels.append(render(scene, f"open_{dst_name}_{which}"))
montage(panels, "P2_열린엣지_병합_전후", cols=3)

def explode(root, amount):
    for s in root.children:
        c = s.matrix_local.translation
        d = Vector((c.x, c.y, (c.z - 0.15) * 0.6))
        s.location = c + (d.normalized() * amount if d.length > 1e-5 else Vector())
    bpy.context.view_layer.update()



# ------------------------------------------------------------------ P3 노멀 방향 검사 (파랑 = 바깥, 빨강 = 뒤집힘)

def face_orientation_material():
    m = bpy.data.materials.new("_FaceOrientation")
    m.use_nodes = True
    m.use_backface_culling = False
    nt = m.node_tree
    nt.nodes.clear()
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.inputs[1].default_value = (0.15, 0.35, 0.95, 1)
    mix.inputs[2].default_value = (0.95, 0.15, 0.12, 1)
    bsdf = nt.nodes.new("ShaderNodeBsdfDiffuse")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(geo.outputs["Backfacing"], mix.inputs[0])
    nt.links.new(mix.outputs[0], bsdf.inputs["Color"])
    nt.links.new(bsdf.outputs[0], out.inputs["Surface"])
    return m


panels = []
for src_name, dst_name, label in PAIRS + [(None, "Vase_Broken_Root", "도자기 파편")]:
    scene = reset()
    eevee(scene)
    fo = face_orientation_material()
    if dst_name == "Vase_Broken_Root":
        explode(bpy.data.objects[dst_name], 0.05)
    ms = meshes_of(dst_name)
    for m in ms:
        for slot in m.material_slots:
            slot.link = 'OBJECT'
            slot.material = fo
    show(*ms)
    cam = frame_camera(scene, ms)
    add_label(scene, cam, f"{label} · Face Orientation")
    add_label(scene, cam, "파랑 = 바깥, 빨강 = 뒤집힌 면", line=1, size=0.045)
    panels.append(render(scene, f"normals_{dst_name}"))
montage(panels, "P3_노멀방향_검사", cols=4)

# ------------------------------------------------------------------ P4 머티리얼 톤 통일 전후

panels = []
for which in ("before", "after"):
    for src_name, dst_name, label in PAIRS:
        scene = reset()
        eevee(scene)
        root_name = src_name if which == "before" else dst_name
        if which == "before":
            match_height(bpy.data.objects[root_name], meshes_of(dst_name))
        ms = meshes_of(root_name)
        show(*ms)
        cam = frame_camera(scene, ms)
        mat = ms[0].material_slots[0].material
        bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        rough = bsdf.inputs["Roughness"]
        rough_txt = "텍스처" if rough.is_linked else f"{rough.default_value:.2f}"
        add_label(scene, cam, f"{label} · {'원본 머티리얼' if which == 'before' else '톤 통일 후'}")
        add_label(scene, cam, f"{mat.name} / Roughness {rough_txt}", line=1, size=0.045)
        panels.append(render(scene, f"mat_{dst_name}_{which}"))
montage(panels, "P4_머티리얼_톤통일_전후", cols=3)

# ------------------------------------------------------------------ P5 도자기 파츠 분리

panels = []
scene = reset()
eevee(scene)
intact = bpy.data.objects["Vase_Intact"]
show(intact)
cam = frame_camera(scene, [intact], ortho_scale=0.5, target=Vector((0, 0, 0.15)))
add_label(scene, cam, "① Vase_Intact (6,000 tris)")
add_label(scene, cam, "닫힌 메시 1개", line=1, size=0.045)
panels.append(render(scene, "frac_1_intact"))

for step, (engine, amount, title, sub) in enumerate([
        ('WB', 0.0, "② 보로노이 절단 (겹친 상태)", "조각별 색 표시"),
        ('WB', 0.06, "③ Vase_Shard_01~09 분리", "조각별 색 표시"),
        ('EE', 0.06, "④ 같은 머티리얼 + 흰 단면", "M_Vase 1슬롯")], 2):
    scene = reset()
    root = bpy.data.objects["Vase_Broken_Root"]
    shards = list(root.children)
    explode(root, amount)
    if engine == 'WB':
        workbench(scene, 'OBJECT')
        palette = [(0.85, 0.45, 0.4), (0.45, 0.7, 0.85), (0.6, 0.8, 0.45), (0.9, 0.75, 0.35), (0.7, 0.5, 0.85),
                   (0.4, 0.75, 0.7), (0.9, 0.6, 0.75), (0.55, 0.6, 0.9), (0.8, 0.8, 0.5), (0.5, 0.85, 0.6)]
        for k, sh in enumerate(sorted(shards, key=lambda o: o.name)):
            sh.color = (*palette[k % len(palette)], 1)
    else:
        eevee(scene)
    show(*shards)
    cam = frame_camera(scene, [bpy.data.objects["Vase_Intact"]], ortho_scale=0.5,
                       target=Vector((0, 0, 0.15)))
    add_label(scene, cam, title)
    add_label(scene, cam, sub, line=1, size=0.045)
    panels.append(render(scene, f"frac_{step}"))
montage(panels, "P5_도자기_파츠분리", cols=4)

# ------------------------------------------------------------------ P6 크기·원점 (정면, 같은 축척)

scene = reset()
eevee(scene)
scene.render.resolution_x, scene.render.resolution_y = 1600, 800
items = []
src_cat = bpy.data.objects["SRC_CAT_Rig"]
src_cat.location.x = -1.0
dst_cat = bpy.data.objects["CAT_Rig"]
table = bpy.data.objects["TABLE_Main"]
table.location.x = 1.8
vase = bpy.data.objects["Vase_Intact"]
vase.location.x = 3.05
ruler_me = bpy.data.meshes.new("Ruler")
bm = bmesh.new()
bmesh.ops.create_cube(bm, size=1.0)
bmesh.ops.transform(bm, verts=bm.verts, matrix=Matrix.Translation((0, 0, 0.5)) @ Matrix.Diagonal((0.03, 0.03, 1.0, 1)))
bm.to_mesh(ruler_me)
bm.free()
ruler = bpy.data.objects.new("Ruler", ruler_me)
ruler.location = (-1.9, 0, 0)
scene.collection.objects.link(ruler)
rm = bpy.data.materials.new("_Ruler")
rm.diffuse_color = (0.2, 0.2, 0.25, 1)
rm.use_nodes = True
rm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.15, 0.15, 0.2, 1)
ruler_me.materials.append(rm)
bpy.context.view_layer.update()
shown = meshes_of("SRC_CAT_Rig") + meshes_of("CAT_Rig") + [table, vase, ruler]
show(*shown)
for o in (src_cat, dst_cat, table, vase):   # 원점 표시
    dot = bpy.data.objects.new("Origin", bpy.data.meshes.new("Origin"))
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=8, radius=0.03)
    bm.to_mesh(dot.data)
    bm.free()
    dot.location = o.location
    om = bpy.data.materials.new("_Origin")
    om.use_nodes = True
    om.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.95, 0.1, 0.1, 1)
    dot.data.materials.append(om)
    scene.collection.objects.link(dot)
lo, hi = world_bounds(*shown)
cam = frame_camera(scene, shown, view=Vector((0, -1, 0)), ortho_scale=(hi.x - lo.x) * 1.12,
                   target=Vector(((lo.x + hi.x) / 2, 0, 0.62)))
add_label(scene, cam, "크기·원점 정리 (같은 축척 정면, 빨간 점 = 원점)", size=0.028)
add_label(scene, cam, f"막대 1.0 m | 원본 고양이 0.84 m → 정리 후 1.15 m | 식탁 {table.dimensions.x:.2f}×{table.dimensions.y:.2f}×{table.dimensions.z:.2f} m | 도자기 0.30 m",
          line=1, size=0.024)
render(scene, "scale_origin")
montage([os.path.join(PANEL_DIR, "scale_origin.png")], "P6_크기_원점", cols=1)

# ------------------------------------------------------------------ P7 애니메이션 키포즈

def pose_panel(action, frame, title, side=-1):
    scene = reset()
    eevee(scene)
    rig = bpy.data.objects["CAT_Rig"]
    rig.location = (0, 0.55, 0)   # 배가 식탁 뒷면(y=0.30)에 닿는 위치
    rig.animation_data.action = bpy.data.actions[action]
    scene.frame_set(frame)
    table = bpy.data.objects["TABLE_Main"]
    show(*meshes_of("CAT_Rig"), table)
    cam = frame_camera(scene, meshes_of("CAT_Rig") + [table], view=Vector((0.45 * side, -1.0, 0.9)).normalized(),
                       pad=0.95, target=Vector((0, 0.05, 0.62)))
    add_label(scene, cam, f"{action} · {frame}F")
    add_label(scene, cam, title, line=1, size=0.045)
    return render(scene, f"pose_{action}_{frame:02d}")


montage([pose_panel("Push_R", 1, "기본 자세"), pose_panel("Push_R", 4, "준비: 오른팔 뒤로"),
         pose_panel("Push_R", 8, "접촉: 앞으로 뻗기"), pose_panel("Push_R", 16, "기본 자세로 복귀")],
        "P7_Push_R_키포즈", cols=4)
montage([pose_panel("Idle_Lean", 1, "시작 (=31F)"), pose_panel("Idle_Lean", 8, "머리 기울임"),
         pose_panel("Idle_Lean", 16, "들숨 + 머리 돌림"), pose_panel("Idle_Lean", 24, "반대로 기울임")],
        "P8_Idle_Lean_키포즈", cols=4)
montage([pose_panel("Push_L", 1, "기본 자세", 1), pose_panel("Push_L", 4, "준비: 왼팔 뒤로", 1),
         pose_panel("Push_L", 8, "접촉: 앞으로 뻗기", 1), pose_panel("Push_L", 16, "기본 자세로 복귀", 1)],
        "P9_Push_L_키포즈", cols=4)
