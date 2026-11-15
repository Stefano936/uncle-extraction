"""Catalogs for materials and prop interaction. No game components are created."""
from pathlib import Path
from PIL import Image
import json,csv,hashlib,numpy as np
ROOT=Path(__file__).resolve().parents[1]
def write(path,obj): (ROOT/path).write_text(json.dumps(obj,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
app=json.loads((ROOT/'data/material_application.json').read_text())
textures=[]
for mat in app['materials']:
 p=ROOT/'textures'/mat['file'];image=Image.open(p);a=np.asarray(image,dtype=np.float32)
 textures.append({'id':mat['id'],'file':'textures/'+mat['file'],'actual_source_pixels':list(image.size),'mode':image.mode,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),
  'map_type':'Base Color only','source_modified_after_generation':False,'color_space':'sRGB','alpha_needed':False,'mipmaps_proposed':True,
  'filter_proposed':'Trilinear','max_size_proposed':2048,'npot_scale_proposed':'ToNearest; inspect actual imported resolution','cpu_read_write_proposed':False,
  'wrap_proposed':mat['wrap'].title(),'metallic':0.0,'smoothness_proposed':round(1-mat['roughness'],3),'unity_tiling_for_supplied_fbx':[1,1],
  'physical_tile_m_encoded_in_uv0':mat['tile_m'],'normal_map':None,'height_map':None,'roughness_map':None,
  'raw_edge_mean_absolute_rgb_difference_0_255':{'left_right':round(float(np.abs(a[:,0,:]-a[:,-1,:]).mean()),3),'top_bottom':round(float(np.abs(a[0,:,:]-a[-1,:,:]).mean()),3)},
  'seam_note':'A raw edge statistic is diagnostic only. Mirror wrapping was selected for wood/wall/fabric; floor uses Repeat. No claim of exact periodic source pixels.'})
write('data/texture_import.json',{'status':'ART_PROTOTYPE_NOT_UNITY_IMPORTED','materials':textures,'shader_proposed':'Universal Render Pipeline/Lit','workflow':'Metallic','surface_type':'Opaque','base_color_multiplier':[1,1,1,1],
 'maps_not_delivered':['normal','height','roughness','metallic','ambient_occlusion'],'notes':['Do not import RGB color as a normal map.','No fabricated .mat or .meta files are included. Create them in the installed Unity version.','Material smoothness is a scalar; it is not stored in the alpha of these opaque RGB files.']})

objects=[]
def prop(id,asset,role,status='MODEL_DELIVERED',**kw):
 objects.append(dict(id=id,asset=asset,role=role,asset_status=status,runtime_components_delivered=False,**kw))
prop('PROP_NPC','uncle_prototype','extraction_subject',mobility='host-driven physical body',player_grab=True,npc_anchor_target=False,proposed_mass_kg=110,
 sockets=['Grab_Shoulder_L','Grab_Shoulder_R','Grab_Forearm_L','Grab_Forearm_R','Grab_Ankle_L','Grab_Ankle_R','Grab_Belt','Grab_Belt_Alt'],optional_sockets=['Grab_Belt_Alt'],
 rule='Mass stays 110kg across party sizes. Authority, attacks and recovery follow original balance. Skin texture does not define contact hitboxes.')
prop('PROP_STOOL','stool','movable_obstacle',mobility='dynamic',player_grab=True,npc_anchor_target=True,proposed_mass_kg=6,existing_sockets=[],
 required_new_sockets=['Player_Seat_Grip','Npc_Seat_Anchor'],npc_anchor_initial_status='requires socket and reach validation',
 rule='Can be moved out of route. NPC holds a valid reachable seat edge. Never turn the stool into unlimited damage or a projectile weapon.')
prop('PROP_TABLE','table','route_obstacle',mobility='static initial version',player_grab=False,npc_anchor_target=False,proposed_mass_kg=None,
 rule='Fixed in initial sample. Do not infer movable from wood texture. A later movable table is a separate balance and physics change.')
prop('PROP_COUNTER','bar_counter','fixed_npc_anchor',mobility='static',player_grab=False,npc_anchor_target=True,proposed_mass_kg=None,existing_sockets=['Npc_Anchor_L','Npc_Anchor_R'],
 rule='One active anchor in initial sample. Player releases NPC hand with the GDD hold action or uses a valid distraction. Counter cannot be pulled out of the world.')
prop('PROP_CART','transport_cart','solo_and_team_transport',mobility='dynamic root; visual wheels first',player_grab=True,npc_anchor_target=False,proposed_mass_kg=18,
 existing_sockets=['Player_Handle','Npc_LoadPoint'],rule='Mass 18kg is an untested starting value. Solo can push with one hand and use the other. Wheels may be decorative on a single stabilized body. NPC retention and escape require implementation.')
prop('PROP_DOOR','exit_door','hinged_route_gate',mobility='hinged',player_grab=False,npc_anchor_target=False,proposed_mass_kg=12,
 existing_sockets=['Interact_Handle'],hinge_design_degrees=[0,105],clear_width_design_m=1.5,
 rule='Interact to open/close; root is hinge origin. Mass/limits are untested. Opening direction is consistent. Complete collider testing; no crushing damage from door animation.')
prop('PROP_VAN','passenger_van','stationary_extraction',mobility='static',player_grab=False,npc_anchor_target=False,proposed_mass_kg=None,
 existing_sockets=['Extraction_Volume','RearDoor_Hinge_L','RearDoor_Hinge_R'],nominal_extraction_box_m=[1.7,2.2,1.5],
 rule='No driving. Validate torso/pelvis/shoulders, clear solids and closing action using GDD. The marker itself is not a trigger; open doors enlarge the mesh bounding box.')
prop('PROP_WORLD','bar_blockout_shell','static_world',mobility='static',player_grab=False,npc_anchor_target=False,proposed_mass_kg=None,
 existing_sockets=['Exit_Clearance_Center','Npc_Start','Player_Start'],
 rule='Tiles and grime are cosmetic. Friction and walkability are configured independently. Raise/complete the lowered inspection wall before gameplay release.')
prop('PROP_LURE_CUP','lure_cup','distraction',status='MODEL_PENDING',mobility='dynamic if implemented',player_grab=True,npc_anchor_target=False,proposed_mass_kg=.2,
 rule='Use original 2.5s distraction and 12s type cooldown. No drug dosing or sleep effect. Model and relevant sockets still need production.')
prop('PROP_KARAOKE','karaoke_station','optional_distraction',status='MODEL_PENDING_P1',mobility='static',player_grab=False,npc_anchor_target=False,proposed_mass_kg=None,
 rule='Optional content after core. Not supplied as a model or a working audio interaction.')
prop('PROP_SNACKS','snack_box','later_profile_lure',status='MODEL_PENDING_P1',mobility='dynamic if implemented',player_grab=True,npc_anchor_target=False,proposed_mass_kg=.3,
 rule='Only when the later Philosopher profile is implemented. This catalog does not make the variant available.')
write('data/interaction_manifest.json',{'status':'DESIGN_RULES_NOT_RUNTIME_COMPONENTS','physics_values':'Initial hypotheses; only NPC mass and existing GDD values are fixed design inputs','objects':objects,
 'global_rules':['Renderer material does not set Rigidbody mass, collider shape, friction, anchor ability or damage.','Host validates grabs, reach and release; existing game_balance.json remains authoritative.','Available sockets in a model are markers; bind them to runtime actions explicitly.','Do not set every prop dynamic; first sample has a capped active-physics budget.']})

rules=[
('PLAYER_GRAB','Thin cream contour plus hand icon on focus','Only a valid reachable socket','Show action binding and object/limb label','Never outline all decoration permanently'),
('NPC_ANCHOR','Show NPC hand and tension; local icon if needed','When NPC actually anchors','Release the hand or distract','An anchor label does not make an object movable'),
('WORLD_STATIC','No grab marker','Always static geometry','No action prompt','Material color is not a gameplay permission'),
('DISTRACTION','Small contextual interest indicator','When lure is available and visible','Offer/activate according to profile','Cooldown and repeated-use rules still apply'),
('EXTRACTION','Door/van outline and hold feedback','Valid pose and clear close volume','Load; align; close','Do not show success before authoritative validation')]
with (ROOT/'data/visual_interaction_rules.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.writer(f);w.writerow(['class','visual','condition','player_message','constraint']);w.writerows(rules)

cases=[('Source maps','Four real RGB maps and dimensions','Correct files; no imaginary PBR channels'),('UV0','Import all eight FBX variants','Primary UV layer preserved; geometry scale unchanged'),
('Bindings','Resolve exported image references','Texture files found in sibling textures folder'),('Shader','Use URP Lit opaque','White tint; correct color space; scalar smoothness'),
('Wrap','Inspect floor Repeat and others Mirror','Checker parity holds; no obvious seams at gameplay scale'),('Scale','Check table and floor repetition','UV encoded scale used once; material tiling 1,1'),
('Readability','Stand NPC against textured room','Hands; face and windup still legible'),('Fabric','View moving shirt at multiple distances','No distracting weave shimmer after mip/compression setup'),
('Physics','Change only renderer material','Mass; friction and collider remain unchanged'),('Stool','Add and validate new grip sockets','No floating grabs beyond arm reach'),
('Table','Try to grab initial table','No false movable prompt'),('Counter','NPC grabs fixed anchor','Only allowed anchor; hand release works'),
('Cart','Solo transport with new appearance','Same handling; one free hand available'),('Door','Test hinge and frame','No clipping or crushing by scripted animation'),
('Van','Validate load and closing','Marker bound to real trigger; not early victory'),('World','Complete inspection wall','No escape through low preview wall'),
('Exports','Reimport textured NPC','12 bones; weights; three clips; UVs and shirt image persist'),('Package','Open blend on another path','Relative textures resolve; no machine-specific image paths'),
('Pending props','Inspect catalogue availability','Cup/karaoke/snacks not declared delivered or playable'),('Runtime','Profile textures and memory','Measure actual imported dimensions and compression; no invented result')]
with (ROOT/'data/materials_qa.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.writer(f);w.writerow(['id','area','steps','expected','status','build','evidence'])
 for i,(a,b,c) in enumerate(cases,1):w.writerow([f'MAT_QA_{i:03}',a,b,c,'NOT_RUN','',''])
print(json.dumps({'textures':len(textures),'object_cards':len(objects),'material_qa_cases':len(cases)}))
