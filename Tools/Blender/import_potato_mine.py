"""Import one verified web download, retain texture and cap runtime triangle count."""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector,Matrix
source=Path(sys.argv[sys.argv.index('--')+1]);project=Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source));meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)]);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=.85/max(.001,hi.z-lo.z)
folder=project/'Assets/StarfieldEra/Environment/GeneratedGarden/PotatoMine';folder.mkdir(parents=True,exist_ok=True)
original=sum(len(o.data.polygons) for o in meshes);materials=[];textures={}
for obj in meshes:
 matrix=obj.matrix_world.copy()
 for v in obj.data.vertices:v.co=(matrix@v.co-center)*scale
 obj.parent=None;obj.matrix_world=Matrix.Identity(4);bpy.context.view_layer.objects.active=obj
 if original>12000:
  dec=obj.modifiers.new('Runtime budget','DECIMATE');dec.ratio=12000/original;bpy.ops.object.modifier_apply(modifier=dec.name)
 for mat in obj.data.materials:
  if not mat or any(x['name']==mat.name for x in materials):continue
  bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
  node=bs.inputs['Base Color'].links[0].from_node if bs and bs.inputs['Base Color'].is_linked else None
  if not node or node.type!='TEX_IMAGE':raise RuntimeError('Missing base texture')
  img=node.image
  if img.name not in textures:
   filename='PotatoMine_BaseColor'+str(len(textures))+'.png';textures[img.name]=filename;img.scale(min(1024,img.size[0]),min(1024,img.size[1]));img.filepath_raw=str(folder/filename);img.file_format='PNG';img.save()
  materials.append({'name':mat.name,'texture':textures[img.name]})
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(folder/'PotatoMine.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
(folder/'ImportReport.json').write_text(json.dumps({'source':source.name,'originalFaces':original,'gameFaces':sum(len(o.data.polygons) for o in meshes),'materials':materials},ensure_ascii=False,indent=2))
