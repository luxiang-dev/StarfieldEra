"""Import verified Hunyuan downloads; preserve original files, fit zombie bind poses and continuous skin weights."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
project=Path(__file__).resolve().parents[2];source=Path('/home/lx/模型/混元生成/2026-10-18')
assets=[('TwinPea','双发射手',False),('IcePea','寒冰射手',False),('ConeZombie','路障僵尸',True),('BucketZombie','铁桶僵尸-网页版',True),('BruteZombie','壮汉僵尸-网页版',True),('Chomper','食人花-网页版',False),('CampLantern','路灯-网页版',False)]
only=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
def reset():
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.ops.outliner.orphans_purge(do_recursive=True)
def bounds(meshes):
 ps=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];lo=Vector([min(p[i] for p in ps) for i in range(3)]);hi=Vector([max(p[i] for p in ps) for i in range(3)]);return lo,hi
for name,folder,animated in assets:
 if only and name not in only:continue
 candidates=list((source/folder).glob('*.glb'))
 if not candidates:print('NOT_DOWNLOADED',name,flush=True);continue
 path=candidates[0];reset();rig=None;base=None
 if animated:
  bpy.ops.import_scene.fbx(filepath=str(project/'Assets/StarfieldEra/Environment/LocalModels/Zombie/Zombie.fbx'))
  rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');base=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig.data.pose_position='REST';bpy.context.view_layer.update();base_lo,base_hi=bounds([base]);rig.animation_data_clear()
  for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 previous=set(bpy.context.scene.objects);bpy.ops.import_scene.gltf(filepath=str(path));meshes=[o for o in set(bpy.context.scene.objects)-previous if o.type=='MESH'];lo,hi=bounds(meshes)
 height=max(.0001,hi.z-lo.z);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));scale=((base_hi.z-base_lo.z) if animated else 1)/height;target=Vector(((base_lo.x+base_hi.x)/2,(base_lo.y+base_hi.y)/2,base_lo.z)) if animated else Vector((0,0,0))
 original=sum(len(o.data.polygons) for o in meshes);out=project/'Assets/StarfieldEra/Environment/GeneratedGarden'/name;out.mkdir(parents=True,exist_ok=True);materials=[];textures={}
 for obj in meshes:
  matrix=obj.matrix_world.copy()
  for v in obj.data.vertices:v.co=(matrix@v.co-center)*scale+target
  obj.parent=None;obj.matrix_world=Matrix.Identity(4);bpy.context.view_layer.objects.active=obj
  if original>18000:
   dec=obj.modifiers.new('Runtime mesh budget','DECIMATE');dec.ratio=18000/original;dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
  for mat in obj.data.materials:
   if not mat or any(e['name']==mat.name for e in materials):continue
   bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
   node=bs.inputs['Base Color'].links[0].from_node if bs and bs.inputs['Base Color'].is_linked else None
   if not node or node.type!='TEX_IMAGE':raise RuntimeError('Missing generated base-color texture: '+name)
   img=node.image
   if img.name not in textures:
    filename=name+'_BaseColor'+str(len(textures))+'.png';textures[img.name]=filename;img.scale(min(2048,img.size[0]),min(2048,img.size[1]));img.filepath_raw=str(out/filename);img.file_format='PNG';img.save()
   materials.append({'name':mat.name,'texture':textures[img.name]})
  if animated:
   # Continuous anatomical weights avoid copying across nearby arms, torn coats and legs.
   # Hats follow the head rigidly; body height excludes the generated headwear.
   body=(base_hi.z-base_lo.z)*(.78 if name=='ConeZombie' else .84 if name=='BucketZombie' else 1)
   floor=base_lo.z;cx=(base_lo.x+base_hi.x)/2
   for bone in rig.data.bones:obj.vertex_groups.new(name=bone.name)
   def blend_chain(value,chain):
    if value<=chain[0][0]:return {chain[0][1]:1.0}
    for i in range(1,len(chain)):
     if value<=chain[i][0]:
      t=(value-chain[i-1][0])/(chain[i][0]-chain[i-1][0]);t=t*t*(3-2*t)
      return {chain[i-1][1]:1-t,chain[i][1]:t}
    return {chain[-1][1]:1.0}
   for v in obj.data.vertices:
    z=(v.co.z-floor)/body;x=(v.co.x-cx)/body;side='Left' if x>0 else 'Right'
    if z<.46:
     w=blend_chain(z,[(.08,side+'Foot'),(.20,side+'Leg'),(.34,side+'UpLeg'),(.46,'Hips')])
    else:
     w=blend_chain(z,[(.46,'Hips'),(.56,'Spine'),(.65,'Spine1'),(.74,'Spine2'),(.81,'Neck'),(.89,'Head')])
    # Hands can hang below the hips: never bind an outstretched hand to a leg.
    low=max(0,min(1,(.50-z)/.12));start=.14+low*(.09 if name=='BruteZombie' else .05);width=.12-low*.05
    arm=max(0,min(1,(abs(x)-start)/width)) if .20<z<.81 else 0
    if arm:
     aw=blend_chain(abs(x),[(.18,side+'Arm'),(.29,side+'ForeArm'),(.38,side+'Hand')])
     w={k:val*(1-arm) for k,val in w.items()}
     for k,val in aw.items():w[k]=w.get(k,0)+val*arm
    for k,val in w.items():
     if val>.00001:obj.vertex_groups[k].add([v.index],val,'REPLACE')
   obj.parent=rig;obj.matrix_parent_inverse=rig.matrix_world.inverted();mod=obj.modifiers.new('Zombie skeleton','ARMATURE');mod.object=rig

 if rig:
  # Fit the bind skeleton to the generated body instead of including hat height.
  fit=(.85,1,.78,1) if name=='ConeZombie' else (.85,1,.84,1) if name=='BucketZombie' else (1.30,1,1,1)
  rig.data.transform(Matrix.Diagonal(fit))
 if base:bpy.data.objects.remove(base,do_unlink=True)
 if rig:
  for pose in rig.pose.bones:pose.matrix_basis=Matrix.Identity(4)
  rig.data.pose_position='POSE'
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes+([rig] if rig else []):o.select_set(True)
 bpy.context.view_layer.objects.active=rig or meshes[0]
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'} if animated else {'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
 (out/'ImportReport.json').write_text(json.dumps({'source':str(path),'name':name,'animated':animated,'originalFaces':original,'gameFaces':sum(len(o.data.polygons) for o in meshes),'materials':materials},ensure_ascii=False,indent=2))
 print('EXPORTED',name,original,sum(len(o.data.polygons) for o in meshes),flush=True)
