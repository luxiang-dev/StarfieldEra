"""Import supplied warehouse, town board, trap and pasture gate without changing the source GLBs."""
import bpy,json,bmesh,sys
from pathlib import Path
from mathutils import Vector,Matrix
project=Path(__file__).resolve().parents[2]
for name,source in [('Warehouse','仓库'),('TownBoard','城镇议事牌'),('AnimalTrap','捕兽夹'),('PastureGate','牧场门')]:
 if '--gate-only' in sys.argv and name!='PastureGate':continue
 out=project/'Assets/StarfieldEra/Environment/LocalModels'/name;out.mkdir(parents=True,exist_ok=True)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.gltf(filepath=str(Path('/home/lx/模型/资源')/(source+'.glb')))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];original=sum(len(o.data.polygons) for o in meshes)
 points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=1/(hi.z-lo.z)
 textures={};materials={}
 for obj in meshes:
  bpy.context.view_layer.objects.active=obj;matrix=obj.matrix_world.copy()
  for v in obj.data.vertices:v.co=(matrix@v.co-center)*scale
  obj.parent=None;obj.matrix_world=Matrix.Identity(4)
  bpy.context.view_layer.update()
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
 if name=='PastureGate':
  # Align post-to-post direction to Unity Z, like the existing snapping fence.
  bpy.ops.object.select_all(action='DESELECT')
  for obj in meshes:obj.select_set(True)
  bpy.context.view_layer.objects.active=meshes[0]
  if len(meshes)>1:bpy.ops.object.join()
  obj=bpy.context.object
  if hi.x-lo.x>hi.y-lo.y:
   for v in obj.data.vertices:v.co=Vector((-v.co.y,v.co.x,v.co.z))
  # Each pillar has a different height; sample the top of each half separately.
  lower=[v.co for v in obj.data.vertices if v.co.y<0];upper=[v.co for v in obj.data.vertices if v.co.y>=0]
  leftTop=max(p.z for p in lower);rightTop=max(p.z for p in upper)
  left=[v.y for v in lower if v.z>leftTop*.90]
  right=[v.y for v in upper if v.z>rightTop*.90]
  a=(min(left)+max(left))/2;b=(min(right)+max(right))/2;span=b-a
  print('GATE_SPAN',a,b,span,flush=True)
  for v in obj.data.vertices:v.co=(v.co-Vector((0,(a+b)/2,0)))/span
  meshes=[]
  for part in ['GatePostStart','GatePostEnd','GateGround','GateLeafStart','GateLeafEnd']:
   copy=obj.copy();copy.data=obj.data.copy();bpy.context.collection.objects.link(copy);copy.name=part
   bm=bmesh.new();bm.from_mesh(copy.data);remove=[]
   for face in bm.faces:
    c=face.calc_center_median()
    band='GatePostStart' if c.y<-.43 else 'GatePostEnd' if c.y>.43 else 'GateGround' if c.z<.12/span else 'GateLeafStart' if c.y<0 else 'GateLeafEnd'
    if band!=part:remove.append(face)
   bmesh.ops.delete(bm,geom=remove,context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(copy.data);bm.free();meshes.append(copy)
  bpy.data.objects.remove(obj,do_unlink=True)
 bpy.ops.object.select_all(action='DESELECT')
 for obj in meshes:obj.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
 (out/'ImportReport.json').write_text(json.dumps(dict(source=str(Path('/home/lx/模型/资源')/(source+'.glb')),originalFaces=original,gameFaces=sum(len(o.data.polygons) for o in meshes),parts={o.name:len(o.data.polygons) for o in meshes},materials=[dict(name=k,texture=v) for k,v in materials.items()]),ensure_ascii=False,indent=2))
 points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 lo2=Vector([min(p[i] for p in points) for i in range(3)]);hi2=Vector([max(p[i] for p in points) for i in range(3)])
 size=max(hi2-lo2);target=(lo2+hi2)/2
 bpy.ops.object.camera_add(location=target+Vector((size*1.2,-size*2,size)))
 camera=bpy.context.object;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=size*1.4
 scene=bpy.context.scene;scene.camera=camera;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_cavity=True
 scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.filepath='/tmp/starfield-resource-'+name+'.png';bpy.ops.render.render(write_still=True)
 print('EXPORTED',name,materials,flush=True)
