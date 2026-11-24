"""Tio S21: cuerpo completo para Unity sobre el rig del kit (fuente editable y reproducible).

Uso (Windows):
  "C:\\Program Files\\Blender Foundation\\Blender 5.3\\blender.exe" -b --python tools/art/blender/build_uncle_s21.py -- <uncle_prototype.fbx> <salida.fbx> <salida.blend>

Que hace:
- Importa el FBX del kit SOLO para tomar su esqueleto (mismos nombres, misma pose de reposo): la malla nueva se liga a
  esos huesos y Unity la monta sobre el rig fisico existente (sin tocar colliders, joints, masas ni puntos de agarre).
- Cuerpo continuo con modificador Piel (skin) + subdivisión. La ropa usa la misma superficie deformable del cuerpo:
  camisa, camiseta en el cuello, bermuda, medias, zapatos y cinturón. Solo la hebilla tiene malla aparte.
- Pesos propios por region (brazo, pierna, torso): ninguna mano toma peso de un muslo; el cuello va con el pecho (la
  cabeza la dibuja UncleHead en Unity) y una esfera guia de la cabeza va 100 % al hueso «head» (marca el tamaño de la
  cabeza nueva y se oculta con el).
- Frente del tio: -Y (como la cara del kit). Unidades: metros, Z arriba. Sin aleatoriedad.
"""
import bpy, bmesh, sys, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:]
KIT, OUT_FBX, OUT_BLEND = argv[0], argv[1], argv[2]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=KIT)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
for o in list(bpy.data.objects):
    if o is not arm:
        bpy.data.objects.remove(o, do_unlink=True)
for a in list(bpy.data.actions):          # la malla nueva no lleva clips: los muestrea Unity del FBX del kit
    bpy.data.actions.remove(a)
arm.animation_data_clear()
B = {b.name: (arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local) for b in arm.data.bones}

# ------------------------------------------------------------------ esqueleto de la piel (vertices, aristas, radios x/y)
verts, edges, radii = [], [], []
def V(p, rx, ry=None):
    verts.append(Vector(p)); radii.append((rx, ry if ry is not None else rx)); return len(verts) - 1
def E(a, b): edges.append((a, b))
def chain(points):
    ids = [V(*p) for p in points]
    for i in range(len(ids) - 1): E(ids[i], ids[i + 1])
    return ids

F = -1.0  # frente = -Y
# tronco: entrepierna -> cadera -> panza -> pecho -> cuello
crotch = V((0, 0.0, 0.80), 0.22, 0.19)
hips   = V((0, 0.0, 0.92), 0.37, 0.27)
belly  = V((0, 0.06 * F, 1.07), 0.38, 0.34)
chest  = V((0, 0.02 * F, 1.24), 0.35, 0.27)
upch   = V((0, -0.035, 1.35), 0.33, 0.272)
neck0  = V((0, 0.01, 1.45), 0.105, 0.10)
neck1  = V((0, 0.02, 1.54), 0.095, 0.09)
for a, b in [(crotch, hips), (hips, belly), (belly, chest), (chest, upch), (upch, neck0), (neck0, neck1)]: E(a, b)

def side(sx):
    # Skin needs a tree. A second connection from the neck to this branch opens a loop at the shoulder.
    sh_in  = V((0.25 * sx, 0.0, 1.37), 0.132, 0.121); E(upch, sh_in)
    sh     = V((0.355 * sx, 0.0, 1.355), 0.12, 0.11); E(sh_in, sh)
    ua, fa = B["upper_arm." + ("L" if sx > 0 else "R")], B["forearm." + ("L" if sx > 0 else "R")]
    mid_u  = V(tuple((ua[0] + ua[1]) * 0.5), 0.118, 0.109); E(sh, mid_u)
    elbow  = V(tuple(ua[1]), 0.10, 0.095); E(mid_u, elbow)
    mid_f  = V(tuple(fa[0] * 0.5 + fa[1] * 0.5), 0.092, 0.082); E(elbow, mid_f)
    d = (fa[1] - fa[0]).normalized()
    wrist  = V(tuple(fa[0] + (fa[1] - fa[0]) * 0.93), 0.058, 0.046); E(mid_f, wrist)
    # mano: palma ancha y plana; dedos en abanico apuntando hacia abajo, levemente curvados hacia el cuerpo
    side_v = Vector((0, 1, 0)).cross(d).normalized()      # a lo ancho de la palma (adelante/atras)
    palm_c = Vector(verts[wrist]) + d * 0.075
    palm   = V(tuple(palm_c), 0.05, 0.028); E(wrist, palm)
    inward = Vector((-sx, 0, 0))
    for k, (off, length) in enumerate([(-0.030, 0.085), (-0.010, 0.095), (0.010, 0.090), (0.029, 0.075)]):
        base = palm_c + d * 0.045 + side_v * off
        p1 = base + d * length * 0.42 + inward * 0.006
        p2 = base + d * length * 0.78 + inward * 0.018
        p3 = base + d * length + inward * 0.032
        ids = chain([(tuple(base), 0.0135), (tuple(p1), 0.0125), (tuple(p2), 0.011), (tuple(p3), 0.0085)])
        E(palm, ids[0])
    tb = palm_c - side_v * 0.045 + d * 0.005
    t_ids = chain([(tuple(tb), 0.016), (tuple(tb + d * 0.03 + side_v * -0.02 + inward * 0.01), 0.014), (tuple(tb + d * 0.06 + side_v * -0.025 + inward * 0.022), 0.011)])
    E(palm, t_ids[0])
    # pierna
    th, cf = B["thigh." + ("L" if sx > 0 else "R")], B["calf." + ("L" if sx > 0 else "R")]
    hip   = V((th[0].x, 0.0, 0.86), 0.23, 0.22); E(hips, hip)
    mid_t = V(tuple((th[0] + th[1]) * 0.5), 0.20, 0.195); E(hip, mid_t)
    knee  = V(tuple(th[1]), 0.14, 0.14); E(mid_t, knee)
    calf  = V(tuple(cf[0] * 0.55 + cf[1] * 0.45 + Vector((0, 0.015, 0))), 0.13, 0.13); E(knee, calf)
    ankle = V((cf[1].x, 0.01, 0.13), 0.075, 0.075); E(calf, ankle)
    heel  = V((cf[1].x, 0.045, 0.058), 0.068, 0.063); E(ankle, heel)
    ball  = V((cf[1].x + 0.008 * sx, -0.10, 0.048), 0.076, 0.052); E(heel, ball)
    toe   = V((cf[1].x + 0.012 * sx, -0.20, 0.046), 0.064, 0.046); E(ball, toe)

side(1.0); side(-1.0)
assert len(edges) == len(verts) - 1, "El esqueleto auxiliar de Skin debe ser un árbol"

me = bpy.data.meshes.new("UncleBodySkel"); me.from_pydata([tuple(v) for v in verts], edges, [])
body = bpy.data.objects.new("Uncle_S21_Body", me); bpy.context.collection.objects.link(body)
sk = body.modifiers.new("Skin", "SKIN"); sk.use_smooth_shade = True; sk.branch_smoothing = 0.6
for i, r in enumerate(radii): me.skin_vertices[0].data[i].radius = r
me.skin_vertices[0].data[hips].use_root = True
sub = body.modifiers.new("Sub", "SUBSURF"); sub.levels = 3; sub.render_levels = 3
bpy.context.view_layer.objects.active = body; body.select_set(True)
bpy.ops.object.modifier_apply(modifier="Skin"); bpy.ops.object.modifier_apply(modifier="Sub")
bpy.ops.object.shade_smooth()
me = body.data

# ------------------------------------------------------------------ pliegues suaves de la tela (desplazamiento por zona)
def in_shirt(p): return p.z > 0.96 and abs(p.x) < 0.50 and p.z < 1.44
bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table(); bm.normal_update()
for v in bm.verts:
    p = v.co
    if in_shirt(p):   # arrugas horizontales de la camisa sobre la panza y en la espalda baja
        k = 0.004 * math.sin(p.z * 55.0 + p.x * 7.0) * max(0.0, 1.0 - abs(p.z - 1.06) * 4.0)
        v.co += v.normal * k
    elif 0.55 < p.z < 0.96 and abs(p.x) < 0.42:   # bermuda: pliegues verticales en las piernas
        k = 0.004 * math.sin(math.atan2(p.y, p.x - math.copysign(0.18, p.x)) * 6.0)
        v.co += v.normal * k
# Coloca aristas exactamente en las costuras para que el color no siga un zigzag entre caras.
def seam(axis, value):
    normal = Vector(tuple(float(i == axis) for i in range(3)))
    bmesh.ops.bisect_plane(bm, geom=[*bm.verts, *bm.edges, *bm.faces], dist=0.00001,
                          plane_co=normal * value, plane_no=normal)

for z in (0.15, 0.30, 0.52, 0.915, 0.965, 1.20, 1.30, 1.43):
    seam(2, z)
for x in (-0.55, -0.44, -0.14, -0.085, 0.085, 0.14, 0.44, 0.55):
    seam(0, x)
seam(1, -0.10)
bm.to_mesh(me); bm.free()

# ------------------------------------------------------------------ materiales por zona
MATS = ["skin", "wine", "cream", "mustard", "shoe", "sock", "belt", "buckle"]
for n in MATS:
    m = bpy.data.materials.get(n) or bpy.data.materials.new(n)
    me.materials.append(m)
MI = {n: i for i, n in enumerate(MATS)}
def zone(p):
    if p.z < 0.15: return "shoe"
    if p.z < 0.30 and abs(p.x) > 0.06: return "sock"
    if p.z > 1.43 and abs(p.x) < 0.14: return "skin"                      # cuello
    ax = abs(p.x)
    if ax > 0.44:                                                       # brazos: manga corta hasta mitad del brazo
        return "wine" if (p.z > 1.20 and ax < 0.55) else "skin"
    if 0.915 <= p.z < 0.965: return "belt"
    if p.z >= 0.965:
        # La abertura se limita al cuello, sobre la misma superficie que la camisa.
        if p.y < -0.10 and p.z > 1.30 and ax < 0.085: return "cream"
        return "wine"
    if p.z >= 0.52: return "mustard"
    return "skin"
for f in me.polygons:
    f.material_index = MI[zone(body.matrix_world @ f.center)]

# ------------------------------------------------------------------ hebilla sobre la superficie del cinturón
bvh = BVHTree.FromObject(body, bpy.context.evaluated_depsgraph_get())
pieces = []
# hebilla (adelante, -Y) y presillas
bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.935))
bk = bpy.context.active_object; bk.name = "Buckle"; bk.scale = (0.07, 0.012, 0.055)
hit = bvh.ray_cast(Vector((0, -0.6, 0.935)), Vector((0, 1, 0)), 0.6); bk.location.y = (hit[0].y if hit[0] else -0.3) - 0.02
bpy.ops.object.transform_apply(scale=True); bk.data.materials.append(bpy.data.materials["buckle"]); pieces.append(bk)
# cabeza guia (la tapa UncleHead en Unity): marca centro y tamaño de la cabeza nueva
bpy.ops.mesh.primitive_uv_sphere_add(radius=0.21, location=(0, -0.04, 1.665), segments=24, ring_count=16)
guide = bpy.context.active_object; guide.name = "Head_Guide"; guide.data.materials.append(bpy.data.materials["skin"])

# una sola malla para Unity (un SkinnedMeshRenderer)
for o in pieces + [guide]: o.select_set(True)
body.select_set(True); bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
me = body.data

# ------------------------------------------------------------------ UV (tela con textura de trama en Unity)
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.004)
bpy.ops.object.mode_set(mode="OBJECT")

# ------------------------------------------------------------------ pesos por region
def seg_dist(p, a, b):
    ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / max(1e-9, ab.dot(ab)))); return (a + ab * t - p).length
groups = {n: body.vertex_groups.new(name=n) for n in ["pelvis", "chest", "head", "upper_arm.L", "forearm.L", "upper_arm.R", "forearm.R", "thigh.L", "calf.L", "thigh.R", "calf.R"]}
gc = Vector((0, -0.04, 1.665))
for v in me.vertices:
    p = body.matrix_world @ v.co
    if (p - gc).length < 0.215 and p.z > 1.47:       # esfera guia de la cabeza
        groups["head"].add([v.index], 1.0, "REPLACE"); continue
    s = "L" if p.x > 0 else "R"; ax = abs(p.x)
    # brazo: cerca del antebrazo o del brazo de ese lado (a cualquier altura: las manos cuelgan por debajo de 0,70 m y antes
    # caian en la region de las piernas; al bajar los brazos los dedos se estiraban hacia el muslo)
    fa0, fa1 = B["forearm." + s]; hand_end = fa1 + (fa1 - fa0).normalized() * 0.24   # el antebrazo sigue por la mano y los dedos
    near_arm = seg_dist(p, fa0, hand_end) < 0.15 or seg_dist(p, *B["upper_arm." + s]) < 0.17
    if near_arm or (ax > 0.40 and p.z > 0.70):
        cands = ["chest", "upper_arm." + s, "forearm." + s]
    elif p.z < 0.84 and ax > 0.03:
        cands = ["pelvis", "thigh." + s, "calf." + s]
    else:
        cands = ["pelvis", "chest"] + (["thigh." + s] if p.z < 0.88 else [])
    ws = []
    for n in cands:
        a, b = B[n]
        if n == "chest": a, b = B["chest"][0], Vector((0, 0, 1.47))
        if n == "pelvis": a, b = Vector((0, 0, 0.82)), Vector((0, 0, 1.0))
        if n.startswith("forearm"): b = b + (b - a).normalized() * 0.24   # mano y dedos van con el antebrazo
        d = seg_dist(p, a, b); ws.append((n, 1.0 / (d ** 4 + 1e-5)))
    ws.sort(key=lambda x: -x[1]); ws = ws[:2]; tot = sum(w for _, w in ws)
    ws = [(n, w) for n, w in ws if w / tot > 0.02]
    tot = sum(w for _, w in ws)
    for n, w in ws:
        groups[n].add([v.index], w / tot, "REPLACE")
body.parent = arm
mod = body.modifiers.new("Armature", "ARMATURE"); mod.object = arm

# Comprueba la malla que se exportará, incluidas la hebilla y la guía de cabeza.
bm = bmesh.new()
bm.from_mesh(me)
non_manifold = sum(not edge.is_manifold for edge in bm.edges)
bm.free()
assert non_manifold == 0, f"{non_manifold} aristas abiertas o no manifold"
assert all(abs(sum(g.weight for g in v.groups) - 1.0) < 0.001 for v in me.vertices), "Pesos sin normalizar"

# ------------------------------------------------------------------ exportar
bpy.ops.object.select_all(action="DESELECT"); body.select_set(True); arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                         axis_forward="-Z", axis_up="Y", add_leaf_bones=False, bake_anim=False, mesh_smooth_type="FACE",
                         use_armature_deform_only=True, object_types={"ARMATURE", "MESH"})
bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
print("S21 vertices", len(me.vertices), "caras", len(me.polygons), "materiales", [m.name for m in me.materials])
