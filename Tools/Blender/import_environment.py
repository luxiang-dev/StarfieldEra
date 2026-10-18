import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'Assets/StarfieldEra/Environment/LocalModels'
root.mkdir(parents=True,exist_ok=True)
report=[]
for name,src,budget in [('PineTree','/home/lx/模型/树/松树.glb',14000),('ResidentHouse','/home/lx/模型/居民房子/f8ddfc8490000b9f9f14860a3b78c366.glb',32000)]:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.gltf(filepath=src)
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];before=sum(len(o.data.polygons) for o in meshes)
 entry={'name':name,'originalFaces':before,'objects':len(meshes),'materials':[]}
 for obj in meshes:
  bpy.context.view_layer.objects.active=obj
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
  if before>budget:
   mod=obj.modifiers.new('Game silhouette reduction','DECIMATE');mod.ratio=budget/before;mod.use_collapse_triangulate=True
   bpy.ops.object.modifier_apply(modifier=mod.name)
  for mat in obj.data.materials:
   if not mat or not mat.use_nodes:continue
   bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
   node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
   if node and node.type=='TEX_IMAGE':
    img=node.image.copy();img.scale(min(img.size[0],2048),min(img.size[1],2048));img.filepath_raw=str(root/(name+'_BaseColor.png'));img.file_format='PNG';img.save();entry['materials'].append({'name':mat.name,'texture':img.filepath_raw})
 bpy.ops.object.select_all(action='DESELECT')
 for obj in meshes:obj.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(root/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP',use_mesh_modifiers=True)
 entry['gameFaces']=sum(len(o.data.polygons) for o in meshes);report.append(entry)
 print('ENVIRONMENT_RESULT',json.dumps(entry),flush=True)
Path('/tmp/starfield-environment-report.json').write_text(json.dumps(report,indent=2))
