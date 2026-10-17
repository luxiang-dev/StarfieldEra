"""Import the user's painted fence, preserve UVs, and separate fixed posts from the gate leaf."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
out=root/'Assets/StarfieldEra/Environment/LocalModels';out.mkdir(parents=True,exist_ok=True)
source=Path('/home/lx/模型/手绘奇幻围栏3D_重新提交/model-02.glb')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
before=sum(len(o.data.polygons) for o in meshes)
for obj in meshes:
 bpy.context.view_layer.objects.active=obj;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 if before>6500:
  mod=obj.modifiers.new('Preserve painted fence silhouette','DECIMATE');mod.ratio=6500/before;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 for mat in obj.data.materials:
  if not mat or not mat.use_nodes:continue
  bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
  if node and node.type=='TEX_IMAGE':
   img=node.image.copy();img.scale(min(img.size[0],2048),min(img.size[1],2048));img.filepath_raw=str(out/'FantasyFence_BaseColor.png');img.file_format='PNG';img.save()
# Join and align the longest horizontal axis to Blender Y (Unity Z after FBX export).
bpy.ops.object.select_all(action='DESELECT')
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();obj=bpy.context.object
bounds=[obj.matrix_world@Vector(c) for c in obj.bound_box];lo=Vector(tuple(min(v[i] for v in bounds) for i in range(3)));hi=Vector(tuple(max(v[i] for v in bounds) for i in range(3)))
if hi.x-lo.x>hi.y-lo.y:obj.rotation_euler.z+=math.pi/2
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
zs=[v.co.z for v in obj.data.vertices];low=min(zs);height=max(zs)-low
ys=[v.co.y for v in obj.data.vertices if v.co.z>low+height*.65];mid=(min(ys)+max(ys))*.5
start=[y for y in ys if y<mid];end=[y for y in ys if y>=mid]
# Tall geometry is the two wooden posts, so their centres define the actual snapping span.
a=(min(start)+max(start))*.5;b=(min(end)+max(end))*.5;span=b-a
for v in obj.data.vertices:v.co=(v.co-Vector((0,(a+b)*.5,low)))/span
verts=obj.data.vertices
# Put the rail portion in its own object, retaining original UVs and all painted detail.
limits=(-.34,.34);parts=[]
for name,band in [('FencePostStart',0),('FenceRails',1),('FencePostEnd',2)]:
 copy=obj.copy();copy.data=obj.data.copy();bpy.context.collection.objects.link(copy);copy.name=name
 import bmesh
 bm=bmesh.new();bm.from_mesh(copy.data)
 remove=[]
 for face in bm.faces:
  y=face.calc_center_median().y;part=0 if y<limits[0] else 2 if y>limits[1] else 1
  if part!=band:remove.append(face)
 bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(copy.data);bm.free();parts.append(copy)
bpy.data.objects.remove(obj,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
for p in parts:p.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.export_scene.fbx(filepath=str(out/'FantasyFence.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
result={'source':str(source),'originalFaces':before,'gameFaces':sum(len(p.data.polygons) for p in parts),'postSpan':span,'normalizedHeight':height/span,'parts':{p.name:len(p.data.polygons) for p in parts}}
Path('/tmp/starfield-fantasy-fence-report.json').write_text(json.dumps(result,indent=2));print('FENCE_RESULT',json.dumps(result),flush=True)
