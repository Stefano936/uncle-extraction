"""Render actual prototype geometry as UI-design backgrounds; not gameplay."""
import bpy
from pathlib import Path
from mathutils import Vector
import sys
ROOT=Path(__file__).resolve().parents[1]
scene_path=ROOT.parent/'uncle_extraction_kit_3D/source/Uncle_Extraction_Blockout.blend'
if '--scene' in sys.argv:scene_path=Path(sys.argv[sys.argv.index('--scene')+1]).resolve()
assert scene_path.is_file(), 'Keep the 3D kit next to this kit or pass --scene PATH.'
bpy.ops.wm.open_mainfile(filepath=str(scene_path));scene=bpy.context.scene
cam=scene.camera;cam.data.type='PERSP';cam.data.lens=25;cam.location=(-1,-.5,1.65);cam.rotation_euler=(Vector((-3,2.6,1.10))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=48;scene.cycles.use_denoising=False
scene.render.resolution_x=1920;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.29,.25,.22,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.50
key=bpy.data.objects['Key'];key.location=(0,1,5);key.data.energy=1100;key.data.color=(1,.70,.43);key.data.size=5
fill=bpy.data.objects['Fill'];fill.location=(-6,1,4);fill.data.energy=550;fill.data.color=(.45,.72,1);fill.data.size=4
rig=bpy.data.objects['Uncle_Prototype_Rig']
for name,action,frame in [('bar_idle','Idle_Prototype',1),('bar_windup','Push_Prototype',33)]:
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame);bpy.context.view_layer.update()
 scene.render.filepath=str(ROOT/'assets/backgrounds'/f'{name}.png');bpy.ops.render.render(write_still=True)
print('UI_BACKGROUNDS_RENDERED')
