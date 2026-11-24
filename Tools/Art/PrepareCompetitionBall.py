"""Build the centered unit-diameter visual interchange and rubber smoothness map."""
from pathlib import Path
import shutil

import bpy
import numpy as np
from mathutils import Matrix, Vector

root = Path(__file__).resolve().parents[2]
source = root / "SourceAssets/Equipment/WPS_Ball_001/Original"
output = root / "Assets/Equipment/CompetitionBall"
output.mkdir(parents=True, exist_ok=True)
path, = source.glob("*.fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(path))
ball, = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
positions = [ball.matrix_world @ vertex.co for vertex in ball.data.vertices]
centre = Vector([(min(p[i] for p in positions) + max(p[i] for p in positions)) * .5 for i in range(3)])
ball.matrix_world = Matrix.Identity(4)
ball.name = "CompetitionBall"
ball.data.name = "CompetitionBall"
ball.data.materials.clear()
for vertex, position in zip(ball.data.vertices, positions):
    vertex.co = (position - centre).normalized() * .5
for face in ball.data.polygons:
    face.use_smooth = True
ball.data.update()
ball.data.calc_loop_triangles()
print(f"{len(ball.data.vertices)} welded vertices, {len(ball.data.loop_triangles)} triangles, "
      f"{len(ball.data.uv_layers)} UV set, {sum(face.area < 1e-10 for face in ball.data.polygons)} degenerate faces")
bpy.ops.object.select_all(action="DESELECT")
ball.select_set(True)
bpy.context.view_layer.objects.active = ball
bpy.ops.export_scene.fbx(filepath=str(output / "CompetitionBall.fbx"), use_selection=True, object_types={"MESH"},
                         bake_anim=False, axis_forward="-Z", axis_up="Y",
                         apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="FACE")

for suffix, name in (("", "BallColor"), ("_normal", "BallNormal")):
    shutil.copyfile(path.with_name(path.stem + suffix + ".png"), output / (name + ".png"))

roughness = bpy.data.images.load(str(path.with_name(path.stem + "_roughness.png")))
roughness.colorspace_settings.name = "Non-Color"
width, height = roughness.size
pixels = np.empty(width * height * 4, dtype=np.float32)
roughness.pixels.foreach_get(pixels)
pixels = pixels.reshape(height, width, 4)
pixels[..., 3] = np.clip(1.0 - pixels[..., 0], .15, .39)
pixels[..., :3] = 0  # The sports-rubber shell is dielectric.
image = bpy.data.images.new("BallSmoothness", width, height, alpha=True)
image.colorspace_settings.name = "Non-Color"
image.pixels.foreach_set(pixels.reshape(-1))
image.file_format = "PNG"
image.filepath_raw = str(output / "BallSmoothness.png")
image.save()
