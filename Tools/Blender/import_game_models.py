import bpy,json,math
from pathlib import Path
from mathutils import Vector
project=Path(__file__).resolve().parents[2]
out=project/'Assets/StarfieldEra/Environment/LocalModels';out.mkdir(parents=True,exist_ok=True)
report=[]
for name,folder,budget in [('FieldRock','石头',6000),('SupplyChest','宝箱',12000),('Hero','主角',22000)]:
 src=next((Path('/home/lx/模型')/folder).glob('*.glb'))
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.gltf(filepath=str(src))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];before=sum(len(o.data.polygons) for o in meshes)
 for obj in meshes:
  bpy.context.view_layer.objects.active=obj;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
  if before>budget:
   mod=obj.modifiers.new('Game silhouette reduction','DECIMATE');mod.ratio=budget/before;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
  for mat in obj.data.materials:
   if not mat or not mat.use_nodes:continue
   bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
   node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
   if node and node.type=='TEX_IMAGE':
    img=node.image.copy();img.scale(min(img.size[0],2048),min(img.size[1],2048));img.filepath_raw=str(out/(name+'_BaseColor.png'));img.file_format='PNG';img.save()
 bpy.ops.object.select_all(action='DESELECT')
 for obj in meshes:obj.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
 bounds=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=Vector(tuple(min(v[i] for v in bounds) for i in range(3)));hi=Vector(tuple(max(v[i] for v in bounds) for i in range(3)));center=(lo+hi)/2;size=max(hi-lo)
 bpy.ops.object.camera_add(location=center+Vector((size*1.25,-size*2.3,size*.85)));cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size*1.4
 scene=bpy.context.scene;scene.camera=cam;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100;scene.render.filepath='/tmp/starfield-'+name+'.png';bpy.ops.render.render(write_still=True)
 item={'name':name,'originalFaces':before,'gameFaces':sum(len(o.data.polygons) for o in meshes),'bounds':list(hi-lo)};report.append(item);print('MODEL_RESULT',json.dumps(item),flush=True)
Path('/tmp/starfield-game-models-report.json').write_text(json.dumps(report,indent=2))
