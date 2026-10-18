"""Prepare supplied garden models and both zombie actions; originals stay untouched."""
import bpy,json
from pathlib import Path
from mathutils import Vector,Matrix
project=Path(__file__).resolve().parents[2]
source=Path('/home/lx/模型/植物大战僵尸')
def clear():
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 bpy.ops.outliner.orphans_purge(do_recursive=True)
def load(f):
 if f.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(f))
 else:bpy.ops.import_scene.fbx(filepath=str(f))
def textures(name,out,meshes):
 entries=[]
 for mat in dict.fromkeys(m for o in meshes for m in o.data.materials if m):
  bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
  node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
  if node and node.type=='TEX_IMAGE':
   image=node.image;filename=name+'_BaseColor'+str(len(entries))+'.png'
   image.scale(min(2048,image.size[0]),min(2048,image.size[1]));image.filepath_raw=str(out/filename);image.file_format='PNG';image.save()
   entries.append({'name':mat.name,'texture':filename})
 return entries
def finish(name,meshes,rigs,out,animated=False):
 original=sum(len(o.data.polygons) for o in meshes)
 for o in meshes:
  bpy.context.view_layer.objects.active=o
  dec=o.modifiers.new('Runtime triangle budget','DECIMATE');dec.ratio=min(1,18000/original);dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
 entries=textures(name,out,meshes)
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes+rigs:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'} if animated else {'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=animated,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,path_mode='STRIP')
 (out/'ImportReport.json').write_text(json.dumps({'sourceDirectory':str(source),'originalFaces':original,'gameFaces':sum(len(o.data.polygons) for o in meshes),'materials':entries,'actions':[a.name for a in bpy.data.actions]},ensure_ascii=False,indent=2))
 points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)]);size=max(hi-lo);target=(lo+hi)*.5
 bpy.ops.object.camera_add(location=target+Vector((size*1.1,-size*2,size*.7)));camera=bpy.context.object;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=size*1.4
 scene=bpy.context.scene;scene.camera=camera;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_cavity=True;scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100;scene.render.filepath='/tmp/garden-'+name+'.png';bpy.ops.render.render(write_still=True)
 print('EXPORTED',name,original,sum(len(o.data.polygons) for o in meshes),flush=True)
for name,file in [('Peashooter','豌豆射手.glb'),('GardenWall','城墙.fbx'),('ZombieTomb','墓碑.glb')]:
 clear();load(source/file);meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];out=project/'Assets/StarfieldEra/Environment/LocalModels'/name;out.mkdir(parents=True,exist_ok=True);finish(name,meshes,[],out)
clear();load(source/'僵尸-走路.fbx');meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];walk=bpy.data.actions[0];walk.name='ZombieWalk';walk.use_fake_user=True
before=set(bpy.context.scene.objects);load(source/'僵尸-沮丧.fbx');added=set(bpy.context.scene.objects)-before
sad=next(a for a in bpy.data.actions if a!=walk);sad.name='ZombieDismay';sad.use_fake_user=True
for o in added:bpy.data.objects.remove(o,do_unlink=True)
rigs[0].animation_data.action=walk;out=project/'Assets/StarfieldEra/Environment/LocalModels/Zombie';out.mkdir(parents=True,exist_ok=True);finish('Zombie',meshes,rigs,out,True)
