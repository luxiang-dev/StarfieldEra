"""Reproducible Hunyuan GLB -> welded skin, Blender rig/actions, game FBX and native .blend.
Usage: blender -b -t 4 --python Tools/Blender/build_starfarer_hero.py -- SOURCE_GLB OUTPUT_DIR
Blender Z-up, character faces -Y. Actions are in place; gameplay controls translation.
"""
import bpy, math, sys, json
from pathlib import Path
from mathutils import Vector, Matrix
args=sys.argv[sys.argv.index('--')+1:];src=Path(args[0]);out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
H=1.8
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(src))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();mesh=bpy.context.object;mesh.name='StarfarerMesh'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
lo=Vector([min(v.co[i] for v in mesh.data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for v in mesh.data.vertices) for i in range(3)])
for v in mesh.data.vertices:v.co=(v.co-Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z)))*H/(hi.z-lo.z)
# UV seams remain on loops while duplicate positions are welded for heat diffusion.
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=0.00001);bpy.ops.object.mode_set(mode='OBJECT')
for poly in mesh.data.polygons:poly.use_smooth=True
for mat in mesh.data.materials:
 if mat and mat.use_nodes:
  bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  node=bsdf.inputs['Base Color'].links[0].from_node if bsdf and bsdf.inputs['Base Color'].is_linked else None
  if node and node.type=='TEX_IMAGE':
   img=node.image.copy();img.scale(2048,2048);img.filepath_raw=str(out/'Starfarer_BaseColor.png');img.file_format='PNG';img.save();node.image=img
  if bsdf:bsdf.inputs['Roughness'].default_value=.82;bsdf.inputs['Metallic'].default_value=0

def p(x,y,z):return Vector((x*H,y*H,z*H))
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='StarfarerRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
def bone(n,a,b,parent=None,deform=True):
 e=rig.data.edit_bones.new(n);e.head=p(*a);e.tail=p(*b);e.use_deform=deform
 if parent:e.parent=rig.data.edit_bones[parent]
 return e
bone('Root',(0,0,0),(0,0,.10),deform=False)
bone('Hips',(0,.025,.51),(0,.025,.59),'Root')
bone('Spine',(0,.025,.59),(0,.025,.68),'Hips')
bone('Chest',(0,.025,.68),(0,.025,.795),'Spine')
bone('Neck',(0,.025,.795),(0,.025,.845),'Chest')
bone('Head',(0,.025,.845),(0,.025,.985),'Neck')
for side,s in [('L',1),('R',-1)]:
 bone('Shoulder.'+side,(0,.025,.78),(s*.115,.025,.79),'Chest')
 bone('UpperArm.'+side,(s*.115,.025,.79),(s*.285,.02,.775),'Shoulder.'+side)
 bone('LowerArm.'+side,(s*.285,.02,.775),(s*.432,.012,.755),'UpperArm.'+side)
 bone('Hand.'+side,(s*.432,.012,.755),(s*.501,-.005,.75),'LowerArm.'+side)
 bone('UpperLeg.'+side,(s*.085,.03,.51),(s*.105,.025,.285),'Hips')
 bone('LowerLeg.'+side,(s*.105,.025,.285),(s*.113,.04,.075),'UpperLeg.'+side)
 bone('Foot.'+side,(s*.113,.04,.075),(s*.113,-.075,.032),'LowerLeg.'+side)
 bone('Toe.'+side,(s*.113,-.075,.032),(s*.113,-.115,.025),'Foot.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
# Fail loudly instead of exporting unweighted or unbound generated geometry.
missing=sum(1 for v in mesh.data.vertices if not any(g.weight>.0001 for g in v.groups))
if missing:raise RuntimeError('Bone heat left %d vertices unweighted; inspect topology before export'%missing)
rig.show_in_front=True
scene=bpy.context.scene;scene.render.fps=30
rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
# Match each bone's bind orientation to a desired world-space direction, retaining its roll.
def aim(n,d):
 b=rig.pose.bones[n];m=b.matrix.copy();q=rest[n].to_quaternion();bind=rest[n].to_3x3()@Vector((0,1,0));rot=bind.rotation_difference(Vector(d).normalized())@q
 m=rot.to_matrix().to_4x4();m.translation=b.head.copy();b.matrix=m;bpy.context.view_layer.update()
def solve_leg(side,ankle):
 a=rig.pose.bones['UpperLeg.'+side];b=rig.pose.bones['LowerLeg.'+side];c=rig.pose.bones['Foot.'+side]
 origin=a.head.copy();delta=ankle-origin;l1=a.length;l2=b.length;d=max(.001,min(delta.length,l1+l2-.0005));direction=delta.normalized()
 bend=Vector((0,-1,0));bend=(bend-direction*bend.dot(direction)).normalized();along=(l1*l1+d*d-l2*l2)/(2*d);lift=math.sqrt(max(0,l1*l1-along*along));knee=origin+direction*along+bend*lift
 aim(a.name,knee-origin);aim(b.name,ankle-b.head);aim(c.name,rest[c.name].to_3x3()@Vector((0,1,0)))
 aim('Toe.'+side,rest['Toe.'+side].to_3x3()@Vector((0,1,0)))

def pose(t,kind):
 moving=kind!='Idle';run=kind=='Run';phase=t*2*math.pi
 for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update()
 hips=rig.pose.bones['Hips'];m=hips.matrix.copy()
 drop=.085 if run else .043 if moving else .012
 m.translation+=p(.007*math.sin(phase) if moving else .002*math.sin(phase),0,-drop+(.004 if moving else .0015)*(1-math.cos(phase*2)))
 hips.matrix=m;bpy.context.view_layer.update()
 lean=.12 if run else .045 if moving else .005
 aim('Spine',(0,-lean,1));aim('Chest',(.018*math.sin(phase) if moving else 0,-lean,1));aim('Neck',(0,0,1));aim('Head',(0,0,1))
 for side,s,offset in [('L',1,0),('R',-1,.5)]:
  f=(t+offset)%1;ph=phase+offset*2*math.pi
  swing=math.sin(ph)
  # Relaxed arms, slight elbow flexion. Opposite arm and leg swing together.
  aim('UpperArm.'+side,(s*.12,(-.15 if run else -.035)+( .48 if run else .25 if moving else .012)*swing,-1))
  aim('LowerArm.'+side,(s*.045,(-.85 if run else -.19)+( .55 if run else .30 if moving else .008)*swing,-1))
  aim('Hand.'+side,(s*.015,-.20 if run else -.10,-1))
  stance=.40 if run else .57;stride=.25 if run else .175;lift=.105 if run else .060
  if not moving:y=.025;z=.075
  elif f<stance:
   u=f/stance;y=.025-stride+2*stride*u;z=.075
  else:
   u=(f-stance)/(1-stance);ease=u*u*(3-2*u);y=.025+stride-2*stride*ease;z=.075+lift*math.sin(math.pi*u)**1.2
  solve_leg(side,p(s*.113,y,z))
 for b in rig.pose.bones:
  # Quaternion sign continuity prevents interpolation taking the long path.
  b.rotation_quaternion.normalize()

report={'source':'Hunyuan 3.0 LowPoly quad / PBR','height':H,'vertices':len(mesh.data.vertices),'faces':len(mesh.data.polygons),'bones':len(rig.data.bones),'unweighted':missing,'actions':[]}
for kind,frames,nominal in [('Idle',61,0),('Walk',25,1.4),('Run',19,3.8)]:
 rig.animation_data_create();action=bpy.data.actions.new(kind);rig.animation_data.action=action;action.use_fake_user=True
 for frame in range(1,frames+1):
  pose((frame-1)/(frames-1),kind)
  for b in rig.pose.bones:
   b.keyframe_insert('rotation_quaternion',frame=frame,group=b.name);b.keyframe_insert('location',frame=frame,group=b.name)
 report['actions'].append({'name':kind,'frames':frames,'fps':30,'nominalSpeed':nominal})
# Keep the Blender source beside the raw model, outside the runtime repository.
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_start=1;scene.frame_end=61;scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(src.parent/'Starfarer_Rig_Animations.blend'))
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(out/'Starfarer.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
(out/'Starfarer_Report.json').write_text(json.dumps(report,indent=2))
# Actual skinned pose previews, not the generation reference.
center=p(0,0,.48);size=H
bpy.ops.object.camera_add(location=center+Vector((size*1.0,-size*2.7,size*.45)));cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=H*1.18;scene.camera=cam
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.background_type='WORLD';scene.world.color=(.17,.20,.19);scene.render.resolution_x=720;scene.render.resolution_y=840;scene.render.resolution_percentage=100
for name,frame in [('Idle',1),('Walk',5),('Run',4)]:
 rig.animation_data.action=bpy.data.actions[name];scene.frame_set(frame);scene.render.filepath=str(src.parent/('Starfarer_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('STARFARER_EXPORT',json.dumps(report),flush=True)
