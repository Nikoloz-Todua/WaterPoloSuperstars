# Water Polo Superstars — Project Plan and Handoff

Last updated: 2026-11-17.

## Current handoff — Milestone 20 championship arena reconstruction (in progress)

2026-11-17: The developer explicitly authorized a substantial environmental rebuild after the poolside pass, including a 50-metre-class architectural basin, three-tier spectator bowl, operational team access, athlete entrance/staging and replaceable animated LED ribbons. This supersedes previous stop statements only for M20. Human-only Git writes remain permanent; preserve all existing local M19/poolside/unrelated work and old-plan.md.

- **Reference gap:** `docs/References/ArenaRebuild/` and the four named images/README_REFERENCES.md are absent. Project and common local-folder searches found no copies; an optional path question is pending. The supplied full brief is the design direction. Current World Aquatics page still lists the 18 February 2026 regulations; the existing exact PDF/text was consulted for field equipment. The pool supplier's published Paris installation description confirms separate 50 m competition/warm-up basins; its image download was unavailable. Do not claim the missing four photographs were inspected.
- Native **Assets/Scenes/Recovery/OlympicMatchArena_PreMilestone20.unity** was copied before geometry changes and verified exact; outside builds. Production scene retains original GUID. Fresh baseline/evidence: **Logs/ArenaRebuild/**. No obsolete setup, new package or Git mutation.
- M20 source/geometry under **Assets/Arena/ChampionshipRebuild/**, authoring **ChampionshipArenaRebuild** plus scoped mesh/bowl/facilities/technology/review helpers. **Tools > Water Polo > Arena > Build Milestone 20 Championship Venue** is applied, repeat-safe and preserves authored tuning. Existing environmental renderers are retired; original objects/assets remain for recovery. Gameplay/athlete/HUD roots are retained.
- New **ChampionshipVenueLayout** separates the **50 × 25 m, 2.05 m-deep** physical basin from the unchanged **25 × 20 m** field and 25.6 × 20 m competition-rope envelope. Existing CompetitionDimensions asset/PoolBounds remain unchanged; legacy basin-buffer fields are no longer used by the M20 geometry generator. One existing water renderer receives one continuous new mesh; existing shader/material/profile/effects/reflection authority is preserved. Bulkheads at Z±14.2 divide the extra end water from the match area; entry notches meet the old re-entry corners without moving gameplay coordinates.
- Three structural seating levels, reused section prefabs/shared meshes, inward seat shells, aisle stairs, supported balcony rings, corner infill, retained original crowd mesh/material remapping plus matching lightweight crowd batches. Low gets coarse upper seats and reduced population/detail; Medium/High use fuller existing preference budgets.
- Two east-side team facilities, nine reserve plus two coach chairs each, actual advertising gates, ramps and stable entry/route/seat anchors. VenueTeamFacility reads current defended goals only for the correct Blue/White labels after halftime; it does not implement substitution rules. South entrance has two open backstage lanes, fourteen future lineup marks, an original generic crest screen with Sprite/Texture input and future route/camera anchors. No athletes are moved into a pre-match cinematic.
- Long animated inter-tier ribbons use one replaceable shared shader/material and bounded 10 Hz property-block updates; Low is static. New media platforms, officials desk, conduits, roof trusses/catwalks/luminaire meshes integrate the existing live primary board/jumbotron/six secondary faces and existing real light budgets. No new gameplay collider or real Camera.
- GameplayCamera adds an optional preparation-only overview transform/FOV and 0.85 s release blend. Live Lengthwise/Sideline profiles, F6/settings/persistence, targeting and input mapping remain. F8 keeps five poses with updated architectural inspection locations. This is a camera presentation adjustment, not a new match phase.
- First actual renders show the longer facility/three tiers. A visual fix pass adds balcony connections/corner seating and corrects a solid bulkhead beneath the entry ramp; the inspection overview is lowered below roof beams. Fresh native structural check passed **2,858 protected records /23,646 existing references**, original field/rigs/rules/HUD/live profiles and saved reload, permitting only the new preparation fields. Architecture clearance/orientation and gameplay acceptance are now being completed; do not call the milestone verified until the final handoff replaces this in-progress note.

## Previous handoff — championship poolside arena redevelopment

2026-11-17: The developer explicitly authorized the supplied **Arena Redesign Execution Instructions** and full pasted arena/UI specification after M19. This authorizes the scoped environment work below and supersedes older milestone stop statements; it does not authorize a new gameplay or UI rewrite. Human-only Git writes remain permanent. No local `waterpolo-arena-spec.md` was found; the complete supplied text is the design reference, with the user's overriding safety/marking instructions and official World Aquatics rules taking precedence.

**Implemented, saved and focused Windows Editor acceptance completed. Stop after this arena pass.** Native recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreArenaRedevelopment.unity**, byte-identical to the pre-pass scene and outside builds. The field was already **25 × 20 m**, with end ropes 0.30 m behind each goal line (**25.6 × 20 m envelope**), inside a **28.6 × 22.5 m, 2.05 m-deep basin**. No field, basin, goal, formation, sprint, camera-profile, HUD or rule dimensions were changed. Full implementation, corrected specification assumptions, evidence and manual checklist: **docs/ARENA_REDEVELOPMENT.md**. Exact changed paths including metas: **docs/ARENA_REDEVELOPMENT_FILES.txt**.

- Applied **Tools > Water Polo > Arena > Apply Championship Poolside Redevelopment**. Native shared mesh/material/prefab assets: **Assets/Arena/PoolsideRedevelopment/**. Repeats preserve authored tuning. New combined visual geometry adds no gameplay collider, real Camera or realtime Light.
- Retained both 1 m-wide, 0.70 m-high referee platforms/paths; widened six local colored surface meshes to 0.96 m, added rubber grips/support/access detail, sloped event skirt, coping, drainage and corner fittings. Original red 2 m/red 5 m/yellow 6 m/white goal/halfway coordinates and green middle rope sections remain. No cross-field 2/5/6 m rope was added; existing correct red end-rope exceptions/re-entry zones remain.
- Replaced rendered sideline team blocks with two seven-chair white armless end-corner benches on the side opposite the officials table. Added raised risers, separate lower camera/equipment decks, cases/coolers/bottles, continuous low original event boards, higher bands/LED-style ribbons, passive side broadcast platforms with access stairs, and grandstand tier-access/soffit detailing. Existing crowd/seating and officials table remain; old replaced objects/assets are retained with renderers retired.
- Reused the existing four-face jumbotron and all live TMP references, moved its root to **(0,25,0)** under the ceiling with a new housing, suspension and spreader truss. Two existing low timing boards move beside the end-corner areas. The west-wall primary live scoreboard and one ArenaEventDisplay observer remain. No duplicate score authority, render camera or RenderTexture was added. Original wide opening, Lengthwise/Sideline rigs and F6/F8 remain.
- Eight local marine/navy/polymer/metal/rubber/LED/graphite-net materials, existing texture/font reuse, new 128 px reflection bake, existing bench/display lights retargeted; no added light count. Existing Low/Medium/High budgets remain **3/9/11** active lights. Fine equipment follows existing Medium/High prop visibility; essential chairs/boards/platforms remain on Low. Existing water shader/assets unchanged.
- A held-ball Stop Play probe exposed an existing scene-teardown dependency on a destroyed PoolBounds. **BallController.Drop** now returns when the field has already been destroyed, avoiding an invalid physical drop during teardown. Normal movement, possession, flight and scoring code is unchanged.
- Fresh native structural comparison passed: original scene GUID, **2,858 protected component records and 23,646 object references**, 14 rigs, all protected files including water/animation/settings/hub, no missing scripts, repeat-safe setup and saved reload. Fresh keyboard/gamepad/touch, paused camera settings, pass/charged shot/goal/steal/lob/keeper/shot-clock, both-camera environmental sightlines and referee movement checks passed. Actual views at 1920/2340/2400 × 1080 and all three quality presets were captured/inspected.
- Fresh four-period test passed with physical acquisition every quarter, exact original opening, selection lock/restoration, one halftime end switch, FINAL and restart preference. Sprint competition passed human/AI wins, capped Shift and real pointer Sprint, one whistle/drop/water/pickup, stamina and immediate losing-side play. Both physical post/crossbar contacts and outside-opening score rejection passed. Actual ordinary foul/two exclusions/re-entry, Blue/White goals and HUD/primary/six secondary display synchronization passed. New geometry leaves **20 sampled actor/ball triangle rays clear across 357 venue meshes**; actual referee north/south travel and full lane are clear.
- Fresh production Boot/menu/pointer **Lobby → Match → Result → Lobby** passed with an isolated memory verification profile: restart cancel/confirm, abandon with no reward/stray device, four real sprint acquisitions with accelerated period endings, one official victory receipt, duplicate reward rejection and serialized profile reload. **Real profile SHA-256 unchanged.** Held-ball Stop Play completed with zero fresh errors/warnings after the narrow lifetime fix.
- Evidence is scoped to **Logs/ArenaRedevelopment/**, including eight before/after camera pairs, goal areas, passive camera platform, live AI, three landscape ratios and quality presets. Original sideline/five inspection transforms/FOV match exactly; the held-ball lengthwise fixture differs by ~1 cm, while the original opening is checked exactly. Some captures park AI/freeze fixture time; live AI captures and real physical tests are separate. Historical M17/M19 logs were not overwritten.
- Content: **69 combined new geometry meshes / 56,992 vertices / 28,496 triangles**, seven geometry materials plus the graphite goal-cord replacement, 38 separate TMP labels, six local widened platform meshes, three visual prefabs and one 128 px cubemap. Native mesh asset count is 75. These are content counts, not measured draw calls or phone performance. All new equipment remains visual-only; no new runtime environment controller.
- Final current Console **0 errors / 0 warnings**, compilationFailed false; named controls/display/period/hub and teardown captures report zero fresh runtime errors. No manual Console clear. Retained Pipeline history includes prior warnings/resolved probes; pre-existing PremiumArenaSetup.cs:123 CS0184 can recur on full compilation. Initial capture-directory and overly broad sightline-filter fixtures were corrected and rerun; only final passes count. Existing QualitySettings AA0 difference predates this pass and remains byte-identical to the baseline, including after reflection bake/restoration.
- Delivery: saved arena, Play stopped, Boot entry, original Game-view/input focus/preference bytes restored, real profile/old-plan.md unchanged, auto-tick override removed. All changes remain unstaged/uncommitted. Existing M19 local files and unrelated SourceAssets/references/package settings/old-plan.md/tatus remain. No Git writes.
- Deferred/limits: full mobile-control/HUD simplification is a documented proposal only; every current action remains. No full-duration match, device/build/profiler/notch/thermal test, exhaustive penalty or exhausted-roster test, new staff models, animated LED video or replay playback. Empty staff chairs and simple procedural camera equipment are placeholders. High roof jumbotron is intentionally outside many gameplay frames. Next work needs separate authorization, prioritizing device performance/framing and any later reviewed UI mapping.

## Previous handoff — Milestone 19 live arena board and two gameplay cameras

**2026-11-16/17: M19 implemented in the existing saved OlympicMatchArena and focused Windows Editor acceptance completed. Stop after M19.** Human-only Git writes remain permanent; nothing was staged, committed or pushed. Original scene GUID `5c4d3741df6b7f54e8cfd34d391d78d1`, 14 rigs, water, field/goals, sprint authority, AI, rules, HUD, hub/economy and saved profile remain. Full architecture, camera tuning, exact screenshots, acceptance and manual checklist: **docs/MATCH_BROADCAST_AND_CAMERAS.md**. Exact M19 paths including assets/metas: **docs/MILESTONE_19_FILES.txt**.

### Saved scene and display

- Native Unity recovery **Assets/Scenes/Recovery/OlympicMatchArena_PreMatchBroadcast.unity** was copied before structural edits and verified byte-identical to the pre-M19 scene. It is outside builds and shares scripts. Scene edits/assets were made through Unity APIs, not YAML replacement. No obsolete setup ran.
- Already applied repeat-safe command: **Tools > Water Polo > Arena > Set Up Live Scoreboard and Broadcast Cameras**, saved OlympicMatchArena outside Play Mode. Repeats preserve authored tuning. Launch normal flow with **Tools > Water Polo > Play Hub Entry**; direct inspection with **Tools > Water Polo > Play Olympic Match Arena**.
- New **Primary Championship Scoreboard** on the west spectator wall at **(-23.15,11.8,0)** faces +X, above seating; 15.8×7.6×0.48 m navy/metal housing, four combined geometry meshes, live front-facing 3D TMP text, cyan traces and reused amber exclusion dots. No display Camera, RenderTexture, Canvas, Light or collider. Visual prefab/native materials/meshes are under **Assets/Arena/MatchBroadcast/**.
- The existing **one ArenaEventDisplay** supplies the primary and all **six existing jumbotron/end-deck score/time faces**. It observes PrototypeMatch events plus cached per-frame actual clocks to match HUD seconds, with no independent score/countdown. Labels rebuild only when displayed values change. Actual BLUE/WHITE score, Q/time/shot, pause/stoppage/sprint/break/final and restrained pause-aware team GOAL emphasis.
- Fourteen actual roster cap bindings (1,2,3,4,5,7,13 each side). Temporary amber indicator/countdown reads **ExclusionRemaining**; real re-entry clears it. Ordinary fouls never light it. Permanent third-personal removal reads **OUT**, without a temporary dot. Restart resets from authoritative rules. No new player database/statistics/reward event.

### Cameras, controls and visual safety

- No Cinemachine installation. **GameplayCamera still controls one gameplay output Camera**; existing High water reflection rendering is unchanged. Lengthwise tuning/lens serialized values preserved. Added configurable Sideline Broadcast: east side, goals horizontally left/right, 22 m distance /17 m height /54° FOV /0.48 s damping /0.65 ball weighting /bounded adaptive height and focus. It tracks actual actor/ball/current goal, with projection checks and cached arrays.
- **F6** toggles; existing **Pause → Settings → Camera View** chooses Lengthwise or Sideline Broadcast. Same lightweight GamePreferences key, additive cameraView field, invalid value → Lengthwise, saved/reloaded. Existing settings rows were compacted to avoid opacity/camera overlap. F8's five inspection poses/return remain.
- 0.6 s unscaled blend works during pause without changing clocks, possession, players or sprint. Original SprintSetup overview remains exact: (0,28.926,-21.6945), rotation(53,0,0), FOV52. Selected gameplay style takes over after whistle. Four starts, one Q3 side reversal, original goal center restarts and selection lock remain.
- SwimmerInput maps human keyboard/gamepad/joystick once through actual projected camera axes before the existing motor. Lengthwise right/up = +X/+Z; Sideline = +Z/−X. AI stays world-space. World-space sprint auto-assist is applied afterward; normal passes/shots/stamina/physics are unchanged.
- New **BroadcastSightlines** cuts away only the two decorative M18 hanging light-gallery groups when their cached bounds block visible actor/ball/primary panel or contain the camera. 0.2 s hold, renderer flags restored on disable. Fixed sideline camera bounds avoid normal wall/stand traversal. This is not a universal occlusion solver; unusual manual poses/future geometry still need review.
- North deck wordmarks corrected from their 180° in-plane rotation. Digital text uses local back-face culling/depth testing, positive scale and retained front-facing mounts. Opposite spectator-side deck text naturally has a different viewing orientation; no field-axis/goal rotation.
- Lighting changes limited to the board's materials/emissive traces. Existing championship lighting/Low-Medium-High budgets, clear water shader/materials/profiles/reflections and venue architecture remain. No extra realtime light or broad lighting redesign.

### Replay boundary, verification and limitations

- Implemented read-only camera snapshot plus live/opening/inspection presentation reporting and reserved future celebration/playback/orbit/return enum values. **No working replay, recording buffer, replay UI or orbit playback.** Guide specifies a bounded future in-memory pose/animation window, presentation-only playback and exclusive Camera ownership. It does not promise deterministic physics reproduction.
- Fresh Structure: **140 protected records unchanged**, exact recovery/GUID/original lens/legacy tuning, 14 roster bindings, one observer/Canvas/EventSystem, no missing scripts, repeat and saved reload. Controls: actual queued keyboard/gamepad/touch in both styles, screen right/up, sprint/release, C, F6 edges, pointer settings at three ratios, paused blend, preference reload/invalid fallback and F8 return.
- Fresh display/rules: actual ordinary foul without marker; two active exclusions; authoritative countdown pause/time expiry, regain and goal re-entry; third-personal OUT; restart; Blue/White physical goals; primary/six faces/HUD agree. Actual B pass/reception/selection, Space charge/release/goal during blend, F steal, L lob, swept keeper catch and shot-clock violation passed in Sideline. Isolated AI/keeper probabilities were controlled/restored.
- Both goal areas, center, near/far sidelines, distant loose ball, keeper control, missing-target fallback, gallery cutaway and read-only snapshot checked. All four actual sprint acquisitions, exact overview each time, one halftime swap, FINAL and restart preference passed. Existing sprint competition rerun with Sideline: human/AI wins, one whistle/drop/water/pickup, real pointer sprint hold/release, capped effort/stamina and pause/resume.
- Actual advancing Game views inspected/captured at **1920/2340/2400×1080** and all quality presets. Before/after opening and matched F8 poses, two gameplay styles, both goals, primary close-up/exclusions/goals/final, sign correction and wide HUD under **Logs/MatchBroadcast/**. F8 close-ups temporarily hide/restore Canvas; some fixtures park AI. Additional full-arena inspection uses temporary FOV64 only; gameplay lens unchanged.
- Fresh Boot/menu/pointer **Lobby → current Match → Result → Lobby** passed: restart cancel/confirm, abandon with no reward/stray holder, four real acquisitions with accelerated period endings, official victory/one receipt, duplicate reward rejection and serialized verification profile reload. **Real disk profile SHA-256 unchanged.** Reused reports/screens under **Logs/FieldSprint/**.
- Final named acceptance runs passed; initial fixture timing/JSON/AI-reset probes were corrected, not counted as passes. Final current Console **0 errors /0 warnings**, compilationFailed false; historical Pipeline buffer retains prior warnings/resolved probes. No manual Console clear. Native scene empty-value YAML triggers read-only Git whitespace notices; hand-authored source/docs checks are clean. Existing QualitySettings AA difference predates M19 and was preserved.
- No physical Android/iOS, build, profiler, thermal or full-duration match claim. Period endings accelerated; exhaustive penalty flight, exhausted rosters, arbitrary camera tuning and notched-device safe areas remain unverified. Main board is environmental/partly outside normal frames; HUD stays primary. Next work needs separate authorization: physical-device framing/performance, then actual replay implementation.
- Delivery: saved arena, Play stopped, Boot play entry, original Game-view/input-focus/preference bytes restored; background/auto-tick overrides removed. No Git mutation or old-plan.md change. Preserve unrelated untracked sources/references/package settings and existing local changes.

## Previous handoff - Milestone 18 sprint refinement and championship lighting

2026-11-16: M18 implemented and focused Editor acceptance completed. M19 was explicitly authorized during handoff and is now the active task. Human-only Git writes remain permanent. Existing OlympicMatchArena/GUID, 14 rigs, water, rules, M16 HUD and hub/economy remain. Full current M18 details, exact before/after captures, checks, limitations and manual steps: **docs/SPRINT_POLISH.md**. Exact file inventory: **docs/MILESTONE_18_FILES.txt**.

- Native exact recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreSprintPolish.unity**. Added repeat-safe **Tools > Water Polo > Arena > Refine Sprint and Championship Lighting**; already applied, scene saved. No obsolete setup ran.
- Deterministic configurable existing roster IDs prototype-blue-1 / prototype-white-1; eligible cap/ID fallback. Designated field at team-left 1.05 m from central keeper, own goal line, legal spacing. Separate PlayerSelection.SwitchingLocked rejects C/right shoulder/mobile/direct changes through setup/race. Genuine pickup restores ordinary possession transfer and selection; teammate proximity cannot steal control.
- Existing stroke authority remains: 0.16 s cap, +0.28 effort, 0.85/s decay, at most 1.35x acceleration, unchanged maximum speed/stamina. Real non-blocking safe-area SprintEffortHUD, cyan fill/gold near max, accepted-stroke pulse, fade and pause reset. No new input system or menus.
- Invalid staged athletes restage; invalid racers or 18 s unresolved deadline release normal selection/pursuit with the same neutral ball. PrototypeMatch awaits real possession before advancing opening clocks; no invented award/teleport. Original four-period mapping (80 live ->480 display), 28 s shot and18 s exclusion remain.
- Real ball releases at whistle independently of opening jaws/1.05 s smooth cradle/cable retraction. Existing water contact ripple/splash. One-frame paused visual catch-up fixed; event-driven ready pose supports immediate pause/restart. Goals keep original center restart and hidden apparatus.
- Dark platform area was navy top surrounding narrow 0.25 m inset, not another decal. Existing renderers now show0.82 m colored sections with navy borders, brighter restrained local materials; feet/path/rails untouched.
- New combined hanging galleries/14 fixtures, cobalt reveals and open jumbotron edge spines; four shadowless accent lights. Same ChampionshipPresentation owns quality: Low3 lights/no shadows/no bloom, Medium9 lights/1024 one-cascade main shadow/bloom.19, High11 lights/2048 two-cascade main shadow/bloom.30. HDR and original transparent water preserved; local copied settings and128px reflection. Added lighting11 renderers/5696 vertices; no new colliders/particles/extra shadowed lights. Asset folder~0.75MiB. No phone performance claim.
- GameplayCamera code/tuning unchanged. Matched opening pose(0,28.926,-21.6945), rotation(53,0,0), FOV52. Seven before/after actual views under Logs/SprintPolish. F8 oblique inspection can project hanging galleries over the pool; inspect/manage this in M19 occlusion work, without altering the opening.
- Fresh acceptance:847 protected records unchanged, exact recovery/same GUID, all14 rigs, oneCanvas/EventSystem, save/reload/repeat safety; keyboard/gamepad/mobile switch lock; five actual touch edges=five strokes; actual acceleration/meter; friendly/opponent pickup and contention; invalid/deadline fallback; independent release/water contact/pause; ordinary foul/exclusion/re-entry; three quality tiers and1920/2340/2400x1080 layout. Reused focused M17 competition/actions/periods and official hub loop passed, including physical human/AI wins, keeper catch, Blue/White goals/normal center passes, all4starts/one halftime swap, restart/abandon/fulltime/one reward. Logged hub loop0 new runtime errors, user profile hash unchanged.
- Final current Console0 errors/0 warnings, compilationFailed false. Historical buffer keeps older warnings and resolved development/probe errors. Full-time periods were accelerated; keeper/contact fixtures controlled/restored AI/probability. No device/build/profiling/full-duration/exhaustive penalty/depletion tests. No unresolved focused acceptance failure. Temporary verification settings restored, Play stopped and Boot entry restored before M19.
- New SprintPolishSetup/SprintEffortHUD/recovery/native assets and verification runner. Existing sprint/selection/match/input/presentation/helper and docs extended; complete paths in manifest. Original assets, old-plan.md and all prior unrelated changes preserved. No Git mutation. M19 authorization supersedes this milestone's stop boundary.

## Previous handoff — Milestone 17 authentic field and competitive period starts

**2026-11-16: M17 implemented, saved and focused Windows Editor acceptance completed.** Explicit M17 authorization supersedes earlier milestone stop instructions. The existing **OlympicMatchArena** scene/GUID, Championship Night venue, transparent water, 14 rigs/animations, M16 HUD, normal gameplay, hub and economy remain. **Stop after M17.** Human-only Git writes remain permanent; everything is unstaged/uncommitted. Full architecture, rule reference, tests/screens, limitations and manual checklist: **docs/FIELD_AND_SPRINT.md**. Exact changed/created paths including native assets/metas: **docs/MILESTONE_17_FILES.txt**.

### Applied scene, field and equipment

- Production scene **Assets/Scenes/OlympicMatchArena.unity** saved through native Unity operations; exact pre-M17 recovery **Assets/Scenes/Recovery/OlympicMatchArena_PreAuthenticField.unity**, outside builds, original scene GUID retained. Recovery shares scripts, so it restores layout rather than source. Setup already applied; **Tools > Water Polo > Arena > Set Up Authentic Field and Period Sprint-Offs** is repeat-safe on saved OlympicMatchArena outside Play Mode.
- Authoritative field remains **25 × 20 m**, center `(0,0,0)`, water Y=0, goal fronts Z=±12.5. Goal-measured markers mirror: white goal ±12.5, red 2 m ±10.5, red 5 m ±7.5, yellow 6 m ±6.5, white half 0. Side rope sections red 0–2 / yellow 2–6 / green 6–half. End ropes 0.30 m behind goal lines, white/red exceptions and east exclusion corner sections. Existing exclusion waiting coordinates are unchanged.
- **Competition Field Equipment** combines waterline-aligned 0.12 m spool buoys, cables/anchors and cross-marks by material. The old purely visual **Water Polo Field Markers — no race lanes** group is inactive: no football center circle, floor center bar or oversized goal-area graphics. Coordinates, formation/start markers and original decorative tile assets remain.
- Both original goals now have chamfered rectangular white **3.0 m inner width × 0.9 m height / 0.08 m frame**, metal stays/supports and an enclosing lightweight cord net with subtle bounded reaction. Original GoalMouth, post/crossbar/net colliders and scoring direction are unchanged/enabled; old renderers retained disabled. No blind collider rescaling.
- New **1.0 m wide / 0.70 m above water** referee lanes at X=±12.65 have navy tops, trim, outer rails, access steps and section-color inset/ticks. Existing referee movement is retained with measured feet at Y≈0.70, root/deckHeight 0.62, offset 2.65 and travelFraction 0.49. Benches remain clear. The colored inset on navy is an intentional visual simplification of the regulation's fully colored platform top. New equipment adds no gameplay colliders.
- Reference consulted: February 18 2026 World Aquatics Competition Regulations Part Six, articles 6, 21.6, 21.7, 21.9 and timing glossary. Exact PDF link/pages and interpretation are in the guide. Current configured game dimensions/rule durations remain; this is not a claim of full regulatory compliance.

### Period sprint state and controls

**PrototypeMatch remains the sole phase/transition authority.** Appended SprintSetup → SprintRace → Playing phases run on initial start, confirmed restart and beginning of Q2/Q3/Q4; ordinary goals keep the conceding-team center pass. No sprint after goals or resume. Q3 uses the existing single halftime end swap; later periods preserve score/fouls, new matches restore original ends/counters.

**PeriodSprintOff** is a staging and motor/input adapter delegated by the match. Every eligible player starts on their own defended goal line; six fields spread with legal spacing and the keeper central. Fastest eligible field athlete/cap tie-break designates each sprinter; Blue is auto-selected. Existing treading pose/root proxy is used, no model or animation change. Preparation lasts 2 pause-aware seconds, movement/pickup/taps gated. One SprintStarted event drives the existing whistle and small M16 banner. The existing camera smoothly frames start/race and returns to its ordinary profile after acquisition.

**BallController alone owns the real ball.** Temporary support disables its collider/collection, synchronizes visible/physics pose in a small open cradle at center. Whistle restores gravity with zero velocity for a 0.45 m drop above the existing surface center; collection stays gated until real water contact. Existing water-impact effects are reused. Reusable visual-only holder lowers 1.5 m over 0.55 s, hides, and has no obstruction. Genuine existing TryCollect/PossessionChanged transitions to Playing immediately: no assigned winner, ball teleport, free pass or defender freeze. Pause/restart/exit clear stroke/holder state. Match/shot/exclusion clocks still run only during Playing, using acquisition as first-contact timing; **80 live -> 480 displayed / four periods / 28 s shot / 18 live s exclusion** unchanged.

During race, repeated **Left Shift / touch SPRINT** accepts at most one stroke per 0.16 s, adds capped/decaying energy and increases acceleration to at most 1.35×; maximum speed and normal stamina costs remain. No pre-whistle banking or post-race energy leak. Holding remains accessible; steering has priority, with auto-swim toward center only when sprint is requested and directional input is zero. SwimmerInput stays the human movement writer. AI uses the same motor/stamina/cap with 0.16–0.30 s reaction; supports use existing formation. Human and AI wins were both observed. Ordinary buttons return on acquisition; opening UI shows only relevant SPRINT/COLLECT, READY/SPRINT and one banner. Existing swimming wakes also render during race. A short motor departure corridor preserves legal starting positions before returning to normal boundary clearance.

### Editor menu cleanup and exact launch commands

`Tools > Water Polo` now has **Play Hub Entry**, **Open Main Lobby**, **Play Olympic Match Arena**, then **Arena / Match UX / Animated Athletes / Verification / Recovery / Legacy Prototype Tools**. The 42 registrations are unique. Normal launch/open commands were executed successfully. Old movement/ball/2v2/AI/keeper/scoring/M8–M11/realism/venue/water/PrototypePool commands remain in Legacy; source helpers and scenes were retained because current tools/tests use them. Rebuild/dimensions/activation/frontend/athlete-import/fallback commands live in Recovery. No setup code was deleted. HubApplication only changes its missing-catalog diagnostic to the new path.

- Normal full flow: **Tools > Water Polo > Play Hub Entry**, PLAY → Division 1 / unlocked mode → Launch.
- Isolated current match: **Tools > Water Polo > Play Olympic Match Arena**.
- Open without playing: **Tools > Water Polo > Arena > Open Olympic Match Arena**; **Open Main Lobby** at root.
- Restore normal startup after direct testing: **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**.
- **Tools > Water Polo > Recovery > Open Pre-M17 Arena Scene** is the native layout recovery; base arena rebuild commands are not review/launch actions.

### Fresh verification — Unity 6000.6.7f1 Windows Editor

Runner **Tools/Verification/FieldSprintVerification.cs** uses async Pipeline script run, advancing Play Mode, actual Input System pointer/keyboard/touch and real Game-view captures. Reports **Logs/FieldSprint/** are ignored. Compilation and runner diagnostics passed. Final current Console **0 errors / 0 warnings**, compilationFailed false; historical buffers retain older Burst/code-integrity and CS0184/UAC0009 warnings separately, without cache/security/package changes or manual clearing.

| Check | Confirmed result |
| --- | --- |
| Structure/safety | Exact native recovery, same GUID, 857 protected athlete/tuning/M16 Canvas records unchanged; original Boot/MainLobby/PrototypePool/catalog/build-entry/swimmer/water hashes; 14 original rigs, one Canvas/EventSystem, no missing scripts; measured mirrored markers, original goal colliders, repeat-safe save/reload. |
| Sprint competition | All14 on current goal lines/spacing; no pre-release tap/movement/pickup, real supported/dropping/water-contact ball and one claim/whistle; visible ball/cradle correct on immediate pause; capped Shift, real pointer hold/release and repeated synthetic touchscreen taps; stamina and pause/resume; human win, AI win, immediate losing-team movement. |
| Goals/actions | Actual post and crossbar ball collisions at both ends, no outside/post-height scores; physical goals both directions, existing keeper swept catch, ordinary keyboard swim/sprint/pass/reception/control transfer/charge release/steal/lob. |
| Four periods/restarts | All four real sprint starts/acquisitions, exactly one halftime swap, preserved score; goals remain conceding center passes without sprint/cradle; draw FullTime and fresh restart. |
| Hub/economy | Actual Boot/lobby pointer Division1 launch; restart cancel/confirm changes session without rewards; abandoned sprint removes holder and returns unfrozen lobby/no rewards; four official real sprint acquisitions, one1:0 victory receipt, repeated bridge/replayed receipt no duplicate transactions, JSON reload/results/lobby; **real profile disk hash unchanged**, isolated memory repository. |
| Game views | Warm actual **1920×1080 / 2340×1080 / 2400×1080**, opening-control bounds, start/race/overview/goal/platform/cradle and all three quality presets; referee feet/lane alignment. Final representative images visually inspected. Goal/platform/cradle detail captures explicitly use paused F8 inspection and temporarily hidden Canvas. |
| Menus | All42 unique paths; actual root hub/lobby/direct arena and Arena/Open commands exercised. Legacy sources retained; dangerous rebuild commands were intentionally not rerun. |

Retained-log audit caught an early abandoned-sprint **MissingReferenceException**: old PrototypeMatch.OnDisable reset the roster after a goal object was destroyed. Fixed OnDisable to restore suspension/unsubscribe only, keeping resets in explicit commands. **Fresh-Hub-Console-PASS** reran the complete official loop with an Application.logMessageReceived observer: **0 new runtime errors**, including sprint/full-time scene unloads. The old exception and a corrected CLI spelling error remain in the historical buffer, alongside older warnings. No manual log clearing; fresh runtime observation supports the acceptance result.

Full-time tests accelerated period endings through original **AdvanceLiveTime**, after real sprint pickups. **No full-duration playthrough or physical phone testing.** Isolated action/scoring fixtures temporarily disable/restore AI; keeper chances controlled/restored only for catch probe. Earlier idle-state probe timeouts were corrected by resetting the fixture and reran successfully. Older broad foul/penalty suites were not rerun; no unresolved focused acceptance failures remain.

### Files, budgets, limits and next step

Modified arena, 14 existing gameplay adapters/presentation scripts, one hub diagnostic string, 22 Editor files' menu attributes, plan/README. Added **AuthenticFieldSetup**, **PeriodSprintOff**, **SprintBallReleaseVisual** and metas; **Assets/Arena/AuthenticField/** (23 meshes, 8 materials, 1 prefab, native metas, ~4.24 MiB); exact recovery scene/meta; verification runner; guide and exact file manifest. Scene equipment/goals: **26 renderers / 23 shared meshes / 86,552 referenced vertices**, no individual buoy GameObjects or added lights/colliders/particle systems. Counts do not establish mobile draw calls/frame time. Existing Low/Medium/High effects/water/crowd budgets remain.

Prior M16 changes and initial TMP fallback-font modification/unrelated SourceAssets, Pipeline settings, old-plan.md and tatus remain. No profile/economy semantics, model/clip, water shader, stadium art or unrelated files changed. A PDF parser was placed only in ignored Logs/FieldSprint/python for rules extraction, no Unity/system dependency. Delivery: scene saved, Play stopped, Boot and original Game-view/input-focus settings restored, auto-tick/background command override disabled, serialized PlayerSettings value retained. No Git write operations.

Limits: no device build/profiling/thumb comfort, statistical sprint balance, full-duration match, exhaustive exclusion/penalty/OS-interruption coverage. Existing treading pose is the start pose; no wall-grip animation. If a team has zero eligible fields, preparation warns/stays stopped rather than inventing a substitute; forfeit rules remain unsupported. Whistle uses the existing procedural fallback when unassigned. Net reaction/cradle are bounded presentation. Manual review: Boot launch; tap/hold/steer race; pause/drop/restart/exit; both goal directions/center passes; all4periods/Q3swap/fulltime/reward; three ratios/quality tiers. Guide contains exact steps and captures. Next separately authorized work: physical-phone comfort/profiling and measured difficulty balancing. **Stop after M17.**

## Previous handoff — Milestone 16 match HUD, controls, pause and UX

**2026-11-16: M16 implemented and focused Windows Editor acceptance completed.** The developer explicitly authorized this match UX pass, superseding earlier milestone stop instructions. Production remains the existing **OlympicMatchArena**, with Championship Night art, transparent water, fourteen rigged athletes, gameplay, AI, rules, hub and economy preserved. **Stop after M16.** All work remains unstaged/uncommitted; human-only Git writes remain permanent. Full implementation, exact file manifest, evidence, limitations and manual checklist: **docs/MATCH_UX.md**.

### Saved scene and commands

- Existing scene/GUID: **Assets/Scenes/OlympicMatchArena.unity**. Exact native pre-change recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreMatchUX.unity**, outside builds. Created through AssetDatabase.CopyAsset before structural UI edits. Recoveries share scripts, so this is scene/layout recovery, not a source rollback.
- **Tools > Water Polo > Match UX > Set Up Milestone 16 Match Interface** on saved OlympicMatchArena outside Play Mode. Already applied; repeat calls preserve tuning and add only a missing standalone Close affordance. No Inspector wiring needed.
- Normal entry: **Tools > Water Polo > Play Hub Entry**, PLAY -> Division 1 / Training -> Launch. Open/direct test: **Tools > Water Polo > Arena > Open Olympic Match Arena / Play Olympic Match Arena Directly**. **Use Boot as Editor Play Entry** restores normal startup.
- Delivery state: scene saved, Play stopped, Boot entry and original Game-view selection restored; temporary Input System focus overrides and Pipeline background/auto-tick verification overrides restored/disabled.

### Implemented presentation and input

- **ScoreCard:** navy/cyan/white broadcast panel with score, dedicated Q/time fields, separate shot clock, authoritative LIVE/STOPPED/PAUSED/FINAL state and possession indicator. Brief score pulse; amber/red low-shot-clock accent with no flashing. No clock/rule configuration changed.
- **One MatchNotification banner:** real goals identify team/score; free throw, exclusion, penalty, actual shot-clock turnover, center restart, quarter/half/final use short title/context. Important events replace lower ones; lower events cannot overwrite unexpired important decisions. No stacked queue. Center PASS instruction stays while awaiting legal release. PresentationTime controls expiry; unscaled time animates fades; menus suppress the banner. Existing stopped-phase menus visibly present period/half/final.
- **Retired overlapping production graphics:** old EventCard, GoalLabel, RestartInstruction, QuickControls, duplicate strategy and ArenaHudReadability plates stay serialized but inactive. Existing audio, rules status and world ball cue remain.
- **Thumb controls:** 240-pixel joystick with immediate drag, existing dead zone and one pointer owner; release/pause/focus/disable recenters it. 176-pixel SHOOT/STEAL primary position, separated PASS/LOB/SPRINT/SWITCH, NORMAL/SKIP/HIGH or loose-ball COLLECT and Press/Zone. Attacking possession keeps action positions visible/dimmed when ineligible; defense uses STEAL. Existing rules gate every command. SwimmerInput remains the sole movement writer and nonzero keyboard/gamepad movement takes priority.
- **Holds/feedback:** existing ShotCharge source ownership and single down/hold/up release retained, accepted-press feedback, no onClick shot path; existing shot power bar above SHOOT. Independent pointer IDs work together; pause/selection/phase/application interruption cancels holds. Original green ring pulses once on switch and returns to its authored scale; compact athlete/stamina card identifies goalkeeper control. No new selection logic or world assets.
- **Pause:** Resume, Settings, separate Controls, Restart confirmation, Exit to Main Menu confirmation; actual score/quarter/time summary, dimmed pool, X/Escape. Period/halftime X/Escape continue exactly once. Full-time X/Escape use the existing saved-result/lobby route; standalone full time returns to the legacy match menu, whose X/Escape starts fresh play. Completed-result exit wording is distinct from abandonment.
- **Real settings:** original audio/crowd (where supported), camera and graphics; added Small/Normal/Large thumb groups (0.85/1/1.12×) and 40–100% opacity. Additive local GamePreferences fields only, same separate PlayerPrefs key. No music slider, fake haptics, HUD scaling framework or profile/economy migration.
- **Responsive layout:** existing 1920×1080 height-based CanvasScaler and safe-area roots. One Canvas/EventSystem. Reusable graphics, non-raycasting decoration and cached selection references; no runtime HUD object reconstruction, art imports or dependencies.

### Confirmed authorities and safety

PrototypeMatch still solely owns score, phase, transitions, IsPaused, timeScale/audio suspension and pause-aware PresentationTime. MatchClock still maps **80 live seconds -> 480 displayed**, four periods; PossessionShotClock still owns independent **28-second** possession. MatchRules owns fouls, eligibility and **18-live-second** exclusions. BallController alone owns possession/flight/scoring contact preparation; PlayerSelection owns control; SwimmerStamina/ShotCharge retain their data. Existing execution orders, velocities and command gates are unchanged. No timeout system currently exists and none was invented.

HubMatchBridge still submits FullTime to HubProgression and binds fresh restart sessions. Confirmed abandonment grants no completed rewards; Training grants no economy/progression; receipt replay does not duplicate rewards. No hub, catalog, save, arena, athlete, animation, water shader or physics source was changed. Original Boot/MainLobby/PrototypePool/build-entry/catalog hashes and **3,317 original non-Canvas component records** are unchanged. Initial fallback-font modification and unrelated untracked Pipeline settings, SourceAssets, old-plan.md and tatus were retained. old-plan.md was not edited.

### Fresh verification — Unity 6000.6.7f1, Windows Editor

Async Pipeline **script run**, actual advancing Play Mode, actual Game views, injected Input System devices, focused physical/rule fixtures. Main source: **Tools/Verification/MatchUxVerification.cs**; reused **Milestone11Verification.MobileButtons / LiveAIPause**. Reports/captures: ignored **Logs/MatchUX/**.

| Check | Confirmed result |
| --- | --- |
| Compilation/structure | Fresh compile without errors; verification source without diagnostics. Exact native recovery/same scene GUID, 3,317 world records preserved; one Canvas/EventSystem, custom CanvasRenderers, no missing scripts, repeat-safe setup and saved reload. |
| Existing controls | Joystick movement, keyboard priority, sprint/stamina, physical pass/reception/control transfer, charged shots/source ownership, lob, switch without possession, legal steal, Press/Zone, skip mode and temporary Blue keeper possession passed. |
| Synthetic touch/gamepad | Actual UI module processed two simultaneous Touchscreen contacts for joystick+sprint/release. Gamepad movement and South charge/release passed. Touch SHOOT showed power feedback, fired once, rejected duplicate release, canceled through pause. C switching/ring pulse/restore and keeper identity passed. |
| Menus/settings/pause | Actual mouse/keyboard Pause/Resume, Controls/X, Settings/X, supported settings applied/persisted, restart confirm/cancel, exit cancel, repeated Escape and no duplicate menus. Live AI decisions/swimmer and ball poses frozen and resumed. |
| Events/clocks | Physical Blue/White goals, conceding center restarts and manual Blue PASS, actual shot-clock violation, warning color, original period display ratio, ordinary foul, exclusion priority, penalty setup/flight pause and resumed physical motion. |
| Period/results | Q1/Q3 breaks, halftime side switch exactly once, Q4 FullTime; draw/victory/defeat; correct next quarter, real X/Escape transitions and standalone result/menu close. |
| Hub/economy | Actual Boot/MainLobby pointer Division 1 launch, current arena, restart session reset/cancel, abandoned exit/no rewards; official win/loss FullTime receipts/results/lobby, repeated bridge submit and receipt replay deduplication, JSON profile reload, Training/no rewards. Isolated in-memory service; **real user disk profile hash unchanged**. |
| Responsive | **18 actual captures** at 1920×1080, 2340×1080, 2400×1080: attack/defense, Pause/Settings/Controls/confirmation at each. Largest controls + simulated 70px sides/24px vertical safe insets. Bounds/nonoverlap/text overflow passed. Representative actual images plus goal/center/power/keeper/results visually inspected. |

Final current Console: **0 errors / 0 warnings**, compilationFailed false. Historical Pipeline buffers separately retain older Burst/Windows-code-integrity and existing analyzer warnings; no security/package/cache changes or manual Console clearing were used to hide them. Evidence names and full scope are in docs/MATCH_UX.md.

### Files, limits and next step

Modified **OlympicMatchArena.unity**, nine existing gameplay UI/input/preference scripts (**GamePreferences, GameplayActionButton, MatchAtmosphere, MatchHUD, MatchMenuController, MatchSettingsView, MobileMatchControls, PrototypeScoreboard, VirtualSwimJoystick**), **plan.md / README.md**. Added **Assets/Editor/MatchUxSetup.cs**, **Assets/Scripts/Gameplay/MatchNotificationBanner.cs / MatchControlPresentation.cs**, their metas, native recovery scene/meta, **Tools/Verification/MatchUxVerification.cs**, **docs/MATCH_UX.md**. No Git write or automatic checkpoint.

No physical Android/iOS, OS phone interruption, real-thumb comfort, exhaustive notch behavior, mobile build or performance test. Full-time loops used the original **AdvanceLiveTime API** to accelerate periods, not altered match settings or a full-duration playthrough; focused action/rule fixtures temporarily disable/restore AI. Live-AI pause and normal-entry checks are separate. The older broad M11 PauseAndRules probe's low-speed guaranteed penalty-goal fixture timed out in this arena; it is not claimed passed. Final penalty evidence covers setup/flight suspension/resume; ordinary physical goals are verified independently. No new timeouts, touch aiming, controller mappings, substitutions, music, haptics, automatic period continuation or economy.

Manual review: normal Boot launch; swim/sprint/switch/pass/lob/charge/steal/keeper; score both directions and manually restart Blue; pause during holds/flight/stoppages and review X/Escape/confirmations/settings; inspect all three ratios; continue halftime/full time and check one result/reward/lobby return. Detailed checklist: docs/MATCH_UX.md. Recommended next separately authorized work: phone comfort review and Android profiling, followed by measured layout/contrast tuning. **Stop after M16.**

## Previous handoff — Championship Night premium arena overhaul

**2026-11-15: major environment-only overhaul implemented in the existing OlympicMatchArena.** The developer explicitly authorized this pass after M14. The same scene/GUID, Boot/MainLobby/PLAY flow, 14 rigged swimmers, current audience, pool/water, HUD, controls and gameplay authorities remain. No external art/package import or Git write occurred. Unrelated local Pipeline configuration, SourceAssets, old-plan.md and tatus remain intact. Stop after this arena pass. Full guide: **docs/PREMIUM_ARENA.md**.

### Visual direction and implementation

Target: a **dark navy/cobalt indoor championship at night**, with a bright readable competition pool, darker spectator bowl, metallic framing and restrained ice-blue accents. Initial inspection/captures confirmed the previous light ambient, pale deck and luminous daylight panels caused a flat, bright presentation. Actual before/after Game views guided a second art pass for bench readability, controlled LED output and original end-wall water motifs.

- **Architecture:** retained hall shell/roof with dark acoustic glazing/panels, structural fins/piers, large end portals, upper-bowl ribbons, section plaques, hanging championship banners, original three-ribbon water signatures and perimeter broadcast trusses. Old bright venue finish is replaced rather than overlapped.
- **Deck/referees:** honed basalt-style ceramic and fine rubber event aprons, white/metal pool trim, drain-grate detail, cyan separation, subtle competition graphics, clear **1.75 m official lanes** centered on existing referee paths. Field distances/floats/bounds remain in place; distance text is lighter and duplicate old access lettering is retired.
- **Teams/officials:** padded chamfered seven-seat reserve zones, armrests/stripes, technical backwalls, coach consoles, task fixtures, cooler/flight-case/towel/bottle clusters and marked access. West timing desk, terminals/chairs and sideline digital barriers sit behind clear referee lanes. Fine props retain existing Medium/High budgeting.
- **Seating:** ten reusable six-tier blocks with sculpted seats, rails/armrests, tier edges and stair nosings. Original spectator meshes/materials/positions and density groups are unchanged; new seats retain their row/seat coordinates.
- **Displays:** larger four-face end-hung jumbotron plus two deck timing screens and six static sideline branding barriers. Six live score/clock faces use existing **ArenaEventDisplay**. No additional scoring authority, video feed, UI Canvas or render texture.
- **Water:** original shader/materials/profiles, transparent surface, caustics, field geometry, M14 animations/effects and underwater visibility are preserved. A new **128 px HDR reflection bake** and lighting/grading integrate the pool with the darker hall; no water experiment/replacement was introduced.

New prefab **Assets/Arena/ChampionshipNight/Prefabs/ChampionshipNightVenue.prefab** is under the original foundation, organized into Competition Deck and Clear Official Lanes; Blue and White Team Technical Areas; Match Operations and Protected Sidelines; Grandstand Portals and Championship Architecture; Perimeter Broadcast Rigging and Pool Light Banks; Championship Scoreboards and Presentation Screens; Fine Operations Props; Technical Area Washlights. Reusable **ChampionshipSeatBlock.prefab** sits under retained Spectator Stands. New source assets: combined meshes, **14 shared materials**, three small authored finish textures, reflection, three URP copies and three Volume profiles. About **7 MB including metas**; no imported environment package.

The previous Championship Venue Finish instance, old bench/equipment/official-table objects and old seat-block instances are retired where replaced. Source prefab assets/recovery scenes remain, but retired venue and bench prefabs are **not production scene dependencies**. One pool and one water surface remain. No presentation collider was added.

### Lighting, rendering and performance boundaries

Existing main directional: cool tint, intensity **0.78**, elevation 66°, shadow strength 0.76. Dark trilight sky/equator/ground **#344767 / #1B2D49 / #0B1628**, reflection intensity 0.72, deep navy camera clear instead of bright skybox. Camera movement/lens/framing/controller data are unchanged; background/post flags are intentional rendering changes.

**ChampionshipPresentation** observes preferences and selects the local Volume/light budget. ArenaQualityController retains pipeline/water/crowd/prop ownership, pointing to new isolated pipeline copies and the new fine-prop group. Generic GamePreferences is unchanged. Its older Medium/Low rule disables all post, so the scene-specific observer keeps cheap Neutral grading enabled on all tiers and disables Bloom on Low through the profile. No other scenes' preferences/looks are changed.

| Tier | Pool spots | Bench washes | Bloom | Preserved budgets |
| --- | ---: | ---: | --- | --- |
| Low | 2 | 0 | Off | Existing Low water/crowd/props/shadow behavior |
| Medium | 4 | 2 | 0.12 | Existing Medium water/crowd/props |
| High | 4 | 2 | 0.22 | Existing High including planar water reflection |

One existing directional is additional: **3 total active lights Low / 7 Medium-High**, with **no additional-light shadows**, four additional lights per object. Neutral tone, exposure +0.05, contrast +12, saturation −6, vignette 0.13. HDR on; bloom threshold 1.1, scatter 0.5, quarter-resolution Dual filter, max four iterations, HQ filtering off. No SSAO, volumetric fog, DoF, grain or motion blur. World-space text remains separate; geometry is combined by material/zone. Main venue: **120 renderers / 34,852 referenced vertices** including text. Ten seating instances: **40 renderers / 144,960 referenced vertices**, sharing a four-mesh block. Content counts do not establish draw calls/frame rate/device performance.

### Safety, files and exact commands

Production: **Assets/Scenes/OlympicMatchArena.unity**, same GUID. Exact native pre-pass recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreChampionshipNight.unity**, outside builds. Original scenes and prior recovery assets remain. Recovery is a scene snapshot with shared scripts, not a Git checkpoint.

- **Tools > Water Polo > Arena > Open Olympic Match Arena** / **Play Olympic Match Arena Directly**; **F8** retains the existing five inspection views.
- **Tools > Water Polo > Arena > Apply Championship Night Overhaul** creates once, then opens/preserves authored tuning; intended scene edits must be saved and Play stopped.
- **Tools > Water Polo > Arena > Refresh Championship Night Reflection** rebakes intentionally without regenerating geometry/materials.
- Old **Polish Existing Championship Venue** now guards against adding legacy scenery over the newer venue. **Refresh Venue Reflection** delegates safely to the new bake when installed.
- **Tools > Water Polo > Play Hub Entry** / normal Boot entry retains the frontend. **Use Boot as Editor Play Entry** restores Boot after direct testing. **Boot entry and original Game-view size are restored at delivery; scene saved and Play stopped.**
- Older **Apply Configured Field Dimensions** explicitly rebuilds the base environment and is not a review tool; reinstall/reverify the presentation after a deliberate regeneration.

Modified **Assets/Scenes/OlympicMatchArena.unity**, **Assets/Editor/ArenaVenuePolishSetup.cs** (legacy tool guards), **plan.md**, **README.md**, **docs/WATER_VISUALS.md**. Added **Assets/Editor/PremiumArenaSetup.cs**, **Assets/Scripts/WaterVisuals/ChampionshipPresentation.cs**, **Assets/Arena/ChampionshipNight/** prefabs/meshes/materials/textures/settings/metas, pre-pass recovery scene/meta, **Tools/Verification/PremiumArenaVerification.cs**, **docs/PREMIUM_ARENA.md**. No swimmer, crowd, gameplay, menu, economy, build-entry or water shader source/asset edits.

### Fresh focused verification and evidence

Windows Unity **6000.6.7f1 / URP 17.6**. Actual advancing Play Mode and Game view, native setup/save/reload, async Pipeline **script run** checks. Source **Tools/Verification/PremiumArenaVerification.cs**; reports/captures in ignored **Logs/PremiumArena/**. No historical full-game suites repeated.

| Check | Actual result |
| --- | --- |
| Structure | PASS **1,854 protected records**: original gameplay/HUD/referee scripts, rig hierarchy/renderers, physics, audience data and camera movement/lens tuning. Rendering/visual-only references intentionally differ. Exact recovery hash and same production GUID. |
| Assets | PASS byte hashes unchanged for Boot, MainLobby, build settings, match destination, hub catalog, all swimmer model/clip/material/controller assets and water materials. No missing scripts/materials, no new venue colliders; replaced venue/bench prefab dependencies retired. |
| Setup | PASS new and legacy setup commands preserve saved authored scene on repeat/reload. |
| Gameplay | PASS actual W/Shift swim/sprint, pointer Pause/Escape and stopped clock, pickup/pass/physical reception/selection, shot/real goal, six live score faces, AI bounds, referee positions, 14 rigged athletes and existing HUD. Controlled goal fixture temporarily disables/restores keeper components; no gameplay tuning persists. |
| Camera/lanes | PASS triangle sightlines for **35 visible athlete/goal targets** across three action positions against new venue/seating; both referee lanes free of new props. Focused samples, not exhaustive camera-path proof. |
| Rendering | PASS HDR, camera Volume mask/post flag, three local profiles/light budgets, actual resolved Volume stack and original supported water variants. Grade on/off captures use a frozen same Game view; nonzero image difference confirms it is active. |
| Normal entry | PASS real pointer **PLAY → Game Modes → Training → Launch → upgraded arena → Pause/Exit → original lobby**. In-memory fixture, user profile file hash unchanged; no menu edits. |
| Game-view review | PASS actual **1920×1080** before/after broadcast, overview, benches, reverse end and hall; white team, officials, jumbotron/waterline detail and **2400×1080 (20:9)** with existing visible HUD buttons in bounds. One labeled architectural inspection capture temporarily hides/restores Canvas; playable comparisons retain HUD. |
| Compilation/Console | PASS fresh compilation; final current Unity Console ground truth **0 errors / 0 warnings**. Historical Pipeline buffers contain resolved older entries. |

Reports: **Structure-PASS, Gameplay-PASS, Sightlines-PASS, Presets-PASS, HubEntry-PASS, FinalDetails-PASS, Grade-AB, Console-Final**. Images: **Before/After-Broadcast / Overview / Benches / Reverse / Hall**, **Verified-Scoring**, **Preset-0/1/2**, **Grade-On/Off**, **Framing-South/Centre/North**, **Final-White-Team-Area / Officials-and-Deck / Jumbotron-Detail / Clear-Water / Wide-2400**, **Hub-Launched-Arena**, **Returned-Lobby**, **Final-Venue-Inspection-NoHUD**.

### Licenses, limits and next step

**No external assets imported.** New geometry/material textures/graphics are original project assets. Existing MIT water and CC0 swimmer notices remain unchanged; no protected event/team branding. The authored three-ribbon water motif is original, not an Olympic logo. Supporting engine references and scoped visual scorecard are in docs/PREMIUM_ARENA.md.

This remains a procedural venue with simple underlying shell/crowd art, static banners, terminal placeholders and text displays, not photoreal final architecture/video. Current M14 athletes/animations are retained as requested. Android/iOS GPU/CPU/thermal performance, real phone touch/notches and full-length match/rewards return were not tested in this pass. Six shadowless local lights, HDR/bloom, seating and existing High planar reflection need device profiling. Scoped view samples are not exhaustive camera verification.

Manual next review: normal MainLobby launch; swim/sprint/switch/pass/lob/charge/shoot; inspect ball/ring/goals and submerged limbs, both ends/benches/official lanes with F8, all presets/pause/settings; finish once through results/rewards/lobby. Recommended next milestone **after separate authorization**: Android profiling and selective authored environment/prop meshes/textures guided by measured budgets. Stop after this arena pass; no later milestone started. All changes uncommitted; human-only Git writes remain permanent.


## Previous handoff — Milestone 14 rigged swimmers and interactive water

**2026-11-15: M14 implemented in the existing OlympicMatchArena.** This explicit authorization supersedes the older milestone stop points below. The improved arena, water shader, frontend, economy, match authorities and physics were preserved. Git inspection remained read-only; all work is uncommitted. Stop after this animation/water prototype. Full guide, licensing, exact commands and limitations: **docs/SWIMMER_ANIMATION.md**.

### Delivered visual architecture

Initial inspection found **no usable skeleton, SkinnedMeshRenderer or Animator**: current ArenaAthleteVisual prefabs were primitive meshes with small sinusoidal limb motions. SwimmerMotor, PlayerSelection, AI, stamina, BallController release/contact/possession events, goalkeeper references, waterline, PoolBounds and quality routing were inspected before integration.

The new **Assets/SwimmerAnimationPrototype/Prefabs/AnimatedWaterPoloAthlete.prefab** contains a visual alignment wrapper, a 66-bone skinned Humanoid model, cap/ear guards and swimwear, Animator/controller/mask and RiggedSwimmerVisual. It uses shared skin/Blue/White/keeper-red materials. The reduced imported model totals approximately **6.6k vertices / 9.9k triangles** across four skinned renderers. All 14 real athlete roots now use it, following successful lab, one-athlete and then 14-athlete checks. Original primitive visual objects/prefabs survive as inactive fallback; no demonstration swimmers replace roster entries.

Gameplay root → Animated Athlete Visual (visual Y alignment only) → WaterPoloAthlete (Animator + RiggedSwimmerVisual) → Armature, body, cap and ear guards. Valid Humanoid Avatar maps 22 core body joints; skeleton retains 66 bones. No new Rigidbody/collider, carry/release anchor or decision authority. Existing keeper arm references are retained on fallback pivots; the new visual observes save attempts separately. Physics and original gameplay component serialized records remain unchanged.

Base blend tree: **TreadWater → HeadUpSwim → Sprint**, actual motor velocity and stamina drive it. Treading uses alternating bent-leg circular motion and sculling; swimming has alternating arm strokes, submerged kicks and torso roll. A masked upper-body layer provides possession, pass, shoot, block and save poses. Free arms fade the layer out so prior actions cannot suppress the crawl. Pass/lob/shoot hooks observe the real Released event once and never delay/reissue it. Keeper block observes ThreateningShot; save pose observes SaveAttempts, not guaranteed catches. Turn follows the existing motor's smooth rotation; no dedicated turn clip. Head alignment holds the head bone about **0.26 m above the configured waterline** while motor Y remains unchanged. Root motion is disabled; imported motion/root channels are baked in place. PreserveHierarchy is required on model and separate animation FBXs for correct Humanoid retargeting.

These are functional **stylized prototype** motions, not final naturalistic water polo mocap. Art remains a jointed mannequin; recovery strokes, eggbeater mechanics, exact hand/ball contact and throw follow-through need authored refinement. No directional dives, advanced catches, contact/foul reactions, facial animation or final cap numbers are claimed.

### Actual sources and licenses

Downloaded from KinemIQ: Quaternius **CC0** mannequin plus **swim_idle_loop.glb / swim_fwd_loop.glb**. The original swim is paired-arm motion with travel; it is a reference, not a falsely labeled head-up freestyle. Converted **one model FBX + two reference animation FBXs**, and authored/adapted **eight project clip FBXs**. Only those eight project clips drive the production controller. Source pose data contributes to the treading foundation. No enormous pack or glTF package was installed.

Original files, exact URLs, SHA256s and CC0 text: **SourceAssets/SwimmerAnimation/**. Source/license declarations: kinemiq.app/sources, animations/move-swimming, animations/idle-in-water and quaternius.com/packs/universalanimationlibrary.html. **Assets/SwimmerAnimationPrototype/NOTICE.md** and **Assets/StreamingAssets/ThirdPartyNotices/Swimmer-Attribution.md / Quaternius-CC0-1.0.txt** preserve distributable notices.

The requested Asset Store **Basic Character Animations FREE 409552** listing was investigated (Standard Asset Store EULA / Extension Asset, Unity 6000.3.6, listed URP support), but **no actual package was downloaded or imported**. Its rig/clip quality remains unverified; optional manual acquisition is Asset Store account → Package Manager > My Assets. The obtained CC0 sources were sufficient for this milestone. Blender MCP was not connected; local **Blender 5.2.3 in headless mode** converted/reduced/fitted the assets without opening a user document. Reproducible script: **Tools/Art/PrepareAnimatedSwimmer.py**.

### Interactive water and mobile budgets

Existing **WaterInteractionEffects** remains presentation-only. It now preallocates **48** shared-mesh ring/splash entries: **16 ball-reserved / 32 athlete** at Standard; Low effect detail uses 8 / 12. The shared ripple mesh contains two concentric rings. Real ball contacts and throw releases remain authoritative event sources. Existing V wakes gain bounded sprint width and camera/range checks.

RiggedSwimmerVisual samples animated hands after alignment: downward surface crossings emit local splashes with a per-hand cooldown; stationary athletes emit small staggered rings roughly every 2.4 seconds; moving submerged hands/feet emit short microbubbles. **One native ParticleSystem, max 128**, manual pooled emission, about 0.4-second life, no collisions or per-stroke GameObjects. Particle simulation starts on demand and respects pause/clear. High surface range 65 m, Medium 45 m; bubbles Standard-only within 32 m; offscreen emissions are rejected. Existing graphics mapping is preserved: **Low effects Off**, Medium Low, High Standard. Main water materials/shader/clear-water visibility are unchanged.

Animators currently always tick and skinned bounds remain enabled offscreen to keep waterline and hand sampling reliable. Device profiling, animation culling and material/skinning batching are future optimization tasks; desktop evidence is not Android/iOS performance evidence.

### Scene safety, files and exact review commands

- Production scene remains **Assets/Scenes/OlympicMatchArena.unity**, original GUID/entry flow retained. Exact pre-animation scene recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreAnimatedAthlete.unity**, outside builds. Recoveries share updated scripts; the older pre-venue recovery also remains.
- Isolated **Assets/Scenes/AnimatedSwimmerLab.unity**, outside builds, uses existing transparent water/ceramic and the one rig prefab. It has no match controller, physics or economy. **Tools > Water Polo > Animated Athletes > Play Isolated Swimmer Lab**; keys **1 tread, 2 swim, 3 sprint, 4 possession, 5 pass, 6 shoot, 7 block, 8 save**. Stopping restores previous Editor entry.
- Same submenu: **Set Up Prototype and Lab**, **Apply to One Match Athlete**, **Apply to All 14 Match Athletes**, **Restore Original Match Visuals**, **Reimport Prototype Source FBXs**. First setup creates missing assets; repeated ordinary setup preserves tuned assets. Apply is additive/repeat-safe; One does not undo other opt-ins. Fallback changes visuals only. Explicit reimport reapplies importer settings after deliberate source regeneration. Require saved Edit Mode scenes.
- Existing **Tools > Water Polo > Arena > Play Olympic Match Arena Directly** and **F8** inspect the actual match. **Use Boot as Editor Play Entry** restores normal startup; **Boot entry is restored at delivery**. No build-list or lobby/destination changes.
- Modified: **Assets/Scenes/OlympicMatchArena.unity**, **Assets/Scripts/Gameplay/WaterInteractionEffects.cs**, **README.md**, **plan.md**. Added: **Assets/Editor/AnimatedAthleteImport.cs**, **AnimatedAthleteSetup.cs**, **Assets/Scripts/WaterVisuals/RiggedSwimmerVisual.cs**, prototype model/clips/materials/controller/mask/prefab/metas, lab/recovery scenes, CC0 source/notices, conversion script, **Tools/Verification/AnimatedAthleteVerification.cs**, **docs/SWIMMER_ANIMATION.md**. Previous venue polish changes and unrelated untracked files were preserved.

### Fresh focused verification

Windows Unity **6000.6.7f1**, actual advancing Game views captured and inspected, including 1920×1080 lab/production close views and broadcast. Source: **Tools/Verification/AnimatedAthleteVerification.cs**, asynchronous Pipeline script run entries; reports/captures in ignored **Logs/SwimmerAnimation/**. No historical full-game suites were rerun.

| Check | Actual result |
| --- | --- |
| Source compilation / Console | PASS fresh compile; final current Console ground truth **0 errors / 0 warnings**. Historical Pipeline retained buffers include older entries. |
| Lab rig/poses | PASS valid Humanoid, all eight preview states, head/waterline, submerged feet, bounded visual offsets, no XZ visual drift. After action-layer correction: about **0.51 m / 0.60 m** sampled hand travel over 0.25 s for swim/sprint. Actual Game captures inspected. |
| One-athlete integration | PASS actual W/Shift movement, sprint, idle ring, animated hand-entry emissions, pickup/pass/reception, one pass hook, one shot hook and real goal; pause, AI bounds, original HUD and presets preserved. |
| All 14 | PASS same focused gameplay check after rollout, including original HUD pointer Pause / Escape resume; 14 real athletes, no new visual collisions, correct waterline, three graphics presets and Low clearing. |
| Structure / persistence | PASS **518 original protected component records**, exact recovery hash, unchanged Boot/MainLobby/build/destination/catalog/water-material hashes. Repeat setup and saved scene reload retain exactly 14 active rigged visuals with valid references; originals inactive. |
| Keeper hook | PASS one real physical swept catch and one observed animation in the existing controlled endpoint fixture: **80 m/s, zero reaction delay, 100% save/catch temporarily in Play Mode**, restored afterward. Default 16 m/s fixture shots did not establish representative save animation coverage; production success rates were not measured. |
| Ball water / pooling | PASS real unreceived lob water impact, active local feedback, fixed effect-object counts and bounded buffers. Separate final particle check confirms simulation starts and microbubbles expire; max 128. |
| Final visual audit | PASS 14 valid Avatars/skin bone/material references, no root motion, correct physics/visual waterline. Cap-fit and actual broadcast captures reviewed after source regeneration. |

Reports: **M14-Lab-PASS**, **M14-Match-1-PASS**, **M14-Match-14-PASS**, **M14-Structure-PASS**, **M14-Keeper-PASS**, **M14-Water-PASS**, **M14-Particle-PASS**, **M14-Final-PASS**. Screens: **Lab-0..7**, **Match-Idle/Swim/Sprint/Possession/Throw-14**, **Match-Keeper**, **Match-Quality-0/1/2-14**, **Match-Ball-Water**, **Final-Cap-and-Waterline / Final-Cap-Rear / Final-Broadcast**.

Full-time rewards, complete menu round trips and division/save-economy suites were not repeated; those systems/entry files are untouched and their prior evidence remains below. No Android/iOS device performance, touch, long-session soak or comprehensive directional keeper-animation test is claimed.

Read-only `git diff --check` flags native Unity scene serialization whitespace on empty `value:` entries and line-ending notices; native generated formatting was retained. This is separate from the fresh zero-error/warning Unity Console result.

### Blender replacement and next step

Replace only the visual model child with a valid Humanoid athlete and compatible rest pose, hands/feet/head mapping; keep identity, motor, physics, carry/release anchors, AI and match authorities on existing athlete roots. Reuse Animator/controller/event observer and wrapper, adapt the skin/cap/swimwear slots in Theme, then retune visual head height/bounds. Reverify one lab athlete and one match athlete before all 14. Core mapping supports future retargeting; final finger mapping, per-athlete art, cap numbers and directional saves remain future work.

Manual review: preview 1–8 in the lab, then launch a normal hub match, swim/sprint/switch, pass/lob/charge/shoot, inspect both keepers and underwater limbs, pause/resume, compare presets/F8, finish once through rewards/lobby. Recommended next milestone **after separate authorization**: authored water-polo athlete, recovery/eggbeater/keeper refinement and Android profiling. No later milestone was started.


## Previous handoff — existing match arena venue polish

**2026-11-15: arena-only enhancement implemented in the existing Assets/Scenes/OlympicMatchArena.unity.** This new explicit authorization supersedes the earlier frontend-only scope below. No new playable destination, menu screen, match flow, athlete design or crowd system was created. Stop after this venue polish. Git was used read-only; changes are uncommitted. Existing Pipeline configuration, old-plan.md and tatus were preserved.

### Environment changes

- **Competition deck:** cobalt event aprons, white perimeter accents, restrained vermilion details and original SUPERSTARS AQUATIC CENTER floor branding. Existing deck/concourse surfaces use neutral non-slip ceramic, with metre UVs and approximately 0.6 m tiles. An original deterministic 128×128 grain/grout texture replaces the pool-blue tile appearance on the deck. Existing 7 m deck width,20×25m field, pool shell/depth, drainage and coping remain.
- **Officials/team areas:** protected 1.65 m referee lane finish and edge markings, preserving X=±12.65/Y=0.18 paths. Original benches/coaches stay in place, with technical-zone outlines, fascia/top rails and presentation strips. Added cooler/rack/towel placeholders, access signs, officials' chairs, terminal props and timing-desk fascia. No new colliders or characters.
- **Hall/seating:** existing ten seating blocks and all fans remain. Added aisle treads/nosings between blocks, section boards, structural piers reaching the existing 30 m roof, upper acoustic closure/clerestory ribbons, cornice edges, original championship banners and venue identity signage. Existing roof stays; broadcast trusses/luminaires sit below it. No protected Olympic/event logos, flags or licensed team identities were copied.
- **Lighting/reflections:** existing single directional light now has 68° overhead pitch, 1.65 intensity, near-neutral color, 0.48 shadow strength and balanced trilight fill. Fixture housings/louvres and emissive diffusers supply the visible rig. No extra real-time lights, new post-processing or GI lightmap bake. Emission is visible/reflective rather than a simulated spotlight contribution. A new 128 px HDR hall reflection bake includes the venue; water shaders/materials, quality profiles, caustics/refraction and High planar behavior remain unchanged.
- **Event presentation:** an end-hung four-face jumbotron beyond the north field and two low poolside boards use dark unlit LED faces with real score/period/display-time/shot-clock values. New `ArenaEventDisplay` observes PrototypeMatch. It subscribes to Changed, polls at 0.15 s intervals and rebuilds strings only when displayed values change. It cannot alter gameplay, clocks, rewards, input or HUD. Six faces share the same authority; no fake video feed or extra render texture.

### Hierarchy, assets and review commands

Original **Olympic Arena — WaterVisualTest Foundation** now contains prefab instance **Championship Venue Finish**, organized into Competition Deck and Protected Official Lanes; Team Technical Areas and Event Operations; Grandstand Framing and Arena Identity; Broadcast Lighting Trusses; Live Venue Displays. Geometry is combined by material/zone; world-space TMP remains separate. Existing pool/goals/referees/athletes/camera/HUD remain in place; no new Canvas/EventSystem.

Reusable prefab: **Assets/Arena/VenuePolish/Prefabs/ChampionshipVenue.prefab**. Its external match reference is assigned on the scene instance. **Assets/Arena/VenuePolish/** holds original meshes, ten independent URP materials, **NeutralCeramic.png** and **ChampionshipReflection.exr**. No downloads/new license obligations; prior water shader notices still apply. Uploaded pool-reference images were not available among project files, so the design follows the actual venue and written brief rather than claiming an exact reference match.

- **Tools > Water Polo > Arena > Polish Existing Championship Venue**: setup on saved Edit Mode scenes; repeated calls open/preserve tuned content without duplicating or regenerating it.
- **Tools > Water Polo > Arena > Refresh Venue Reflection**: explicitly rebake after intentional lighting/environment edits; does not reset lighting or rebuild geometry.
- Existing **Open Olympic Match Arena / Play Olympic Match Arena Directly** remain the inspection commands. **F8** retains all five original viewpoints and return to broadcast. Normal Editor Play entry was restored to **Boot**; **Play Hub Entry** uses the unchanged frontend.
- Exact recovery: **Assets/Scenes/Recovery/OlympicMatchArena_PreVenuePolish.unity**, outside builds. It shares project scripts; production scene GUID stays unchanged. The older **Apply Configured Field Dimensions** command explicitly regenerates the original foundation and removes the finish instance: reapply polish and reverify afterward. Do not regenerate merely to review the scene.

### Fresh focused verification

Source **Tools/Verification/ArenaPolishVerification.cs**, using Pipeline **script run** for async checks; reports/captures in ignored **Logs/ArenaPolish/**. No historical full-game suite or season/reward simulation was repeated.

| Check | Actual result |
| --- | --- |
| Structure | PASS **664** protected original records compared against recovery: existing scripts, colliders/poses, rigidbodies, camera/settings/pose, athlete visual transforms, audience meshes/renderers. No missing scripts/materials; 14 athletes, unchanged 20×25 field, six wired faces, no new colliders, one real-time light, exact recovery, repeat-safe setup. |
| Flow preservation | Boot, MainLobby, EditorBuildSettings, HubMatchDestination and HubCatalog byte hashes unchanged; same production scene GUID; Boot Editor entry restored. |
| Gameplay smoke | PASS advancing actual Game view, real keyboard swimming, pointer HUD Pause/Escape, physical pickup/pass/reception/selection, physical shot/goal, live score on all six venue faces, valid referee positions and Low/Medium/High water variants. |
| Camera | PASS new-venue triangle intersection checks for visible athlete/goal sightlines at near/centre/far action positions. Original camera/controller/viewpoint data unchanged. Focused checks, not exhaustive camera-path proof. |
| Existing app entry | PASS real pointer PLAY→Training→Launch enters upgraded OlympicMatchArena with 14 athletes; existing Pause/Exit returns to the same lobby. Isolated memory profile; user profile SHA256 unchanged; no menu edits. |
| Game-view evidence | Captured/inspected 1920×1080 before/after using identical broadcast, overview and bench viewpoints, plus waterline and lighting/roof views. Other captures cover action at both ends/centre, scoring and three presets. Paired stills freeze simulation only for composition without Pause UI; advancing gameplay was verified separately. |
| Console | Runtime/editor source compiles; fresh Console ground truth: 0 errors / 0 warnings. Existing ArenaDeveloperCamera DEVELOPMENT_BUILD/UAC0009 deprecation warning can appear on recompilation; that file is unchanged. Historical Pipeline buffers contain older entries. |

Added venue: **86 renderer components  / 14,088 referenced mesh vertices**, including world text; approximately 2.5 MB source assets including metas. These are content counts, not measured draw calls, GPU memory or device performance. New static geometry remains on all presets; prior water/crowd/prop budgets stay authoritative. No Android/iOS hardware profiling or full-length manual match was performed.

Evidence: **Compare-Before/After-Broadcast.png**, **Compare-Before/After-Overview.png**, **Compare-Before/After-Benches.png**, **Final-Lighting-and-Structure.png**, **Final-Waterline.png**, **Final-Broadcast-South/Center/North.png**, **Final-Scoring.png**, **Final-Preset-0/1/2.png**. Reports: **Structure-PASS.json**, **GameplaySmoke-PASS.json**, **ExistingEntry-PASS.json**, **PairedViews-PASS.json** under Logs/ArenaPolish.

### Files, limitations and next step

Modified **Assets/Scenes/OlympicMatchArena.unity**, **plan.md**, **README.md**, **docs/WATER_VISUALS.md**. Added **Assets/Editor/ArenaVenuePolishSetup.cs**, **Assets/Scripts/WaterVisuals/ArenaEventDisplay.cs**, venue prefab/materials/meshes/textures/metas, pre-polish recovery scene/meta and focused verification source. No gameplay, hub, input, HUD, economy, quality-pipeline assets, saved profile or original player/fan design changed.

This remains a coherent procedural venue, not a photoreal architectural replacement. Benches, coolers, terminals, seating and audience retain placeholder geometry; banners are rigid and jumbotron content is text rather than video. Broadcast prioritizes play; upper architecture is best inspected with F8/Scene view. Device performance/touch and full-match feel remain manual tests. Next recommended milestone **after separate authorization**: Android venue profiling and selective authored architecture/bench materials/meshes while retaining the proven field/camera and current fan/player scope. No next milestone was started.

## Previous handoff — unified frontend, Game Modes and four divisions

The developer explicitly authorized this **frontend UI/UX milestone** after accepting the asset-driven home. The home artwork/layout is retained; secondary presentation and navigation are now unified. This supersedes the older home-only scope below. Stop after this milestone. No Git write commands, downloads, replacement artwork, networking, Firebase, billing, advertising or new arena/gameplay systems were introduced. Existing unrelated local changes/untracked files remain intact.

### Saved scene, commands and recovery

Production UI remains **Assets/Scenes/MainLobby.unity**, with its original GUID. Run **Tools > Water Polo > Main Lobby > Set Up Unified Frontend** outside Play Mode with intended scene edits saved. Setup creates the missing shell/import wiring once, then validates/opens the tuned existing scene without rebuilding it. **Tools > Water Polo > Main Lobby > Open MainLobby** opens it; **Tools > Water Polo > Play Hub Entry** starts normal Boot entry. The older **Set Up Asset Home Screen** command also preserves the configured home.

Exact pre-milestone scene recovery: **Assets/Scenes/Recovery/MainLobby_PreUnifiedFrontend.unity**, outside the build list. The earlier PreAssetHome recovery is also retained. These are scene/layout snapshots; scripts remain shared project code. SHA256 verification confirms **Boot, OlympicMatchArena, HubCatalog, EditorBuildSettings and every supplied PNG** are byte-identical to this milestone's baseline; scene/source asset GUIDs are preserved. No arena regeneration or primary PrototypePool routing was introduced.

### Unified navigation and hierarchy

`HubView` remains the **single navigation authority**. `Show` closes stale modals and pushes the previous page; `ShowSibling` changes Team tabs without adding history; `Back` closes the current modal first, otherwise pops the page history. Lobby clears history. Pending results load the Result route; closing Result acknowledges its saved receipt without granting rewards again. Input System Escape and gamepad East call the same Back path; Android Back's Escape mapping and physical touch still require device verification.

The cause of the prior inconsistency was `HubView.Render` re-enabling the legacy header/sidebar on secondary pages, coupled with Back always targeting Lobby. The production scene now removes those obsolete **Navigation/ProfileBar visual objects** and their Nav_* button paths. Their services and functional page actions remain. The old text-only four-mode and social-preview builders are removed. There is one Canvas, GraphicRaycaster, InputSystemUIInputModule, EventSystem and HubView; no second active UI controller.

Hierarchy: **LobbyCanvas / MainMenuBackdrop + FrontendBackdrop / SafeArea**. SafeArea retains `SafeAreaLayout`, `HubView`, `GamePreferences`, existing **MainMenuHome**, reusable **Page** content, **UnifiedFrontend/Header**, Feedback and the last-sibling blocking Modal. `FrontendShell` owns presentation references only: shared top-left X, title, profile summary and actual coin/diamond balances. Navy surfaces, cyan accents and common panel/button factories wrap functional secondary content. `FrontendControls.Back` supplies reusable large X controls for the shell and dialogs. `MenuButtonFeedback` scales artwork/labels only, preserving hit regions; Unity Button colors supply hover/selected/press states. PNG lettering is never duplicated.

| Route | Function retained / return |
| --- | --- |
| Home PLAY | New six-card **Game Mode** page; X → home. |
| Unlocked division / Training | Details page; X/Escape → Game Modes; launch → OlympicMatchArena. |
| Locked division | Clickable explanatory modal naming the required preceding championship; X/Escape → unchanged modes page. |
| Shop | Six existing local offer tabs, real confirmations, currency spend and inventory grants. X → home. |
| Team | Lineup & Tactics / Collection tabs; role-valid GK + six fields, Press/Zone, owned athletes, player details and existing upgrades. Details X → Collection; Team X → home. |
| Ranking | Actual local trophies/rank/counters and achievements; no online leaderboard. |
| Missions | Existing six achievements, real incomplete/claimable/claimed states and once-only coin claims. |
| Battle Pass | Existing Season XP/free rewards in the purple page, scrollable tiers, distinct free/premium tracks; premium locked preview only. |
| Profile / Settings | Existing editable name/avatar/palette and local volume/preferences; profile uses supplied default artwork in place of the procedural portrait. |
| Packs / confirmations | Original transaction/timer/odds/reveal actions, modal X/Cancel and previous page retained. Empty slots now say “Earn in matches.” |
| Friends / Clubs / Messages / World Tournament / Lucky Shot | Informational Coming Soon modal; no fake service or rewards. World Tournament explicitly describes future country competition, separate from World Champions League. |

Home still uses its 21 original sprites, open centre, top bar, four real pack slots and large PLAY. No owned pack, balance, timer or profile was reset. Secondary page content remains deliberately simpler than final illustrated artwork, using the same header, navy foundation and buttons. Athlete placeholders reuse profile_default; no new character illustration/rig was generated.

### Imported Game Mode / Battle Pass artwork

Inspected **docs/UIReferences/reference_game_mode_layout.png** and all eight runtime PNGs. **Assets/UI/MainMenu/FrontendArt.asset** holds serialized Sprite references: `mode_division_1`, `mode_premium_league`, `mode_continental_cup`, `mode_world_champions_league`, `mode_lucky_shot`, `mode_training`, `bg_game_modes`, `bg_battle_pass_purple`. Zero missing sprites; no external assets/licenses were added. Source files are developer-provided.

Actual card sizes: Division 1 1115×1410, Premium 951×1653, Continental 1331×1181, World Champions 1331×1182, Lucky Shot 1372×1147, Training 1254×1254. Backgrounds: modes 1672×941, purple pass 1846×852. These eight sources are **opaque**, including their baked corner/background pixels; source pixels were preserved, not destructively cropped or made transparent. Importers: Sprite Single, Full Rect, source alpha retained, sRGB, Clamp/Bilinear, no mip/read-write/NPOT rescaling, cards max1024/backgrounds2048, Desktop BC7, Android/iOS ASTC4×4.

The mode composition uses two large tall cards on the left and a 2×2 group on the right, matching the reference's relative roles. Normalized safe-area anchors and `Image.preserveAspect` fit source proportions at all three tested ratios. Lock badges occupy the upper artwork region, leaving baked titles visible. Selected/hover/press tint and bounded scale feedback remain lightweight. Supplied background fills by cover aspect fitting; purple is selected **only for Battle Pass**. No added energy meter, flags or fake currency. Flags already present inside supplied card/background artwork were preserved as source pixels.

### Four-division offline foundation and save compatibility

Static configuration: **Assets/Resources/DivisionCatalog.asset**, separate from HubCatalog and save state. Required stable order: **division_1 → premium_league → continental_cup → world_champions_league**. Initially only Division 1 is unlocked. Default format **two_wins_in_three_v1**: two victories within at most three completed official matches. Draws/losses consume attempts; an attempt ends early when the win target is unreachable. A failed/completed attempt can be restarted. One current championship attempt is retained; starting a different division explicitly confirms replacing an unfinished attempt. Completed unlocks/inventory remain.

`HubProgression` plus its `HubDivisionProgression` partial is still the sole transaction authority. An official session stores a stable division ID, championship UUID and next round ordinal. `HubMatchBridge` only submits at authoritative FullTime; it retains division context when restarting the arena. Result processing verifies the active session/run/round, updates wins/matches, records the completion ID and grants the **existing per-match economy** in the same atomic transaction. Only reaching the configured win target marks a division complete and opens the next. There is no extra unlock bonus to replay. Existing `match:<sessionId>` keys prevent duplicate result grants/progress. Replay of an already won division retains ordinary match rewards; unlock IDs remain unique.

This is a small offline championship, **not a simulated club season**: the current opponent roster/AI and match rules are reused at every tier, without scheduling, different club rosters or tier-specific difficulty. Training is the existing playable four-period practice match with movement/passing/shooting and **zero coins, XP, packs, trophies or league advancement**. Lucky Shot is deferred to avoid changing match mechanics in this UI milestone. World Tournament is a separate country-based future feature; Premium League is a progression name, not a purchase entitlement.

Save schema is now **2**, at the existing **profile-v1.json** location. `LocalProfileCodec` explicitly migrates schema1→2 in memory after base validation, initializes only empty division history/current attempt, and preserves economy, collection, lineup, transactions, claims, pack timers, active Quick/Training session and pending receipts. Loading does not rewrite files. The next successful atomic save retains the schema-1 primary as `.bak`. Fully empty inline session/result/championship records normalize to null; non-empty records are retained. Unknown future schemas are refused without silently loading an older backup. The current format ID/config must remain compatible with in-progress attempts; changing a shipped format requires retaining its definition or an explicit migration. Do not casually retune wins/matches under an unchanged format ID after release. These are trusted local saves, not anti-cheat or server proof; future cloud synchronization must validate compact IDs/receipts server-side (see backend document).

### Files added / modified in this milestone

- New runtime: `DivisionCatalog.cs`, `HubDivisionProgression.cs`, `FrontendArt.cs`, `FrontendShell.cs`, `FrontendControls.cs`, `HubModeViews.cs` under **Assets/Scripts/Hub/**.
- Changed runtime: `HubView`, `HubWidgets`, `HubCollectionViews`, `HubRewardViews`, `HubApplication`, `HubMatchBridge`, `HubProgression`, `LocalProfile`, `LocalProfileCodec`, `JsonProfileRepository`, `MainMenuPackSlot`.
- Setup/assets: **Assets/Editor/UnifiedFrontendSetup.cs**, small empty-slot copy change in MainMenuSetup, FrontendArt/DivisionCatalog assets, eight new sprite importer settings, saved MainLobby and exact recovery scene, associated Unity metas.
- Verification: **Tools/Verification/FrontendVerification.cs**; reports/captures in ignored **Logs/Frontend/**. Documentation: plan, README, AGENTS and docs/FUTURE_BACKEND_AND_ECONOMY.md.

### Fresh focused verification and limitations

Windows Unity **6000.6.7f1 / URP / D3D12**, actual advancing Game view, using Pipeline **script run** for asynchronous checks. No historical full-game suites repeated. Fixtures use isolated memory repositories or unique disk folders under Logs; user profile hashes were checked unchanged.

| Check | Actual result |
| --- | --- |
| Setup | PASS: eight new serialized sprites, same scene/source GUIDs, exact recovery copy, untouched Boot/arena/catalog/build/PNG bytes, one Canvas/EventSystem/controller, required CanvasRenderers, no missing scripts, repeat-safe saved setup, Boot entry and registered OlympicMatchArena. |
| Progression | PASS: initial locks/order, reject locked launch and unrelated result, persist in-progress attempt, official win threshold, duplicate result refusal, Training/abandonment exclusions, failed attempt retry, all four IDs/unlocks through reload, atomic failure, schema1 migration including old session/pending result, original backup retention and future-schema refusal. |
| Pointer/keyboard transactions | PASS: six actual card images, press feedback, locked explanation, nested X/Escape, modal blocking/close, every home route, Team tabs/detail history/GK assignment/Press tactic, actual Shop purchase, Season/Mission claim-once, profile save, four real pack states, timer update, unlock/claim/reload. |
| Collection upgrade / scroll | PASS additional focused real pointer check: upgrade confirmation/transaction, vertical swipe to the final collection card, disabled upgrade for unowned athlete and correct nested Back. |
| Final details smoke | PASS after final label refinement: Training is identified as practice under MODE DETAILS, Division details retain their format, both nested pointer returns reach home; final home/Training/Division Game views captured and inspected. |
| Landscape | PASS actual **1920×1080, 2340×1080, 2400×1080**, simulated70px horizontal/24px vertical insets: six cards/header inside safe bounds with no overlap, retained aspect, all page X controls usable, header text fits, actual screenshots of home/modes/details/secondary pages/dialogs, horizontal Battle Pass swipe, one Input System simulated touch close. |
| Full loop | PASS two controlled actual arena matches launched through home→mode→details: 14 athletes and lineup mapping, physical goal each, four accelerated periods with one halftime switch, official restart context, once-only bridge receipts/rewards, reward/Continue returns, Premium unlock visible/persisted. A real Training completion grants no economy/league progress and returns home. |
| Console/compile | Source compiles; current Console ground truth after fresh runtime checks has no errors/warnings. Recompilation retains an existing **UAC0009 DEVELOPMENT_BUILD deprecation warning in ArenaDeveloperCamera.cs**; that arena file was not changed. Historical Pipeline buffers include older unrelated entries, not current runtime failures. |

Actual captures: **Logs/Frontend/GameModes-{1920,2340,2400}.png**, **SeasonPass-*.png**, **BattlePass-Scrolled-*.png**, **Home-*.png**, other page names by width, **Official-Arena.png**, **Official-Result-1/2.png**, **Premium-Unlocked.png**, **Final-Training-Details.png**, **Final-Division-Details.png** and **Final-Home.png**. These are actual Unity Game-view screenshots. Test input-focus settings were temporarily relaxed inside fixtures and restored; no production input-setting change. Device Back/touch, phone performance, memory/texture quality and real full-length matches remain manual/device testing. Existing collection stat/cosmetic limits and offline device-clock pack timing remain unchanged. Source/docs whitespace checks are clean; Git's full diff check reports Unity-serialized trailing spaces on empty YAML values, which were not hand-edited.

Manual review: Play Hub Entry; browse every page and X/Escape, swipe the season track, open Team Collection/details, test an affordable Shop confirmation and claimable reward, then PLAY→Division1→Start Championship. Win two official matches to see Premium unlock; quit/relaunch to check persistence. Test Training without rewards. Stop here; no further gameplay/backend milestone is authorized.

## Previous handoff — asset-driven mobile MainLobby implemented and verified

This is the developer's explicitly authorized **home-screen visual replacement**, using their existing 21 PNGs. It does not redesign the arena, athletes, backend, economy or all secondary pages. Stop at this completed scope. All changes are **uncommitted**; Git was used read-only. Pre-existing Art files, Pipeline settings, `old-plan.md` and `tatus` were preserved. No downloaded/replacement artwork, network SDK, fake inventory or new economy was added.

### Scene, commands and recovery

**Assets/Scenes/MainLobby.unity** is configured and saved with its original scene GUID and existing single Canvas/EventSystem/InputSystemUIInputModule. Normal startup remains **Boot → MainLobby → PLAY → Quick Match → OlympicMatchArena → rewards → MainLobby**. **OlympicMatchArena, Boot, HubCatalog and EditorBuildSettings bytes are unchanged from this session's baseline**, verified by SHA256. The original lobby has an exact independent recovery copy at **Assets/Scenes/Recovery/MainLobby_PreAssetHome.unity**, outside the build list. No existing scene was overwritten with a newly generated scene.

Use **Tools > Water Polo > Main Lobby > Open MainLobby** to open the saved home, or **Set Up Asset Home Screen** in the same submenu. Setup refuses unsaved scenes/Play Mode, configures missing content once and opens an already configured lobby without rebuilding tuned layout/imports or duplicating objects. Normal Play entry is **Boot**; **Tools > Water Polo > Play Hub Entry** runs the complete startup. Source builder: **Assets/Editor/MainMenuSetup.cs**. Mobile autorotation now permits both landscape directions and disables both portrait orientations; only those two PlayerSettings flags changed. This is persisted configuration, not a device-rotation test.

### Assets and importer settings

All **21 supplied PNGs** under **Assets/Art/UI/MainMenu/** were inspected for dimensions, alpha/padding and appearance. There are **zero missing sprites**. Catalog **Assets/UI/MainMenu/MainMenuArt.asset** holds serialized references directly to their imported Sprite subassets; there are no scattered art Resources.Load calls.

| Group | Assets and use |
| --- | --- |
| Buttons (9) | btn_ranking, btn_shop, btn_team on left; btn_friends, btn_clubs, btn_world_tournament on right; btn_battle_pass/btn_missions bottom left; btn_play bottom right. |
| Icons (5) | icon_coin/icon_diamond with actual balances; icon_plus for each shop shortcut; icon_settings/icon_messages beside profile. |
| Cards (4) | card_common/card_rare/card_epic/card_legendary selected from each stored pack's rarity; never displayed as owned when empty. |
| Brand / Profile / Background (3) | One small logo_mascot in the top bar, profile_default for the profile entry and bg_main_menu across the viewport. |

Source widths are **500–1842 px**, heights **408–1273 px**; background **1672×941**. Background is opaque; the other 20 have alpha. Some assets have excessive transparent padding, especially Common/Rare cards. Sprite metadata uses one Full Rect sprite per PNG (Sprite Mode Multiple) with a non-destructive visible-art rect: alpha≥64 bounds plus a safety margin. Source PNG hashes and original asset GUIDs are unchanged. Embedded PNG lettering/icons are not redrawn or duplicated. **World Tournament's actual supplied image is a trophy without lettering**, so a small caption sits below its art, outside the button image.

Import: Sprite (2D/UI), source alpha/transparency for non-background images, sRGB, no mipmaps/read-write/NPOT rescaling, Clamp/Bilinear, no fallback physics shapes. Max size: buttons 1024, icons/cards/profile/mascot 512, background 2048. Desktop BC7; Android/iOS ASTC 4×4 overrides. These are sensible UI budgets; mobile decoding/memory/image quality were not profiled on devices. Repeat setup preserves authored Sprite/layout tuning. When replacing artwork later, review its Sprite rect; do not destructively crop original PNGs.

### Hierarchy and responsive home layout

The existing **LobbyCanvas** retains its CanvasScaler (1920×1080, Scale With Screen Size, match=.5), GraphicRaycaster and one EventSystem. **MainMenuBackdrop** sits under SafeArea: supplied image with cover aspect fitting plus restrained navy tint. Existing **SafeArea** retains SafeAreaLayout, HubView and GamePreferences. It now contains **MainMenuHome**, existing Feedback and the existing Modal drawn last.

**MainMenuHome** contains **TopBar** (profile/name/live level and fractional XP, Settings/Messages, small brand, diamonds/coins and plus controls), **LeftNavigation**, **RightNavigation**, **BottomLeft**, **RewardPackSlots/PackSlot0–3**, and **Play**. Home occupies normalized safe-area bounds: top bar ~13% height; left/right rails ~17%/19% width; bottom pack row ~36.5% width; PLAY ~24.7% width. The centre is intentionally clear—no player model, giant portrait/logo or additional banner. There is no energy meter or added country-flag widget. Background banner details are the supplied image itself.

PNG Images preserve aspect inside generous independent hit rectangles. All interactive bounds fit simulated safe areas without overlapping. Larger PLAY art remains dominant; Battle Pass preserves its wide banner proportions. Common text uses TMP autosizing; pack state labels were enlarged after Game-view review. XP bar width is the actual XP fraction, including zero; it does not show a false full bar. Reusable factories build image buttons, currency groups and badges. Four reusable MainMenuPackSlot components and MenuButtonFeedback provide consistent behavior. Press feedback scales only artwork to .95, hover to 1.025, uses unscaled time, resets on cancel/disable and stops transform writes after settling. No per-frame home hierarchy rebuilds; pack text updates once a second through the existing hub timer.

The old ProfileBar/Navigation/Page objects remain available for secondary screens and are hidden on home. Opening another page hides home/backdrop and reveals the existing shell. Back/Escape returns to home. The original large procedural lobby portrait is never shown in the new home; existing Profile/Collection detail visuals remain on those secondary pages. The provided default profile icon stays fixed; name/cosmetic editing remains in the existing Profile system.

### Navigation and pack integration

`HubView.Show(...)` remains the central route API; its existing dialogs and pack actions are reused. `MainMenuHomeView` reads detached HubProgression snapshots and requests actions through HubView. **HubProgression, JsonProfileRepository, LocalProfileCodec, HubCatalog and all match scripts were not changed.**

| Home control | Destination |
| --- | --- |
| Ranking | Existing Trophies/local rank and achievements; no fake online ranking. |
| Shop | Existing Shop. |
| Team | Existing Tactics; Collection remains reachable in the preserved secondary navigation. |
| Battle Pass | Existing Season Pass. |
| Missions | Existing six achievement definitions/counters/claims, reused through a shared list; no new daily-mission backend. |
| Profile / Settings | Existing editable Profile / local Settings. |
| Diamond plus / Coin plus | Existing Shop Gems / Coins tabs; the premium-store preview remains unavailable. No purchase is triggered just by opening it. |
| PLAY | Existing mode selection with Quick Match/Training and preserved arena routing. |
| Friends / Clubs / Messages / World Tournament | Reusable informational popup with exact feature title, Coming Soon and Close; Escape closes it and underlying buttons are blocked. Original social preview pages remain available in the old secondary navigation. |

Four slots render the real compact pack list: **Empty** shows no card image and cannot claim; **Waiting** opens the existing unlock confirmation/odds; **Unlocking** displays service.Remaining countdown; **Ready** opens the existing once-only claim/reveal. Actions pass stable pack IDs back to the original PackPanel/StartPack/ClaimPack flow. Inventory removal, grants, timers, recovery writes and deduplication stay authoritative. No pack instances are created by UI. Known stored rarities map to four supplied sprites; unknown rarities use a generic reward label, never fabricated rarity art. Current catalog normally awards Common Tide packs; the other sprites are prepared for actual stored rarities, not added to drop tables. Battle Pass/Missions badges show only real unclaimed eligible reward counts, hiding at zero.

### Fresh focused verification and screenshots

Windows **Unity 6000.6.7f1 / URP 17.6 / D3D12**, actual advancing Play Mode. Verification source **Tools/Verification/MainMenuVerification.cs**, using **script run** for Task-returning methods. No broad historical gameplay suite was repeated. Reports/captures: ignored **Logs/MainMenu/**. Inspection utility **Tools/Verification/InspectMainMenuAssets.ps1** writes a diagnostic contact sheet/alpha report outside Assets; it does not edit artwork.

| Check | Fresh result |
| --- | --- |
| Setup | PASS: 21 assigned Sprite references / zero missing; all source PNG bytes/GUIDs preserved; same MainLobby GUID; exact original recovery copy; arena/Boot/catalog/build list unchanged; one Canvas/EventSystem/GraphicRaycaster, required CanvasRenderers, no missing scripts, repeat-safe scene bytes. |
| Interactions | PASS real Input System mouse/key events: home routes, press scale, existing editable profile form plus pointer Save, live balances/XP, all four pack states, rarity mapping, countdown changes/completion, unlock/claim persistence, claim-once refusal, inventory reload, actual achievement badges, four popup titles/modal blocking/Close/Escape. |
| Layouts | PASS actual **1920×1080, 2340×1080, 2400×1080** (16:9/19.5:9/20:9), simulated 70px side / 24px vertical safe insets. All touch targets visible/in bounds, no control overlap, text fits, PNG proportions retained, pointer Shop/Back and popup close work at each size. Home and popup Game views inspected at all three ratios. |
| Match/rewards loop | PASS existing ArenaVerification.HubFlow once through the new PLAY button: Boot/lobby, actual Quick Match pointer, 14 roster/collection mappings, physical goal, 4 periods with one halftime reversal, one receipt, reward reload, pointer rewards/Continue return to new home; training abandonment grants nothing. Period completion is accelerated through existing live-time API; no full-length manual/device match claim. |
| Final feedback smoke | PASS after idle feedback optimization: real pointer scale/route, live profile/currency values, correct XP geometry. |
| Data safety | Interaction/pack/form tests and full match rewards used isolated in-memory profiles; production profile SHA256 unchanged during each fixture. Catalog/economy/save implementations and persisted user inventory were not reset. |
| Compilation/Console | Latest Editor/runtime source compiles; final current Console ground truth **0 errors / 0 warnings**. Historical Pipeline buffers retain resolved prior-session entries. |

Commands: `unity command script run --file Tools/Verification/MainMenuVerification.cs --entry MainMenuVerification.Setup --caller plugin --skill unity-pipeline --format json` in saved Edit Mode; **Interactions**, **Layouts**, **FeedbackSmoke** in Boot/lobby Play Mode. Use `--timeout 60` for Interactions, 45 for Layouts. The match-loop CLI result is **Logs/MainMenu/MatchFlow-cli.json**; its actual result/reward captures remain in Logs/Arena. Never return Task objects through synchronous eval.

Actual-profile home screenshots: **Logs/MainMenu/Home-1920.png, Home-2340.png, Home-2400.png**. Also **ComingSoon-1920/2340/2400.png**, **Shop-1920/2340/2400.png** and **Popup-FRIENDS/CLUBS/MESSAGES/WORLDTOURNAMENT.png**. **Fixture-Pack-States.png** deliberately uses isolated waiting/unlocking/ready/empty test records and test balances; it is not the developer's inventory. Final home screenshots use the unchanged real profile (one owned Common pack and three empty slots in this run).

### Files, limits and next step

New runtime classes under **Assets/Scripts/Hub/**: **MainMenuArt.cs, MainMenuHomeView.cs, MainMenuPackSlot.cs, MenuButtonFeedback.cs**. New **Assets/Editor/MainMenuSetup.cs**, **Assets/UI/MainMenu/MainMenuArt.asset**, recovery MainLobby scene and metas; **Tools/Verification/MainMenuVerification.cs, InspectMainMenuAssets.ps1**. Modified **MainLobby.unity**, the 21 imported PNG `.meta` settings, **HubView.cs** (home switching/routes/popup/events), **HubRewardViews.cs** (home timer adapter), **HubCollectionViews.cs** (shared achievement list for Missions), **ProjectSettings.asset** (two portrait flags), **AGENTS.md, README.md, plan.md**. No source PNG, arena, Boot, build-list, economy catalog, persistence or gameplay-source changes. Imported Art folders/files were pre-existing untracked developer work and remain present.

Remaining limits: no Android/iOS build, hardware touch/multitouch/rotation, safe-area hardware or performance profiling. Only the main home has the new art; secondary screens deliberately retain the existing functional style. Friends/Clubs/Messages/World Tournament and original online/career previews remain unavailable. Sprite text is baked English; localization would need appropriate supplied variants. Future secondary-screen polish can use the same art catalog/components under a separate explicit scope. No future milestone was started.

Editor is left outside Play with saved **MainLobby** open, **Boot** as Play entry and auto-tick enabled as found. Next action is developer visual/touch review: use Play Hub Entry, try navigation/Back and the Coming Soon close, optionally unlock/claim an owned pack and complete a Quick Match to review rewards/return. Actual phone touch/rotation/performance remains a future hardware check. Stop here.

## Current handoff — OlympicMatchArena is the verified production match destination

The developer explicitly authorized a major arena replacement based on **the complete WaterVisualTest appearance**, beyond the earlier additive water-only integration. This environment scope is complete in the Windows Editor; it is not a later gameplay/backend milestone. All changes remain **uncommitted**. No Git write operation or automatic checkpoint was performed. Existing unrelated local files/changes, including `old-plan.md`, `tatus`, Resources meta and Pipeline settings, were preserved. Read this handoff before the historical water/M12 acceptance notes below.

### Production scene, safety and entry

**Assets/Scenes/OlympicMatchArena.unity** is saved and registered. Normal Editor Play entry is **Boot**, with **MainLobby → PLAY → Quick Match → OlympicMatchArena → full-time rewards → MainLobby** freshly verified. `Assets/Resources/HubMatchDestination.asset` selects the arena; HubApplication retains PrototypePool as fallback when no destination asset exists. M12 hub setup now respects the selected destination and does not re-add PrototypePool to production builds.

**PrototypePool.unity and WaterVisualTest.unity are byte-identical to this session's start.** A separate exact recovery scene is **Assets/Scenes/Recovery/PrototypePool_PreArena.unity**. Original camera/gameplay tuning remains available in both old gameplay scenes. WaterVisualTest stays an independent graphics fixture. Old/test/recovery scenes are outside the production build list; existing SampleScene entry/config objects were preserved. No old scripts, prefabs or references were deleted. The new arena has one physical tiled shell/water surface; its retired original pool/floor/marking/deck render geometry was removed, rather than overlaid.

Menu paths:

- **Tools > Water Polo > Arena > Open Olympic Match Arena** — open saved production scene.
- **Tools > Water Polo > Arena > Set Up Olympic Match Arena** — create once; existing complete arena opens without rebuilding or changing tuning.
- **Tools > Water Polo > Arena > Play Olympic Match Arena Directly** — isolated match. Restore normal entry with **Tools > Water Polo > Use Boot as Editor Play Entry** or **Play Hub Entry**.
- **F8** in Editor/development builds cycles five inspection views then returns to the same adaptive broadcast camera.
- **Tools > Water Polo > Arena > Apply Configured Field Dimensions** — explicit generated-environment/formation rebuild using **Assets/Arena/Settings/CompetitionDimensions.asset**. Save intended edits and stop Play first. Prefab/material/profile and broadcast tuning/scene identity are preserved; reverify altered dimensions.
- **Activate Verified Arena for Lobby**, in the Arena submenu, requires the integration PASS report. It was used only after direct integration passed, then the real-pointer hub flow was checked.

### Confirmed visual and gameplay architecture

Inspection reports for both original scenes are in ignored **Logs/Arena/Inspect-*.json**. WaterVisualTest looked better because of the complete 2.05 m tiled shell, metre UVs, inward-facing walls, coping/waterline detail, warm directional daylight/trilight fill, recognizable hall reflections, Forward URP budgets, perspective and complete submerged bodies. The prior match's additive water change retained flat ambience, old decks and shallow anatomy.

The new scene copies the proven match authorities/references, then reconstructs that fixture architecture using its same ceramic/enamel/steel/daylight/water materials. It adds a wide non-slip tiled deck/drainage, clear referee paths, Blue/White benches/coaches/equipment, officials table, ten reusable six-tier seating blocks, railings, original bounded audience, tiled concourses, hall walls/windows/ceiling/trusses/emissive fixtures and original game/studio banners. No Olympic logos/event/team branding or extra third-party art was imported. One directional light; roof geometry avoids heavy shadow bands. Full architecture, source/licenses and replacement details: **docs/WATER_VISUALS.md**.

Default field is **25 × 20 m**, goals at **Z=±12.5, Y=0**. Physical water shell is **28.6 × 22.5 m**, depth **2.05 m**; deck **7 m wide**, top **Y=0.18**. PoolBounds, existing physical goal views/barriers, fourteen start markers/poses, six-role formations and referee offsets are configured together. Targets preserve role distances from current goals; no global athlete/ball scaling. Goal openings remain 3 × 0.9 m, depth 1 m, posts 0.08 m. World Aquatics February 2026 Part Six Articles 20/21 were inspected for field dimensions, 2/5/6 m markers, colored ropes, goal-area indicators and substitution/re-entry zones. This does not replace the existing simplified gameplay rule profile or add actual substitutes.

Referees remain presentation observers, moving at **X=±12.65, Y=0.18**, with benches/stands beyond their paths. MatchRules has an arena-only `useCompetitionReentryArea` option to park excluded bodies in water behind the **current defended goal**, keeping them out of the new deck/benches; halftime still uses authoritative goal references. Original scene default parking is unchanged. Decisions, clocks, foul history and collider ownership remain with existing systems.

All fourteen real athletes retain original identity/motor/AI/physics/stamina/selection/ball anchors. Their old mesh renderers are disabled; reusable visual-only field/keeper prefabs add opaque submerged anatomy at local Y=-0.42, team caps/suits and red keeper caps. Keeper arm-lift references point to the replacement pivots. `ArenaAthleteVisual` documents skin/cap/swimwear slots and supplies only temporary pose movement. Blender replacement must replace that child, preserve gameplay roots/anchors, use opaque depth-writing URP materials and animation-inclusive bounds, and replace the primitive pose adapter with an Animator. No heavyweight rig was generated. Collection ratings/cosmetics retain their prior lobby/collection scope.

PrototypeMatch/BallController/selection/tactical authority boundaries and ball (-140), keeper (-120), scoring (-100) ordering are unchanged. GameplayCamera remains the adaptive broadcast authority (arena base tuning 14.4 m height, 10.8 m distance, 53° pitch, 52° FOV), retaining ball/goal prediction, keeper/set-piece modes and preferences. F8 positions the same camera at viewpoints and returns through its snap API; there is one AudioListener.

### Quality budgets, effects and hub correction

ArenaQualityController observes existing GamePreferences and owns scene-only pipeline/water/audience/prop budgets, restoring the prior pipeline on unload. Low: 0.85 scale/2× MSAA, depth/normals/baked 128px reflection, no refraction/caustics/planar/audience/fine props. Medium: 1.0/2×, half opaque copy/refraction/caustics/baked reflection, 160 audience. High: 1.0/4×, full opaque copy, 0.3-scale planar reflection, 320 audience, 2048 directional shadows. Existing preference policy keeps camera shadows off below High. All preserve visible submerged bodies/floor; no ocean displacement or basic blue replacement water. High still has a second render and upstream CPU/allocation costs. Android/iOS performance is **not measured**.

Existing wakes/ripples remain bounded; normal water contacts now activate the existing splash mesh too (capacity 16). Original generated eight-second mono crowd ambience is assigned, looped by MatchAtmosphere and controlled by the existing crowd slider. Static floats/seating/rails/props/roof/audience are combined by material; audience uses 54-vertex ellipsoids. Final Edit Mode inventory: **456 renderer components / 166,801 referenced mesh vertices**, including disabled/repeated references; about 6.3 MB arena source assets. These are not runtime memory/draw-call/device measurements.

The first real hub run exposed a pre-existing M12 blocker: Unity serializes null nested `activeMatch`/`pendingResult` records as empty objects, and Snapshot's JSON copy reopened a blank Result page instead of PLAY. **LocalProfileCodec** normalizes only fully empty optional records on copy/load. Schema 1, transaction authority, atomic replacement/recovery, receipt keys, reward values, catalog and existing saves are preserved. No default progress reset or migration was added. Fresh core/recovery checks and the actual launch/result loop passed after this correction. See **docs/FUTURE_BACKEND_AND_ECONOMY.md**.

### Fresh verification — Windows Unity 6000.6.7f1 / URP 17.6 / D3D12

New focused script: **Tools/Verification/ArenaVerification.cs**. All Task entries use **script run**, never synchronous eval. Reports/screenshots: ignored **Logs/Arena/**. Historical suites were not repeatedly rerun; Hub Core/Navigation were run once after the actual optional-record correction. Goal fixtures deliberately clear lanes/disable keepers; keeper resolution has its own controlled check.

| Check | Fresh result |
| --- | --- |
| Independent fixture first | WaterVisualVerification.Rendering PASS, frame 4512→4932; three quality variants, animation/effect A/B and wholly submerged anatomy visible. Original fixture retained. |
| Setup | PASS final: original bytes and exact recovery, 14 distinct original IDs/anchors, 6 fields + 1 keeper/team, single water, no demo swimmers/scenery colliders/missing scripts, one Canvas/EventSystem, CanvasRenderers and repeat-safe scene GUID/bytes. |
| Arena Integration | PASS, frame 246→2339: AI bounds/referee paths, 3 water variants; actual W/Shift/C; pointer joystick/Pause/charged Shoot; physical pass reception, airborne lob, goal/conceding restart; exclusion/waiting water area/stopped rule clocks; 4 periods/one halftime reversal. |
| Appearance | PASS final: actual opposite attacking goals after halftime at viewport Y≈0.687 / 0.283; six fully submerged real-athlete meshes contribute **2,996 pixels**, with shadow contributions excluded. Final Game views inspected. |
| ScreenSizes | PASS: actual 1920/2340/2400 ×1080 plus simulated safe insets. Controls in bounds, pointer Pause/Settings at every size; real graphics preset clicks switch pipeline/water/crowd/reflections; injected F8 works. Images inspected. |
| WaterImpact | PASS: physical lob landing emits water contact and active pooled splash, capacity 16. |
| Keepers | PASS both relocated goals: physical swept catch, one owner/no score, human Blue keeper selection. Test probabilities=1/reaction=0 and 80 m/s shot; original tuning restored. Not a natural save-balance claim. |
| HubFlow | PASS Boot→lobby→real PLAY/Quick Match pointer→arena; 14 collection mappings, physical goal/4 periods/one reversal, one receipt, reload, real rewards/lobby pointers; training abandonment no reward/stuck pause. Isolated profile; production profile hash unchanged during fixture. |
| Hub Core | PASS 18 economy/persistence/recovery groups, including once-only claims/failed-write rollback/corrupt recovery. No economy values changed. |
| M12 Navigation | PASS fresh: real pointer Shop, 12 populated pages, keyboard Back, profile identity stable. Full historical 36-page layout matrix not repeated. |
| Compilation/Console | Current compilation passes, final ground truth zero errors/warnings. Historical Pipeline buffers retain resolved earlier messages. |

Game-view PNGs: **Final-Arena-Overview.png, Final-Broadcast.png, Final-Reflections.png, Final-Team-Benches.png, Real-Athlete-Underwater.png, Verified-North-Attack.png, Verified-South-Attack.png, Screen-1920-Broadcast.png, Screen-2340-Settings.png, Full-Time.png, Rewards.png, Returned-Lobby.png**. Earlier First-* captures predate refinements. Actual match/underwater/reflection/both-end/overview, screen-size broadcast/settings and full-time/reward captures were visually inspected.

### Changed files, limits and next step

New: **Assets/Editor/OlympicArenaSetup.cs**; **Assets/Scenes/OlympicMatchArena.unity**, **Scenes/Recovery/PrototypePool_PreArena.unity**; **Assets/Arena/** generated prefabs/meshes/materials/pipelines/profiles/reflection/audio/dimensions; **ArenaDimensions, ArenaQualityController, ArenaDeveloperCamera, ArenaAthleteVisual, ArenaHudReadability** under Scripts/WaterVisuals; **HubMatchDestination, LocalProfileCodec** under Scripts/Hub; **Resources/HubMatchDestination.asset**; **Tools/Verification/ArenaInspection.cs, ArenaVerification.cs**, associated Unity metas. Modified: **Milestone12HubSetup.cs, HubApplication.cs, HubProgression.cs, JsonProfileRepository.cs, MatchRules.cs, MatchAtmosphere.cs, WaterInteractionEffects.cs, EditorBuildSettings.asset, README.md, docs/WATER_VISUALS.md, docs/FUTURE_BACKEND_AND_ECONOMY.md, plan.md**. Unity also normalized `URPProjectSettings.asset` by adding its default project-setting-folder field; original pipeline assets/QualitySettings/packages/Editor version are unchanged. Prior-session water files/PrototypePool changes remain as found; this session did not modify their scene bytes.

Limitations: no Android/iOS build/device profiling/real multitouch, no Blender import, no detailed athlete/audience/substitute animation or complete substitution/rule implementation. Screen-space refraction/caustic/transparent-object/underwater-camera and High planar-reflection limitations remain. No claim that every historical gameplay edge or every hub page/resolution is certified. Existing backend/billing/ads/social previews are unchanged; no online services added. All original third-party notices remain, no new third-party art licenses required.

Editor is left outside Play with saved OlympicMatchArena open and Boot as normal Play entry; auto-tick remains enabled as found. Evidence/references/recovery working files outside Assets stay in ignored Logs/Arena and Temp/ArenaRecovery/Temp/ArenaReference. Next step is developer visual review using normal PLAY or F8; later mobile profiling/Blender character work requires its own scope. No later milestone is started.

## Current handoff — transparent pool water verified and applied additively to PrototypePool

The developer explicitly authorized this isolated major water-graphics investigation/integration alongside the existing M12 boundary. This is **not M13**, and does not complete the pending M12 hub acceptance below. No Git write operation was performed. Existing `old-plan.md`, `tatus`, Resources meta and Pipeline settings were preserved. The pre-existing `EditorBuildSettings.asset` working change was not overwritten.

### Delivered graphics and scene isolation

**Assets/Scenes/WaterVisualTest.unity** is saved and configured: 14m × 25m tiled competition pool, real floor at Y=-2.05m, inward-facing tiled walls, lane markings/coping/waterline band, reflection landmarks and three opaque test swimmers (surface, upright partly submerged, fully submerged). The 112-transform fixture has no gameplay colliders or match authorities. It is absent from the game build list and Boot/lobby navigation. **After this isolated water passed verification, the presentation was applied additively to PrototypePool**, preserving its existing gameplay components and physics.

Use **Tools > Water Polo > Water Visuals > Set Up WaterVisualTest**, outside Play on saved scenes. Repeating setup opens the existing scene and preserves GUID/layout/material/profile tuning. **Tools > Water Polo > Water Visuals > Play WaterVisualTest** temporarily bypasses Boot for this scene and restores the exact prior Editor entry on stop (the developer had direct entry/null at this session's start). Keys **1/2/3** select quality; **V** cycles anatomy detail/overview/reflection angle. No new UI/Canvas/EventSystem or pointer controls.

`PoolWaterVisuals` is presentation only, owns a runtime material copy, explicitly requests camera depth/opaque textures and applies/restores a test-only pipeline override. Dedicated Forward renderer/pipelines and three materials/profiles live under **Assets/WaterVisuals/**; gameplay PC/Mobile assets and Quality settings are untouched. Slow animated ripple normals, depth tint, Fresnel reflection, restrained refraction and animated parallax caustics keep opaque submerged limbs and tiles visible. Ocean displacement, shoreline/foam and underwater foam are off. A baked 128px hall reflection is assigned; High can also render a bounded-resolution planar reflection.

| Quality | Configuration / scope |
| --- | --- |
| Low | Depth, ripple normals, baked reflection; 0.85 render scale, 2× MSAA; no opaque copy/refraction/caustics/second camera. |
| Balanced | Depth, half-resolution opaque copy/refraction, caustics, baked reflection; 1.0 scale, 2× MSAA, no second camera. |
| High | Full-resolution opaque copy, refraction/caustics, 4× MSAA and 0.4-scale planar reflection every frame; Editor visual reference. |

These are editable mobile-budget candidates, **not hardware performance results**. The table's render scale/MSAA/pipeline overrides apply to the fixture only. Runtime test-quality changes never edit the persistent profile/material assets; no per-frame content rebuilds in the new controller/rigs. Upstream planar reflection still has a second render and CPU/allocation costs in fixture High only.

**PrototypePool integration:** **Tools > Water Polo > Water Visuals > Apply Verified Water to PrototypePool** is saved-scene/Edit-Mode-only and repeat-safe. It adds `Environment/Verified Pool Water Presentation`: transparent surface at the authoritative PoolBounds waterline, tiled floor/walls at 2.05m depth, floor copies of the three existing cross-pool lines, a baked actual-match reflection and presentation/preference observers. Original WaterSurface, PoolFloor and three shallow line renderers are retained but hidden. Only those five original renderer enabled flags and Environment's new child reference change; no original components are removed. Normalized YAML comparison confirmed **all original MonoBehaviours, athlete transforms, rigidbodies and colliders unchanged** (43 additive YAML objects).

`PoolWaterPreferenceBridge` observes the existing GamePreferences.Changed event and maps Low/Medium/High to the three water material variants, without adding UI or intercepting gameplay input. Match use of `PoolWaterVisuals` has **useIsolatedTestPipeline=false**, keyboard shortcuts off, and no planar manager/volume; even High uses the baked match cubemap and the original single GameplayCamera. Depth/opaque texture camera overrides make refraction work on the original Mobile pipeline without changing its global asset/settings. The existing match retains its own render scale/MSAA. To inspect the prior presentation manually, disable the new presentation root and re-enable the five retained original renderers; do not alter physics.

### Imported source and licensing

GitHub download/import succeeded through approved network access after the initial sandbox connection failure. Imported source revision **1fdd16c6312611fef7fb248dee86cdd3fd456a1e** (2026-10-04), confirmed by archive comment. ZIP SHA256 **9473DC7AF3BF8D864384A854A8221D0A0689BB77EFFADF77564866BEC585CD22**. Source: https://github.com/MatrixRex/Uber-Stylized-Water . Upstream supports Unity 6000.0.30+ / URP / Shader Graph 17.0.3+; current **6000.6.7f1 / URP 17.6.0** retained.

Selected prebuilt shader, CubeMap/CustomLighting includes, material Inspector, planar scripts, Normal 1/2, Noise 1, Caustic 1 and Guide imported under **Assets/Shaders/Uber Stylized Water/** with original `.meta` identities. No demo/project settings/package manifest, RexTools, source graph or ShaderGraphVariables extension was installed; no global graph variant limits were raised. `UPSTREAM.md` records provenance/subset/local changes. `UPSTREAM-CREDITS.md` preserves original special thanks/source links.

**MIT Copyright (c) 2025 MatrixRex** and dependency **MIT Copyright (c) 2020 Cyanilux** are preserved in the vendor folder and **docs/Licenses/**. Full license copies and attribution/credits are also under **Assets/StreamingAssets/ThirdPartyNotices/** so player distributions retain notices. Upstream's cited Material Maker/Calinou caustic sources are CC0; another optional caustic source is CC-BY-SA 3.0. **Optional Caustic 2/3 maps are excluded**; do not assume every linked upstream art source is MIT.

Only reflection C# needed local compatibility fixes: Unity 6.6 GetEntityId/unsorted FindObjectsByType, explicit SingleCameraRequest destination required by URP 17.6, and try/finally restoration of temporary render state. The generated shader and HLSL includes remain unchanged. Follow **docs/WATER_VISUALS.md** for exact installation/reinstall links, properties, profile costs, commands and future integration.

### Fresh verification — focused Windows Editor runs

**Tools/Verification/WaterVisualVerification.cs**, always via **script run** for Task methods. No repeated historical verification suites. Actual Game view PNGs and machine results: ignored **Logs/WaterVisuals/**.

| Entry / evidence | Fresh result |
| --- | --- |
| Setup | PASS: repeat-safe 112 transforms, same scene GUID/profile tuning, three test swimmers, zero colliders, assigned baked cubemap, PrototypePool bytes retained. |
| Rendering | PASS D3D12: advancing frame 388 → 950, real Input System 1/2/3/V events, supported/rendered Low/Balanced/High shader variants, no shader errors. |
| Submersion A/B | PASS: only wholly underwater meshes hidden, with shadows excluded as a false-positive source. Upright/surface/submerged rigs contribute 3,056 / 2,091 / 1,793 visible pixels (6 / 5 / 8 meshes). |
| Effect A/B | PASS: water 330,568 changed pixels; refraction distortion 46,213; caustics 331,136; reflections 166,788. Frozen character poses plus advancing water change 38,080 pixels. Visibility evidence, not quality/performance scores. |
| Actual Game view | High detail/overview/reflection-angle captures `Final-0/1/2.png`, all three quality captures, dry/hidden-mesh/effect comparisons. High detail/reflection angle, Low/Balanced and dry baseline visually inspected. Approximate current Game view 1031×518; no phone-layout claim. |
| GameplaySetup | PASS after additive integration: 301 transforms, fourteen athletes, no new colliders/planar manager, repeated setup leaves saved scene byte-identical and preserves tuning. |
| GameplayPresentation | PASS after integration: frames 29,329 → 29,818, Low/Medium/High via existing GamePreferences API, rendered water variants and original Mobile_RPAsset throughout; one camera, fourteen athletes/one ball, real injected W movement, authoritative collection and airborne physical shot. User preference record restored. `Match-Quality-0/1/2.png` and `Match-Final.png`; High/final actual match views visually inspected with the existing HUD/controls. Focused smoke, not a historical regression-suite claim. |
| Console/compilation | Final ground truth 0 errors / 0 warnings, compilationFailed false. Historical Pipeline buffer retains earlier resolved import messages. |
| File preservation | Prior to applying presentation, PrototypePool was byte-identical to session start. Final SampleScene, PC_RPAsset, Mobile_RPAsset, QualitySettings and EditorBuildSettings remain byte-identical to session start; final PrototypePool has only the additive presentation/renderer changes detailed above. No existing gameplay scripts changed. |

### Limitations, Blender and next step

Clear pool appearance is established in the isolated Editor fixture and the actual match Game view. Screen-space refraction/parallax caustics are approximations, not volumetric water or physical light transport. Underwater cameras/meniscus, overlapping transparent hair/particles, split-screen/XR/camera stacking, multiple water heights and seamless additive scene overlap remain unverified. No Android/iOS build, device profiling or Blender/model-generation session. Existing placeholder gameplay swimmers lack full submerged anatomy; a shader cannot create it.

Future Blender athletes: closed full bodies in metres with actual arms/legs, valid normals/tangents and animation-inclusive skinned bounds, opaque URP Lit skin/suit depth passes before transparent queue 3000. First replace only the fixture's visual child; test animated limbs crossing Y=0 and distant broadcast readability. Transparent hair/eyewear need separate rendering review. Keep gameplay identity, collider/motor, release poses, waterline, ball/selection/match authorities unchanged. Detailed handoff: **docs/WATER_VISUALS.md**.

New files: **Assets/Editor/WaterVisualTestSetup.cs, PoolWaterGameplaySetup.cs**; five runtime classes under **Assets/Scripts/WaterVisuals/**; **Assets/WaterVisuals/** generated materials/textures/cubemaps/meshes/pipelines/profiles; **WaterVisualTest.unity**; selected vendor assets; **StreamingAssets/ThirdPartyNotices/**; **Tools/Verification/WaterVisualVerification.cs**; **docs/WATER_VISUALS.md**, **docs/Licenses/** and associated metas. Modified **PrototypePool.unity** presentation and **plan.md**. Editor left outside Play with saved PrototypePool open, original PC pipeline and previous direct-scene/null Play entry restored; auto-tick remains enabled as found. Temporary downloads/source and the pre-integration scene copy are in ignored Temp/WaterImport; evidence is outside Assets in ignored Logs/WaterVisuals. Next step is developer visual review and eventual Android profiling/Blender visual rig integration. No later milestone is authorized; M12 runtime acceptance remains pending below.

## Current handoff — M12 offline hub implemented; runtime acceptance pending

The developer explicitly authorized Milestone 12, superseding the older M11 stop instruction. Stop after M12. All changes are **unstaged and uncommitted**. Human-only Git writes remain permanent. Pre-existing `old-plan.md`, `tatus` and `ProjectSettings/Packages/com.unity.pipeline/` are untouched.

### Delivery status and critical verification boundary

The offline hub source, catalog, Boot/MainLobby scenes and additive match wiring exist. Unity **6000.6.7f1** compiled the first complete implementation without errors; focused Core and repeat-safe Setup checks passed in the connected Editor. **Do not call M12 fully verified or visually accepted yet.** The first runtime navigation test was mistakenly returned through synchronous `script eval`; Pipeline serialized its Task.Result on the main thread and stalled Unity. The saved scenes were clean before Play. That test Editor was stopped to recover; subsequent sandbox launches could not finish licensing/package-manager startup. Scene recovery dialogs were handled with **Keep Backups**, and failed recovery launches were closed. The developer was asked to reopen through Unity Hub. No unsaved intended Edit Mode changes were discarded.

Latest runtime and Editor C# sources, including subsequent save-validation/retry/typography corrections, compile with the pinned Editor's Roslyn compiler and the project's generated reference/define response files, writing only to ignored `Logs/M12/`. **This is compilation, not a fresh Unity runtime test of those corrections.** The first Core pass predates the final deep-content backup validation/schema-refusal changes. No successful Game view capture, completed pointer test, match-loop run or gameplay regression is claimed for M12. Prior M11 evidence below remains historical.

### Scenes and entry flow

- **Assets/Scenes/Boot.unity**: saved loading Canvas, aquatic geometric theme, title/ToduStudios, animated indicator and status. Loads preferences and local catalog/profile; transitions asynchronously with no artificial long delay. Error state retains saves and offers retry.
- **Assets/Scenes/MainLobby.unity**: saved uGUI shell, one Canvas/EventSystem/InputSystemUIInputModule, safe-area root, profile/currency bar, nine navigation entries, page container, modal confirmation and feedback. Runtime fills profile-driven pages; only page transitions/actions rebuild bounded content, never every frame.
- **Assets/Scenes/PrototypePool.unity**: existing scene retained. Only one HubMatchBridge component added to PrototypeMatch. Existing fourteen athletes, authorities, physics, clocks, referee, mobile controls, camera and pause behavior remain.
- Shared build scene list: Boot, MainLobby, PrototypePool, then preserved SampleScene. Existing Android Build Profile has `m_OverrideGlobalSceneList: 0`, so it uses this shared list. Scene loads use names, never numeric indices. No device build was made.
- Editor `playModeStartScene` was set to Boot. **Tools > Water Polo > Play Hub Entry** launches normal entry. **Tools > Water Polo > Play PrototypePool Directly** clears that override for isolated tests. **Tools > Water Polo > Use Boot as Editor Play Entry** restores it. **Set Up Milestone 12 Hub** is saved-scene/Edit-Mode-only, additive, preserves catalog tuning and existing hub scene GUIDs, repairs old typography and restores the original open-scene setup.

### Navigation and real offline functionality

Lobby -> Play -> Quick Match or Training; Online and Career/Tournament are disabled previews. Lobby also opens Shop, Collection -> Details, Tactics, Season Pass, Trophies, Profile, Clubs, Friends and Settings. Secondary pages have Lobby/Back; Escape/gamepad East returns to Lobby or cancels the modal. Confirmations block underlying pointer input. Buttons have hover/press/disabled colors; pages fade using unscaled time. No added UI sound clip/framework.

- Lobby: local name/level/XP/trophies/currencies, original procedural athlete portrait, saved color preview, squad/tactic summary, Play, free-season preview and configurable pack slots.
- Collection: twelve fictional stable athlete IDs across five rarity definitions, owned/unowned states, role, OVR/level/shards, details and core stats. Nine starting owned athletes include one reserve field and a reserve goalkeeper. Upgrades spend rarity-configured coins and level*5 shards, max level 10, and use expected-level idempotency. **Collection ratings do not change live movement/shot/keeper skill.**
- Tactics: top-down pool, goalkeeper plus six field slots, bench/eligible selector, tap slot then athlete to swap/replace, no duplicates and role/ownership validation. Press/Zone persists and applies through the existing Blue PrototypeTeamTactics.SetDefense. Formation remains Classic Six; no new AI strategies or drag framework. The bridge maps lineup IDs to `SwimmerIdentity.CollectionDefinitionId` while retaining existing scene PlayerId/team/role identities.
- Profile: editable plain-text 2–18-character display name, four named local avatar choices, curated cap/suit/accent palettes, level/career/collection summary and Save. Lobby portrait previews cap/suit/accent. **Live match materials and unique avatar icon artwork are not integrated; the selected avatar is shown as a local badge label.** Team identity stays data-based.
- Trophies: Ripple/Surge/Deepwater starter ranks, trophy/next-rank and career summaries, six stable achievement IDs with validated once-only coin claims.
- Clubs/Friends: integrated informational previews, local identity and disabled future actions. No fake friends, sent invitations, real clubs or online status. Settings modifies/persists master volume and directs to the preserved match settings for camera/SFX/graphics.

### Economy, rewards and persistence

`HubProgression` is one local economy/progression authority implementing small service interfaces. UI gets detached Snapshot copies. Transactions mutate a copy, validate, save then publish. Integer balances, affordability checks, overflow-checked grants, duplicate IDs and expected upgrade levels prevent accidental repeated grants/spending. No generic UI balance setter exists.

Placeholder catalog defaults: introductory **600 coins / 20 gems**, existing nine-athlete ownership and one free waiting pack created **only on first run**. Quick Match rewards: win 120 coins, draw 85, defeat 60; 40 account XP and 35 season XP on each eligible completion; +12 win trophies, 0 draw, defeat removes up to 3 without going negative. One free pack per completed Quick Match if a slot is available. Training and abandoned sessions grant no progression. Values are editable in Assets/Resources/HubCatalog.asset.

`HubMatchBridge` observes actual PrototypeMatch.FullTime and scores, never scoreboard text. It records one durable receipt for the active session UUID, consumes that session, and leaves a pending result for the lobby. Results show score, coins/XP/trophies and pack/overflow feedback; Continue acknowledges the result. Full-time menu offers REWARDS & LOBBY. If reward save fails, the button retries the authoritative result transaction before leaving. Confirmed match Exit abandons the session and returns to lobby, releasing pause. Restart keeps training classified as training and starts a fresh session. Actual end-to-end behavior still needs the pending runtime check.

Four pack slots by default; 45-second configurable sample unlock. Waiting -> unlocking -> ready are derived from persisted instance/start/duration; claim and reward grant/slot removal are atomic. Fixed outcome generated at acquisition prevents rerolls during reveal/reload. Free odds: 60% 80 coins + 8 Niko Reef shards; 30% 120 coins + Noa Crest + 5 shards; 10% 160 coins + Eli Deep + 5 shards + 3 gems. UI displays odds before unlock/open. Claim uses a lightweight confirmation/reveal panel. **No elaborate reveal animation.** Full slots refuse an additional pack and report that on the result; existing packs are retained. Backward clock changes clamp remaining time to original duration; forward changes can unlock early. Fully offline device time is not tamper-proof.

Shop tabs: Featured, Player Cards, Upgrade Materials, Coins, Gems, Special Offers. Five known-content offers use coins/gems: Reef shards, Crest acquisition/shards, Surge acquisition/shards, keeper shards and a 250-coin bundle. Each has fixed contents, price, confirmation, insufficient-balance/success feedback and saved transaction ID. Empty future tabs explain availability. Ads and real-money store controls are disabled; no SDK/provider or fake success.

Season 01: twenty free reward tiers, 60 cumulative season XP per tier, coins (80 + tier*10), plus 3 gems each fifth tier. Eligible claims are once-only under stable season/tier keys. Horizontal scroll, premium locked preview. No local-date expiry or automatic progress wipe.

Save: `Application.persistentDataPath/profile-v1.json` (normally `%USERPROFILE%/AppData/LocalLow/ToduStudios/Water Polo Superstars/profile-v1.json` on Windows). Schema 1 stores UUID, owned IDs/levels/shards, lineup, cosmetics, pack records, season XP/claims, counters, last twenty receipts, pending/active match and revision/transaction journal. No art/stat catalog duplication. Atomic validated temp replacement, .bak recovery, content validation before selecting snapshots. Unsupported schemas halt rather than downgrade. Unrecoverable existing saves halt initialization with retry; they are not overwritten with defaults. Important transactions save immediately, so there is no dirty economy buffer awaiting lifecycle callbacks. Existing preferences remain separate in GamePreferences' PlayerPrefs record.

### Firebase readiness

See **docs/FUTURE_BACKEND_AND_ECONOMY.md** for compact records, schema/catalog migrations, unknown IDs, explicit local-to-account linking, two-device conflicts, operation IDs/outbox/revisions, server transaction deduplication and verified store entitlements. ICloudSyncAdapter and IPurchaseProvider are future boundaries only. Current status is Local only. Client JSON is not fraud-resistant. Never apply whole-profile last-write-wins to currencies or entitlements. Acquired pack outcome indices currently depend on preserving table order; migrate to stable versioned outcome IDs before changing that meaning.

### Files

New runtime files (all with Unity meta files) in **Assets/Scripts/Hub/**: HubCatalog.cs, LocalProfile.cs, JsonProfileRepository.cs, HubProgression.cs, HubApplication.cs, HubMatchBridge.cs, HubWidgets.cs, BootScreen.cs, HubView.cs, HubCollectionViews.cs, HubRewardViews.cs. New Editor files: **Milestone12HubSetup.cs, HubVerification.cs** plus metas. New **Resources/HubCatalog.asset**, Resources folder meta, Boot/MainLobby scenes plus metas, **Tools/Verification/Milestone12Run.cs**, **docs/FUTURE_BACKEND_AND_ECONOMY.md**. Modified **PrototypePool.unity, MatchMenuController.cs, SwimmerIdentity.cs, ProjectSettings/EditorBuildSettings.asset, AGENTS.md, README.md, plan.md**. No changes to BallController, PrototypeMatch, MatchRules, camera, AI or input implementations.

### Tests actually run and remaining steps

| Check | Actual status |
| --- | --- |
| Unity Editor compilation | PASS on first complete implementation, after correcting an Average double-to-float conversion. |
| HubVerification.Core | PASS in Editor: first-run identity, fixed purchase, insufficient balance, duplicate purchase/result/achievement/upgrade/season/pack claims, role/ownership/lineup, training reward exclusion, profile/tactics/upgrade reload, pack persisted timer/backward-time bounds, overflow, failed-write rollback, compact save, corrupt-primary backup and unrecoverable-file preservation. Predates final validation/schema refinements. |
| HubVerification.Setup | PASS in Editor, repeated: hub scene GUIDs retained, shared Boot-first list, fourteen athletes, one bridge. |
| Latest source compilation | PASS: pinned Unity Roslyn with generated runtime and Editor response files; no diagnostics. Compilation artifacts stay in ignored Logs/M12. |
| Boot runtime | Entered Boot at frame 2; not sufficient evidence of completed lobby flow. |
| Navigation/pointer | NOT COMPLETE. Synchronous eval misuse stalled Editor; use script run below. |
| ActualLayouts | NOT RUN. Contains 36 Game view captures at 1920/2340/2400 x1080 with simulated safe insets, panel mesh and text-overflow assertions. Must capture and inspect images. |
| MatchLoop | NOT RUN. Isolated memory profile, real shot/goal, 14-athlete mapping, pause, four periods/one end swap, receipt once, result/lobby, reload and training abandonment. Production save restored in finally. |
| GameplayRealismVerification.Integration | NOT RUN this session. Run once in direct match for consolidated physical/AI regression. |
| Android/iOS/device performance/touch | NOT TESTED; no compatibility/performance claim. |

Historical compile error was corrected. First boot produced TMP ellipsis-glyph warnings; new widgets, saved scenes and repeat-safe repair now use Truncate, but the runtime correction has not been rechecked. Recovery failed on licensing/package-manager startup within sandbox. No fresh clean-runtime Console result is available. Do not conflate historical captured errors with current compile errors.

After reopening through Unity Hub, save intended scenes and use **Tools > Water Polo > Set Up Milestone 12 Hub** if restoring Editor entry. Press Play and focus Game view. Run asynchronous checks using **script run**, never `return Task` from script eval:

- `unity command script eval 'return ToduStudios.WaterPolo.Editor.HubVerification.Core();' --format json` (synchronous core is safe).
- `unity command script run --file Tools/Verification/Milestone12Run.cs --entry Milestone12Run.Navigation --format json --timeout 45`
- Same file, entry **Milestone12Run.Layouts**, timeout 60; inspect actual PNGs under Logs/M12 and resolve any overflow before declaring visual acceptance.
- Same file, entry **Milestone12Run.MatchLoop**, timeout 60; profile fixtures are isolated.
- Stop Play, use **Play PrototypePool Directly**, then `unity command script run --file Tools/Verification/GameplayRealismVerification.cs --entry GameplayRealismVerification.Integration --format json --timeout 60`. Restore Boot entry afterwards.

### Developer manual review

1. Open project through Unity Hub, keep recovery backups if prompted. Use **Play Hub Entry**. Verify loading -> lobby, balances/collection and one local profile after stop/restart. Test 16:9, 19.5:9, 20:9 and safe areas.
2. Visit every nav section and Back/Escape. Shop -> Player Cards -> buy Reef shards -> confirm balance/resources; attempt unaffordable purchases. Collection -> details -> upgrade. Tactics -> tap a field slot -> reserve/starter; try keeper slot. Season XP/rewards become available after eligible matches. Profile -> name/avatar/colors -> Save -> restart and compare.
3. Start the free pack, stop Play/reopen after 45 seconds, open once and inspect balance/shards/acquisition; ensure slot clears and repeated opening is unavailable. Review odds.
4. Lobby PLAY -> Quick Match -> START. Play four periods, Continue at breaks/halftime. At full time choose **REWARDS & LOBBY**, review saved reward screen, Continue. Reopen app and confirm balances, XP, trophies, pack and career counters persist without another grant. Pause -> Exit -> Confirm on an unfinished match must return without rewards. Training must not grant progression.
5. Confirm preserved joystick/actions/keyboard, rules, goals, keepers, halftime and pause feel. Inspect Console. Subjective visual quality and actual phone input/performance remain developer/device review.

Next action is **finish M12 runtime verification and polish**, not start M13. A later authorized milestone can connect live cosmetic/stat presentation and then implement authenticated backend/verified entitlements using the architecture document.

---
## Current handoff — M11 UI visibility, mobile controls and centre-pass correction

This is the explicitly authorized corrective implementation following the developer's M11 manual test. All prior uncommitted M11 changes are preserved. **Do not perform Git write operations.** The saved PrototypePool scene is now configured through the Editor; this delivery must include actual scene wiring and Game view evidence, not source compilation alone. No new milestone, project, gameplay scene or authority was created.

### Confirmed defects and repairs

1. **The saved scene was never configured for M11.** Fresh inspection found the old active Screen Space Overlay Canvas, fourteen athletes, one EventSystem, **zero MatchMenuController components and no Pause button**. The previous source-only delivery had left setup/runtime verification pending. The existing Canvas was working; the new interface was absent from the scene. The setup is now applied and saved directly in PrototypePool.
2. **Custom graphics lacked CanvasRenderer components.** The first actual Game view capture showed text with invisible panel backgrounds/icon bars and repeated MissingComponentException errors. RoundedPanelGraphic now explicitly requires CanvasRenderer, and setup repairs missing renderers on existing active/inactive graphics. A trial triangle-winding change did not solve this and was reverted. Subsequent actual renders show the panels, Pause bars, joystick and action backgrounds correctly.
3. **Goal restarts returned directly to Playing.** The old delayed CompleteRestart reset athletes and awarded the first conceding athlete possession at its start marker, then immediately enabled competition. It had no protected centre-pass procedure. Explicit centre restart phases now replace that goal-only path.
4. **Pause could move a held ball once.** BallController's LateUpdate carry smoothing lacked an immediate pause guard; changing timeScale does not retroactively zero the current frame's deltaTime. Carry smoothing now exits while paused. AI, commands, clocks, swimmers, rules, referee and water updates already have explicit suspension gates.

Setup initially assumed every athlete had human SwimmerInput. White AI athletes do not. This was corrected to wire existing human components only; incomplete setup can resume using an interface-version marker and existing objects. No swimmers/components were recreated to work around the failure.

### On-screen input architecture

- `MobileMatchControls` is a presentation/input adapter on the existing Canvas. It caches the selected athlete's existing APIs. **SwimmerInput remains the sole writer of human movement**, choosing keyboard/gamepad movement when non-zero, otherwise the selected athlete's virtual joystick. Sprint requests combine keyboard Shift with the UI hold. It never creates possession, a ball, another motor or a second phase controller.
- `VirtualSwimJoystick` implements pointer down/drag/up/cancel with one pointer ID, analogue magnitude, dead zone and a reusable handle. Release/disable recentres it. Keyboard movement has priority during simultaneous input.
- `GameplayActionButton` implements pointer down/up/exit/cancel; it does not fake holds with Button.onClick. **SHOOT** begins charge on down and releases on up; leaving the button cancels. **SPRINT** remains held until release/cancel. Independent pointer IDs permit joystick plus action holds. PASS, LOB, SWITCH, STEAL, COLLECT, defensive strategy and shot-mode cycling call existing commands.
- ShotCharge tracks an optional initiating input source. A keyboard release cannot release a pointer-owned charge, or vice versa. Both retain the same charge timing, speed, mode and release APIs. Pass/lob, pause, selection changes, stoppages, lost focus and disabled controls cancel relevant holds; nothing replays on resume.
- Actions adapt to possession and rule eligibility. Attacking exposes SHOOT/PASS/LOB, with NORMAL → SKIP → HIGH mode selection. Defending exposes STEAL; a loose ball exposes COLLECT. SPRINT/SWITCH and the strategy control remain accessible but respect current eligibility. During a human centre restart PASS is available after the signal, while shooting, swimming, sprint, switching, stealing and lobs are blocked. Pause remains available. Modal screens hide/block gameplay controls.
- Existing WASD/arrows, E/B/C/F/Space/Shift/L/Z/G and Q/R shot modifiers remain. Menu mouse/touch interactions use the existing InputSystemUIInputModule. No legacy Input API or external assets are introduced.

### Authoritative centre restart

PrototypeMatch appends **CentreRestartSetup** and **CentreRestartReady** without renumbering earlier phases. GoalRestart still provides the brief GOAL/net pause. Its expiry now enters centre setup instead of Playing. Period starts and shot-clock turnovers keep their separate existing procedures; foul/penalty phases remain owned by MatchRules plus PrototypeMatch.

- Reset eligible athletes to roster starts, keep goalkeepers at their current defended ends, and constrain other fields to their own half using each team's **current AttackDirection**. Excluded/permanently removed athletes remain ineligible and parked through the existing rules observer.
- Choose the conceding team's eligible field taker deterministically by cap number. Place that taker at the centre marker, choose an eligible teammate outlet, and face the taker toward it. BallController.TryAwardRestart is extended for this exact authoritative centre award; it remains the sole owner of possession.
- The possession clock is awarded/reset to the conceding team while stopped. Match/exclusion/possession time stays frozen through setup and ready. No centre-setup steals or shots are permitted.
- After the configurable **0.85s preparation** default, enter Ready and emit **one CentreRestartSignalled event**. MatchAtmosphere whistles; deck referees show a restart arm signal. They observe the authoritative transition and do not decide readiness themselves. GameplayCamera uses its existing set-piece profile.
- **Blue:** automatically select the taker and show **PASS TO RESTART | B OR PASS**. There is **no human auto-pass timeout**. B and UI PASS enter SwimmerPassing.TryPass → PrototypeMatch.TryCentrePass → the existing physical pass API toward the designated outlet.
- **White:** after the same signal plus a configurable **0.45s AI pass delay**, use the same pass API without keyboard simulation. The pass has velocity and travel time; the receiver acquires only when physically reachable.
- BallController.Released is the single successful-release transition into Playing. It does not reset/re-award the already travelling ball. Centre passes cannot count as scoring shots. The shot clock remains with the awarded team through friendly reception rather than resetting twice. Pause-aware PresentationTime preserves both preparation deadlines. Restart/exit clears the centre references and signal count.

### Saved layout and repeat-safe automation

Use **Tools > Water Polo > Set Up Milestone 11 Interface**, outside Play Mode on saved PrototypePool. This now repairs custom renderers, extends an existing M11 interface with missing controls and preserves completed layout tuning. `MobileControlsSetup` creates/reuses controls and wires only athletes that already have SwimmerInput. It adds no Canvas/EventSystem, ball, swimmer, goal or duplicate menu. Repeated setup verified **419 transforms and the same fourteen athlete IDs**. SampleScene is unchanged.

CanvasScaler remains 1920×1080 with landscape height scaling. The top-right Pause icon is 106×106 reference pixels. Left thumb space contains the 220×220 joystick; compact athlete status sits above it. Right actions have separate thumb zones; rule status sits above them. Safe-area roots cover HUD and menus, with a full-screen dim backdrop. Existing score, clocks, stamina, shot power, strategy and foul information retain their observers. The pause heading spacing was corrected in the saved scene to match the current setup defaults.

### Fresh automated evidence

Pinned Windows Editor: **Unity 6000.6.7f1**. Verified an advancing player loop using job wait on frameCount before trusting Play Mode renders. Checks are in **Tools/Verification/Milestone11Verification.cs**; results/captures in ignored **Logs/UIFix-***.

| Entry | Fresh result / coverage |
| --- | --- |
| Setup | PASS: repeat-safe wiring, CanvasRenderer requirements, one Canvas/EventSystem/menu/preferences component, fourteen identities. |
| MousePause | PASS: injected mouse down/up through the actual InputSystemUIInputModule opens Pause; pointer Resume returns to play. |
| MobileButtons | PASS: joystick, keyboard priority after physical turning, sprint hold/release, source-owned charge/release, pass plus real reception and automatic control transfer, lob, switch without ownership, valid steal, strategy, skip mode, cancelled pause holds, Blue goalkeeper control/distribution eligibility. |
| MenusSettingsAndTransitions | PASS: visible button raycasts, confirmation/cancel, restart/exit/landing/start, applied and persisted preferences (user values restored after test), Q2 end swap, full-time draw and foul/stamina reset. |
| PauseAndRules | PASS: Escape, charge cancellation, clock/position/velocity/ownership/stamina preservation, direct command rejection, airborne ball, exclusion, free throw, penalty and delayed goal/turnover restarts. |
| CentreRestarts | PASS: physical Blue/White scoring and conceding restarts in Q1 and Q3, own-half formations, exactly one signal, no premature movement with enabled AI, physical AI pass/reception, manual B/UI PASS after >6s wait, pause during ready, clock release timing and exact-once score. |
| ActualGameViewLayouts | PASS: **18 actual Game view renders**, HUD/Pause/Settings/Confirmation/Landing/Transition at **1920×1080, 2340×1080, 2400×1080** (16:9, 19.5:9, 20:9), simulated 70px side/24px vertical safe insets, button bounds, text overflow and populated panel meshes. Original Game view selection restored; temporary resolution entries removed. Representative images and Q3 centre restart inspected visually. |
| LiveAIPause | Carry guard correction implemented; final rerun pending below. |
| GameplayRealismVerification.Integration | One consolidated regression pending below. |

Failed checks were investigated rather than weakened: the null human-input wiring and absent renderers were repaired; the keyboard-turn test now allows finite motor acceleration; UI centre-pass testing allows the presentation frame after Resume; the live AI pause check exposed the carry guard defect. Source compilation alone is not counted as runtime success. No historical suites were repeatedly rerun wholesale.

### Exact developer review

1. Open **Assets > Scenes > PrototypePool** and press Play. The saved interface is configured; no Inspector wiring is needed. If repairing a different local copy, stop Play Mode, save intended edits and run **Tools > Water Polo > Set Up Milestone 11 Interface** once.
2. Confirm the top-right Pause icon, joystick and right-side actions. Click Pause and Resume; repeat with Escape. Pause while swimming, sprinting, holding SHOOT, and during ball flight. Scores, ball ownership, positions and both clocks must remain stable. Release held inputs while paused, then resume without a stale action.
3. Drag the joystick; hold a movement key at the same time to check keyboard priority. Hold SPRINT while moving, then release it. Collect the ball; hold/release SHOOT, use PASS/LOB, cycle NORMAL/SKIP/HIGH, SWITCH and the defense-mode button. Test STEAL against a nearby exposed opponent after acquisition protection.
4. Settings: move audio/camera sliders, choose graphics presets, Save & Back, resume, then stop/re-enter Play to inspect persistence. Crowd volume is intentionally absent with no crowd clip. Subjective audio levels and camera comfort need human review.
5. Restart/Exit > Cancel must preserve play. Confirm Restart resets Q1, 0:0, ends, stamina and foul history while keeping preferences. Confirm Exit opens the landing screen; Start Match returns to the match.
6. Score as Blue: inspect the GOAL pause, White's centre setup, whistle and real automatic pass. Let White score: Blue's selected taker must remain at centre with **PASS TO RESTART** indefinitely. Press B or PASS; verify physical travel and resumed clocks. SHOOT/SWITCH/joystick cannot bypass the protected phase; Pause still works.
7. Continue through Q2 halftime and repeat both goal restarts after ends change. Period starts retain the neutral centre ball. Review full-time win/loss/draw and Play Again. Automated transition coverage includes draw; subjective win/loss presentation remains manual.
8. Game view resolution dropdown: review 16:9, 19.5:9 and 20:9 landscape. Check pointer targets and comfortable visibility while playing, plus Console. Screenshots in Logs/UIFix-* provide repeatable Editor evidence; no physical phone/touch/performance claim is made.

### Files and limits

New for this correction: **MobileMatchControls.cs, VirtualSwimJoystick.cs, GameplayActionButton.cs** in Assets/Scripts/Gameplay and **MobileControlsSetup.cs** in Assets/Editor, with Unity meta files. Existing uncommitted M11 files remain new in Git. Modified for this correction: PrototypeMatch, MatchRules, BallController, SwimmerPassing, PossessionShotClock, ShotCharge, SwimmerInput, SwimmerMotor, PoolsideReferee, RefereeVisual, MatchAtmosphere, GameplayCamera, RoundedPanelGraphic, Milestone11UISetup, PrototypePool scene, consolidated M11 verification, README, AGENTS and this handoff. The older M11 inventory below records its other integration changes.

No Android/iOS hardware or real multitouch verification; pointer IDs are implemented, but simultaneous physical touch and cancellation by device OS remain manual/device follow-up. No touchscreen aim stick: joystick/keyboard facing aims ordinary play; centre passes use the prepared outlet. No substitution/forfeit rule is added for a team with too few permanently eligible fields to form a centre pass; it remains stopped rather than inventing possession or replacing athletes. Graphics presets remain lightweight presentation controls, not measured device performance. Windows Burst code-integrity warning remains an external issue; security, package versions and caches were not altered.

Stop after this correction. Human manual review and human-only Git operations follow.

## Historical handoff — M11 source-only interface (2026-11-10)

Milestone 11 is explicitly authorized. It extends the committed realism baseline `4def2fb`; all M11 changes must remain **unstaged and uncommitted**. Earlier M10 sections below retain their historical status. The full **14-athlete** match, one ball authority, physical shots, tactics, rules and four-period flow remain the foundation.

### Interface implementation

- Existing uGUI Canvas and observers are extended in place. No new UI framework, gameplay scene, ball authority or competing match timer. **Tools > Water Polo > Set Up Milestone 11 Interface** configures PrototypePool with Play Mode stopped and the scene saved. It refuses other scenes/unsaved edits and preserves layout/Inspector tuning on subsequent runs. SampleScene is untouched.
- Navy/water-blue rounded mesh panels, compact Blue/White score, quarter/displayed 8:00 clock, urgent shot clock, possession chip, athlete/energy card, Press/Zone chip, rules/personal-foul/exclusion card and short hints. Existing charged-power graphic remains. Rules notifications use a compact event card; goals retain their own notice.
- Top-right Pause uses the existing InputSystemUIInputModule for mouse and future touch. Escape and gamepad Start use a private New Input System action; Escape is also the platform Back route where provided by the input backend. No legacy input. Android Back and physical touch require device verification.
- Pause offers Resume, Settings, Restart and Exit. Restart/Exit use cancellable confirmations. Exit calls the existing ReturnToPrototype reset into PreMatch and opens an in-scene prototype landing screen; Start Match uses the existing reset API. It does not quit the application or load/overwrite another scene.
- Quarter, halftime and full-time cards observe the existing phases. Continue uses the existing atomic end swap; full-time shows Victory/Defeat/Draw from Blue's result. Fades use unscaled time; hidden panels cannot accept pointer input. Buttons have color feedback. Events/actions have explicit enable/disable/disposal lifetimes.

### Pause integration

**PrototypeMatch remains the sole authority.** Its orthogonal `IsPaused` preserves the underlying phase. `IsPlaying` and `BallInMotion` reject suspended activity. Suspension saves/restores timeScale and AudioListener.pause without calling the restart freeze that clears swimmer velocities or changes ball kinematics. No position/velocity/ownership snapshot is teleported back on resume.

Gameplay input, selection, rule release/keeper permissions, restart awards, AI decisions, keeper distribution, referee updates and stamina are explicitly gated. Human charge cancels safely; held Space is blocked until released and same-frame input is suppressed on return. Human command vectors clear on resume while physical velocity remains. AI decisions are preserved rather than reset through a phase Changed event. Water feedback freezes without removing existing effects.

Existing unscaled goal/turnover, free-throw setup/fallback, penalty-resolution and referee/feed/camera deadlines now use `PrototypeMatch.PresentationTime`: the original unscaled timeline minus suspended duration. It freezes during suspension and is **not** another match/shot clock. Do not reset its epoch during a new match. Scaled cooldowns/physics freeze with simulation. Resume retains the phase; confirmed Restart/Exit release suspension before the synchronous authoritative reset. Disable/teardown releases owned global pause state. Application backgrounding opens pause; returning does not silently resume competition.

### Settings and landscape architecture

`GamePreferences` persists one versioned local JSON PlayerPrefs key, `ToduStudios.WaterPolo.UI.v1`. Values are validated/clamped; unsupported versions/corrupt JSON use defaults. Sliders apply immediately; disk saves happen on leaving menus/disable/backgrounding, not every slider frame. Preferences survive match reset.

- Master controls AudioListener.volume; SFX controls the match cue source. Optional crowd audio has a separate source/level; its slider is hidden when no crowd clip is configured. No fake crowd control or downloaded audio.
- Camera distance (0.85–1.25x) multiplies existing bounded framing; smoothing (0.65–1.8x) multiplies damping without overwriting authored profiles.
- Low disables water feedback and camera shadows/post effects. Medium uses reduced water feedback with those camera features disabled. High restores standard water feedback and the camera's original shadow/post flags. These are presentation presets, not measured phone-performance claims or URP asset quality changes.
- CanvasScaler uses 1920×1080 and landscape height scaling. HUD/menu content have separate normalized safe-area roots; the dim backdrop covers the whole view. A controls reference documents current keyboard actions. Full touchscreen gameplay controls remain future scope.

### Files

New runtime files in `Assets/Scripts/Gameplay/`: `MatchMenuController.cs`, `GamePreferences.cs`, `MatchSettingsView.cs`, `SafeAreaLayout.cs`, `RoundedPanelGraphic.cs`, plus Unity-generated meta files. New setup: `Assets/Editor/Milestone11UISetup.cs` (+ meta). Focused checks: `Tools/Verification/Milestone11Verification.cs` outside player builds.

Modified integration files in `Assets/Scripts/Gameplay/`: PrototypeMatch, MatchRules, PlayerSelection, BallController, BallInteractionInput, SwimmerInput, PrototypeSwimmerAI, PrototypeGoalkeeper, GoalkeeperDistribution, SwimmerStamina, PoolsideReferee, GameplayCamera, WaterInteractionEffects, MatchAtmosphere, MatchHUD, PrototypeScoreboard and ActiveStaminaHUD. Scene configuration and final documentation changes are recorded with verification below.

### Fresh verification status / remaining handoff

- Fresh Unity script recompilation completed without errors. The complete external M11 verification file compiled through `Milestone11Verification.SourceContracts` (latest 684ms compilation, no diagnostics). This is source/API verification, not gameplay validation. Latest native Console audit: **0 errors, 1 warning** (pre-existing Burst JIT DLL blocked by Windows code-integrity policy); Unity AI NoSubscription entries are ordinary logs. The Console was not cleared.
- The initial Editor was ready but its Play Mode was **paused**. M11 setup correctly refused to change a playing scene. The user's session is preserved pending explicit approval to stop it. **Scene setup, focused Play Mode checks, layout renders and consolidated gameplay regression have not yet run.**
- Focused entries: Setup (repeat safety/14 identities); PauseAndRules (Escape, charge cancellation, physical state, clocks, exclusions, throws, penalties, restarts); MenusSettingsAndTransitions (pointer raycasts, confirmations, reset/exit, preferences and periods); LiveAIPause (enabled AI decision preservation); LandscapeLayouts (16:9, 19.5:9, 20:9 and simulated safe insets). Reuse GameplayRealismVerification.Integration once for consolidated gameplay coverage.
- No developer manual gameplay, phone/touch or performance verification is claimed. Burst/Smart App Control remains the pre-existing external issue documented below; M11 does not change security settings, packages or caches.

### Manual Unity review

1. Open **Assets > Scenes > PrototypePool**. Stop Play Mode, save intended edits, then run **Tools > Water Polo > Set Up Milestone 11 Interface** if not already configured. Press Play and focus Game view.
2. Click Pause; verify frozen clocks, athletes, ball and referees. Resume. Repeat with Escape while swimming, charging Space, during high/lob flight and a protected restart. Release Space while paused; resume must not release a stale shot.
3. In Settings, change Master/SFX, camera distance/smoothing and Low/Medium/High. Save & Back, resume and compare actual behavior. Stop/re-enter Play Mode to check persistence. Crowd slider should be absent with the current clip-free crowd configuration.
4. Restart > Cancel preserves the match. Restart > Confirm returns Q1, 0:0, full stamina, clean fouls/exclusions and original ends. Exit > Cancel preserves play; Exit > Confirm opens the landing screen. Settings and Start Match there must work.
5. In the **Game view resolution/aspect dropdown**, review **16:9, 19.5:9 and 20:9** landscape. Check pause/confirm/settings click targets, readable text, HUD spacing and visible play. Real cutouts and touch remain unverified.
6. Play through quarter/halftime/full time. Continuing after Q2 swaps ends once. Check result and Play Again confirmation, including win/loss/draw as available. Check Console and exact-once goals/turnovers, including pause near shot-clock expiry.

Stop after M11. Next work is human manual review and human-only Git operations.

## Historical handoff — M10 gameplay realism expansion

This is the explicitly authorized follow-up to the existing **uncommitted M10 phase**, not a separately committed/pushed milestone. It preserves the previous M10 work and all **14 athletes**. Verification uses the pinned Windows **Unity 6000.6.7f1** Editor; no Android hardware, Blender, paid assets, imported rigs or external services are needed. The earlier M10 handoff below remains technical history, with its rule-profile limits still applicable.

### Architecture and actual behavior

**One BallController remains authoritative.** `BallFlightProfile` contains serializable release/impact tuning plus the shared ShotType and impact-event models. `BallTrajectory` provides pure equations, not another simulation/possession component. BallController runs its flight/water preparation at execution order **-140**, before existing keeper swept contact (-120) and match scoring (-100). PhysX still advances real 3D flight with gravity/CCD; the existing water-plane model handles floating drift. PrototypeMatch alone awards scores/transitions, using the unchanged directional whole-sphere GoalMouth geometry.

| Input/API | Implemented release |
| --- | --- |
| Space tap | Normal direct shot, limited genuine height; default release 0.22m above the floating centre. |
| Hold/release Space | Existing 0.7s charge and 10–22m/s range; charge >=0.65 selects Powerful, default release height 0.35m. |
| Hold Q, then hold/release Space | Skip. Default 0.55m release height and a downward launch chosen for a water strike before the goal. |
| Hold R, then hold/release Space | High. Real gravity trajectory with flight time based on current goal distance/speed and a bounded goal-height target; default release height 0.45m. |
| B | Ordinary water-skimming pass with distance-scaled speed, bounded motion lead and existing angle assistance. |
| L | Real lob; existing duration remains, with configurable minimum arc height (default 1.8m). |

The modifier is latched when charging begins; Q wins if both Q and R are held. Releasing a modifier mid-charge does not change the chosen type. Direct `TryShoot(speed, ShotType)` and `ShotCharge.BeginCharge(ShotType)` expose the same actions to AI/future touch controls. Existing C/F/G/Z/Shift/E/gamepad actions, power bar, keeper shooting and possession selection remain. AI can choose High under nearby pressure and Skip for a clear centre-forward chance at suitable range, through the same APIs and configured AI speed; it has no speed or possession bypass.

**Water impact** uses the actual crossing between successive physics poses and the incoming 3D velocity. A qualifying shallow, fast Skip impact retains 90% horizontal speed and uses 0.42 vertical restitution, capped to a single intentional bounce and 0.6–2.7m/s upward speed. Subsequent water contact settles; it cannot bounce indefinitely. Normal/power shots retain 94% horizontal speed at the transition to existing water drag; soft lob landings retain 32%. These are configurable game values, not claims of full hydrodynamics. The effective water plane is the existing `SurfaceHeight` floating-centre level. Frame/athlete colliders stay enabled in flight. Side/end bounds preserve height, permit valid goal-pocket travel and reject above-bar/outside-post goals.

`PredictPosition` uses bounded measured velocity, gravity, water response (including one predicted skip), and drag. It does not know future input. Pass lead is capped (1.4m ordinary/1.5m lob) and retains angle/cone restrictions; nearby lane blockers reduce assisted target preference. Field reception still requires an actual travelled segment/current reachable position, low descending height, available ownership and match eligibility. Default maximum field reception speed is **14m/s**; keeper swept-contact saves remain separate. Receiver/loose-ball AI swims toward predicted travel/landing positions rather than acquiring remotely. Predictions approximate drag and unobserved collisions and can be wrong.

**Keeper contact** remains physical 3D swept hand-sphere contact with reaction, skill, station and recovery checks. Hand height moves at a bounded rate up to 0.65m and the primitive arms rise with it. Tracking reads the actual held-ball carrier position, avoiding the frozen Rigidbody pose while held. Powerful shots lower ordinary catch probability, while explicit 0/1 tuning endpoints remain useful for verification. Deflections retain 3D motion when above water and can subsequently land or skip. Manual Blue keeper possession/distribution policy is unchanged.

**Posts and net:** the ball/posts/crossbar share a new 0.7-restitution, low-friction PhysicsMaterial asset. Actual colliders rebound shots; scoring dimensions remain 3m × 0.9m. On a validated crossing, PrototypeMatch asks BallController to contain the scored ball with reduced momentum inside the goal pocket during the existing goal pause. It remains visible moving into the net instead of freezing at the plane. `GoalNetReaction` deforms existing non-colliding strands briefly, then restores them; it cannot award a goal. New restarts clear capture/flight state. No replay, cloth solver or invisible scoring shortcut was added.

### Broadcast camera

GameplayCamera remains the only camera controller. It preserves the broadcast yaw through selection/halftime and combines controlled athlete, actual ball/carrier, bounded velocity prediction, current possessing team's goal, and nearby eligible teammates/defenders. A bounded point/frustum calculation widens distance/height when needed at the current aspect ratio. Zoom uses hysteresis and damping rather than tracking every small change. Selection retains the existing smooth target handoff; explicit restart snaps read physics poses to avoid stale rendered restart positions.

Inspector profiles: **Normal Broadcast**, **Wide Tactical**, **Goalkeeper Possession**, **Set Piece**, each with size, ball weighting, maximum zoom and damping. Normal/Wide are the ordinary manual mode choices; keeper/set-piece profiles activate from gameplay context. Existing distance/height/FOV/lead tuning is retained; adaptive framing also has a legacy follow fallback. Minimum/maximum zoom are bounded, so extreme off-ball separation cannot guarantee the whole pool/all athletes remain visible. No uncontrolled halftime rotation or camera shake was introduced.

### Referees and event presentation

Two visible primitive referees stand on observation decks outside the pool at approximately X=±11.4m. PoolsideReferee observes carrier/ball position, rule phases, goals and period changes. It chooses central/advanced/end observation sectors at **0.65s** intervals rather than copying ball Z every frame. Movement uses **3.5m** start hysteresis, **0.5m** stopping tolerance, **2.2m/s** speed, **3m/s²** acceleration/deceleration and bounded end positions. It smoothly looks toward play, pauses for decisions/goals and returns toward midfield at breaks. The current scene's straight deck corridors are clear of goals/walls; this is not dynamic obstacle navigation for future arena furniture.

RefereeVisual implements `IRefereePresentation`; movement holds a replaceable MonoBehaviour presentation adapter. Primitive torso/head/arms/legs provide restrained walking, idle, turning, whistle-hand, foul, exclusion, penalty, goal and period poses. There is no IK or imported rig. Signals subscribe to MatchRules.Decided and PrototypeMatch.Changed; proximity never creates a foul or possession award. All referee/deck visuals have no gameplay colliders. MatchAtmosphere supplies short and stronger original procedural whistles, period/final cues and throttled water-impact audio; it never controls timing. Sound quality/listening remain manual review.

### Water and impact effects

WaterInteractionEffects creates **16 reusable ripple/splash slots** (8 available in Low), two shared procedural meshes, and one reusable wake line per athlete at startup. Events size effects by impact intensity. Skip/save/frame contacts can show small splash spokes; landings ripple. Movement produces small V wakes, with sprint/keeper movement increasing visible disturbance. A short reusable shot trail distinguishes fast releases, while the existing BallHeightCue supplies the airborne shadow. Effects have no colliders, ownership logic or match timers. They expire in 0.9s and clear on restart; no per-frame GameObject/mesh spawning is used. **Off / Low / Standard** quality is configurable; both runtime API and Inspector changes clear active effects/trails. Low keeps only faster wakes and half the impact pool. No fluid simulation, tessellation or large particle overdraw was added.

### Setup and file inventory for this expansion

Saved scene is already configured. Optional command, after stopping Play Mode and saving intended edits: **Tools > Water Polo > Set Up Gameplay Realism Expansion**. It extends the current M10 scene, rejects unexpected/dirty scenes, preserves all identities and useful tuning, wires references, creates the deck/referees/materials/effects/net observers, and avoids duplicate objects on repeat. It does not rerun M1–M10 builders or touch SampleScene.

Created under `Assets/Scripts/Gameplay/`, each with `.meta`: `BallFlightProfile.cs`, `GoalNetReaction.cs`, `PoolsideReferee.cs`, `RefereeVisual.cs`, `WaterInteractionEffects.cs`. Created Editor utility `Assets/Editor/GameplayRealismSetup.cs` with `.meta`; created `Tools/Verification/GameplayRealismVerification.cs` outside player builds. New materials under `Assets/Materials/Prototype/` with `.meta`: `ObservationDeck.mat`, `RefereeWhite.mat`, `RefereeDark.mat`, `RefereeSkin.mat`, `WaterFeedback.mat`, `RealismBall.asset` (PhysicsMaterial).

Modified existing runtime files: `BallController.cs`, `BallInteractionInput.cs`, `GameplayCamera.cs`, `LobPassing.cs`, `MatchAtmosphere.cs`, `PrototypeGoalkeeper.cs`, `PrototypeMatch.cs`, `PrototypeSwimmerAI.cs`, `PrototypeTeamTactics.cs`, `ShotCharge.cs`, `SwimmerBallInteraction.cs`, `SwimmerPassing.cs`, `WaterPoloDecision.cs`. Saved additive wiring/objects in `Assets/Scenes/PrototypePool.unity`; updated README, AGENTS and this handoff. The earlier M10 uncommitted files remain part of the shared review set. No package/security/Git write is authorized or needed.

### Verification and review limits

Fresh compilation passed. **Eight distinct focused entries** have passed in the Windows Editor: five new entries and three reused regressions. One consolidated pass covered the representative systems; only changed/failed checks were repeated afterward. No claim is made that all historical suites or device tests ran.

| Entry | Fresh automated coverage |
| --- | --- |
| GameplayRealismVerification.Setup | Repeat-safe setup, all 14 IDs, two non-colliding referees, one ball/effects authority, two net observers, material wiring and retained tuning. Edit scene has 276 transforms. |
| GameplayRealismVerification.BallPhysics | Normal water transition, real injected Space/Q/R charge controls, high and skip goals, bounded skip/net motion and one score, physical post/crossbar collisions, above-bar rejection, soft lob height/landing, contested low reception and exclusive acquisition. |
| GameplayRealismVerification.CameraRefereesEffects | Controlled player/ball framing at 16:9, 19.5:9, 21:9; manual API/actual-acquisition handoffs; keeper/set-piece/wide profiles; deck bounds, speed, turning and stationary dead zone; foul/high-charge cancellation and penalty signal while repositioning; real pooled impacts, expiry/stable object count, no effect colliders and Inspector quality. |
| GameplayRealismVerification.Integration | Moving-carrier keeper tracking, real high-shot catch and powerful post-skip deflection, airborne shot-clock cleanup/opposing restart, halftime orientation/end mapping, full time/new match and 10s of AI play with exclusive possession/eligible Blue control. |
| GameplayRealismVerification.EffectsQualitySwitch | Active impact exists before Inspector Off; active ripple/splash objects and shot trail clear. |
| Milestone10Verification.FoulsAndExclusions | Actual F-contact foul, charge cancellation, protected free throws, frozen clocks, expiry after release, personal fouls, exclusions/re-entry, permanent removal, new match and halftime history. |
| Milestone10Verification.PenaltiesAndIntegration | Human/AI penalty qualification, physical save/AI goal, man-up goal re-entry, shot/period clock handling, no keeper return teleport and 12s integrated play. |
| FullTeamMilestone9Verification.GoalsAndKeepers | Five physical goals including post-halftime own goal, two catches/distributions at new ends, conceding ownership and all-athlete starts. |

Evidence: entry-specific `Logs/Realism-Final-*.json`. These checks include deliberate isolated physics poses and forced keeper skill endpoints where necessary; they do not imply manual gameplay confirmation. The effect-expiry fixture stops later ball motion after its first actual impact so another legitimate wall ripple cannot obscure expiry. Real frame progression was confirmed with the CLI frame-count wait, not assumed from Play Mode entry. Actual Game-view captures are `Logs/Realism-Broadcast.png`, `Realism-Skip.png` and `Realism-Referee.png`. Final Console/settings/Git audit is recorded below after cleanup.

Known limitations: hybrid water surface and one intentional skip rather than full buoyancy/spin/waves; approximate prediction and bounded pass help; no guarantee every high shot beats or is saved by a keeper; limited hand/arm lift without diving; simple sector-based referee walking on the current clear decks rather than navigation around future furniture; symbolic primitive signals; tonal procedural audio; a fixed-orientation broadcast view with bounded zoom that cannot show arbitrarily separated action. Earlier M10 rule simplifications still apply. Long match balance, every collision angle, simultaneous period/foul edges, audible mix, every screen layout and mobile CPU/GPU/thermal behavior remain unverified. No Android device is required for this Editor handoff.

### Exact manual Unity review

1. Open **Assets > Scenes > PrototypePool**, press Play, click Game. The scene is wired; optional rebuild is **Tools > Water Polo > Set Up Gameplay Realism Expansion** outside Play after saving intended edits.
2. Use E/B/L/C/F/G/Z/Shift/WASD as before. Tap Space, fully charge Space, then hold Q before Space for Skip and R before Space for High. Watch actual height/shadow, water strike, retained skip travel, post/bar rebounds, reachable pickups and single goals/net motion. Charge cancellation on possession loss/foul must not release a stale shot.
3. Pass to moving receivers and contest lob landings. Test actual Blue keeper catch/control/distribution and keeper responses to high/skip shots. Physics and prediction do not guarantee reception/save success.
4. Select **Hierarchy > Main Camera > Gameplay Camera** (the object with GameplayCamera). Compare Mode Normal Broadcast/Wide Tactical; tune the four profiles in Play for review only. Test player handoffs, keeper possession, midfield/goal approaches and long passes at Game-view 16:9 and wider presets. Verify the attacking end remains correct after halftime without camera rotation.
5. Watch **Hierarchy > GameplayRealism > RefereeEast / RefereeWest**: purposeful deck movement, natural starts/stops, facing, and rule-driven signals. Move play between midfield/ends, then provoke F/G fouls and a qualifying penalty; referees must not collide with players/ball or independently decide outcomes.
6. Select **Hierarchy > GameplayRealism > Water Interaction Effects**. Compare Quality Standard/Low/Off while splashes/wakes are active. Confirm effects clear/expire and goals, pickup and restarts remain unaffected. Listen to ordinary/stronger/period whistles and impact volume.
7. Continue through halftime and full time, then Restart Match. Check ends, scores, clocks, exclusions, pending flight/net state and old effects. These are manual Windows Editor review steps; no manual playtest or Android/iOS performance claim is made.

Final audit: compilation completed with **compilationFailed=false**. The last live gameplay/physics checks observed **0 errors / 0 warnings**. After the final Editor domain reload, the current native Console contains **0 errors / 1 warning**: the previously investigated **Burst JIT DLL blocked by Windows Smart App Control / WDAC** warning recurred. This is not a remaining kinematic-body or C# compile warning. Native Console inspection confirmed its exact source (`Logs/Realism-Native-Console.json`); no Console clearing or security changes were used. See the prior Burst investigation below for safe manual diagnostics. Current snapshots: `Logs/Realism-Final-Console.json` and `Logs/Realism-Final-Editor.json`.

The final compatibility check also confirms water landing does not bypass release protection. The final camera change removed a temporary params-array allocation from the framing loop using equivalent binary maximum operations; compilation was rechecked without repeating unchanged historical suites. Broadcast, skip/splash, and close-up foul-signal captures were inspected; explicit camera snaps now frame the new physics pose correctly and the goal stays clear of the HUD in the reviewed goal-area capture.

Editor is left **stopped**, Run In Background restored **off**, auto-tick disabled, and generated Pipeline configuration moved under ignored Logs. Incidental Run In Background and URP importer settings were restored; **no package, ProjectSettings or SampleScene diff remains**. SampleScene SHA1 remains `E1AF35BE9552429AF329973CC3C3FB1C9478A32B`. C#/Markdown whitespace checks passed; normal Unity-generated scene/material YAML whitespace is retained. Source/meta pairs and original serialized references were preserved.

Read-only Git audit: HEAD remains **`07f46d5`**, staging index empty. Combined earlier M10 plus this expansion: **26 modified tracked files and 34 new scoped files**, all unstaged/uncommitted. Of the new files, **25 belong to this expansion**, including source/meta/material/verification files; 9 were already present from M10. Existing unrelated untracked `old-plan.md` and `tatus` remain untouched. The temporary unused material created while setting up restitution was removed only after checking it had no scene references. No Git write, checkpoint or push occurred.

Next step: developer reviews this expanded uncommitted M10 build, then personally chooses/stages/commits/pushes the review set. Stop after this expansion; no subsequent milestone or service integration is authorized.

## Previous handoff — initial Milestone 10

The developer explicitly authorized M10 in the current session and reported functional manual M9 gameplay. Earlier statements below that M10 was not authorized or M9 manual review was pending are historical. This session extends the existing **14-athlete** scene in place; it does not rebuild M9 or start a later milestone. Only the human performs Git writes.

### Architecture and implementation

- **PrototypeMatch** remains the only phase/transition authority. Added `FreeThrowSetup`, `FreeThrowReady`, `PenaltySetup`, `PenaltyReady`, and `PenaltyFlight`. `IsPlaying` still means only ordinary live play. `BallInMotion` additionally permits physical penalty flight/scoring/keeper contact; it does not advance clocks. Rule transitions synchronize score-observer samples, cancel input/charges and notify existing listeners.
- **MatchRules** classifies explicit illegal contact, tracks per-athlete foul history/eligibility and orchestrates protected restarts through PrototypeMatch. It caches the roster's collision state once. SwimmerIdentity exposes runtime history, while MatchRules alone mutates it. This is a prototype rule profile, not a complete officiating simulator.
- **BallController** remains the sole possession/flight authority. `TryAwardRestart` validates the rules taker and awards through its existing exclusive-owner path. Releases validate eligibility and restart action type; initial free-throw passes cannot score directly. Existing flat/lob flights, physical saves and goal detection are reused. Ball release/save/impact events feed presentation only.
- **PlayerSelection** retains acquisition-driven carrier selection, temporary human Blue keeper possession, ring/input exclusivity and camera retargeting. It never transfers ownership. Excluded/permanently removed athletes fail selection and action eligibility. During a friendly restart, control stays with the taker; opponent restarts permit a valid Blue field fallback.
- **PrototypeTeamTactics / WaterPoloDecision / PrototypeSwimmerAI** remain the shared team brain, intent model and physical action adapter. No additional AI or ball manager was introduced. Keepers remain outside field formations, marking and loose-ball pursuit.

### Contextual switching policy

`ContextualSelection.Score` is a separate, bounded scoring policy evaluated immediately on C. Defense scores estimated arrival using actual motor speed, movement toward the action, goal-side position and the team's primary presser. When another player already presses, off-ball candidates get a small lane-related adjustment. Loose-ball scoring uses predicted aerial landing/current ball position, arrival, momentum, designated pursuer and intended receiver. Attacking scoring favors open reachable progressive support and the player's role support position. The current athlete is excluded when another candidate exists.

Ties resolve by ordinal PlayerId. The most recently departed manual player receives a **1.5-point return penalty for 0.8s**, rather than an input delay or selection lock; urgent situations may still justify a return. `PlayerSelection.contextualSwitching=false` restores roster order; missing tactical context also falls back. Neither mode selects White or cycles goalkeepers. Legitimate Blue acquisition still selects the actual field recipient/keeper.

### Tactical improvements

- Shared assignments refresh at the existing **0.5s** interval or ownership change. One field presser is selected with incumbent advantage and recovery-side cost; other defenders get unique nearest-suitable marks, prioritizing threats nearer goal and favouring centre-back protection of the centre-forward. Incumbent marks receive hysteresis. Zone uses role stations with bounded passing-lane bias; man-down stations compact toward the defended goal. Unmarked threats are handled by danger priority rather than duplicate markers.
- Support lanes shift modestly with ball lateral position. Wings/other advanced roles progress during a **3s recovery transition**, and man-up width expands. The centre-back retains its role support offset. Carrier routes continue toward goal once clear; contested left/right alternatives need a pressure advantage before changing sides.
- Passing evaluates progress, openness and lane obstruction; immediate return passes to the last passer are penalized for **6s**. Counterattack passes receive a progress bonus. Existing goalkeeper-aware corner targeting, legal shot range/drag budget and pressure scoring remain in use.
- Shots/pass intentions commit for **0.45s** while valid, reducing aim/intent conflict. Maximum-hold and urgent-shot fallback respect physical range instead of forcing distant speculative shots. Movement still uses acceleration, arrival slowing, spacing, stamina and the same release APIs for both teams. These are geometric heuristics; sophisticated opponent prediction, deliberate AI off-ball holds and measured mobile balance remain future work.

### Rule sources and prototype adaptations

The [official World Aquatics regulations page](https://www.worldaquatics.com/rules/competition-regulations) currently links the **18 February 2026** edition. Its roughly 40MB PDF exceeded the browsing tool's fetch limit; local download was also unavailable. Do **not** claim a full 2026 rule audit. The readable reference was the World Aquatics June 2025 Part Six extract [hosted by the British Water Polo League](https://www.bwpl.org/uploads/2/6/5/9/26598283/wa_competition-regulations_june-2025_updated-25-06-2025_water_polo_extract.pdf), especially its ordinary/exclusion, penalty, throw and personal-foul articles. Reconcile the profile against the current full text before claiming current competition compliance.

That reference supports an 18-live-second exclusion, a third-personal-foul limit and a 5m penalty throw. These are reference values; the prototype intentionally compresses the period display while keeping exclusions and possession seconds independently paced. Reset/eligibility choices below are implemented mechanics, with full regulations and replacement procedures outside scope.

| Gameplay event | Implemented decision and behavior |
| --- | --- |
| F attempt rejected outside legal reach/facing/protection | No random foul. Normal failed attempts retain their previous result/cooldown. |
| Shielded rear F reach through the carrier's body within 1.1m | Explicit illegal-push event in this game profile; ordinary foul unless the narrow penalty test qualifies. Ordinary fouls are configurable and do not add personal fouls. |
| G intentional hold within 1.4m of an opponent, with that team in possession | Same command API available to human/AI. Off-ball hold awards exclusion; a hold of the carrier uses ordinary/qualifying penalty handling. AI currently uses F, not deliberate G holds. |
| Qualifying rear foul | Requires the actual carrier inside 6m, facing goal, a current human charge or AI Shoot intent, offender behind, and no eligible defending field player in the shooting corridor. Merely contacting a player near goal is insufficient. |
| Free throw | Captures ball location before clearing play; valid carrier/eligible field taker, goal-area relocation, 0.8s stopped setup, opposing clearance and frozen translation. B/L releases; direct shot/charge is blocked. AI aims/passes through gameplay APIs. After 6s an idle human also gets the same bounded automatic release. This pass-only profile and fixed protection/clearance are game simplifications. |
| Exclusion | Personal foul recorded, 18 independent live seconds by default, collider disabled, swimmer parked outside the pool near its current defended end. No ball actions, human selection, ordinary movement/tactics or stamina advancement. Live expiry, own-team recovery/awarded throw, or a goal permits temporary re-entry; permanent removal never expires. |
| Third personal foul | Athlete permanently ineligible for that match; history remains separate from temporary time. No substitute is invented because this roster has no replacement bench. |
| Penalty | Personal foul, short stopped setup, shooter at 5m, other field athletes outside 6m and at least 3m clear. Keeper uses a bounded goal-line station, then real hand contact/save chance. Human turns with movement input and uses charged Space; AI aims/shoots through normal APIs. Pass/lob and field motion are blocked until resolution. |

Exclusion removal/re-entry and legal restart placement are explicit administrative relocations, not live AI teleporting or unearned possession. The opponent clearance search includes an inward-route fallback at walls. Ordinary fouls do not refill the possession clock; exclusions raise it to at least the configurable 18-second restart value. Penalty catches give the defending team a fresh full possession; retained/loose attacking resolution uses the restart value. A goal uses the existing conceding-team restart. New match restores foul history/colliders; halftime retains history/time and moves waiting positions to the new defended end.

All three clocks freeze outside Playing, including **the entire penalty sequence**. During penalty flight only ball physics, keeper contact and the existing goal observer run; a catch or a **3s** flight limit returns to ordinary play without inventing possession. A miss/rebound becomes a live ball after that window. This isolated penalty flight, immediate foul whistle rather than delayed advantage, fixed automatic throw deadline, absence of substitutions, and lack of a comprehensive rebound/out-of-play/6m direct-free-shot model are deliberate prototype limitations. No claim is made that every official foul category is implemented.

### Presentation and audio

MatchAtmosphere listens to authoritative rules/match/ball events. The existing uGUI Canvas gains a two-message referee feed plus selected cap/personal fouls, active field counts/man-up, and excluded cap numbers/remaining time or OUT. Score, period, possession clock, Z defense, stamina and power bar remain. Labels update on events or at 5Hz; geometry/objects are not created during gameplay.

An AudioSource uses event-driven whistle, goal, save, impact, period and final cues. Four small original mono procedural clips are synthesized once and destroyed with the component; optional licensed clips and an optional AudioMixerGroup can override/reroute them. Optional crowd audio plays only on goals at reduced gain. Missing clips are safe; impact audio is throttled. Sound never schedules or changes gameplay. No downloaded audio, crowd rendering or rigged referee was added. Audible quality, overlapping-cue balance and Android audio latency need manual/device review.

### Burst / Windows investigation

Pinned Editor **6000.6.7f1** resolves built-in **com.unity.burst 2.0.0** (`type: shim`), confirmed in packages-lock, PackageCache package.json and the Editor's built-in package directory. Old dependency minimums referencing 1.8.x do not establish that 1.8.x is the resolved version. No package or Editor upgrade was made.

The live Pipeline Console journal records Burst JIT DLLs under Library/BurstCache blocked by **Windows code integrity policy (Smart App Control / WDAC)**, explicitly reported by Unity. This establishes a compatibility/enforcement symptom, not evidence of malware or proof of which policy is responsible. Compilation and Play Mode verification succeed despite those recorded blocks. [Microsoft's Smart App Control explanation](https://learn.microsoft.com/windows/apps/develop/smart-app-control/overview) describes blocking unknown unsigned binaries; this is consistent with the observed JIT behavior. No verified package-specific fix was found, and changing to arbitrary package versions is unjustified for this built-in shim.

Safe manual follow-up if the warning recurs: retain the exact warning and inspect **Event Viewer > Applications and Services Logs > Microsoft > Windows > CodeIntegrity > Operational** around that timestamp, plus Windows Security's block details. Report Editor/Burst versions and policy/event details to Unity. As a diagnostic, the documented **Jobs > Burst > Enable Compilation** toggle can disable Editor Burst compilation without disabling Windows protection; confirm that menu in this Editor and expect different Editor performance. The available [Unity menu reference](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/editor-burst-menu.html) describes the 1.8 menu and distinguishes Editor from Player settings; applicability to built-in 2.0 should be confirmed manually. Do not disable Smart App Control, whitelist unsigned DLLs, alter policy/registry, delete Library or sign changing cache DLLs as a guessed fix. No security settings were changed. The CLI version-update check could not reach its CDN; the installed CLI connected normally.

### Files changed

Created, each Unity script with its `.meta`: `Assets/Scripts/Gameplay/ContextualSelection.cs`, `MatchRules.cs`, `MatchAtmosphere.cs`; `Assets/Editor/Milestone10PrototypeSetup.cs`. Created verification source: `Tools/Verification/Milestone10Verification.cs` (outside Assets/player builds).

Modified runtime files under `Assets/Scripts/Gameplay/`: `BallController.cs`, `BallInteractionInput.cs`, `GoalkeeperInterception.cs`, `PlayerSelection.cs`, `PossessionShotClock.cs`, `PrototypeGoalkeeper.cs`, `PrototypeMatch.cs`, `PrototypeRoster.cs`, `PrototypeSwimmerAI.cs`, `PrototypeTeamTactics.cs`, `ShotCharge.cs`, `SwimmerIdentity.cs`, `SwimmerMotor.cs`, `SwimmerPassing.cs`, `SwimmerSteal.cs`, `WaterPoloDecision.cs`. Modified setup helper: `Assets/Editor/MatchHUDSetup.cs`. Saved scene: `Assets/Scenes/PrototypePool.unity` (additive wiring/HUD/components; identities and existing tuning retained). Modified `Tools/Verification/Milestone8Verification.cs` to isolate extra full-team outlets in its keeper passing fixture; physical reception assertions remain. Modified `GoalScoringPrototypeVerification.cs` to assert the already-established M9 conceding-team owner/current roster starts rather than historical M3 neutral-ball/zero-position restarts; goal geometry/exact-once assertions remain. Updated `AGENTS.md`, `README.md` and this handoff. No package, SampleScene or intended project-setting change belongs to M10.

### Verification and remaining manual review

Fresh compilation and **12 distinct verification entries passed** in the running **6000.6.7f1** Editor. Canonical results are the twelve entry-specific `Logs/M10-Final-*.json` files named below; earlier shorter-name duplicates are intermediate records, not additional coverage. One consolidated regression was run near completion, pausing only to adapt the two historical fixtures described above. The final bounded keeper-return correction was then checked by PenaltiesAndIntegration and KeeperControl; the full regression was not needlessly repeated. Historical results elsewhere in this file are not fresh M10 evidence.

| Entry | Fresh evidence |
| --- | --- |
| `Milestone10Verification.Setup` | Fourteen identities preserved, one rules/presentation authority, contextual wiring, repeat setup adds no objects. |
| `Milestone10Verification.SelectionAndTactics` | Injected actual C selects useful goal-side athlete over remote roster successor; loose-ball route, ownership preservation, auto-acquisition, off-ball control, camera, White rejection, roster fallback, five unique stable marks plus presser and distinct Press/Zone geometry. |
| `Milestone10Verification.FoulsAndExclusions` | Real close rear F command, charge cancellation, no duplicate stopped decision, protected pass-only free throw, both clocks frozen, preserved near-expiry clock turning over once after release, autonomous White free throw, three personal fouls, disabled excluded collider/actions/selection, timed re-entry, permanent third removal, new-match reset, active exclusion/history/waiting end across halftime. |
| `Milestone10Verification.PenaltiesAndIntegration` | Near-goal contact alone rejected as penalty, genuine human and AI shooting-action qualification, legal other-field clearance, real physical keeper save/exclusive catch, clock reset, no keeper pool-clamp teleport, autonomous AI physical penalty goal/exact-once score, goal-triggered temporary re-entry, 12s integrated fourteen-athlete play with bounded stamina/eligible actors and exclusive possession. Two live ordinary-foul decisions occurred in the final integrated run. |
| `Milestone8Verification.ChargedShooting` | Real hold/release/tap input, capped physical shot, no duplicate release, charge bar, possession/switch/stoppage cancellation and immediate AI API. |
| `Milestone8Verification.KeeperControl` | Blue catch/ring/camera/input, human swimming/charged/lateral shots, physical outlet/reception, field-only C, lock/loss recovery, steal cancellation and autonomous White keeper distribution. |
| `Milestone9Verification.Aerial` | Physical height, high pickup rejection, legitimate reception, landing, above-bar rejection and transition cleanup. |
| `GoalkeeperSaveVerification.Run` | Both high-speed catches, exclusive ownership, reachable/missed shots, physical deflection, glancing goal, seeded skill/reaction endpoints and no duplicate contact reroll. |
| `GoalScoringPrototypeVerification.Run` | Both whole-ball planes, held/reverse/post/crossbar rejection, physical misses, fast/long goals, score/HUD, repeated exact-once conceding restarts and current roster starts. |
| `TacticalOffenseVerification.Run` | Actual White pickup/turn/shot/goal, carrying advance, improved-option physical pass/reception and configured anti-stall release. |
| `FullTeamMilestone9Verification.GoalsAndKeepers` | Five physical goals including post-halftime own goal, two catches/distributions at new ends, conceding ownership, fourteen reset positions. |
| `FullTeamMilestone9Verification.TimingAndEnds` | Four 80/480 periods, stopped clocks, one Q2 switch, all fourteen physical relocations and new-match restoration. |

Live frame advancement was proved with the Pipeline frame-count wait (not inferred from Play Mode entry). The Game-view referee capture is `Logs/M10-FreeThrow.png`; visual review caught and corrected a feed/clock overlap in the actual saved HUD, whose clock row had custom centred anchors. Feed placement now sits below that row. Audio hooks/procedural clips are source/compile verified; audible quality was not manually tested. C#/Markdown whitespace checks pass. Unity's serializer adds ordinary blank-value trailing spaces in scene YAML. No historical fixture assertion was removed merely to make a run pass.

Fresh Console ground truth after gameplay verification: **0 errors, 0 warnings, compilationFailed=false**. Burst/code-integrity messages remain in the earlier Pipeline journal; these are not the current Console count. No Console clearing was used to hide failures. Final Editor settings/status and read-only Git inventory are recorded at the end of this handoff after cleanup. The live Editor menu listing confirmed **Jobs > Burst > Enable Compilation** exists in built-in 2.0.0; it was inspected, never toggled.

Manual steps after opening **Assets > Scenes > PrototypePool**:

1. Press Play and focus Game. Use WASD/arrows, Shift, E, B/L, charged Space, C, F and Z. Verify ordinary possession, shots, saves, stamina and camera. With an opponent carrying or a loose ball, tap C repeatedly and judge the tactical choices; friendly receptions must still select the real carrier.
2. While defending, approach just behind a carrier and use F at close body reach. Check FOUL, whistle, cancelled charge where applicable, stopped clocks and protected free throw. Misses at ordinary reach must not randomly become fouls. As a Blue taker, aim in place and press B/L; Space should not fire a direct free-throw shot. Wait through a White free throw to verify autonomous resumption.
3. With White in possession, move beside an off-ball White athlete and press G. Check Blue cap identity, PF increment, reduced field count, non-blocking waiting position, unavailable C selection, man-down shape and countdown only during live play. Verify re-entry after expiry, recovery and goals. Repeat three personal fouls on the same cap: OUT must survive time and later goals.
4. For a qualifying penalty, use a clear shooting chance inside 6m: a carrier facing goal, charge/AI shooting action, and an illegal rear reach within body range with no intervening field defender. This requires positioning; mere nearby contact should produce an ordinary foul. On the award verify 5m shooter, clear other fields, stationary aiming/Space shooting, physical keeper save or goal, single scoring and reliable resumption. The consolidated verification source supplies reproducible automated fixtures for both teams if manual positioning is difficult.
5. Continue through Q1/Q2, verify the halftime goal/keeper/formation swap happens once and preserves foul history. Finish all four periods and restart the match; scores, ends, exclusions, OUT status, colliders and personal fouls must reset. Check a goal during man-up and a nearly expired shot clock during a free-throw setup.
6. Review referee feed/HUD readability and sound volume at the intended landscape aspect ratio. Device-test Android CPU/GPU/allocations/audio/thermal behavior separately; no device/build performance claim is implied. Later milestones remain unauthorized.

Known limits: narrow, command-based foul recognition rather than full physical grappling; no automatic sanctions for ordinary swimmer overlap, deliberate AI off-ball holding, substitute bench, official advantage delay, bonus/team-foul/violent-conduct categories, neutral referee disputes or complete out-of-play handling. Re-entry uses an administrative placement rather than an animation. Penalty rebounds wait for the bounded isolated flight to end; human goalie movement during an opponent's penalty is AI-managed. The HUD is a prototype landscape layout and can become crowded with many simultaneous exclusions. Tactical quality/balance, long matches, every simultaneous period/foul edge and Android/iOS builds remain manual/device work. Do not describe this as full official 2026 rules compliance.

Next step: developer manually reviews M10, then personally chooses/stages/commits/pushes the relevant files. No later milestone begins automatically.

Final audit: Editor left **stopped**, Run In Background restored **off**, auto-tick disabled, generated Pipeline config moved to ignored `Logs/M10-EditorPipelineConfig.json`, and the temporary persisted Run In Background override restored in place. No package/ProjectSettings/SampleScene diff remains. Final Console snapshot: `Logs/M10-Final-Console.json`, 0 errors/0 warnings, compilationFailed=false; final Editor snapshot: `Logs/M10-Final-Editor.json`. The post-layout capture was inspected again and the feed no longer overlaps the clock row. No full mobile aspect-ratio matrix or audible/device test was performed.

Read-only Git audit: HEAD **`07f46d5` — Complete full 6v6 water polo match prototype - Milestone 9**. Staging index is empty. M10 leaves **23 modified tracked files and 9 new source/meta/verification files**, all unstaged/uncommitted. Pre-existing untracked `old-plan.md` and `tatus` remain untouched and excluded from the M10 file inventory. No Git write operation, checkpoint, security-setting modification or later-milestone work occurred. The script/meta inventory above is the scoped review set; generated Logs and unrelated local files are not backup candidates.

This document describes the NEW 3D project. Distinguish confirmed implementation from goals and user-reported setup. Update after meaningful development tasks with changes, tests, issues, and next steps. Do not import the previous game's historical logs.

## Identity and vision

- Game: Water Polo Superstars. Studio: ToduStudios. Unity organization: TODU.
- Studio email: todustudios@gmail.com. Game email: waterpolosuperstars@gmail.com.
- Genre: 3D mobile water polo sports simulation.
- Primary release: Android / Google Play. Secondary: iOS / App Store.
- Solo developer assisted by ChatGPT, Codex, Unity AI, and Blender.
- Presentation target: attractive normal gameplay inspired by Dream League Soccer, within realistic solo-development scope and mobile performance limits.
- Long-term goals: believable athletes, swimming/passing/shooting/blocking/goalkeeper animations, water and splashes, arena, responsive controls, tactical AI, cameras, cinematic goals/replays, career, championships, World Cup, transfers, cards, progression, and monetization. These are ambitions, not completed systems.

Build the new game around its own requirements. The old game is an optional reference, not the foundation or a mandatory migration target. The implemented prototype uses XZ pool-plane tactical calculations with separate 3D movement/presentation; useful legacy tactical concepts inform this separation without importing Rigidbody2D/Collider2D code.

## Project location and creation

- Main project: `C:\Users\todua\UnityProjects\WaterPoloSuperstars`.
- Created on the desktop through Unity Hub as a fresh Unity 6.6 project, using the Universal 3D template and URP.
- Editor: `6000.6.7f1`, verified in `ProjectSettings/ProjectVersion.txt` during this update.
- Successfully opened in Unity and VS Code, according to the developer.
- Normal project directories: `Assets`, `Packages`, `ProjectSettings`. Generated folders such as `Library`, `Temp`, `Logs`, and `UserSettings` must remain excluded from Git.
- Root documentation: `AGENTS.md`, `README.md`, and this `plan.md`; setup also includes `.gitignore` and `.vsconfig`.

## Hardware and test devices

Primary desktop:

| Component | Hardware |
| --- | --- |
| CPU | Intel Core i5-12600K |
| GPU | NVIDIA GeForce RTX 5060 Ti, 8GB VRAM |
| RAM | 32GB DDR4, 3000 MT/s |
| SSD | PNY CS2340 1TB NVMe |
| OS | Windows 11 Pro |

Installed tools reported: Unity Hub/Editor, VS Code, Blender, Git for Windows, Node.js/npm, Codex CLI, and Unity CLI.

Travel laptop: HP EliteBook, Intel Core i7-7500U, Intel HD Graphics 620, 8GB DDR4 at 2133 MHz, Samsung MZVLW256HEHP-000H1 256GB SSD. Use for documentation, Git, lightweight coding, and limited Unity tasks; do not rely on it for heavy 3D work.

Available phone: iPhone 16. Google Pixel 10a is a planned Android test device; acquisition is unconfirmed. Android device testing, including weaker hardware, remains necessary. Desktop performance does not establish mobile performance.

## Reported Unity and Android setup

The foundation inspection confirmed the saved identity, Android API/backend/architecture/input settings, Visible Meta Files and Force Text values below. Android SDK/NDK/OpenJDK directories exist and the Editor log records the Android profile as active; no Android build has been tested. Other setup history remains developer-reported:

- Company: `ToduStudios`; product: `Water Polo Superstars`; version: `0.1.0`.
- Android Build Support, SDK/NDK tools, and OpenJDK installed; Android build profile active.
- Application ID: `com.todustudios.waterpolosuperstars`; bundle version code: `1`.
- Minimum API 26 / Android 8.0. Target API: automatic, highest installed. Verify store requirements near release.
- IL2CPP, ARM64 enabled, Input System Package (New).
- Visible Meta Files and Force Text serialization configured.
- Unity AI Assistant and Unity CLI enabled during creation. A previous Unity AI `NoSubscription` error concerns that service, not proof of a broken project.

## Git and tools

- GitHub account: `Nikoloz-Todua`.
- Public repository: https://github.com/Nikoloz-Todua/WaterPoloSuperstars
- Remote: `https://github.com/Nikoloz-Todua/WaterPoloSuperstars.git`, verified locally during this update.
- Default branch reported: `main`.
- Initial pushed commits reported: `9c3204b` (Unity setup), `0eab526` (documentation/instructions). Always check current status; a previously clean tree is not a current guarantee.
- Do not initialize another repository inside this one. Preserve Unity `.meta` files and avoid unrelated edits. Existing checkpoint entries below are historical records, not authorization for an AI to create another checkpoint.
- **Only the human developer may perform Git write operations.** AI may inspect read-only status/diffs/history/remote state and edit authorized project files. It must NEVER stage, commit, push, fetch, reset, rebase, merge, stash, alter branches/refs/index/configuration, or use equivalent APIs/Unity integrations to change Git state. Previous command approvals do not supersede this rule. Give the human exact commands for backups/checkpoints; never execute them.
- Human workflow: inspect status/diff, stage only reviewed files, inspect the staged contents, commit using the existing identity, push to the reviewed remote/branch, then compare local and remote commit IDs. Never upload caches, secrets or local legacy references. Do not automatically change `.gitignore`.

Setup-time versions reported: Git `2.56.0.windows.4`, Node.js `v26.13.0`, npm `11.20.0`, Codex CLI `0.166.0`, Unity CLI `1.0.0-beta.15`. These are historical values, not guaranteed current installations.

Codex was authenticated with the developer's ChatGPT account. `unity setup codex` installed the Unity plugin, reporting `0.1.8-beta` at setup; installed plugin/skills may change later. Launch Codex from this project folder with `codex`, or `codex.cmd` if PowerShell blocks the `.ps1` launcher. External Codex and Unity AI services are separate.

## Current implementation and verification

Milestones 1-8 are implemented and have the developer's manual confirmations below. Milestone 8 was manually tested successfully and backed up as `734d8da` before Milestone 9. **Milestone 9 — complete full-team prototype expansion** now contains six fields and one goalkeeper per team, six tactical roles, four compressed periods, halftime ends switching, possession clocks, shared sprint/stamina and real aerial lobs. Its fresh automated evidence is below; developer manual gameplay/device testing remains pending. Earlier results are historical and do not establish exhaustive manual edge-case coverage. SampleScene and the old 2D project are preserved.

Current build: **six Blue fields, six White fields, one keeper per team (14 athletes)**. All six Blue fields are selectable; Blue keeper acquisition grants temporary human control, and White stays AI-controlled. X is width, Z is pool length/tactics, Y is height including real lob flight. Blue initially attacks +Z; Continue after Q2 reverses both teams' ends for Q3/Q4. New matches restore the originals. BallController owns possession/flight, PlayerSelection human control, ShotCharge human shooting, PrototypeTeamTactics/WaterPoloDecision field tactics, SwimmerMotor physics/stamina, and PrototypeMatch/GoalMouth phases/scoring/restarts. MatchClock defaults to four periods of **80 live seconds / 480 displayed seconds (6×)**; PossessionShotClock independently defaults to **28 live seconds at rate 1**. Both advance exclusively through live PrototypeMatch steps. Keeper behavior and HUD remain separate components.

**Current manual confirmation (developer-reported):** swimming, camera follow, passing, shooting, F stealing, friendly attacking support, active opposing swimmers, scoring by both teams and scoreboard updates. The developer reports a gameplay screenshot showing `BLUE 1 - 2 WHITE`; this is their reported playtest evidence, distinct from the earlier automated preview capture. Do not infer manual validation of every selection, cooldown, missed-shot, restart, duplication or other edge case from this list.

**Automated evidence:** Milestones 6/7/8 recorded their passing suites below. Milestone 9 uses one reusable focused verification script and a selected consolidated regression; see its section for actual fresh results. Earlier two-field/six-athlete counts, free-play clocks and AI-only Blue keeper expectations are historical rules, superseded where explicitly described by later milestones. Milestone 8 manual gameplay is confirmed. Milestone 9 manual game feel, exhaustive device/aspect-ratio checks and mobile performance remain pending.

### Milestone 1 files

- `Assets/Scripts/Gameplay/PoolBounds.cs`: configurable axis-aligned rectangle (default width 20m, length 30m), waterline and position clamping; independent of decorative walls.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: kinematic 3D Rigidbody movement in FixedUpdate with interpolation, acceleration/deceleration, limited turning, normalized direction and boundary clearance. Public `SetMoveInput(Vector2)` accepts world X/Z direction from a human adapter or future AI. Actual planar `Velocity` is available for future animation. No future gameplay systems were added.
- `Assets/Scripts/Gameplay/SwimmerInput.cs`: clones and owns only the existing `Player/Move` action; WASD/arrows and analogue gamepad input. Input lifetime is separate from shared assets and the movement motor.
- `Assets/Editor/MovementPrototypeBuilder.cs`: explicit `Tools > Water Polo > Create Movement Prototype` command. Creates and saves the separate scene and references. Repeated runs open the existing scene without rebuilding/duplicating it. Refuses Play Mode or unsaved scene changes. Opens the prototype alone to avoid duplicate cameras/listeners; never saves or overwrites SampleScene.
- `Assets/Materials/Prototype/{Water,Floor,PoolEdge,Swimmer,Cap,Skin,Markings}.mat`: seven lightweight URP materials.
- `Assets/Scenes/PrototypePool.unity`: Environment (floor/water/boundaries/markings), Actors/Swimmer with separate Visual child, Gameplay/PoolBounds, CameraRig/Main Camera, Lighting/Directional Light.
- `Tools/Verification/MovementSandboxVerification.cs`: reproducible Editor Play Mode integration checks invoked through Unity Pipeline; outside Assets so it is not included in player builds.
- Unity-generated `.meta` files accompany the new Assets files and folders. No packages or build settings were changed.

### Confirmed architecture and scope

Pool gameplay uses world X for width, Z for length, Y for height; origin at pool centre, one unit approximately one metre. Future goals will sit at opposite Z ends. Swimming root height is fixed by PoolBounds; visual geometry is replaceable independently of the physics/input root.

The swimmer uses a horizontal CapsuleCollider and conservative 1.1m boundary clearance, so turning does not take its body over the edge. Rectangular clamping, not decorative wall colliders, enforces the bounds. This milestone does not implement swimmer-to-swimmer obstacle avoidance or collision resolution.

Milestone 1 used a fixed full-pool overview camera; Milestone 2 supersedes that camera as described below. Water is an opaque blue unlit placeholder, with a separate floor beneath it; no fluid simulation or water effects. Temporary prototype palette: blue pool, pale edges, coral swimmer and yellow cap. These are readability choices, not final art direction. Detailed animation, Cinemachine, audio, menus, online services and visual polish were outside Milestone 1.

### Verification performed in Unity 6000.6.7f1

- Direct Editor connection verified for this project; scripts compiled with `compilationFailed=false` and no compiler errors.
- Builder ran successfully and saved the scene/materials; repeat invocation retained one swimmer and existing scene content.
- Actual Play Mode with advancing frames/physics: injected Input System keyboard and gamepad devices exercised WASD movement, arrow input, analogue strength, acceleration, release/deceleration, stopped drift, facing, all four boundaries under sustained input, and waterline correction.
- Straight speed: approximately 3.999998m/s; diagonal speed: approximately 4.000003m/s, both matching the configured 4m/s maximum.
- Camera viewport checks contained all four pool corners at the current Game view aspect; a 1280x720 camera capture was visually inspected.
- Console ground truth after runtime checks: zero errors, zero warnings, compilation not failed. Verification helper also compiled without diagnostics after replacing deprecated object lookup calls.
- `SampleScene.unity` content hash stayed `1c63aa8ded8c325623e162675b13bfcd0b50aa5a` (Git blob hash).
- Play Mode stopped after checks; temporary Editor background-run/auto-tick overrides disabled. Capture and transient automation helpers are kept only under ignored Logs/Library paths.

Developer manual Play Mode verification after Milestone 1: WASD, turning, stopping, pool containment and opening/running PrototypePool all work. The full-pool camera makes the swimmer far too small; a closer gameplay camera is the next approved improvement. Physical gamepad feel, other screen aspects, Android builds/device performance and iOS remain unverified. No claim of mobile readiness is made.

### Milestone 1 usage and original limitations

Open `Assets/Scenes/PrototypePool.unity` from the Project panel, press Play, click the Game view and use WASD/arrows or a gamepad left stick. Existing scene creation is already complete; the menu command is optional and preserves existing work. Save any dirty scenes first.

Inspector tuning: `Gameplay/PoolBounds` controls Width, Length and Waterline; `Actors/Swimmer > Swimmer Motor` controls speed, acceleration, deceleration, turn speed and clearance. Changing playable bounds does not resize the decorative pool or reframe the fixed camera. Keep clearance appropriate to any replacement collider/visual size. The builder deliberately does not regenerate edited scenes.

Original Milestone 1 limitations: placeholder presentation; no touch controls, ball, goals/scoring, AI, sprint/stamina, or animations. Milestone 2 adds the ball below. Project orientation still allows portrait rotation; landscape configuration belongs to a later approved mobile-control task.

Milestone 1 passed the developer's manual movement review and is recorded in local checkpoint `a8312f6` (`Checkpoint manually verified Milestone 1 movement sandbox`). It includes the implementation, materials/scene/metas, verification tool and relevant documentation. It excludes `old-plan.md`, raw legacy references, logs and generated folders. No push was made.

### Milestone 2 — Gameplay camera and basic ball interaction

Implemented and saved in the existing PrototypePool scene; no duplicate swimmer or environment was created. The original `SwimmerMotor.cs`, `SwimmerInput.cs` and `PoolBounds.cs` are unchanged from Milestone 1.

Files added:

- `Assets/Scripts/Gameplay/GameplayCamera.cs`: fixed-angle perspective camera following the interpolated controlled swimmer in LateUpdate with SmoothDamp and velocity-based forward lead. Inspector settings: Distance, Height, Pitch, Yaw, Smooth Time, Look Ahead, Bounds Inset and Outside Margin. Defaults are distance 6.5m, height 7.5m, pitch 50 degrees; camera FOV is 50 degrees. The camera is deliberately one small component for this bounded prototype; no Cinemachine dependency, replays or effects were added.
- `Assets/Scripts/Gameplay/BallController.cs`: sole ownership authority and Free/Held/Released state machine. Atomic range-checked pickup uses the physics position. Held ball is kinematic with collider disabled and follows the hand anchor smoothly in LateUpdate. Release clears ownership and applies configurable flat velocity; 0.6s protection precedes return to Free. The previous owner's collision is ignored until protection has expired and the colliders are separated. Dropping/disabling a carrier clears ownership.
- `Assets/Scripts/Gameplay/SwimmerBallInteraction.cs`: movement-independent commands (`TryCollect`, `TryShoot`, `HasBall`), assigned ball and carry anchor, configurable 2m pickup range, 12m/s shot speed and 1.5m release distance. These commands can later be called by touch or AI. One assigned ball is sufficient for this prototype; no global manager or singleton.
- `Assets/Scripts/Gameplay/BallInteractionInput.cs`: private new-Input-System actions; E collects, Space shoots. Optional gamepad buttons: West collects, South shoots. Shoot takes priority over simultaneous pickup. The existing movement action asset is untouched.
- `Assets/Editor/CameraBallPrototypeSetup.cs`: `Tools > Water Polo > Set Up Camera and Ball Prototype`. Reuses the existing scene and components, creates only missing ball/anchor/components, assigns references and saves. Repeated runs preserve tunable values and retain exactly one ball. Refuses Play Mode or unsaved scene changes. Never modifies SampleScene.
- `Assets/Materials/Prototype/Ball.mat`: yellow URP Lit ball material, plus all new asset `.meta` files.
- `Tools/Verification/CameraBallPrototypeVerification.cs`: repeatable injected-input Play Mode checks, outside Assets/player builds.

Files modified: `Assets/Scenes/PrototypePool.unity`, `Tools/Verification/MovementSandboxVerification.cs` (the obsolete full-pool camera assertion now checks swimmer visibility), and this `plan.md`.

Scene additions: `Actors/Ball` (unit-scale physics root, SphereCollider radius 0.14m, 0.28m yellow sphere Visual child), `Actors/Swimmer/BallCarryAnchor` (local 0.45, 0.07, 0.85), swimmer interaction/input components, and GameplayCamera on `CameraRig/Main Camera`.

The free/released ball uses a dynamic 3D Rigidbody with no gravity, frozen vertical position, water drag and continuous collision detection. Its centre is 0.07m above PoolBounds.Waterline, so the sphere meets the placeholder surface. Rectangular bounds constrain the ball and damp/reflect its outward velocity; decorative pool edges remain nonphysical. The flat-water model is explicitly a prototype, to be extended or replaced before lob/skip/airborne shots.

Camera bounds constrain the follow focus and an estimated ground footprint of the inner 70% of the viewport, allowing a configurable deck margin. This limits excess outside views while keeping the swimmer visible at corners. It is not a promise that every screen pixel remains over water. Extreme Inspector settings/aspects need manual review; near-horizon pitch falls back to bounded focus.

### Milestone 2 verification and handoff

Verified in Unity 6000.6.7f1 with direct Unity CLI/Pipeline access:

- Runtime/editor scripts compiled successfully; no compilation failure. New verification helper compiled without diagnostics.
- Scene setup executed and was repeated: exactly one ball, one swimmer, valid assigned references; no duplicate environment.
- Movement regression passed: WASD/arrow input, analogue gamepad strength, acceleration/deceleration, diagonal speed approximately 4m/s, facing, waterline and all four swimmer boundaries.
- Actual Play Mode with advancing frames and physics: Space without possession does nothing; out-of-range E is rejected; nearby E collects; ball reaches hand anchor; a second valid claimant is rejected; held collider is disabled.
- Swimming while carrying works; ball follows within the expected smoothing lag. Space releases the ball in the swimmer's facing direction with dynamic physics, rejects immediate re-collection and holds the waterline.
- Released ball becomes Free after cooldown and can be collected again with E. Disabling the carrier drops ownership. Ball containment/floating passed at all four edges with injected 80m/s velocities.
- Camera follows movement with forward lead, responds partially rather than snapping to a 3m target displacement (about 0.23m in the early sample), settles, and keeps the swimmer within the viewport at every pool corner.
- A 1280x720 centre-pool camera capture was visually inspected: the swimmer is substantially larger than in Milestone 1 and the yellow ball is readable. Capture is stored under ignored `Logs/Milestone2CameraBall.png`.
- Console ground truth after checks: zero errors, zero warnings, compilation not failed. SampleScene retains blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.
- Play Mode stopped after checks; temporary Editor auto-tick/background-run overrides disabled. Temporary capture/config artifacts moved out of Assets/ProjectSettings into ignored Logs. No packages, persistent project settings or build scene list were changed.

Developer manual Milestone 2 results: WASD/turning, smooth camera follow, E pickup, carrying while moving, Space shooting and re-collection all work correctly. Camera is somewhat too close; a wider FIFA/Dream League Soccer-style broadcast view is a future task, not the priority for Milestone 3. Gamepad interaction buttons, different screen aspects and mobile hardware remain unverified. Android builds, touch UI, device performance and iOS were not tested.

To test: open `Assets/Scenes/PrototypePool.unity`, press Play, click the Game view, use WASD/arrows, press E near the initial ball, swim/turn while carrying, press Space to shoot, then approach the loose ball and press E again after the short cooldown. Camera tuning is on `CameraRig/Main Camera > Gameplay Camera`; pickup/shot tuning is on `Actors/Swimmer > Swimmer Ball Interaction`; drag/cooldown tuning is on `Actors/Ball > Ball Controller`.

Milestone 2 scope limits: flat shots only; conservative release distance and capsule clearance are tuned to this placeholder swimmer; changing collider/model size requires retuning. One ball/controlled swimmer, no goalkeeper, teams of actors, shot clock, shot charge, lob/skip shots, touch UI or replays. Milestone 3 adds goals/scoring below. Pool visuals do not auto-resize when playable dimensions change.

Milestone 2 passed manual review and is recorded in local checkpoint `2ca3909` (`Checkpoint manually verified Milestone 2 camera and ball interaction`). It includes its scene, scripts, material, verification and manual-results documentation. It excludes legacy references and generated files. No push was made.

### Milestone 3 — Goals, scoring and repeatable restart

Implemented in the same PrototypePool scene. The camera script/settings and movement/input behavior are preserved. The user's request for a wider FIFA/Dream League Soccer-style broadcast camera remains a future task; this milestone does not redesign the camera.

New code:

- `Assets/Scripts/Gameplay/GoalMouth.cs`: independent goal geometry and `PrototypeTeam` (Blue/White). Unit-scale root uses local +Z into the net; configurable clear width (3m), height (0.9m), depth (1m) and post thickness (0.1m). The scoring plane is local Z=0, with a named ScoringPlane child and selected-object gizmo. Swept sphere tests count only outward crossings where the whole sphere passes the plane and clears posts/crossbar; reverse crossings and tangent post contact are rejected.
- `Assets/Scripts/Gameplay/PrototypeGoalView.cs`: separate replaceable placeholder view. Updates the frame, back/side net colliders, lattice net strands, pocket water and split end-boundary sections when dimensions change. Goal dimensions are on GoalMouth, independent of a future detailed goal model. Goal roots must retain unit scale and the documented axis convention.
- `Assets/Scripts/Gameplay/PrototypeMatch.cs`: one score authority with two score values, per-shot deduplication, physics-path observation and restart coordination. Samples actual Rigidbody positions before ball containment; a new shot starts its path at the recorded release point, so pickup/release/reset position changes cannot fabricate a crossing. Positive-Z net credits Blue; negative-Z net credits White, including an own-goal-equivalent shot into that net. No shooter/team roster attribution is implemented.
- `Assets/Scripts/Gameplay/PrototypeScoreboard.cs`: event-driven TextMeshPro scoreboard and GOAL notification; no per-frame text rebuilding.
- `Assets/Editor/GoalScoringPrototypeSetup.cs`: `Tools > Water Polo > Set Up Goals and Scoring Prototype`. Adds/reuses goals, references, restart markers, score authority, HUD and materials; preserves existing swimmer/pool/camera objects. Refuses Play Mode or unsaved scenes; saves only PrototypePool. No duplicate objects on repeat invocation.
- `Tools/Verification/GoalScoringPrototypeVerification.cs`: repeatable Play Mode scoring/restart checks outside Assets/player builds.

Modified code/assets:

- `Assets/Scripts/Gameplay/BallController.cs`: retains ownership/state authority; adds configured goal pockets, release-shot eligibility/sequence/origin, restart pause and reset API. The Released cooldown may transition to Free while a long shot remains scoring-eligible; a pickup, drop or reset clears eligibility. Release spawning stays clamped to the playing side, so only actual travel into the goal scores. Goal pockets permit valid paths beyond the pool end; all other containment remains. Fast valid shots reach the bounded pocket rather than reflecting prematurely before the line.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: adds `ResetPose` to clear movement intent/velocity and reset a clamped pose. Ordinary movement/turning is unchanged.
- `Assets/Scenes/PrototypePool.unity`: new goals, split end geometry, restart markers, score authority and HUD. Original NearEdge/FarEdge objects are retained with their uninterrupted slab renderers disabled; split solid sections close the rest of each end. Existing side bounds remain.
- `Tools/Verification/CameraBallPrototypeVerification.cs`: closed-end containment checks now target X=4m, outside the intentional goal opening.
- `Assets/Materials/Prototype/GoalFrame.mat`, `GoalNet.mat`, and new asset `.meta` files. White solid frames, back/side colliders and simple lattice nets; no external goal models.
- `Assets/TextMesh Pro/` and its `.meta`: standard TMP Essential Resources (fonts, materials, shaders/settings and accompanying resources) imported through the installed package's non-interactive importer. No TMP examples, new packages or external model/art imports.

Scene locations:

- `Environment/Goals/BlueScoringGoal`: goal plane at world Z=+15m, root Y=-0.08m; local +Z points outward. WhiteScoringGoal is at Z=-15m with Y rotation 180 degrees. Both are centred at X=0.
- `Gameplay/PrototypeMatch`: authoritative scores and configurable 1.5-second restart delay.
- `Gameplay/RestartPositions/{SwimmerStart,BallStart}`: saved centre swimmer pose and initial nearby collection position.
- `PrototypeHUD`: screen-space overlay Canvas with responsive scaler, dark score panel (`BLUE 0 - 0 WHITE`) and temporary GOAL text. It is passive UI; no EventSystem is required for these non-interactive labels.

Restart locks further scoring, pauses ball simulation, and temporarily disables swimmer movement/input and ball input. After the unscaled configurable delay it clears possession, shot eligibility, velocities and input intent, resets both actors to the start markers, restores previously enabled controls, hides GOAL and preserves the score. Scores reset only when a new Play Mode session begins. No match clock/quarters were added.

### Milestone 3 verification and handoff

Verified in Unity 6000.6.7f1:

- Runtime/editor compilation completed with no errors. All three verification scripts compiled without diagnostics and ran in actual Play Mode with advancing physics.
- Movement regression passed (WASD/arrows, analogue input, normalized diagonal speed, turning, stopping, fixed height and swimmer containment).
- Milestone 2 camera/pickup/carry/shoot/cooldown regression passed. Camera configuration was not redesigned.
- Directional sphere geometry tested at both goal planes: high-speed swept crossing, whole-radius threshold, wrong direction, post tangency and crossbar miss.
- Held ball moved across the line did not score. Real outside-post and post-centred shots did not score or escape through the closed end wall. A released ball travelling from net toward pool did not score.
- Integration test scored three Blue goals and one White goal: high-speed 80m/s shot, repeat pickup/Space shot after restart, opposite goal, and a long shot after the cooldown changed Released to Free. Every event scored exactly once.
- Scoreboard/GOAL updates, restart scoring lock, restored controls, cleared ownership/shot state, correct start positions and retained scores all passed after repeated restarts.
- Repeated Editor setup retained 90 scene objects, two goals, one score authority and one scoreboard. An Inspector-width change moved the placeholder post correctly; original width was restored and saved.
- Near-goal and GOAL-notification screenshots were captured and visually inspected under ignored `Logs/Milestone3GoalsNear.png` and `Logs/Milestone3GoalNotification.png`. Scoreboard and net/frame are visible. The preview shot added one further temporary Blue score; runtime scores are not saved into the scene.
- Final Console ground truth after Play Mode exit: zero errors, zero warnings, no compilation failure. SampleScene retains blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.
- Play Mode stopped; temporary auto-tick/background overrides disabled. Temporary capture/config files are kept outside Assets/ProjectSettings in ignored Logs/Library paths. No persistent project settings, package versions or build scene list changed.

Manual test: open PrototypePool, Play, click Game, E to collect, W toward the positive-Z goal, release movement while facing the goal and Space to shoot. Confirm Blue increments once, GOAL appears, then the centre restart permits E/Space again. After restart, swim/face toward negative Z with S and shoot to test White. Miss outside the posts to confirm the score stays unchanged.

Developer manual Milestone 3 verification: movement/turning, pickup/carry/shoot, both goals, rejection of missed shots, scoreboard, GOAL notification and restart all work; no problems were observed. Other aspect ratios and Android/iOS builds/device performance remain pending. Milestone 3 scope: flat waterline shots, single swimmer/ball, geometric whole-ball crossing, simple rigid nets and pocket containment. Pool dimension changes do not reposition goal roots automatically.

Milestone 3 passed manual review and is recorded in local checkpoint `0e43094` (`Checkpoint manually verified Milestone 3 goals scoring and restart`). This includes the verified implementation, necessary TMP resources and manual-results documentation; legacy references and generated files are excluded. No push was made.

### Milestone 4 — Two swimmers, selection, passing and wider camera

Implemented in the existing PrototypePool. Both swimmers belong to the prototype Blue team and use the existing kinematic SwimmerMotor. No tactical AI or opposing team was added. Legacy architecture notes were reviewed for the separation of TeamManager selection, PlayerMovement/body execution, TeammateAI adapters and MatchContext ownership; those are conceptual references only. No legacy source implementation was inspected or copied for this milestone, and no 2D physics dependency was introduced.

New runtime files under `Assets/Scripts/Gameplay/`:

- `SwimmerIdentity.cs`: stable prototype player ID, team, cap number, start marker and selection visual; cached access to its own motor/interaction. No possession state is stored here.
- `PrototypeRoster.cs`: one shared member list, unique-ID/start validation, per-swimmer simulation pause and restart positions. Human selection and passing consult the same roster; membership is separate from movement or tactical decisions.
- `PlayerSelection.cs`: one authoritative active human-controlled swimmer. Enables only that swimmer's SwimmerInput and BallInteractionInput, stops the previous swimmer's movement intent/velocity, updates the selection ring and camera target. It never transfers/drops the ball or disables SwimmerBallInteraction. Inactive motors remain available to future AI but currently hold their positions.
- `PlayerSwitchInput.cs`: C (optional gamepad right shoulder) calls SelectNext. Processes before actor input; restart input lock rejects switching. Future touch controls can call the same selection API.
- `SwimmerPassing.cs`: movement/input-independent pass command, same-team target choice, modest launch assistance and configurable receiving parameters. Defaults: speed 8m/s, 35-degree candidate cone, 45% aim blend capped at 12 degrees, targets 2.5-12m away, receiving radius 1.35m, minimum flight 0.08s and receiving window 2.5s. An unaligned pass remains unassisted. No homing, interception logic or tactical movement.

Other new files/assets:

- `Assets/Editor/TeamPlayPrototypeSetup.cs`: `Tools > Water Polo > Set Up Two Swimmer Passing Prototype`. Extends the previous setup without rebuilding the environment/goals. Clones only the swimmer body when Swimmer2 is missing, assigns distinct identity/child references, roster, selection/input, passing, markers and restart wiring. Existing actor tuning and subsequent camera tuning are preserved; newly cloned actors get their own ID. Refuses Play Mode/unsaved scenes and saves only PrototypePool.
- `Assets/Materials/Prototype/TeammateCap.mat` (white), `SelectionRing.mat` (green unlit), with new asset `.meta` files. Selection rings use a 48-point LineRenderer each; no per-frame mesh creation.
- `Tools/Verification/TeamPlayPrototypeVerification.cs`: injected-input Play Mode checks outside Assets/player builds.

Modified files:

- `BallController.cs`: remains the sole ownership/state authority. Adds directional TryPass and one expiring intended receiver; manual pickup and automatic reception share the same private ownership assignment. Receiving checks the actual travelled segment against the intended teammate's current position. It begins after minimum flight time, may accept the intended teammate during Released cooldown, and never grants the passer automatic re-collection. Manual loose pickup retains the existing cooldown. Missing/disabled receivers or expiry leave ordinary free-ball physics. Reception clears goal eligibility; positive-speed pass releases otherwise use the existing release sequence/goal path.
- `BallInteractionInput.cs`: adds B (optional gamepad North) to call the actor's passing component. Only the selected actor's input adapter is enabled. Priority is shoot, pass, then collect for simultaneous actions.
- `SwimmerBallInteraction.cs`: exposes the assigned authoritative Ball reference; HasBall still derives from Ball.Owner.
- `SwimmerMotor.cs`: adds Position and StopMotion for physics-position queries and clean handoff; movement integration, acceleration, turning and bounds are unchanged.
- `GameplayCamera.cs`: adds Target/SetTarget without resetting focus or smoothing velocity. Existing camera system is retained. The scene's first Milestone 4 upgrade sets distance 9.5m, height 11.5m, smoothing 0.3s and FOV 55; pitch stays 50 degrees. Further setup runs preserve Inspector tuning. This is wider active-player framing, not a ball-aware broadcast rig.
- `PrototypeMatch.cs`: leaves goal detection/deduplication/scoreboard intact; delegates multi-swimmer freeze/reset to roster and selection when assigned, with a single-swimmer fallback for older scenes. Both start poses reset, ball ownership/pass intent clears, C is locked during GOAL, and the previously selected swimmer remains selected after restart.
- `Assets/Scenes/PrototypePool.unity`, `plan.md`, and the three older verification scripts. Their swimmer fixture now explicitly selects the first roster member; expected actor count follows the roster. Existing goal/ball/movement assertions remain.

Scene/identity details:

- `Actors/Swimmer`: `prototype-blue-1`, cap 1 (yellow), start at (0,0,0), faces +Z; initially controlled.
- `Actors/Swimmer2`: `prototype-blue-2`, cap 2 (white), start at (2,0,5), faces -Z. Separate Rigidbody, visuals, carry anchor, motor and interaction, referencing the same ball.
- `Gameplay/PrototypeRoster` and `Gameplay/PlayerSelection` hold roster and control; `Gameplay/RestartPositions/Swimmer2Start` adds the second saved pose.
- Exactly one green selection ring marks human control; it is not a ball-ownership indicator. Receiving does not auto-select the receiver. Switching away from a carrier leaves that swimmer carrying the ball.

### Milestone 4 verification and handoff

Verified in Unity 6000.6.7f1 through the existing CLI/Pipeline integration:

- Runtime/editor compilation completed without errors. All four Play Mode verification scripts compiled without diagnostics and passed with advancing physics.
- Team checks: unique same-team IDs, one enabled pair of human input adapters, one selection ring, independent W/D movement, inactive position holding, C switching and smooth camera retargeting. Initial wider framing includes both swimmers.
- Switching preserved possession; selected non-owners could not pass or shoot the other swimmer's ball.
- B created a visible moving, assisted pass with no instantaneous ownership transfer. Passer re-collection was blocked, an inactive teammate received, ownership stayed exclusive, and the second swimmer passed back successfully without automatic selection changes.
- Moving the intended receiver away caused a miss: trajectory did not home, receiving intent expired, and the free ball remained collectible.
- The second swimmer picked up and shot a valid goal. Restart locked both motors/input and C, reset both saved positions, cleared ball ownership/receiving intent and restored one active controller. A new pickup/pass/shot and second restart succeeded. The team test added two Blue scores.
- All movement, camera/pickup/carry/shoot and scoring/restart regressions passed. The goal test added three Blue and one White score, including miss/held/reverse rejection and a long shot; original both-goal behavior is preserved.
- Repeated Editor setup retained 103 scene objects, two distinct swimmers, one ball, one selector, two goals and one score authority; camera tuning was preserved.
- Screenshots under ignored `Logs/Milestone4Team.png`, `Milestone4Pass.png` and `Milestone4Receiver.png` capture wider framing, travelling pass and selected receiver. Initial and receiver views were visually inspected. Preview scores are temporary runtime values; a new Play session starts at 0-0.
- Final Console ground truth: zero errors, zero warnings, no compilation failure. SampleScene retains blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`. New Assets have companion `.meta` files.
- Play Mode stopped, auto-tick/background overrides disabled, temporary generated settings kept in ignored Logs. No package versions, persistent project settings or build scene list changed.

Manual test: open PrototypePool and Play, click Game, E to collect with the selected yellow-cap swimmer, B to pass toward the white-cap teammate, C to select that receiver, B to pass back, and C to return control to the yellow cap. WASD/arrows move only the ring-selected swimmer; Space shoots only if that swimmer owns the ball. Aim/facing matters. Test switching while carrying separately: the ball must stay with its existing owner. After a goal both swimmers reset; selection is retained, so C back to the swimmer near the starting ball if needed.

Inspector tuning: per-actor SwimmerPassing controls speed/assistance/receiving; CameraRig/Main Camera controls wider framing; identity Start Position and roster members define reset membership. Existing shot speed, pickup distance and goal/restart tuning remain available.

Remaining limits: manual keyboard/gamepad feel, other screen aspects, Android touch controls/builds and device performance await testing. Only the intended teammate auto-receives a pass, with a coarse placeholder receiving radius; missed/unaligned loose balls require E. Assistance does not guarantee a catch. No interceptions, AI positioning, swimmer overlap resolution, lob/charged passes, opposing teams, 6v6 match, goalkeeper, clock, fouls, economy, advanced animation or realistic water were added. Camera follows selection, so a distant unselected carrier/ball can leave view; ball-aware broadcast framing is deferred.

Developer manual Milestone 4 verification: two visible swimmers, C switching, single-player input isolation, B passing/receiving, pickup/carry/shoot, goals and restarts all work. The missing automatic selection of a receiving teammate is the highest-priority Milestone 5 task. Gamepad/device testing remains pending.

Milestone 4 is recorded in local checkpoint `4babe88` (`Checkpoint manually verified Milestone 4 team selection and passing`), including its manual-results documentation. Legacy references, old-plan.md and generated files were excluded. No push was made.

### Milestone 5 — Possession switching, basic teammate/opponent AI and wider framing

Implemented in order: possession switching verified first, friendly AI verified next, one opponent verified next, then camera framing and final regression checks. Historical Milestone 4 descriptions of stationary inactive swimmers and reception without control switching describe the earlier milestone; the behavior below is current.

New runtime/editor files:

- `Assets/Scripts/Gameplay/SwimmerTactics.cs`: small pure XZ target calculations for support and goal-side defense. No input, physics or ownership mutation.
- `Assets/Scripts/Gameplay/PrototypeSwimmerAI.cs`: one shared controller for any inactive friendly or opponent. Recomputes decisions every 0.15 seconds and on owner changes; supplies direction to the existing SwimmerMotor. Support defaults to 4.5m forward / 2.5m lateral relative to the carrier, with simple spacing and arrival slowing. One nearest eligible member per team pursues a collectible loose ball within a configurable range, including the human in nearest-player arbitration. AI requests pickup through SwimmerBallInteraction.TryCollect; it cannot bypass distance, release cooldown or exclusive ownership. An opposing carrier causes goal-side defensive pressure at 2.8m. The selected human's motor input is never written by AI. Handoff and restart force a fresh decision. Indexed cached-roster loops avoid per-frame searches/collection allocations.
- `Assets/Editor/BasicAIPrototypeSetup.cs`: final menu `Tools > Water Polo > Set Up Automatic Switching and AI Prototype`. Reuses the previous setup chain, creates only a missing opponent/start marker/materials, configures all AI references and selection's ball reference, registers all three participants, applies the first wider-camera upgrade and saves PrototypePool. Repeated setup preserves existing camera/actor tuning and does not duplicate objects. Refuses Play Mode and unsaved scenes. The intermediate `Set Up Friendly AI Prototype` command remains available; use the final command for the complete milestone.

New assets:

- `Assets/Materials/Prototype/OpponentCap.mat` (red), `OpponentBody.mat` (pale), and companion `.meta` files for these materials and the three new scripts.
- `Actors/Opponent`: `prototype-white-1`, White team, cap 3, starts at (4,0,8) facing -Z, with its own visual/carry anchor/body/start marker. Its loose-ball pursuit range is 6m; friendly default is 9m. White defends the +Z goal. It never has human input or an active selection ring.
- `Gameplay/RestartPositions/OpponentStart`: third restart pose. PrototypeRoster now contains all three participants, across both teams. Team membership and human-selection eligibility are separate from overall restart membership.

Modified implementation:

- `Assets/Scripts/Gameplay/BallController.cs`: still the only ball ownership authority. Publishes `PossessionChanged(previous, next)` once when ownership actually changes, after pickup/reception/release/drop/reset state is applied. A rejected claim or unchanged null owner does not publish another change. Adds read-only PhysicsPosition for loose-ball decisions. No alternate ownership model or 2D physics was introduced.
- `Assets/Scripts/Gameplay/PlayerSelection.cs`: subscribes/unsubscribes to that event. A newly acquired eligible Blue carrier is selected immediately, updating the existing input adapters, ring and smoothly retargeted camera. Null/loose-ball events, opponents and locked selection are ignored; there is no polling or queued switch to replay after a restart. C cycles only eligible friendlies. Manual switching preserves ownership and is not undone while the same carrier continues holding the ball. Selection never grants possession.
- `Assets/Scripts/Gameplay/GameplayCamera.cs`: preserves fixed orientation, bounded focus and smooth target changes. Adds a small nearby-ball bias (influence 0.15, range 10m, maximum offset 1.5m, smoothly faded with distance), not a separate camera mode. Scene upgrade uses distance 11m, height 13m, pitch 50 degrees, FOV 55 and smoothing 0.3 seconds; all tuning remains in the Inspector. Wider framing includes all three starting swimmers. A distant ball can still leave view.
- `Assets/Editor/TeamPlayPrototypeSetup.cs`: recognizes the known opponent and preserves it in roster/restart membership on repeated lower-level setup. Unknown extra actors are still rejected. Opponent inputs/selection remain disabled.
- `Assets/Scenes/PrototypePool.unity`: saved/wired three-swimmer scene, explicit possession-event reference, three AI components, opponent start/materials, and wider camera settings. Existing goals, ball mechanics, pool and scoreboard remain. The existing PrototypeMatch roster pause/reset path handles all participants without rewriting scoring.

Control and AI behavior:

- WASD/arrows move the ring-selected Blue swimmer; E requests pickup; Space shoots; B passes in the facing/assisted direction; C manually selects the next Blue swimmer. Future touch/AI commands can use the same movement, interaction and selection APIs.
- Pass travel does not switch selection. Successful friendly reception or legitimate inactive-friendly loose pickup switches once. A return pass works the same way. The old carrier becomes eligible for AI; only the new selection receives human input.
- During an intended pass, inactive AI waits in position to preserve the existing receiving contract. It does not intercept or home toward a travelling pass. A missed pass becomes ordinary loose-ball pursuit after its receiving intent expires. After shooting, the active swimmer remains selected unless a friendly genuinely acquires the loose ball.
- Opponent pressure is positional only: no steals/contact possession. If the opponent collects a loose ball it stops and holds it; no autonomous pass/shoot/attack behavior is implemented. Blue inactive swimmers can then position defensively. Stop and restart Play Mode to begin a fresh possession if the opponent is holding the ball; do not treat missing recovery rules as a complete competitive match.
- During GOAL, all three motors and human interaction are locked. The existing restart resets every participant and clears ball owner/pass/shot state while preserving scores and a valid friendly selection. Null ownership notifications cannot trigger stale camera changes.

### Milestone 5 verification and handoff

Verified in the running Unity 6000.6.7f1 Editor through Unity CLI/Pipeline, with advancing Play Mode physics:

- Runtime/editor compilation completed without errors. Seven Play Mode verification suites compiled without diagnostics and passed. Existing fixture suites temporarily disable/restore AI where they require stationary actors; live AI behavior is tested separately.
- `Tools/Verification/MovementSandboxVerification.cs`: existing movement/keyboard/arrows/analog gamepad, acceleration/stopping/turning, waterline, bounds and visibility. Straight/diagonal speed both approximately 4m/s.
- `Tools/Verification/CameraBallPrototypeVerification.cs`: existing pickup range/exclusive ownership, carrying, facing-direction shots, cooldown/recollection, disable cleanup, 80m/s containment, smooth camera follow and visibility at all corners at the current Game view aspect.
- `Tools/Verification/GoalScoringPrototypeVerification.cs`: both scoring planes, held/reverse/outside-post/post-hit rejection, high-speed and long shots, exactly-once scores, HUD/GOAL and repeat restarts. Added three Blue and one White score during this regression run.
- `Tools/Verification/TeamPlayPrototypeVerification.cs`: updated intentional reception expectations for automatic switching and roster-sized actor counts; original selection isolation, physical passes/return passes, missed passes, shots and repeated restarts passed. Added two Blue scores during this regression run. This fixture isolates AI.
- New `Tools/Verification/PossessionSelectionVerification.cs`: acquisition-time switch on pass and return, camera/ring/input synchronization, failed pass/no premature switching, manual C independence, inactive-friendly pickup and lock/no stale replay. Counted 12 genuine ownership notifications in the fixture; no per-frame repeated switching.
- New `Tools/Verification/FriendlyAIVerification.cs`: live friendly support movement, human movement unaffected, handoff to AI while retaining ball ownership, nearest-friendly pursuit and automatic selection only after legitimate pickup. Human closer to ball suppresses redundant teammate chase. Opponent AI is isolated for this fixture.
- New `Tools/Verification/OpponentAIVerification.cs`: all three AI adapters enabled. Defense toward the defended goal, opponent input/selection exclusion, real loose-ball approach/acquisition, exclusive enemy ownership without selection transfer, pool/waterline containment, all-three goal freeze/reset, then new pickup/pass/return-pass with automatic switching and camera retargeting all passed.
- New `Tools/Verification/AIPrototypeSetupVerification.cs` (Edit Mode): repeated final setup retained 113 objects, three unique IDs/motors/AI adapters, one ball/selection/match authority and two goals. All required AI/start/ball/ring references were assigned; temporary custom camera distance survived repeated setup and was restored afterward. No duplicate actor components.
- A 1280x720 camera capture at ignored `Logs/Milestone5Broadcast.png` was visually inspected: three swimmers, selected ring and ball are readable. This camera-render capture excludes screen-space overlay UI; the HUD was verified by the scoring suite.
- Console ground truth at final Play verification: zero errors, zero warnings, compilation not failed. SampleScene remains unchanged. All new Unity assets have companion `.meta` files. No packages, build-scene list or persistent project settings were intentionally changed. Play Mode and temporary auto-tick/background overrides are stopped/disabled at handoff.

At the Milestone 5 checkpoint, isolated manual playtesting was pending. The later developer Milestone 6 manual results are recorded in the current snapshot and Milestone 6 section; do not reinterpret them as exhaustive testing of this earlier checkpoint. Different aspect ratios, gamepad feel, mobile touch UI and Android/iOS builds/device performance are not verified by the automated checks. Simple spacing is not full collision/avoidance; kinematic swimmers can overlap. Tactical formations and stealing were not part of Milestone 5; Milestone 6 adds them below, and Milestone 7 adds keepers. Full teams, clock, fouls, advanced animations/water and economy remain unimplemented.

Manual test: open `Assets/Scenes/PrototypePool.unity`, Play and click the Game view. E collects with the yellow cap; face the white-cap teammate and B to pass. Selection/ring/camera must remain with the passer during travel, then transfer on reception; WASD must move the receiver. Face/pass back and verify automatic return selection. Try C while carrying: ownership must stay with the previous carrier. Watch the inactive friendly support the carrier and the red-cap opponent defend. Shoot/miss and let an eligible inactive friendly approach a loose ball; acquisition should select that friendly. The red cap may collect but must never become selected. Score at either goal and verify all three reset and another pickup/pass/shot works. Check `Window > General > Console` for errors/warnings.

No manual Inspector wiring is required. The final setup menu is optional because the scene is already saved. To run it, exit Play Mode, save any intended scene edits, then choose `Tools > Water Polo > Set Up Automatic Switching and AI Prototype`. Inspector tuning: each actor's Prototype Swimmer AI for support/pressure/range/speed; `Gameplay/PlayerSelection` for Human Team; `CameraRig/Main Camera > Gameplay Camera` for framing and `Camera` for FOV. Keep Human Team Blue for this two-friendly prototype.

Milestone 5 is recorded in local checkpoint `d647882` (`Checkpoint Milestone 5 automated-tested; manual verification pending`). Before extending it, all seven existing Play Mode suites and its Edit Mode repeated-setup check were rerun successfully with zero current Console errors/warnings. This checkpoint includes only Milestone 5 assets, scripts, verification and documentation; legacy/generated files were excluded. It is NOT a manual-playtest approval. No push was made.

### Milestone 6 — First playable 2v2, tactical offense/defense and stealing

The current PrototypePool contains two Blue and two White field swimmers. Blue attacks +Z, White attacks -Z. Blue is human-selectable; inactive Blue swimmers and all White swimmers use the same shared reasoning/physical action architecture. All four are registered in the existing PrototypeRoster for pause/reset. Gameplay remains a score-based free-play prototype: no keeper, official clock, quarters, match end or competition rules.

Foundation review included actual current gameplay/editor/verification scripts, saved scene, AGENTS.md, README and this plan. No `docs/ARCHITECTURE.md`, `docs/MIGRATION_AUDIT.md` or docs directory was present. The available old-plan architectural summaries of WaterPoloBrain/IAgentBody, TeamSide, TeamManager, MatchContext, TeammateAI and BotMovement were reviewed as concepts. No legacy C# source archive/local reference clone was available or imported; no 2D physics code was copied.

New implementation files (with companion Unity `.meta` files):

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Gameplay/PrototypeTeamTactics.cs` | One component per team. Explicit team/roster/ball/pool/match/attacking-goal/defended-goal context; formation/support targets, presser/loose-pursuer coordination, lane/pressure/shot/pass evaluation and team pass cooldown. No ownership assignment or physical movement. |
| `Assets/Scripts/Gameplay/WaterPoloDecision.cs` | Shared deterministic decision model returning a small value-type SwimmerDecision: formation, support, pressure/defense, loose pursuit, carry, pass, shoot or pass wait. No scene searches, keyboard simulation or body/ownership mutation. |
| `Assets/Scripts/Gameplay/SwimmerSteal.cs` | Reusable human/touch/AI steal command, configurable cooldown/range/facing/protection/exposure; calls the authoritative ball resolver and reports a StealResult. Match stoppages block attempts and reset attempt timers. |
| `Assets/Editor/TwoVTwoPrototypeSetup.cs` | Explicit repeat-safe `Tools > Water Polo > Set Up 2v2 Prototype`; creates only missing White Player 2/start marker and team context objects, wires every AI/steal/team/goal reference and saves PrototypePool. |
| `Assets/Materials/Prototype/BlueTeamBody.mat` | Blue team body color; White retains its pale body and red cap. Yellow/white Blue caps distinguish the two friendly prototypes. Colors are presentation only; explicit identity drives rules. |

Modified implementation files:

- `Assets/Scripts/Gameplay/PrototypeSwimmerAI.cs`: the existing shared adapter now consumes WaterPoloDecision and PrototypeTeamTactics instead of embedding the old support/hold logic. Retains movement speed/arrival/spacing/decision Inspector tuning. Executes commands through SwimmerMotor, SwimmerPassing, SwimmerBallInteraction and SwimmerSteal; never assigns Owner. Default decision interval is 0.15s. AI actions are canceled/recomputed on match changes, control handoff and owner changes. Selected human input is never overwritten. Pass/shot execution counters are read-only runtime diagnostics, not progression stats.
- `Assets/Scripts/Gameplay/BallController.cs`: adds read-only OwnershipStartedAt and atomic TrySteal validation/assignment using its existing AssignOwner/PossessionChanged path. Pickup compares both Rigidbody positions so interpolation cannot reject a valid post-reset/moving claim; non-swimmer diagnostic claimants retain the transform fallback. Avoids setting velocities on an already kinematic held body during transfers. Existing physical pass/reception, flat shots, release protection, bounds and scoring eligibility remain authoritative.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: adds optional independent facing direction for stationary AI aiming/pressure, using the same limited Rigidbody rotation in FixedUpdate. Stop/reset/disable clears aim and velocity. Existing movement integration is retained.
- `Assets/Scripts/Gameplay/SwimmerInput.cs`: clears AI aim before applying human movement so handoff cannot leave stale aim control.
- `Assets/Scripts/Gameplay/BallInteractionInput.cs`: F issues the shared steal command through the new Input System; no conflicting existing F binding was found. Existing E/B/Space remain, with action priority shoot, pass, steal, collect. C remains in PlayerSwitchInput. No touch controls were added.
- `Assets/Editor/TeamPlayPrototypeSetup.cs`, `BasicAIPrototypeSetup.cs`: lower-level setup preserves registered extra White swimmers instead of truncating/rejecting the four-member roster. Basic AI setup retains existing matching tactical-team references. Use the final 2v2 menu to ensure all Milestone 6 references are configured.
- `Assets/Scenes/PrototypePool.unity`: fourth swimmer, fourth start, Blue/White team contexts, four steal components, matching AI team references and Blue team materials. Existing goal geometry, HUD, original swimmers, ball and camera remain. No SampleScene changes.
- `plan.md` and the eight existing verification scripts listed below. No PlayerSelection, PrototypeRoster, PrototypeMatch, GoalMouth or scoreboard rewrite was needed.

Scene details:

- Blue: `Actors/Swimmer` (`prototype-blue-1`, initially selected) and `Actors/Swimmer2` (`prototype-blue-2`), preserving their existing starts/anchors/motors and cap colors.
- White: `Actors/Opponent` (`prototype-white-1`, existing start (4,0,8)) and `Actors/Opponent2` (`prototype-white-2`, new start (-4,0,8), yaw 180). New `Gameplay/RestartPositions/Opponent2Start` is independent of every other saved start.
- `Gameplay/BlueTeam`, `Gameplay/WhiteTeam`: PrototypeTeamTactics with opposite goal references. Goal.ScoringTeam identifies the attacking team credited at that net; Blue defends the -Z net, White the +Z net.
- Runtime decisions iterate roster membership/team/slot data, independent of GameObject names or a fixed team count. The explicit Editor setup verifies a 2+2 scene; it does not create full teams. There are 125 scene objects after repeated setup.

Tactical behavior and tunables:

- Each team coordinates one nearest loose-ball pursuer and one defensive presser, including the human in nearest-member selection. Assignments update every 0.5s and on owner change, with a 1m retention advantage to avoid rapid swaps. Other swimmers hold formation or protect a goal-side passing lane/mark. Loose-ball pursuit covers the pool; the earlier 6/9m initial-AI search limits no longer govern play.
- Support uses the team's explicit attacking direction, default 4m forward/3m lateral, and stable roster slots. Formation and all targets are pool-clamped. The adapter uses soft spacing and arrival slowdown; pressure can enter the 1.8m steal radius without being repelled by ordinary carrier-spacing rules. This is lightweight steering, not physical swimmer overlap resolution or full pathfinding.
- AI carriers advance toward a central attacking position. After a minimum 0.8s hold they assess a 9m shot range, lateral angle, clear lane, defender pressure and default quality threshold 0.55. They stop, turn using the motor, and shoot through the same API as the human. The actual ball must cross the existing goal geometry to score.
- If a shot is unattractive, an unobstructed teammate 2.5-11m away with better progress/pressure can be selected for a directional physical pass. The adapter aims and calls the existing passing API; no homing/instant ownership transfer. Team pass cooldown is 3s, limiting repeated exchanges. Aiming and ordinary motor deceleration occur before release.
- A carrier exceeding the configurable 8s maximum hold selects a physical fallback shot when no better pass is chosen. Rotation/settling adds a short aim allowance; this prevents indefinite holding without altering the score directly. AI may miss long/pressured attempts.
- During an intended pass, inactive AI waits, preserving the existing receiving contract. Misses expire to normal loose-ball play. No new interception system, lobs or charged shots were added.

Prototype stealing:

- F and AI pressure call exactly the same SwimmerSteal.TrySteal path. Valid attempts consume a default 1.6s cooldown, including range/facing/protection failures. The core ball authority verifies an opposing held carrier, range <=1.8m, attacker facing within 65 degrees, >=1s since the carrier acquired possession, and carrier-forward dot toward attacker >=-0.25 (rear approaches are shielded). Exact overlapping centres are rejected because they have no meaningful facing direction.
- This is a deterministic prototype rule: every valid exposed-front/side attempt succeeds; there is no hidden random roll. Missed conditions preserve the existing owner. Same-team attempts, free-ball attempts and stoppages cannot transfer possession. The ball has one Owner before and after the atomic transfer and emits the existing genuine ownership-change event.
- Friendly pickup/reception/steal therefore automatically selects the actual new Blue carrier, updates the ring/input/camera once, and never selects White. C still changes only control and does not transfer possession; the previous human becomes eligible for tactical AI immediately. Future touch controls can call the same movement/pass/shoot/steal/selection APIs.
- No foul probability, referee, exclusion or penalty is implemented. The protection/cooldown and shielding rules are prototype fairness constraints, not official water polo rules.

Goals, presentation and restart:

- Existing goal detection/scoreboard/GOAL/deduplication remains unchanged. Both teams release real balls and can score at their correct attacking net; own-goal-equivalent crossings still credit that net's configured team.
- Existing roster pause/reset includes all four. Team assignment/pass timers, per-actor pending decisions/aim and steal cooldowns reset from match Changed events; ball reset clears possession, shot and receiving intent. Scores and a valid Blue selection survive restart. No deferred action/event queue exists to fire a stale pass after reset.
- The Milestone 5 elevated camera is retained (distance 11m, height 13m, pitch 50, FOV 55, smoothing 0.3s and bounded nearby-ball bias). It already frames all four initial swimmers and follows selection smoothly; no camera rewrite or additional HUD was needed. A distant ball can leave the selected-player view. Screen-space HUD remains score/GOAL only.

### Milestone 6 automated verification

Verification tools remain outside Assets/player builds. New files:

- `Tools/Verification/TwoVTwoSetupVerification.cs`: Edit Mode scene repeatability, 2+2 identities, unique starts, shared authorities, correct goal/team references and preserved camera tuning.
- `Tools/Verification/TacticalOffenseVerification.cs`: real White pickup/advance/turn/shot/goal, physical pass and valid White reception, and configured anti-stall release. Isolates other AI for deterministic decision fixtures.
- `Tools/Verification/StealDefenseVerification.cs`: injected F input, protection/range/facing/shielding failures, cooldown, same-team/lock rejection, successful atomic steal/auto-selection, coordinated targets/hysteresis and an actual AI pressure/steal sequence.
- `Tools/Verification/TwoVTwoMatchVerification.cs`: all AI enabled; human E/B/return/C interactions, camera retargeting, two White AI and two Blue human goals, exact-once locks and reset snapshots for every actor, followed by 20 simulated seconds of ordinary autonomous play with bounds/ownership/no-stall assertions.

Existing fixtures updated: `MovementSandboxVerification.cs`, `CameraBallPrototypeVerification.cs`, `GoalScoringPrototypeVerification.cs`, `TeamPlayPrototypeVerification.cs`, `PossessionSelectionVerification.cs`, `FriendlyAIVerification.cs`, `OpponentAIVerification.cs`, `AIPrototypeSetupVerification.cs`. Changes isolate deterministic starting poses from ongoing live AI, preserve original gameplay assertions and update intentionally superseded expectations (four actors, passive holding replaced by offense, idle replaced by formation, tactical-team references). The follow-only camera settling measurement parks the ball outside its framing influence after containment checks, so legitimate moving-ball bias does not alter that measurement.

Stage checks completed: pre-change Milestone 5 regression (eight suites); four-actor scene/configuration plus passing regression; White offense; defense/stealing; full-AI integrated match. No developer manual test or mobile performance claim is implied.

Recorded final verification from the Milestone 6 implementation session in Unity 6000.6.7f1 (not rerun during this documentation review):

- Runtime/editor compilation completed successfully. All 12 verification scripts compiled with zero diagnostics and passed: ten Play Mode suites (`MovementSandboxVerification`, `CameraBallPrototypeVerification`, `GoalScoringPrototypeVerification`, `TeamPlayPrototypeVerification`, `PossessionSelectionVerification`, `FriendlyAIVerification`, `OpponentAIVerification`, `TacticalOffenseVerification`, `StealDefenseVerification`, `TwoVTwoMatchVerification`) and two Edit Mode suites (`AIPrototypeSetupVerification`, `TwoVTwoSetupVerification`).
- The final integrated match run added two Blue and four White goals: the four explicitly arranged scoring/restart cycles passed, and additional White attacks scored during the 20-second uninterrupted observation. Five completed restart snapshots had been verified when observation ended; the final goal was still in its restart delay. The preview capture then waited for that restart to finish. No false duplicate or incorrect-team score was observed.
- Both repeated-setup suites retained 125 objects, four unique players/start references, exactly two members per team, one ball/selection/score authority, correctly assigned tactical/steal references and preserved custom camera tuning. Original SampleScene retains Git blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.
- At that session's final Console check: zero errors, zero warnings and `compilationFailed=false`. Historical Pipeline journal counts are not the current Console status. No Console clearing was used to mask errors. This review did not query the live Editor Console.
- `Logs/Milestone6Final-*.json` contains the actual final suite results, and `Logs/Milestone6Baseline-*.json` contains the pre-change checks. These are ignored local artifacts. `Logs/Milestone6Gameplay.png` is an actual Game-view capture with score HUD, four swimmers, ball and selected ring; it was visually inspected for prototype readability. Preview scores were transient and reset on the next Play session.
- The implementation session ended with Play Mode stopped and temporary auto-tick/background overrides disabled. Automation-generated configuration/captures/helpers remain in ignored Logs/Library paths. Player/URP settings were restored; no packages, Android configuration or build scene list changed. New Unity assets have `.meta` files. At the subsequent documentation review Milestone 6 was still uncommitted and the index empty; the developer later completed its backup as `48ab2f6`. old-plan.md remained untracked/excluded.

### Milestone 6 developer manual verification

The developer personally tested the current 2v2 build and confirmed swimming movement, camera following, passing, shooting, stealing with F, friendly attacking support, opposing swimmers moving/playing, both teams scoring and scoreboard updates. Their reported gameplay screenshot showed `BLUE 1 - 2 WHITE`. The screenshot was described by the developer in the documentation request; it was not separately inspected during this review.

These confirmations establish playable core 2v2 behavior, not exhaustive edge-case coverage. Automated-only assertions, including detailed directional/duplicate scoring checks, cooldown/shielding boundaries, full restart snapshots and repeat-setup invariants, retain their separate recorded evidence. Gamepad, Android/iOS builds, touch input, device performance and broader aspect-ratio coverage remain unverified.

Milestone 6 limitations: deterministic close-contact stealing needs gameplay balancing; soft steering can still allow kinematic bodies to overlap; formations/pass-risk/shot quality are simple heuristics without player attributes. All shots/passes stay at the waterline. Goal restarts return to the same prototype start positions and centre ball rather than official possession/kickoff rules. Milestone 6 added no keeper; Milestone 7 below adds that role. Fouls, exclusions, clocks, match end, full teams, touch UI, network/backend, economy, advanced water/athletes/animation and audio polish remain absent. Core 2v2 manual results are confirmed above; exhaustive manual edge cases, device builds, mobile performance, touch controls, aspect-ratio coverage and final camera/AI tuning remain pending.

Manual handoff: open `Assets > Scenes > PrototypePool` in the Project panel, Play, and click the Game view. WASD/arrows swim; E picks up; B passes; Space shoots; C selects the other Blue swimmer; F attempts a steal. Face the nearby white-cap Blue teammate to pass and observe automatic selection on receipt. Leave a loose ball for White to collect and attack; defend from the carrier's front/side, face the carrier and use F within reach after acquisition protection expires. White must never become selected. Score at +Z for Blue; allow White to attack -Z. Confirm scoreboard/GOAL, all four resets and repeated pickup/pass/shot sequences. Open `Window > General > Console` to inspect errors/warnings. No Inspector wiring is required.

Milestone 6 setup remains available at `Tools > Water Polo > Set Up 2v2 Prototype`, outside Play Mode after saving intended scene edits. Tune team attack/support/shot/hold settings at `Gameplay/BlueTeam` or `WhiteTeam > Prototype Team Tactics`; per-actor speed/decision/spacing at Prototype Swimmer AI, steal conditions at Swimmer Steal; camera remains at `CameraRig/Main Camera`. Milestone 7 extends this setup through the final keeper menu below. Only the developer may stage/commit/push; they completed the Milestone 6 backup and explicitly authorized Milestone 7 afterward.

### Milestone 7 — AI goalkeepers, saves and distribution

Authorized scope: add one AI-only goalkeeper to each existing two-field-player team. The human developer completed/pushed the Milestone 6 backup as `48ab2f6` (`Complete 2v2 water polo prototype through Milestone 6`) before this work. Initial read-only status showed main matching origin/main, with only old-plan.md untracked. No AI checkpoint, staging, commit, push, fetch or other Git write was performed. The absolute human-only Git rule takes precedence over checkpoint wording in milestone templates.

Foundation review covered AGENTS.md, this plan, actual runtime/editor/verification scripts, saved PrototypePool hierarchy/configuration and the available old-plan goalkeeper summary. No old Goalkeeper.cs source archive was available. Ball tracking, physical contact, skill/catch decisions, protected possession and distribution were useful concepts; no Rigidbody2D code, old coordinate constants, halftime/shot-height/animation/manual-keeper logic was imported.

New runtime files under `Assets/Scripts/Gameplay/`:

| File | Responsibility |
| --- | --- |
| `GoalkeeperPositioning.cs` | Pure goal-local tracking/projection and relative swept-sphere contact geometry. It reads no global scene state and never assigns ownership or moves actors. |
| `PrototypeGoalkeeper.cs` | Dedicated role-specific tracking, bounded target generation, reaction/recovery and seeded save/catch decisions. Sends movement/facing commands to the existing motor, and confirmed contact results to the existing ball authority. |
| `GoalkeeperInterception.cs` | One scene observer at execution order -120, before PrototypeMatch (-100). Samples actual ball/keeper hand travel, finds limited physical contact, prevents post-line rescue and latches contacts to avoid repeated save rolls. No whole-goal wall or full-scene per-frame searches. |
| `GoalkeeperDistribution.cs` | Secures a hold, chooses an eligible field-player outlet, aims with the shared motor and releases through SwimmerPassing; timed fallback prevents indefinite possession. Match changes cancel cached outlet/timing. |

Other new files/assets:

- `Assets/Editor/GoalkeeperPrototypeSetup.cs`: explicit `Tools > Water Polo > Set Up Goalkeeper Prototype` menu. Extends the previous repeat-safe setup, creates only missing keeper objects/start markers/components/materials, registers all six athletes and assigns keeper/interception/distribution references. Refuses Play Mode or unsaved scenes, preserves existing field actors/pool/goals/camera/HUD and subsequent tuning, and saves only PrototypePool.
- `Assets/Materials/Prototype/GoalkeeperBody.mat`, `BlueGoalkeeperCap.mat`, `WhiteGoalkeeperCap.mat`: upright orange placeholder bodies with blue/white caps. These colors do not determine identity. Companion `.meta` files accompany all new Unity scripts/materials.
- New verification files outside Assets: `GoalkeeperSetupVerification.cs`, `GoalkeeperPositionVerification.cs`, `GoalkeeperSaveVerification.cs`, `GoalkeeperDistributionVerification.cs`, `GoalkeeperMatchVerification.cs` under `Tools/Verification/`.

Modified implementation:

- `SwimmerIdentity.cs`: adds explicit AthleteRole (FieldPlayer=0 default, Goalkeeper=1), Role and IsFieldPlayer. Existing four actors retain the default field role and IDs. The same identity, motor and SwimmerBallInteraction can represent either role; no competing keeper ownership model/interface was necessary.
- `PrototypeRoster.cs`: Members contains every athlete for pause/reset; cached FieldPlayers contains only the four field roles for selection/tactics/test fixtures. Cache rebuilds when serialized roster data changes. No per-frame filtered list allocation.
- `PlayerSelection.cs`: eligibility additionally requires IsFieldPlayer. A Blue keeper catch does not change human selection, ring or camera target; C continues cycling only the two Blue field players. The existing possession event selects a receiving Blue field player after a keeper pass.
- `SwimmerMotor.cs`: introduces generic ISwimmerMovementBounds and an optional bounds provider. Keepers supply a goal-local envelope; the same acceleration/deceleration/MovePosition/facing integration remains authoritative. Field behavior has no additional constraint. Pose reset respects the keeper envelope.
- `PrototypeTeamTactics.cs`, `WaterPoloDecision.cs`: field eligibility/ranks/pressure/pursuit exclude keepers. A friendly keeper owner produces outlet support roughly 6m into the pool; opponents hold defensive lanes rather than trying to press the keeper. Overall possession still derives solely from Ball.Owner. Normal field support/defense is retained.
- `SwimmerPassing.cs`: adds MaximumTargetDistance and directed TryPassTo with the same range/aim/receiver validation and shared assisted launch path as ordinary passes. Current automatic target selection/outlets prefer field players. Existing physical flight, receiving window/cooldown and exclusivity remain unchanged.
- `BallController.cs`: adds release kind/time/releaser metadata and MotionRevision/MotionOrigin for release/reset/catch/deflection discontinuities. TryCatchContact and TryDeflectContact consume contacts confirmed by the interception observer; BallController alone applies ownership or velocity/state changes. A bounded keeper's secured hold is protected from the prototype field-steal command. Catch clears scoring eligibility; deflection retains its original shot sequence/eligibility, clears pending pass intent and applies a short 0.15s loose-release protection.
- `PrototypeMatch.cs`: retains directional whole-ball geometry, score authority and per-shot deduplication. Path sampling now starts a changed physical trajectory at Ball.MotionOrigin, so a reflected ball cannot reuse its old incoming segment and release/reset spawning cannot fabricate a goal.
- `Assets/Editor/TeamPlayPrototypeSetup.cs`, `BasicAIPrototypeSetup.cs`, `TwoVTwoPrototypeSetup.cs`: role-aware field setup preserves existing keepers in overall roster membership without assigning field AI/human inputs or truncating them. The final keeper menu ensures the complete current wiring.
- `Assets/Scenes/PrototypePool.unity`: six-athlete roster, two new keepers/starts, one observer and two distributors, explicit goal/team/ball links and placeholder keeper materials. SampleScene, camera script/tuning, score HUD, goal geometry, field input bindings and field AI controller implementation are preserved.
- All 12 previous verification scripts now distinguish field fixtures from the complete athlete roster. The ten original Play Mode suites disable/restore keeper behavior while testing the established field systems; five new keeper suites test role inclusion, interception and six-athlete integration separately. Startup waits for any current restart before capturing the controlled swimmer reference. AGENTS.md, README.md and this plan describe the current baseline.

Scene and movement:

- `Actors/BlueGoalkeeper`: role Goalkeeper, team Blue, ID `prototype-blue-goalkeeper`, defends the White-scoring -Z goal. `Actors/WhiteGoalkeeper`: role Goalkeeper, team White, ID `prototype-white-goalkeeper`, defends the Blue-scoring +Z goal. Field IDs/start poses remain unchanged. Each has its own `Gameplay/RestartPositions/{BlueGoalkeeperStart,WhiteGoalkeeperStart}` marker.
- At current geometry the roots start at world Z=-13.6/+13.6, face into the pool and remain at PoolBounds.Waterline. Positioning uses each assigned goal's transform/width, not names/colors or hardcoded world ends.
- Default station is 1.4m in front of the goal line. Lateral extent is clear half-width minus 0.45m margin (1.05m for a 3m mouth). The motor clamps to this envelope every physics integration, speed 2.5m/s, acceleration 8m/s², deceleration 10m/s². Target smoothing is 0.15s; a nearby opponent carrier or approaching ball supplies lateral tracking/projected crossing. A distant harmless ball causes a smooth central return. No teleporting/dive animation or pool-wide chase.
- The upright keeper's CapsuleCollider is a trigger, not a solid invisible save wall. Actual save geometry is the limited hand sphere below. Held ball uses the existing carry anchor, collider-disabled state and exclusive owner; human controls remain available on the selected Blue field player.

Physical saves:

- The observer tests the actual travelled ball segment against the hand's travelled segment, with full 3D relative swept-sphere geometry. Effective radius is configurable hand reach (default 0.5m) plus actual ball radius (0.14m); hand centre is 0.55m in front of the keeper root at the waterline holding anchor. Shots must really enter that area. The observer runs before score evaluation and rejects contacts after the scoring plane has already been crossed.
- Default reaction delay is 0.16s, enforced both on threat acquisition and against the authoritative release timestamp, including shots arriving between Update calls. Keeper tracking distinguishes ownership/team and approach velocity; shot/pass/drop metadata remains in BallController. A harmless distant pass causes no remote save.
- At an eligible fast contact, default save probability is 0.8; conditional catch probability is 0.65. Each keeper has a configurable seeded System.Random (default seed 173), used only at new contact attempts. The contact latch prevents rerolling every frame; recovery defaults to 0.5s. Deterministic tests force probability endpoints and known geometry.
- Catch moves the ball to its actual contact point and uses the existing ownership assignment/holding behavior. Deflection reflects incoming velocity around the physical contact normal with default restitution 0.65 and remains unowned/collectible after 0.15s. Glancing reflections can continue into the net and still score legitimately. A keeper cannot cancel a shot outside its hand reach or rescue a goal retroactively.
- A slow collectible loose ball actually within the hand sphere can be collected without a probabilistic fast-shot roll, including after it settles inside a prior contact area. No remote proximity ownership assignment is added. Keeper holds cannot score, and another athlete cannot simultaneously acquire them.

Distribution and restart:

- Each keeper secures possession for 1s by default, considers outlets every 0.2s, prefers forward field teammates with clearer lanes/less pressure and valid range, and aims before release. Current keeper pass speed is 11m/s, maximum target range 22m and receiving window 5s; these are independently tunable from field passes. The ball physically travels and can miss; only the existing legitimate reception/pickup changes owner.
- If no suitable outlet becomes available by the default 3s maximum hold, the keeper performs a physical clearance into the pool. A short aiming/settling allowance may add to that timer. No goalkeeper may stay human-selected or hold the ball indefinitely.
- All six motors pause/reset through the existing roster. Match Changed events reset keeper reaction/recovery, contact latches and distributor target/timers. Ball reset clears owner/reception/shot state. Scores and valid field selection persist. There are no delayed distribution coroutines/actions that survive a goal.

### Milestone 7 verification and handoff

Fresh pre-change baseline: both existing Edit Mode setup suites and ten existing Play Mode suites passed before adding keeper behavior. Background keyboard routing required temporary Editor-only Input System overrides (AllDeviceInputAlwaysGoesToGameView / IgnoreFocus) for injected-device tests while the Editor was unfocused; restore the original PointersAndKeyboardsRespectGameViewFocus / ResetAndDisableNonBackgroundDevices at handoff. Gameplay input settings/controls are not changed for this test environment.

Final automated verification in Unity 6000.6.7f1:

- Runtime/editor compilation completed without errors. All **17 final verification suites** compiled with zero diagnostics and passed: 14 Play Mode suites and three Edit Mode setup suites. Actual structured outputs are in ignored `Logs/Milestone7Final-*.json`; the 12 pre-change baseline results are in `Logs/Milestone7Baseline-*.json`.
- Original field regressions passed: MovementSandboxVerification, CameraBallPrototypeVerification, GoalScoringPrototypeVerification, TeamPlayPrototypeVerification, PossessionSelectionVerification, FriendlyAIVerification, OpponentAIVerification, TacticalOffenseVerification, StealDefenseVerification and TwoVTwoMatchVerification. Keepers are explicitly isolated in these field fixtures, preserving the previous movement/input, physical pass/shot, selection, scoring, stealing and restart assertions. AIPrototypeSetupVerification and TwoVTwoSetupVerification also passed on the full scene while checking field-only role counts and total physical actor counts correctly.
- GoalkeeperSetupVerification passed in Edit Mode: repeated final setup retained 143 objects, six distinct IDs/start markers, exactly one keeper per team, four field players, one observer and two distributors; keeper/camera tuning and shared references were preserved. No duplicate objects/components were created.
- GoalkeeperPositionVerification passed: both assigned goals/teams, keeper selection rejection, smooth lateral tracking to both sides, fixed waterline, enforced goal envelope and central return.
- GoalkeeperSaveVerification passed: 80m/s swept catches at both goals, one authoritative owner, no keeper selection/camera transfer, no duplicate catch, keeper-held ball beyond the plane rejected, valid inside-post shot outside reach scored, central reflection became collectible, glancing deflection entering the net scored, saveChance=0 allowed a physical goal without rerolls, and a 2s reaction delay prevented a point-blank high-speed catch. Probability endpoints and stationary-keeper fixtures made the outcomes deterministic.
- GoalkeeperDistributionVerification passed four physical collect/secure/pass/receive cycles (two per team), legitimate field reception/exclusive ownership, Blue field auto-selection/camera and White control isolation, outlet support decisions, plus a no-eligible-outlet timed clearance. Pass velocity/travel was checked before reception.
- GoalkeeperMatchVerification passed both teams' physical goals, two full six-athlete freeze/reset snapshots, cleared keeper recovery/ownership/outlet state and no stale post-goal distribution. A further 20 simulated seconds of live six-athlete play preserved field-only Blue selection/camera, exclusive possession, bounded keepers, non-stalling keeper holds, field-only pressing/pursuit and active offensive shooting.
- Final Console ground truth showed zero errors, zero warnings and compilationFailed=false; historical Pipeline journal counts were not treated as the current Console status. No Console clearing was used to hide failures.
- Actual Game-view captures `Logs/Milestone7Goalkeepers.png` and `Logs/Milestone7Catch.png` were visually inspected for the placeholder keeper and caught ball near the goal. Preview probabilities were restored, and runtime preview scores are not saved. This is not developer manual game-feel or mobile-performance verification.
- At the Milestone 7 implementation handoff Play Mode was stopped and temporary test overrides were restored/disabled. Original Player/URP settings, packages/build scene list and SampleScene were unchanged; new assets had `.meta` files and no AI Git write occurred. The developer subsequently manually tested and backed up Milestone 7 as `a1374a8` before authorizing Milestone 8.

Historical Milestone 7 limits (see Milestone 8 for the later control/charge changes): shots/passes stayed flat at the waterline; the hand volume was spherical with seeded skill decisions. That milestone added no charged shooting, manual keeper control, advanced trajectories/dives, official rules/clocks, halftime, fouls, stamina, full teams, final art, backend, multiplayer or economy. Keeper distribution/save balancing, kinematic field overlap and a selected-field camera leaving distant saves outside view remained limitations. Both keepers stayed in their station envelopes. No SAVE HUD, replay or cinematic effect was required.

Historical Milestone 7 manual instructions used immediate Space shots and AI-only keepers. Its no-keeper-selection rule is intentionally superseded for Blue possession by Milestone 8 below; do not apply the historical rule when testing the current scene.

To examine outcomes deliberately, during Play select `Actors/WhiteGoalkeeper > Prototype Goalkeeper` in the Inspector: temporarily set Save Chance=1 and Catch Chance=1 for catches, Catch Chance=0 for deflections, or Save Chance=0 for missed save attempts. Reaction Delay and Save Reach remain physical limits, so chance 1 does not guarantee an unreachable/late save. Play Mode tuning reverts on exit; do not save test overrides as production tuning accidentally. Keeper Hold Duration/Maximum Hold are on Goalkeeper Distribution. Moving/turning speed is on Swimmer Motor. Inspect `Window > General > Console` after play. No manual wiring is required.

Historical Milestone 7 setup: `Tools > Water Polo > Set Up Goalkeeper Prototype` retained 143 objects. The developer subsequently manually tested, committed and pushed it as `a1374a8` (`Implement AI goalkeepers and ball distribution - Milestone 7`). The current final setup command and changed selection/shooting policy are recorded in Milestone 8 below. No Git write operations are performed by AI or scene setup.

### Milestone 8 — Manual keeper possession, charged shots and readability

Historical Milestone 8 starting point: main at `a1374a8`, matching stored origin/main, with only old-plan.md untracked. No AI Git writes occurred. The developer subsequently manually tested this milestone successfully and backed it up as `734d8da` (`Implement manual goalkeeper control and charged shooting - Milestone 8`) before authorizing Milestone 9. Its original automated evidence below remains valid as historical coverage; device performance remains unverified.

#### Files and responsibilities

New files (each new Assets file has its Unity-generated `.meta`):

- `Assets/Scripts/Gameplay/ShotCharge.cs`: per-Blue-athlete human preparation state; reusable `BeginCharge`, `ReleaseCharge`, `CancelCharge` commands, independent of keyboard/UI and ball ownership.
- `Assets/Scripts/Gameplay/ShotPowerBar.cs`: one reusable uGUI presentation component on the existing score Canvas. Caches the selected actor's charge component only on target changes, updates an existing fill rectangle/color, and hides on release/cancellation/lock.
- `Assets/Editor/Milestone8PrototypeSetup.cs`: final repeat-safe setup menu, Blue keeper human adapters/ring, three charge components, one bar, explicit attacking-goal camera reference and readable goal material. New camera defaults apply only when the bar controller is first created; repeated setup preserves camera/charge/keeper tuning.
- `Assets/Materials/Prototype/ReadableGoalFrame.mat`: white URP Unlit frame presentation; no scoring/collision geometry change.
- `Tools/Verification/Milestone8Verification.cs`: focused `KeeperControl`, `ChargedShooting`, `Presentation` Play Mode entries and `Setup` Edit Mode entry. Runtime entries guard against being run in Edit Mode before mutating fixtures.

Modified files:

- `Assets/Scripts/Gameplay/PlayerSelection.cs`: possession-based temporary Blue keeper eligibility, remembered field fallback, field-only manual cycle and unlock cleanup. BallController ownership events remain authoritative.
- `Assets/Scripts/Gameplay/PrototypeGoalkeeper.cs`: suspend positioning while human-controlled; 3.2m/s configurable carrying speed; ordinary pool bounds with 1.1m clearance; swim back to the goal station after release/loss instead of snapping. Interception remains available only at the goal station. Configurable protection distance is 1.5m from the station envelope.
- `Assets/Scripts/Gameplay/GoalkeeperDistribution.cs`: reset pending AI distribution decisions while human control is active; White keeper and explicitly deselected Blue keeper retain AI distribution.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: transient speed override for human keeper carrying; cleared on stop/disable/reset. Existing field physics/acceleration/waterline movement remains shared.
- `Assets/Scripts/Gameplay/BallController.cs`: near-goal keeper steal protection now depends on its configured station distance; a keeper carrying into the pool uses the existing opposing steal rules. Ownership/release, swept saves and scoring remain single authorities.
- `Assets/Scripts/Gameplay/SwimmerBallInteraction.cs`: speed-taking `TryShoot(float)` overload shares the existing physical release; parameterless `TryShoot()` retains direct AI shooting.
- `Assets/Scripts/Gameplay/BallInteractionInput.cs`: press begins charge, release executes it; passing or disabling the adapter cancels charge. Handoffs suppress same-frame button replay and wait for an already-held shoot button to be released before accepting a new charge.
- `Assets/Scripts/Gameplay/GameplayCamera.cs`: smooth keeper/field height/distance blending and a small explicit attacking-goal framing bias for field play. Existing elevated angle, ball influence, focus smoothing and pool limits remain.
- `Assets/Editor/GoalkeeperPrototypeSetup.cs`: wires the shared PoolBounds reference required by keeper carrying/return.
- `Assets/Scenes/PrototypePool.unity`: all components/references, Blue keeper marker, bar hierarchy, camera tuning and visible frame material saved. SampleScene is unchanged.
- `Tools/Verification/{GoalkeeperSaveVerification,GoalkeeperMatchVerification,GoalkeeperDistributionVerification,GoalkeeperSetupVerification}.cs`: intentional Milestone 8 expectations replace the old Blue-no-control rule. Physical contact, ownership, pass travel, goal/restart and uniqueness assertions remain. Blue AI distribution tests explicitly return control to a field player before expecting automatic release. Live holding timeouts apply to AI keepers, not a deliberately controlled human keeper.
- Root `plan.md`, `README.md`, `AGENTS.md`: synchronized current behavior, controls, actual evidence and manual-test status. No old migration reference was removed.

#### Control, shooting and presentation policy

- A genuine friendly ownership event selects the Blue carrier, including its goalkeeper. Approaching a shot or attempting a pass does not select the keeper. Selection enables exactly one human movement/action pair, moves the existing-style green ring and retargets the camera. White possession never selects White.
- C cycles only registered friendly **field** players, including when used during a keeper hold; possession is unchanged. Deselecting the keeper intentionally resumes its AI. Keeper release/loss falls back to the last valid selected field player (or another eligible field); actual friendly pass reception then selects the receiver. Lock/reset cleanup cannot replay stale keeper possession.
- The controlled keeper moves/turns through SwimmerInput/SwimmerMotor at the fixed waterline, constrained inside the normal playable pool. Distribution and AI movement do not fight input. After release/loss it swims home and resumes goal-local tracking. Its trigger collider and conservative outer clearance prevent crossing goal geometry; this milestone does not add hard athlete-to-athlete collision resolution.
- Hold Space (gamepad south button likewise) while possessing to begin charge; release to shoot using facing at release. Duration defaults to **0.7s**, speed **10–22m/s**, with clamped maximum and a usable minimum for quick taps. Holding does not fire or alter possession; releases are one-shot. Loss, C handoff, pass, lock or input disable cancels. AI calls the existing immediate **12m/s** default shot API (or its existing chosen speed), without keyboard simulation. Future touch buttons can call the same charge commands.
- One bottom-centre bar uses the existing 1920x1080 CanvasScaler. It appears only while charging, fills green → yellow → orange/red and hides on cancel/release. No UI objects, scene searches or materials are created per frame. It does not control ball physics.
- The pool remains **20m X × 30m Z**. Both physical goal mouths remain **3m × 0.9m**, with their existing whole-ball directional planes, posts and keeper reach. Only frame material and camera presentation changed. Field camera: distance **10m**, height **11.8m**, pitch **50°**, FOV **48°**, focus smoothing **0.3s**, attacking-goal bias **1.2m**. Keeper framing: distance **8.5m**, height **10.5m**, profile smoothing **0.4s**. These are Inspector settings, with smooth retargeting and no new camera mode/cinematic system.
- Existing match handling freezes/resets all six athletes, clears possession and temporary charge/keeper actions, preserves scores and restores valid field input. A held keeper ball remains ineligible for scoring; valid physical deflections into the net still count.

#### Fresh verification in Unity 6000.6.7f1

Runtime/editor compilation passed. Executed checks compiled with **zero script diagnostics**. Structured final outputs are local ignored `Logs/Milestone8-*.json`:

| Executed entry/suite | Result / important coverage |
| --- | --- |
| `Milestone8Verification.KeeperControl` | PASS: actual Blue catch; exactly one input/ring/camera; AI hold/distribution suppression beyond timeout; WASD movement; fully charged keeper shot; lateral facing tap shot; swimming return without teleport; immediate physical B pass and receiver selection; C field-only; locked/unexpected loss; opposing steal cancels keeper charge; White keeper remains AI/distributes. |
| `Milestone8Verification.ChargedShooting` | PASS: injected Space hold/release, max clamp, usable quick tap, physical speed/direction, one shot per release, ownership preservation; cancellation on loss/switch/lock and no held-button replay; bar visibility; direct AI shooting and touch-ready API. |
| `Milestone8Verification.Presentation` | PASS: keeper viewport visibility, gradual field return, attacking field/held ball/goal in view; unchanged 3m × 0.9m opening. At the current 861x373 Game view the goal projects to approximately 65px wide. Actual `Logs/Milestone8PowerBar.png` was inspected for brighter frame, actor/ball readability and the visible bar. This is a prototype presentation check, not final game-feel validation. |
| `Milestone8Verification.Setup` | PASS: repeated final setup retains **147 objects**, six athletes/four field roles, three charge components, one bar and exactly one human input pair per Blue athlete; camera tuning survives YAML save/reload; saved field/keeper AI remains enabled. |
| `TeamPlayPrototypeVerification` | PASS: WASD isolation, C selection/ring/camera, exclusive possession, physical B/return passes, misses/expiry, human Space shots, two goals/restarts and subsequent pickup/pass/shot. |
| `StealDefenseVerification` | PASS: F input, range/facing/protection/shielding/cooldown, atomic successful steal/selection, team/lock rejection, coordinated defense/hysteresis and real AI pressure/steal. |
| `GoalScoringPrototypeVerification` | PASS: both swept whole-ball planes, posts/crossbar, held/reverse/missed shots, high-speed and long-shot goals, exact-once score/HUD and repeated restarts. |
| `TacticalOffenseVerification` | PASS: White pickup/advance/turn/physical shot/goal, physical passing/reception and anti-stall release. |
| `GoalkeeperSaveVerification` | PASS: **80m/s** catches at both goals with the new Blue/White selection policy, exclusive ownership, held-ball goal rejection, reach-limited goals, physical reflection, glancing-deflection goal, deterministic skill/reaction endpoints and no duplicate rerolls. |
| `GoalkeeperMatchVerification` | PASS: both teams' physical goals, two complete six-athlete freeze/reset snapshots, no stale distributions; **20 simulated seconds** of live six-athlete play with valid selection/camera, ownership, field-only tactics, bounded keepers and active offensive shots. |
| `GoalkeeperDistributionVerification` | PASS: four physical collect/secure/pass/receive cycles across both teams, Blue field opt-out restoring keeper AI, legitimate receiver selection and no-outlet timed clearance. |
| `GoalkeeperSetupVerification` | PASS: repeated keeper setup preserves **147 objects**, teams/roles/start markers/shared references, optional Blue human adapters, keeper reach and camera tuning. |

The selected consolidated regression was run after feature checks; all 17 historical suites were **not** rerun after minor edits. Other historical movement/camera/possession/friendly/opponent/2v2 setup suites and the old standalone keeper-position suite were not freshly run in this milestone; their previous results remain historical. Their critical selection/physics/tactics interactions were covered by the focused and selected regression above. Early verification exposed an Edit Mode test invocation, YAML float precision in a strict tuning comparison, and insufficient projected goal size at the first camera tuning; these were investigated, corrected and the affected assertions passed in final runs. No assertion about physical saves, goals or exclusive ownership was removed to force success.

Final current Unity Console ground truth: **0 errors, 0 warnings, compilationFailed=false**. Historical Pipeline journal counters are not the current Console; no Console clear was used to conceal failures. Play Mode is stopped; temporary Input System focus/background, Pipeline auto-tick and background-run overrides are restored/disabled. Incidental Player/URP settings were restored and generated Pipeline configuration moved to ignored Logs; packages/build settings remain unchanged. New assets have `.meta` companions. SampleScene hash remains `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.

#### Manual handoff, limits and next step

The saved scene is ready: Project panel **Assets > Scenes > PrototypePool**, double-click, press **Play**, click **Game**. WASD/arrows swim, E collects, B passes, **hold/release Space** shoots, C cycles Blue fields, F steals. Optional setup: stop Play Mode, save intended edits with **File > Save**, then **Tools > Water Polo > Set Up Milestone 8 Prototype**; no manual reference wiring is needed.

Test a quick tap and a full charge on a Blue field carrier; confirm the bar disappears on release, pass, loss and C switching. Let White attack the Blue goal: an actual catch should select the keeper, move its ring/camera, and suspend AI distribution even while holding beyond three seconds. Swim/turn and B-pass to a visible field outlet, or charge/release a facing shot; field control must return and the keeper must swim home. White keeper catches should remain autonomous. During a Blue keeper hold C may return to a field player deliberately, resuming keeper AI distribution. Repeat at both goals and check scores, all-six restarts and the next pickup/pass/shot.

Inspector tuning: **Actors > [Blue goalkeeper] > Prototype Goalkeeper** for carrying speed/protection; the three Blue actors' **Shot Charge** for duration/min/max speeds; **CameraRig > Main Camera > Gameplay Camera** for field/keeper framing, smoothing and goal bias, and **Camera** for FOV. Goal Mouth dimensions and keeper save/reaction values are unchanged.

Limitations: manual gameplay feel, goal/camera preference across aspect ratios, gamepad feel and mobile performance/builds remain unverified. Camera still follows the selected actor; a distant ball/save may leave frame. Human keeper possession has no automatic timeout; C explicitly gives control back to fields. Interception remains near the keeper station, rather than a general field catch mechanic. Ordinary kinematic overlap/soft-spacing limits remain. No touch HUD, lob/skip/charged AI shots, dives, clocks, fouls, full teams, rigged art, realistic water, backend, monetization or later milestone was implemented.

The developer completed Milestone 8 manual review and backup as `734d8da`, then explicitly authorized Milestone 9. The current milestone below supersedes this historical handoff's team count, clock-free state and setup command. Only the developer may perform Git writes; old-plan.md stays excluded.

### Milestone 9 initial implementation — historical 3v3 stage

**Historical record:** the following initial-stage counts (eight), 90/480 timing, 30-second possession clock, fixed ends and manual instructions are superseded by **MILESTONE 9 — COMPLETE FULL-TEAM PROTOTYPE EXPANSION** below. Preserve the original evidence without treating old fixture expectations as current defaults. This is one milestone, not a new Milestone 10.

Authorized scope: three field swimmers plus one keeper per team, coordinated tactics, match/shot clocks, shared sprint/stamina, real aerial lobs and an extended prototype HUD. Initial read-only inspection found `main` at **734d8da**, matching stored origin/main, with only old-plan.md untracked. Milestone 8 is developer-manually verified. No Git checkpoint/write was performed. Milestone 9 manual playtesting and device builds/performance remain pending.

#### Team structure and tactical model

- Eight athletes: three Blue human-eligible fields, three White AI fields and the two existing keepers. Existing four field identities and both keepers are retained. Added IDs are `prototype-blue-3` and `prototype-white-3`; every actor has its own start marker. A `FieldPositionRole` (Central/Left/Right) is separate from AthleteRole, so keepers are never counted as field formation slots. Loops use roster membership rather than a fixed three-player runtime array.
- Prototype starts: Blue centre `(0,0,0)`, wings `(-5,0,-2)` / `(5,0,-2)`; White centre `(0,0,6)`, wings mirrored at Z=8. Keeper starts and goal geometry remain unchanged. First expansion initializes these; repeat setup preserves subsequent start/role tuning. Future full formations still need additional role/slot definitions and balancing.
- PrototypeTeamTactics remains the shared team context. Role lanes provide central/wide support, clamp forward progress near goal and maintain separation from the carrier. One presser is selected with assignment intervals and hysteresis; other defenders receive distinct marking ranks or zone lanes. Press closes space aggressively; Zone protects central/wide stations nearer the defended goal and only presses very close danger. Z toggles Blue strategy; White chooses with a three-second interval and 7m/11m danger hysteresis.
- Pass scoring includes range, progress, receiver pressure/openness, lane obstruction, direction and receiver shot quality; pass tuning remains serialized. Selected AI receivers use the shared directed physical pass API. Defenders continue defending/contesting during flat passes; only the intended receiver waits for its normal pass. During lobs eligible pursuers/receivers move toward the estimated landing point; no high-ball ownership is fabricated.
- Shot quality includes range/alignment, blockers/pressure and a keeper-position penalty. Effective shooting range is capped by 85% of BallController's unobstructed `speed / waterDrag` travel estimate, so low configured shot power cannot receive an optimistic out-of-range quality score. Aim chooses a mouth-width/radius-safe corner opposite the opposing keeper, with configurable AI shot speed (default 16m/s). The existing minimum/maximum hold timers remain; under five shot-clock seconds the carrier prioritizes a shot without waiting for a fresh minimum hold. This is a deterministic prototype quality heuristic, not a calibrated real-world scoring probability.
- The available local old-plan descriptions of TeamSide/WaterPoloBrain/MatchContext, compressed MatchTimer, ShotClock keeper-hold behavior and StaminaSystem were reviewed conceptually. Actual legacy source groups were not present; no 2D source/physics or old coordinates were imported.

#### One authoritative match phase and transition contract

PrototypeMatch retains whole-ball directional goal detection and score authority and now owns `PreMatch`, `Playing`, `GoalRestart`, `TurnoverRestart`, `QuarterBreak`, `FullTime`. `IsPlaying` is the shared competitive action guard. `IsRestarting` remains a compatibility query for goal/turnover restarts, not a second phase flag. Keeper/field/steal/interception adapters and BallController reject stopped play. PlayerSelection's input lock is applied by the central transition; charge/input disable cancels pending shots.

- MatchClock is a serialized timing model inside PrototypeMatch, not another independent ticking manager. Defaults: **four quarters, 90 live seconds per quarter, 480 displayed seconds**. Display ratio is 480/90. Physics-step time advances only while Playing; goal/turnover pauses, quarter breaks, ready state and full time do not consume clock time. Default time scale is unchanged.
- PossessionShotClock is one event observer advanced by PrototypeMatch after goal and quarter checks. Default **30 live seconds** per team possession, independent of displayed quarter compression. A real opposing acquisition resets once; same-team passing, human switching, failed steals and keeper collection/distribution preserve remaining time. Release/loose flight retains the last possession's ticking clock; before first acquisition the shot clock waits. Goal and quarter restarts establish a fresh clock.
- A completed goal crossing observed in a physics step takes priority over that step's clock deadline. Scores increment once and the existing GOAL/restart delay pauses both clocks. If little quarter time remains, the quarter can finish shortly after the goal restart; no extra goal is awarded during that transition.
- Shot-clock expiration requests one controlled turnover: lock inputs, pause all eight motors, cancel charge, clear owner/pass/flight, show turnover notification, then reset legal positions. The awarded team's first registered field player receives a range-checked BallController pickup at its start. Blue selection is restored only to eligible Blue athletes. Clock/score authorities are not duplicated.
- Quarter end locks all eight, clears pending ball movement and shows Continue/Start. Continue resets starts/ball and begins the next quarter, preserving score and stamina. Last-quarter end shows BLUE/WHITE final score plus WIN/LOSE/DRAW from Blue's perspective. Restart Match resets scores, quarter/shot clocks and stamina. Return to Prototype opens the **same scene's paused ready state**; Continue/Start starts a fresh match. No career scene or external menu was added.
- Every central transition notifies the existing team/keeper/match listeners to clear obsolete decisions, reaction/recovery and distribution targets. Restarts restore field selection if an old keeper hold is no longer valid. No delayed throw coroutine or arbitrary action-delay workaround was introduced.
- **Halftime ends swapping is deferred**: goal score attribution, goal-local keeper stations, tactical goals, camera attack reference and restart positions currently use fixed ends. They must change atomically in a separately verified feature. Blue continues attacking +Z and White -Z through all quarters.

#### Sprint and stamina

SwimmerStamina is shared by every field motor, with the same rules for humans and AI. Hold Left Shift with movement to request sprint. Normal tuned speed remains 4m/s; default multiplier is **1.5** (6m/s at full input) and sprint acceleration multiplier **1.25**. Default normalized drain is **0.16/s**; recovery **0.12/s** at rest and **0.06/s** during ordinary swimming. Values clamp to [0,1]. Exhaustion blocks further sprint until **0.25** recovery, preventing rapid zero-stamina oscillation. Field AI requests bursts for distant loose balls, defensive pressure and useful unpressured carries; it has no unlimited White-team stamina exception.

The motor advances fatigue once per enabled live physics step. Stamina freezes during stoppages, persists through goals/turnovers/quarters and resets on the authoritative NewMatch event. Selection/stop clears sprint requests without refilling stamina. Keepers have an explicit non-sprinting profile; controlled carrying remains 3.2m/s. One ActiveStaminaHUD bar follows selected Blue fields or labels the keeper profile.

#### Real aerial flight

- B remains the flat pass; **L** calls LobPassing. It chooses a modest assisted facing direction and eligible field recipient, never teleports possession. Default unassisted range is **8m from release**, max assisted range **16m**, flight duration **1.3s**, assist cone **30°**, correction cap **12°**.
- BallController remains the sole authority. `BallReleaseKind.Lob` / `IsAirborne` enable actual Rigidbody gravity, unlock Y and remove water drag for flight. Horizontal velocity reaches the bounded target over the requested duration; initial vertical velocity is `-Physics.gravity.y * duration / 2`, approximately a 2.07m rise for 1.3s at default gravity. LandingPoint predicts from the current physical velocity/height, so collision changes affect pursuit.
- Rising/high balls cannot be collected. A descending ball within **0.65m above SurfaceHeight**, after release protection, can be legitimately received/collected within the existing horizontal reach. This prevents unrealistic high pickups and permits low reception before a ball rests on a swimmer collider. Opponents can contest eligible low/landed balls. Keeper interception still requires actual swept 3D hand contact and its existing reach/reaction rules.
- On reception, landing, deflection or reset, the shared controller restores flat water physics and exclusive possession rules. Landing synchronizes the water pose before restoring the Y lock/interpolation, avoiding a stale PhysX underwater constraint anchor. Releases use SwimmerMotor physics position/facing, avoiding stale interpolated render poses immediately after resets. Flat charged shots and normal passes keep the same release authority.
- Existing GoalMouth whole-sphere checks include vertical clearance; above-crossbar crossings remain invalid and there is no new scoring authority. Airborne XZ containment reflects at pool limits, while PhysX handles posts/athletes. Low keeper deflections return to the prototype's flat-water model. A pooled inexpensive ground disk cues height only while airborne; no per-frame object/material creation or water particles.

#### Camera, HUD and setup

The existing camera remains the only camera behavior. Initial M9 field framing is distance **10.5m**, height **12.2m**, pitch **50°**, FOV **50°**, goal bias **1.5m**; nearby-ball influence/range/max offset become **0.2 / 12m / 2m**. Existing keeper profile and smooth retargeting remain. These initial changes apply only on first M9 presentation setup; repeat setup retains custom tuning. The camera is not a full-pool overview; a distant ball or athlete can leave view.

The existing Canvas/scoreboard/charge bar remain. MatchHUD adds quarter/display time, shot time, defense label, turnover message and quarter/full-time/ready panel. ActiveStaminaHUD adds the selected-athlete bar. UI objects and button listeners are created/wired once; no UI object or scene search is created every gameplay frame. uGUI uses the new Input System UI module/EventSystem and existing resolution scaler. This is a prototype landscape layout, not a finished touch interface.

Saved scene command: stop Play Mode, save intended edits (**File > Save**), then **Tools > Water Polo > Set Up Milestone 9 Match**. No manual Inspector wiring is needed. Repeated setup retains eight identities/start markers, one ball/match/shot-clock authority and current tuning; old lower-level builders accept/preserve extra registered athletes. SampleScene is never saved or overwritten.

#### Files created or modified

New runtime files under `Assets/Scripts/Gameplay/`: `MatchClock.cs`, `PossessionShotClock.cs`, `SwimmerStamina.cs`, `LobPassing.cs`, `TeamStrategyInput.cs`, `MatchHUD.cs`, `ActiveStaminaHUD.cs`, `BallHeightCue.cs`. Each has its Unity-generated `.meta`.

New Editor files: `Assets/Editor/Milestone9PrototypeSetup.cs` and `MatchHUDSetup.cs`, both with `.meta`. New material: `Assets/Materials/Prototype/BallShadow.mat` and `.meta`. New verification file: `Tools/Verification/Milestone9Verification.cs` (outside player builds).

Modified runtime files under `Assets/Scripts/Gameplay/`: `SwimmerIdentity.cs` (field roles), `PrototypeTeamTactics.cs`, `WaterPoloDecision.cs`, `PrototypeSwimmerAI.cs` (coordinated tactics, action/urgency/sprint), `PrototypeMatch.cs` (phases/clocks/transitions), `PrototypeScoreboard.cs` (GOAL only for goal restarts), `BallController.cs` (live action guards, real flight/low pickup/restoration, physics release pose), `BallInteractionInput.cs` (L action), `SwimmerMotor.cs` (shared stamina and authoritative Facing), `SwimmerInput.cs` (Shift), `SwimmerPassing.cs` (physics facing), `SwimmerSteal.cs`, `PrototypeGoalkeeper.cs`, `GoalkeeperInterception.cs` (central live-phase guards).

Modified Editor files: `Assets/Editor/TeamPlayPrototypeSetup.cs`, `BasicAIPrototypeSetup.cs`, `TwoVTwoPrototypeSetup.cs` (preserve expanded teams; expose the shared setup helper internally). Modified scene: `Assets/Scenes/PrototypePool.unity`. Updated existing verification: `Tools/Verification/GoalScoringPrototypeVerification.cs` parks all non-shooters away from its clear-lane fixture, including the added central defender; `StealDefenseVerification.cs` explicitly selects Press and suspends/restores White automatic mode for its pressure-specific assertions. No physical goal/steal assertions were removed. Updated root docs: `AGENTS.md`, `README.md`, `plan.md`. No packages, SampleScene or unrelated project settings changed. Existing `.meta` files and old-game references remain intact.

#### Verification and manual handoff

Fresh verification in the running **Unity 6000.6.7f1** Editor: runtime/editor compilation passed. All **13 distinct executed verification entry points** passed (seven new focused/integrated entries plus six reused entries). There are 12 passing final structured result files in ignored `Logs/Milestone9-Final-*.json`, plus the initial passing expansion result in `Logs/Milestone9-A-Expansion.json`. All final verification scripts compiled with zero diagnostics.

| Executed entry | Actual coverage / result |
| --- | --- |
| `Milestone9Verification.Setup` | PASS: **183 objects**, eight unique athletes/start markers, 3+1 per team, distinct Central/Left/Right roles; repeat setup preserves identity, camera/stamina Inspector tuning, one match/ball/shot clock, valid Input System UI references and single persistent button listeners. |
| `Milestone9Verification.Expansion` | PASS: C cycles three Blue fields, White excluded, exactly one movement input, eight athletes bounded at the waterline. |
| `Milestone9Verification.Tactics` | PASS: one presser, distinct Press/Zone positioning, three zone lanes, spaced role support, obstructed/open pass lanes, deterministic stable decisions, low-power range rejection and shooting urgency under five possession seconds. |
| `Milestone9Verification.Clocks` | PASS: live countdown; real same-team pass/reception and keeper hold preserve time; C preserves time; opposing acquisition resets once; charged-shot cancellation on timeout; stopped clocks during turnover; legitimate opposing restart; compressed quarter display, all-eight freeze/reset, quarter continuation, full time DRAW/panel, ready state and new match. Shortened runtime-only fixture tuning was restored. |
| `Milestone9Verification.Stamina` | PASS: injected W+Shift reaches sprint speed and drains, release restores tuned speed, rest recovers, held Shift survives control switching with only the new actor sprinting; exhaustion/recovery threshold/clamps, persistence through turnover, new-match reset and real AI pursuit using the same drain. Keeper non-sprint profiles verified. |
| `Milestone9Verification.Aerial` | PASS: injected L, real gravity/height above 1m, high pickup rejected, low physical receiver acquisition/auto-selection, immediate subsequent flat shot, missed-lob collectible waterline landing, above-crossbar geometry rejection and in-flight turnover cleanup. |
| `Milestone9Verification.Integration` | PASS: actual last-seconds goal before quarter deadline, retained score/goal pause then quarter break; keeper catch preserving same-team countdown and human selection, controlled physical keeper lob/field reception, turnover cleanup, high lob near keeper outside real reach, quarter transition canceling flight, **six eight-athlete reset snapshots**, then **16 simulated seconds** of all-AI-enabled play with exclusive possession, bounded athletes/fatigue, valid selection/camera and an actual White offensive shot. Z input toggling verified. |
| `Milestone8Verification.KeeperControl` | PASS: Blue catch/control/ring/camera, AI distribution suspension, manual swim/fully charged and lateral keeper shots, swimming return, physical B outlet/reception, field-only C, locked/unexpected loss, steal canceling keeper charge and unchanged White AI distribution. |
| `Milestone8Verification.ChargedShooting` | PASS: actual hold/release, quick tap, max clamp, no early/duplicate shot, facing direction/speed, loss/switch/lock cancellation, no held-button replay, reusable charge API/bar and direct AI shot compatibility. |
| `GoalkeeperSaveVerification.Run` | PASS: both **80m/s** catches/exclusive owner, correct Blue/White control policy, held-ball goal rejection, unreachable shot scores, real reflection, glancing deflection goal, seeded skill/reaction endpoints and no duplicate save rerolls. |
| `GoalScoringPrototypeVerification.Run` | PASS: both whole-ball planes, partial/held/reverse/post/crossbar/miss rejection, high-speed/long/cooldown-crossing goals, scoreboard/GOAL and repeated exact-once restarts. Clear-lane fixture now isolates the complete roster. |
| `StealDefenseVerification.Run` | PASS: F input/range/facing/protection/shielding/cooldown, atomic steal/auto-selection, same-team/lock rejection, explicit Press coordination/hysteresis and actual AI pressure/steal. Automatic strategy tuning restored after fixture. |
| `TacticalOffenseVerification.Run` | PASS: White real pickup/advance/turn/physical shot/goal, improved-option physical pass/legitimate White reception and configurable anti-stall release. |

The consolidated pass reused relevant earlier suites; it did not run every historical suite after each edit. Scoped fixture corrections were rerun, and focused stamina/integration checks followed the held-Shift handoff and allocation-free tactical-loop refinements. The final pure shot-range assessment addition was checked by Tactics and physical TacticalOffense; default shot tuning is unchanged. Other historical fixed-count setup/team suites were not freshly run; their 2v2/six-athlete assertions are historical until adapted for the current scene. No manual game-feel, long-run match balance, every aspect ratio or Android/iOS performance claim is implied.

Final Unity Console ground truth: **0 errors, 0 warnings, compilationFailed=false**. Historical Pipeline journal counters include earlier sessions and are not the current Console. No Console clearing was used to hide failures. Play Mode is stopped; temporary focus/background routing, auto-tick and Run In Background test overrides are restored/disabled. Incidental Player/URP settings were restored and generated Pipeline configuration moved under ignored Logs. SampleScene hash remains `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`. New assets have all companion `.meta` files; code/Markdown whitespace checks passed. Current source contains no gameplay scene-wide searches.

Actual Game-view captures `Logs/Milestone9Match.png`, `Milestone9QuarterBreak.png`, `Milestone9FullTime.png` were inspected for prototype state/HUD readability. Full-time capture uses the two-quarter shortened test fixture, not saved default match tuning. Default scene configuration remains four 90-second quarters and a 30-second shot clock. UI wiring/state was verified; developer mouse/controller feel and final presentation remain manual review tasks.

Known prototype limits: fixed ends/no halftime swap; simple geometric roles/spacing and deterministic pass/shot heuristics need balancing; kinematic athlete overlap is still possible; no referee/out-of-play process (an aerial ball may physically lodge on goal geometry until the possession-clock restart); AI currently chooses flat passes while the shared lob API is available to both teams and the controlled keeper. Keeper deflections flatten to water after contact; no advanced lob/skip shots or dives. Stamina recovery during breaks is frozen, not an official fatigue/substitution model. No official fouls/exclusions, touch HUD, mobile builds, full teams, final art, backend, multiplayer, economy or later milestone was added.

Open **Assets > Scenes > PrototypePool**, Play, click Game. WASD/arrows swim; E collects, B passes, Space hold/release charges/shoots, C cycles three Blue fields, F steals, **Shift sprints, L lobs, Z toggles defense**. Test keeper catches/control, B/L distribution and charged keeper shots. Watch stamina across possessions and quarters. Quarter breaks require clicking Continue/Start; full time offers Restart Match and Return to Prototype. The default match has six minutes of live play plus breaks/restarts. For an optional faster flow-only review: stop Play, select **Hierarchy > Gameplay > PrototypeMatch**, expand **Inspector > Prototype Match > Clock**, set Real Seconds Per Quarter temporarily to 10, then Play; restore 90 afterward. The displayed quarter remains eight minutes. Keep the default quarter length when manually testing a full 30-second shot-clock possession.

Next task: developer manually reviews Milestone 9 (controls, AI pacing/spacing, fatigue, air pickups, clock transitions and HUD/camera). Only the developer stages/commits/pushes reviewed changes. Milestone 10 is not authorized. No Git write operation was performed; old-plan.md remains untouched and excluded.

### MILESTONE 9 — COMPLETE FULL-TEAM PROTOTYPE EXPANSION

Implemented on 2026-11-10 in Unity 6000.6.7f1, extending the initial M9 implementation in place. Read-only inspection found `main` at `734d8da`, matching stored `origin/main`, with the earlier M9 implementation already unstaged/uncommitted. No Git write operation or checkpoint was performed. The previous user-confirmed manual gameplay evidence belongs to Milestone 8 and earlier; **this full-team build requires developer manual review**. Available old-plan descriptions informed role/team/clock separation; no old source clone or Rigidbody2D implementation was imported.

#### Team composition and shared tactics

- Exactly **14 athletes: six fields plus one existing goalkeeper per team**. Initial eight athlete identities and existing GameObjects remain. New fields use `prototype-blue-4/5/6` and `prototype-white-4/5/6`; setup assigns unused cap numbers without changing the earlier identities. All twelve fields share the same physical motor, ball interaction, AI decision/action adapter and stamina components.
- `SwimmerIdentity.FieldPosition` gives six stable roles independently of team, cap, object name and goalkeeper AthleteRole. Previously serialized Central/Left/Right values 0/1/2 retain their indices as CentreForward/LeftWing/RightWing; CentreBack/LeftFlat/RightFlat add slots 3/4/5. Keepers remain outside field assignment, pursuit, human C cycling and role counts.
- New serialized `FieldFormation` entries store period-start, attack and zone offsets in team coordinates: lateral X and forward progress relative to pool centre/current AttackDirection. Runtime roster loops do not require exactly six; missing roles have a generic layout, and repeated roles receive extra spacing offsets. The default six slots are:

| Role | Period start (lateral, forward) | Attack | Zone |
| --- | --- | --- | --- |
| Centre Forward | (0, -3.5) | (0, 11) | (0, -6) |
| Centre Back | (0, -10) | (0, 3.5) | (0, -10) |
| Left Wing | (-7, -5.8) | (-7, 9) | (-6, -9) |
| Right Wing | (7, -5.8) | (7, 9) | (6, -9) |
| Left Flat | (-3.8, -7.6) | (-4, 6.5) | (-3.5, -6.5) |
| Right Flat | (3.8, -7.6) | (4, 6.5) | (3.5, -6.5) |

`PrototypeTeamTactics` converts these through current team direction and pool bounds. Attack slots translate with carrier progress, retain a minimum carrier separation and a goal-side centre back. This creates five off-ball options instead of five followers. Zone maintains six lanes with limited threat bias. Press chooses one suitable field-carrier presser with hysteresis; remaining defenders get distinct marking ranks blended toward their role zones. Keeper possession does not allocate a bogus field presser. Shared existing pass-risk/open-lane, shot-range/keeper/pressure, shot-clock urgency and sprint/stamina decisions remain. Goalkeeper distribution evaluates registered field teammates, now six candidates. No duplicate tactical brain or possession manager was added.

All six Blue fields cycle with C. Every legitimate friendly field acquisition still selects that recipient; a confirmed Blue keeper catch temporarily grants keeper control. Releasing keeper possession returns valid field control. White is never human-selectable. Existing ring, charge cancellation, input exclusivity and camera targeting remain centralized in PlayerSelection.

#### Four periods and possession clock

- MatchClock remains the single serialized period timer inside PrototypeMatch. Defaults: **4 periods, 80 real live seconds, 480 displayed seconds, ratio 6**. Display starts Q1 — 8:00; after 10 live seconds it is 7:00, after 40 it is 4:00, and after 80 the period expires once. Each period starts a fresh double-precision live accumulator; derived remaining time clamps to zero, including fixed-step floating-point boundary tolerance.
- PrototypeMatch.AdvanceLiveTime is the shared clock path for normal FixedUpdate and deterministic verification. It advances only in Playing, clamps to the earliest clock deadline, and ignores invalid/non-positive deltas. Goals, turnover restarts, quarter breaks, HalfTime, PreMatch and FullTime stop both clocks. A valid completed crossing already observed during the physics step takes precedence over that step's deadline.
- Q1 -> QuarterBreak -> Q2 -> **HalfTime** -> Q3 -> QuarterBreak -> Q4 -> FullTime. Continue resumes; full time retains scores and reports WIN/LOSE/DRAW. No extra time or shootout. Total default live time is **320 seconds (5 minutes 20 seconds)** plus stoppages.
- PossessionShotClock defaults to **28 actual live seconds**, independently configurable through duration and clockRate (default 1). It does not use the period factor 6. True opposing-team acquisition resets it once; loose flight, same-team passing, manual selection and same-team keeper possession preserve the current possession. Expiration enters the existing controlled opposing turnover restart, canceling charge/ownership/actions and pausing the quarter until resumption.
- The 28-second baseline follows [World Aquatics competition regulations, Part Six §9.2 (18 February 2026)](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf). This prototype does not implement the full foul/exclusion/rebound reset exceptions; its independent real-time tuning is deliberate.

#### Halftime, score attribution and restart contract

Halftime enters a stopped phase after Q2 and displays **HALFTIME — SWITCHING ENDS** with the current score. No end change occurs until Continue. PrototypeMatch then atomically remaps both teams' attack/defend GoalMouth references, each physical goal's current ScoringTeam, both keepers' DefendedGoal and reaction state, the human camera's attacking-goal reference, and every roster start marker. PrototypeRoster mirrors base poses 180 degrees around pool centre; it does not repeatedly rotate already-mirrored positions. The same athletes reset into their new defensive halves with current facing. Exactly one reversal occurs; a second Continue is ignored once Playing. Q4 retains Q3 ends. Restart Match restores the captured Q1 goals/base poses, clears the switch count, score and clocks.

Physical goal geometry/directional whole-ball crossing is unchanged (3m opening, 0.9m high). Crediting follows the team currently attacking that physical goal, independent of last shooter; this also handles own goals after reversal. Goalkeepers' station/tracking/save/distribution logic reads their new defended goals. Held balls cannot score. No visual-only camera rotation or permanent Blue/+Z scoring rule was introduced.

Period/new-match starts use a **neutral centre ball and separated own-half positions**, not an official sprint-duel/swim-off subsystem. After a goal, the **conceding team** is awarded a valid range-checked pickup by its first eligible field player after reset. Scores survive goals, turnovers and periods. Central stop/reset logic locks human actions, clears charge and obsolete flight/ownership, stops AI/keeper decisions, resets all fourteen athletes and restores valid selection. No queued keeper outlet survives a break. Halftime preserves the instantaneous camera pose and smoothly follows the changed target; it does not SnapToTarget during the ends swap.

#### Camera, HUD, setup and changed files

The existing camera keeps its consistent world orientation. Initial full-team setup widens field distance/height to 13/15m, FOV 52, goal bias 1.8m, keeper distance/height 10.5/12.3m and limited ball-aware focus. Existing smoothing, pool clamps and role blending remain; repeat setup preserves subsequent Inspector tuning. HUD reuses score, Q/time, shot clock, stamina, defense, charge bar and modal buttons, adding the halftime state rather than another Canvas/controller.

Open the already saved `Assets/Scenes/PrototypePool.unity`. Optional repeat-safe setup outside Play Mode: **Tools > Water Polo > Set Up Milestone 9 Match**. No manual wiring is required. Exactly six fields and one keeper per team are registered; existing actors, goals, UI and camera are preserved. Unexpected excess fields cause a clear setup failure rather than deletion. The verified scene has **243 GameObjects**, fourteen unique athlete identities and one existing match/ball/shot-clock authority. SampleScene is unchanged.

Files added by this final expansion: `Assets/Scripts/Gameplay/FieldFormation.cs` and its `.meta`; `Tools/Verification/FullTeamMilestone9Verification.cs` (outside player builds).

Files changed by this final expansion: `Assets/Editor/Milestone9PrototypeSetup.cs`; `Assets/Scenes/PrototypePool.unity`; runtime `SwimmerIdentity.cs`, `PrototypeTeamTactics.cs`, `PrototypeRoster.cs`, `MatchClock.cs`, `PossessionShotClock.cs`, `GoalMouth.cs`, `PrototypeGoalkeeper.cs`, `GameplayCamera.cs`, `PrototypeMatch.cs`, `MatchHUD.cs`; `Tools/Verification/GoalkeeperSaveVerification.cs`; `AGENTS.md`, `README.md`, `plan.md`. The keeper verification now parks all non-shooters outside the shot lane so the new centre back does not legitimately block its isolated keeper fixtures. No save/geometry assertion was removed. Other initial-M9 source/material/meta changes listed in its historical section remain part of the combined uncommitted milestone.

#### Fresh verification for the full-team expansion

Actual Unity Editor compilation passed. All **six executed entry points** below passed with zero script compilation diagnostics. Results are preserved in ignored `Logs/Milestone9Full-*.json`; these are fresh results, separate from initial-M9 historical evidence.

| Executed entry | Actual result and coverage |
| --- | --- |
| `FullTeamMilestone9Verification.Setup` | PASS: 14 distinct actors, 6+1 per team, six roles, separated own-half starts, original identities retained, 4/80/480/6 and independent 28 defaults, repeat setup stays at 243 objects and preserves camera tuning. |
| `FullTeamMilestone9Verification.TimingAndEnds` | PASS: 10/40/80 live-second conversion, 4000 fixed steps reaching the 80-second deadline, stopped clocks, Q1/Q2 sequence, halftime panel and single guarded Continue swap, both goal/keeper mappings, all fourteen physical mirrored poses, unchanged instantaneous camera pose, Q3/Q4/full-time DRAW, new-match original ends restored. |
| `FullTeamMilestone9Verification.GoalsAndKeepers` | PASS: five physical goals including Blue/White goals after halftime and a Blue own goal credited White; exact-once/conceding restarts; keeper-held/charging state cleared at Q2; both keepers catch at their new goals and execute two physical distributions; fourteen-actor resets and valid field selection. |
| `FullTeamMilestone9Verification.RolesAndPossession` | PASS: injected C selects all six fields; real pickup auto-selects each with one input/camera; five spaced support options, one Press presser and six Zone slots, keeper-possession assignments, independent 28-second clock, same-team keeper/C preserving time, opposing reset once and charged-turnover cleanup/awarded restart. |
| `FullTeamMilestone9Verification.Gameplay` | PASS consolidated regression: injected W/Shift movement and stamina drain; physical B pass/reception and preserved possession clock; max-charge/release shot; true-height L lob/high pickup rejection/reception; F protection/cooldown/success; actual White AI physical goal before and after halftime; **20 simulated seconds** of live all-AI-enabled full-team play across the two halves with bounds/waterline/fatigue, exclusive ownership and valid selection/camera assertions. |
| `GoalkeeperSaveVerification.Run` | PASS reused regression: both 80m/s catches/exclusive owner/Blue-keeper control, held-ball scoring rejection, out-of-reach shot scoring, physical deflection/loose-ball continuation, glancing deflection goal, deterministic save skill endpoints/reaction delay and no repeated contact reroll. |

Clock tests use deterministic accelerated simulation instead of waiting through repeated complete real-time matches. The consolidated gameplay test uses actual physics/input updates. Historical fixed-count setup and initial-M9 clock suites were not redundantly rerun; their old eight-athlete/fixed-end defaults are superseded. No assertion was weakened to force success. Actual Game-view captures `Logs/Milestone9Full-Gameplay.png` and `Milestone9Full-Halftime.png` were inspected for state/readability; a separate capture-only check confirmed Q2 HalfTime and the Continue panel. Screenshots do not establish game feel or mobile performance.

Final Unity Console ground truth: **0 errors, 0 warnings, compilationFailed=false**. Pipeline historical journal counters are separate from current Console counts; no Console clearing was used to hide failures. Play Mode is stopped; temporary Input System focus/background routing, run-in-background override and auto-tick are restored/disabled. Generated Pipeline configuration was moved to ignored Logs. No ProjectSettings/package differences remain. SampleScene retains hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`. New Assets have all companion `.meta` files. Source/Markdown whitespace checks pass with Windows CRLF recognized; Unity's serializer emits trailing spaces for empty YAML fields in the scene, which were left as generated.

Final read-only Git state: `main`, HEAD `734d8da`, zero ahead/behind stored `origin/main`, **empty index**, 27 modified tracked files and 27 untracked files. Untracked files comprise 26 milestone implementation/meta/verification files plus the pre-existing untouched `old-plan.md`. This includes the initial M9 work and this expansion. No remote fetch was run, so stored upstream parity is not a fresh remote-network verification. No Git staging, commit, push, checkpoint or other write operation occurred.

#### Remaining limits and manual handoff

Full-team spacing, pass/shot heuristics, keeper balance and camera framing require manual tuning; kinematic athletes can still overlap. This is geometric tactical coordination, not final collision-avoidance or official referee logic. Period starts are neutral restarts, not a complete swim-off. No out-of-play/referee process, official foul/exclusion/rebound shot-clock exceptions, substitutions, final animation/art, touch HUD, device build/performance validation, backend, multiplayer or economy is included. AI still primarily chooses flat passes; the shared actual-height lob API works for human fields/keeper. Stamina is frozen during breaks rather than using an official rest/substitution model. Very long unattended sessions and every screen aspect ratio were not tested.

Manual review: open **Assets > Scenes > PrototypePool**, Play and click the Game view. C cycles six Blue fields; WASD/arrows swim, Shift sprints, E picks up, B passes, Space hold/release charges/shoots, F steals, L lobs, Z toggles Press/Zone. Check Blue keeper catch/control, passing out and returning to field control. Observe Q1 8:00 -> 7:00 after ten *live* seconds (goal/turnover pauses add wall time). Continue after Q1; after Q2 verify halftime, Continue once and check both keepers/teams at opposite ends, reversed attacks and correct scores. Q4 must retain Q3 ends. Full time offers Restart Match (restores original ends) and Return to Prototype. A full default match is 5:20 of live play plus stoppages. For a temporary transition-only review, outside Play select **Hierarchy > Gameplay > PrototypeMatch > Inspector > Prototype Match > Clock > Real Seconds Per Quarter**, set 10, and restore **80** afterward; do not use shortened periods to evaluate a 28-second possession.

Next step: developer manually tests this complete **Milestone 9** build, then personally reviews/stages/commits/pushes. Milestone 10 is not authorized. All changes remain unstaged/uncommitted; `old-plan.md` stays untouched and excluded. No AI Git checkpoint or write operation was performed.

## Optional old-game references

Previous game: Water Polo Manager, Unity `6000.4.7f1`, C#, 2D physics, URP 2D. Reference repository supplied by the developer: https://github.com/Nikoloz-Todua/WaterPoloManager . Remote contents were not verified in this session. A local reference clone has not been confirmed.

The developer is happy to share old files, screenshots, videos, and logic examples at any time. Codex can and should request the specific material useful to the task. Keep the old project and detailed old documentation outside this active project unless deliberately selecting something for reuse. Preserve the original project/repository. Do not upgrade it just to read files.

### Useful card and UI/UX ideas

- Mystery packs and collectible player cards are important references. The old plan describes Common, Rare, Epic, and Legendary PACK tiers shared by shop packs and match rewards.
- Confirm PLAYER rarities separately: an earlier old-plan description lists Common/Rare/Legendary only. Do not assume pack tiers and player rarities are identical; ask about Epic when designing the new system.
- Card information: identity, portrait, overall rating, position, and rarity styling.
- Old positions: GK (goalkeeper), CB (centre-back), LW/RW (wings), CF (centre-forward), LF/RF (flats). Preserve useful role concepts, not old enum ordering or starter-slot indices.
- Roster reference: starters, bench, position filtering, lineup changes, buy/sell/upgrade, team overall, club identity/customization.
- Opening reference: shared reveal screen, staggered reveals, rarity borders, NEW/duplicate feedback, duplicate-to-currency conversion, and a shared odds popup driven by the opening definitions.
- Reward reference: four post-match slots, timed unlock, one active unlock, ready/collect states, persistence across closing the app.
- Hub reference: club/profile, currencies, team/shop/play navigation, reward slots, missions, and season progression.
- Shop reference: scrollable sections and navigation tabs for offers, packs, deals, and reward offers.
- Review screenshots/video before choosing the new presentation. Do not inherit old coordinates, layout constants, scene wiring, asset-loader workarounds, prices, odds, unlock times, or balancing automatically.
- Old local JSON persistence and development purchase stubs are not production billing. Backend integration was incomplete. Plan trusted purchase/entitlement validation before releasing real-money features.

### Files to request when useful

Names below are references from the handoff/old plan, not inspected implementations. Request small dependency groups and relevant examples rather than importing everything.

| Task | Useful old references |
| --- | --- |
| Player cards, stats, positions, collection/save model | `PlayerData.cs`, `PlayerDatabase.cs`, `Roster.cs`, `RosterManager.cs`, sample player assets/save |
| Mystery packs and reveal | `CardPack.cs`, `PackRevealUI.cs`, `PackCardFX.cs`, `PostMatchRewardManager.cs`, screenshots/video |
| Hub/shop/team/club UX | `NavigationManager.cs`, `ShopUI.cs`, `TeamScreenUI.cs`, `ClubCustomizationUI.cs`, navigation examples |
| Competitions/progression | `LeagueSeason.cs`, `WorldCupSeason.cs` if present, `MatchPresentationContext.cs`, `MissionManager.cs`, `SeasonPassManager.cs`, `LeaderboardManager.cs` |
| Tactical logic for a specific milestone | `WaterPoloAI.cs` / `WaterPoloBrain`, `IAgentBody`, `TeamSide.cs`, `MatchContext.cs`, then relevant movement adapters |
| Specific rules | Relevant scoring, timer, shot-clock, exclusion, penalty, timeout, substitution, stamina or replay scripts plus dependencies |

Inspect dependencies and asset licensing before reuse. Adapt independent data/domain logic where helpful; rebuild 2D physics, sprite presentation, and camera assumptions as needed for 3D. Old architecture instructions do not govern this new project.

For new-project decisions, inspect relevant existing scripts/scenes, `Packages/manifest.json`, and `ProjectSettings` files before relying on their contents.

## Working process

- Read `plan.md` and `AGENTS.md` before significant changes.
- Explain substantial architecture changes and implement approved scope in small milestones. Do not automatically migrate the old game.
- Prefer simple, maintainable C# and account for mobile CPU/GPU/memory limits.
- Give clear Unity instructions with exact menu/panel paths. During interactive setup, provide a few steps and wait for requested screenshots/output.
- Do not call file edits tested without evidence. Report compilation, runtime and device checks separately.
- After meaningful work, document behavior/files changed, confirmed decisions, actual tests, incomplete work, bugs, and the next step. The human alone reviews/stages/commits/pushes; AI must not perform Git write operations or automatic checkpoints.
- Documentation instructions do not guarantee updates after a session ends; include updates explicitly in task completion.

## Roadmap

1. Foundation inspection: completed; saved configuration examined, with live/device verification limits stated above.
2. Milestone 1 movement sandbox: implemented, exercised in automated Editor Play Mode checks, and manually verified by the developer; camera needs closer framing.
3. Milestone 2: following gameplay camera, possession, carrying and shooting one ball. Implemented, automatically verified and manually confirmed by the developer.
4. Milestone 3 goals/scoring/restart: implemented, automatically verified and manually confirmed by the developer.
5. Milestone 4 two swimmers/selection/passing/wider framing: implemented, automatically verified and manually confirmed by the developer.
6. Milestone 5 possession switching, basic friendly/opponent AI and wider camera: implemented and automatically verified at its historical checkpoint; later core gameplay confirmation belongs to the combined Milestone 6 build.
7. Milestone 6 first playable 2v2: shared tactical offense/defense, stealing, four-player scene and scoring/restarts; 12 recorded automated suites passed, and the developer confirmed the specific core gameplay listed above.
8. The human completed the Milestone 6 backup as `48ab2f6` before Milestone 7. AI performs no Git writes; new milestone changes remain uncommitted until the developer tests and backs them up.
9. Milestone 7 adds two AI goalkeepers, physical saves and distribution. The developer manually tested and backed it up as `a1374a8` before Milestone 8; this does not imply exhaustive manual edge-case/device coverage.
10. Milestone 8 adds temporary manual Blue keeper possession, charged shots/power bar and presentation adjustments; developer manual testing and backup completed as `734d8da`.
11. Milestone 9 — complete full-team prototype expansion: six fields plus one keeper per team, six-role tactics, four 80-live-second/8:00 periods, halftime physical ends swap, independent 28-second possession clock, sprint/stamina and aerial passes. Developer reports functional manual gameplay; current human-created HEAD is `07f46d5`.
12. Milestone 10 is implemented in place with contextual selection, shared tactical improvements, prototype referee/free-throw/exclusion/penalty handling and event presentation/audio. See the current handoff at the top for fresh evidence and limits. Developer manual review is next; later milestones require separate authorization.
13. Improve animation, water, lighting, feedback and performance through measured iterations.
14. Request old card/pack/roster examples when useful and choose the new data/UI architecture.
15. Expand competitions, progression, backend and monetization after the playable foundation is validated.

Local checkpoint history: Milestone 1 `a8312f6`, Milestone 2 `2ca3909`, Milestone 3 `0e43094`, Milestone 4 `4babe88`, Milestone 5 `d647882` (its original label records automated testing with manual verification pending at that time). These predate the absolute human-only Git rule and grant no AI Git-write permission. The developer's backups are Milestone 6 `48ab2f6`, Milestone 7 `a1374a8` and Milestone 8 `734d8da`. Milestone 9 awaits developer manual testing/commit/push; no AI checkpoint was created. `old-plan.md` remains untracked, untouched and excluded.

## Documentation and manual backup review — Milestone 6 history

Documentation updated: AGENTS.md, README.md and this canonical plan only. Current source, saved Unity configuration and all 12 stored Milestone 6 result files were inspected; no gameplay code, assets, scenes or settings were changed, and no new Unity tests were run for this documentation-only task. There is no docs/ directory to synchronize; do not create duplicate architecture/roadmap sources unnecessarily.

Read-only Git inspection found branch `main`, local HEAD `d647882`, an empty staging index and five commits ahead / zero behind the stored `origin/main`. A read-only remote query confirmed GitHub `refs/heads/main` at `0eab526e611ef15ac811f0f9d1830651f9780d3b` at review time. The configured fetch/push remote is `https://github.com/Nikoloz-Todua/WaterPoloSuperstars.git`. These are pre-backup observations; recheck before and after the human's operations. A normal push will also publish the existing five local milestone commits; no history rewrite or force push is needed.

Proposed backup comprises the Milestone 6 implementation/scene/material, associated `.meta` files, verification scripts and the three updated root Markdown files. Stage only the explicit reviewed paths supplied to the developer, then inspect staged names/diffs before committing. Preserve `old-plan.md` locally and leave it untracked: it is not covered by the current .gitignore. Existing ignore rules cover Library, Temp, Logs and UserSettings. No .gitignore change is necessary for this explicit-path backup.

The repository-content review found no credential-signature matches or suspicious credential/archive paths in the proposed files, no missing asset/meta pairs and no tracked generated cache folders. A read-only scan of each of the five unpushed commit snapshots also found no credential-signature matches or generated/legacy path matches. The largest proposed file is the existing TMP LiberationSans SDF asset (2,256,862 bytes); no oversized generated content was found. The manual staging list contains 33 reviewed paths: 19 modified files (including these three Markdown files) and 14 new implementation/meta/verification files. old-plan.md is the only additional untracked file and stays excluded. The existing Git name/email are configured; use them without replacing the identity. This is a targeted review, not a guarantee that arbitrary future changes contain no secrets. Canonical handoff details such as the requested local project path and hardware are intentionally retained in plan.md; README omits personal setup details. Do not publish local tokens, private keys, service-account credentials or raw legacy archives. No staging, commit, push, fetch or other Git write operation was performed during this review.
