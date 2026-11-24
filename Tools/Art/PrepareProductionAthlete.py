"""Build the athlete interchange meshes and editable Blender source in a clean process."""

import json
import math
import shutil
from collections import Counter
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "SourceAssets/Athletes/WPS_Athlete_001"
ASSETS = ROOT / "Assets/ProductionAthletes"
LOGS = ROOT / "Logs/ProductionAthletes"
for directory in (SOURCE / "Blender", ASSETS / "Models", ASSETS / "Textures", LOGS):
    directory.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
source_path = next((SOURCE / "Original").glob("*.fbx"))
bpy.ops.import_scene.fbx(filepath=str(source_path))
source = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
source.data.calc_loop_triangles()
points = [source.matrix_world @ vertex.co for vertex in source.data.vertices]
faces = [tuple(triangle.vertices) for triangle in source.data.loop_triangles]
uvs = [tuple(source.data.uv_layers.active.data[index].uv) for index in range(len(source.data.loops))]
surface = BVHTree.FromPolygons(points, faces, all_triangles=True)

before = set(bpy.context.scene.objects)
bpy.ops.import_scene.gltf(filepath=str(ROOT / "SourceAssets/SwimmerAnimation/character_ual.glb"))
imported = set(bpy.context.scene.objects) - before
rig = next(obj for obj in imported if obj.type == "ARMATURE")
for obj in imported - {rig}:
    bpy.data.objects.remove(obj, do_unlink=True)

def smooth(lo, hi, value):
    t = max(0., min(1., (value - lo) / (hi - lo)))
    return t * t * (3. - 2. * t)

def v(x, y, z):
    return Vector((x, y, z))

def bone(name):
    return rig.matrix_world @ rig.data.bones[name].head_local

# Material-space maps are copied from the developer export. Data maps are sampled in linear space.
def source_image(suffix, linear=False):
    path = source_path.with_name(source_path.stem + suffix + ".png")
    image = bpy.data.images.load(str(path))
    if linear:
        image.colorspace_settings.name = "Non-Color"
    image.pack()
    return image

albedo = source_image("")
normal = source_image("_normal", True)
metal = source_image("_metallic", True)
rough = source_image("_roughness", True)
pixels = np.zeros((2048 * 2048, 4), dtype=np.float32)
metal.pixels.foreach_get(pixels.reshape(-1))
metal_min, metal_max = float(pixels[:, 0].min()), float(pixels[:, 0].max())
rough.pixels.foreach_get(pixels.reshape(-1))
rough_min, rough_max = float(pixels[:, 0].min()), float(pixels[:, 0].max())
smoothness = np.clip(1. - pixels[:, 0], .18, .52)
pixels[:, :3] = 0.  # The supplied metallic map is verified below. Skin uses a dielectric workflow.
pixels[:, 3] = smoothness
packed = bpy.data.images.new("Skin Metallic-Smoothness", 2048, 2048, alpha=True)
packed.colorspace_settings.name = "Non-Color"
packed.pixels.foreach_set(pixels.reshape(-1))
packed.file_format = "PNG"
packed.filepath_raw = str(ASSETS / "Textures/SkinMetallicSmoothness.png")
packed.save()
packed.pack()
for suffix, name in (("", "SkinAlbedo.png"), ("_normal", "SkinNormal.png")):
    shutil.copyfile(source_path.with_name(source_path.stem + suffix + ".png"), ASSETS / "Textures" / name)

def material(name, color, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.)
    mat.use_nodes = True
    shader = next(node for node in mat.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (*color, 1.)
    shader.inputs["Roughness"].default_value = roughness
    return mat, shader

skin, skin_shader = material("Skin", (.55, .31, .21), .56)
nodes = skin.node_tree.nodes
links = skin.node_tree.links
base_node = nodes.new("ShaderNodeTexImage")
base_node.image = albedo
links.new(base_node.outputs["Color"], skin_shader.inputs["Base Color"])
rough_node = nodes.new("ShaderNodeTexImage")
rough_node.image = rough
links.new(rough_node.outputs["Color"], skin_shader.inputs["Roughness"])
normal_node = nodes.new("ShaderNodeTexImage")
normal_node.image = normal
normal_map = nodes.new("ShaderNodeNormalMap")
normal_map.inputs["Strength"].default_value = .65
links.new(normal_node.outputs["Color"], normal_map.inputs["Color"])
links.new(normal_map.outputs["Normal"], skin_shader.inputs["Normal"])
fabric, _ = material("Competition briefs", (.018, .075, .32), .72)
cloth, _ = material("Cap fabric", (.022, .11, .48), .68)
trim, _ = material("Binding and ear guard", (.045, .19, .65), .55)
numbers, _ = material("Cap number print region", (.8, .82, .86), .72)

# Source landmarks in metres. The target rest hierarchy stays identical to the shipped
# Humanoid so its Avatar, procedural joints and FK/IK positions remain authoritative.
source_arm = (v(.205, .030, 1.475), v(.351, .018, 1.242),
              v(.480, -.024, 1.045), v(.565, -.057, .839))
source_leg = (v(.115, .006, .940), v(.157, .009, .545),
              v(.223, .063, .105), v(.237, -.121, .027))

def mirrored(point, sign):
    return v(point.x * sign, point.y, point.z)

def axis_mapping(point, origin, end, destination, target_end):
    src = end - origin
    dst = target_end - destination
    axis = src.normalized()
    along = (point - origin).dot(axis)
    radial = point - origin - axis * along
    return destination + dst * (along / src.length) + axis.rotation_difference(dst.normalized()) @ radial

def limb(point, sign, landmarks, names, blend_ranges):
    joint = [mirrored(pos, sign) for pos in landmarks]
    target = [bone(name) for name in names]
    levels = [(point - joint[i]).dot((joint[i] - joint[i-1]).normalized()) for i in (1, 2)]
    first = smooth(*blend_ranges[0], levels[0])
    second = smooth(*blend_ranges[1], levels[1])
    shares = (1. - first, first * (1. - second), second * first)
    posed = sum((axis_mapping(point, joint[i], joint[i+1], target[i], target[i+1]) * shares[i]
                 for i in range(3)), v(0, 0, 0))
    return posed, dict(zip(names, shares))

def bind_point(point):
    x, y, z = point
    sign = 1 if x >= 0 else -1
    side = "l" if sign == 1 else "r"
    ax = abs(x)
    torso = v(x, y * .98 + .012, z)
    torso += v(0, .009, -.021) * smooth(1.43, 1.69, z)
    inner_arm = .174 + (1. - smooth(1.28, 1.35, z)) * .025 + (1. - smooth(1.06, 1.24, z)) * .045
    arm = smooth(inner_arm, inner_arm + .042, ax) * smooth(.74, .83, z)
    # Below the armpit, restrict the arm weights to the disconnected limb.
    arm *= 1. - smooth(1.525, 1.60, z)
    leg = (1. - smooth(.81, 1.015, z)) * smooth(.015, .10, ax)
    if z < .80:
        leg = max(leg, 1. - smooth(.74, .80, z))
    hips_to_back = smooth(.99, 1.14, z)
    lower_to_chest = smooth(1.10, 1.29, z)
    upper_chest = smooth(1.25, 1.42, z)
    to_neck = smooth(1.49, 1.62, z)
    to_head = smooth(1.59, 1.68, z)
    torso_names = ("pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "Head")
    gates = (1., hips_to_back, lower_to_chest, upper_chest, to_neck, to_head, 0.)
    base = 1. - max(arm, leg)
    weights = {name: base * (gates[i] - gates[i+1]) for i, name in enumerate(torso_names)}
    result = torso * base
    if arm > 0.:
        arm_pose, arm_weights = limb(point, sign, source_arm,
             ["upperarm_" + side, "lowerarm_" + side, "hand_" + side, "middle_03_" + side],
             ((-.055, .055), (-.030, .024)))
        result += arm_pose * arm
        for name, weight in arm_weights.items():
            weights[name] = weight * arm
    leg *= 1. - arm
    if leg > 0.:
        leg_pose, leg_weights = limb(point, sign, source_leg,
              ["thigh_" + side, "calf_" + side, "foot_" + side, "ball_leaf_" + side],
              ((-.070, .068), (-.065, .060)))
        result += leg_pose * leg
        for name, weight in leg_weights.items():
            weights[name] = weight * leg
    best = sorted(((name, weight) for name, weight in weights.items() if weight > .0001),
                  key=lambda entry: entry[1], reverse=True)[:4]
    total = sum(weight for name, weight in best)
    return result, [(name, weight / total) for name, weight in best]

def build_mesh(name, coords, triangles, loop_uvs, materials, ids, weighting, smooth_shading=True):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(coords, [], triangles)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if loop_uvs:
        layer = mesh.uv_layers.new(name="UVMap")
        for polygon, values in zip(mesh.polygons, loop_uvs):
            for index, value in zip(polygon.loop_indices, values):
                layer.data[index].uv = value
    for mat in materials:
        mesh.materials.append(mat)
    for polygon, index in zip(mesh.polygons, ids):
        polygon.material_index = index
        polygon.use_smooth = smooth_shading
    groups = {}
    for index, influences in enumerate(weighting):
        for name, strength in influences:
            if name not in groups:
                groups[name] = obj.vertex_groups.new(name=name)
            groups[name].add([index], strength, "REPLACE")
    obj.parent = rig
    obj.matrix_parent_inverse = rig.matrix_world.inverted()
    modifier = obj.modifiers.new("Shipped swimmer skeleton", "ARMATURE")
    modifier.object = rig
    return obj

def garment_fields(point):
    x, y, z = point
    side = smooth(.018, .199, abs(x))
    back = smooth(-.045, .095, y)
    bottom = .804 + .168 * side + .008 * back * (1. - side)
    top = 1.037 - .017 * smooth(.02, .21, abs(x)) * (1. - back)
    return z - bottom, top - z, .29 - abs(x)

def garment_uv(point):
    x, y, z = point
    angle = math.atan2(x, -y + .022)
    return ((angle / (2. * math.pi)) % 1., (z - .74) / .315)

class MeshRegion:
    def __init__(self):
        self.coords, self.triangles, self.uvs, self.weights = [], [], [], []
        self.indices = {}

    def add(self, corners, clothing):
        if len(corners) < 3:
            return
        indices = []
        triangle_uv = []
        for key, point, normal, uv in corners:
            if key not in self.indices:
                self.indices[key] = len(self.coords)
                mapped, weights = bind_point(point)
                # Move the fabric outward slightly, including at the shared cut boundary.
                if clothing:
                    mapped += normal * (.0015 * smooth(0., .012, min(garment_fields(point))))
                self.coords.append(mapped)
                self.weights.append(weights)
            indices.append(self.indices[key])
            triangle_uv.append(garment_uv(point) if clothing else uv)
        if clothing and max(uv[0] for uv in triangle_uv) - min(uv[0] for uv in triangle_uv) > .5:
            triangle_uv = [(u + 1. if u < .5 else u, v0) for u, v0 in triangle_uv]
        for i in range(1, len(indices) - 1):
            triangle = (indices[0], indices[i], indices[i+1])
            if len(set(triangle)) == 3:
                self.triangles.append(triangle)
                self.uvs.append((triangle_uv[0], triangle_uv[i], triangle_uv[i+1]))

def split_polygon(corners, field):
    outside, inside = [], []
    for i, b in enumerate(corners):
        a = corners[i-1]
        da, db = garment_fields(a[1])[field], garment_fields(b[1])[field]
        if (da >= 0.) != (db >= 0.):
            fraction = da / (da - db)
            # Both triangles adjacent to an edge must resolve its cut to the same node.
            key = (field, *sorted((a[0], b[0]), key=str))
            new_uv = tuple(x + (y - x) * fraction for x, y in zip(a[3], b[3]))
            corner = (key, a[1].lerp(b[1], fraction), a[2].lerp(b[2], fraction).normalized(), new_uv)
            outside.append(corner)
            inside.append(corner)
        (inside if db >= 0. else outside).append(b)
    return outside, inside

body = MeshRegion()
briefs = MeshRegion()
for triangle in source.data.loop_triangles:
    corners = [(i, points[i], source.matrix_world.to_3x3() @ source.data.vertices[i].normal, uvs[loop])
               for i, loop in zip(triangle.vertices, triangle.loops)]
    for field in (2, 1, 0):
        outside, corners = split_polygon(corners, field)
        body.add(outside, False)
        if not corners:
            break
    briefs.add(corners, True)

# Relax the rest-pose shoulder transition only. Preserve UVs, garment boundaries
# and the source sculpt over the face and torso.
adjacent = [set() for _ in body.coords]
for a, b, c in body.triangles:
    adjacent[a].update((b, c))
    adjacent[b].update((a, c))
    adjacent[c].update((a, b))
relaxation = []
for key, index in body.indices.items():
    if isinstance(key, int) and 1.31 < points[key].z < 1.60:
        x, _, z = points[key]
        factor = .48 * smooth(.12, .20, abs(x)) * (1. - smooth(.29, .34, abs(x)))
        factor *= smooth(1.31, 1.42, z) * (1. - smooth(1.53, 1.60, z))
        if factor > .01:
            relaxation.append((index, factor))
for _ in range(6):
    changes = [(i, body.coords[i].lerp(sum((body.coords[j] for j in adjacent[i]), v(0, 0, 0)) /
                                      len(adjacent[i]), factor)) for i, factor in relaxation if adjacent[i]]
    for index, position in changes:
        body.coords[index] = position

objects = [
    build_mesh("AthleteBody", body.coords, body.triangles, body.uvs, [skin],
               [0]*len(body.triangles), body.weights),
    build_mesh("CompetitionBriefs", briefs.coords, briefs.triangles, briefs.uvs, [fabric],
               [0]*len(briefs.triangles), briefs.weights),
]

class HeadMesh:
    def __init__(self):
        self.coords, self.faces, self.uvs, self.materials = [], [], [], []

    def grid(self, rows, material, reverse=False, closed=False):
        width = len(rows[0])
        start = len(self.coords)
        self.coords.extend(bind_point(point)[0] for row in rows for point in row)
        for r in range(len(rows) - 1):
            for j in range(width if closed else width - 1):
                k = (j + 1) % width
                a, b = start + r*width + j, start + r*width + k
                face = (a, b, start + (r+1)*width + k, start + (r+1)*width + j)
                u0, u1 = j/(width if closed else width-1), (j+1)/(width if closed else width-1)
                uv = ((u0, r/(len(rows)-1)), (u1, r/(len(rows)-1)),
                      (u1, (r+1)/(len(rows)-1)), (u0, (r+1)/(len(rows)-1)))
                self.faces.append(tuple(reversed(face)) if reverse else face)
                self.uvs.append(tuple(reversed(uv)) if reverse else uv)
                self.materials.append(material)

    def create(self, name, materials):
        return build_mesh(name, self.coords, self.faces, self.uvs, materials, self.materials,
                          [[("Head", 1.)]]*len(self.coords))

head_centre = v(0, .020, 1.771)
def fitted(direction, offset):
    hit, normal, _, _ = surface.ray_cast(head_centre, direction.normalized())
    if hit is None:
        raise ValueError(f"Head surface missed along {direction}")
    return hit + direction.normalized() * offset

def head_edge(angle):
    front = max(0., math.cos(angle))
    rear = max(0., -math.cos(angle))
    return .02 + .30 * front**2 - .43 * rear**1.4

def shell_point(angle, elevation, offset=.008):
    return fitted(v(math.sin(angle)*math.cos(elevation), -math.cos(angle)*math.cos(elevation),
                    math.sin(elevation)), offset)

cap = HeadMesh()
segments = 36
angles = [2.*math.pi*j/segments for j in range(segments)]
rows = [[shell_point(a, head_edge(a)-.045, .005) for a in angles]]
rows += [[shell_point(a, head_edge(a)+.042+(1.559-head_edge(a)-.042)*i/7.) for a in angles] for i in range(8)]
cap.grid(rows, 0, closed=True)
cap.materials[:segments] = [1] * segments
# One shared crown vertex avoids a pinhole and overlapping pole triangles.
pole = len(cap.coords)
cap.coords.append(bind_point(shell_point(0., math.pi/2))[0])
for j in range(segments):
    cap.faces.append((8*segments+j, 8*segments+(j+1)%segments, pole))
    cap.uvs.append(((j/segments, .985), ((j+1)/segments, .985), ((j+.5)/segments, 1.)))
    cap.materials.append(0)

# Molded external ear guards with a rolled lip and seven genuine open ports per side.
# A polar grid supplies the front plate, port walls and cup profile in one mesh.
def guards(sign):
    cy, cz = .023, 1.753
    def layer(radius, depth):
        return [v(sign*(.091+depth), cy + .045*radius*math.cos(j*math.tau/24),
                  cz + .047*radius*math.sin(j*math.tau/24)) for j in range(24)]
    for profile in (((1., -.011), (1., .004), (.91, .016), (.76, .024)),
                    ((.76, .024), (.64, .027), (.36, .031))):
        start = len(cap.faces)
        cap.grid([layer(r, d) for r, d in profile], 1, reverse=sign<0, closed=True)
        if profile[0][0] < 1.:
            # Leave six narrow slots in the raised plate, spaced around the centre.
            for index in reversed([start + j for j in (1, 5, 9, 13, 17, 21)]):
                del cap.faces[index], cap.uvs[index], cap.materials[index]
    cap.grid([layer(.36, .031), layer(.19, .031), layer(.14, .023)], 1, reverse=sign<0, closed=True)
    # Front and rear strap paths converge underneath the jaw.
    strap = [v(sign*.096, .000, 1.719)]
    for x, y, z in ((.083, -.018, 1.700), (.067, -.039, 1.683),
                    (.046, -.064, 1.669), (.022, -.074, 1.658), (0., -.076, 1.655)):
        hit, normal, _, _ = surface.find_nearest(v(sign*x, y, z))
        strap.append(hit + normal*.0035)
    cap.grid([[p+v(0, -.0033, .003), p+v(0, .0033, -.0025)] for p in strap], 1, reverse=sign>0)
    cap.grid([[p+v(0, .0033, -.0025), p+v(0, -.0033, .003)] for p in strap], 1, reverse=sign>0)
for sign in (-1, 1):
    guards(sign)
# Unity preserves FBX submesh order by first polygon use.
for values in (cap.faces, cap.uvs, cap.materials):
    values.insert(0, values.pop(segments))
objects.append(cap.create("WaterPoloCap", [cloth, trim]))

print_region = HeadMesh()
for sign in (-1, 1):
    # Increasing UV U follows the viewer's right from each outward side.
    azimuth = sign * 1.28
    print_region.grid([[shell_point(azimuth + (j/8.-.5)*.87,
                                    .43 + (i/6.)*.48, .0113)
                        for j in range(9)] for i in range(7)], 0)
objects.append(print_region.create("CapNumber", [numbers]))

# Keep the unmodified body in a hidden, explicitly named authoring collection.
archive = bpy.data.collections.new("Untouched Meshy import - hidden from export")
bpy.context.scene.collection.children.link(archive)
for collection in list(source.users_collection):
    collection.objects.unlink(source)
archive.objects.link(source)
archive.hide_viewport = True
archive.hide_render = True

bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
for obj in objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(ASSETS / "Models/Athlete001.fbx"), use_selection=True,
    object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False,
    axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL",
    mesh_smooth_type="FACE", use_mesh_modifiers=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "Blender/WPS_Athlete_001.blend"))

report = {"blender": bpy.app.version_string, "sourceVertices": len(points), "sourceTriangles": len(faces),
          "sourceMetallic": [metal_min, metal_max], "sourceRoughness": [rough_min, rough_max],
          "smoothness": [float(smoothness.min()), float(smoothness.max())],
          "bones": len(rig.data.bones), "meshes": {}}
for obj in objects:
    mesh = obj.data
    mesh.calc_loop_triangles()
    report["meshes"][obj.name] = {"triangles": len(mesh.loop_triangles), "vertices": len(mesh.vertices),
                                  "influenceCounts": dict(Counter(len(vertex.groups) for vertex in mesh.vertices)),
                                  "degenerateFaces": sum(poly.area < 1e-10 for poly in mesh.polygons)}
(LOGS / "build-report.json").write_text(json.dumps(report, indent=2))
print(json.dumps(report))

