"""Build and audit a registered Atlas competition-brief candidate in Blender.

Run with WPS_AquaticMotion_001.blend open in background Blender. The input
contains the 66-bone production rig, skin cut and four shipped aquatic actions.
All production Unity assets remain untouched by this authoring script.
"""

from collections import Counter, defaultdict
from pathlib import Path
import json
import math

import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_point_line


PROJECT = Path(__file__).resolve().parents[2]
SOURCE = PROJECT / "SourceAssets/Athletes/WPS_Swimwear_001/Blender"
MAPS = PROJECT / "Assets/ProductionAthletes/AuthenticEquipment/Textures"
RESULTS = PROJECT / "Logs/Milestone30/Briefs"
UNITY_PREVIEW = PROJECT / "Assets/ProductionAthletes/Candidate"
CANDIDATE = "Atlas Competition Briefs"
WAIST_BREAKS = (.004, .012, .022)
LEG_BREAKS = (.0035, .008)
SUIT_NAME = "CompetitionBriefs"
RIG_NAME = "Armature"
BODY_NAME = "AthleteBody"


def edge_key(a, b):
    return tuple(sorted((a, b)))


def boundary_loops(faces):
    uses = Counter(edge_key(a, b) for face in faces for a, b in zip(face, face[1:] + face[:1]))
    neighbors = defaultdict(set)
    for (a, b), count in uses.items():
        if count == 1:
            neighbors[a].add(b)
            neighbors[b].add(a)
    if any(len(ring) != 2 for ring in neighbors.values()) or max(uses.values()) > 2:
        raise ValueError("Atlas cut is not a set of manifold boundary loops.")
    unseen = set(neighbors)
    rings = []
    while unseen:
        start = min(unseen)
        ring = [start]
        previous, current = start, min(neighbors[start])
        while current != start:
            ring.append(current)
            previous, current = current, next(iter(neighbors[current] - {previous}))
        unseen.difference_update(ring)
        rings.append(ring)
    return rings


def point_segment_distance(point, a, b):
    _, parameter = intersect_point_line(point, a, b)
    return (point - a.lerp(b, max(0, min(1, parameter)))).length


def source_mesh(obj):
    return (
        [obj.matrix_world @ vertex.co for vertex in obj.data.vertices],
        [tuple(poly.vertices) for poly in obj.data.polygons],
    )


def vertex_weights(obj, index):
    return {
        obj.vertex_groups[item.group].name: item.weight
        for item in obj.data.vertices[index].groups
    }


def mix_weights(a, b, ratio):
    return {name: a.get(name, 0) * (1 - ratio) + b.get(name, 0) * ratio for name in a.keys() | b.keys()}


def ramp(low, high, value):
    t = max(0., min(1., (value - low) / (high - low)))
    return t * t * (3. - 2. * t)


def profile(distance, keys):
    for (a, x), (b, y) in zip(keys, keys[1:]):
        if distance <= b:
            t = max(0., (distance - a) / (b - a))
            return x + (y - x) * t
    return keys[-1][1]


class BriefPattern:
    def __init__(self, reference, skin):
        self.points, self.faces = source_mesh(reference)
        self.normals = [vertex.normal.copy() for vertex in reference.data.vertices]
        self.weights = [vertex_weights(reference, i) for i in range(len(self.points))]
        skin_points, skin_faces = source_mesh(skin)
        skin_openings = boundary_loops(skin_faces)
        rings = boundary_loops(self.faces)
        if len(rings) != 3:
            raise ValueError(f"Expected three Atlas openings, found {len(rings)}.")
        rings.sort(key=lambda ring: sum(self.points[i].z for i in ring) / len(ring), reverse=True)
        self.seam = {}
        for ring in rings:
            skin_ring, = [opening for opening in skin_openings if len(opening) == len(ring)]
            registrations = (direction[offset:] + direction[:offset]
                             for direction in (skin_ring, skin_ring[::-1])
                             for offset in range(len(ring)))
            matched = min(registrations, key=lambda candidate: sum(
                (self.points[a] - skin_points[b]).length_squared
                for a, b in zip(ring, candidate)
            ))
            for i, nearest in zip(ring, matched):
                if (self.points[i] - skin_points[nearest]).length > .0003:
                    raise ValueError("Atlas body and source briefs no longer share the coverage cut.")
                self.seam[i] = nearest
                self.points[i] = skin_points[nearest]
                self.weights[i] = vertex_weights(skin, nearest)
        if len(set(self.seam.values())) != sum(map(len, skin_openings)):
            raise ValueError("Duplicate seam registration.")
        self.openings = rings
        segments = [
            [(self.points[a], self.points[b]) for a, b in zip(ring, ring[1:] + ring[:1])]
            for ring in rings
        ]
        self.distances = [
            tuple(min(point_segment_distance(point, a, b) for a, b in group) for group in segments)
            for point in self.points
        ]
        self.relax_body_panel()
        self.rim_faces = []
        self.rim_origins = []
        self.edge_nodes = {}

    def relax_body_panel(self):
        adjacency = [set() for _ in self.points]
        for a, b, c in self.faces:
            adjacency[a].update((b, c))
            adjacency[b].update((a, c))
            adjacency[c].update((a, b))
        source = self.points
        for _ in range(10):
            previous = self.points
            self.points = []
            for i, point in enumerate(previous):
                relief = ramp(.008, .024, min(self.distances[i]))
                if relief <= 0. or i in self.seam:
                    self.points.append(point)
                    continue
                average = sum((previous[j] for j in sorted(adjacency[i])), Vector()) / len(adjacency[i])
                displaced = point.lerp(average, .50 * relief)
                offset = displaced - source[i]
                if offset.length > .005 * relief:
                    displaced = source[i] + offset.normalized() * (.005 * relief)
                self.points.append(displaced)

    def split(self, face, opening, level):
        below = []
        above = []
        for i, b in enumerate(face):
            a = face[i - 1]
            x = self.distances[a][opening] - level
            y = self.distances[b][opening] - level
            if abs(y) <= 1e-7:
                below.append(b)
                above.append(b)
                continue
            if x * y < 0 and abs(x) > 1e-7:
                node = self.edge_point(a, b, opening, level, x / (x - y))
                below.append(node)
                above.append(node)
            (above if y > 0 else below).append(b)
        return [side for side in (below, above) if len(side) >= 3]

    def edge_point(self, a, b, opening, level, t):
        key = (*edge_key(a, b), opening, level)
        if key in self.edge_nodes:
            return self.edge_nodes[key]
        index = len(self.points)
        self.edge_nodes[key] = index
        self.points.append(self.points[a].lerp(self.points[b], t))
        self.normals.append(self.normals[a].lerp(self.normals[b], t).normalized())
        self.weights.append(mix_weights(self.weights[a], self.weights[b], t))
        self.distances.append(tuple(a + (b - a) * t for a, b in zip(self.distances[a], self.distances[b])))
        return index

    def build(self, rig, reference):
        for origin, face in enumerate(self.faces):
            pieces = [list(face)]
            for opening, breaks in enumerate((WAIST_BREAKS, LEG_BREAKS, LEG_BREAKS)):
                for level in breaks:
                    pieces = [side for poly in pieces for side in self.split(poly, opening, level)]
            for piece in pieces:
                self.rim_faces.extend((piece[0], piece[j], piece[j + 1]) for j in range(1, len(piece) - 1))
                self.rim_origins.extend([origin] * (len(piece) - 2))
        raised = []
        for i, point in enumerate(self.points):
            waist, left, right = self.distances[i]
            band = profile(waist, ((0, 0), (.004, .0028), (.012, .0024), (.022, .0015)))
            binding = profile(min(left, right), ((0, 0), (.0035, .0022), (.008, .0015)))
            relief = max(band, binding) * ramp(0., .003, min(waist, left, right))
            raised.append(point.copy() if i in self.seam else point + self.normals[i] * relief)
        mesh = bpy.data.meshes.new(CANDIDATE)
        mesh.from_pydata(raised, [], self.rim_faces)
        mesh.update()
        mesh.materials.append(preview_fabric())
        surface = mesh.uv_layers.new(name="FabricWrap")
        masks = mesh.color_attributes.new(name="FabricMask", type="FLOAT_COLOR", domain="POINT")
        # Preserve the periodic wrap used by the supplier fabric reprojection.
        # R is the waistband region, G the leg bindings, B the side accent.
        for i, (point, distances) in enumerate(zip(self.points, self.distances)):
            waist, left, right = distances
            flank = abs(point.x) / max(.0001, math.hypot(point.x, point.y - .022))
            masks.data[i].color = (
                1 - ramp(.015, .022, waist),
                1 - ramp(.003, .008, min(left, right)),
                ramp(.91, .99, flank) * ramp(.010, .028, min(distances)),
                1,
            )
        for poly in mesh.polygons:
            poly.use_smooth = True
            coords = [self.points[i] for i in poly.vertices]
            us = [(math.atan2(p.x, .022 - p.y) / math.tau) % 1. for p in coords]
            if max(us) - min(us) > .5:
                us = [u + 1 if u < .5 else u for u in us]
            for loop, p, u in zip(poly.loop_indices, coords, us):
                surface.data[loop].uv = (u, (p.z - .740) / .315)
        obj = bpy.data.objects.new(CANDIDATE, mesh)
        reference.users_collection[0].objects.link(obj)
        groups = {}
        for i, weights in enumerate(self.weights):
            selected = sorted(((name, weight) for name, weight in weights.items() if weight > 0.),
                              key=lambda pair: (-pair[1], pair[0]))[:4]
            total = sum(weight for _, weight in selected)
            for name, weight in selected:
                if name not in groups:
                    groups[name] = obj.vertex_groups.new(name=name)
                groups[name].add([i], weight / total, "REPLACE")
        obj.parent = rig
        obj.matrix_parent_inverse = rig.matrix_world.inverted()
        obj.modifiers.new("Atlas Humanoid", "ARMATURE").object = rig
        return obj


def preview_fabric():
    mat = bpy.data.materials.new("Atlas two-color competition fabric")
    mat.use_nodes = True
    mat.diffuse_color = (.027, .102, .36, 1)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Roughness"].default_value = .65
    bsdf.inputs["Sheen Weight"].default_value = .15
    fabric = nodes.new("ShaderNodeTexImage")
    fabric.image = bpy.data.images.load(str(MAPS / "BriefsNeutral.png"), check_existing=True)
    fabric.image.pack()
    zones = nodes.new("ShaderNodeVertexColor")
    zones.layer_name = "FabricMask"
    channels = nodes.new("ShaderNodeSeparateColor")
    links.new(zones.outputs["Color"], channels.inputs["Color"])
    trim = nodes.new("ShaderNodeMath")
    trim.operation = "MAXIMUM"
    links.new(channels.outputs["Red"], trim.inputs[0])
    links.new(channels.outputs["Green"], trim.inputs[1])
    palettes = nodes.new("ShaderNodeMixRGB")
    palettes.label = "Team and waistband / opening binding"
    palettes.inputs[1].default_value = (.012, .042, .19, 1)
    palettes.inputs[2].default_value = (.032, .135, .43, 1)
    links.new(trim.outputs[0], palettes.inputs[0])
    accent = nodes.new("ShaderNodeMixRGB")
    accent.label = "Optional side accent"
    accent.inputs[2].default_value = (.02, .071, .26, 1)
    links.new(channels.outputs["Blue"], accent.inputs[0])
    links.new(palettes.outputs[0], accent.inputs[1])
    tint = nodes.new("ShaderNodeMixRGB")
    tint.blend_type = "MULTIPLY"
    tint.inputs[0].default_value = .72
    links.new(accent.outputs[0], tint.inputs[1])
    links.new(fabric.outputs["Color"], tint.inputs[2])
    links.new(tint.outputs[0], bsdf.inputs["Base Color"])
    normal = nodes.new("ShaderNodeTexImage")
    normal.image = bpy.data.images.load(str(MAPS / "BriefsNormal.png"), check_existing=True)
    normal.image.colorspace_settings.name = "Non-Color"
    normal.image.pack()
    mapped = nodes.new("ShaderNodeNormalMap")
    mapped.inputs["Strength"].default_value = .45
    links.new(normal.outputs["Color"], mapped.inputs["Color"])
    links.new(mapped.outputs[0], bsdf.inputs["Normal"])
    return mat


def rest(rig):
    rig.animation_data.action = None
    for bone in rig.pose.bones:
        bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()


def audit(label, candidate, reference, skin, pattern):
    graph = bpy.context.evaluated_depsgraph_get()
    dressed, faces = source_mesh(candidate.evaluated_get(graph))
    prior, panels = source_mesh(reference.evaluated_get(graph))
    exposed, exposed_faces = source_mesh(skin.evaluated_get(graph))
    shell = BVHTree.FromPolygons(dressed, faces, all_triangles=True)
    skin_tree = BVHTree.FromPolygons(exposed, exposed_faces, all_triangles=True)
    skin_distance = max((dressed[i] - exposed[j]).length for i, j in pattern.seam.items())
    possible_contacts = shell.overlap(skin_tree)
    previous_contacts = BVHTree.FromPolygons(prior, panels, all_triangles=True).overlap(skin_tree)
    garment_seam = set(pattern.seam)
    skin_seam = set(pattern.seam.values())
    interior_skin = {
        b for a, b in possible_contacts
        if not garment_seam.intersection(panels[pattern.rim_origins[a]]) and not skin_seam.intersection(exposed_faces[b])
    }
    previous_interior_skin = {
        b for a, b in previous_contacts
        if not garment_seam.intersection(panels[a]) and not skin_seam.intersection(exposed_faces[b])
    }
    missing_area = 0.
    prior_area = 0.
    for ia, ib, ic in panels:
        a, b, c = prior[ia], prior[ib], prior[ic]
        centre = (a + b + c) / 3.
        cross = (b - a).cross(c - a)
        half_area = cross.length * .5
        prior_area += half_area
        normal = cross.normalized()
        hit, _, _, _ = shell.ray_cast(centre + normal * .030, -normal, .060)
        if hit is None:
            missing_area += half_area
    return {
        "pose": label,
        "coverage_missed_pct": round(100 * missing_area / prior_area, 6),
        "max_seam_separation_m": round(skin_distance, 7),
        "body_overlap_pairs": len(possible_contacts),
        "previous_body_overlap_pairs": len(previous_contacts),
        "non_seam_skin_polygons": len(interior_skin),
        "previous_non_seam_skin_polygons": len(previous_interior_skin),
        "additional_non_seam_skin_polygons": len(interior_skin - previous_interior_skin),
    }


def main():
    rig = bpy.data.objects[RIG_NAME]
    old = bpy.data.objects[SUIT_NAME]
    skin = bpy.data.objects[BODY_NAME]
    initial_action = rig.animation_data.action
    rest(rig)
    pattern = BriefPattern(old, skin)
    candidate = pattern.build(rig, old)
    faces = [tuple(p.vertices) for p in candidate.data.polygons]
    rings = boundary_loops(faces)
    if {frozenset(ring) for ring in rings} != {frozenset(ring) for ring in pattern.openings}:
        raise ValueError("Seam split generated new boundary vertices.")
    report = {
        "candidate": CANDIDATE,
        "triangles": len(faces),
        "vertices": len(candidate.data.vertices),
        "boundary_vertices": sorted(len(r) for r in rings),
        "degenerate_triangles_below_1e_10": sum(p.area <= 1e-10 for p in candidate.data.polygons),
        "weights_per_vertex": sorted({len(v.groups) for v in candidate.data.vertices}),
        "max_weight_error": max(abs(sum(g.weight for g in v.groups) - 1.) for v in candidate.data.vertices),
        "poses": [audit("Rest", candidate, old, skin, pattern)],
    }
    for name in ("WaterPoloFreestyle", "WaterPoloSprint", "WaterPoloEggbeater", "WaterPoloBallTread"):
        action = bpy.data.actions[name]
        rig.animation_data.action = action
        start, end = action.frame_range
        for step in range(9):
            frame = round(start + (end - start) * step / 8)
            bpy.context.scene.frame_set(frame)
            report["poses"].append(audit(f"{name} at {frame}", candidate, old, skin, pattern))
    RESULTS.mkdir(parents=True, exist_ok=True)
    (RESULTS / "GeometryAudit.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in report.items() if key != "poses"}))
    print("Worst geometry metrics",
          max(p["coverage_missed_pct"] for p in report["poses"]),
          max(p["max_seam_separation_m"] for p in report["poses"]),
          max(p["additional_non_seam_skin_polygons"] for p in report["poses"]))
    rest(rig)
    old.hide_set(True)
    old.hide_render = True
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    candidate.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = candidate
    UNITY_PREVIEW.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(UNITY_PREVIEW / "AtlasCompetitionBriefs.fbx"), use_selection=True,
        object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False,
        axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL",
        mesh_smooth_type="FACE", use_mesh_modifiers=True,
    )
    rig.select_set(False)
    rig.animation_data.action = initial_action
    if initial_action is not None:
        bpy.context.scene.frame_set(0)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "AtlasCompetitionBriefs.blend"))


if __name__ == "__main__":
    main()
