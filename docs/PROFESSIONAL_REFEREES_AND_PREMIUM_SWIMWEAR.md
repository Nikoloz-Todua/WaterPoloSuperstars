# Milestone 29B — Professional officials and premium swimwear

## Current swimwear in Milestone 30H

The saved arena uses the registered `AtlasCompetitionBriefs.fbx` with 2,688 triangles and 66 bone bindings on all fourteen athletes. Shared Blue and White URP Lit materials now use fabric maps baked from the waistband, opening and side masks in `SourceAssets/Athletes/WPS_Swimwear_001/Blender/AtlasCompetitionBriefs.blend`. The finish was checked in a dry comparison and in advancing production crawl under the existing water. [Diagnosis](MILESTONE_30H_DIAGNOSTIC_REPORT.md) · [Rendered evidence](Evidence/Milestone30H/)

## Milestone 29B historical configuration

Both saved OlympicMatchArena official roots used the skinned Meshy model. The 2,002-triangle swimwear fit candidate failed the body-coverage gate. The 820-triangle competition cut was the production configuration at that checkpoint.

## Referee source and rig

- Desktop source directory is `C:/Users/todua/OneDrive/Desktop/WPS_Referee_001/Meshy_AI_Man_in_White_Polo_and_Pants_20261124_texture_fbx/`. Its FBX and four PBR PNGs are preserved in `SourceAssets/Officials/WPS_Referee_001/Original/`. All five copied files match the Desktop SHA-256 hashes.
- `Tools/Art/PrepareProfessionalReferee.py` fits the A-pose to a separate copy of the project's Humanoid bone hierarchy, assigns normalized regional skin weights and preserves the model's seam UVs. The single mesh has 8,316 triangles and 4,160 welded vertices. The saved Blender rig source is `SourceAssets/Officials/WPS_Referee_001/Blender/PoolsideOfficial.blend`.
- The Unity prefab at `Assets/Equipment/ProfessionalReferee/Professional Official.prefab` is scaled to about 1.80 m, with its own Animator and bone transforms. The importer references the compatible Humanoid Avatar asset. It reuses the existing 60 Hz `Walking` take from `MeshyDeckSource.fbx`. The original athlete rig and controller remain available.
- Both officials share one URP Lit material. The supplied albedo supplies skin, hair and clothing color. The material uses sRGB albedo, a normal map at 0.5 strength and a linear matte smoothness texture derived from roughness and clamped to 0.12–0.36. Metallic is zero. The mipmapped Android overrides use 1024 ASTC 6×6 for color and normal and 8×8 for smoothness. Estimated ASTC payload is about 1.55 MiB including mipmaps. Live lighting uses the existing stadium. In ceremony, both copies share a temporary lower-reflection material with the existing cast exposure correction.

## Movement and match events

`PoolsideReferee` still moves each original deck root and observes existing match events. The professional visual uses in-place gait sampled by deck distance at a 1.36 m reference stride, turns the visual toward travel and applies a bounded sole correction. The referee prefab adds no collider. Existing whistle audio and match-rule decisions are independent of these poses.

`ProfessionalRefereePose` covers relaxed standing, walking, a whistle-to-mouth gesture, a centre-point after a goal, restart arm lowering, foul and boundary directions, an exclusion sweep and a raised penalty arm. These are simplified upper-body representations of the signals. They do not include the cap-number finger sequence or a modeled whistle and lanyard. [World Aquatics rulebook signal diagrams](https://www.worldaquatics.com/sites/default/files/2019_2021_wp_rules_congress_amended_06012020_0.pdf) · [Referee manual](https://www.worldaquatics.com/sites/default/files/_july-2020-fina-water-polo-referees-manual-2019-2021-clean.pdf)

The prematch cast copies the current skinned model and standing pose for both officials. It keeps the existing lineup and pennant positions, timing and skip behavior. The replay system still reproduces recorded athlete and ball transforms. Officials visible in the surrounding arena use the new live visuals. Their poses are outside the recorded athlete-and-ball buffer.

## Meshy swimwear fit and coverage gate

The previous milestone used its 820-triangle fitted shell so the athlete body cut, UVs and weights matched throughout animation. It used reprojected Meshy fabric instead of the supplier geometry. The supplier FBX and PBR maps were already under `SourceAssets/Athletes/WPS_Swimwear_001/Original/` from `C:/Users/todua/OneDrive/Desktop/WPS_Swimwear_001`. These files were reused.

`Tools/Art/FitPremiumCompetitionBriefs.py` fits the actual source shell to that athlete in the existing rig. The candidate retains 999 welded vertices, 2,002 triangles, the waistband and both sides of the closed fabric shell. It keeps the Meshy loop UVs. The fit transfers up to four normalized influences by barycentric interpolation on the old fitted surface and binds the existing Humanoid bones. The editable candidate is `SourceAssets/Athletes/WPS_Swimwear_001/Blender/PremiumCompetitionBriefs_FitCandidate.blend`.

Standing and 17 sampled frames of freestyle, sprint, eggbeater and ball tread failed the matching-coverage test. Rays within 30 mm on either side of each prior coverage panel missed about 24% of sampled panel area standing, 23.9–24.5% in freestyle and sprint and 18.2–19.6% in tread poses. The audit also found 374–622 body/shell triangle overlap pairs. Those figures describe the geometric coverage proxy and BVH candidate contacts. The Blender close-ups show gaps at the waist and pinching at the rear. There is no Unity import or roster rollout for this candidate. [Front, side and rear candidate inspection in Blender](Evidence/Milestone29/Briefs-Candidate-Not-Shipped.jpg)

Production keeps the fitted shell, previous cut and weight compatibility, existing neutral fabric and team materials, and dynamic cap numbers. For another fitting pass, register the three garment openings to the Atlas body, derive the body cut from the repaired openings and test coverage through standing, swimming, throwing, blocking, walking and celebration before enabling it on one athlete. The current candidate does not establish complete coverage in those actions.

## Aquatic presentation and performance

Regular and sprint dribbling use the existing free-arm head-up crawl clips. The focused changes are lead clearance, recovery, and surface and flight spin. No new freestyle or eggbeater Action was promoted. The Game View depth probe found the hip joint 0.077–0.106 m below the waterline in crawl and 0.029–0.066 m below in sprint, and showed alternating arms around the ball. The pool's clear surface leaves submerged legs visible, especially in the high replay angle. The measured live poses gave no reason to lower the swimmer root or change the water renderer. The replay remained on sampled bone positions. [Crawl sequence](Evidence/Milestone29/Crawl-Sequence.jpg) · [Sprint sequence](Evidence/Milestone29/Sprint-Sequence.jpg) · [Live match](Evidence/Milestone29/Live-Match.png)

| Measurement | Milestone 28 reference | Milestone 29 |
| --- | ---: | ---: |
| Triangles per athlete / roster | 13,870 / 194,180 | 13,870 / 194,180 |
| Enabled skinned renderers, roster / officials | 56 / provisional | 56 / 2 |
| Triangles per official / two officials | Provisional | 8,316 / 16,632 |
| Ball triangles | Previous sphere | 3,014 |
| Production briefs triangles per athlete | 820 | 820 |
| Unique materials, athletes + officials + ball | — | 13 |
| Referenced Editor texture objects | 10, roster only | 16, roster + officials + ball |
| Referenced Editor texture memory | 41.53 MiB, roster only | 76.84 MiB, roster + officials + ball |
| Average Unity draw calls | About 1,027 in the M28 parked camera | 845 lengthwise / 1,023 sideline |

The M29 draw-call sample used 30 advancing Windows Editor frames per camera at 894 × 476 on Direct3D 12, with roster movement parked before sampling. Render totals averaged 2.05M and 2.14M triangles including arena passes. The M28 comparison used a different camera and resolution. Texture memory describes referenced Editor objects. The six ball and referee maps add an estimated 3.1 MiB of mipmapped ASTC payload on Android. ADB found no attached device. Phone FPS, thermals and actual graphics memory remain unverified.

## Verification and recovery

- Unity sampled a valid in-place referee Humanoid in the Game View with one renderer, 8,316 triangles and a 0.343 m maximum sampled left-foot movement in the walking pose check. Standing, walk and goal-point poses were visually inspected. Both saved officials were verified in the formal lineup and pennant exchange. The full ceremony fixture advanced all eight stages to live match. [Live referee pose samples](Evidence/Milestone29/Officials-Pose-Samples.jpg) · [Both ceremony officials](Evidence/Milestone29/Officials-Ceremony.jpg)
- The accelerated Boot fixture launched an official match through the lobby, exercised restart and ceremony exit, then reached results and the lobby through the normal flow. It passed four physical sprint acquisitions, both replay angles, centre restart and result receipt deduplication on an isolated in-memory profile. The user profile hash was unchanged. Result is `Logs/Milestone29/Regression/PreMatchPresentation/Hub-PASS.json`.
- Signal sweeps, walking and deck contact have limited sample coverage. A complete deck foot-plant polish pass and every individual live gesture from both sides were outside the verification achieved here. Android FPS and thermals were not measured.
- Raw logs and captures are under `Logs/Milestone29/Officials/` and `Logs/Milestone29/Regression/`. The candidate's pose-by-pose audit is `Logs/Milestone29/BriefsCoverage.txt`.
- In saved Edit Mode, **Tools > Water Polo > Arena > Restore Previous Officials** shows the previous per-root models. **Tools > Water Polo > Arena > Apply Professional Officials** restores the production Humanoid. Both use the same two roots. The previous visual children remain in the scene for recovery.
- The combined ball and officials recovery test restored both old representations, reapplied both production representations and verified an identical scene on repeated Apply. It verified the original arena GUID and the Boot Play entry. Result is `Logs/Milestone29/Regression/DeliveryAudit.txt`.
- For the shipped athlete equipment and clips, **Tools > Water Polo > Athletes > Play Athlete Preview** shows the current preview. **Tools > Water Polo > Athletes > Restore Milestone 27 Athlete Motion and Equipment** and **Apply Authentic Athlete Motion and Equipment** remain available. The 29B fit candidate is a Blender-only asset and needs no production rollback.
