"""Bake editable 60 Hz cycles on the Milestone 27 rig.

Run with the shipped athlete .blend in background Blender. Blender space is
X lateral, -Y forward, Z up. The root is never animated.
"""

from math import cos, pi, radians, sin, sqrt
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "SourceAssets/Athletes/WPS_Animations_001/Blender"
UNITY = ROOT / "Assets/ProductionAthletes/AuthenticMotion"
FPS = 60
X = Vector((1, 0, 0))
Y = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
bones = rig.pose.bones
scene = bpy.context.scene
scene.render.fps = FPS
scene.render.fps_base = 1.0

# Wrist offset from shoulder in metres, with an elbow pole and palm pitch.
# The pull is mostly below the shoulder and the recovery leads with the elbow.
STROKE = (
    (0.00, (.035, -.467, -.055), (.23, -.22, .19), 12),
    (0.12, (.075, -.315, -.243), (.29, -.23, .01), 44),
    (0.26, (.014, -.035, -.357), (.37, -.07, -.13), 63),
    (0.42, (.054, .290, -.291), (.37, .17, -.13), 22),
    (0.53, (.125, .421, -.126), (.30, .22, .12), -12),
    (0.65, (.186, .232, .153), (.31, .11, .32), -35),
    (0.77, (.211, -.062, .202), (.30, -.05, .31), -10),
    (0.89, (.102, -.319, .116), (.30, -.16, .25), 3),
)

# Ankle offsets from hip. Out-kick moves laterally and down, in-kick
# sweeps under the same-side hip and recovery folds under the forward knee.
EGGBEATER = (
    (0.00, (.184, .126, -.518), (.285, -.372, -.207), (.45, -.87, .03)),
    (0.18, (.342, .091, -.533), (.321, -.348, -.228), (.84, -.51, -.13)),
    (0.33, (.397, -.032, -.558), (.308, -.351, -.250), (.78, -.59, -.18)),
    (0.50, (.300, -.164, -.573), (.277, -.386, -.227), (.10, -.95, -.13)),
    (0.69, (.094, -.152, -.556), (.259, -.402, -.180), (-.42, -.89, .03)),
    (0.84, (.083, .016, -.482), (.269, -.391, -.160), (.07, -.99, .23)),
)


def profile(rows, phase):
    """Interpolate a closed Bézier-equivalent spline with timed knots."""
    idx = max(i for i, row in enumerate(rows) if row[0] <= phase)
    a, b, c, d = (rows[(idx + i) % len(rows)] for i in (-1, 0, 1, 2))
    ta = a[0] - (a[0] > b[0])
    tb = b[0]
    tc = c[0] + (c[0] < tb)
    td = d[0] + (d[0] < tc)
    span = tc - tb
    t = (phase - tb) / span
    t2, t3 = t * t, t * t * t
    values = []
    for va, vb, vc, vd in zip(a[1:], b[1:], c[1:], d[1:]):
        if isinstance(va, tuple):
            va, vb, vc, vd = map(Vector, (va, vb, vc, vd))
        slope_b = (vc - va) / (tc - ta)
        slope_c = (vd - vb) / (td - tb)
        values.append((2*t3-3*t2+1)*vb + (t3-2*t2+t)*span*slope_b
                      + (-2*t3+3*t2)*vc + (t3-t2)*span*slope_c)
    return values


def mirrored(v, side):
    return Vector((v.x * side, v.y, v.z))


def set_rotation(bone, rotation):
    bone.matrix = Matrix.Translation(bone.head) @ rotation.to_matrix().to_4x4()
    bpy.context.view_layer.update()


def orient(bone, end, direction, reference=None, plane=None):
    """Aim the real joint segment, using flexion/palm plane to control twist."""
    rest = bone.bone
    baseline = end.bone.head_local - rest.head_local if end else rest.tail_local - rest.head_local
    align = baseline.normalized().rotation_difference(direction.normalized())
    if reference is not None:
        axis = direction.normalized()
        old = align @ reference
        old -= axis * axis.dot(old)
        desired = plane - axis * axis.dot(plane)
        if old.length_squared > 1e-7 and desired.length_squared > 1e-7:
            old.normalize()
            desired.normalize()
            from math import atan2
            align = Quaternion(axis, atan2(axis.dot(old.cross(desired)), old.dot(desired))) @ align
    set_rotation(bone, align @ rest.matrix_local.to_quaternion())


def solve_joint(start, target, pole, first, second):
    aim = target - start
    d = max(abs(first - second) + .008, min(aim.length, first + second - .008))
    axis = aim.normalized()
    lateral = pole - start
    lateral -= axis * lateral.dot(axis)
    along = (first**2 - second**2 + d**2) / (2 * d)
    offset = lateral.normalized() * sqrt(max(0.0, first**2 - along**2))
    return start + axis * along + offset, start + axis * d


def reach(upper, lower, terminal, target, pole, flex, tip, dorsal=None, sole=None):
    shoulder = upper.head.copy()
    first = (lower.bone.head_local - upper.bone.head_local).length
    second = (terminal.bone.head_local - lower.bone.head_local).length
    joint, end = solve_joint(shoulder, target, pole, first, second)
    forward = joint - shoulder
    follow = end - joint
    orient(upper, lower, forward, flex, follow)
    orient(lower, terminal, follow, flex, -forward)
    orient(terminal, None, tip, dorsal, sole)


def position_body(phase, swimming, sprint, holding):
    lean = radians((78 if sprint else 75) if swimming else (5 if holding else 7))
    beat = 2 * pi * phase
    roll = radians((9 if sprint else 11) * sin(beat - .3) if swimming else 1.7 * sin(beat))
    pelvis = bones["pelvis"]
    rotation = Quaternion(X, lean) @ Quaternion(Z, roll * .35)
    bob = (.008 if swimming else .014) * cos(beat * 2)
    pelvis.matrix = (Matrix.Translation(pelvis.bone.head_local + Z * bob)
                     @ (rotation @ pelvis.bone.matrix_local.to_quaternion()).to_matrix().to_4x4())
    bpy.context.view_layer.update()
    chest = bones["spine_03"]
    chest_basis = chest.matrix.to_quaternion()
    set_rotation(chest, Quaternion((rotation @ Z).normalized(), roll * .65) @ chest_basis)
    if swimming:
        neck = bones["neck_01"]
        set_rotation(neck, Quaternion(X, -lean * .55) @ neck.matrix.to_quaternion())
    head = bones["Head"]
    # Stable gaze down the pool, with the neck sharing the extension.
    pitch = radians(9 if swimming else 3)
    set_rotation(head, Quaternion(X, pitch) @ head.bone.matrix_local.to_quaternion())


def arm(side, suffix, phase, swimming, sprint, holding):
    upper, lower, hand = (bones[f"{part}_{suffix}"] for part in ("upperarm", "lowerarm", "hand"))
    start = upper.head.copy()
    if swimming:
        wrist, pole, pitch = profile(STROKE, phase)
        if sprint:
            wrist.y = wrist.y * .94 + .018
            wrist.z *= 1.06
        tangent = Vector((side * .08, -cos(radians(pitch)), -sin(radians(pitch))))
        # At hand exit and early recovery the relaxed hand trails the elbow.
        trail = max(0., sin(pi * min(1., max(0., (phase - .44) / .43)))) ** 1.4
        tangent = tangent.lerp(Vector((side * .05, .28, -.95)), trail)
        dorsal = Vector((side * .20, .38, .88))
        reach(upper, lower, hand, start + mirrored(wrist, side), start + mirrored(pole, side),
              -Y, tangent, Z, dorsal)
        return

    beat = 2 * pi * phase
    target = start + Vector((side * (.19 + .10 * cos(beat)), -.29 - .055 * sin(beat), -.205))
    pole = start + Vector((side * .29, -.045, -.27))
    tip = Vector((side * (.4 + .42 * cos(beat)), -.88, .03 * sin(beat)))
    if holding and side < 0:
        target = start + Vector((-.14, -.26, .19))
        pole = start + Vector((-.30, -.03, -.10))
        tip = Vector((.1, -.2, .98))
    reach(upper, lower, hand, target, pole, -Y, tip, Z,
          Vector((side * .33 * sin(beat), .08, 1)))


def leg(side, suffix, phase, swimming, sprint):
    thigh, calf, foot = (bones[f"{part}_{suffix}"] for part in ("thigh", "calf", "foot"))
    hip = thigh.head.copy()
    if swimming:
        # Three hip waves per leg, with the opposite leg offset half a kick.
        # The shin and ankle lag the hip and extend rearward through each kick.
        beat = 6 * pi * phase
        hip_pitch = radians(-11 + (11 if sprint else 9) * sin(beat))
        knee_bend = radians(10 + (14 if sprint else 12) * (.5 + .5 * sin(beat - .92)))
        thigh_dir = Vector((side * .057, cos(hip_pitch), sin(hip_pitch)))
        shin_pitch = hip_pitch + knee_bend
        shin_dir = Vector((side * .038, cos(shin_pitch), sin(shin_pitch)))
        orient(thigh, calf, thigh_dir, Y, Z)
        orient(calf, foot, shin_dir, Y, Z)
        ankle_pitch = shin_pitch - radians(16 + 6 * sin(beat - 1.47))
        orient(foot, None, Vector((side * .04, cos(ankle_pitch), sin(ankle_pitch))),
               Z, Z)
        return

    ankle, pole, toes = profile(EGGBEATER, phase)
    reach(thigh, calf, foot, hip + mirrored(ankle, side), hip + mirrored(pole, side), Y,
          mirrored(toes, side), Z, Vector((side * -.28 * sin(2*pi*phase), .18, 1.)))


def pose(phase, swimming, sprint, holding):
    for bone in bones:
        bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    position_body(phase, swimming, sprint, holding)
    for side, suffix in ((1, "l"), (-1, "r")):
        offset = (phase + (0 if side > 0 else .5)) % 1.
        arm(side, suffix, offset, swimming, sprint, holding)
        leg(side, suffix, offset, swimming, sprint)


CYCLES = (
    ("WaterPoloFreestyle", 84, True, False, False),
    ("WaterPoloSprint", 65, True, True, False),
    ("WaterPoloEggbeater", 74, False, False, False),
    ("WaterPoloBallTread", 74, False, False, True),
)

SOURCE.mkdir(parents=True, exist_ok=True)
UNITY.mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.object.select_all(action="DESELECT")
rig.hide_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
rig.animation_data_create()
actions = []
for name, frames, swimming, sprint, holding in CYCLES:
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data.action = action
    scene.frame_start = 0
    scene.frame_end = frames
    for bone in bones:
        bone.rotation_mode = "QUATERNION"
    prior = {}
    for frame in range(frames + 1):
        scene.frame_set(frame)
        pose((frame % frames) / frames, swimming, sprint, holding)
        for bone in bones:
            # Quaternion hemisphere continuity avoids interpolating the long way.
            rotation = bone.rotation_quaternion.copy()
            if bone.name in prior and rotation.dot(prior[bone.name]) < 0:
                rotation.negate()
            bone.rotation_quaternion = rotation
            prior[bone.name] = rotation
            if bone.name != "root":
                bone.keyframe_insert("rotation_quaternion", frame=frame, group=bone.name)
        bones["pelvis"].keyframe_insert("location", frame=frame, group="pelvis")
    bpy.ops.export_scene.fbx(
        filepath=str(UNITY / (name + ".fbx")), use_selection=True, object_types={"ARMATURE"},
        add_leaf_bones=False, bake_anim=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False, bake_anim_simplify_factor=.04,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL",
    )
    actions.append(action)
    print("EXPORTED", name, frames / FPS)

rig.animation_data.action = actions[0]
scene.frame_end = CYCLES[0][1]
scene.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "WPS_AquaticMotion_001.blend"))
