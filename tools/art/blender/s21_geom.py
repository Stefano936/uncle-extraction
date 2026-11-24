# Ayudantes de geometria de la sesion 21 (modelado por script en el marco de Unity: x derecha, y arriba, z adelante).
# Los usan build_bar_s21.py (y son los mismos que build_route_s21.py, que los trae copiados para no reexportar la ruta).
# Conversion comprobada con los limites importados en Unity (RouteS21Setup): Unity (x, y, z) = Blender (-x, z, -y).
import bpy, bmesh, math, os
from mathutils import Vector, Matrix


class Part:
    def __init__(self, name):
        self.name = name; self.bm = bmesh.new(); self.mats = []

    def _add(self, tmp, mat, smooth, recalc=True):
        if recalc: bmesh.ops.recalc_face_normals(tmp, faces=tmp.faces[:])
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

    def tube(self, pts, r, mat, corner=0.0, seg=12, n=6, caps=True, closed=False):
        pts = [Vector(p) for p in pts]
        if corner > 0 and len(pts) > 2: pts = rounded(pts, corner, n)
        t = bmesh.new(); rings = []
        tan0 = (pts[1] - pts[0]).normalized()
        nrm = tan0.orthogonal().normalized()
        m = len(pts)
        for i, p in enumerate(pts):
            if closed: tg = ((pts[(i + 1) % m] - p).normalized() + (p - pts[i - 1]).normalized()).normalized()
            elif i == 0: tg = tan0
            elif i == m - 1: tg = (pts[-1] - pts[-2]).normalized()
            else: tg = ((pts[i + 1] - pts[i]).normalized() + (pts[i] - pts[i - 1]).normalized()).normalized()
            nrm = (nrm - tg * nrm.dot(tg)).normalized(); bi = tg.cross(nrm)
            rings.append([t.verts.new(p + (nrm * math.cos(a) + bi * math.sin(a)) * r) for a in (2 * math.pi * k / seg for k in range(seg))])
        pairs = list(zip(rings, rings[1:])) + ([(rings[-1], rings[0])] if closed else [])
        for a, b in pairs:
            for k in range(seg): t.faces.new((a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]))
        if caps and not closed: t.faces.new(rings[0]); t.faces.new(rings[-1])
        self._add(t, mat, True)

    def ring(self, c, R, r, mat, seg=24, tseg=8):
        """Aro horizontal (toro) de radio R y grosor r centrado en c."""
        pts = [(c[0] + R * math.cos(2 * math.pi * k / seg), c[1], c[2] + R * math.sin(2 * math.pi * k / seg)) for k in range(seg)]
        self.tube(pts, r, mat, seg=tseg, closed=True)

    def cyl(self, a, b, r, mat, seg=16):
        self.tube([a, b], r, mat, seg=seg)

    def lathe(self, c, prof, mat, seg=20, smooth=True):
        """Solido de revolucion alrededor del eje y que pasa por c. prof = [(radio, y)] de abajo hacia arriba; los extremos
        con radio 0 cierran la pieza (si no, se tapan con una cara)."""
        t = bmesh.new(); rings = []
        for r, y in prof:
            if r <= 1e-5: rings.append([t.verts.new((c[0], c[1] + y, c[2]))])
            else: rings.append([t.verts.new((c[0] + r * math.cos(2 * math.pi * k / seg), c[1] + y, c[2] + r * math.sin(2 * math.pi * k / seg))) for k in range(seg)])
        for a, b in zip(rings, rings[1:]):
            if len(a) == 1 and len(b) == 1: continue
            if len(a) == 1:
                for k in range(seg): t.faces.new((a[0], b[(k + 1) % seg], b[k]))
            elif len(b) == 1:
                for k in range(seg): t.faces.new((a[k], a[(k + 1) % seg], b[0]))
            else:
                for k in range(seg): t.faces.new((a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]))
        if len(rings[0]) > 1: t.faces.new(rings[0])
        if len(rings[-1]) > 1: t.faces.new(rings[-1])
        self._add(t, mat, smooth)

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


def material(name, colors):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    c = colors.get(name, (0.5, 0.5, 0.5)); m.diffuse_color = (c[0], c[1], c[2], 1.0); return m


def write(part, colors):
    """Convierte a Blender (-x, -z, y), proyecta UV en metros y crea el objeto."""
    bm = part.bm
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.normal_update()
    uv = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            p = l.vert.co
            l[uv].uv = (p.z, p.y) if ax == 0 else (p.x, p.z) if ax == 1 else (p.x, p.y)
    for v in bm.verts:
        p = v.co; v.co = Vector((-p.x, -p.z, p.y))
    bmesh.ops.reverse_faces(bm, faces=bm.faces[:])   # la conversion es un reflejo: invertir el sentido de las caras
    me = bpy.data.meshes.new(part.name); bm.to_mesh(me); bm.free()
    for mn in part.mats: me.materials.append(material(mn, colors))
    ob = bpy.data.objects.new(part.name, me); bpy.context.scene.collection.objects.link(ob)
    print(f"[s21] {part.name}: {len(me.vertices)} vertices, {len(me.polygons)} caras, materiales {part.mats}")
    return ob


def clear():
    for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes): bpy.data.meshes.remove(me)


def export(objs, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_anim=False, mesh_smooth_type="FACE", object_types={"MESH"})


def U(x, y, z):
    """Punto de Unity en coordenadas de Blender (camaras de control)."""
    return Vector((-x, -z, y))
