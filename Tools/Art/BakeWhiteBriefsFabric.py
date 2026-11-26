"""Bake Atlas sewn-band masks into the White kit using its existing FabricWrap UVs."""

from array import array
from math import ceil, floor
from pathlib import Path

import bpy


root = Path(__file__).resolve().parents[2]
textures = root / "Assets/ProductionAthletes/AuthenticEquipment/Textures"
size = 256
garment = next(obj for obj in bpy.data.objects
               if obj.type == "MESH" and obj.data.color_attributes.get("FabricMask"))
mesh = garment.data
mesh.calc_loop_triangles()
uv = mesh.uv_layers["FabricWrap"].data
regions = mesh.color_attributes["FabricMask"].data
trim = array("f", [0.0]) * (size * size)

for triangle in mesh.loop_triangles:
    points = [tuple(uv[loop].uv * size) for loop in triangle.loops]
    (ax, ay), (bx, by), (cx, cy) = points
    det = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
    if abs(det) < 1e-5:
        continue
    weights = [max(mask[0] * .76, mask[1] * .61, mask[2] * .42)
               for mask in (regions[index].color for index in triangle.vertices)]
    for y in range(floor(min(ay, by, cy)), ceil(max(ay, by, cy))):
        for x in range(floor(min(ax, bx, cx)), ceil(max(ax, bx, cx))):
            u = ((by - cy) * (x + .5 - cx) + (cx - bx) * (y + .5 - cy)) / det
            v = ((cy - ay) * (x + .5 - cx) + (ax - cx) * (y + .5 - cy)) / det
            w = 1 - u - v
            if min(u, v, w) >= -.018:
                index = (y % size) * size + x % size
                trim[index] = max(trim[index], weights[0] * u + weights[1] * v + weights[2] * w)

neutral = bpy.data.images.load(str(textures / "BriefsNeutral.png"), check_existing=False)
neutral.scale(size, size)
pixels = array("f", [0.0]) * (size * size * 4)
neutral.pixels.foreach_get(pixels)
for index, strength in enumerate(trim):
    at = index * 4
    for channel, accent in enumerate((.16, .26, .40)):
        cloth = min(1.0, pixels[at + channel] * 1.15)
        pixels[at + channel] = cloth * (1 - strength + accent * strength)
    pixels[at + 3] = 1.0

baked = bpy.data.images.new("White Briefs Fabric", width=size, height=size, alpha=False)
baked.pixels.foreach_set(pixels)
baked.file_format = "PNG"
baked.filepath_raw = str(textures / "WhiteBriefsFabric.png")
baked.save()
