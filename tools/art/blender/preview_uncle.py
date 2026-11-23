# Vista previa del tio S21 (Workbench, colores de material): frente, perfil, espalda y tres cuartos.
import bpy, sys, math
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
out = argv[0]
cols = {"skin": (0.86, 0.6, 0.47), "wine": (0.62, 0.1, 0.13), "cream": (0.85, 0.82, 0.72), "mustard": (0.8, 0.55, 0.15),
        "shoe": (0.2, 0.11, 0.06), "sock": (0.9, 0.9, 0.88), "belt": (0.15, 0.08, 0.04), "buckle": (0.85, 0.7, 0.3)}
for m in bpy.data.materials:
    if m.name in cols: m.diffuse_color = (*cols[m.name], 1)
sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"; sc.display.shading.color_type = "MATERIAL"
sc.display.shading.light = "STUDIO"; sc.display.shading.show_cavity = True; sc.display.shading.show_shadows = True
sc.render.resolution_x = 600; sc.render.resolution_y = 900
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.lens = 60
for name, ang in [("frente", 180), ("tres_cuartos", 220), ("perfil", 270), ("espalda", 0)]:
    a = math.radians(ang); target = Vector((0, 0, 1.0))
    cam.location = target + Vector((math.sin(a) * 4.2, math.cos(a) * 4.2, 0.25))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = f"{out}_{name}.png"; bpy.ops.render.render(write_still=True)
