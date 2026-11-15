# Production arena and clear pool water

## Latest presentation — Championship Night

**2026-11-15:** the same production OlympicMatchArena now has a dark navy championship treatment, rebuilt deck/technical areas/seating, perimeter light banks, a larger live jumbotron and a scene-local HDR grade. The pool shell, water shader/materials/profiles, dimensions, 14 rigged athletes, audience data and interactive-water systems are unchanged. A new 128 px reflection bake belongs to **Assets/Arena/ChampionshipNight/**. No external art/package import was needed.

Use **Tools > Water Polo > Arena > Apply Championship Night Overhaul** on saved Edit Mode scenes, or **Refresh Championship Night Reflection** after deliberate lighting edits. The older polish command preserves the newer venue and the older reflection command delegates safely. New scene-local presets retain neutral grading at every level, budget bloom/lights separately and preserve the existing water/crowd/prop tiers. Exact pre-pass recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreChampionshipNight.unity**, outside builds. Read [the premium arena guide](PREMIUM_ARENA.md) for hierarchy, budgets, sources, fresh checks, actual screenshots and limitations. Current Boot entry is restored.

## Previous venue polish — same production scene

**2026-11-15:** OlympicMatchArena was enhanced in place with cobalt/white event aprons, neutral 0.6 m ceramic decking, protected referee lanes, technical-area props, grandstand aisle/section framing, full-height upper enclosure/clerestory treatment, lighting trusses and original Superstars venue signage. Existing pool, fourteen athletes, fans, physics, field dimensions, camera and HUD remain. **Championship Venue Finish** is a reusable child prefab under the existing foundation; assets are in **Assets/Arena/VenuePolish/**. No external art/licensing import was required.

The existing directional light uses 68° pitch, 1.65 neutral intensity and 0.48 shadow strength with balanced trilight fill. Emissive rig surfaces and a new 128 px HDR reflection bake improve presentation without adding real-time lights or GI lightmaps. Water shaders/materials and existing Low/Medium/High behavior remain intact. End-hung and poolside displays read real match scores/clocks through presentation-only ArenaEventDisplay; they add no match authority, video feed or UI canvas.

Use **Tools > Water Polo > Arena > Polish Existing Championship Venue** on saved Edit Mode scenes; repeating it preserves tuning. **Refresh Venue Reflection** explicitly rebakes after lighting edits. Existing Open/Play Direct/F8 remain; Boot entry is restored for normal use. Exact pre-polish scene recovery is **Assets/Scenes/Recovery/OlympicMatchArena_PreVenuePolish.unity**, outside builds. The older field-dimension regeneration command rebuilds the foundation: reapply the polish afterward.

Fresh focused verification confirms 664 unchanged original component records, valid references, no new colliders/light budget, real swimming/pass/scoring, HUD Pause/Escape, six live score faces, clear sampled broadcast sightlines, three quality presets and existing menu launch/return without user-profile writes. Actual1920×1080 paired views and lighting/waterline captures are in **Logs/ArenaPolish/**; details/limits are in the newest [plan handoff](../plan.md). Added source content is 86 renderers / 14,088 referenced vertices, approximately 2.5 MB including metas; these are not mobile performance measurements. Geometry remains procedural, displays are text only, and device profiling is deferred.

## Current production environment — OlympicMatchArena

**2026-11-15:** the explicitly authorized arena replacement promotes WaterVisualTest's visual architecture into **Assets/Scenes/OlympicMatchArena.unity**. The new scene retains the existing fourteen-athlete gameplay authorities and serialized references. The original **PrototypePool** and **WaterVisualTest** scene bytes remain unchanged from session start. **Assets/Scenes/Recovery/PrototypePool_PreArena.unity** is an additional exact gameplay recovery copy. None of those three scenes is included in the production build list.

Normal flow: **Boot → MainLobby → PLAY → Quick Match → OlympicMatchArena → full-time rewards → MainLobby**. `Resources/HubMatchDestination.asset` selects the arena; absent that asset, routing falls back to PrototypePool. Repeat-safe M12 hub setup respects the production destination. No profile/catalog economy values were changed.

### Open, inspect and configure

- **Tools > Water Polo > Arena > Open Olympic Match Arena** opens the saved production scene in Edit Mode.
- **Set Up Olympic Match Arena**, in the same submenu, builds once; subsequent calls validate/open the existing scene without rebuilding tuned content.
- **Play Olympic Match Arena Directly** opts into isolated gameplay. Restore normal entry with **Tools > Water Polo > Use Boot as Editor Play Entry** or **Play Hub Entry**.
- **F8**, in Editor/development builds, cycles overview, underwater anatomy, waterline reflection, benches, reverse end, then broadcast. It uses the same camera/listener; gameplay continues. The original camera/tuning remains available in untouched PrototypePool and its recovery scene.
- **Activate Verified Arena for Lobby** registers the destination/build scene only after an integration PASS report. It was run after direct integration passed and followed by real-pointer hub verification.
- Edit **Assets/Arena/Settings/CompetitionDimensions.asset**, stop Play, save intended scene edits, then **Tools > Water Polo > Arena > Apply Configured Field Dimensions**. This explicitly rebuilds generated arena geometry and recalculates competition starts/formations. Scene identity, reusable prefab/material/profile and broadcast camera tuning are preserved. Reverify gameplay/framing after dimension changes; 25 m is the tested default.

### Why the fixture looked better

The old match used flat ambient lighting, a dark background, narrow decks and shallow placeholder bodies. The earlier additive water integration left that presentation in place. WaterVisualTest supplied a real 2.05 m deep ceramic shell, metre-scaled tile UVs, inward-facing walls, white coping/mosaic waterline, warm daylight, trilight fill, recognizable hall reflections, dedicated Forward URP budgets and full submerged anatomy. Its close perspective also exposed these improvements.

The arena recreates that complete construction using the **same fixture ceramic, enamel, steel, daylight-panel and water materials**, matched 1.6-intensity warm directional lighting and ambient colors. The fixture's swimming lanes/starting blocks are replaced by water polo markings. The new scene was initialized by copying the working gameplay scene, preserving internal references, then removing its retired water/floor/marking/deck presentation. Only one tiled shell and one water surface remain. The three demonstration swimmers and test camera remain exclusively in WaterVisualTest; their visual construction informs the new athlete prefabs.

### Hierarchy and reusable assets

`Olympic Arena — WaterVisualTest Foundation` contains the tiled pool, wide non-slip deck/drainage/referee inlays, water polo markers, Aquatic Centre Architecture, team benches/coaches/equipment, officials' table, spectator stands, Medium/High audience batches, transparent water, baked reflection, High planar-reflection rig and developer viewpoints. Existing Gameplay, Actors, goal physics, camera, HUD, EventSystem and referee/effect authorities remain separate.

`Assets/Arena/Prefabs/` holds **BlueTeamBench, WhiteTeamBench, TieredSeatingBlock**, and Blue/White field/keeper visual prefabs. Benches have seven substitute seats, a coach seat, equipment trunk, bottle/towel placeholders and access space. Ten repeated seating blocks supply six tiers, aisle rails and 480 seats; audience density is bounded separately. Hall walls, upper daylight panels, ceiling/trusses, emissive light fixtures and original Water Polo Superstars/ToduStudios banners frame the action. Roof geometry does not cast broad shadow bands over the pool. Fixtures introduce no extra real-time lights. Tiled concourses fill the perimeter around stands and end decks.

Floats, seating, rails, equipment, roof and audience meshes are combined by material. Audience uses a 54-vertex ellipsoid instead of high-resolution primitives. Final Edit Mode inventory: **456 renderer components / 166,801 referenced mesh vertices**, including disabled original athlete visuals and repeated meshes. Arena source assets occupy about **6.3 MB** on disk. These are content/source counts, not measured draw calls, player memory or build size.

### Dimensions and gameplay integration

Default field: **25 m between goal lines × 20 m between sidelines**. Physical shell: **28.6 × 22.5 m**, including 1.8 m behind each goal and 1.25 m outside each sideline. Water depth **2.05 m**; deck width **7 m**, deck top **Y=0.18**, water/swimmer root **Y=0**. Field, shell and deck are separate measurements; players, goals, ball and physics are not globally scaled.

Reference: [World Aquatics Competition Regulations, February 2026, Part Six](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf), Articles 20.2–20.7 and 21.6–21.7. The official PDF was downloaded/extracted outside Assets. Markers use red at 2 m and 5 m, yellow at 6 m, white halfway/goal references and green midfield ropes. End ropes sit 0.3 m behind goal lines, with red goal-area/re-entry sections. Re-entry areas sit behind goals on the bench side, beside a flying-substitution lane. Subtle floor outlines identify the 2 m goal areas; race lanes are absent. This is a competition-inspired arena, not a claim of full rule certification or new substitution gameplay.

Setup updates authoritative PoolBounds, relocates existing unit-scale GoalMouths to **Z=±12.5 at waterline**, refreshes their physical views, adjusts original side/end barriers, fourteen start markers/poses, six-role layouts and referee offsets. Goal openings stay **3 × 0.9 m**, depth **1 m**, posts **0.08 m**. Attack/zone targets retain distances from current goals; start depths follow field length and lateral spacing follows width. Existing centre ball/restart markers remain central. Goal detection, possession, shot flight, motor/AI APIs, clocks, fouls, pause and match phases remain with their original authorities and execution ordering.

Referees occupy **X=±12.65, Y=0.18** on unobstructed side deck paths. Benches are farther out and stands beyond the safety deck. MatchRules has an arena-only opt-in to park excluded athletes in the marked water behind their **current defended goal**, including after halftime. Original PrototypePool keeps legacy parking. Decisions, timers, foul history and collider control remain in MatchRules.

### Water, lighting and presets

The same clear transparent water retains moving normals, depth tint, animated caustics, restrained refraction and Fresnel reflections. There is no ocean displacement/basic blue replacement plane. `ArenaQualityController` observes existing GamePreferences, switches arena-only pipeline/profile budgets and restores the previous pipeline on unload. `PoolWaterVisuals` owns a runtime material copy and per-camera depth/opaque overrides. Persistent assets are not mutated during gameplay.

| Settings preset | Arena budget |
| --- | --- |
| Low | 0.85 scale, 2× MSAA, depth/normals and baked 128px reflection; no opaque-copy refraction/caustics/planar pass, audience or fine equipment; camera shadows off. Submerged limbs and floor stay visible. |
| Medium | 1.0 scale, 2× MSAA, half-resolution opaque copy, refraction/caustics and baked reflection; 160 audience placeholders/fine equipment; no planar pass; existing preference policy leaves camera shadows off. |
| High | 1.0 scale, 4× MSAA, full opaque copy, refraction/caustics, baked fallback and 0.3-scale planar reflection; 320 audience placeholders, fine equipment and 2048 directional shadow map. |

High adds a reflection render camera, not another gameplay-camera authority. It still incurs upstream per-frame render/CPU/allocation/bandwidth costs. Low/Medium use one camera. Existing pooled wakes/ripples observe swimmers; authoritative water contacts now also activate the existing pooled splash mesh. Capacity remains 16. MatchAtmosphere plays an original generated eight-second mono crowd ambience, using the preserved crowd volume setting. **No Android/iOS performance measurements were made.**

### Blender visual replacement and camera

Every original athlete keeps identity, Rigidbody/capsule, motor, AI, stamina, selection and carry/release anchors. Old mesh renderers are disabled. A visual-only prefab at local **Y=-0.42** supplies opaque torso/head/cap/suit/arms/legs. Caps/suits distinguish teams; keepers have red caps and their existing arm-lift observer references point to the new pivots. No heavyweight rig was created. Fixed team visuals do not extend collection stat/cosmetic scope.

Replace only the **ArenaAthleteVisual** child prefab for Blender art. Its skin/cap/swimwear renderer arrays document slots. Preserve external gameplay roots/anchors; use closed bodies in metres, opaque URP Lit depth participation and animation-inclusive skinned bounds. Test animated limbs crossing waterline. Disable/remove the primitive pose adapter when installing an Animator. Transparent hair/eyewear still requires separate review.

GameplayCamera remains the adaptive broadcast authority, tuned to 14.4 m base height / 10.8 m distance / 53° pitch / 52° FOV, retaining prediction, smoothing, user zoom and keeper/set-piece modes. F8 temporarily suspends only that camera's presentation update, then returns through its existing snap API. Both actual attack directions were checked after the authoritative halftime reversal, with goals inside the HUD-safe view. Original fallback tuning survives in both preserved gameplay scenes.

### Fresh verification and evidence

Run asynchronous entries with **script run**, never return Tasks through synchronous eval:

`unity command script run --file Tools/Verification/ArenaVerification.cs --entry ArenaVerification.<entry> --caller plugin --skill unity-pipeline --format json`

Machine reports and actual Game view PNGs are outside Assets in ignored **Logs/Arena/**.

| Entry | Fresh Windows Editor result |
| --- | --- |
| Independent WaterVisualVerification.Rendering | PASS first: advancing fixture, all quality variants, submerged anatomy/effect A/B and moving water. Fixture remains independent. |
| Setup | PASS: original scene bytes/exact recovery, fourteen IDs/anchors, one pool/water, no test swimmers/scenery colliders, one Canvas/EventSystem, required CanvasRenderers, no missing scripts, repeat-safe scene GUID/bytes. |
| Integration | PASS: frames 246→2339; AI bounds/referee deck, three water variants, real W/Shift/C and pointer joystick/Pause/charged Shoot, physical pass reception, airborne lob, goal/conceding restart, exclusion/stopped rule clocks, four periods/one end swap. Goal fixtures clear the lane/disable keepers; catches have a separate test. |
| Appearance | PASS final: both actual attack ends after halftime, goal viewport Y≈0.687 / 0.283; six wholly submerged real-athlete meshes contribute **2,996 pixels**, shadows excluded. Final broadcast/detail/overview/reflection/bench captures inspected. |
| ScreenSizes | PASS: actual 1920/2340/2400 ×1080 with simulated 70px lateral/24px vertical safe insets; controls in bounds, pointer Pause/Settings, actual Low/Medium/High clicks switch pipeline/water/audience/reflections, real F8 works. Broadcast/Settings images inspected. |
| WaterImpact | PASS: physical lob water contact activates a pooled splash; capacity 16. |
| Keepers | PASS: both relocated goals support physical swept catches; one owner/no goal; Blue possession selects keeper. Controlled probability 1/reaction 0 and 80 m/s test shot; original tuning restored. Not a default balance test. |
| HubFlow | PASS: Boot/lobby, real PLAY/Quick Match pointers, fourteen collection mappings, physical goal/four periods/end swap, once-only receipt, reward reload, real rewards/lobby pointers, abandoned training grants nothing; production profile hash unchanged during isolated fixture. |
| Hub Core / M12 Navigation | PASS fresh: 18 economy/persistence/recovery groups; pointer Shop, twelve populated pages, keyboard Back/stable identity. Historical 36-page layout matrix not repeated. |
| Compilation / Console | Compiled without errors; final ground truth 0 errors / 0 warnings. Historical Pipeline buffers contain resolved earlier messages. |

Images: **Final-Arena-Overview.png, Final-Broadcast.png, Final-Reflections.png, Final-Team-Benches.png, Real-Athlete-Underwater.png, Verified-North-Attack.png, Verified-South-Attack.png, Screen-1920-Broadcast.png, Screen-2340-Settings.png, Full-Time.png, Rewards.png, Returned-Lobby.png**. Prefer Final/Screen captures over First-* images taken before presentation refinements.

### Assets, licenses and limits

Arena meshes, benches, tile-derived deck, banners, audience, temporary anatomy and generated ambience are original project assets. No additional third-party environment/character pack was imported. No protected Olympic logos, event branding or team identities are used; OlympicMatchArena is a scene name. Existing MatrixRex/Cyanilux MIT and source/caustic notices remain as documented below, including player-distributed StreamingAssets notices. The World Aquatics PDF is reference-only in ignored Temp.

No Android/iOS build, hardware profiling, phone multitouch or Blender import occurred. Primitive athletes/audience/props remain temporary art. Detailed substitute animation and actual substitution gameplay were not added. Refraction/caustics are screen-space approximations; underwater cameras, transparent character materials, XR/stacking and multiple water levels remain unverified. High reflections need device budgeting. Original simplified match rules/clocks/third-foul removal remain. This verification does not certify every historical gameplay edge or every hub page at every phone size. Current files/handoff are in `plan.md`.

---

## Historical fixture and additive PrototypePool handoff

Implemented and verified in Windows Unity **6000.6.7f1**, URP **17.6.0**, D3D12 on 2026-11-15. The separate graphics fixture was verified first, then its water presentation was applied additively to PrototypePool. Existing gameplay/physics authorities remain intact. This does not advance the milestone roadmap or complete pending M12 hub acceptance.

## Open and run

1. Save intended scene edits and stop Play Mode.
2. Use **Tools > Water Polo > Water Visuals > Set Up WaterVisualTest**. It creates missing assets/scene on first run and opens the existing scene thereafter, preserving scene identity, material/profile tuning and authored layout.
3. Use **Tools > Water Polo > Water Visuals > Play WaterVisualTest**. This temporarily bypasses the Boot Editor entry, then restores the exact previous entry when Play stops. This is useful whether the previous entry was Boot or direct-scene mode.
4. In Play Mode, **1 / 2 / 3** select Low / Balanced / High. **V** cycles anatomy detail, overview and reflection-angle views. Stop Play when finished.

The scene is `Assets/Scenes/WaterVisualTest.unity`. It is deliberately absent from the game's build scene list and Boot/lobby navigation. The test has no Canvas, EventSystem, gameplay athletes, ball, match authority, colliders or save transactions.

## Appearance and controls

A 14m × 25m pool has an actual tiled floor **2.05m below** the surface, inward-facing tiled walls, lane markings, a waterline band and coping. Pool hall windows, ceiling strips and starting blocks supply reflection landmarks. Three opaque test rigs show a surface swimmer, an upright partly submerged swimmer and a fully submerged swimmer. Their bounded visual animation is independent of gameplay.

`CompetitionPool_High.mat` uses the imported **UberStylizedWater** prebuilt shader. Slow counter-moving ripple normals, restrained screen refraction, world-depth coloration, Fresnel reflections and animated parallax caustics supply the water appearance. Ocean displacement, foam, shoreline bands and the underwater foam layer are disabled. The water mesh has no collider and never decides swimming height or ball response.

Tune the three materials under **Assets/WaterVisuals/Materials** using the included material Inspector. Useful controls:

| Control | Current reference / purpose |
| --- | --- |
| Shallow / deep color alpha | 0.12 / 0.58; controls absorption/tint without hiding submerged anatomy |
| Water Depth | 2.5m coloration falloff; separate from actual pool depth |
| Normal Map / Strength / Scale / Pan | Normal 1; 0.48 / 1.8 / 0.055; surface ripple detail and speed |
| Refraction Strength | 0.045 near, 0.008 distant; keep small to preserve anatomy and lane readability |
| Reflection Strength / Fresnel / Distortion | 0.62 / 3 / 0.6; strongest at grazing angles |
| Caustics Scale / Pan / Strength | 1.25 / 0.075 / 5.5; the source map uses dark dual samples, so this strength is intentional |

Quality changes instantiate a scene-owned material once per change. Neither gameplay materials nor profile assets are mutated during Play. There are no per-frame texture/mesh/material rebuilds in the new controller/rigs. The optional upstream High reflection manager renders every frame and has its own CPU/allocation costs; this is not a production mobile performance result.

## Quality and URP isolation

`PoolWaterQualityProfile` assets under **Assets/WaterVisuals/Settings** reference persistent material variants, dedicated pipeline assets and reflection settings. Dedicated pipelines share a copied Forward renderer. The test controller temporarily uses these pipelines and restores the prior quality-slot pipeline on disable/unload. The original PC/Mobile pipeline assets and project Quality settings are untouched. The camera explicitly requests depth and, when refraction is on, an opaque texture.

| Profile | Scale / MSAA | Opaque copy | Caustics | Reflections |
| --- | --- | --- | --- | --- |
| Low | 0.85 / 2× | Off; refraction off | Off | Baked 128px pool hall cubemap; no extra camera |
| Balanced | 1.0 / 2× | 2× bilinear downsample | On | Baked cubemap; no extra camera |
| High, visual reference | 1.0 / 4× | Full resolution | On | 0.4-scale planar render plus fallback cubemap |

Depth remains enabled at every level. Balanced intentionally looks softer because the opaque image is downsampled. Inspector fields permit other reflection scales/material variants/pipeline budgets. Mobile candidates require real Android GPU/frame-time, bandwidth, thermal and memory measurements, including fourteen skinned athletes and match effects. No Android/iOS shader build or hardware profiling was performed. This table's render scale/MSAA/pipeline values apply only to the test scene; the match retains its original pipeline budget.

## Applied match presentation

After fixture verification, **Tools > Water Polo > Water Visuals > Apply Verified Water to PrototypePool** was run and the scene saved. Repeat calls preserve the existing presentation and tuning. It adds `Environment/Verified Pool Water Presentation`: actual tiled floor/walls at 2.05m depth, transparent surface at the existing PoolBounds waterline, floor copies of the existing cross-pool marks and a baked reflection of the actual match environment. No new physics colliders, test characters, match/ball authority, UI or camera.

Five original renderers (WaterSurface, PoolFloor and three shallow lines) remain in the scene, disabled. The only other change to original scene objects is Environment's new child reference. A normalized YAML block comparison confirmed original scripts/athlete transforms/rigidbodies/colliders are unchanged. For manual visual rollback, disable the new presentation root and enable those five original renderers.

`PoolWaterPreferenceBridge` observes the existing GamePreferences event. Match Low/Medium/High selects the corresponding water material without rebuilding it on unrelated preference changes. Match controller uses `useIsolatedTestPipeline=false` and keyboard shortcuts off. There is **no planar reflection manager or second camera** in gameplay; High uses the baked match cubemap. The same GameplayCamera receives per-camera depth/opaque overrides, which were verified on the original Mobile_RPAsset even though its authored global texture flags remain off. Original pipeline assets, render scale/MSAA and project quality settings remain intact.

## Focused verification and evidence

`Tools/Verification/WaterVisualVerification.cs` runs through Pipeline **script run**, which safely awaits Task-returning entries. Do not return these Tasks through synchronous eval.

```powershell
# WaterVisualTest open in saved Edit Mode:
unity command script run --file Tools/Verification/WaterVisualVerification.cs --entry WaterVisualVerification.Setup --caller plugin --skill unity-pipeline --format json
# Use Play WaterVisualTest; then:
unity command script run --file Tools/Verification/WaterVisualVerification.cs --entry WaterVisualVerification.Rendering --caller plugin --skill unity-pipeline --format json --timeout 55
unity command script run --file Tools/Verification/WaterVisualVerification.cs --entry WaterVisualVerification.CaptureFinal --caller plugin --skill unity-pipeline --format json
```

Fresh results:

- **Setup PASS:** repeat-safe 112-transform scene, retained scene GUID/profile tuning, three test swimmers, zero colliders, assigned baked reflection and byte-identical PrototypePool.
- **Rendering PASS:** frame 388 → 950; real Input System 1/2/3 and V events; all three rendered shader variants supported with no shader compile errors. Actual Game view captures, not Scene view substitutes.
- **Submersion PASS:** disabled only meshes whose bounds lie entirely below the surface, with their shadow casting disabled to avoid false positives. Upright/surface/fully submerged rigs contributed **3,056 / 2,091 / 1,793** distinct visible pixels respectively (6 / 5 / 8 wholly submerged meshes). This verifies the opaque geometry contributes to the rendered underwater image.
- **Effect A/B PASS:** water on/off changed 330,568 pixels; refraction distortion on/off 46,213; caustics 331,136; reflections 166,788. With character poses frozen, animated water changed 38,080 pixels over the sample interval. These are visibility checks, not image-quality/performance scores.
- **GameplaySetup PASS:** repeat-safe 301-transform final match, fourteen athletes and no added physics/planar manager; repeated setup leaves saved scene bytes unchanged.
- **GameplayPresentation PASS:** frames 29,329 → 29,818, actual match Low/Medium/High through the existing preferences API, original Mobile_RPAsset/one camera throughout, real injected W movement, original ball collection and airborne physical shot. User preference record restored. These are focused smoke checks, not the historical gameplay regression suites.
- Final Console ground truth: **0 errors, 0 warnings, compilationFailed false**. The Pipeline historical log buffer also retains earlier resolved import errors; use ground truth/new entries to distinguish them.

PNG evidence and machine reports are in ignored **Logs/WaterVisuals/**: `Final-0/1/2.png`, `Quality-0/1/2.png`, `Transparency-*`, effect A/B captures, `Rendering-result.json`, `GameplaySetup-result.json`, `GameplayPresentation-result.json`, `Match-Quality-0/1/2.png` and `Match-Final.png`. High fixture detail/reflection-angle, Low/Balanced, dry baseline and final High match images were visually inspected. Current Game view size was approximately 1031×518; no mobile-layout/device claim is implied. No UI was added, so no new pointer controls require verification.

For the final PrototypePool, run `WaterVisualVerification.GameplaySetup` via script run in Edit Mode. Enter direct match Play Mode and run `WaterVisualVerification.GameplayPresentation` through script run (timeout 55). The fixture's `Setup` and `Rendering` methods still target WaterVisualTest, not the match.

## Assets, license and installation

Upstream [installation guide](https://github.com/MatrixRex/Uber-Stylized-Water/blob/1fdd16c6312611fef7fb248dee86cdd3fd456a1e/docs/usage-guide/getting-started.md) supports Unity 6000.0.30+ / URP / Shader Graph 17.0.3+. It prescribes copying its shader folder or importing a release, enabling depth and opaque textures, and preserving your own URP setup. The pinned project meets those requirements. This integration imports the prebuilt shader only; it does not edit/import the source graph or raise global Shader Graph variant limits.

Download/import succeeded through approved network access after the sandbox's initial connection failure. Exact revision, archive SHA256, imported subset, omitted dependencies and local Unity/URP compatibility changes are in **Assets/Shaders/Uber Stylized Water/UPSTREAM.md**. Original .meta GUIDs were retained. Main MIT license (MatrixRex, 2025), dependency MIT license (Cyanilux, 2020), upstream credits and player-distributed notices are preserved. The optional Caustic 2/3 maps are excluded; one upstream caustic source uses CC-BY-SA rather than MIT/CC0. No unrelated demo art/dependencies are installed.

For a future manual reinstall, download the [exact source ZIP](https://codeload.github.com/MatrixRex/Uber-Stylized-Water/zip/1fdd16c6312611fef7fb248dee86cdd3fd456a1e), extract outside Assets, and follow the subset in UPSTREAM.md. Preserve its `.meta` files and both license texts, then reapply the documented two-script Unity 6.6/URP 17.6 fixes. Do not import its Packages or ProjectSettings folders.

## Limits and future Blender integration

This is clear pool water using screen-space raster effects, not a fluid simulation or volumetric underwater renderer. Caustics/refraction approximate the viewed opaque geometry; off-screen data, screen borders and overlapping transparent objects can show limitations. Camera-underwater rendering, water volumes, meniscus/facial detail and physical light transport are not implemented. The fixed surface has normal ripples rather than displaced ocean geometry. The imported reflection implementation is effectively one current-main-camera/one water-height system; split-screen, stacked cameras, XR, multiple water heights and seamless additive scene overlap are unverified.

Future Blender swimmers should be complete closed body meshes in metres with actual underwater arms/legs, sensible normals/tangents, rig bounds and LODs. Import as opaque URP Lit materials (skin/suit) so both MeshRenderer and SkinnedMeshRenderer enter the opaque/depth pass before water queue 3000. Keep normal ZWrite/depth participation; avoid making the entire athlete transparent or placing it in a camera-excluded layer. Set skinned bounds to include all animated submerged limbs. Alpha-clipped hair needs depth-pass testing; transparent hair/eyewear/particles are not included in the opaque refraction capture and need a separate reviewed solution. Preserve above-water caps/faces with restrained refraction.

Replace only the **visual child rig** in this fixture first, keep gameplay root identity/collider/motor/release poses separate, and retest animated limbs crossing the surface from the broadcast distance. The existing PrototypePool athletes have placeholder geometry; a water shader cannot add missing anatomy. The new floor/walls in gameplay are graphics only, with no colliders. Do not introduce a second swimming/ball authority or change waterline/physics. Review full rig/match effects and device cost before production use. No Blender session/import or model generator was required or used here.
