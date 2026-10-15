"""장면 점검용: 오브젝트, 크기, 폴리곤 수, 본 목록, 머티리얼을 출력한다.

blender --background file.blend --python inspect_scene.py
"""
import bpy
import bmesh

def mesh_stats(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    non_manifold = sum(1 for e in bm.edges if not e.is_manifold)
    loose = sum(1 for v in bm.verts if not v.link_edges)
    tris = sum(len(f.verts) - 2 for f in bm.faces)
    bm.free()
    return tris, non_manifold, loose

print("=" * 70)
for coll in bpy.data.collections:
    print(f"[Collection] {coll.name}: {[o.name for o in coll.objects]}")
for obj in bpy.data.objects:
    d = obj.dimensions
    line = (f"{obj.name:28s} {obj.type:9s} parent={obj.parent.name if obj.parent else '-':16s} "
            f"dim=({d.x:.3f},{d.y:.3f},{d.z:.3f}) loc={tuple(round(v, 3) for v in obj.location)} "
            f"rot={tuple(round(v, 3) for v in obj.rotation_euler)} scale={tuple(round(v, 3) for v in obj.scale)}")
    if obj.type == 'MESH':
        tris, nm, loose = mesh_stats(obj)
        mats = [s.material.name if s.material else None for s in obj.material_slots]
        line += f"\n    tris={tris} non_manifold_edges={nm} loose_verts={loose} mats={mats} vgroups={len(obj.vertex_groups)}"
    if obj.type == 'ARMATURE':
        line += f"\n    bones({len(obj.data.bones)}): {[b.name for b in obj.data.bones]}"
    print(line)
for act in bpy.data.actions:
    print(f"[Action] {act.name} frames={tuple(act.frame_range)}")
print("=" * 70)
