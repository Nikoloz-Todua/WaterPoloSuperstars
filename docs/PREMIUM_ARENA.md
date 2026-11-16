# Championship Night — premium arena presentation

> The subsequent [poolside redevelopment](ARENA_REDEVELOPMENT.md) is now applied: end-corner white chairs, layered boards, pool-edge detail, wider referee colors and a ceiling-centred existing jumbotron. It preserves the field, cameras, crowd, water and lighting budgets. The historical padded sideline benches/end-hung display described below have their replaced renderers retired in the current scene.

> M19 adds the west-wall live board, two gameplay views and bounded light-gallery sightlines. See [MATCH_BROADCAST_AND_CAMERAS.md](MATCH_BROADCAST_AND_CAMERAS.md) for current assets, images and checks. Existing venue lighting and clear water remain.

> M18 refinement is documented in [SPRINT_POLISH.md](SPRINT_POLISH.md); the sections below retain the original milestone architecture and evidence. Use the latest plan.md handoff for current authorization.


The production scene is still **Assets/Scenes/OlympicMatchArena.unity**, with the same GUID and game entry. This pass replaces the previous bright venue finish with a darker indoor championship presentation. The existing pool, dimensions, clear water, 14 rigged swimmers, spectators, referees, controls, HUD and hub/gameplay authorities remain.

## Art direction

1. **Style:** clean international water polo event architecture, chamfered seating/equipment, layered metallic/navy surfaces and restrained original championship graphics.
2. **Palette:** midnight `#0B1830`, cobalt `#123B75`, graphite `#101A27`, lane slate `#43566C`, brushed aluminum `#7890A2`, competition white `#DDECF3`; ice blue `#36AAD0` is the accent. Existing athlete/team/ball/field-marker colors are preserved.
3. **Light story:** an indoor event at night, with a bright competition pool and readable technical zones framed by a darker spectator bowl. No daylight panels illuminating the whole hall.

Palette values live in the scoped `PremiumArenaSetup.Materials` authoring method. This is not a global recolor of athletes, audience or HUD. It is a substantial procedural venue treatment, not a claim of photoreal AAA art.

## What changed

- **Deck:** honed basalt-style ceramic, fine rubber event aprons, new white/metal pool trim, drainage-grate detail, cyan separation lines, directional competition branding and protected 1.75 m official walkways centered on the unchanged referee paths. Duplicate older access lettering is retired; existing field distances gain lighter text against the darker deck. Pool floats, sport markings and actual bounds retain their positions.
- **Teams:** seven padded reserve seats per zone, chamfered backs/pans, armrests, team stripes, technical backwalls/header strips, coach consoles, task fixtures, cooler/flight-case/towel/bottle clusters and marked access. The two team zones remain in their established east-side positions. Fine equipment geometry follows the existing Medium/High prop budget.
- **Officials:** a beveled timing desk, worktop, three terminal placeholders, chairs and recessed light detail, with equipment behind the protected referee lane. Sideline digital barriers have breaks between segments and remain outside referee travel.
- **Stands:** ten reusable six-tier blocks with chamfered seat upholstery, armrests, metal rails, tier reveals and stair nosings. Original spectator mesh/material data, placement and density groups are unchanged. The new seating uses the same row/seat coordinates beneath them.
- **Hall:** dark acoustic glazing/panels instead of luminous daylight treatment, structural piers/fins, upper-bowl ribbons, section plaques, large end portals, hanging championship banners, original three-ribbon water motifs and perimeter broadcast trusses. The existing hall shell/roof is reused.
- **Displays:** a larger chamfered four-face end-hung jumbotron, two deck timing screens and six sideline branding barriers. Six live score/clock faces use the existing `ArenaEventDisplay` observer; no new score authority, video feed or extra render texture. Static barriers are labeled event graphics, not simulated sponsor feeds.
- **Water presentation:** the current shader/materials/profiles, underwater visibility and interactive effects remain unchanged. The new light balance, grading and a new 128 px HDR reflection bake make the water belong to the darker venue. Water is temporarily hidden during the reflection bake to avoid capturing its own previous reflection.

No additional physical collider was introduced. All 14 athlete visual/controller hierarchies remain intact. The new venue is outside the gameplay component roots.

## Scene and assets

Existing `Olympic Arena — WaterVisualTest Foundation` contains the retained pool, deck base, markers, shell, audience groups, water, reflection components and developer viewpoints, plus **Championship Night Venue**:

- Competition Deck and Clear Official Lanes
- Blue and White Team Technical Areas
- Match Operations and Protected Sidelines
- Grandstand Portals and Championship Architecture
- Perimeter Broadcast Rigging and Pool Light Banks
- Championship Scoreboards and Presentation Screens
- Fine Operations Props
- Technical Area Washlights

`Assets/Arena/ChampionshipNight/` holds the venue/seating prefabs, combined meshes, shared materials, three small authored finish textures, reflection cubemap, three URP pipeline copies and three Volume profiles. New world-space text remains separate; geometry is combined by material and zone. The main venue has **120 renderer components / about 34,852 referenced vertices**, including world-space text. The ten seating instances add **40 renderers / 144,960 referenced vertices**, using a shared four-mesh block. These are content counts, not measured draw calls, frame times or GPU memory.

Earlier scene instances of `Championship Venue Finish`, old team benches/equipment/officials table and old seat blocks are removed where replaced. Original source prefabs/assets remain on disk for recovery, but the retired venue/bench prefabs are no longer dependencies of the production scene. There is one competition pool and one water surface; no duplicate active day/night venues.

**No external art assets or packages were imported in this pass.** New geometry, textures and event graphics are original project work. Existing Unity/TMP, MIT water and CC0 athlete notices are preserved; no protected Olympic/event/team logos were reproduced.

## Lighting, post-processing and budgets

The existing directional light becomes a cool fill/key at intensity **0.78**, elevation 66°, shadow strength 0.76. Gradient ambient colors are `#344767 / #1B2D49 / #0B1628`. The bright skybox is removed from this scene; camera clearing uses deep navy. Reflection intensity is 0.72. Original camera movement, lens/FOV, framing and controls are unchanged; only its rendering flags/background change.

Four focused pool spotlights sit on perimeter rigs outside the playing field. Two short-range technical-area washes keep benches readable. All six additional lights have **no real-time shadows**; existing main-light shadow presets remain. Emissive housings/inlays provide visible accents and baked reflection detail; they are not additional simulated lights.

| Preset | Focused pool lights | Bench washes | Bloom | Water/crowd/props |
| --- | ---: | ---: | --- | --- |
| Low | 2 | 0 | Off | Existing Low behavior retained |
| Medium | 4 | 2 | 0.12, quarter-resolution Dual | Existing Medium behavior retained |
| High | 4 | 2 | 0.22, quarter-resolution Dual | Existing High behavior retained, including planar reflection |

The directional light is additional to these counts: **3 total active lights on Low, 7 on Medium/High**. URP per-object additional-light limit is four. No volumetric fog, SSAO, depth of field, grain or motion blur was added.

`ChampionshipPresentation` observes existing `GamePreferences.Changed` and selects a scene-local Volume profile/light budget. `ArenaQualityController` still owns pipeline/water/crowd/prop switching; its references point to the new pipeline copies and fine-prop root. No generic preference/economy/controller script was rewritten.

One global Volume uses **Neutral tonemapping**, exposure +0.05, contrast +12, saturation −6 and vignette 0.13. HDR is enabled. Bloom threshold is 1.1, scatter 0.5, Dual filtering, quarter resolution, max four iterations, high-quality filtering off. The generic older preference code disables all post on Medium/Low; this venue's observer explicitly keeps the cheap grade enabled on every preset and disables Bloom on Low through the profile. This exception is local to this arena; other scenes/preferences keep their existing behavior. Overlaid HUD rendering remains separate and readable.

## Safe commands and recovery

- **Tools > Water Polo > Arena > Open Olympic Match Arena** — open the saved production scene.
- **Tools > Water Polo > Arena > Play Olympic Match Arena Directly** — isolate gameplay; **F8** cycles the existing five inspection views.
- **Tools > Water Polo > Arena > Apply Championship Night Overhaul** — first-time installation on saved Edit Mode scenes; repeat calls open/preserve the authored venue without rebuilding it.
- **Tools > Water Polo > Arena > Refresh Championship Night Reflection** — rebake after intentional environment/lighting edits; does not reset materials or geometry.
- Older **Polish Existing Championship Venue** is guarded when the new venue is present, and **Refresh Venue Reflection** delegates to the new reflection command. It cannot add an overlapping old venue or overwrite the old reflection asset by accident.
- **Tools > Water Polo > Play Hub Entry** / normal Play with Boot entry — unchanged frontend flow. **Use Boot as Editor Play Entry** restores Boot after direct testing. Boot is restored at delivery.

Exact recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreChampionshipNight.unity**, outside builds. Native scene copy and baseline hashes replace any Git checkpoint; Git writes were prohibited. Original WaterVisualTest, prior recoveries and athlete fallback prefabs remain. The older **Apply Configured Field Dimensions** tool explicitly rebuilds the base environment; it is not a review command. If deliberately used, reinstall/reverify the presentation afterward. No automatic restoration/deletion of unrelated files is performed.

## Actual verification

Windows Unity **6000.6.7f1 / URP 17.6**, focused Pipeline async `script run` checks in `Tools/Verification/PremiumArenaVerification.cs`. Evidence lives in ignored **Logs/PremiumArena/**.

| Check | Fresh result |
| --- | --- |
| Structure | PASS exact recovery hash and same production GUID; **1,854 protected records** compare unchanged: original gameplay/HUD/referee scripts, physics, rig hierarchy/renderers, audience data and camera movement/lens tuning. Rendering/visual-only references intentionally differ. |
| Asset safety | PASS Boot, MainLobby, build settings, match destination, hub catalog, swimmer model/clips/materials/controller and water-material hashes unchanged. No missing scripts/materials or new venue colliders. Old venue/bench prefab dependencies retired. |
| Setup safety | PASS both new and older setup commands preserve the saved authored scene on repeat; saved scene reopens correctly. |
| Gameplay | PASS actual W/Shift movement/sprint, pointer Pause/Escape, pause clock, physical pickup/pass/reception/selection, real shot/goal and real score on all six display faces; AI bounds, 14 rigged athletes, referee positions and original HUD retained. Keeper components were temporarily disabled only for the controlled scoring fixture and restored. |
| Camera/paths | PASS sampled triangle sightline checks for **35 visible athlete/goal targets at three action positions** against new venue/seating geometry; no intersections. Both referee lanes clear of new props. This is not exhaustive proof for every camera trajectory. |
| Rendering | PASS HDR, camera post flag/mask, actual resolved Volume stack, three profiles/light budgets, supported original water variants and grade on/off captures. |
| Entry/return | PASS real pointer PLAY → modes → Training → Launch → upgraded arena → Pause/Exit → original lobby. In-memory verification profile; **user save hash unchanged**. No full-time reward/official-division suite repeated. |
| Game views | PASS inspected actual 1920×1080 before/after broadcast/overview/bench/reverse/hall frames, detail and waterline views. Additional actual **2400×1080 (20:9)** capture has existing visible HUD buttons in bounds. One explicitly named clean inspection capture temporarily hides the Canvas, then restores it; playable comparison captures retain the HUD. |
| Console | Fresh compilation passed; final current Unity Console ground truth **0 errors / 0 warnings**. Historical Pipeline buffers contain earlier resolved entries and do not equal current Console counts. |

Reports: `Structure-PASS`, `Gameplay-PASS`, `Sightlines-PASS`, `Presets-PASS`, `HubEntry-PASS`, `FinalDetails-PASS`, `Grade-AB`, `Console-Final`. Frozen 1920×1080 grade on/off captures have mean RGB byte differences **5.84 / 7.47 / 8.07**; grading is measurably active. Images: `Before/After-Broadcast`, `Before/After-Overview`, `Before/After-Benches`, `Before/After-Reverse`, `Before/After-Hall`, `Final-White-Team-Area`, `Final-Officials-and-Deck`, `Final-Jumbotron-Detail`, `Final-Clear-Water`, `Final-Wide-2400`, `Verified-Scoring`, `Preset-0/1/2`, `Grade-On/Off`, `Hub-Launched-Arena`, `Returned-Lobby`, `Final-Venue-Inspection-NoHUD`.

Scoped visual review, subjective /5 scores (unchanged systems assessed for fit rather than redesigned):

- Palette discipline: **4/5**, before 3 — navy/metal environment and restrained ice accents, retained sports/team identity.
- Style uniformity: **3/5**, before 3 — stronger coherent procedural venue; existing mannequin athletes and simple crowd remain intentionally.
- Lighting depth: **4/5**, before 2 — distinct bright pool, readable technical zones and darker bowl, controlled highlights.
- Silhouette: **3/5**, unchanged — current swimmers/ball/goals remain readable from broadcast; no athlete replacement.
- UI intentionality: **4/5**, unchanged — original mobile HUD preserved and exercised.
- Feedback visibility: **4/5**, before 3 — authoritative score visible on six larger venue faces, existing ball/ring/water feedback retained.

The first captures prompted a second art pass for bench illumination/upholstery, controlled LED output and the end-wall water signature. Implementation mechanics were checked against local URP 17.6 source and Unity's [Volumes](https://docs.unity.com/en-us/engine/6000.6/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes) and [Bloom](https://docs.unity.com/en-us/engine/6000.6/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom) documentation. Craft references used: `art-direction.md`, `scene-lighting.md`, `visual-scorecard.md`; Unity CLI/Pipeline and URP post-processing skills supplied the native edit/verification workflow. User scope takes precedence over generic warm-light/global-palette/commit recipes.

## Limits and manual review

This remains a procedural arena with simple underlying shell/crowd art, static banners, blank terminal faces and text-based displays. Cushions, cases and fixtures are improved placeholders, not scanned/production architectural props. Actual substitute characters, video content and volumetric beams are not implemented. The current swimmer animation/model limitations remain those in SWIMMER_ANIMATION.md.

Android/iOS device performance, real touch/notches, complete matches/reward return and prolonged thermal behavior were not tested in this pass. Six shadowless local lights, HDR/bloom, the new seating geometry and existing High planar reflection need device profiling; content budgets do not establish mobile frame rate. A few sampled sightlines do not cover every zoom/follow state.

Manual review: launch normally from MainLobby; swim/sprint/switch/pass/lob/charge/shoot; inspect both ends, goal/ring/ball visibility, pause/settings and Low/Medium/High; use F8 for architecture/bench views; finish a full match through results/rewards/lobby. Recommended next authorized milestone: Android GPU/CPU/thermal profiling, then selective authored architectural/prop meshes and texture polish based on measured budgets. Stop here; no next milestone was started.
