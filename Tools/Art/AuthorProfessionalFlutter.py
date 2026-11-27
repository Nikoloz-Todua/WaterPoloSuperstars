"""Author and bake the production lower-body flutter in Blender 5.2.

Run on WPS_AquaticMotion_001.blend from the project root. Existing takes
are retained. Unity receives rotational deltas from the unchanged bind rig.
"""
import json
from math import atan2, cos, pi, radians, sin
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[2]
RIG = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
BONES = RIG.pose.bones
SCENE = bpy.context.scene
X = Vector((1, 0, 0))
Y = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
UNITY = Matrix(((-1, 0, 0), (0, 0, 1), (0, -1, 0)))
PARTS = ("pelvis", "thigh_l", "calf_l", "foot_l", "ball_l", "thigh_r", "calf_r", "foot_r", "ball_r")


def aim(bone, end, direction, rest_normal, normal):
    """Solve the direction of the actual joint segment and its axial rotation."""
    bind = bone.bone.matrix_local.to_quaternion()
    baseline = (end.bone.head_local - bone.bone.head_local).normalized()
    direction = direction.normalized()
    alignment = baseline.rotation_difference(direction)
    before = alignment @ rest_normal
    before = (before - direction * before.dot(direction)).normalized()
    after = (normal - direction * normal.dot(direction)).normalized()
    roll = atan2(direction.dot(before.cross(after)), before.dot(after))
    set_pose(bone, Quaternion(direction, roll) @ alignment @ bind)


def set_pose(bone, orientation):
    bone.matrix = Matrix.Translation(bone.head) @ orientation.to_matrix().to_4x4()
    bpy.context.view_layer.update()


def flutter(phase, sprint):
    pelvis = BONES["pelvis"]
    stroke = phase * 2 * pi
    lean = Quaternion(X, radians((83 if sprint else 81) + 1.3 * cos(stroke * 2)))
    plane = Quaternion(lean @ Z, radians((5 if sprint else 6.5) * sin(stroke - .30))) @ lean
    set_pose(pelvis, plane @ pelvis.bone.matrix_local.to_quaternion())

    aft = plane @ -Z
    dorsal = plane @ Y
    across = plane @ X
    for side, suffix, offset in ((1, "l", 0), (-1, "r", pi)):
        beat = 3 * stroke + offset
        hip = radians((7.8 if sprint else 6.5) * (sin(beat) + .13 * sin(2 * beat - .58)))
        flex = radians(6 + (14 if sprint else 11) * (.5 + .5 * sin(beat - 1.05)))
        toe_pitch = radians(11 + (6 if sprint else 4) * sin(beat - 1.62))
        upper = (aft * cos(hip) + dorsal * sin(hip) + across * (side * .028)).normalized()
        pole = (dorsal - upper * dorsal.dot(upper)).normalized()
        lower = (upper * cos(flex) + pole * sin(flex)).normalized()
        shin_back = upper.rotation_difference(lower) @ pole
        toes = lower * cos(toe_pitch) - shin_back * sin(toe_pitch)
        thigh, calf, foot, toe = (BONES[f"{part}_{suffix}"] for part in ("thigh", "calf", "foot", "ball"))
        aim(thigh, calf, upper, Y, pole)
        aim(calf, foot, lower, Y, shin_back)
        # Pointed toes trail the shin. The top of the pointed foot continues the
        # FRONT of the shin; using shin_back here rolls the whole foot inside out.
        aim(foot, toe, toes, Z, -shin_back)
        toe.rotation_mode = "QUATERNION"
        toe.rotation_quaternion = Quaternion()
        bpy.context.view_layer.update()
        axis = toe.matrix.to_quaternion().inverted() @ across
        toe.rotation_quaternion = Quaternion(axis, radians(2 * sin(beat - 1.95)))
        bpy.context.view_layer.update()


def delta(bone):
    rotation = bone.matrix.to_quaternion() @ bone.bone.matrix_local.to_quaternion().inverted()
    q = (UNITY @ rotation.to_matrix() @ UNITY.transposed()).to_quaternion()
    return q


def author(source_name, name, frames, sprint):
    existing = bpy.data.actions.get(name)
    if existing:
        bpy.data.actions.remove(existing)
    action = bpy.data.actions[source_name].copy()
    action.name = name
    action.use_fake_user = True
    RIG.animation_data.action = action
    SCENE.frame_end = frames
    samples = []
    prior = {}
    for frame in range(frames + 1):
        SCENE.frame_set(frame)
        flutter((frame % frames) / frames, sprint)
        for index, name in enumerate(PARTS):
            bone = BONES[name]
            bone.keyframe_insert("rotation_quaternion", frame=frame, group=name)
            q = delta(bone)
            if index in prior and prior[index].dot(q) < 0:
                q.negate()
            prior[index] = q
            samples.append(dict(x=round(q.x, 6), y=round(q.y, 6), z=round(q.z, 6), w=round(q.w, 6)))
    return action, {"frames": frames, "rotations": samples}


def main():
    crawl, normal = author("WaterPoloFreestyle", "WPS_FreestyleLowerBody_Professional", 84, False)
    _, sprint = author("WaterPoloSprint", "WPS_SprintFlutter_Professional", 65, True)
    output = ROOT / "Assets/Resources/WPSFlutterMotion.json"
    output.write_text(json.dumps({"crawl": normal, "sprint": sprint}, separators=(",", ":")), encoding="utf8")
    RIG.animation_data.action = crawl
    SCENE.frame_end = 84
    SCENE.frame_set(0)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath, compress=True)


if __name__ == "__main__":
    main()

