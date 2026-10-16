"""Convert the supplied den to a normalized, budgeted game FBX, leaving its source intact."""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];out=root/'Assets/StarfieldEra/Environment/LocalModels/WolfDen';out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath='/home/lx/模型/狼窟/狼窟.glb')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];original=sum(len(o.data.polygons) for o in meshes)
points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=1/(hi.z-lo.z)
for obj in meshes:
 bpy.context.view_layer.objects.active=obj
 matrix=obj.matrix_world.copy()
 for v in obj.data.vertices:v.co=(matrix@v.co-center)*scale
 obj.matrix_world.identity()
 if original>18000:
  dec=obj.modifiers.new('Game mesh budget','DECIMATE');dec.ratio=18000/original;bpy.ops.object.modifier_apply(modifier=dec.name)
 for mat in obj.data.materials:
  if not mat or not mat.use_nodes:continue
  bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if bsdf and bsdf.inputs['Base Color'].is_linked:
   node=bsdf.inputs['Base Color'].links[0].from_node
   if node.type=='TEX_IMAGE':
    img=node.image;img.scale(2048,2048);img.filepath_raw=str(out/'WolfDen_BaseColor.png');img.file_format='PNG';img.save()
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'WolfDen.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='AUTO',use_mesh_modifiers=True)
(out/'ImportReport.json').write_text(json.dumps(dict(source='/home/lx/模型/狼窟/狼窟.glb',originalFaces=original,gameFaces=sum(len(o.data.polygons) for o in meshes)),indent=2))
