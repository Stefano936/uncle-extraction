import bpy, sys
argv = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=argv[0])
o = [x for x in bpy.data.objects if x.type == "MESH"][0]
me = o.data
acc = {}
for p in me.polygons:
    m = me.materials[p.material_index].name
    c = o.matrix_world @ p.center
    a = acc.setdefault(m, [0, 0, 0, 0, 9, -9, 9, -9, 9, -9])
    a[0] += c.x; a[1] += c.y; a[2] += c.z; a[3] += 1
    a[4] = min(a[4], c.x); a[5] = max(a[5], c.x); a[6] = min(a[6], c.y); a[7] = max(a[7], c.y); a[8] = min(a[8], c.z); a[9] = max(a[9], c.z)
for m, a in acc.items():
    n = a[3]; print("MAT", m, "n=", n, "mean=", (round(a[0]/n,3), round(a[1]/n,3), round(a[2]/n,3)), "x=", (round(a[4],2), round(a[5],2)), "y=", (round(a[6],2), round(a[7],2)), "z=", (round(a[8],2), round(a[9],2)))
