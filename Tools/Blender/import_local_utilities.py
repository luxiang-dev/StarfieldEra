"""Import user GLBs as optimized textured game meshes. Originals are never changed."""
import bpy,json,bmesh
from pathlib import Path
from mathutils import Vector
project=Path(__file__).resolve().parents[2];out=project/'Assets/StarfieldEra/Environment/LocalModels/Utilities';out.mkdir(parents=True,exist_ok=True)
report=[]
for name,folder,budget in [('MeadowRabbit','兔子',16000),('AncientPillar','石柱',12000),('Lumberyard','伐木场',42000),('Quarry','采石场',48000)]:
 if len(__import__('sys').argv)>1 and '--rabbit-only' in __import__('sys').argv and name!='MeadowRabbit':continue
 sources=sorted((Path('/home/lx/模型')/folder).glob('*.glb'))
 if not sources:raise RuntimeError('Missing source: '+folder)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(sources[0]))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];before=sum(len(o.data.polygons) for o in meshes)
 for obj in meshes:
  bpy.context.view_layer.objects.active=obj;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  if name=='MeadowRabbit':
   weld=bmesh.new();weld.from_mesh(obj.data);bmesh.ops.remove_doubles(weld,verts=list(weld.verts),dist=.00002);weld.to_mesh(obj.data);weld.free()
  if before>budget:
   mod=obj.modifiers.new('Game mesh reduction','DECIMATE');mod.ratio=budget/before;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 if name=='MeadowRabbit':
  # Remove the generated presentation mat: a following animal must not carry a patch of grass.
  obj=meshes[0];bm=bmesh.new();bm.from_mesh(obj.data);z0=min(v.co.z for v in bm.verts);h=max(v.co.z for v in bm.verts)-z0
  cut=z0+h*.075
  bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,cut),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
  # Keep the connected animal, discard detached ground petals and pebbles.
  remaining=set(bm.verts);components=[]
  while remaining:
   seed=remaining.pop();group={seed};queue=[seed]
   while queue:
    v=queue.pop()
    for edge in v.link_edges:
     other=edge.other_vert(v)
     if other in remaining:remaining.remove(other);group.add(other);queue.append(other)
   components.append(group)
  # Main body has the greatest vertical reach; preserve only pieces that touch it.
  main=max(components,key=len)
  discard=[v for g in components if g is not main for v in g];bmesh.ops.delete(bm,geom=discard,context='VERTS');bm.to_mesh(obj.data);bm.free()
 coords=[v.co for obj in meshes for v in obj.data.vertices];lo=Vector([min(v[i] for v in coords) for i in range(3)]);hi=Vector([max(v[i] for v in coords) for i in range(3)]);offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));height=hi.z-lo.z
 materials=[]
 for obj in meshes:
  for v in obj.data.vertices:v.co=(v.co-offset)/height
  for poly in obj.data.polygons:poly.use_smooth=True
  for mat in obj.data.materials:
   if not mat or not mat.use_nodes:continue
   bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None);node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
   if node and node.type=='TEX_IMAGE':
    img=node.image.copy();img.scale(2048,2048);img.filepath_raw=str(out/(name+'_BaseColor.png'));img.file_format='PNG';img.save();node.image=img;materials.append(img.filepath_raw)
 bpy.ops.object.select_all(action='DESELECT')
 for obj in meshes:obj.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP',use_mesh_modifiers=True)
 item={'name':name,'sourceFolder':folder,'originalFaces':before,'gameFaces':sum(len(o.data.polygons) for o in meshes),'gameVertices':sum(len(o.data.vertices) for o in meshes),'baseRemoved':name=='MeadowRabbit'};report.append(item);print('UTILITY_EXPORT',json.dumps(item),flush=True)
 center=Vector((0,0,.5));size=max((hi-lo)/height)
 bpy.ops.object.camera_add(location=center+Vector((size*1.1,-size*2,size*1.1)));cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size*1.35
 s=bpy.context.scene;s.camera=cam;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_cavity=True;s.render.resolution_x=640;s.render.resolution_y=640;s.render.resolution_percentage=100;s.render.filepath='/tmp/utility-'+name+'.png';bpy.ops.render.render(write_still=True)
(out/('RabbitReport.json' if '--rabbit-only' in __import__('sys').argv else 'ImportReport.json')).write_text(json.dumps(report,indent=2,ensure_ascii=False))
