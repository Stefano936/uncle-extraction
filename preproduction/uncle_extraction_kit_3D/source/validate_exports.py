"""Round-trip the actual FBX and GLB exports through Blender.
Run: blender --background --python source/validate_exports.py
Tests asset interchange only; does not validate Unity, collisions or gameplay.
"""
import bpy,json,math,struct
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
meta=json.loads((ROOT/'data/asset_metadata.json').read_text())
results=[]

def reset():
 # Include hidden importer helper collections, which selection-based deletion omits.
 for obj in list(bpy.data.objects):bpy.data.objects.remove(obj,do_unlink=True)
 for action in list(bpy.data.actions):bpy.data.actions.remove(action)
 bpy.context.scene.frame_set(1)

def mesh_bounds(meshes):
 corners=[o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
 return [max(v[i] for v in corners)-min(v[i] for v in corners) for i in range(3)]

for item in meta['assets']:
 for fmt in ['fbx','glb']:
  reset();path=ROOT/item[fmt]
  assert path.is_file() and path.stat().st_size>100,path
  if fmt=='fbx':bpy.ops.import_scene.fbx(filepath=str(path),use_anim=True)
  else:
   header=path.read_bytes()[:12];magic,ver,length=struct.unpack('<4sII',header)
   assert magic==b'glTF' and ver==2 and length==path.stat().st_size
   bpy.ops.import_scene.gltf(filepath=str(path))
  bpy.context.view_layer.update()
  # Blender's glTF importer can create hidden custom-shape meshes to display bones.
  # They are not nodes in the delivered GLB and must not count as asset geometry.
  helpers={pb.custom_shape for arm in bpy.context.scene.objects if arm.type=='ARMATURE' for pb in arm.pose.bones if pb.custom_shape}
  meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and o not in helpers];assert meshes,path
  bounds=mesh_bounds(meshes);expected=item['bounds_blender_xyz_m']
  # Different importer rest-action policies may affect a few cm on skinned geometry.
  tolerance=.08 if item['animated'] else .025
  assert all(abs(a-b)<=max(tolerance,abs(b)*.025) for a,b in zip(bounds,expected)),(path,bounds,expected)
  assert all(math.isfinite(v) and v>=0 for v in bounds)
  arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
  info={'asset':item['id'],'format':fmt,'status':'PASS','bounds_blender_xyz_m':[round(v,5) for v in bounds],'mesh_count':len(meshes),'materials':len({m.name for o in meshes for m in o.data.materials if m})}
  if item['animated']:
   assert len(arms)==1,(path,len(arms));rig=arms[0]
   assert len(rig.data.bones)==12,(path,len(rig.data.bones))
   socket_names={o.name.split('.')[0] if not o.name.startswith('Grab_') else o.name for o in bpy.context.scene.objects}
   # Named empties must survive and preserve their attachment in the export.
   for socket in meta['npc']['grab_sockets']:
    found=[o for o in bpy.context.scene.objects if o.name==socket['id'] or o.name.startswith(socket['id']+'.')]
    assert found,(path,socket['id'])
   actions=list(bpy.data.actions)
   for key in meta['npc']['clips']:assert any(key in a.name for a in actions),(path,key,[a.name for a in actions])
   weighted=sum(1 for o in meshes for v in o.data.vertices if v.groups)
   vertices=sum(len(o.data.vertices) for o in meshes)
   assert weighted==vertices,(path,weighted,vertices)
   push=next(a for a in actions if 'Push_Prototype' in a.name)
   rig.animation_data_create();rig.animation_data.action=push
   bpy.context.scene.frame_set(1);bpy.context.view_layer.update();a=rig.pose.bones['forearm.L'].matrix.copy()
   bpy.context.scene.frame_set(42);bpy.context.view_layer.update();b=rig.pose.bones['forearm.L'].matrix.copy()
   delta=max(abs(a[i][j]-b[i][j]) for i in range(4) for j in range(4));assert delta>.02,(path,'No observable push pose change')
   info.update({'bones':len(rig.data.bones),'weighted_vertices':weighted,'vertices':vertices,'clips':[a.name for a in actions],'push_pose_matrix_delta':round(delta,5)})
  else:assert not arms,(path,'Unexpected armature')
  results.append(info)

report={'blender_version':bpy.app.version_string,'checks':'Actual FBX and GLB reimports; bounds; named sockets; weighted vertices; animation presence and pose change','unity_import_tested':False,'physics_tested':False,'gameplay_tested':False,'results':results}
(ROOT/'data/validation_report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n')
print('ROUNDTRIP_VALIDATION_COMPLETE',len(results))
