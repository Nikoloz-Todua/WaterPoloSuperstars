"""Author editable walk and stand Actions on the production athlete in Blender 5.2."""
import json
from math import cos, pi, radians, sin, sqrt
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from AuthorProfessionalFlutter import BONES, RIG, ROOT, SCENE, X, Y, Z, aim, set_pose, bpy, Matrix, Quaternion, Vector

STRIDE = 1.28
SUPPORT = .61
WALK_FRAMES = 66
STAND_FRAMES = 150


def ease(x):
    x = min(1., max(0., x))
    return x*x*(3-2*x)


def contact(phase):
    phase %= 1.
    front = STRIDE * SUPPORT * .5
    if phase < SUPPORT:
        distance = front - STRIDE * phase
        pitch = 7.5 * (1 - ease(phase/.115)) - 21 * ease((phase - .41)/(SUPPORT - .41))
        # The heel rolls off the floor while the toe pad stays down.
        return distance, 0., pitch, max(0., -pitch) * .91

    t = (phase - SUPPORT) / (1 - SUPPORT)
    # Match the stance velocity briefly at each end of the swing to avoid a reversal snap.
    distance = -front + 2*front*ease(t) - STRIDE*(1-SUPPORT)*t*(1-t)*(1-2*t)
    lift = .091 * sin(pi*t)**1.35
    pitch = -21 + 28.5*ease(t)
    release = 19.11 * (1 - ease(t/.34))
    return distance, lift, pitch, release


def deck_foot(side, phase, walking):
    thigh, calf, foot, toe = (BONES[f"{name}_{side}"] for name in ("thigh", "calf", "foot", "ball"))
    forward, lift, pitch, extension = contact(phase) if walking else (0., 0., 0., 0.)
    foot_rot = Quaternion(X, radians(-pitch))
    back = Vector((0, .049, -foot.bone.head_local.z))
    tip = toe.bone.head_local - foot.bone.head_local - Z * toe.bone.head_local.z
    heel, ball = foot_rot @ back, foot_rot @ tip
    pivot = back - heel if pitch >= 0 else tip - ball
    point = foot.bone.head_local.copy()
    point.x = .102 if side == "l" else -.102
    point.y -= forward
    point.xy += pivot.xy
    point.z = -min(heel.z, ball.z) + lift
    return thigh, calf, foot, toe, point, foot_rot, extension


def leg_reach(leg):
    thigh, calf, foot, toe, point, orientation, extension = leg
    axis = point - thigh.head
    first = (calf.bone.head_local - thigh.bone.head_local).length
    second = (foot.bone.head_local - calf.bone.head_local).length
    distance = min(first + second - .009, max(abs(first - second) + .008, axis.length))
    axis.normalize()
    front = -Y + axis * axis.dot(Y)
    along = (first*first - second*second + distance*distance) / (2*distance)
    knee = thigh.head + axis*along + front.normalized()*sqrt(max(0., first*first - along*along))
    aim(thigh, calf, knee - thigh.head, -Y, -Y)
    aim(calf, foot, point - knee, -Y, -Y)
    set_pose(foot, orientation @ foot.bone.matrix_local.to_quaternion())
    toe_axis = toe.matrix.to_quaternion().inverted() @ X
    toe.rotation_quaternion = Quaternion(toe_axis, radians(-extension))


def upper_body(phase, walking):
    h = 2*pi*phase
    pelvis, torso, head = (BONES[n] for n in ("pelvis", "spine_03", "Head"))
    yaw = radians((4.4 if walking else .7)*cos(h - .12))
    lean = radians(2.3 if walking else .65)
    roll = radians((2.2 if walking else .75)*sin(h - .18))
    centre = pelvis.bone.head_local + X * ((.026 if walking else .012)*sin(h - .27)) - Y * .014
    centre.z += (-.036 - .012*cos(2*h - .26)) if walking else (-.018 + .003*cos(h))
    hips = Quaternion(Z, yaw) @ Quaternion(Y, roll) @ Quaternion(X, lean)
    pelvis.matrix = Matrix.Translation(centre) @ (hips @ pelvis.bone.matrix_local.to_quaternion()).to_matrix().to_4x4()
    bpy.context.view_layer.update()
    set_pose(torso, Quaternion(Z, -yaw*.58) @ Quaternion(X, lean*.36) @ torso.bone.matrix_local.to_quaternion())
    set_pose(head, Quaternion(Z, yaw*.12) @ head.bone.matrix_local.to_quaternion())
    for sign, side in ((1, "l"), (-1, "r")):
        upper, fore, hand = (BONES[f"{part}_{side}"] for part in ("upperarm", "lowerarm", "hand"))
        swing = sign * (.27*cos(h - .26) if walking else .017*sin(h))
        aim(upper, fore, Vector((sign*.115, swing, -1)), -Y, -Y)
        aim(fore, hand, Vector((sign*.045, swing*.74 - .14, -1)), -Y, -Y)
        set_pose(hand, fore.matrix.to_quaternion() @ fore.bone.matrix_local.to_quaternion().inverted()
                 @ hand.bone.matrix_local.to_quaternion())


def author(name, frames, walking):
    existing = bpy.data.actions.get(name)
    if existing:
        bpy.data.actions.remove(existing)
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    RIG.animation_data.action = action
    SCENE.frame_end = frames
    for frame in range(frames + 1):
        SCENE.frame_set(frame)
        phase = (frame % frames) / frames
        for bone in BONES:
            bone.rotation_mode = "QUATERNION"
            bone.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        upper_body(phase, walking)
        for side, offset in (("l", 0.), ("r", .5)):
            leg_reach(deck_foot(side, phase + offset, walking))

        for bone in BONES:
            if bone.name != "root":
                bone.keyframe_insert("rotation_quaternion", frame=frame, group=bone.name)
        BONES["pelvis"].keyframe_insert("location", frame=frame, group="pelvis")
    bpy.ops.export_scene.fbx(filepath=str(ROOT / f"Assets/Resources/{name}.fbx"), use_selection=True,
        object_types={"ARMATURE"}, add_leaf_bones=False, bake_anim=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False, bake_anim_simplify_factor=0., axis_forward="-Z", axis_up="Y",
        apply_scale_options="FBX_SCALE_ALL")
    return action


def main():
    bpy.ops.object.select_all(action="DESELECT")
    RIG.hide_set(False)
    RIG.select_set(True)
    bpy.context.view_layer.objects.active = RIG
    walk = author("WPSDeckWalk", WALK_FRAMES, True)
    author("WPSDeckStand", STAND_FRAMES, False)
    data, toe_extension = [], []
    for i in range(WALK_FRAMES + 1):
        d, lift, pitch, toe = contact(i/WALK_FRAMES)
        data.append(dict(x=round(d, 6), y=round(lift, 6), z=round(pitch, 6)))
        toe_extension.append(round(toe, 6))
    (ROOT / "Assets/Resources/WPSDeckContacts.json").write_text(
        json.dumps({"stride": STRIDE, "support": SUPPORT, "samples": data, "toeExtension": toe_extension},
                   separators=(",", ":")), encoding="utf8")
    RIG.animation_data.action = walk
    SCENE.frame_end = WALK_FRAMES
    SCENE.frame_set(0)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath, compress=True)


if __name__ == "__main__":
    main()

