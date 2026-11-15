# Milestone 14 animated swimmer prototype

The saved production scene remains `Assets/Scenes/OlympicMatchArena.unity`. All **14 existing gameplay athletes** now have the same reusable visual pipeline after an isolated lab and one-athlete match check passed. This is a rigged mannequin prototype with stylized water polo keyframes, not final athlete art or professional motion capture.

## Review in Unity

- **Tools > Water Polo > Animated Athletes > Play Isolated Swimmer Lab** opens the separate lab and temporarily bypasses Boot. Stopping restores the previous Editor Play entry. Keys **1 tread, 2 swim, 3 sprint, 4 possession, 5 pass, 6 shoot, 7 block, 8 save** select visual previews. The lab has no game controllers, collisions or economy.
- **Tools > Water Polo > Arena > Play Olympic Match Arena Directly** runs the actual match. Existing WASD, Shift, E, B, L, Space, C and HUD controls remain unchanged. F8 retains the existing venue inspection views.
- **Tools > Water Polo > Use Boot as Editor Play Entry** restores normal startup after direct arena inspection. Normal entry is already restored at delivery.
- **Tools > Water Polo > Animated Athletes > Set Up Prototype and Lab** prepares missing assets/lab once and then preserves prefab/controller tuning. It opens the lab, not the production arena.
- **Apply to One Match Athlete / Apply to All 14 Match Athletes** in that submenu opt existing athlete roots into the prefab. They retain original inactive visuals, do not duplicate existing rigged children, and preserve gameplay components/references. “One” applies to the first Blue field athlete; it does not undo other existing opt-ins.
- **Restore Original Match Visuals** reactivates the retained primitive `ArenaAthleteVisual` objects and disables the animated wrappers. It leaves new assets available for later work.
- **Reimport Prototype Source FBXs** deliberately reapplies the Humanoid/in-place/compression import settings after intentional source regeneration. Stop Play Mode and save intended scene edits before setup/reimport.

Exact pre-animation recovery: `Assets/Scenes/Recovery/OlympicMatchArena_PreAnimatedAthlete.unity`, outside builds. This is a scene snapshot; scripts, including cosmetic water effects, remain shared. Existing WaterVisualTest, PrototypePool and venue recoveries are retained. No new frontend or game-flow destination was added.

## Sources and licenses

Actually downloaded: Quaternius **Universal Animation Library mannequin**, `swim_idle_loop.glb` and `swim_fwd_loop.glb`, distributed by KinemIQ under **CC0 1.0**. [KinemIQ credits](https://kinemiq.app/sources/), [idle clip](https://kinemiq.app/animations/idle-in-water/), [swim clip](https://kinemiq.app/animations/move-swimming/) and [original library](https://quaternius.com/packs/universalanimationlibrary.html) identify the sources and license. The pages describe a unified humanoid rig and FBX/GLB/BVH exports. The swim reference is about 1.33 seconds and contains travel; the idle reference is about 3.33 seconds. Their source frame data was inspected locally. The swim's paired-arm motion is unsuitable as the final water polo crawl.

Original three GLBs, SHA256 hashes, direct URLs, retrieval date and CC0 legal text are retained in `SourceAssets/SwimmerAnimation/`, outside Unity imports. Imported assets are **one converted model FBX, two reference animation FBXs and eight project-authored/adapted animation FBXs** under `Assets/SwimmerAnimationPrototype/`. No full animation/environment package or glTF runtime dependency was installed. Notices ship under `Assets/StreamingAssets/ThirdPartyNotices/`.

The requested [Basic Character Animations FREE / 409552](https://assetstore.unity.com/packages/3d/animations/basic-character-animations-free-409552) was inspected: publisher Burak Uzun, Standard Asset Store EULA/Extension Asset, original Editor 6000.3.6, listed URP support. **It was not downloaded or imported**, so its actual rig, clips and retargeting quality were not verified. To evaluate it later, the developer must add it to their Asset Store account, then download/import through Package Manager > My Assets. That is optional; the working prototype uses the obtained CC0 files.

The live Blender MCP addon was unavailable. Conversion used **local headless Blender 5.2.3**, without opening/saving a user document. Reproducible conversion: `Tools/Art/PrepareAnimatedSwimmer.py`. No Blender rig was built on gameplay objects.

## Rig and hierarchy

`SwimmerIdentity / SwimmerMotor / SwimmerBallInteraction / existing gameplay components`

→ `Animated Athlete Visual` — prefab wrapper; waterline correction changes this visual Y only

→ `WaterPoloAthlete` — valid Humanoid Avatar, Animator, `RiggedSwimmerVisual`

→ `Armature / root / pelvis / spine / head / limbs / fingers` and skinned body/cap/ear guards

Original primitive visual objects remain inactive siblings. Original goalkeeper arm references still point to those fallback pivots; physics hand/reach calculations are unchanged. No rigged-visual collider, Rigidbody, release anchor or scoring component was added.

The retained rig has **66 bones**, with explicit mapping for 22 core Humanoid body joints. The body was reduced from roughly 32k source vertices to approximately **6.2k**; body, cap and two ear guards total about **6.6k vertices / 9.9k triangles** per athlete in imported meshes. These counts are content statistics, not measured draw calls or device performance. Four shared URP Lit materials provide skin, Blue, White and goalkeeper-red cap support. Swimwear is the first body submesh, skin the second. No large character textures were imported.

Model and animation importers preserve the Armature hierarchy; skeleton-only FBXs otherwise collapsed that node and produced incorrect retargeting offsets. All production clips copy the model Avatar, bake root rotation/position, use optimal animation compression with 0.5 error settings, and run with `Animator.applyRootMotion=false`. No animation event owns a gameplay action.

## Implemented states

| State | Current implementation |
| --- | --- |
| Tread | Upright bob, balancing scull and alternating circular bent-leg motion adapted from the source pose foundation; visible submerged limbs. Eggbeater approximation, not final biomechanical animation. |
| Swim | In-place head-up crawl prototype with alternating arm paths, leg kicks and torso roll. Blend-tree value follows actual motor velocity. |
| Sprint | Faster clip with stronger stroke/kick cycle; uses actual `SwimmerStamina.IsSprinting` and measured velocity. |
| Turn | Existing motor orientation smoothly turns the visual hierarchy. No separate turn clip or movement authority. |
| Possession | Upper-body pose plus bounded right-hand Humanoid IK toward the real held ball; original carry/release positions remain authoritative. |
| Pass / lob / shoot | Short upper-body follow-through triggered once by the actual `BallController.Released` event. No delayed/repeated physical release. Lob uses the pass presentation. |
| Goalkeeper | Tread/raised-arm block while the real keeper detects a threatening shot; short reach/save pose observes `SaveAttempts`. This reports an attempt, not a guaranteed successful catch. No authored diving or directional save library yet. |

The base Animator blend tree mixes TreadWater → HeadUpSwim → Sprint with 0.16-second input damping. A masked upper-body layer supplies possession/block and pass/shoot/save one-shots, leaving locomotion legs running. It fades to zero when free arms resume; this avoids retained override poses suppressing swimming strokes. Animation speed and effects respect authoritative match pause. Visual-only head alignment holds the head bone approximately **0.26 m above the configured pool waterline**; no motor, ball or pool physics changes compensate for animation.

## Interactive water and budgets

The existing water shader, transparent surface, material/quality assets, caustics and rendering path are unchanged. `WaterInteractionEffects` remains a cosmetic observer. Its existing V wakes now vary slightly with sprint and obey camera visibility/range. The existing impact observer uses a two-ring shared mesh for concentric ball ripples.

`RiggedSwimmerVisual` samples animated hand crossings after pose/waterline alignment. Downward surface entries emit small local splashes, with a per-hand cooldown; stationary athletes emit gentle rings roughly every 2.4 seconds, staggered by cap number. Moving submerged hands/feet can emit short microbubbles. Real throws emit one local disturbance. No constant stationary splashes or fluid simulation.

- **48 preallocated ring/splash entries:** 16 reserved for ball contacts, 32 for athletes on High. Medium uses 8 ball / 12 athlete entries. All use shared meshes/material property blocks.
- **One native ParticleSystem, maximum 128 particles**, manually emitted, lifetime about 0.4 seconds, no collisions or per-stroke GameObjects. Simulation explicitly starts on emission; pause and quality clearing are handled.
- Existing graphics mapping stays authoritative: **Low = cosmetic effects Off**, Medium = Low effect detail/no bubbles, High = Standard detail/bubbles. Clear-water materials remain available at every preset.
- Rings/wakes reject offscreen and distant positions; High range 65 m, Medium 45 m; bubbles limited to Standard and 32 m. Effect buffers are reused, without per-stroke Instantiate/Destroy.

## Actual verification and limits

Focused source: `Tools/Verification/AnimatedAthleteVerification.cs`; async entries ran through Unity Pipeline `script run`, not synchronous Task eval. Evidence is in ignored `Logs/SwimmerAnimation/`.

- Fresh Unity 6000.6.7f1 source compilation passed. Final Console ground truth: **0 errors, 0 warnings**. Historical Pipeline buffers contain unrelated earlier entries and are not the current Console count.
- Isolated advancing Game view exercised all eight previews, with a valid Humanoid, reasonable waterline/root offsets, submerged feet and no XZ root drift. After the action-layer correction, sampled hand travel over 0.25 seconds was about **0.51 m swim / 0.60 m sprint**, rather than a suppressed pose. Inspected actual 1920×1080 Game captures show treading, strokes, underwater limbs and local rings.
- One real athlete passed, then all 14 passed: actual Input System W/Shift movement and sprint, idle rings, hand entries, physical pickup/pass/reception, exactly one observed pass and shot release, real goal scoring, AI within existing bounds, pause-aware animation, original HUD pointer Pause/Escape and Low/Medium/High rendering/effect clearing.
- **518 original component records** compare unchanged: gameplay/rules/input/referee scripts, physics components and camera settings/references. Boot, MainLobby, build configuration, destination, hub catalog and water-material hashes remain unchanged. Exact recovery hash passed. Repeat setup and saved scene reload retain exactly 14 active rigged visuals; original fallback objects remain inactive.
- Goalkeeper save-hook test passed with a real physical swept catch using the existing deterministic endpoint fixture (**80 m/s, zero reaction delay, 100% save/catch temporarily in Play Mode**), with production tuning restored. Default 16 m/s shots in the initial fixture did not produce a save attempt; no claim of representative keeper success rates or directional animation coverage is made.
- An unreceived real lob produced authoritative water contact, visible pooled feedback and fixed effect-object counts. Particle simulation/lifetime and 128-particle ceiling were checked separately after the final particle change. No historical full-game suites were repeated.

Reports: `M14-Lab-PASS`, `M14-Match-1-PASS`, `M14-Match-14-PASS`, `M14-Structure-PASS`, `M14-Keeper-PASS`, `M14-Water-PASS`, `M14-Particle-PASS`. Screenshots include `Lab-0..7`, `Match-Idle/Swim/Sprint/Possession/Throw-14`, `Match-Keeper`, `Match-Quality-0/1/2-14` and `Match-Ball-Water`.

This milestone did not repeat full-time rewards, division progression or complete menu round trips; those systems and their serialized entry paths were untouched and their prior evidence remains in plan.md. Android/iOS hardware, real touch and performance are **unverified**. Animators currently always tick and skinned bounds stay enabled offscreen so waterline/hand sampling remains reliable; culling, skinning/material batching and device profiling are future optimization work. The jointed mannequin, simple swimwear boundaries, blank caps, hand/ball contact, stroke recovery and eggbeater mechanics need authored art/animation polish. Ball release intentionally remains at the existing gameplay location, so the follow-through is approximate. No directional dives, catches, contact/foul reactions or advanced facial/finger animation are claimed.

Read-only `git diff --check` flags whitespace on Unity-generated empty `value:` entries in the saved scene, plus line-ending normalization notices. Those native serialized entries were left as Unity writes them; this is separate from the zero-error/warning Unity Console result.

## Future Blender replacement

Replace only the model child inside the visual prefab. Keep the athlete root, motor, collider, identity, interaction/carry anchor and match references intact. Supply a valid Humanoid Avatar with the mapped body joints, animation-compatible rest pose and hands/feet/head; disable root motion. Reassign the Animator/controller and `RiggedSwimmerVisual.alignmentRoot`, preserve skin/cap/swimwear material roles (or adapt `Theme` for named slots), and retune visual head height/bounds. Clips retarget through Humanoid; do not move authoritative release/keeper anchors to match artwork. Revalidate one isolated swimmer and one real athlete before reapplying to 14. Finger Humanoid mapping, per-athlete materials/cap numbers and authored directional saves can be added later without replacing gameplay controllers.

Manual next review: compare idle/swim/sprint in the lab, play a normal hub-launched match, switch players, carry/pass/lob/charge/shoot, inspect both keepers, pause/resume, use F8 and compare all three quality presets. Finish a normal match through rewards/lobby once to reconfirm the unchanged full flow. The next recommended milestone, requiring separate authorization, is authored athlete/eggbeater/recovery/save refinement followed by Android profiling. No later milestone was started.
