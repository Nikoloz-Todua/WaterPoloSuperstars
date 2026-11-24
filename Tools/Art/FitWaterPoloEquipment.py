"""Fit the premium cap to the shipped head and adapt briefs textures to its coverage cut.

Run with the Milestone 27 athlete .blend in background Blender. Its head and
garment boundary are the fit reference. The shipped file is read only.
"""

from math import cos, pi, sin
from pathlib import Path
import shutil

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

ROOT = Path(__file__).resolve().parents[2]
ATHLETES = ROOT / "SourceAssets/Athletes"
SOURCE = ATHLETES / "WPS_Cap_001" / "Blender"
ASSETS = ROOT / "Assets/ProductionAthletes/AuthenticEquipment"
TEXTURES = ASSETS / "Textures"
for folder in (SOURCE, TEXTURES):
    folder.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene
rig = bpy.data.objects["Armature"]
body = bpy.data.objects["AthleteBody"]
body_points = [body.matrix_world @ vertex.co for vertex in body.data.vertices]
body_faces = [tuple(p.vertices) for p in body.data.polygons]
head_faces = [face for face in body_faces if all(body_points[i].z > 1.577 for i in face)]
head = BVHTree.FromPolygons(body_points, head_faces, all_triangles=True)


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def read_equipment(package):
    path, = (ATHLETES / package / "Original").glob("*.fbx")
    before = set(scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    original, = [obj for obj in set(scene.objects) - before if obj.type == "MESH"]
    return original, path


def read_map(path, suffix, data=False):
    image = bpy.data.images.load(str(path.with_name(path.stem + suffix + ".png")))
    if data:
        image.colorspace_settings.name = "Non-Color"
    pixels = np.empty(image.size[0] * image.size[1] * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    return image, pixels.reshape(image.size[1], image.size[0], 4)


def save_map(name, pixels, data=False):
    height, width = pixels.shape[:2]
    image = bpy.data.images.new(name, width, height, alpha=True)
    if data:
        image.colorspace_settings.name = "Non-Color"
    image.pixels.foreach_set(np.asarray(pixels, dtype=np.float32).reshape(-1))
    image.file_format = "PNG"
    image.filepath_raw = str(TEXTURES / (name + ".png"))
    image.save()
    image.pack()
    return image


def fabric_shader(name, base, normal, smoothness, tint):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*tint, 1)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    maps = []
    for image in (base, normal, smoothness):
        node = nodes.new("ShaderNodeTexImage")
        node.image = image
        maps.append(node)
    tint_node = nodes.new("ShaderNodeMixRGB")
    tint_node.blend_type = "MULTIPLY"
    tint_node.inputs[0].default_value = 1
    tint_node.inputs[2].default_value = (*tint, 1)
    links.new(maps[0].outputs["Color"], tint_node.inputs[1])
    links.new(tint_node.outputs[0], bsdf.inputs["Base Color"])
    normal_node = nodes.new("ShaderNodeNormalMap")
    normal_node.inputs["Strength"].default_value = .5
    links.new(maps[1].outputs["Color"], normal_node.inputs["Color"])
    links.new(normal_node.outputs["Normal"], bsdf.inputs["Normal"])
    inverse = nodes.new("ShaderNodeMath")
    inverse.operation = "SUBTRACT"
    inverse.inputs[0].default_value = 1.
    links.new(maps[2].outputs["Alpha"], inverse.inputs[1])
    links.new(inverse.outputs[0], bsdf.inputs["Roughness"])
    return material


def neutralize(pixels, fabric_range=(.45, .92)):
    # Remove source dye without discarding seam and protective guard relief.
    light = pixels[..., :3] @ np.array((.22, .36, .42), dtype=np.float32)
    low, high = np.percentile(light[light > .002], (4, 92))
    return np.interp(light, (low, high), fabric_range).astype(np.float32)


def rgba(gray):
    color = np.ones((*gray.shape, 4), dtype=np.float32)
    color[..., :3] = gray[..., None]
    return color


def dielectric(roughness):
    color = np.zeros((*roughness.shape, 4), dtype=np.float32)
    color[..., 3] = np.clip(1 - roughness, .12, .41)
    return color


def bind_head(obj):
    group = obj.vertex_groups.new(name="Head")
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    obj.parent = rig
    obj.matrix_parent_inverse = rig.matrix_world.inverted()
    obj.modifiers.new("Existing Humanoid head", "ARMATURE").object = rig


def new_mesh(name, points, faces, uvs, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points, [], faces)
    mesh.update()
    layer = mesh.uv_layers.new()
    for polygon, coords in zip(mesh.polygons, uvs):
        polygon.use_smooth = True
        for index, uv in zip(polygon.loop_indices, coords):
            layer.data[index].uv = uv
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


original_cap, cap_path = read_equipment("WPS_Cap_001")
source_briefs, briefs_path = read_equipment("WPS_Swimwear_001")
print("SUPPLIED CAP, BRIEFS", original_cap.name, source_briefs.name)
_, cap_base = read_map(cap_path, "")
_, cap_rough = read_map(cap_path, "_roughness", True)
cap_gray = neutralize(cap_base, (.55, .97))
cap_neutral = save_map("CapNeutral", rgba(cap_gray))
cap_smooth = save_map("CapSmoothness", dielectric(cap_rough[..., 0]), True)
cap_normal, cap_normals = read_map(cap_path, "_normal", True)
cap_normal.pack()
shutil.copyfile(cap_path.with_name(cap_path.stem + "_normal.png"), TEXTURES / "CapNormal.png")
cloth = fabric_shader("Premium cap fabric", cap_neutral, cap_normal, cap_smooth, (.045, .17, .55))
guard = fabric_shader("Premium cap guard and binding", cap_neutral, cap_normal, cap_smooth, (.1, .21, .58))

cap = original_cap.copy()
cap.data = original_cap.data.copy()
scene.collection.objects.link(cap)
cap.name = "PremiumCapCinematic"
cap.matrix_world = Matrix.Identity(4)
fit_origin = Vector((0., .023, 1.762))
for v in cap.data.vertices:
    p = original_cap.matrix_world @ v.co
    p.x *= 1.03
    p.y = p.y * .97 + .024
    p.z = 1.770 + p.z if p.z > -.074 else 1.696 + (p.z + .074) * .22
    radial = p - fit_origin
    ray = radial.normalized()
    skull, _, _, _ = head.ray_cast(fit_origin + ray * .4, -ray, .36)
    if skull is not None:
        margin = .0039 if p.z > 1.78 else .003
        clearance = (p - skull).dot(ray)
        if clearance < margin:
            p += ray * min(.037, (margin - clearance) * .90)
    v.co = p

# Relax Meshy remesh creases while retaining the guard ports and folded binding.
bm = bmesh.new()
bm.from_mesh(cap.data)
for _ in range(3):
    bmesh.ops.smooth_vert(bm, verts=[v for v in bm.verts if abs(v.co.x) < .089],
                         factor=.28, use_axis_x=True, use_axis_y=True, use_axis_z=True)
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(cap.data)
bm.free()

# The rear hairline projects beyond the generic cap in a few places. Lift
# the rear shell and lining together, with a fade above the guard and brim.
def ramp(value, low, high):
    t = min(1., max(0., (value - low) / (high - low)))
    return t * t * (3. - 2. * t)


for vertex in cap.data.vertices:
    p = vertex.co
    mask = ramp(p.y, .063, .095) * ramp(p.z, 1.756, 1.794)
    p += (p - fit_origin).normalized() * (.0042 * mask)
cap.data.update()
cap.data.materials.clear()
cap.data.materials.append(cloth)
cap.data.materials.append(guard)
for polygon in cap.data.polygons:
    polygon.use_smooth = True
    polygon.material_index = int(abs(polygon.center.x) > .088 and polygon.center.z < 1.789
                                 and abs(polygon.center.y - .015) < .054)

# Two short woven ribbons follow the lateral jaw and close at the submental point.
strap_points, strap_faces, strap_uvs = [], [], []
chin = Vector((0., -.063, 1.638))
def swatch_score(x, y):
    tone = cap_gray[y-5:y+5, x-5:x+5]
    slope = cap_normals[y-5:y+5, x-5:x+5, :2]
    return float(tone.std() + .3 * np.abs(tone.mean() - .78) + np.abs(slope - .5).mean())


swatch_x, swatch_y = min(((x,y) for x in range(32, 2016, 32) for y in range(32, 2016, 32)),
                         key=lambda xy: swatch_score(*xy))
swatch = tuple(((swatch_x + x) / 2048, (swatch_y + y) / 2048)
               for x,y in ((-2,-2),(2,-2),(2,2),(-2,2)))
for side in (-1, 1):
    route = [(side*.090, .001, 1.701), (side*.077, -.011, 1.679),
             (side*.056, -.032, 1.660), (side*.035, -.051, 1.646),
             (side*.016, -.062, 1.636), tuple(chin)]
    samples = []
    for a, b in zip(route[:-1], route[1:]):
        for j in range(3):
            p = Vector(a).lerp(Vector(b), j / 3.)
            hit, norm, _, _ = head.find_nearest(p)
            samples.append(hit + norm * .003)
    hit, norm, _, _ = head.find_nearest(chin)
    samples.append(hit + norm * .003)
    start = len(strap_points)
    for point in samples:
        strap_points.extend((point + Vector((0, -.0037, .0015)), point + Vector((0, .0037, -.0015))))
    for j in range(len(samples) - 1):
        idx = start + 2*j
        face = (idx, idx+1, idx+3, idx+2)
        strap_faces.extend((face, face[::-1]))
        strap_uvs.extend((swatch, swatch[::-1]))
strap = new_mesh("Fitted chin strap", strap_points, strap_faces, strap_uvs, guard)

def reduced_copy(name, ratio):
    obj = cap.copy()
    obj.data = cap.data.copy()
    obj.name = name
    scene.collection.objects.link(obj)
    activate(obj)
    decimation = obj.modifiers.new("Keep smooth ear and fabric silhouettes", "DECIMATE")
    decimation.ratio = ratio
    bpy.ops.object.modifier_apply(modifier=decimation.name)
    return obj


gameplay = reduced_copy("PremiumCapGameplay", .46)
low = reduced_copy("PremiumCapLow", .22)
number_material = bpy.data.objects["CapNumber"].data.materials[0]


def numbers_for(obj, name):
    vertices, faces, uv = [], [], []
    shell = BVHTree.FromPolygons([v.co for v in obj.data.vertices],
                                 [tuple(p.vertices) for p in obj.data.polygons], all_triangles=True)
    for side in (-1, 1):
        start = len(vertices)
        for row in range(7):
            for col in range(9):
                y = .010 + side * (.5 - col / 8.) * .078
                z = 1.797 + row / 6. * .047
                hit, _, _, _ = shell.ray_cast(Vector((side*.25, y, z)), Vector((-side,0,0)), .23)
                if hit is None:
                    raise ValueError(f"Number panel grazes the cap at {side}, {y}, {z}")
                vertices.append(hit + Vector((side*.0022, 0, 0)))
        for row in range(6):
            for col in range(8):
                i = start + row * 9 + col
                faces.append((i+9, i+10, i+1, i))
                uv.append(((col/8,(row+1)/6), ((col+1)/8,(row+1)/6), ((col+1)/8,row/6), (col/8,row/6)))
    result = new_mesh(name, vertices, faces, uv, number_material)
    bind_head(result)
    return result


numbers = [numbers_for(obj, f"PremiumNumber{kind}") for obj, kind
           in zip((cap, gameplay, low), ("Cinematic", "Gameplay", "Low"))]
for obj in (cap, gameplay, low):
    ribbon = strap.copy()
    ribbon.data = strap.data.copy()
    scene.collection.objects.link(ribbon)
    activate(obj)
    ribbon.select_set(True)
    bpy.ops.object.join()
    bind_head(obj)

# Reproject the source swatch onto the existing continuous wrap. The original
# fitted garment, weights and body cut remain the fit and coverage authority.
_, briefs_pixels = read_map(briefs_path, "")
_, briefs_rough = read_map(briefs_path, "_roughness", True)
briefs_gray = neutralize(briefs_pixels, (.48, .95))
sample_points = [source_briefs.matrix_world @ v.co for v in source_briefs.data.vertices]
sample_faces = [tuple(p.vertices) for p in source_briefs.data.polygons]
sample_tree = BVHTree.FromPolygons(sample_points, sample_faces, all_triangles=True)
source_uv = source_briefs.data.uv_layers.active.data
size = 512
base = np.empty((size,size), dtype=np.float32)
rough = np.empty_like(base)
directions = [Vector((sin(x/size*2*pi), -cos(x/size*2*pi), 0)) for x in range(size)]
for y in range(size):
    height = min(.121, max(-.120, ((y+.5)/size - .215)*.335 - .121))
    for x, direction in enumerate(directions):
        start = direction * .38 + Vector((0,0,height))
        hit, _, index, _ = sample_tree.ray_cast(start, -direction, .36)
        if hit is None:
            hit, _, index, _ = sample_tree.find_nearest(direction * .15 + Vector((0,0,height)))
        p = source_briefs.data.polygons[index]
        a, b, c = [Vector((*source_uv[i].uv, 0)) for i in p.loop_indices]
        uv = barycentric_transform(hit, *(sample_points[i] for i in p.vertices), a, b, c)
        u, v = int(uv.x * briefs_gray.shape[1]) % briefs_gray.shape[1], int(uv.y * briefs_gray.shape[0]) % briefs_gray.shape[0]
        base[y, x] = briefs_gray[v, u]
        rough[y, x] = briefs_rough[v, u, 0]

yy, xx = np.mgrid[0:size, 0:size]
u, v = xx / size, yy / size
waist_height = (.287 - .017 * np.maximum(0, np.cos(2*pi*u))) / .315
waist_seam = np.exp(-((v - waist_height + .024)/.006)**2)
stitch = waist_seam * (0.66 + .34 * np.cos(2*pi*u*96))
base = .58 * base + .32 - .08 * stitch
brief_base = save_map("BriefsNeutral", rgba(base))
brief_smooth = save_map("BriefsSmoothness", dielectric(rough), True)
# The source tangent map belongs to a different UV layout. Rebuild the normal
# from the transferred fabric signal and wrap-aware waistband seam.
bump = .19 * base + .07 * stitch
dx = (np.roll(bump, -1, 1) - np.roll(bump, 1, 1)) * -.5
dy = (np.roll(bump, -1, 0) - np.roll(bump, 1, 0)) * -.5
norm = np.sqrt(1 + dx**2 + dy**2)
brief_normal = rgba(np.ones_like(norm))
brief_normal[..., :3] = np.dstack((dx/norm, dy/norm, 1/norm)) * .5 + .5
brief_normal = save_map("BriefsNormal", brief_normal, True)
fitted_briefs = bpy.data.objects["CompetitionBriefs"]
fitted_briefs.data.materials[0] = fabric_shader(
    "Premium fitted briefs", brief_base, brief_normal, brief_smooth, (.055,.13,.46))

archive = bpy.data.collections.new("Unfitted premium source meshes and prior cap")
scene.collection.children.link(archive)
for obj in (original_cap, source_briefs, strap, bpy.data.objects["WaterPoloCap"], bpy.data.objects["CapNumber"]):
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    archive.objects.link(obj)
archive.hide_viewport = archive.hide_render = True
for obj in (cap, gameplay, low, *numbers, fitted_briefs):
    obj.data.calc_loop_triangles()
    print("PART", obj.name, len(obj.data.loop_triangles), "triangles",
          sum(p.area < 1e-10 for p in obj.data.polygons), "degenerate polygons")

# Unity preview imports the gameplay pair. The editable file retains the
# higher and lower detail variants and the original supplier meshes.
activate(rig)
for obj in (gameplay, numbers[1]):
    obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ASSETS / "PremiumHeadgear.fbx"), use_selection=True,
    object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False, axis_forward="-Z",
    axis_up="Y", apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE")
for obj in (gameplay, low, numbers[1], numbers[2]):
    obj.hide_set(True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "WPS_EquipmentFit_001.blend"))
