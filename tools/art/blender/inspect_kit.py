# Inspecciona el rig del kit (uncle_prototype.fbx): huesos en espacio mundo, mallas, materiales y limites.
import bpy, sys
argv = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=argv[0])
for o in bpy.data.objects:
    print("OBJ", o.type, o.name, "parent=", o.parent.name if o.parent else "-", "loc=", tuple(round(x, 3) for x in o.matrix_world.translation), "scale=", tuple(round(x, 3) for x in o.scale))
    if o.type == "ARMATURE":
        for b in o.data.bones:
            h = o.matrix_world @ b.head_local; t = o.matrix_world @ b.tail_local
            print("BONE", b.name, "parent=", b.parent.name if b.parent else "-", "head=", tuple(round(x, 3) for x in h), "tail=", tuple(round(x, 3) for x in t))
    if o.type == "MESH":
        import mathutils
        vs = [o.matrix_world @ v.co for v in o.data.vertices]
        mn = mathutils.Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
        mx = mathutils.Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
        print("MESH", o.name, "verts=", len(vs), "min=", tuple(round(x, 3) for x in mn), "max=", tuple(round(x, 3) for x in mx), "mats=", [m.name for m in o.data.materials], "groups=", len(o.vertex_groups))
