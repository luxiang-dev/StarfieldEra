"""Normalize and rig the generated quadruped; preserve downloaded source files."""
import bpy,bmesh,math,json,sys,numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
project=Path(__file__).resolve().parents[2]
source=Path(sys.argv[sys.argv.index('--')+1]);out=project/'Assets/StarfieldEra/Environment/LocalModels/NightWolf';out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(source))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];original=sum(len(o.data.polygons) for o in meshes)
for obj in meshes:
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 if original>14000:
  dec=obj.modifiers.new('Game geometry budget','DECIMATE');dec.ratio=14000/original;dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
# Sever the thin presentation plinth below the paws, then keep the connected wolf.
for obj in meshes:
 bm=bmesh.new();bm.from_mesh(obj.data)
 # Ground paint is warm green/brown; the wolf's paws are neutral silver/blue.
 # Remove the painted plinth without clipping the low front toes.
 uv_layer=bm.loops.layers.uv.active;ground_images={}
 for index,mat in enumerate(obj.data.materials):
  if not mat or not mat.use_nodes:continue
  bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
  if node and node.type=='TEX_IMAGE':
   img=node.image;pixels=np.empty(len(img.pixels),dtype=np.float32);img.pixels.foreach_get(pixels);ground_images[index]=(pixels.reshape((img.size[1],img.size[0],4)),img.size[0],img.size[1])
 bottom=min(v.co.z for v in bm.verts);top=max(v.co.z for v in bm.verts);remove=[]
 for face in bm.faces:
  if max(v.co.z for v in face.verts)>bottom+(top-bottom)*.14 or not uv_layer or face.material_index not in ground_images:continue
  pixels,w,h=ground_images[face.material_index];uv=sum((loop[uv_layer].uv for loop in face.loops),Vector((0,0)))/len(face.loops)
  rgb=pixels[min(h-1,max(0,int(uv.y*h))),min(w-1,max(0,int(uv.x*w))),:3]
  if rgb[2]<max(rgb[0],rgb[1])*.86:remove.append(face)
 bmesh.ops.delete(bm,geom=remove,context='FACES');bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00003)
 bottom=min(v.co.z for v in bm.verts);top=max(v.co.z for v in bm.verts)
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=(0,0,bottom+(top-bottom)*.015),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
 remaining=set(bm.verts);parts=[]
 while remaining:
  vertex=remaining.pop();group={vertex};queue=[vertex]
  while queue:
   vertex=queue.pop()
   for edge in vertex.link_edges:
    other=edge.other_vert(vertex)
    if other in remaining:remaining.remove(other);group.add(other);queue.append(other)
  parts.append(group)
 main=max(parts,key=lambda g:max(v.co.z for v in g)-min(v.co.z for v in g))
 bmesh.ops.delete(bm,geom=[v for part in parts if part is not main for v in part],context='VERTS');bm.to_mesh(obj.data);bm.free()
coords=[v.co for o in meshes for v in o.data.vertices]
def bounds(coords):return Vector([min(v[i] for v in coords) for i in range(3)]),Vector([max(v[i] for v in coords) for i in range(3)])
lo,hi=bounds(coords);height=hi.z-lo.z
# The ears mark the head end. Align it toward Blender -Y / Unity +Z.
ears=[p for p in coords if p.z>lo.z+height*.90];head=sum(ears,Vector())/len(ears);center=(lo+hi)*.5
heading=math.atan2(head.y-center.y,head.x-center.x);yaw=-math.pi/2-heading;rotation=Matrix.Rotation(yaw,4,'Z')
for o in meshes:
 for v in o.data.vertices:v.co=rotation@v.co
coords=[v.co for o in meshes for v in o.data.vertices];lo,hi=bounds(coords);height=hi.z-lo.z;offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for obj in meshes:
 for v in obj.data.vertices:v.co=(v.co-offset)/height
 for p in obj.data.polygons:p.use_smooth=True
images=[]
for obj in meshes:
 for mat in obj.data.materials:
  if not mat or not mat.use_nodes:continue
  bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
  if node and node.type=='TEX_IMAGE' and node.image:images.append(node.image)
if not images:raise RuntimeError('Generated wolf has no base-colour texture')
image=images[0];image.scale(2048,2048);image.filepath_raw=str(out/'NightWolf_BaseColor.png');image.file_format='PNG';image.save()
coords=[v.co for o in meshes for v in o.data.vertices];lo,hi=bounds(coords)
feet=[v for v in coords if v.z<.13];mid_y=(min(v.y for v in feet)+max(v.y for v in feet))*.5
paws={}
for prefix,front in [('Fore',True),('Hind',False)]:
 for side,left in [('L',True),('R',False)]:
  candidates=[v for v in feet if (v.y<mid_y)==front and (v.x>=0)==left]
  if not candidates:raise RuntimeError('Cannot locate four separated wolf paws')
  paws[prefix+'Leg_'+side]=sum(candidates,Vector())/len(candidates)
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='NightWolfRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
def bone(name,a,b,parent=None):
 e=rig.data.edit_bones.new(name);e.head=Vector(a);e.tail=Vector(b)
 if parent:e.parent=rig.data.edit_bones[parent]
 return e
bone('WolfBody',(0,.1,.54),(0,-.2,.6))
fore_y=sum(p.y for n,p in paws.items() if n.startswith('Fore'))/2
hind_y=sum(p.y for n,p in paws.items() if n.startswith('Hind'))/2
bone('WolfHead',(0,fore_y-.04,.68),(0,lo.y+.12,.85),'WolfBody')
bone('WolfTail',(0,hind_y+.08,.53),(0,hi.y-.05,.35),'WolfBody')
for name,p in paws.items():
 hip=Vector((p.x*.85,p.y,.60 if name.startswith('Fore') else .56));knee=Vector((p.x,p.y+.025,.27))
 bone(name,hip,knee,'WolfBody');bone(name.replace('Leg','Paw'),knee,(p.x,p.y-.04,.045),name)
bpy.ops.object.mode_set(mode='OBJECT')
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
for obj in meshes:
 groups={b.name:obj.vertex_groups.new(name=b.name) for b in rig.data.bones}
 for v in obj.data.vertices:
  x,y,z=v.co;leg=min(paws,key=lambda n:(x-paws[n].x)**2*1.8+(y-paws[n].y)**2)
  weight=(1-smooth(.42,.64,z))*(1-smooth(.14,.30,abs(y-paws[leg].y)))*(smooth(.02,.12,abs(x)) if z>.30 else 1)
  if weight>0:
   lower=1-smooth(.19,.34,z);weights={leg:weight*(1-lower),leg.replace('Leg','Paw'):weight*lower,'WolfBody':1-weight}
  else:
   head_weight=smooth(.02,.24,fore_y-y)*smooth(.5,.65,z)
   tail_weight=smooth(.12,.36,y-hind_y)*(1-smooth(.59,.73,z))
   weights={'WolfHead':head_weight,'WolfTail':tail_weight*(1-head_weight),'WolfBody':(1-head_weight)*(1-tail_weight)}
  for name,w in weights.items():
   if w>0:groups[name].add([v.index],w,'REPLACE')
 obj.parent=rig;mod=obj.modifiers.new('Wolf skin','ARMATURE');mod.object=rig;obj.name='NightWolfMesh'
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(out/'NightWolf.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
report={'source':str(source),'originalFaces':original,'gameFaces':sum(len(o.data.polygons) for o in meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'presentationBaseRemoved':True,'bones':len(rig.data.bones),'facingCorrectionDegrees':math.degrees(yaw),'normalizedBounds':list(hi-lo),'paws':{n:list(p) for n,p in paws.items()}}
(out/'ImportReport.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print('WOLF_EXPORT',json.dumps(report),flush=True)
# Texture preview from the front quarter, with paws and silhouette visible.
center=Vector((0,0,.48));span=max(hi-lo)
bpy.ops.object.camera_add(location=center+Vector((span*.95,-span*1.4,span*.7)));cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=span*1.3
scene=bpy.context.scene;scene.camera=cam;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_cavity=True;scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.filepath=str(source.parent/'NightWolfPreview.png');bpy.ops.render.render(write_still=True)
