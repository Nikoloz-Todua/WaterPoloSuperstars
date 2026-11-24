"""Fit the supplier's 2,002-triangle shell and audit the existing body coverage cut."""
from pathlib import Path

import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

root = Path(__file__).resolve().parents[2]
source = root / "SourceAssets/Athletes/WPS_Swimwear_001"
folder = source / "Blender"
folder.mkdir(exist_ok=True)
path, = (source / "Original").glob("*.fbx")
rig = bpy.data.objects["Armature"]
reference = bpy.data.objects["CompetitionBriefs"]
skin = bpy.data.objects["AthleteBody"]
motion = rig.animation_data.action
rig.animation_data.action = None
for bone in rig.pose.bones:
    bone.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()

raw_meshes = set(bpy.context.scene.objects)
bpy.ops.import_scene.fbx(filepath=str(path))
garment, = [obj for obj in bpy.context.scene.objects if obj not in raw_meshes and obj.type == "MESH"]
garment.name = "PremiumCompetitionBriefs_Candidate"
rest_points = [reference.matrix_world @ v.co for v in reference.data.vertices]
rest_triangles = [tuple(p.vertices) for p in reference.data.polygons]
transfer = BVHTree.FromPolygons(rest_points, rest_triangles, all_triangles=True)
source_points = [garment.matrix_world @ v.co for v in garment.data.vertices]
garment.matrix_world = Matrix.Identity(4)
groups = {}
for vertex, src in zip(garment.data.vertices, source_points):
    # Match the material envelope while retaining the A/B garment's two
    # surfaces, raised waist seam, openings and source loop UVs.
    point = Vector((src.x * .895, src.y * .835 + .013, src.z * .928 + .919))
    hit, normal, face, _ = transfer.find_nearest(point)
    vertex.co = point.lerp(hit, .74) + normal * .0035
    indices = rest_triangles[face]
    weights = barycentric_transform(hit, *(rest_points[i] for i in indices),
        Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    influence = {}
    for i, index in enumerate(indices):
        for group in reference.data.vertices[index].groups:
            name = reference.vertex_groups[group.group].name
            influence[name] = influence.get(name, 0.) + group.weight * max(0., weights[i])
    selected = sorted((item for item in influence.items() if item[1] > .001),
                      key=lambda item: item[1], reverse=True)[:4]
    total = sum(value for _, value in selected)
    for name, value in selected:
        if name not in groups:
            groups[name] = garment.vertex_groups.new(name=name)
        groups[name].add([vertex.index], value / total, "REPLACE")

garment.parent = rig
garment.matrix_parent_inverse = rig.matrix_world.inverted()
garment.modifiers.new("Atlas Humanoid", "ARMATURE").object = rig
garment.data.update()
garment.data.calc_loop_triangles()
assert all(v.groups for v in garment.data.vertices)
print("Candidate geometry", len(garment.data.vertices), "vertices", len(garment.data.loop_triangles), "triangles",
      sum(p.area < 1e-10 for p in garment.data.polygons), "degenerate faces")


def mesh_points(obj, graph):
    evaluated = obj.evaluated_get(graph)
    mesh = evaluated.data
    points = [evaluated.matrix_world @ v.co for v in mesh.vertices]
    faces = [tuple(p.vertices) for p in mesh.polygons]
    return points, faces


def audit(label):
    graph = bpy.context.evaluated_depsgraph_get()
    worn, triangles = mesh_points(garment, graph)
    shell = BVHTree.FromPolygons(worn, triangles)
    old, panels = mesh_points(reference, graph)
    exposed, triangles = mesh_points(skin, graph)
    skin_surface = BVHTree.FromPolygons(exposed, triangles)
    missing = 0
    area = 0.
    missing_area = 0.
    for face in panels:
        a, b, c = [old[idx] for idx in face]
        centre = (a+b+c)/3.
        cross = (b-a).cross(c-a)
        area += cross.length * .5
        normal = cross.normalized()
        hit, _, _, _ = shell.ray_cast(centre+normal*.030, -normal, .060)
        if hit is None:
            missing += 1
            missing_area += cross.length*.5
    print(label, "masked surface misses", missing, "of", len(panels),
          "estimated area %", round(100*missing_area/area, 1), "skin overlap pairs", len(shell.overlap(skin_surface)))


audit("Stand")
for name, frames in (("WaterPoloFreestyle", (0, 18, 36, 54, 72)),
                     ("WaterPoloSprint", (0, 16, 32, 48)),
                     ("WaterPoloEggbeater", (0, 18, 36, 54)),
                     ("WaterPoloBallTread", (0, 18, 36, 54))):
    rig.animation_data.action = bpy.data.actions.get(name)
    if rig.animation_data.action is None:
        continue
    for frame in frames:
        bpy.context.scene.frame_set(frame)
        audit(f"{name} frame {frame}")

rig.animation_data.action = motion
if motion is not None:
    bpy.context.scene.frame_set(0)
reference.hide_set(True)
reference.hide_render = True
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(folder / "PremiumCompetitionBriefs_FitCandidate.blend"))
