# Entorno de la ruta (sesion 21): carrito, camioneta (carroceria, interior de carga y hojas traseras), puerta de salida
# y cuadra de enfrente. Todo es VISUAL: se monta en ejecucion sobre la geometria funcional existente (colisiones, masas,
# sockets, piso de carga, rampa y zona sin cambios). Las medidas salen de PhysicsLabBuilder / LevelSpecs (Bar_LastRound).
# Coordenadas: se modela en el marco de Unity (x derecha, y arriba, z adelante) y se convierte al exportar.
# Uso (PowerShell): & "C:\Program Files\Blender Foundation\Blender 5.3\blender.exe" -b --python tools\art\blender\build_route_s21.py
#      [-- --preview carpeta]   (con --preview guarda vistas de control en esa carpeta)
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "RouteS21")
SRC = os.path.join(ROOT, "art_source", "route_s21")
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PREVIEW = ARGS[ARGS.index("--preview") + 1] if "--preview" in ARGS else None
# Conversion comprobada con los limites importados en Unity (RouteS21Setup): Unity (x, y, z) = Blender (-x, z, -y)

V = lambda x, y, z: Vector((x, y, z))


# ------------------------------------------------------------------ geometria (en coordenadas de Unity)
class Part:
    def __init__(self, name):
        self.name = name; self.bm = bmesh.new(); self.mats = []

    def _add(self, tmp, mat, smooth):
        bmesh.ops.recalc_face_normals(tmp, faces=tmp.faces[:])
        if mat not in self.mats: self.mats.append(mat)
        idx = self.mats.index(mat); vm = {}
        for v in tmp.verts: vm[v] = self.bm.verts.new(v.co)
        for f in tmp.faces:
            nf = self.bm.faces.new([vm[v] for v in f.verts]); nf.material_index = idx; nf.smooth = smooth
        tmp.free()

    def box(self, c, s, mat, bevel=0.0, rot=None, seg=2):
        t = bmesh.new(); bmesh.ops.create_cube(t, size=1.0)
        m = Matrix.Diagonal((s[0], s[1], s[2], 1.0))
        if rot is not None: m = rot.to_4x4() @ m
        for v in t.verts: v.co = (m @ v.co) + Vector(c)
        if bevel > 0:
            bmesh.ops.bevel(t, geom=t.edges[:] + t.verts[:], offset=min(bevel, min(s) * 0.45), segments=seg, profile=0.5,
                            affect="EDGES", clamp_overlap=True)
        self._add(t, mat, False)

    def tube(self, pts, r, mat, corner=0.0, seg=12, n=6, caps=True):
        pts = [Vector(p) for p in pts]
        if corner > 0 and len(pts) > 2: pts = rounded(pts, corner, n)
        t = bmesh.new(); rings = []
        tan0 = (pts[1] - pts[0]).normalized()
        nrm = tan0.orthogonal().normalized()
        for i, p in enumerate(pts):
            if i == 0: tg = tan0
            elif i == len(pts) - 1: tg = (pts[-1] - pts[-2]).normalized()
            else: tg = ((pts[i + 1] - pts[i]).normalized() + (pts[i] - pts[i - 1]).normalized()).normalized()
            nrm = (nrm - tg * nrm.dot(tg)).normalized(); bi = tg.cross(nrm)
            rings.append([t.verts.new(p + (nrm * math.cos(a) + bi * math.sin(a)) * r) for a in (2 * math.pi * k / seg for k in range(seg))])
        for a, b in zip(rings, rings[1:]):
            for k in range(seg): t.faces.new((a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]))
        if caps: t.faces.new(rings[0]); t.faces.new(rings[-1])
        self._add(t, mat, True)

    def cyl(self, a, b, r, mat, seg=16):
        self.tube([a, b], r, mat, seg=seg)

    def prism(self, prof, x0, x1, mat, bevel=0.0):
        """Perfil (z, y) extruido en x de x0 a x1."""
        t = bmesh.new()
        a = [t.verts.new((x0, y, z)) for z, y in prof]; b = [t.verts.new((x1, y, z)) for z, y in prof]
        t.faces.new(a); t.faces.new(b)
        n = len(prof)
        for k in range(n): t.faces.new((a[k], a[(k + 1) % n], b[(k + 1) % n], b[k]))
        if bevel > 0:
            bmesh.ops.bevel(t, geom=t.edges[:] + t.verts[:], offset=bevel, segments=2, profile=0.5, affect="EDGES", clamp_overlap=True)
        self._add(t, mat, False)


def rounded(pts, rad, n):
    out = [pts[0]]
    for i in range(1, len(pts) - 1):
        p0, p1, p2 = pts[i - 1], pts[i], pts[i + 1]
        d0 = (p0 - p1).normalized(); d2 = (p2 - p1).normalized()
        ang = d0.angle(d2)
        if ang > math.pi - 1e-3: out.append(p1); continue
        tt = min(rad / math.tan(ang / 2), (p0 - p1).length * 0.49, (p2 - p1).length * 0.49)
        a = p1 + d0 * tt; b = p1 + d2 * tt
        for k in range(n + 1):
            s = k / n; out.append((1 - s) ** 2 * a + 2 * (1 - s) * s * p1 + s * s * b)
    out.append(pts[-1]); return out


def arc(cz, cy, r, a0, a1, n=12):
    return [(cz + r * math.cos(math.radians(a0 + (a1 - a0) * k / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]


def rot_x(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


# ------------------------------------------------------------------ materiales (los reemplaza Unity por nombre)
COLORS = {
    "RS_CartPaint": (0.95, 0.42, 0.1), "RS_Steel": (0.66, 0.67, 0.7), "RS_Rubber": (0.05, 0.05, 0.055), "RS_Wood": (0.62, 0.45, 0.28),
    "RS_VanBody": (0.8, 0.72, 0.56), "RS_VanStripe": (0.72, 0.28, 0.12), "RS_VanInner": (0.55, 0.45, 0.32), "RS_Black": (0.04, 0.04, 0.045),
    "RS_Chrome": (0.85, 0.86, 0.88), "RS_Glass": (0.05, 0.07, 0.09), "RS_Tail": (1.0, 0.08, 0.05), "RS_Head": (1.0, 0.95, 0.8),
    "RS_Amber": (1.0, 0.55, 0.1), "RS_Plate": (0.92, 0.92, 0.88), "RS_DoorPaint": (0.1, 0.25, 0.2), "RS_DoorGlass": (0.2, 0.28, 0.3),
    "RS_Brick": (0.42, 0.2, 0.14), "RS_Stucco": (0.5, 0.5, 0.52), "RS_StuccoWarm": (0.55, 0.45, 0.35), "RS_Concrete": (0.4, 0.4, 0.4),
    "RS_Trim": (0.12, 0.11, 0.1), "RS_WinWarm": (1.0, 0.75, 0.45), "RS_WinCold": (0.5, 0.7, 1.0), "RS_WinDark": (0.04, 0.05, 0.07),
    "RS_Awning": (0.55, 0.1, 0.08), "RS_Shutter": (0.45, 0.46, 0.48), "RS_ShopGlow": (1.0, 0.8, 0.5), "RS_Sign": (0.08, 0.3, 0.25),
}


def material(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    c = COLORS.get(name, (0.5, 0.5, 0.5)); m.diffuse_color = (c[0], c[1], c[2], 1.0); return m


def write(part, origin=(0, 0, 0)):
    """Convierte a Blender (-x, -z, y), proyecta UV en metros y crea el objeto."""
    bm = part.bm; o = Vector(origin)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.normal_update()
    uv = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            p = l.vert.co
            l[uv].uv = (p.z, p.y) if ax == 0 else (p.x, p.z) if ax == 1 else (p.x, p.y)
    for v in bm.verts:
        p = v.co - o; v.co = Vector((-p.x, -p.z, p.y))
    bmesh.ops.reverse_faces(bm, faces=bm.faces[:])   # la conversion es un reflejo: invertir el sentido de las caras
    me = bpy.data.meshes.new(part.name); bm.to_mesh(me); bm.free()
    for mn in part.mats: me.materials.append(material(mn))
    ob = bpy.data.objects.new(part.name, me); bpy.context.scene.collection.objects.link(ob)
    print(f"[route] {part.name}: {len(me.vertices)} vertices, {len(me.polygons)} caras, materiales {part.mats}")
    return ob


# ================================================================== CARRITO (marco local del Transport_Cart)
def cart():
    p = Part("RS_Cart")
    P, S, R, W = "RS_CartPaint", "RS_Steel", "RS_Rubber", "RS_Wood"
    # chasis pintado (piso funcional: y 0,25-0,35; 0,90 x 1,40 m)
    for x in (-0.42, 0.42): p.box((x, 0.272, 0), (0.05, 0.06, 1.38), P, 0.008)
    for z in (-0.67, 0.0, 0.67): p.box((0, 0.272, z), (0.84, 0.05, 0.05), P, 0.008)
    for x in (-0.445, 0.445): p.box((x, 0.32, 0), (0.02, 0.06, 1.4), P, 0.004)          # cantos del piso
    for i in range(6): p.box((0, 0.336, -0.6 + i * 0.24), (0.86, 0.028, 0.215), W, 0.006)  # tablas
    # barandas tubulares (alto funcional de los laterales: 0,55 m)
    for x in (-0.43, 0.43):
        p.tube([(x, 0.34, -0.66), (x, 0.56, -0.66), (x, 0.56, 0.66), (x, 0.34, 0.66)], 0.016, S, corner=0.06)
        p.cyl((x, 0.34, 0.0), (x, 0.56, 0.0), 0.012, S)
        p.cyl((x, 0.45, -0.66), (x, 0.45, 0.66), 0.01, S)
    p.tube([(-0.43, 0.34, 0.685), (-0.43, 0.56, 0.685), (0.43, 0.56, 0.685), (0.43, 0.34, 0.685)], 0.016, S, corner=0.06)
    p.cyl((-0.43, 0.45, 0.685), (0.43, 0.45, 0.685), 0.01, S)
    # asa en U con empunaduras de goma (socket cart_handle en (0; 1,0; -0,74)) y tirantes
    p.tube([(-0.35, 0.3, -0.69), (-0.35, 1.0, -0.74), (0.35, 1.0, -0.74), (0.35, 0.3, -0.69)], 0.02, P, corner=0.08)
    for x in (-0.2, 0.2): p.cyl((x - 0.07, 1.0, -0.74), (x + 0.07, 1.0, -0.74), 0.027, R)
    for x in (-0.35, 0.35):
        p.cyl((x, 0.62, -0.715), (x, 0.3, -0.5), 0.011, S)
        p.box((x, 0.3, -0.69), (0.07, 0.025, 0.07), S, 0.004)
    # ruedas giratorias: placa, horquilla, rueda de goma con taza
    for x in (-0.38, 0.38):
        for z in (-0.55, 0.55):
            p.box((x, 0.236, z), (0.11, 0.012, 0.11), S, 0.003)
            p.cyl((x, 0.215, z), (x, 0.235, z), 0.03, S)
            for dx in (-0.032, 0.032): p.box((x + dx, 0.17, z + 0.02), (0.008, 0.09, 0.05), S, 0.002)
            p.cyl((x - 0.026, 0.12, z), (x + 0.026, 0.12, z), 0.12, R, seg=24)
            for s in (-1, 1): p.cyl((x + s * 0.026, 0.12, z), (x + s * 0.03, 0.12, z), 0.055, S, seg=16)
    # topes de goma en las esquinas
    for x in (-0.44, 0.44):
        for z in (-0.69, 0.69): p.box((x, 0.28, z), (0.05, 0.08, 0.05), R, 0.012)
    return [write(p)]


# ================================================================== CAMIONETA (marco del Passenger_Van = mundo)
RZ, FZ, CZ, FL, RF, HW = -9.25, -14.65, -12.4, 0.30, 2.3, 1.25
WR, WF, WRAD = -10.25, -13.55, 0.36   # ruedas (centros en z) y radio


def side_bottom(z_from, z_to, arches):
    """Borde inferior de la carroceria de z_from a z_to (z decreciente) con arcos sobre las ruedas."""
    pts = [(z_from, 0.42)]
    for wz in arches:
        r = WRAD + 0.11; a = math.degrees(math.asin((0.42 - WRAD) / r))
        pts += arc(wz, WRAD, r, a, 180 - a, 14)
    pts.append((z_to, 0.42)); return pts


def van():
    b = Part("RS_Van")
    B, ST, IN, K, CH, G = "RS_VanBody", "RS_VanStripe", "RS_VanInner", "RS_Black", "RS_Chrome", "RS_Glass"
    # cabina maciza (detras de la mampara no se ve el interior): perfil lateral con capot y parabrisas inclinado
    cab = side_bottom(CZ, FZ + 0.1, [WF]) + [(FZ + 0.02, 0.5), (FZ, 0.95), (FZ + 0.06, 1.16), (FZ + 0.62, 1.3),
                                             (FZ + 1.3, 2.22), (FZ + 1.45, 2.36), (CZ, 2.36)]
    b.prism(cab, -HW, HW, B, bevel=0.035)
    # caja de carga hueca: laterales con el arco trasero, techo y cabecero sobre las hojas
    side = side_bottom(RZ, CZ, [WR]) + [(CZ, 2.36), (RZ, 2.36)]
    for x0, x1 in ((HW - 0.08, HW), (-HW, -HW + 0.08)): b.prism(side, x0, x1, B, bevel=0.02)
    b.box((0, 2.32, (RZ + CZ) / 2), (HW * 2, 0.08, RZ - CZ), B, 0.03)
    b.box((0, 2.23, RZ + 0.05), (HW * 2 - 0.16, 0.26, 0.1), B, 0.015)        # cabecero (encima del dintel funcional)
    for s in (-1, 1): b.box((s * (HW - 0.04), 1.33, RZ + 0.03), (0.08, 2.06, 0.06), B, 0.012)  # parantes traseros
    # interior de carga: forro de terciado, barras de amarre, arcos del techo, mampara con ventanita
    L = RZ - CZ
    for s in (-1, 1):
        b.box((s * 1.165, 1.28, (RZ + CZ) / 2), (0.01, 1.92, L - 0.06), IN, 0.0)
        for y in (0.78, 1.5): b.box((s * 1.16, y, (RZ + CZ) / 2), (0.02, 0.045, L - 0.12), "RS_Steel", 0.004)
        for z in (-9.6, -10.9, -12.1): b.box((s * 1.158, 1.28, z), (0.018, 1.9, 0.06), IN, 0.004)
    b.box((0, 2.255, (RZ + CZ) / 2), (2.33, 0.012, L - 0.04), IN, 0.0)
    for z in (-9.7, -10.45, -11.2, -11.95): b.box((0, 2.235, z), (2.3, 0.03, 0.06), "RS_Steel", 0.006)
    b.box((0, 1.28, CZ + 0.03), (2.33, 1.94, 0.06), IN, 0.006)
    b.box((0, 1.82, CZ + 0.065), (0.9, 0.36, 0.012), G, 0.0)
    b.tube([(-0.47, 1.62, CZ + 0.07), (-0.47, 2.02, CZ + 0.07), (0.47, 2.02, CZ + 0.07), (0.47, 1.62, CZ + 0.07), (-0.47, 1.62, CZ + 0.07)],
           0.012, "RS_Steel", corner=0.04)
    b.box((0, 0.292, RZ + 0.02), (2.2, 0.025, 0.06), "RS_Steel", 0.006)    # umbral de aluminio (a ras del piso)
    # zocalos negros, guardabarros, franja y paragolpes
    for s in (-1, 1):
        x = s * (HW + 0.006)
        b.box((x, 0.44, (WR + 0.5 + RZ) / 2 + 0.02), (0.03, 0.18, abs(RZ - (WR + 0.5))), K, 0.008)
        b.box((x, 0.44, (WR - 0.5 + WF + 0.5) / 2), (0.03, 0.18, abs((WR - 0.5) - (WF + 0.5))), K, 0.008)
        for wz in (WR, WF):
            for dx in (0.03, 0.09):                                   # guardabarros ensanchados sobre la rueda
                pts = [(x + s * dx, y, z) for z, y in arc(wz, WRAD, WRAD + 0.1, 6, 174, 18)]
                b.tube(pts, 0.04, K, seg=8)
        b.box((s * (HW + 0.008), 1.17, (FZ + RZ) / 2 + 0.06), (0.012, 0.13, (RZ - FZ) - 0.3), ST, 0.003)
        b.box((s * (HW + 0.009), 1.27, (FZ + RZ) / 2 + 0.06), (0.012, 0.025, (RZ - FZ) - 0.3), B, 0.002)
        # ventanillas de la cabina y de la carga, juntas de puertas y manijas
        b.box((s * (HW + 0.008), 1.72, -13.0), (0.014, 0.6, 0.82), G, 0.01)
        b.box((s * (HW + 0.008), 1.78, -11.65), (0.014, 0.45, 1.3), G, 0.01)
        for z in (-12.48, -13.5, -10.95, -12.35):
            b.box((s * (HW + 0.004), 1.35, z), (0.012, 1.7, 0.01), K, 0.0)
        b.box((s * (HW + 0.004), 2.08, -11.6), (0.012, 0.02, 1.4), K, 0.0)       # riel de la puerta corrediza
        for z in (-12.7, -11.1): b.box((s * (HW + 0.02), 1.15, z), (0.025, 0.035, 0.16), CH, 0.008)
        # espejos
        b.box((s * (HW + 0.07), 1.62, -13.75), (0.14, 0.04, 0.05), K, 0.01)
        b.box((s * (HW + 0.17), 1.62, -13.73), (0.06, 0.28, 0.17), K, 0.025)
        # faros, giros y luces traseras envolventes
        b.box((s * 0.82, 0.96, FZ - 0.004), (0.34, 0.15, 0.03), "RS_Head", 0.01)
        b.box((s * 1.1, 0.96, FZ + 0.01), (0.12, 0.12, 0.03), "RS_Amber", 0.01)
        b.box((s * (HW + 0.012), 1.25, RZ - 0.18), (0.02, 0.62, 0.26), "RS_Tail", 0.008)
        b.box((s * (HW + 0.012), 0.86, RZ - 0.18), (0.02, 0.14, 0.26), "RS_Amber", 0.008)
        b.box((s * 1.05, 0.26, RZ + 0.07), (0.38, 0.17, 0.16), K, 0.025)          # paragolpes trasero (fuera de la rampa)
        # ruedas: cubierta, llanta y tuercas
        # la cara interna de la cubierta queda detras del forro (x 1,17): la rueda no asoma dentro de la caja de carga
        xw = s * 1.275
        b.cyl((xw - 0.1, WRAD, WR), (xw + 0.1, WRAD, WR), WRAD, "RS_Rubber", seg=28)
        b.cyl((xw - 0.1, WRAD, WF), (xw + 0.1, WRAD, WF), WRAD, "RS_Rubber", seg=28)
        for wz in (WR, WF):
            b.cyl((xw + s * 0.1, WRAD, wz), (xw + s * 0.108, WRAD, wz), 0.21, "RS_Steel", seg=24)
            b.cyl((xw + s * 0.108, WRAD, wz), (xw + s * 0.115, WRAD, wz), 0.07, CH, seg=16)
    # frente: parabrisas, limpiaparabrisas, parrilla, paragolpes y patente
    d = Vector((0, 2.22 - 1.3, (FZ + 1.3) - (FZ + 0.62))); ln = d.length; d.normalize()
    rot = Matrix(((1, 0, 0), (0, d.y, -d.z), (0, d.z, d.y)))
    c = Vector((0, (1.3 + 2.22) / 2, FZ + (0.62 + 1.3) / 2)) + Vector((0, d.z, -d.y)) * -0.012
    b.box(c, (2.2, ln - 0.08, 0.02), G, 0.03, rot=rot)
    for s in (-1, 1): b.box(c + Vector((s * 0.4, -0.38 * d.y, -0.38 * d.z)) + Vector((0, d.z, -d.y)) * -0.02, (0.55, 0.015, 0.015), K, 0.0, rot=rot)
    b.box((0, 0.68, FZ - 0.01), (1.1, 0.3, 0.03), K, 0.01)
    for y in (0.6, 0.68, 0.76): b.box((0, y, FZ - 0.028), (1.04, 0.018, 0.012), CH, 0.004)
    b.box((0, 0.33, FZ - 0.04), (HW * 2 + 0.06, 0.2, 0.14), K, 0.04)
    b.box((0, 0.33, FZ - 0.115), (0.52, 0.12, 0.01), "RS_Plate", 0.0)
    # bajos (chasis y escape) y portaequipajes
    b.box((0, 0.2, (FZ + RZ) / 2), (1.9, 0.12, (RZ - FZ) - 0.8), K, 0.0)
    b.cyl((-0.6, 0.17, RZ - 0.15), (-0.6, 0.17, RZ + 0.06), 0.035, "RS_Steel")
    for x in (-0.75, 0.75):
        b.tube([(x, 2.36, -12.1), (x, 2.46, -11.95), (x, 2.46, -9.75), (x, 2.36, -9.6)], 0.018, K, corner=0.08)
    for z in (-11.6, -10.85, -10.1): b.cyl((-0.78, 2.465, z), (0.78, 2.465, z), 0.014, K)
    return [write(b)]


def van_leaf(sign):
    """Hoja trasera en el marco de su bisagra (x = +-1,22; z = -9,23). sign = +1 hoja izquierda (abre hacia +x)."""
    p = Part("RS_VanLeaf_" + ("L" if sign > 0 else "R"))
    B = "RS_VanBody"
    cx = sign * 0.6
    p.box((cx, 1.2, 0.035), (1.18, 1.74, 0.04), B, 0.014)                    # piel exterior
    p.box((cx, 1.2, 0.011), (1.1, 1.66, 0.008), "RS_VanInner", 0.0)          # forro interior
    p.box((cx, 1.75, 0.057), (0.92, 0.42, 0.006), "RS_Glass", 0.01)          # ventanilla
    p.box((cx, 1.17, 0.056), (1.16, 0.13, 0.006), "RS_VanStripe", 0.002)
    p.box((cx, 1.27, 0.057), (1.16, 0.025, 0.006), B, 0.002)
    for y in (0.55, 1.2, 1.85): p.cyl((0.0, y - 0.06, 0.04), (0.0, y + 0.06, 0.04), 0.022, "RS_Black")
    if sign > 0: p.box((cx, 0.68, 0.058), (0.52, 0.12, 0.006), "RS_Plate", 0.0)
    else:
        p.box((-1.05, 1.05, 0.065), (0.04, 0.16, 0.025), "RS_Chrome", 0.008)
        p.box((-1.12, 1.33, 0.058), (0.02, 0.02, 0.006), "RS_Black", 0.0)
    return [write(p)]


# ================================================================== PUERTA DE SALIDA (marco de la bisagra)
def door():
    p = Part("RS_ExitDoor")
    D, S, G, K = "RS_DoorPaint", "RS_Steel", "RS_DoorGlass", "RS_Black"
    x0, x1, wx0, wx1, wy0, wy1 = 0.03, 1.47, 0.42, 1.08, 1.25, 1.86
    p.box(((x0 + x1) / 2, 0.635, 0), (x1 - x0, 1.25, 0.05), D, 0.01)
    p.box(((x0 + x1) / 2, (wy1 + 2.1) / 2, 0), (x1 - x0, 2.1 - wy1, 0.05), D, 0.01)
    p.box(((x0 + wx0) / 2, (wy0 + wy1) / 2, 0), (wx0 - x0, wy1 - wy0, 0.05), D, 0.006)
    p.box(((wx1 + x1) / 2, (wy0 + wy1) / 2, 0), (x1 - wx1, wy1 - wy0, 0.05), D, 0.006)
    p.box(((wx0 + wx1) / 2, (wy0 + wy1) / 2, 0), (wx1 - wx0, wy1 - wy0, 0.012), G, 0.0)
    for z in (-0.028, 0.028):
        p.tube([(wx0, wy0, z), (wx0, wy1, z), (wx1, wy1, z), (wx1, wy0, z), (wx0, wy0, z)], 0.012, K, seg=6)
        p.box((0.75, 0.6, z * 1.05), (1.04, 0.78, 0.008), D, 0.004)           # tablero en relieve
        p.box((0.75, 0.13, z * 1.2), (1.38, 0.22, 0.005), S, 0.002)          # placa de patada
    # adentro (+z): barra antipanico con soportes; afuera (-z): tirador vertical (socket en (1,3; 1,0))
    p.box((0.82, 1.0, 0.075), (1.0, 0.06, 0.045), S, 0.012)
    for x in (0.3, 1.34): p.box((x, 1.0, 0.05), (0.09, 0.13, 0.05), K, 0.012)
    p.tube([(1.3, 0.8, -0.025), (1.3, 0.8, -0.085), (1.3, 1.22, -0.085), (1.3, 1.22, -0.025)], 0.016, "RS_Chrome", corner=0.04)
    for y in (0.8, 1.22): p.box((1.3, y, -0.03), (0.05, 0.05, 0.012), "RS_Chrome", 0.004)
    p.box((1.3, 1.55, -0.028), (0.08, 0.08, 0.006), "RS_Chrome", 0.002)     # escudo de la cerradura
    for y in (0.3, 1.05, 1.8): p.cyl((0.01, y - 0.07, 0), (0.01, y + 0.07, 0), 0.022, S)
    p.box((0.75, 2.07, 0.028), (0.1, 0.05, 0.03), K, 0.006)                 # cierrapuertas
    p.box((0.45, 2.04, 0.05), (0.5, 0.05, 0.06), K, 0.01)
    return [write(p)]


# ================================================================== CUADRA DE ENFRENTE (mundo)
def street():
    p = Part("RS_Street")
    rnd = random.Random(7)
    front = -20.2
    wins = ["RS_WinWarm"] * 2 + ["RS_WinCold"] + ["RS_WinDark"] * 6
    for i in range(5):
        cx, h = -14.0 + i * 7.0, 6.0 + (i * 37 % 5)
        wall = ["RS_Brick", "RS_Stucco", "RS_StuccoWarm", "RS_Brick", "RS_Stucco"][i]
        x0, x1, back = cx - 3.4, cx + 3.4, front - 2.0
        p.box((cx, h / 2, (back + front - 0.2) / 2), (6.8, h, 1.8), wall, 0.0)        # volumen (cara a -20,40)
        # planta baja: zocalo, local con cortina o vidriera, cartel y toldo
        p.box((cx, 0.2, front - 0.02), (6.8, 0.4, 0.24), "RS_Concrete", 0.01)
        shop = i % 2 == 0
        for sx in (-1, 1): p.box((cx + sx * 3.0, 1.75, front - 0.1), (0.8, 3.1, 0.2), wall, 0.01)   # pilares
        p.box((cx, 3.05, front - 0.08), (6.8, 0.5, 0.24), "RS_Trim", 0.02)           # viga con cartel
        p.box((cx, 3.05, front + 0.045), (3.2, 0.36, 0.02), "RS_Sign", 0.005)
        if shop:
            for k in range(26): p.box((cx, 0.45 + k * 0.088, front - 0.16), (4.4, 0.07, 0.03), "RS_Shutter", 0.01)
            p.box((cx, 2.72, front - 0.12), (4.5, 0.18, 0.12), "RS_Shutter", 0.02)
        else:
            p.box((cx, 1.6, front - 0.2), (4.3, 2.3, 0.03), "RS_ShopGlow", 0.0)
            for mx in (-1.4, 0.0, 1.4): p.box((cx + mx, 1.6, front - 0.17), (0.06, 2.3, 0.05), "RS_Trim", 0.0)
            p.box((cx, 0.47, front - 0.15), (4.3, 0.06, 0.08), "RS_Trim", 0.0)
            d = rot_x(-25)
            p.box((cx, 2.62, front + 0.45), (4.6, 0.04, 1.1), "RS_Awning", 0.01, rot=d)
            p.box((cx, 2.4, front + 0.94), (4.6, 0.22, 0.02), "RS_Awning", 0.004)
        # pisos altos: ventanas hundidas con marco, alfeizar, dintel y algunas con balcon
        floors = int((h - 3.4) / 2.6)
        for fl in range(floors):
            y0 = 3.6 + fl * 2.6 + 0.55
            for c in range(3):
                wx = cx - 2.0 + c * 2.0
                p.box((wx, y0 + 0.65, front - 0.19), (1.0, 1.3, 0.02), rnd.choice(wins), 0.0)   # vidrio hundido
                p.box((wx, y0 + 0.65, front - 0.17), (0.04, 1.3, 0.03), "RS_Trim", 0.0)
                p.box((wx, y0 + 0.9, front - 0.17), (1.0, 0.04, 0.03), "RS_Trim", 0.0)
                p.box((wx, y0 - 0.04, front + 0.05), (1.2, 0.07, 0.22), "RS_Concrete", 0.01)
                p.box((wx, y0 + 1.36, front + 0.01), (1.16, 0.12, 0.08), "RS_Concrete", 0.01)
                # marco del vano (cara del muro con el hueco)
                p.box((wx - 0.55, y0 + 0.65, front - 0.1), (0.1, 1.3, 0.2), wall, 0.0)
                p.box((wx + 0.55, y0 + 0.65, front - 0.1), (0.1, 1.3, 0.2), wall, 0.0)
                if (i + fl + c) % 5 == 0:
                    p.box((wx, y0 - 0.02, front + 0.35), (1.5, 0.08, 0.7), "RS_Concrete", 0.01)
                    p.tube([(wx - 0.72, y0, front + 0.68), (wx - 0.72, y0 + 0.95, front + 0.68), (wx + 0.72, y0 + 0.95, front + 0.68),
                            (wx + 0.72, y0, front + 0.68)], 0.018, "RS_Trim", corner=0.03, seg=8)
                    for k in range(9): p.cyl((wx - 0.64 + k * 0.16, y0, front + 0.68), (wx - 0.64 + k * 0.16, y0 + 0.95, front + 0.68), 0.008, "RS_Trim", seg=6)
            # paños de muro entre ventanas (cara frontal a -20,2)
            yb, yt = y0 - 0.55, y0 + 2.05
            p.box((cx, (yb + y0) / 2, front - 0.1), (6.8, y0 - yb, 0.2), wall, 0.0)
            p.box((cx, (y0 + 1.3 + yt) / 2, front - 0.1), (6.8, yt - (y0 + 1.3), 0.2), wall, 0.0)
            for px in (cx - 3.0, cx - 1.0, cx + 1.0, cx + 3.0):
                wdt = 0.8 if abs(px - cx) > 2.5 else 0.8
                p.box((px, y0 + 0.65, front - 0.1), (wdt, 1.3, 0.2), wall, 0.0)
        top = 3.6 + floors * 2.6
        if top < h: p.box((cx, (top + h) / 2, front - 0.1), (6.8, h - top, 0.2), wall, 0.0)
        p.box((cx, h + 0.1, front + 0.06), (7.0, 0.22, 0.32), "RS_Concrete", 0.03)      # cornisa
        p.box((cx, h + 0.4, front - 0.05), (6.8, 0.4, 0.2), wall, 0.01)                # parapeto
        p.box((cx + 3.4, h / 2, front + 0.02), (0.12, h, 0.12), "RS_Concrete", 0.01)   # junta entre edificios
    # vereda de enfrente con cordon
    p.box((0, 0.07, -19.3), (36.0, 0.14, 1.8), "RS_Concrete", 0.0)
    p.box((0, 0.08, -18.36), (36.0, 0.16, 0.12), "RS_Concrete", 0.02)
    return [write(p)]


# ------------------------------------------------------------------ armar, exportar y vistas de control
def clear():
    for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes): bpy.data.meshes.remove(me)


def export(objs, name):
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_anim=False,
                             mesh_smooth_type="FACE", object_types={"MESH"})


clear()
groups = {"route_cart": cart(), "route_van": van(), "route_van_leaf_l": van_leaf(1), "route_van_leaf_r": van_leaf(-1),
          "route_exit_door": door(), "route_street": street()}
for n, objs in groups.items(): export(objs, n)
os.makedirs(SRC, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC, "route_s21.blend"))
print("[route] exportado en", OUT)

if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"; sc.render.resolution_x = 1200; sc.render.resolution_y = 800
    sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "MATERIAL"; sc.display.shading.show_cavity = True
    sc.display.shading.show_shadows = True
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    def U(x, y, z): return Vector((-x, -z, y))
    views = {
        "cart": [(U(1.6, 1.3, -1.8), U(0, 0.45, 0), "route_cart"), (U(-1.4, 1.6, 1.5), U(0, 0.45, 0), "route_cart")],
        "van": [(U(3.5, 2.2, -6.0), U(0, 1.1, -10.5), "route_van"), (U(-4.5, 1.8, -16.5), U(0, 1.0, -12.5), "route_van"),
                (U(0.0, 1.4, -7.2), U(0, 1.1, -11.5), "route_van")],
        "door": [(U(0.75, 1.1, -3.0), U(0.75, 1.05, 0), "route_exit_door"), (U(1.5, 1.3, 2.4), U(0.75, 1.05, 0), "route_exit_door")],
        "street": [(U(0, 2.0, -6.0), U(0, 4.0, -20.2), "route_street")],
    }
    for key, vs in views.items():
        for k, (pos, tgt, grp) in enumerate(vs):
            for n, objs in groups.items():
                show = n == grp or (grp == "route_van" and n.startswith("route_van"))
                for o in objs: o.hide_render = not show
            cam.location = pos; cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
            cam.data.lens = 28 if key != "street" else 20
            sc.render.filepath = os.path.join(PREVIEW, f"{key}_{k}.png"); bpy.ops.render.render(write_still=True)
    print("[route] vistas en", PREVIEW)
