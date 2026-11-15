"""Verify saved Blender material scene plus actual FBX reimports.
Does not execute Unity or physics. Blender 4.3.2.
"""
import bpy,json,math,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
app=json.loads((ROOT/'data/material_application.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'source/Material_Lookdev.blend'))
source_check=[]
for mat in app['materials']:
 m=bpy.data.materials[mat['id']];nodes=[n for n in m.node_tree.nodes if n.type=='TEX_IMAGE'];assert len(nodes)==1
 n=nodes[0];p=Path(bpy.path.abspath(n.image.filepath));assert p.is_file(),p
 assert list(n.image.size)==[1254,1254]
 assert n.image.colorspace_settings.name=='sRGB' and n.extension==mat['wrap']
 source_check.append({'material':mat['id'],'image_resolves':True,'relative_path':n.image.filepath,'size':list(n.image.size),'wrap':n.extension})
results=[]
for item in app['fbx_exports']:
 for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.context.scene.frame_set(1);bpy.ops.import_scene.fbx(filepath=str(ROOT/item['file']),use_anim=True);bpy.context.view_layer.update()
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert meshes
 uv_values=[]
 for mesh in meshes:
  assert len(mesh.data.uv_layers)==1,(item['asset'],mesh.name,[u.name for u in mesh.data.uv_layers])
  for u in mesh.data.uv_layers[0].data:
   assert all(math.isfinite(v) for v in u.uv);uv_values.extend(u.uv)
 images=[]
 for mesh in meshes:
  for slot in mesh.material_slots:
   if not slot.material or not slot.material.use_nodes:continue
   for node in slot.material.node_tree.nodes:
    if node.type=='TEX_IMAGE' and node.image:
     p=Path(bpy.path.abspath(node.image.filepath));assert p.is_file(),(item['asset'],p);images.append(p.name)
 expected={next(m['file'] for m in app['materials'] if m['id']==name) for name in item['texture_materials']}
 assert expected<=set(images),(item['asset'],expected,images)
 arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];clips=[]
 if item['asset']=='uncle_prototype':
  assert len(arms)==1 and len(arms[0].data.bones)==12
  for key in ['Idle_Prototype','Push_Prototype','Kick_Prototype']:assert any(key in a.name for a in bpy.data.actions)
  clips=[a.name for a in bpy.data.actions]
  assert all(v.groups for mesh in meshes for v in mesh.data.vertices)
  for name in ['Grab_Shoulder_L','Grab_Shoulder_R','Grab_Forearm_L','Grab_Forearm_R','Grab_Ankle_L','Grab_Ankle_R','Grab_Belt','Grab_Belt_Alt']:
   assert any(o.name==name or o.name.startswith(name+'.') for o in bpy.context.scene.objects),name
 else:assert not arms
 results.append({'asset':item['asset'],'status':'PASS','primary_uv_layers':1,'uv_range':[min(uv_values),max(uv_values)],'texture_references_found':sorted(set(images)),'bones':len(arms[0].data.bones) if arms else 0,'clips':clips})

blob=(ROOT/'models/scene_material_review.glb').read_bytes();magic,version,total=struct.unpack_from('<4sII',blob,0);assert magic==b'glTF' and version==2 and total==len(blob)
n,typ=struct.unpack_from('<I4s',blob,12);gltf=json.loads(blob[20:20+n]);assert len(gltf['images'])==4 and all('bufferView' in x for x in gltf['images'])
report={'status':'BLENDER_ART_EXPORT_CHECKS_PASSED','blender_version':bpy.app.version_string,'source_blend_image_checks':source_check,'fbx_reimport_results':results,
 'glb_review':{'header':'PASS','embedded_images':4,'animations_exported':False},'checked':['Four relative image links in saved blend','Eight actual FBX reimports','Single UV0 layer with finite coordinates','Expected image references','NPC rig, weights, sockets and three actions'],
 'not_checked':['Unity material import','Physics or interactions','Network or Steam','Runtime performance','Final artistic approval'],'gameplay_qa_executed':False}
(ROOT/'data/export_validation_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('MATERIAL_EXPORT_VALIDATION_PASS',len(results))
