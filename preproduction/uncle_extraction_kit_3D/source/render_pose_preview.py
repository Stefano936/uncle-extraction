"""Render three actual prototype poses; this is not a gameplay capture."""
import bpy
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'source/Uncle_Extraction_Blockout.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['Uncle_Prototype_Rig'];mesh=bpy.data.objects['Uncle_Prototype_Mesh']
for o in scene.objects:
 if o.type=='MESH':o.hide_render=True
rig.location=(0,0,0)
for x,action,frame in [(-2.15,'Idle_Prototype',1),(0,'Push_Prototype',39),(2.15,'Kick_Prototype',45)]:
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame);bpy.context.view_layer.update()
 snapshot=bpy.data.meshes.new_from_object(mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()))
 o=bpy.data.objects.new('Review_'+action,snapshot);scene.collection.objects.link(o);o.location=(x,0,0)
 o.hide_render=False
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.025));plane=bpy.context.object;plane.data.materials.append(bpy.data.materials['chalk'])
cam=scene.camera;cam.data.type='ORTHO';cam.data.ortho_scale=7.2;cam.location=(4,-10,3.8);cam.rotation_euler=(Vector((0,0,.95))-cam.location).to_track_quat('-Z','Y').to_euler()
key=bpy.data.objects['Key'];key.location=(0,-4,7);key.data.energy=1000;key.data.size=7
fill=bpy.data.objects['Fill'];fill.location=(-3,2,5);fill.data.energy=700
scene.cycles.samples=48;scene.cycles.use_denoising=False;scene.render.resolution_x=1600;scene.render.resolution_y=800
scene.render.filepath=str(ROOT/'previews/03_actual_animation_poses.png');bpy.ops.render.render(write_still=True)
