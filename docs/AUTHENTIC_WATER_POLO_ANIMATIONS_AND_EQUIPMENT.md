# Milestone 28 — Water polo motion and equipment

Milestone 28 adds authored aquatic motion and premium equipment for the existing Atlas athlete. It uses the Milestone 27 body, 66-bone Humanoid Avatar, skin, kit numbering, Animator state machine, ball-hand IK and gameplay roots.

## Source assets

| Desktop source under `C:/Users/todua/OneDrive/Desktop` | Project source under `SourceAssets/Athletes` | Inspection |
| --- | --- | --- |
| `WPS_Cap_001/Meshy_AI_Blue_Water_Polo_Cap_20261124_texture_fbx/` | `WPS_Cap_001/Original/` | 4,788 triangles, one UV set and one material. The remesh has a cloth crown, a hollow face opening, inner fabric and perforated ear guards. |
| `WPS_Swimwear_001/Meshy_AI_Blue_Swim_Briefs_20261124_texture_fbx/` | `WPS_Swimwear_001/Original/` | 2,002 triangles, one UV set and one material. High leg openings, continuous waistband and closed inner and outer surfaces. |
| `Meshy_AI_Atlas_Form_20261124_animated_unity_humanoid_without_skin_60fps_All_animation.zip` | `WPS_Animations_001/Original/` | One skinless 66-bone animation FBX. Four ancillary textures in the ZIP were left at the source. |

Both equipment packs contain 2048 × 2048 albedo, normal, roughness and metallic PNGs. All ten equipment files matched the Desktop originals by SHA-256. The extracted animation FBX matched the ZIP entry and the Unity import copy by SHA-256. The project copies and their checksums are listed in `Logs/AuthenticWaterPoloAnimations/SourceHashes.json`. Blender MCP verified the live 5.2.3 LTS connection. Reproducible authoring runs used background Blender with the Milestone 27 blend file to keep the live Blender document intact.

## Geometry, fit and materials

| Part | Gameplay triangles | Other authored variants |
| --- | ---: | --- |
| Atlas body | 10,536 | Existing skin fit and UVs |
| Fitted competition briefs | 820 | 2,002-triangle supplier reference in Blender |
| Cap including chin strap | 2,322 | 4,908 cinematic and 1,172 lower-detail |
| Curved number surfaces | 192 | A separately fitted pair per cap variant |
| **Total per athlete** | **13,870** | **Four enabled skinned renderers** |

- The imported cap was transformed into the existing head’s rest space. A radial clearance pass used the production head mesh to push cloth away from the face and hair where required. The lower flaps were shortened. The cloth crown was relaxed with the protective parts largely excluded to preserve the recognizable vents, bindings and seam relief. A second rear-shell lift corrected small hairline intersections. All three variants cleared the 147 covered upper-head vertex rays that were evaluated.
- The two-sided woven ribbons follow the jaw from the guard area and meet under the chin. They are part of the cap mesh. The cap and both number patches are weighted entirely to the existing head. The print patches follow the cap surface with about 2.2 mm lateral clearance and keep the existing 512 atlas and actual roster values.
- The source briefs are a closed shell with their own waist and leg borders. For the runtime cut, the supplied fabric and roughness were reprojected onto the previous fitted 820-triangle cut. This leaves the shared garment/body coverage boundary and matching hip and thigh weights in place. A wrap-aware waistband and stitch signal yields the small normal detail. The supplier tangent-space normal map was authored for its own UV layout and was retained with the source. The existing wrap supports repeated custom patterns and logos.
- Blue, White and Red goalkeeper caps use a neutral luminance texture and separate team fabric and guard tints. The cap normal map keeps the supplier UVs. Smoothness is `1 - roughness`, clamped to 0.12–0.41 and packed into linear alpha. The metallic channel is zero for dielectric fabric. Base color is sRGB. Normal maps use the Unity normal importer. Mipmaps are enabled.
- Android overrides use 1024 for the cap maps and 512 for the briefs. Albedo and normal use ASTC 6×6 and packed smoothness uses ASTC 8×8. The texture sets and materials are shared across players. The gameplay cap pair is imported into Unity. The other two cap pairs remain in the editable source for later LOD wiring.

Editable sources are `SourceAssets/Athletes/WPS_Cap_001/Blender/WPS_EquipmentFit_001.blend` and `SourceAssets/Athletes/WPS_Animations_001/Blender/WPS_AquaticMotion_001.blend`. Use `Tools/Art/FitWaterPoloEquipment.py` and `Tools/Art/AuthorWaterPoloCycles.py` with the M27 athlete blend as the input. Their exports go into `Assets/ProductionAthletes/AuthenticEquipment` and `Assets/ProductionAthletes/AuthenticMotion`.

## Animation import and authoring

| Downloaded Unity clip | Source take | Rate and length | Use |
| --- | --- | --- | --- |
| Walking | `SmartRigArmature|Walking|BaseLayer` | 60 Hz, 1.033 s | Retargeted pool-deck preview and walkout |
| Running | `SmartRigArmature|Running|BaseLayer` | 60 Hz, 0.633 s | Retargeted pool-deck preview |
| Swim_Idle | `SmartRigArmature|Swim_Idle|BaseLayer` | 60 Hz, 3.000 s | Source take retained for comparison |

Unity imports the source Avatar as Humanoid and validates its mapping separately. The production clips map onto the existing athlete Avatar. Root motion is disabled. The `Swim_Idle` sample showed alternating sweeps over a three-second cycle, with some ankle crossover and travel behind the hips. The authored eggbeater uses shorter, consistently separated sweeps. The ceremony samples the walking take by travelled deck route distance and uses the lower of the two estimated sole heights for limited vertical correction. The existing lineup, pennant, huddle and stage timing remain in the same controller.

| Authored Blender Action and Unity clip | Duration | Motion |
| --- | ---: | --- |
| WaterPoloFreestyle | 1.400 s | Entry, catch, pull, push, exit, high-elbow recovery and extension. Mirrored left and right phases offset by half a cycle. |
| WaterPoloSprint | 1.083 s | Shorter reach and quicker turnover, with a separate hip/ankle flutter profile. |
| WaterPoloEggbeater | 1.233 s | Alternating lateral and fore-aft ankle loops, outward/forward knees, foot pitch and subtle hands-out sculling. |
| WaterPoloBallTread | 1.233 s | Eggbeater support, raised right throwing side and free-hand scull. The existing match IK still drives the held-ball target. |

The authoring script solves wrists and ankles with segment lengths and explicit elbow and knee bend planes. The flutter initiates at each hip. Three waves per leg offset by half a kick create six alternating beats per full stroke cycle. Shin and ankle motions lag the hip. Eggbeater uses timed outward sweep, inward sweep and recovery positions with an independently oriented foot. The cyclic curves close at the same pose. Lumbar and chest pitch keep the athlete flatter. A mostly fixed head orientation shares extension with the neck. The exported root is stationary.

The existing Animator override replaces the three locomotion clips and the possession clip. Existing action masks and triggers continue through the original pass, shoot, block and save clips, with the new leg cycle while upright. The visual waterline targets the head joint at approximately 0.15 m above the surface in tread and 0.12 m in crawl, plus small appearance-only buoyancy and brief action lift. The motor, root Rigidbody, shot timing, ball ownership and keeper logic keep their existing authority. No separate ball or dribble rules were added.

The art reference for stroke phases was de Jesus et al.’s filmed comparison of head-up, head-down and ball-leading front crawl. The eggbeater was based on the filmed outward, inward and recovery sequence and the importance of lateral travel and foot angle. The project clips are authored interpretations. [Front crawl study](https://pubmed.ncbi.nlm.nih.gov/22409603/) · [Eggbeater foot kinematics](https://pmc.ncbi.nlm.nih.gov/articles/PMC13342048/)

## Inspect in Unity

- **Tools > Water Polo > Athletes > Play Athlete Preview** opens the saved one-athlete preview. Tab toggles standing and aquatic. In the aquatic view, 1–8 select tread, crawl, sprint, possession, pass, shoot, block and save. Left/Right orbits, O toggles overhead, U toggles the low angle, and F1–F3 frame body, cap and briefs. P toggles slow playback. F4 and F5 play walking and running. Right Bracket steps the deck clip by one 60 Hz frame.
- **Tools > Water Polo > Athletes > Open Athlete Preview** opens the saved scene in Edit Mode. On **Athlete Equipment**, change Preview Team, Preview Goalkeeper and Preview Number. The two **Configure** commands import and bind the new assets when rebuilding that scene.
- **Tools > Water Polo > Play Olympic Match Arena** isolates the production match. **Tools > Water Polo > Play Hub Entry** starts the normal Boot flow. **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** restores the default after direct arena testing.

## Ceremony and replay

The ceremony still copies visible renderers and their existing bones, shared meshes and cap-atlas property blocks. During the team walkout, the temporary copy of the animation root gets a manual Humanoid playable using the existing Avatar. It samples the walking clip as the established deck route advances, with a 1.36 m per-cycle stride calibration. The graph is discarded when the actor stops walking. No Animator or altered timing was added to goal replay.

The earlier mannequin-like lineup was reproduced in the Editor. The ceremony copies contained fourteen Atlas body meshes and the correct material references. Low diffuse exposure and the strong reflection response in the tunnel created the uniform dark appearance. The final ceremony uses shared, temporary copies of the cast materials with a textured diffuse exposure floor and lower environment reflection. The copies are disposed at the end of the ceremony. The pool, live skin, replay skin, referee visuals and arena lighting keep their prior references. See the before and after in [Ceremony-Exposure.png](Evidence/AuthenticWaterPoloAnimations/Ceremony-Exposure.png).

The goal replay buffer continues storing actual bone poses, ball positions and cap numbers. Both playback angles observed the recorded action in the isolated and full-roster checks. The existing restoration to live play and the restart proceed from the goal presentation director.

## Measurements

Unity 6000.6.7f1. Windows Direct3D 12 on RTX 5060 Ti and i5-12600K. Thirty samples at 983 × 494 for each equipment set with the same parked camera, athlete positions and frozen poses. Render totals include the arena and render passes.

| Measurement | Milestone 27 kit | Milestone 28 kit |
| --- | ---: | ---: |
| Roster mesh triangles | 179,872 | 194,180 |
| Enabled skinned renderers | 56 | 56 |
| Average rendered triangles | 2,129,965 | 2,182,070 |
| Average draw calls | 1,029.0 | 1,027.0 |
| Referenced texture object memory in this Editor run | 19.36 MiB | 41.53 MiB |

The benchmark used separately initialized renderers for the old kit. The first comparison triggered Unity GPU skinning errors when swapping meshes on the active renderers, and those readings were superseded. The six new mobile texture maps have an estimated 1.9 MiB ASTC build payload with mipmaps. That estimate excludes engine overhead and fallback formats. CPU and render-thread frame timing were unavailable in the sampled Editor counters. Android ADB listed no attached device. Phone FPS, skinning time, GPU time, memory, thermals and device controls remain unverified.

## Verification and limitations

- Milestone 27 appearance was captured before the change on the standing, tread and swim preview. The new preview captures contain eight phases each for the side, overhead and low-angle crawl and low-angle eggbeater. Side eggbeater, sprint, front crawl, walking and running frames are also saved. The preview’s sampled head, hips, hands and leg joints returned to their initial positions at millimetre precision for crawl, sprint and eggbeater. The low-angle preview uses unobscured geometry for inspection. The match captures show the real transparent water and competition camera.
- The isolated arena first installed Milestone 28 on Blue #1 with thirteen Milestone 27 athletes. Full stage timing, active walk on the copied Humanoid, real keyboard swimming and sprint, the physical pass and catch, held-ball IK, one scored shot and both recorded replay angles passed. The last run measured crawl head-joint elevation at +0.122 m and pelvis at −0.094 m. In tread those values were +0.141 m and −0.502 m. The held ball was 0.074 m from the right wrist at the sampled frame.
- The rollout kept the production scene GUID `5c4d3741df6b7f54e8cfd34d391d78d1`. The arena structure check found fourteen equipped avatars, four visible pieces per athlete, no added physics under the visual rigs and unchanged Build scene entries. The benchmark checked all fourteen roster numbers and field/keeper palette assignments. Queued Input System keyboard events verified the extended preview controls.
- Fresh full-roster checks passed all eight ceremony stages, pointer pause/resume, physical sprint pickup with AI enabled, and camera-relative keyboard movement in both gameplay cameras. The goal suite covered Blue and White goals, both own-goal cases, attributed deflections and unknown credit. Boundary tests covered both sides and ends, the keeper corner, physical pass restarts, and halftime end reversal. A wide penalty returned to a legal goal throw. The physical five-metre goalkeeper catch passed with deterministic save and catch chances. The White third-foul test replaced cap 3 with cap 6, restored six active field players and verified the new number atlas cell. A separate test passed contextual selection and shot-clock turnover.
- Boot → Lobby → Match → Results → Lobby passed with an in-memory progression profile. The test exercised restart and exit during the intro, both replay angles, replay skip, the real centre restart, four physical sprint acquisitions, one result and reward deduplication. Period endings were accelerated. The real profile file’s SHA-256 matched before and after the suite.
- The original pass, shot, save and block upper-body clips are still used. There is no dedicated ball-leading crawl or additional keeper-specific directional clip in this pass. Walkout step phase follows route distance with estimated sole correction. Residual foot slide at route bends and the roughness of the supplier ear-guard detail remain candidates for artistic review. The lower- and higher-detail caps are editable Blender variants. The game uses one cap detail level.
- The fitted briefs retain the coverage seam and their existing weights. Sampled preview poses cover the motion families. A comprehensive extreme-pose clipping sweep and physical-device review remain open.
- The first redirected regression runs hit missing screenshot/report folders and a missing profile checksum fixture. The legacy penalty-save check initially timed out before release and passed after the verification keyboard was routed to the Game view. Raw outputs are in `Logs/AuthenticWaterPoloAnimations/Regression/`. The first structure run refreshed the ignored M27 `Logs/ProductionAthletes/structure.json`. The new run is retained in the M28 regression folder. Final Unity validation reported zero current errors and warnings, with the arena saved in Edit Mode and Boot selected for Play.

[Selected Game View evidence and frame sequences](Evidence/AuthenticWaterPoloAnimations/) · [Exact changed files](MILESTONE_28_FILES.txt)

## Recovery

In saved Edit Mode, **Tools > Water Polo > Athletes > Restore Milestone 27 Athlete Motion and Equipment** rebinds the old cap, number surfaces, kit and controller on the existing arena. **Apply Authentic Athlete Motion and Equipment** installs the Milestone 28 bindings. Use **Configure Authentic Motion Preview** and **Configure Premium Equipment Preview** to rebuild the standalone inspection scene. Keep Boot as the Editor Play entry after direct arena checks.

The native pre-rollout scene is `Assets/Scenes/Recovery/OlympicMatchArena_PreAuthenticAthletes.unity`. The recovery file bytes were checked against the original arena before rollout. Its SHA-256 is `2DE9181C9B0441F56EAA898D43FA3E1CD1CB046FE1C9477FF922FC89D29A9BB6`. It has its own GUID and is outside player builds. Both recovery and reapply were verified on all fourteen bindings. Reapplying produced the same saved arena hash. The original cap, briefs, skin, kit definitions, controller and Avatar remain available.
