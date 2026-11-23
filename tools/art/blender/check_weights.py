# Comprueba que la zona de las manos (|x| > 0.55) solo tome peso de huesos del brazo y que no haya vertices sin peso.
import bpy
o = bpy.data.objects["Uncle_S21_Body"]; names = {g.index: g.name for g in o.vertex_groups}
bad, unweighted, hands = 0, 0, 0
for v in o.data.vertices:
    p = o.matrix_world @ v.co
    gs = [(names[g.group], g.weight) for g in v.groups if g.weight > 0.001]
    if not gs: unweighted += 1
    if abs(p.x) > 0.55:
        hands += 1
        if any(not (n.startswith("forearm") or n.startswith("upper_arm") or n == "chest") for n, w in gs): bad += 1
print(f"CHECK manos={hands} fuera_del_brazo={bad} sin_peso={unweighted}")
