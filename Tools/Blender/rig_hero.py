import bpy, math, os
from pathlib import Path
project = Path(__file__).resolve().parents[2]
from mathutils import Vector
out=str(project / 'Assets/StarfieldEra/Environment/LocalModels')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=out+'/Hero.fbx')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=mesh;mesh.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
vs=mesh.data.vertices
lo=Vector(tuple(min(v.co[i] for v in vs) for i in range(3)));hi=Vector(tuple(max(v.co[i] for v in vs) for i in range(3)))
h=hi.z-lo.z;cx=(lo.x+hi.x)/2;cy=(lo.y+hi.y)/2
for v in vs:v.co=(v.co-Vector((cx,cy,lo.z)))/h*1.85
# Coordinates below are fractions of standing height, in Blender's Z-up space.
def p(x,y,z):return Vector((x*1.85,y*1.85,z*1.85))
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='HeroRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
def bone(n,a,b,parent=None):
 e=rig.data.edit_bones.new(n);e.head=p(*a);e.tail=p(*b)
 if parent:e.parent=rig.data.edit_bones[parent]
 return e
bone('Root',(0,0,0),(0,0,.12))
bone('Hips',(0,0,.48),(0,0,.58),'Root')
bone('Spine',(0,0,.58),(0,0,.67),'Hips')
bone('Chest',(0,0,.67),(0,0,.76),'Spine')
bone('Head',(0,0,.76),(0,0,.98),'Chest')
for side,s in [('L',1),('R',-1)]:
 bone('UpperLeg.'+side,(s*.095,0,.48),(s*.12,0,.26),'Hips')
 bone('LowerLeg.'+side,(s*.12,0,.26),(s*.135,0,.065),'UpperLeg.'+side)
 bone('Foot.'+side,(s*.135,0,.065),(s*.135,-.10,.025),'LowerLeg.'+side)
 bone('UpperArm.'+side,(s*.14,0,.70),(s*.23,0,.57),'Chest')
 bone('LowerArm.'+side,(s*.23,0,.57),(s*.30,-.015,.44),'UpperArm.'+side)
 bone('Hand.'+side,(s*.30,-.015,.44),(s*.34,-.02,.39),'LowerArm.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
groups={b.name:mesh.vertex_groups.new(name=b.name) for b in rig.data.bones}
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def blend(a,b,t):return {a:1-t,b:t}
# Follow connected lower-arm geometry so inner fingers cannot inherit leg weights.
from collections import defaultdict, deque
keys=[tuple(round(float(c)/1.85,5) for c in v.co) for v in vs]
adjacency=defaultdict(set)
for poly in mesh.data.polygons:
 ks=[keys[i] for i in poly.vertices]
 for a,b in zip(ks,ks[1:]+ks[:1]):
  adjacency[a].add(b);adjacency[b].add(a)
arm_keys=set();queue=deque(k for k in adjacency if abs(k[0])>.245 and .30<k[2]<.64)
while queue:
 k=queue.popleft()
 if k in arm_keys:continue
 if not (abs(k[0])>.13 and .27<k[2]<.68):continue
 arm_keys.add(k);queue.extend(adjacency[k]-arm_keys)
for v in vs:
 x,y,z=v.co/1.85;s='L' if x>=0 else 'R';ax=abs(x)
 if z<.49:
  if keys[v.index] in arm_keys:w=blend('Hand.'+s,'LowerArm.'+s,smooth(.42,.48,z))
  elif z>.43:w=blend('UpperLeg.'+s,'Hips',smooth(.43,.51,z))
  elif z>.22:w=blend('LowerLeg.'+s,'UpperLeg.'+s,smooth(.22,.30,z))
  else:w=blend('Foot.'+s,'LowerLeg.'+s,smooth(.065,.13,z))
 elif z<.75 and ax>(.19 if z<.59 else .14):
  if z<.60:w=blend('LowerArm.'+s,'UpperArm.'+s,smooth(.52,.60,z))
  else:w=blend('UpperArm.'+s,'Chest',1-smooth(.13,.19,ax))
 elif z>.73:w=blend('Chest','Head',smooth(.73,.79,z))
 elif z>.64:w=blend('Spine','Chest',smooth(.64,.74,z))
 else:w=blend('Hips','Spine',smooth(.54,.64,z))
 for n,weight in w.items():
  if weight>0:groups[n].add([v.index],weight,'REPLACE')
mesh.parent=rig;mod=mesh.modifiers.new('Hero skin','ARMATURE');mod.object=rig
mesh.name='HeroMesh'
for name,walk in [('Idle',False),('Walk',True)]:
 rig.animation_data_create();rig.animation_data.action=bpy.data.actions.new(name)
 for f in range(1,26):
  phase=(f-1)/24*2*math.pi
  for b in rig.pose.bones:b.rotation_mode='XYZ';b.rotation_euler=(0,0,0);b.location=(0,0,0)
  rig.pose.bones['Hips'].location.z=(.014*(1-math.cos(phase*2)) if walk else .003*math.sin(phase))*1.85
  rig.pose.bones['Chest'].rotation_euler.y=.025*math.sin(phase) if walk else .008*math.sin(phase)
  for side,offset in [('L',0),('R',math.pi)]:
   t=phase+offset
   rig.pose.bones['UpperLeg.'+side].rotation_euler.x=.40*math.sin(t) if walk else 0
   rig.pose.bones['LowerLeg.'+side].rotation_euler.x= .60*max(0,math.sin(t)) if walk else 0
   rig.pose.bones['Foot.'+side].rotation_euler.x=.12*math.sin(t) if walk else 0
   rig.pose.bones['UpperArm.'+side].rotation_euler.x=-.23*math.sin(t) if walk else 0
   rig.pose.bones['LowerArm.'+side].rotation_euler.x=-.08 if walk else 0
  for b in rig.pose.bones:
   b.keyframe_insert('rotation_euler',frame=f,group=b.name);b.keyframe_insert('location',frame=f,group=b.name)
 # Keep both actions available to the FBX exporter.
 rig.animation_data.action.use_fake_user=True
scene=bpy.context.scene;scene.render.fps=30;scene.frame_start=1;scene.frame_end=25;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig

bpy.ops.export_scene.fbx(filepath=out+'/Hero.fbx',use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
print('RIG_EXPORT_DONE',len(vs),len(rig.data.bones),flush=True)
