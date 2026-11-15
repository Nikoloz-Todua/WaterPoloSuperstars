"""Headless Blender conversion of selected CC0 sources. Never opens/saves a user's .blend."""
import bpy, json, os, math
from mathutils import Vector, Matrix, Quaternion, Euler
root=os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
source=os.path.join(root,'SourceAssets/SwimmerAnimation')
reports=os.path.join(root,'Logs/SwimmerAnimation');os.makedirs(reports,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(source,'character_ual.glb'))
report=[]
for obj in bpy.context.scene.objects:
    row={'name':obj.name,'type':obj.type,'location':list(obj.location),'rotation':list(obj.rotation_euler),'scale':list(obj.scale),'dimensions':list(obj.dimensions)}
    if obj.type=='ARMATURE':row['bones']=[{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local),'parent':b.parent.name if b.parent else None} for b in obj.data.bones]
    if obj.type=='MESH':row['vertices']=len(obj.data.vertices);row['polygons']=len(obj.data.polygons)
    report.append(row)
with open(os.path.join(reports,'Blender-rig-inspection.json'),'w') as f:json.dump(report,f,indent=2)
print('INSPECTION',[(o.name,o.type,list(o.dimensions)) for o in bpy.context.scene.objects])

out=os.path.join(root,'Assets/SwimmerAnimationPrototype')
os.makedirs(os.path.join(out,'Models'),exist_ok=True)
os.makedirs(os.path.join(out,'Animations'),exist_ok=True)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.vertices)>1000)
for obj in list(bpy.context.scene.objects):
    if obj not in [rig,body]:bpy.data.objects.remove(obj,do_unlink=True)
def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);return m
skin=material('AthleteSkin',(.69,.40,.25));suit=material('Swimwear',(.025,.15,.62));cap=material('WaterPoloCap',(.025,.15,.62))
body.data.materials.clear();body.data.materials.append(skin);body.data.materials.append(suit)
for polygon in body.data.polygons:
    center=sum((body.data.vertices[v].co for v in polygon.vertices),Vector())/len(polygon.vertices)
    polygon.material_index=1 if .81<center.z<1.035 else 0
    polygon.use_smooth=True
bpy.context.view_layer.objects.active=body
mod=body.modifiers.new('Mobile prototype mesh budget','DECIMATE');mod.ratio=.16;mod.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier=mod.name)
def bind(obj,bone,mat):
    obj.data.materials.clear();obj.data.materials.append(mat)
    group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    modifier=obj.modifiers.new('Skin','ARMATURE');modifier.object=rig;obj.parent=rig
    for p in obj.data.polygons:p.use_smooth=True
# Fitted cap hemisphere, cut at the ear line. It is weighted, not a collider or gameplay attachment.
verts=[];faces=[];segments=24;rings=8
for r in range(rings+1):
    angle=(math.pi/2)*r/rings
    for s in range(segments):
        a=2*math.pi*s/segments;verts.append((.137*math.cos(angle)*math.cos(a),.035+.20*math.cos(angle)*math.sin(a),1.715+.15*math.sin(angle)))
for r in range(rings):
    for s in range(segments):
        n=(s+1)%segments;faces.append((r*segments+s,r*segments+n,(r+1)*segments+n,(r+1)*segments+s))
mesh=bpy.data.meshes.new('Fitted cap');mesh.from_pydata(verts,[],faces);mesh.update();obj=bpy.data.objects.new('Competition cap',mesh);bpy.context.collection.objects.link(obj);bind(obj,'Head',cap)
for sign in [-1,1]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=(sign*.132,.03,1.708));obj=bpy.context.object;obj.name='Padded ear guard';obj.scale=(.032,.047,.056);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bind(obj,'Head',cap)
def export(path,armature,animated=False,meshes=False):
    bpy.ops.object.select_all(action='DESELECT');armature.select_set(True)
    if meshes:
        for o in bpy.context.scene.objects:
            if o.type=='MESH':o.select_set(True)
    bpy.context.view_layer.objects.active=armature
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=animated,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_simplify_factor=.15,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',mesh_smooth_type='FACE',use_mesh_modifiers=True)
export(os.path.join(out,'Models','WaterPoloAthlete.fbx'),rig,meshes=True)
source_idle=[];source_stats={}
for name in ['swim_idle_loop','swim_fwd_loop']:
    existing=set(bpy.context.scene.objects);bpy.context.scene.render.fps=30
    bpy.ops.import_scene.gltf(filepath=os.path.join(source,name+'.glb'))
    imported=set(bpy.context.scene.objects)-existing;arm=next(o for o in imported if o.type=='ARMATURE')
    action=arm.animation_data.action;start,end=map(int,action.frame_range);bpy.context.scene.frame_start=start;bpy.context.scene.frame_end=end
    source_stats[name]={'start':start,'end':end,'bones':len(arm.data.bones)}
    for frame in [start,(start+end)//2,end]:
        bpy.context.scene.frame_set(frame);source_stats[name][str(frame)]={n:list(arm.pose.bones[n].head) for n in ['pelvis','Head','hand_l','hand_r','foot_l','foot_r']}
    if name=='swim_idle_loop':
        for frame in range(start,end+1):
            bpy.context.scene.frame_set(frame);source_idle.append({b.name:b.matrix_basis.copy() for b in arm.pose.bones})
    export(os.path.join(out,'Animations','Source_'+name+'.fbx'),arm,animated=True)
    for o in imported:bpy.data.objects.remove(o,do_unlink=True)
with open(os.path.join(reports,'Blender-source-motion-inspection.json'),'w') as f:json.dump(source_stats,f,indent=2)

# Original in-place water-polo prototype clips. Source idle contributes balancing/finger motion;
# leg paths and alternating head-up strokes are authored here, not unrelated walking motions.
bones=rig.pose.bones
def set_bone(name,head,direction):
    bone=bones[name];bone.matrix=Matrix.Translation(head)@direction.to_track_quat('Y','Z').to_matrix().to_4x4()
    bpy.context.view_layer.update()
def limb(upper,lower,end,target,pole,terminal_direction):
    shoulder=bones[upper].head.copy();a=rig.data.bones[upper].length;b=rig.data.bones[lower].length
    delta=target-shoulder;distance=max(.05,min(delta.length,(a+b)*.985));direction=delta.normalized()
    projection=(a*a-b*b+distance*distance)/(2*distance);height=math.sqrt(max(.0001,a*a-projection*projection))
    side=pole-shoulder;side-=direction*side.dot(direction)
    if side.length<.001:side=Vector((1,0,0))
    elbow=shoulder+direction*projection+side.normalized()*height
    wrist=shoulder+direction*distance
    set_bone(upper,shoulder,elbow-shoulder);set_bone(lower,elbow,wrist-elbow);set_bone(end,bones[end].head.copy(),terminal_direction)
def pose(mode,t):
    moving=mode in ['HeadUpSwim','Sprint'];sprint=mode=='Sprint';lean=math.radians(68 if sprint else 60 if moving else 5)
    for b in bones:b.matrix_basis=Matrix.Identity(4)
    if not moving and source_idle:
        base=source_idle[int(t*(len(source_idle)-1))%len(source_idle)]
        for name,matrix in base.items():
            if name not in ['root','pelvis']:bones[name].matrix_basis=matrix
    hips=Vector((0,.043,1.22 if moving else .949+math.sin(t*2*math.pi)*.008))
    rotation=Euler((lean,math.radians(math.sin(t*2*math.pi)*(5 if moving else 2)),0),'XYZ').to_matrix().to_4x4()
    bones['pelvis'].matrix=Matrix.Translation(hips)@rotation@rig.data.bones['pelvis'].matrix_local.to_quaternion().to_matrix().to_4x4();bpy.context.view_layer.update()
    for name,amount in [('neck_01',.55),('Head',.45)]:
        b=bones[name];b.matrix=Matrix.Translation(b.head)@Quaternion((1,0,0),-lean*amount).to_matrix().to_4x4()@b.matrix.to_quaternion().to_matrix().to_4x4();bpy.context.view_layer.update()
    for sign,suffix,phase in [(1,'l',t),(-1,'r',(t+.5)%1)]:
        a=phase*2*math.pi
        if moving:
            if phase<.56:
                u=phase/.56;hand=Vector((sign*(.34+.06*math.sin(u*math.pi)),-.99+.95*u,1.34-.19*math.sin(u*math.pi)))
            else:
                u=(phase-.56)/.44;hand=Vector((sign*(.35+.12*math.sin(u*math.pi)),-.04-.95*u,1.34+.26*math.sin(u*math.pi)))
            foot=Vector((sign*.14,.78, .67+.12*math.sin(4*a)))
        else:
            hand=Vector((sign*(.43+.055*math.sin(a)),-.22+.07*math.cos(a),1.30+.025*math.sin(a)))
            foot=Vector((sign*(.35+.08*math.cos(a)),.04+.15*math.sin(a),.27+.07*math.cos(a)))
        if mode=='Possession' and suffix=='r':hand=Vector((-.18,-.55,1.62))
        if mode in ['Pass','Shoot'] and suffix=='r':
            u=math.sin(math.pi*t);hand=Vector((-.24,-.42-.16*u,1.55+.16*u))
        if mode in ['Block','Save']:
            hand=Vector((sign*(.36+.07*math.sin(a)), -.12,1.91+(.04 if mode=='Save' else 0)*math.sin(math.pi*t)))
        limb('upperarm_'+suffix,'lowerarm_'+suffix,'hand_'+suffix,hand,Vector((sign*.85,-.08,1.7 if moving else 1.17)),Vector((0,-1,-.08)))
        limb('thigh_'+suffix,'calf_'+suffix,'foot_'+suffix,foot,Vector((sign*.60,-.25,.65)),Vector((sign*.12,1 if moving else -.5,-.15)))
def bake(name,duration):
    rig.animation_data_create();action=bpy.data.actions.new(name);rig.animation_data.action=action
    for b in bones:b.rotation_mode='QUATERNION'
    frames=round(duration*30);bpy.context.scene.frame_start=0;bpy.context.scene.frame_end=frames
    for frame in range(frames+1):
        bpy.context.scene.frame_set(frame);pose(name,frame/frames)
        for b in bones:
            b.keyframe_insert('location',frame=frame,group=b.name);b.keyframe_insert('rotation_quaternion',frame=frame,group=b.name);b.keyframe_insert('scale',frame=frame,group=b.name)
    export(os.path.join(out,'Animations',name+'.fbx'),rig,animated=True)
for name,duration in [('TreadWater',2.4),('HeadUpSwim',1.35),('Sprint',.95),('Possession',2.4),('Pass',.45),('Shoot',.55),('Block',1.4),('Save',.55)]:bake(name,duration)
with open(os.path.join(reports,'conversion-summary.json'),'w') as f:json.dump({'bodyVertices':len(body.data.vertices),'bodyTriangles':sum(len(p.vertices)-2 for p in body.data.polygons),'bones':len(rig.data.bones),'clips':8,'sourceClips':2},f,indent=2)
print('EXPORTED',out)
