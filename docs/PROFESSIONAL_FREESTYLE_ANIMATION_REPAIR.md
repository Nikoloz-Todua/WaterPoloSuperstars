# Professional freestyle animation repair

## Pipeline diagnosis

The 30F production character was inspected close up in the underwater Test Lab. The comparison covers the side, rear, overhead and underwater three-quarter views. At some phases the shin and foot silhouette looked flared relative to the pelvis, especially from below. The captured steady crawl did not establish that the pelvis or legs were flipped by exactly 180 degrees across the entire cycle.

The original Blender Action built the leg swings using a fixed world-space direction while the pelvis and chest rolled. Its animation-only FBX retained that behaviour. A fresh Blender reimport at nine matched phases agreed within 0.05° on the sampled knee angle and within 0.1 mm on hip-to-forefoot distance at the displayed precision. Unity Humanoid changes the sampled local axes, including the shin, through Avatar conversion. The 30C runtime fix addressed the shin. The 30D override drove the leg chain with its own oscillator and depended on the Humanoid pelvis and a pose gate. The result still combined two separately authored orientations at the hips.

The phase comparison evaluates the Animator, invokes the final production pose, holds Animator evaluation for the captured frame, and renders the Game View. Earlier fixed-frame capture left automatic Animator sampling enabled between applying the pose and rendering. That could overwrite the pose in the screenshot.

## Authored motion

`WPS_FreestyleLowerBody_Professional` and `WPS_SprintFlutter_Professional` are editable Actions in `SourceAssets/Athletes/WPS_Animations_001/Blender/WPS_AquaticMotion_001.blend`. They retain the earlier arm animation for Blender review and use the unchanged 66-bone skeleton. Earlier Actions are still in the same file. The new actions share a pelvis-based leg frame. Opposite flutter kicks originate at the hip. Knee flexion lags the thigh and the ankle follows with a smaller delayed roll and slight toe articulation. The lower-body roll follows the pelvis.

Ordinary crawl uses an 84-frame stroke at 60 Hz. Sprint uses 65 frames and a slightly stronger hip beat. These loops carry three kicks per leg per stroke cycle. The production animator supplies phase, and its normal-to-sprint blend mixes the two authored chains. The end sample matches the first pose. The swimming principles are based on measured alignment, alternating kicks and controlled amplitude. [U.S. Masters Swimming freestyle kick](https://www.usms.org/fitness-and-training/guides/freestyle/kick)

`Tools/Art/AuthorProfessionalFlutter.py` exports nine rotation deltas per sample in `Assets/Resources/WPSFlutterMotion.json`. The deltas are relative to the bind skeleton and include the coordinate basis conversion. `ProneFlutterPose` composes them on cached production joint frames after Humanoid evaluation. The hips are evaluated with the legs and the sampled upper-spine rotation is preserved. The authored overlay blends in with the existing movement state. It drops out during tread, held recovery and receiving. The existing shooting kick is selected separately. The Avatar, skinning, water, gameplay motor and ball-leading rules remain authoritative in their existing paths. [Unity Humanoid configuration](https://docs.unity3d.com/6000.6/Documentation/Manual/ConfiguringtheAvatar.html) · [Blender bone roll](https://docs.blender.org/manual/en/latest/animation/armatures/bones/editing/bone_roll.html)

## Evidence and limits

A focused advancing Windows Editor run exercised a normal cycle, a sprint cycle, return to tread, both ball-leading speeds, and gathering to stationary possession. The phase probe rendered 0 through 100% at 12.5% intervals for ordinary crawl and sampled sprint and eggbeater. The upright tread pose off the overlay matched the prior probe at the sampled phase. The overlay uses a shared data load and cached per-athlete transforms. Receiver compatibility and final production status are recorded in the final report. [Matched phases](Evidence/Milestone30G/Freestyle-Before-After-Matched-Phases.jpg) · [Other production views](Evidence/Milestone30G/Freestyle-Production-Views.jpg) · [Final report](MILESTONE_30G_FINAL_REPORT.md)

The original visual defect stays open for personal inspection. The phase sheet shows the actual scale of the correction and includes the matching legacy poses. Android performance and irregular match transitions require device and personal checks.
