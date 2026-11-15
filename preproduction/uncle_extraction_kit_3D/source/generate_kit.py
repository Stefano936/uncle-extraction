"""Procedural blockout assets for UNCLE EXTRACTION.
Blender 4.3.2: blender --background --python source/generate_kit.py
Original prototype geometry. No downloaded assets, textures, or third-party voices.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[1]
for sub in ['models','previews','data']: (ROOT/sub).mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for data in list(bpy.data.materials):bpy.data.materials.remove(data)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.fps=30;scene.frame_start=1;scene.frame_end=90

def material(name,rgb):
 m=bpy.data.materials.new(name);m.diffuse_color=(*rgb,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*rgb,1);bs.inputs['Roughness'].default_value=.84
 return m
MAT={k:material(k,v) for k,v in {
 'skin':(.68,.39,.25),'wine':(.43,.075,.12),'cream':(.82,.74,.56),'mustard':(.62,.32,.055),
 'hair':(.075,.035,.02),'shoe':(.16,.08,.035),'white':(.91,.9,.83),'pupil':(.035,.025,.02),
 'teal':(.055,.35,.34),'orange':(.85,.30,.055),'purple':(.29,.17,.45),'blue':(.08,.26,.48),
 'wood':(.30,.16,.08),'metal':(.14,.16,.17),'tire':(.025,.03,.033),'floor':(.36,.34,.29),
 'wall':(.26,.23,.22),'glass':(.11,.22,.24),'chalk':(.68,.68,.58),'dark':(.06,.07,.08)
 }.items()}

def empty(name,loc=(0,0,0),parent=None):
 o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.location=loc;o.empty_display_type='PLAIN_AXES';o.empty_display_size=.12
 if parent:o.parent=parent
 return o
def attach(o,parent=None,bone=None):
 if parent:o.parent=parent
 if bone:
  group=o.vertex_groups.new(name=bone);group.add(list(range(len(o.data.vertices))),1.0,'REPLACE')
 return o
def cube(name,loc,size,mat,parent=None,bone=None,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('simple_bevel','BEVEL');mod.width=bevel;mod.segments=1
  bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(MAT[mat]);return attach(o,parent,bone)
def sphere(name,loc,size,mat,parent=None,bone=None):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(MAT[mat]);return attach(o,parent,bone)
def cylinder(name,loc,radius,depth,mat,parent=None,bone=None,axis='Z'):
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=radius,depth=depth,location=loc);o=bpy.context.object;o.name=name
 if axis=='X':o.rotation_euler[1]=math.pi/2
 elif axis=='Y':o.rotation_euler[0]=math.pi/2
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);o.data.materials.append(MAT[mat]);return attach(o,parent,bone)
def segment(name,a,b,r,mat,bone):
 a,b=Vector(a),Vector(b);o=cylinder(name,(a+b)*.5,r,(b-a).length,mat,bone=bone)
 o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);return o
def descendants(root):return [root]+list(root.children_recursive)
def select_only(objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 if objects:bpy.context.view_layer.objects.active=objects[0]
def export_asset(name,root,animated=False):
 objects=descendants(root);select_only(objects);scene.frame_set(1)
 fbx=ROOT/'models'/f'{name}.fbx';glb=ROOT/'models'/f'{name}.glb'
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'EMPTY','MESH','ARMATURE'},
   axis_forward='-Z',axis_up='Y',global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
   add_leaf_bones=False,use_armature_deform_only=False,bake_anim=animated,bake_anim_use_all_actions=animated,
   bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,path_mode='AUTO')
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_animations=animated,
   export_animation_mode='ACTIONS',export_skins=animated,export_yup=True)
 tris=0
 for o in objects:
  if o.type=='MESH':o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
 corners=[o.matrix_world @ Vector(c) for o in objects if o.type=='MESH' for c in o.bound_box]
 bounds=[max(v[i] for v in corners)-min(v[i] for v in corners) for i in range(3)]
 return {'id':name,'fbx':f'models/{name}.fbx','glb':f'models/{name}.glb','triangles':tris,'objects':len(objects),'bounds_blender_xyz_m':bounds,
         'animated':animated,'status':'PROTOTYPE_NOT_UNITY_TESTED'}

assets=[]
# NPC: a rigid-segment skinned mesh and deliberately simple generic rig.
parts=[]
parts += [sphere('belly',(0,-.055,1.05),(.41,.30,.31),'cream',bone='chest'),
 cube('shirt_back',(0,.125,1.24),(.82,.28,.45),'wine',bone='chest',bevel=.055),
 cube('shirt_left',(.30,-.155,1.26),(.23,.16,.48),'wine',bone='chest',bevel=.035),
 cube('shirt_right',(-.30,-.155,1.26),(.23,.16,.48),'wine',bone='chest',bevel=.035),
 cube('shorts_waist',(0,0,.85),(.63,.40,.24),'mustard',bone='pelvis',bevel=.035),
 cylinder('neck',(0,0,1.48),.105,.18,'skin',bone='head'),
 sphere('head',(0,-.01,1.68),(.24,.225,.255),'skin',bone='head'),
 sphere('nose',(0,-.265,1.65),(.085,.09,.065),'skin',bone='head')]
for side,sign in [('L',1),('R',-1)]:
 arm=f'upper_arm.{side}';fore=f'forearm.{side}';thigh=f'thigh.{side}';calf=f'calf.{side}'
 shoulder=(sign*.37,0,1.39);elbow=(sign*.61,-.005,1.13);wrist=(sign*.77,-.025,.89)
 hip=(sign*.17,0,.85);knee=(sign*.18,0,.47);ankle=(sign*.18,0,.12)
 parts += [segment('sleeve_'+side,shoulder,(sign*.48,0,1.27),.135,'wine',arm),
 segment('upper_arm_'+side,(sign*.45,0,1.30),elbow,.105,'skin',arm),
 sphere('elbow_'+side,elbow,(.105,.10,.10),'skin',bone=fore),
 segment('forearm_'+side,elbow,wrist,.09,'skin',fore),
 cube('hand_'+side,(sign*.79,-.035,.83),(.13,.13,.18),'skin',bone=fore,bevel=.025),
 segment('short_leg_'+side,hip,(sign*.18,0,.64),.17,'mustard',thigh),
 segment('thigh_'+side,(sign*.18,0,.67),knee,.125,'skin',thigh),
 sphere('knee_'+side,knee,(.12,.115,.115),'skin',bone=calf),
 segment('calf_'+side,knee,ankle,.09,'skin',calf),
 cube('sandal_'+side,(sign*.18,-.055,.045),(.23,.39,.085),'shoe',bone=calf,bevel=.018),
 cube('foot_'+side,(sign*.18,-.085,.11),(.17,.28,.11),'skin',bone=calf,bevel=.025),
 cube('strap_'+side,(sign*.18,-.12,.168),(.205,.08,.025),'shoe',bone=calf),
 sphere('ear_'+side,(sign*.235,0,1.67),(.055,.065,.09),'skin',bone='head'),
 sphere('eye_'+side,(sign*.09,-.217,1.72),(.058,.033,.042),'white',bone='head'),
 sphere('pupil_'+side,(sign*.085,-.248,1.72),(.018,.013,.023),'pupil',bone='head'),
 cube('eyebrow_'+side,(sign*.09,-.234,1.78),(.15,.035,.033),'hair',bone='head',bevel=.008),
 sphere('moustache_'+side,(sign*.09,-.245,1.60),(.135,.065,.05),'hair',bone='head'),
 sphere('side_hair_'+side,(sign*.19,.015,1.80),(.09,.18,.11),'hair',bone='head')]
parts.append(cube('mouth',(0,-.218,1.55),(.115,.045,.023),'hair',bone='head'))
select_only(parts);bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();mesh=parts[0];mesh.name='Uncle_Prototype_Mesh'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
arm_data=bpy.data.armatures.new('Uncle_Prototype_Rig');rig=bpy.data.objects.new('Uncle_Prototype_Rig',arm_data);scene.collection.objects.link(rig)
select_only([rig]);bpy.ops.object.mode_set(mode='EDIT')
defs=[('root',(0,0,0),(0,0,.15),None),('pelvis',(0,0,.85),(0,0,1.0),'root'),
('chest',(0,0,1.0),(0,0,1.40),'pelvis'),('head',(0,0,1.40),(0,0,1.88),'chest')]
for side,s in [('L',1),('R',-1)]:
 defs += [(f'upper_arm.{side}',(s*.37,0,1.39),(s*.61,-.005,1.13),'chest'),
 (f'forearm.{side}',(s*.61,-.005,1.13),(s*.77,-.025,.89),f'upper_arm.{side}'),
 (f'thigh.{side}',(s*.17,0,.85),(s*.18,0,.47),'pelvis'),
 (f'calf.{side}',(s*.18,0,.47),(s*.18,0,.12),f'thigh.{side}')]
for name,head,tail,parent in defs:
 b=arm_data.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=arm_data.edit_bones[parent]
 if name=='root':b.use_deform=False
bpy.ops.object.mode_set(mode='OBJECT');mesh.parent=rig;mod=mesh.modifiers.new('Prototype_Armature','ARMATURE');mod.object=rig;rig.show_in_front=True
socket_specs=[('Grab_Shoulder_L','chest',(.33,-.04,1.39)),('Grab_Shoulder_R','chest',(-.33,-.04,1.39)),
('Grab_Forearm_L','forearm.L',(.71,-.025,1.0)),('Grab_Forearm_R','forearm.R',(-.71,-.025,1.0)),
('Grab_Ankle_L','calf.L',(.18,0,.17)),('Grab_Ankle_R','calf.R',(-.18,0,.17)),
('Grab_Belt','pelvis',(0,.15,.91)),('Grab_Belt_Alt','pelvis',(0,-.19,.91))]
for name,bone,pos in socket_specs:
 o=empty(name);o.parent=rig;o.parent_type='BONE';o.parent_bone=bone;bpy.context.view_layer.update();o.matrix_world=Matrix.Translation(pos);bpy.context.view_layer.update();o['socket_role']='player_grab';o['source_bone']=bone

def pose_key(action,frame,targets=None,bob=0):
 rig.animation_data_create();rig.animation_data.action=action
 targets=targets or {}
 for name,head,tail,parent in defs:
  pb=rig.pose.bones[name];pb.rotation_mode='QUATERNION';rest=rig.data.bones[name]
  if name=='root':pb.location=(0,0,bob);pb.rotation_quaternion=(1,0,0,0)
  else:
   rest_dir=Vector(tail)-Vector(head);direction=Vector(targets.get(name,rest_dir))
   delta=rest_dir.rotation_difference(direction)
   rot=delta.to_matrix() @ rest.matrix_local.to_3x3()
   if parent:
    parent_pose=rig.pose.bones[parent].matrix;parent_rest=rig.data.bones[parent].matrix_local
    location=(parent_pose @ parent_rest.inverted()) @ Vector(head)
   else:location=Vector(head)
   pb.matrix=Matrix.Translation(location) @ rot.to_4x4()
  pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=name)
  pb.keyframe_insert(data_path='location',frame=frame,group=name)
  bpy.context.view_layer.update()

idle=bpy.data.actions.new('Idle_Prototype')
for f,bob in [(1,0),(30,.018),(60,0)]:pose_key(idle,f,bob=bob)
push=bpy.data.actions.new('Push_Prototype')
wind={};active={}
for side,s in [('L',1),('R',-1)]:
 wind[f'upper_arm.{side}']=(s*.23,.10,-.14);wind[f'forearm.{side}']=(s*.03,-.23,.13)
 active[f'upper_arm.{side}']=(s*.06,-.31,-.045);active[f'forearm.{side}']=(s*.01,-.285,0)
for f,p in [(1,{}),(25,wind),(33,wind),(39,active),(46,active),(84,{})]:pose_key(push,f,p)
kick=bpy.data.actions.new('Kick_Prototype')
for f,p in [(1,{}),(29,{'thigh.L':(.01,-.22,-.13),'calf.L':(.01,.22,-.15)}),
 (36,{'thigh.L':(.01,-.24,-.1),'calf.L':(.01,.2,-.13)}),
 (42,{'thigh.L':(.01,-.30,-.12),'calf.L':(.01,-.32,-.10)}),(48,{'thigh.L':(.01,-.30,-.12),'calf.L':(.01,-.32,-.10)}),(90,{})]:pose_key(kick,f,p)
for action in [idle,push,kick]:
 action.use_fake_user=True
 for curve in action.fcurves:
  for key in curve.keyframe_points:key.interpolation='LINEAR'
rig.animation_data.action=idle;scene.frame_set(1);bpy.context.view_layer.update()
assets.append(export_asset('uncle_prototype',rig,True))

# Simple functional props. Meshes keep separate named parts where hinges matter.
stool=empty('Stool_Root');cylinder('Seat',(0,0,.77),.27,.12,'wine',stool)
for x in [-.17,.17]:
 for y in [-.17,.17]:cube('Leg',(x,y,.36),(.065,.065,.72),'wood',stool)
cylinder('Foot_Ring',(0,0,.27),.21,.035,'metal',stool)
assets.append(export_asset('stool',stool))
table_root=empty('Table_Root');cube('TableTop',(0,0,.77),(1.4,.9,.10),'wood',table_root,bevel=.025)
for x in [-.52,.52]:
 for y in [-.3,.3]:cube('TableLeg',(x,y,.36),(.09,.09,.72),'metal',table_root)
assets.append(export_asset('table',table_root))
counter=empty('Counter_Root');cube('CounterBody',(0,0,.48),(4.0,.75,.96),'wood',counter,bevel=.015);cube('CounterTop',(0,0,1.02),(4.12,.9,.12),'cream',counter,bevel=.025)
for x,s in [(-1.1,'R'),(1.1,'L')]:empty('Npc_Anchor_'+s,(x,-.47,1.02),counter)
assets.append(export_asset('bar_counter',counter))
cart=empty('Cart_Root');cube('CartDeck',(0,0,.30),(1.2,.8,.12),'teal',cart,bevel=.02)
for x in [-.46,.46]:
 for y in [-.29,.29]:cylinder('Wheel',(x,y,.15),.14,.09,'tire',cart,axis='X')
for x in [-.5,.5]:cylinder('HandleUpright',(x,.38,.62),.035,.6,'metal',cart)
cylinder('HandleBar',(0,.38,.92),.04,1.05,'metal',cart,axis='X')
empty('Player_Handle',(0,.41,.91),cart);empty('Npc_LoadPoint',(0,0,.49),cart)
assets.append(export_asset('transport_cart',cart))
door=empty('Door_Hinge_Pivot');cube('DoorPanel',(.75,0,1.1),(1.5,.075,2.2),'teal',door,bevel=.015);sphere('DoorKnob',(1.32,-.075,1.04),(.065,.055,.065),'metal',door);empty('Interact_Handle',(1.32,-.09,1.04),door)
assets.append(export_asset('exit_door',door))
van=empty('Van_Root');cube('Chassis',(0,0,.50),(2.20,4.4,.25),'metal',van,bevel=.08)
cube('Cabin',(0,1.18,1.2),(2.16,1.65,1.20),'teal',van,bevel=.11);cube('Windshield',(0,2.025,1.55),(1.85,.035,.60),'glass',van,bevel=.03)
cube('Roof',(0,-.1,2.28),(2.20,4.15,.16),'teal',van,bevel=.04)
for x in [-1.04,1.04]:
 cube('RearSide',(x,-.84,1.45),(.12,2.55,1.56),'teal',van)
 for y in [-1.40,1.40]:cylinder('Wheel',(x*1.06,y,.42),.39,.20,'tire',van,axis='X')
cube('RearFloor',(0,-.79,.64),(1.94,2.65,.10),'floor',van)
cube('PassengerBench',(0,.21,.92),(1.75,.55,.40),'wine',van,bevel=.035)
for s in [-1,1]:
 hinge=empty('RearDoor_Hinge_'+('L' if s<0 else 'R'),(s*1.04,-2.15,.66),van)
 panel=cube('RearDoorPanel',(-s*.52,0,.75),(1.04,.08,1.5),'teal',hinge,bevel=.015)
 hinge.rotation_euler[2]=math.radians(100*s)
vol=empty('Extraction_Volume',(0,-.80,1.40),van);vol.empty_display_type='CUBE';vol.empty_display_size=1;vol.scale=(.85,1.1,.75)
assets.append(export_asset('passenger_van',van))

# Blockout shell, with a real 1.5 m opening and 14 x 5 m sidewalk.
env=empty('Bar_Blockout_Root');cube('InteriorFloor',(0,0,-.12),(18,12,.24),'floor',env)
cube('BackWall',(0,6,1.2),(18,.2,2.4),'wall',env)
for x in [-9,9]:cube('SideWall',(x,0,1.2),(.2,12,2.4),'wall',env)
# South/front wall is intentionally 0.6 m high for inspection; full runtime walls are a task.
cube('FrontWall_Left',(-2.375,-6,.30),(13.25,.2,.6),'wall',env)
cube('FrontWall_Right',(7.375,-6,.30),(3.25,.2,.6),'wall',env)
cube('Sidewalk',(0,-8.5,-.10),(14,5,.20),'chalk',env)
empty('Exit_Clearance_Center',(5,-6,1.1),env);empty('Npc_Start',(-3,2.6,0),env);empty('Player_Start',(4,-4,0),env)
assets.append(export_asset('bar_blockout_shell',env))

# Assembly for review, not physics. Props are reusable instances of exported roots.
rig.location=(-3,2.6,0);counter.location=(-3,4.4,0);stool.location=(-4,3,0)
table_root.location=(-3,-.5,0);cart.location=(-7,-4,0);door.location=(4.25,-6,0);door.rotation_euler[2]=math.radians(95)
van.location=(0,-8.7,0);van.rotation_euler[2]=math.pi
def instance_root(root,name,loc):
 objects=descendants(root);copies={o:o.copy() for o in objects}
 for original,copy in copies.items():
  if original.data:copy.data=original.data
  scene.collection.objects.link(copy)
  if original.parent in copies:copy.parent=copies[original.parent]
  else:copy.parent=None
 result=copies[root];result.name=name;result.location=loc;return result
instance_root(table_root,'Table_B',(1.0,-.5,0));instance_root(table_root,'Table_C',(-3,-3.4,0));instance_root(table_root,'Table_D',(1,-3.4,0))
for i,loc in enumerate([(-2,2.9,0),(-4,1.9,0),(1,-1.5,0),(-3,-2.3,0)]):instance_root(stool,'Stool_'+str(i),loc)

world=scene.world or bpy.data.worlds.new('World');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.77,.79,.81,1);world.node_tree.nodes['Background'].inputs[1].default_value=.7
def area(name,loc,power,size):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler();return o
key=area('Key',(2,-5,7),1200,6);fill=area('Fill',(-4,1,5),750,5)
cam_data=bpy.data.cameras.new('Review_Camera');cam=bpy.data.objects.new('Review_Camera',cam_data);scene.collection.objects.link(cam);scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=48;scene.cycles.use_denoising=False;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'

# Save an editable assembly and metadata before any review-only visibility changes.
rig.animation_data.action=idle;scene.frame_set(1)
cam_data.type='ORTHO';cam_data.ortho_scale=26;cam.location=(17,-21,20);cam.rotation_euler=(Vector((0,-1.8,0))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1600;scene.render.resolution_y=1200
key.data.energy=2400;key.location=(2,-4,12);fill.data.energy=1600;fill.location=(-5,4,12)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'source/Uncle_Extraction_Blockout.blend'))
socket_data=[]
for name,bone,pos in socket_specs:socket_data.append({'id':name,'bone':bone,'position_blender_rest_m':list(pos)})
meta={'version':'0.1','status':'PROTOTYPE_ASSETS_NOT_A_GAME','generator_blender':bpy.app.version_string,
 'source_units':'meters','source_up':'Z','source_character_front':'-Y','fbx_forward':'-Z','fbx_up':'Y','export_scale':1,
 'unrealistic_or_simplified':['rigid segment weights; not production skinning','3 rough animation actions; not final gameplay timing','front blockout wall lowered for inspection','no physics bodies, collisions, scripts, UI or networking included'],
 'assets':assets,'npc':{'bone_count':len(rig.data.bones),'deform_bones':len(rig.data.bones)-1,'clips':['Idle_Prototype','Push_Prototype','Kick_Prototype'],'grab_sockets':socket_data},
 'placement_blender_m':{'npc':[-3,2.6,0],'counter':[-3,4.4,0],'cart':[-7,-4,0],'door_hinge':[4.25,-6,0],'van':[0,-8.7,0]},
 'runtime_setup_required':['Unity import and material extraction','Generic animation rig import and clip review','Rigidbody and 11-body ragdoll configuration','primitive colliders and bounded joints','NPC states, attacks, hand authority and input','connection and Steam integration']}
(ROOT/'data/asset_metadata.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2))
if '--skip-renders' in sys.argv:
 print('KIT_GENERATED_SKIP_RENDERS');sys.exit(0)

# Actual Blender renders of the delivered geometry, not generated concept art or gameplay.
all_meshes=[o for o in scene.objects if o.type=='MESH'];npc_set=set(descendants(rig))
for o in all_meshes:o.hide_render=o not in npc_set
rig.location=(0,0,0);bpy.context.view_layer.update()
ground=cube('Review_Only_Ground',(0,0,-.045),(8,8,.08),'chalk');ground.hide_render=False
key.data.energy=1200;key.location=(2,-5,7);fill.data.energy=750;fill.location=(-4,1,5)
cam_data.type='ORTHO';cam_data.ortho_scale=2.85;cam.location=(3,-6,2.45);cam.rotation_euler=(Vector((0,0,.99))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.filepath=str(ROOT/'previews/01_uncle_actual_mesh.png');bpy.ops.render.render(write_still=True)
for o in all_meshes:o.hide_render=False
ground.hide_render=True;rig.location=(-3,2.6,0);bpy.context.view_layer.update()
cam_data.ortho_scale=26;cam.location=(17,-21,20);cam.rotation_euler=(Vector((0,-1.8,0))-cam.location).to_track_quat('-Z','Y').to_euler();key.data.energy=2400;key.location=(2,-4,12);fill.data.energy=1600;fill.location=(-5,4,12)
scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.filepath=str(ROOT/'previews/02_bar_actual_assembly.png');bpy.ops.render.render(write_still=True)
print('KIT_GENERATED',json.dumps({'assets':len(assets),'bones':len(rig.data.bones),'clips':3,'root':str(ROOT)}))
