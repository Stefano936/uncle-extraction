# Muebles del bar (sesion 21): barra con frente de paneles, tapa con canto redondo, baranda de anclaje y apoyapies de
# bronce, canillas y caja; mueble del fondo con espejo, estantes de vidrio y botellas; banquetas tapizadas; mesas; tarima
# de karaoke con microfono y parlantes; silla liviana (objeto dinamico) y vaso senuelo con cerveza.
# Todo es VISUAL: se monta en ejecucion (RouteDressing) sobre las colisiones existentes, que no cambian. Medidas y
# posiciones de LevelSpecs.Bar (Bar_LastRound) y PhysicsLabBuilder.
# Uso (PowerShell): & "C:\Program Files\Blender Foundation\Blender 5.3\blender.exe" -b --python tools\art\blender\build_bar_s21.py
#      [-- --preview carpeta]
import bpy, math, os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from s21_geom import Part, write, clear, export, U

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "RouteS21")
SRC = os.path.join(ROOT, "art_source", "route_s21")
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PREVIEW = ARGS[ARGS.index("--preview") + 1] if "--preview" in ARGS else None

COLORS = {
    "RS_BarWood": (0.36, 0.2, 0.1), "RS_BarTop": (0.22, 0.11, 0.05), "RS_Brass": (0.78, 0.58, 0.26), "RS_Chrome": (0.85, 0.86, 0.88),
    "RS_Black": (0.04, 0.04, 0.045), "RS_Steel": (0.66, 0.67, 0.7), "RS_Vinyl": (0.55, 0.08, 0.07), "RS_Mirror": (0.12, 0.14, 0.15),
    "RS_ShelfGlass": (0.5, 0.6, 0.6), "RS_BottleAmber": (0.55, 0.32, 0.08), "RS_BottleGreen": (0.12, 0.35, 0.15),
    "RS_BottleClear": (0.75, 0.78, 0.72), "RS_BottleRed": (0.45, 0.08, 0.1), "RS_Label": (0.9, 0.85, 0.7), "RS_Carpet": (0.2, 0.07, 0.22),
    "RS_LedMagenta": (0.95, 0.3, 1.0), "RS_Speaker": (0.05, 0.05, 0.05), "RS_Grille": (0.12, 0.12, 0.13), "RS_Beer": (0.85, 0.55, 0.12),
    "RS_Foam": (0.95, 0.93, 0.85), "RS_GlassClear": (0.8, 0.9, 0.95), "RS_TableWood": (0.42, 0.25, 0.13), "RS_Paper": (0.92, 0.9, 0.85),
}

# ================================================================== BARRA + MUEBLE DEL FONDO (origen: centro de la barra en el piso)
# Colision de la barra: 4,12 x 1,08 x 0,90 m (x +-2,06; z +-0,45; frente hacia -z). Baranda de anclaje: y 0,95, z -0,49, 4 m.
# Pared del fondo en z local 1,12 (mundo 9,97). Campana en x 1,7 (mundo 6,2) sobre la tapa: queda libre.
def counter():
    p = Part("RS_BarCounter")
    W, T, B, K = "RS_BarWood", "RS_BarTop", "RS_Brass", "RS_Black"
    hx, fz, bz = 2.06, -0.45, 0.45
    p.box((0, 0.05, 0.0), (2 * hx - 0.08, 0.1, 0.82), K, 0.0)                         # zocalo hundido
    p.box((0, 0.54, 0.0), (2 * hx, 0.88, 0.88), W, 0.01)                              # cuerpo
    # frente: pilastras, travesanos y paneles en relieve (6 tramos)
    bays = 6; bw = (2 * hx) / bays
    for i in range(bays + 1):
        x = -hx + i * bw
        p.box((x, 0.55, fz - 0.012), (0.09, 0.86, 0.03), W, 0.008)
    for y in (0.14, 0.95): p.box((0, y, fz - 0.012), (2 * hx + 0.06, 0.07, 0.03), W, 0.008)
    for i in range(bays):
        x = -hx + (i + 0.5) * bw
        p.box((x, 0.545, fz - 0.018), (bw - 0.2, 0.64, 0.02), W, 0.025, seg=3)
        p.box((x, 0.545, fz - 0.006), (bw - 0.14, 0.7, 0.012), T, 0.004)                # rebaje oscuro alrededor del panel
    for s in (-1, 1):                                                                 # laterales con un panel
        p.box((s * (hx + 0.012), 0.545, 0.0), (0.02, 0.64, 0.62), W, 0.02)
    # tapa con canto redondo (sobresale 6 cm al frente)
    p.box((0, 1.03, -0.02), (2 * hx + 0.1, 0.07, 0.98), T, 0.012)
    p.cyl((-hx - 0.05, 1.03, fz - 0.07), (hx + 0.05, 1.03, fz - 0.07), 0.04, T, seg=16)
    for s in (-1, 1): p.lathe((s * (hx + 0.05), 1.03, fz - 0.07), [(0, -0.04), (0.028, -0.028), (0.04, 0.0), (0.028, 0.028), (0, 0.04)], T, seg=16)
    # baranda de anclaje (bronce) con soportes y remates; apoyapies con soportes al piso
    p.cyl((-2.0, 0.95, fz - 0.04), (2.0, 0.95, fz - 0.04), 0.03, B, seg=16)
    for x in (-2.0, 2.0): p.lathe((x, 0.95, fz - 0.04), [(0, -0.04), (0.04, 0.0), (0, 0.04)], B, seg=12)
    for x in (-1.6, -0.55, 0.55, 1.6): p.box((x, 0.95, fz - 0.015), (0.03, 0.03, 0.05), B, 0.008)
    p.cyl((-1.95, 0.2, fz - 0.17), (1.95, 0.2, fz - 0.17), 0.026, B, seg=16)
    for x in (-1.95, 1.95): p.lathe((x, 0.2, fz - 0.17), [(0, -0.035), (0.035, 0.0), (0, 0.035)], B, seg=12)
    for x in (-1.8, -0.9, 0.0, 0.9, 1.8):
        p.box((x, 0.2, fz - 0.085), (0.025, 0.03, 0.17), B, 0.006)
        p.tube([(x, 0.2, fz - 0.17), (x, 0.03, fz - 0.17)], 0.012, B, seg=8)
        p.lathe((x, 0.0, fz - 0.17), [(0.035, 0.0), (0.035, 0.012), (0, 0.016)], B, seg=12)
    # sobre la tapa: dos torres de canillas, caja registradora, bandeja de vasos y servilletero (lejos de la campana)
    for tx in (-0.7, 0.45):
        p.box((tx, 1.08, 0.12), (0.12, 0.03, 0.12), "RS_Chrome", 0.01)
        p.cyl((tx, 1.08, 0.12), (tx, 1.36, 0.12), 0.045, "RS_Chrome", seg=16)
        p.lathe((tx, 1.36, 0.12), [(0.045, 0.0), (0.05, 0.02), (0, 0.035)], "RS_Chrome", seg=16)
        for dx in (-0.06, 0.06):
            p.cyl((tx + dx, 1.31, 0.12), (tx + dx, 1.31, 0.05), 0.012, "RS_Chrome", seg=8)
            p.box((tx + dx, 1.4, 0.06), (0.03, 0.12, 0.025), "RS_Black" if dx < 0 else "RS_BottleRed", 0.008)
        p.box((tx, 1.085, 0.04), (0.22, 0.01, 0.1), "RS_Steel", 0.004)                  # rejilla de goteo
    p.box((-1.6, 1.16, 0.2), (0.36, 0.16, 0.3), K, 0.02)                                # caja registradora
    p.box((-1.6, 1.27, 0.28), (0.3, 0.1, 0.06), K, 0.015, rot=None)
    p.box((-1.6, 1.27, 0.25), (0.22, 0.06, 0.01), "RS_LedMagenta", 0.0)
    p.box((-1.6, 1.25, 0.06), (0.3, 0.02, 0.14), "RS_Steel", 0.004)
    p.box((-0.15, 1.09, 0.22), (0.34, 0.02, 0.24), "RS_Black", 0.004)                   # bandeja con vasos
    for i in range(6): p.lathe((-0.27 + (i % 3) * 0.12, 1.1, 0.16 + (i // 3) * 0.12), [(0.03, 0.0), (0.036, 0.11), (0.0, 0.11)], "RS_GlassClear", seg=12)
    p.box((1.05, 1.13, -0.1), (0.1, 0.09, 0.06), "RS_Chrome", 0.006)                    # servilletero
    p.box((1.05, 1.14, -0.1), (0.08, 0.08, 0.04), "RS_Paper", 0.0)
    # ---- mueble del fondo (contra la pared, z 0,80 .. 1,12): bajo mesada, espejo, estantes de vidrio y botellas
    cz = 0.97
    p.box((0, 0.45, cz), (4.1, 0.9, 0.3), W, 0.01)
    for i in range(6):
        x = -1.71 + i * 0.684
        p.box((x, 0.47, cz - 0.155), (0.6, 0.72, 0.012), T, 0.004)
        p.box((x + 0.22, 0.62, cz - 0.17), (0.02, 0.1, 0.02), B, 0.005)
    p.box((0, 0.92, cz - 0.02), (4.2, 0.04, 0.36), T, 0.01)
    p.box((0, 1.65, 1.1), (3.9, 1.3, 0.02), "RS_Mirror", 0.0)                          # espejo
    for x in (-2.0, -0.67, 0.67, 2.0): p.box((x, 1.68, 1.07), (0.1, 1.5, 0.07), W, 0.01)  # columnas del marco
    p.box((0, 2.4, 1.05), (4.3, 0.1, 0.14), W, 0.015)                                  # cornisa
    p.box((0, 2.33, 1.06), (4.1, 0.04, 0.1), T, 0.006)
    rnd = random.Random(21)
    bottle_mats = ["RS_BottleAmber", "RS_BottleGreen", "RS_BottleClear", "RS_BottleRed"]
    shapes = [  # (radio, alto del cuerpo, cuello)
        [(0.035, 0.0), (0.036, 0.19), (0.03, 0.22), (0.013, 0.25), (0.013, 0.31), (0.0, 0.31)],          # whisky
        [(0.04, 0.0), (0.04, 0.14), (0.02, 0.2), (0.012, 0.22), (0.012, 0.29), (0.0, 0.29)],            # vino
        [(0.045, 0.0), (0.046, 0.12), (0.036, 0.15), (0.015, 0.18), (0.015, 0.22), (0.0, 0.22)],          # licor ancho
        [(0.03, 0.0), (0.03, 0.2), (0.02, 0.23), (0.01, 0.25), (0.01, 0.33), (0.0, 0.33)],               # alta
    ]
    for y in (1.18, 1.62, 2.02):
        p.box((0, y, 0.98), (3.86, 0.018, 0.24), "RS_ShelfGlass", 0.0)
        for s in (-1, 1): p.box((s * 1.88, y - 0.03, 1.05), (0.03, 0.05, 0.08), B, 0.006)
        x = -1.85
        while x < 1.82:
            k = rnd.randrange(len(shapes)); prof = shapes[k]
            m = bottle_mats[rnd.randrange(len(bottle_mats))]
            p.lathe((x, y + 0.009, 0.98 + rnd.uniform(-0.04, 0.04)), prof, m, seg=12)
            if rnd.random() < 0.7: p.lathe((x, y + 0.06, 0.98), [(prof[0][0] + 0.002, 0.0), (prof[0][0] + 0.002, 0.07), (0.0, 0.07)], "RS_Label", seg=12)
            x += prof[0][0] * 2 + rnd.uniform(0.03, 0.08)
    return p


# ================================================================== BANQUETA (origen en el piso; colision 0,42 x 0,83 m)
def stool():
    p = Part("RS_Stool")
    p.lathe((0, 0, 0), [(0.0, 0.0), (0.2, 0.0), (0.2, 0.02), (0.06, 0.05), (0.0, 0.05)], "RS_Black", seg=24)
    p.cyl((0, 0.04, 0), (0, 0.74, 0), 0.028, "RS_Chrome", seg=14)
    p.ring((0, 0.3, 0), 0.16, 0.012, "RS_Chrome")
    for a in range(4):
        ang = a * math.pi / 2 + math.pi / 4
        p.cyl((0, 0.3, 0), (0.155 * math.cos(ang), 0.3, 0.155 * math.sin(ang)), 0.008, "RS_Chrome", seg=6)
    p.lathe((0, 0.72, 0), [(0.0, 0.0), (0.12, 0.0), (0.19, 0.02), (0.0, 0.025)], "RS_Chrome", seg=24)
    p.lathe((0, 0.74, 0), [(0.0, 0.0), (0.195, 0.0), (0.205, 0.03), (0.2, 0.07), (0.17, 0.088), (0.0, 0.092)], "RS_Vinyl", seg=28)
    p.ring((0, 0.745, 0), 0.198, 0.008, "RS_Chrome", seg=28)
    return p


# ================================================================== MESA (origen en el piso; colision 1,40 x 0,82 x 0,90 m)
# El vaso senuelo apoya en el centro de la mesa (-4,2; 6,2): el centro queda libre.
def table():
    p = Part("RS_Table")
    W = "RS_TableWood"
    p.box((0, 0.8, 0), (1.4, 0.04, 0.9), W, 0.012)
    p.box((0, 0.765, 0), (1.36, 0.03, 0.86), "RS_BarTop", 0.006)                      # canto oscuro bajo la tapa
    for s in (-1, 1):
        p.box((s * 0.6, 0.7, 0), (0.04, 0.1, 0.7), W, 0.008)
        p.box((0, 0.7, s * 0.37), (1.16, 0.1, 0.04), W, 0.008)
    for sx in (-1, 1):
        for sz in (-1, 1):
            x, z = sx * 0.6, sz * 0.36
            p.lathe((x, 0, z), [(0.0, 0.0), (0.03, 0.0), (0.032, 0.04), (0.026, 0.08), (0.03, 0.35), (0.036, 0.5), (0.028, 0.62),
                                (0.034, 0.66), (0.034, 0.75), (0.0, 0.75)], W, seg=12)
    for s in (-1, 1): p.box((s * 0.6, 0.14, 0), (0.035, 0.035, 0.7), W, 0.006)            # travesanos bajos entre patas
    p.box((0, 0.14, 0), (1.17, 0.035, 0.035), W, 0.006)
    # servilletero y salero en un extremo
    p.box((0.5, 0.86, 0.22), (0.09, 0.08, 0.05), "RS_Chrome", 0.006)
    p.box((0.5, 0.865, 0.22), (0.075, 0.07, 0.035), "RS_Paper", 0.0)
    p.lathe((0.42, 0.82, 0.3), [(0.016, 0.0), (0.016, 0.06), (0.012, 0.075), (0.0, 0.08)], "RS_GlassClear", seg=10)
    return p


# ================================================================== TARIMA DE KARAOKE (origen: centro en el piso; 3,0 x 0,24 x 2,4 m)
def stage():
    p = Part("RS_Stage")
    p.box((0, 0.11, 0), (3.0, 0.22, 2.4), "RS_Black", 0.01)
    p.box((0, 0.23, 0), (3.0, 0.02, 2.4), "RS_Carpet", 0.006)
    p.box((0, 0.235, -1.205), (3.0, 0.035, 0.03), "RS_Steel", 0.005)                     # canto metalico al frente
    p.box((0, 0.06, -1.21), (2.96, 0.015, 0.012), "RS_LedMagenta", 0.0)                 # tira de luz
    for s in (-1, 1): p.box((s * 1.505, 0.06, 0), (0.012, 0.015, 2.36), "RS_LedMagenta", 0.0)
    # pie de microfono (donde estaba el decorativo): base, cana, jirafa y microfono
    mx, mz = 0.2, -0.2
    p.lathe((mx, 0.24, mz), [(0.0, 0.0), (0.14, 0.0), (0.14, 0.015), (0.03, 0.03), (0.0, 0.03)], "RS_Black", seg=20)
    p.cyl((mx, 0.25, mz), (mx, 1.45, mz), 0.012, "RS_Chrome", seg=10)
    p.cyl((mx, 1.45, mz), (mx + 0.05, 1.5, mz - 0.3), 0.008, "RS_Chrome", seg=8)
    p.lathe((mx + 0.05, 1.47, mz - 0.33), [(0.0, 0.0), (0.016, 0.0), (0.022, 0.1), (0.03, 0.12), (0.028, 0.16), (0.0, 0.17)], "RS_Black", seg=12)
    # parlantes en las esquinas del fondo
    for s in (-1, 1):
        x = s * 1.15
        p.box((x, 0.6, 0.92), (0.46, 0.72, 0.4), "RS_Speaker", 0.02)
        p.lathe((x, 0.5, 0.715), [(0.0, 0.0), (0.15, 0.0), (0.15, 0.012), (0.0, 0.012)], "RS_Grille", seg=20)
        p.lathe((x, 0.82, 0.715), [(0.0, 0.0), (0.06, 0.0), (0.06, 0.012), (0.0, 0.012)], "RS_Grille", seg=16)
    return p


# ================================================================== SILLA LIVIANA (origen: centro de la caja 0,45 x 0,60 x 0,45 m)
def chair():
    p = Part("RS_LightChair")
    W = "RS_TableWood"
    p.box((0, 0.275, 0), (0.45, 0.05, 0.45), W, 0.012)
    for sx in (-1, 1):
        for sz in (-1, 1): p.box((sx * 0.19, -0.03, sz * 0.19), (0.045, 0.56, 0.045), W, 0.008)
    for s in (-1, 1):
        p.box((s * 0.19, -0.18, 0), (0.025, 0.03, 0.36), W, 0.005)
        p.box((0, -0.18, s * 0.19), (0.36, 0.03, 0.025), W, 0.005)
    return p


# ================================================================== VASO SENUELO (origen: centro del cilindro d 0,09 x h 0,16 m)
def pint():
    p = Part("RS_Pint")
    p.lathe((0, -0.08, 0), [(0.0, 0.0), (0.036, 0.0), (0.038, 0.012), (0.0, 0.012)], "RS_GlassClear", seg=20)
    p.lathe((0, -0.068, 0), [(0.0, 0.0), (0.035, 0.0), (0.04, 0.12), (0.0, 0.12)], "RS_Beer", seg=20)
    p.lathe((0, 0.052, 0), [(0.0, 0.0), (0.041, 0.0), (0.043, 0.012), (0.035, 0.022), (0.0, 0.025)], "RS_Foam", seg=20)
    p.lathe((0, -0.08, 0), [(0.0, 0.0), (0.04, 0.0), (0.045, 0.16), (0.0, 0.16)], "RS_GlassClear", seg=20)
    return p


clear()
parts = {"bar_counter": counter(), "bar_stool": stool(), "bar_table": table(), "bar_stage": stage(), "bar_chair": chair(), "bar_pint": pint()}
objs = {n: write(p, COLORS) for n, p in parts.items()}
for n, o in objs.items(): export([o], os.path.join(OUT, n + ".fbx"))
os.makedirs(SRC, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC, "bar_s21.blend"))
print("[s21] bar exportado en", OUT)

if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"; sc.render.resolution_x = 1200; sc.render.resolution_y = 800
    sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "MATERIAL"; sc.display.shading.show_cavity = True
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    views = [("counter_front", U(1.2, 1.5, -3.2), U(0, 0.9, 0.2), "bar_counter", 26), ("counter_back", U(-1.0, 1.7, -1.6), U(0, 1.5, 0.95), "bar_counter", 24),
             ("stool", U(0.8, 1.0, -1.0), U(0, 0.45, 0), "bar_stool", 35), ("table", U(1.6, 1.5, -1.8), U(0, 0.5, 0), "bar_table", 30),
             ("stage", U(1.8, 1.6, -3.6), U(0, 0.6, 0), "bar_stage", 26), ("chair", U(0.7, 0.6, -0.9), U(0, 0, 0), "bar_chair", 35),
             ("pint", U(0.15, 0.08, -0.25), U(0, 0, 0), "bar_pint", 50)]
    for name, pos, tgt, show, lens in views:
        for n, o in objs.items(): o.hide_render = n != show
        cam.location = pos; cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler(); cam.data.lens = lens
        sc.render.filepath = os.path.join(PREVIEW, name + ".png"); bpy.ops.render.render(write_still=True)
    print("[s21] vistas en", PREVIEW)
