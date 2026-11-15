"""Apply supplied base-color textures to a COPY of the 3D kit.
Blender 4.3.2. This is art preparation, not Unity/gameplay implementation.
Run: blender --background --python-exit-code 1 --python source/apply_materials.py
Optional: -- --scene PATH_TO_ORIGINAL_BLEND --skip-renders
"""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT.parent/'uncle_extraction_kit_3D/source/Uncle_Extraction_Blockout.blend'
if '--scene' in sys.argv:SOURCE=Path(sys.argv[sys.argv.index('--scene')+1]).resolve()
assert SOURCE.is_file(),'Place alongside the 3D kit or supply --scene.'
(ROOT/'models').mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene;scene.frame_set(1)

specs=[
 {'id':'MAT_Wood_Boards','file':'01_wood_basecolor.png','tile_m':1.0,'roughness':.78,'wrap':'MIRROR','role':'tabletops and counter surfaces'},
 {'id':'MAT_Floor_Tiles','file':'02_floor_basecolor.png','tile_m':2.0,'roughness':.82,'wrap':'REPEAT','role':'interior bar floor only'},
 {'id':'MAT_Wall_Taupe','file':'03_wall_basecolor.png','tile_m':2.0,'roughness':.95,'wrap':'MIRROR','role':'bar walls'},
 {'id':'MAT_Shirt_Fabric','file':'04_fabric_basecolor.png','tile_m':.5,'roughness':.90,'wrap':'MIRROR','role':'burgundy shirt only'}]
mats={};images=[]
for s in specs:
 image=bpy.data.images.load(str(ROOT/'textures'/s['file']),check_existing=True);image.colorspace_settings.name='sRGB';images.append(image)
 mat=bpy.data.materials.new(s['id']);mat.use_nodes=True;nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=s['roughness'];bs.inputs['Metallic'].default_value=0
 tex=nodes.new('ShaderNodeTexImage');tex.name='BaseColor_Only';tex.image=image;tex.interpolation='Linear';tex.extension=s['wrap']
 uv=nodes.new('ShaderNodeUVMap');uv.uv_map='MaterialUV';uv.location=(-550,100);tex.location=(-330,100);bs.location=(0,100)
 mat.node_tree.links.new(uv.outputs['UV'],tex.inputs['Vector']);mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 mat['basecolor_file']='textures/'+s['file'];mat['physical_tile_m']=s['tile_m'];mat['unity_smoothness_proposed']=1-s['roughness'];mat['wrap_proposed']=s['wrap']
 mats[s['id']]=mat
by_id={s['id']:s for s in specs}

# Keep plain skin, eyes, sandals, vinyl and paint: not every face needs a texture.
for o in scene.objects:
 if o.type!='MESH':continue
 for slot in o.material_slots:
  if slot.material is None:continue
  old=slot.material.name
  if o.name=='Uncle_Prototype_Mesh' and old=='wine':slot.material=mats['MAT_Shirt_Fabric']
  elif o.name.startswith(('TableTop','CounterBody','CounterTop')) and old in ['wood','cream']:slot.material=mats['MAT_Wood_Boards']
  elif o.name=='InteriorFloor' and old=='floor':slot.material=mats['MAT_Floor_Tiles']
  elif old=='wall':slot.material=mats['MAT_Wall_Taupe']

roots=[('uncle_prototype','Uncle_Prototype_Rig'),('stool','Stool_Root'),('table','Table_Root'),('bar_counter','Counter_Root'),
('transport_cart','Cart_Root'),('exit_door','Door_Hinge_Pivot'),('passenger_van','Van_Root'),('bar_blockout_shell','Bar_Blockout_Root')]
uv_report=[];seen=set()
for asset_id,name in roots:
 root=bpy.data.objects[name];inv=root.matrix_world.inverted()
 for o in [root]+list(root.children_recursive):
  if o.type!='MESH' or o.data.as_pointer() in seen:continue
  seen.add(o.data.as_pointer());mesh=o.data;uv=mesh.uv_layers.get('MaterialUV') or mesh.uv_layers.new(name='MaterialUV')
  if len(mesh.uv_layers)>1:
   old=next((layer for layer in mesh.uv_layers if layer.name!='MaterialUV'),None)
   if old:
    for i in range(len(uv.data)):uv.data[i].uv=old.data[i].uv
  relative=inv@o.matrix_world;normal_matrix=relative.to_3x3().inverted().transposed();count=0
  for poly in mesh.polygons:
   mat=mesh.materials[poly.material_index] if poly.material_index<len(mesh.materials) else None
   if not mat or mat.name not in by_id:continue
   n=normal_matrix@poly.normal;axis=max(range(3),key=lambda i:abs(n[i]));tile=by_id[mat.name]['tile_m']
   for loop_index in poly.loop_indices:
    v=relative@mesh.vertices[mesh.loops[loop_index].vertex_index].co
    coords=(v.x,v.y) if axis==2 else (v.x,v.z) if axis==1 else (-v.y,v.z)
    uv.data[loop_index].uv=(coords[0]/tile,coords[1]/tile)
   count+=1
  # Unity's ordinary Lit shader samples UV0: keep the prepared map as channel zero.
  for other in list(mesh.uv_layers):
   if other.name!='MaterialUV':mesh.uv_layers.remove(other)
  uv=mesh.uv_layers.get('MaterialUV');mesh.uv_layers.active=uv;uv.active_render=True
  uv_report.append({'asset':asset_id,'mesh':o.name,'loops':len(uv.data),'textured_polygons':count,'method':'Root-local planar projection by dominant face; prototype UV, not hand-authored production unwrap'})

def select(objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 if objects:bpy.context.view_layer.objects.active=objects[0]
exports=[]
for asset_id,name in roots:
 root=bpy.data.objects[name];original=root.matrix_world.copy();root.location=(0,0,0);root.rotation_euler=(0,0,0);bpy.context.view_layer.update()
 objects=[root]+list(root.children_recursive);select(objects)
 path=ROOT/'models'/f'{asset_id}_material_variant.fbx'
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'EMPTY','MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',
   global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',add_leaf_bones=False,bake_anim=asset_id=='uncle_prototype',
   bake_anim_use_all_actions=asset_id=='uncle_prototype',bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='RELATIVE',embed_textures=False)
 used=sorted({slot.material.name for o in objects if o.type=='MESH' for slot in o.material_slots if slot.material and slot.material.name in by_id})
 exports.append({'asset':asset_id,'file':'models/'+path.name,'texture_materials':used,'purpose':'UV/material prototype variant; no gameplay components'})
 root.matrix_world=original;bpy.context.view_layer.update()

# A self-contained visual-review scene. FBX remains the route proposed for Unity.
rig=bpy.data.objects['Uncle_Prototype_Rig'];rig.animation_data.action=bpy.data.actions['Idle_Prototype'];scene.frame_set(1)
select([o for o in scene.objects if o.type in {'MESH','ARMATURE','EMPTY'}])
bpy.ops.export_scene.gltf(filepath=str(ROOT/'models/scene_material_review.glb'),export_format='GLB',use_selection=True,export_animations=False,export_skins=True,export_yup=True)
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=48;scene.cycles.use_denoising=False;scene.render.resolution_percentage=100
camera=scene.camera;camera.data.type='ORTHO';camera.data.ortho_scale=26;camera.location=(17,-21,20);camera.rotation_euler=(Vector((0,-1.8,0))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1600;scene.render.resolution_y=1200
target=ROOT/'source/Material_Lookdev.blend';bpy.ops.wm.save_as_mainfile(filepath=str(target));bpy.ops.file.make_paths_relative();bpy.ops.wm.save_as_mainfile(filepath=str(target))
metadata={'status':'LOOKDEV_NOT_UNITY_TESTED','blender_version':bpy.app.version_string,'source_scene':'sibling 3D kit or --scene argument','color_maps_only':True,
 'new_gameplay_logic':False,'normal_roughness_height_maps_delivered':False,'materials':specs,'uv':uv_report,'fbx_exports':exports,
 'glb_review_file':'models/scene_material_review.glb','source_blend':'source/Material_Lookdev.blend',
 'notes':['Four generated unedited base-color maps; source files are 1254x1254, not 2048.','Mirror is a shader wrapping choice, not proof that original edge pixels are periodic.','Floor uses Repeat to preserve checkerboard parity.','UV scale encodes meters per repeat; use material tiling 1,1 with these FBX variants.','Tiny wood legs remain flat color; avoid board seams painted across thin legs.']}
(ROOT/'data/material_application.json').write_text(json.dumps(metadata,indent=2,ensure_ascii=False)+'\n')
if '--skip-renders' in sys.argv:print('MATERIALS_APPLIED_SKIP_RENDER');sys.exit(0)

scene.render.filepath=str(ROOT/'previews/01_bar_materials.png');bpy.ops.render.render(write_still=True)
# Close character preview from actual weighted mesh.
for o in scene.objects:
 if o.type=='MESH':o.hide_render=o.name!='Uncle_Prototype_Mesh'
rig.location=(0,0,0);bpy.context.view_layer.update()
bpy.ops.mesh.primitive_plane_add(size=10,location=(0,0,-.025));ground=bpy.context.object;ground.name='PreviewOnlyGround';ground.data.materials.append(bpy.data.materials['chalk'])
camera.data.ortho_scale=2.85;camera.location=(3,-6,2.45);camera.rotation_euler=(Vector((0,0,.99))-camera.location).to_track_quat('-Z','Y').to_euler()
key=bpy.data.objects['Key'];key.data.energy=1200;key.location=(2,-5,7);fill=bpy.data.objects['Fill'];fill.data.energy=750;fill.location=(-4,1,5)
scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.filepath=str(ROOT/'previews/02_uncle_fabric.png');bpy.ops.render.render(write_still=True)

# Four planes, each showing 2x2 coordinate repetitions through the material shader.
for o in scene.objects:
 if o.type=='MESH':o.hide_render=True
for i,s in enumerate(specs):
 x=(-1 if i%2==0 else 1)*1.25;y=(1 if i<2 else -1)*1.25
 bpy.ops.mesh.primitive_plane_add(size=2.25,location=(x,y,0));o=bpy.context.object;o.name='PreviewOnlySwatch_'+s['id'];o.data.materials.append(mats[s['id']])
 uv=o.data.uv_layers.get('MaterialUV') or o.data.uv_layers.new(name='MaterialUV')
 for poly in o.data.polygons:
  for idx in poly.loop_indices:
   v=o.data.vertices[o.data.loops[idx].vertex_index].co;uv.data[idx].uv=(v.x/2.25*2+1,v.y/2.25*2+1)
 o.data.uv_layers.active=uv;uv.active_render=True
camera.data.ortho_scale=5.2;camera.location=(0,0,8);camera.rotation_euler=(0,0,0);camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
key.location=(0,0,7);key.data.energy=500;key.data.size=7;fill.data.energy=0
scene.render.resolution_x=1400;scene.render.resolution_y=1400;scene.render.filepath=str(ROOT/'previews/03_material_repeat_review.png');bpy.ops.render.render(write_still=True)
print('MATERIAL_LOOKDEV_COMPLETE')
