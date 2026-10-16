"""Import supplied workshop/camp without changing the source GLBs."""
import bpy,json
from pathlib import Path
from mathutils import Vector
project=Path(__file__).resolve().parents[2]
for name,source in [('Workshop','工坊'),('Camp','营地')]:
 out=project/'Assets/StarfieldEra/Environment/LocalModels'/name;out.mkdir(parents=True,exist_ok=True)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.gltf(filepath=str(Path('/home/lx/模型/建筑')/(source+'.glb')))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];original=sum(len(o.data.polygons) for o in meshes)
 points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=1/(hi.z-lo.z)
 textures={};materials={}
 for obj in meshes:
  bpy.context.view_layer.objects.active=obj;matrix=obj.matrix_world.copy()
  for v in obj.data.vertices:v.co=(matrix@v.co-center)*scale
  obj.matrix_world.identity()
  if original>24000:
   dec=obj.modifiers.new('Game mesh budget','DECIMATE');dec.ratio=24000/original;dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
  for mat in obj.data.materials:
   if not mat or mat.name in materials:continue
   bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
   node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
   if not node or node.type!='TEX_IMAGE':raise RuntimeError('Missing base texture: '+mat.name)
   img=node.image
   if img.name not in textures:
    filename=name+'_BaseColor'+str(len(textures))+'.png';textures[img.name]=filename
    img.scale(min(2048,img.size[0]),min(2048,img.size[1]));img.filepath_raw=str(out/filename);img.file_format='PNG';img.save()
   materials[mat.name]=textures[img.name]
 bpy.ops.object.select_all(action='DESELECT')
 for obj in meshes:obj.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
 (out/'ImportReport.json').write_text(json.dumps(dict(source=str(Path('/home/lx/模型/建筑')/(source+'.glb')),originalFaces=original,gameFaces=sum(len(o.data.polygons) for o in meshes),materials=[dict(name=k,texture=v) for k,v in materials.items()]),ensure_ascii=False,indent=2))
 size=max((hi-lo)*scale);target=Vector((0,0,.5))
 bpy.ops.object.camera_add(location=target+Vector((size*1.2,-size*2,size)))
 camera=bpy.context.object;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=size*1.4
 scene=bpy.context.scene;scene.camera=camera;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_cavity=True
 scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.filepath='/tmp/starfield-building-'+name+'.png';bpy.ops.render.render(write_still=True)
 print('EXPORTED',name,materials,flush=True)
