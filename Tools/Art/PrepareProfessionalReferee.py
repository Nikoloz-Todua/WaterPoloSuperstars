"""Fit the supplied A-pose official to a separate copy of the shared Humanoid reference."""
from pathlib import Path
import shutil

import bpy
import numpy as np
from mathutils import Matrix, Vector

root = Path(__file__).resolve().parents[2]
source = root / "SourceAssets/Officials/WPS_Referee_001"
output = root / "Assets/Equipment/ProfessionalReferee"
output.mkdir(parents=True, exist_ok=True)
blend_path = source / "Blender/PoolsideOfficial.blend"
blend_path.parent.mkdir(exist_ok=True)
path, = (source / "Original").glob("*.fbx")
rig = bpy.data.objects["Armature"]
rig.animation_data_clear()
for obj in list(bpy.context.scene.objects):
    if obj != rig:
        bpy.data.objects.remove(obj, do_unlink=True)

bpy.ops.import_scene.fbx(filepath=str(path))
official, = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
points = [official.matrix_world @ v.co for v in official.data.vertices]
bottom = min(v.z for v in points)
scale = 1.9 / (max(v.z for v in points) - bottom)
official.name = "ProfessionalOfficial"
official.matrix_world = Matrix.Identity(4)
official.data.name = official.name


def fade(lo, hi, value):
    t = max(0., min(1., (value - lo) / (hi - lo)))
    return t * t * (3. - 2. * t)


def map_axis(p, a, b, c, d):
    source_axis = (b-a).normalized()
    target_axis = (d-c).normalized()
    along = (p-a).dot(source_axis)
    radial = p-a-source_axis*along
    return c + (d-c) * (along/(b-a).length) + source_axis.rotation_difference(target_axis) @ radial


groups = {}
def weight(vertex, name, value):
    if value < .0001:
        return
    if name not in groups:
        groups[name] = official.vertex_groups.new(name=name)
    groups[name].add([vertex], value, "REPLACE")


for vertex, original in zip(official.data.vertices, points):
    p = Vector((original.x * scale, original.y * scale, (original.z-bottom) * scale))
    side = 1 if p.x >= 0 else -1
    suffix = "l" if side > 0 else "r"
    x, y, z = p
    x = abs(x)
    origin = Vector((side*.207, .048, 1.475))
    elbow = Vector((side*.348, .044, 1.258))
    wrist = Vector((side*.460, -.034, 1.018))
    fingers = Vector((side*.523, -.048, .869))
    arm = [f"upperarm_{suffix}", f"lowerarm_{suffix}", f"hand_{suffix}"]
    target = [rig.data.bones[name].head_local for name in arm]
    target.append(rig.data.bones[arm[-1]].tail_local)
    source_arms = [origin, elbow, wrist, fingers]
    sleeve = fade(.221, .311, x) * fade(.78, .84, z) * (1-fade(1.49, 1.60, z))
    elbow_mix = fade(-.065, .065, (p-elbow).dot((wrist-origin).normalized()))
    wrist_mix = fade(-.048, .048, (p-wrist).dot((fingers-elbow).normalized()))
    arm_weights = (1-elbow_mix, elbow_mix*(1-wrist_mix), wrist_mix*elbow_mix)
    if sleeve > 0.:
        mapped = sum((map_axis(p, source_arms[i], source_arms[i+1], target[i], target[i+1])*w
                      for i,w in enumerate(arm_weights)), Vector())
        vertex.co = p.lerp(mapped, sleeve)
    else:
        vertex.co = p

    legs = (1-fade(.74, 1.00, z)) * (1-sleeve)
    calf = 1-fade(.455, .615, z)
    ankle = 1-fade(.079, .183, z)
    for name, value in zip(arm, arm_weights):
        weight(vertex.index, name, sleeve*value)
    weight(vertex.index, f"thigh_{suffix}", legs*(1-calf))
    weight(vertex.index, f"calf_{suffix}", legs*calf*(1-ankle))
    weight(vertex.index, f"foot_{suffix}", legs*calf*ankle)
    torso = 1-sleeve-legs
    gates = [1., fade(.96, 1.12, z), fade(1.10, 1.28, z), fade(1.26, 1.47, z),
             fade(1.47, 1.59, z), fade(1.57, 1.64, z), 0.]
    for i, name in enumerate(("pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "Head")):
        weight(vertex.index, name, torso*(gates[i]-gates[i+1]))

official.parent = rig
official.matrix_parent_inverse = rig.matrix_world.inverted()
official.modifiers.new("Official rig", "ARMATURE").object = rig
official.data.update()
for face in official.data.polygons:
    face.use_smooth = True

for suffix, name in (("", "OfficialColor"), ("_normal", "OfficialNormal")):
    shutil.copyfile(path.with_name(path.stem + suffix + ".png"), output / (name + ".png"))
rough = bpy.data.images.load(str(path.with_name(path.stem + "_roughness.png")))
rough.colorspace_settings.name = "Non-Color"
w, h = rough.size
pixels = np.empty(w*h*4, dtype=np.float32)
rough.pixels.foreach_get(pixels)
pixels = pixels.reshape(h, w, 4)
pixels[..., 3] = np.clip(1-pixels[..., 0], .12, .36)
pixels[..., :3] = 0
image = bpy.data.images.new("OfficialSmoothness", w, h, alpha=True)
image.colorspace_settings.name = "Non-Color"
image.pixels.foreach_set(pixels.reshape(-1))
image.file_format = "PNG"
image.filepath_raw = str(output / "OfficialSmoothness.png")
image.save()

official.data.materials.clear()
material = bpy.data.materials.new("Official uniform and skin")
material.use_nodes = True
nodes = material.node_tree.nodes
shader = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
base = nodes.new("ShaderNodeTexImage")
base.image = bpy.data.images.load(str(output / "OfficialColor.png"))
base.image.pack()
material.node_tree.links.new(base.outputs["Color"], shader.inputs["Base Color"])
shader.inputs["Roughness"].default_value = .69
official.data.materials.append(material)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
official.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(output / "PoolsideOfficial.fbx"), use_selection=True,
    object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False,
    axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE")
