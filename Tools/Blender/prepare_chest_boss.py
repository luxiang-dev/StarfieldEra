import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
project=Path(__file__).resolve().parents[2];out=project/'Assets/StarfieldEra/Environment/LocalModels'
# Slice the supplied single-mesh chest into a fixed base and a real hinged lid.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(out/'SupplyChest.fbx'))
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH');bpy.context.view_layer.objects.active=mesh;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
coords=[v.co for v in mesh.data.vertices];lo=Vector(tuple(min(v[i] for v in coords) for i in range(3)));hi=Vector(tuple(max(v[i] for v in coords) for i in range(3)));cut=lo.z+(hi.z-lo.z)*.52
lid=mesh.copy();lid.data=mesh.data.copy();bpy.context.collection.objects.link(lid)
for obj,is_lid in [(mesh,False),(lid,True)]:
 obj.name='ChestLid' if is_lid else 'ChestBody'
 bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=(0,0,cut),plane_no=(0,0,1),clear_inner=is_lid,clear_outer=not is_lid,dist=.00001);bm.to_mesh(obj.data);bm.free()
 if is_lid:
  pivot=Vector(((lo.x+hi.x)*.5,hi.y-(hi.y-lo.y)*.12,cut));obj.location=pivot
  for v in obj.data.vertices:v.co-=pivot
bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(out/'SupplyChest.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
# Preserve the existing barbarian rig and animations, while reducing its game copy.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(project/'Assets/StarfieldEra/Characters/Barbarian/Barbarian.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];before=sum(len(o.data.polygons) for o in meshes)
for obj in meshes:
 bpy.context.view_layer.objects.active=obj;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
 if before>22000:
  mod=obj.modifiers.new('Boss game reduction','DECIMATE');mod.ratio=22000/before;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
img=bpy.data.images.load(str(project/'Assets/StarfieldEra/Characters/Barbarian/Barbarian_BaseColor.png'),check_existing=False);img.scale(2048,2048);img.filepath_raw=str(out/'WildBarbarian_BaseColor.png');img.file_format='PNG';img.save()
bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(out/'WildBarbarian.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
print('BOSS_RESULT',before,sum(len(o.data.polygons) for o in meshes),flush=True)
