# Milestone 18 — sprint effort and championship lighting

> M19 adds the live arena board and two gameplay views: [MATCH_BROADCAST_AND_CAMERAS.md](MATCH_BROADCAST_AND_CAMERAS.md). Sprint, selection and ball-release authorities below remain unchanged. The latest plan.md governs current work.

2026-11-16, Unity 6000.6.7f1 / URP 17.6, Windows Editor. Implemented in the existing OlympicMatchArena; M19 was subsequently authorized during handoff. No Git writes. Original M17 scene recovery was followed by exact native **Assets/Scenes/Recovery/OlympicMatchArena_PreSprintPolish.unity** before M18 authoring. Recovery shares scripts, not a source rollback.

## Gameplay and selection

PrototypeMatch retains phase/time/transition authority. PeriodSprintOff remains its staging/input/AI adapter; no new sprint state machine or possession authority.

- Preferred sprinters use existing scene IDs **prototype-blue-1 / prototype-white-1**, configurable on PeriodSprintOff. Unavailable IDs use eligible lowest cap then stable ID. This avoids selecting from a stamina-dependent live speed value.
- Designated field swimmer starts **1.05 m to the team's left of the central goalkeeper**, exactly on its current defended goal line; remaining fields span ±8.1 m with legal spacing. The keeper remains central. Each side has one designated field and one goalkeeper between posts. No illegal advance or new athlete.
- PlayerSelection.SwitchingLocked is separate from movement suspension: preparation and active race reject C, gamepad right shoulder, mobile SWITCH and direct selection of another athlete. Proximity alone never changes control. On real pickup the match transitions first, then explicitly restores possession selection, independent of event-listener ordering. Friendly pickup transfers control; opponent pickup unlocks normal Blue selection. Pause keeps designation and cancels effort.
- Unavailable preparation sprinters restage deterministically. An invalid active sprinter or **18 s unresolved deadline** ends only the special contest. The actual loose ball is retained, switching/normal pursuit resume, and opening clocks wait for genuine possession. No awarded winner, teleport or fake first touch. The HUD reports LOOSE BALL. A new match resets this opening flag/counters. A fully depleted roster still has no invented substitute/forfeit implementation.

## Effort, meter and release

Existing press-edge input is preserved: Left Shift and actual mobile pointer-down call the same Stroke API. At most one accepted stroke per **0.16 s**, +0.28 effort capped at 1, decay 0.85/s; actual acceleration scales **1–1.35×**, maximum speed unchanged. Existing sprint stamina applies. Holding and manual steering remain accessible; with no directional input a sprint request heads toward center. AI uses the same motor/stamina/cap and existing deterministic 0.16–0.30 s reaction. No pre-whistle banking; pause/resume requires a fresh Shift edge for another stroke.

**SprintEffortHUD** is one compact, non-raycasting navy panel in the existing safe-area Canvas, bottom center (350×88 reference pixels). Cyan fill reads actual HumanStrokePower, smoothly follows it, adds a brief accepted-stroke highlight and restrained gold near maximum. It fades in during the race and out after resolution, hides during menus/pause, and avoids idle RectTransform writes when unchanged. No new input system, menu screen or fullscreen instruction.

**SprintBallReleaseVisual** now opens two small ring jaws over 0.10 s at the whistle; the real ball is released immediately by BallController, independently of visual completion. A thin 12 mm cable, small joints and compact base replace the old thick stem renderer (retained disabled). Retraction is configurable, default **1.05 s**, with 0.08 s opening lead-in and smooth 1.5 m lowering. Tiny damped cable movement; no collider or duplicate ball. Existing impact splash/ripple is reused. Preparation pose resets synchronously through match events; paused LateUpdate returns immediately, fixing a detected one-frame visual catch-up. Device remains hidden after ordinary goals.

## Walkways and arena lighting

The apparent dark covering was the M17 **navy deck beside a 0.25 m inset**, not a separate decal or overlapping carpet above the markers. Existing strip renderers now use **0.82 m colored top sections** on the 1 m platform, leaving navy borders; goal/2/5/6/half cross-marks remain. New local materials give a lighter navy, restrained red/yellow/green/white, low emission and moderate roughness. Original rope materials, platform dimensions, referee feet/path, rails and benches are unchanged. No gameplay collider added.

New **Championship Lighting Refinement** adds two suspended truss galleries and fourteen combined luminaire housings/lenses, cobalt structural reveals and open jumbotron corner spines/edge traces. Display faces stay clear. Four shadowless accent spots are budgeted by quality; the original competition spots/technical lights remain. Existing ChampionshipPresentation applies the budgets rather than a second quality authority. The main light is cool white, 0.95 intensity, 58° elevation, soft 0.60-strength shadows; cooler gradient ambient preserves bright water against the navy bowl. Local copied Volume/URP assets isolate the pass from original presentation settings. A refreshed **128 px baked reflection** and existing High planar reflection preserve the original transparent water shader/materials.

Emission flags are explicitly configured and verified after asset reload so URP validation retains _EMISSION. [Unity's emission flags](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/materialglobalilluminationflags) and [Bloom reference](https://docs.unity.com/en-us/engine/6000.6/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom) informed the native implementation. No GI bake, volumetrics, fake beams, extra particles, realtime reflection camera or package install was added by M18.

| Preset | Active lights | Additional shadowed lights | Main shadows | Bloom |
| --- | ---: | ---: | --- | ---: |
| Low | 3 | 0 | Off | 0 |
| Medium | 9 | 0 | 1024, 1 cascade, 52 m | .19 |
| High | 11 | 0 | 2048, 2 cascades, 65 m | .30 |

HDR/Neutral grading remain; bloom threshold 1.2, quarter-resolution filtering, 3/4 iterations Medium/High. Four additional lights per object (two Low). Original crowd/water/effects budgets remain; no gameplay differs by preset. Added lighting: **11 renderers / 5,696 referenced vertices**, four real lights, no added shadowed lights/colliders/particle systems. New asset folder: **22 meshes, 10 materials, six settings assets, one visual prefab, one reflection EXR**, ~0.75 MiB including metas. Counts are not mobile performance measurements.

## Camera and visual evidence

M18 did not modify GameplayCamera source or tuning. Matching opening captures have exactly **position (0,28.926,-21.6945), rotation (53,0,0), FOV 52°**. F8 inspection views remain. All seven before/after Game views are under ignored Logs/SprintPolish:

| View | Before | After |
| --- | --- | --- |
| Opening | [Opening](../Logs/SprintPolish/Before-Opening.png) | [Opening](../Logs/SprintPolish/After-Opening.png) |
| Preparation | [Preparation](../Logs/SprintPolish/Before-Preparation.png) | [Preparation](../Logs/SprintPolish/After-Preparation.png) |
| Effort | [Effort](../Logs/SprintPolish/Before-Effort.png) | [Effort](../Logs/SprintPolish/After-Effort.png) |
| Apparatus | [Apparatus](../Logs/SprintPolish/Before-Apparatus.png) | [Apparatus](../Logs/SprintPolish/After-Apparatus.png) |
| Walkway | [Walkway](../Logs/SprintPolish/Before-Walkway.png) | [Walkway](../Logs/SprintPolish/After-Walkway.png) |
| Lighting | [Lighting](../Logs/SprintPolish/Before-Lighting.png) | [Lighting](../Logs/SprintPolish/After-Lighting.png) |
| Post-sprint | [Gameplay](../Logs/SprintPolish/Before-Gameplay.png) | [Gameplay](../Logs/SprintPolish/After-Gameplay.png) |

Apparatus/walkway/lighting use paused F8 inspection with Canvas temporarily hidden; post-sprint is the existing F8 overview. These are actual Editor Game views, not device captures or mockups. Visual comparison guided fixture angle, emission persistence, marker width and restrained architectural accents. Existing F8 oblique views can project the new hanging galleries over the pool; M19 camera/occlusion work must inspect this explicitly. Normal M18 lengthwise opening remains clear.

Craft scope: existing navy/cobalt art direction, equipment materials, game-feel, scene-lighting and visual-scorecard. Final scoped board: palette 4/5 (coherent eight original plus ten local materials), style 3/5 (original mannequin/procedural venue retained), lighting depth 4/5 (cool ambient, competition highlights, controlled shadows), silhouette 3/5 (unchanged readable athletes), UI intentionality 4/5 (compact real-effort panel), feedback visibility 4/5 (accepted strokes, real drop and readable retraction). Fix passes addressed lighting visibility/emission, overly detached accent treatment and pause fidelity. No art/model dependency imported.

## Fresh checks and limits

Runner **Tools/Verification/SprintPolishVerification.cs**; focused reused M17 routines in **FieldSprintVerification.cs**. Reports/captures under Logs/SprintPolish; M17 runner also writes current copies under Logs/FieldSprint.

- Structure: exact recovery/same GUID, **847 protected records** (original camera/athletes/tuning/M16 UI) unchanged; Boot/MainLobby/catalog/build/swimmer/water hashes retained, all14 rigs, one Canvas/EventSystem, no missing scripts, repeat-safe native setup/save/reload.
- Selection-Effort-PASS: deterministic legal lane; C/gamepad/mobile/direct selection lock during preparation/race; real keyboard/motor acceleration and capped speed; five actual touchscreen presses = five strokes; proximity does not switch; friendly/opponent pickup, contention, normal switching restoration, pause and no leak.
- Recovery-Edges-PASS: unavailable staged sprinter replacement and new-match restoration; aged-deadline fixture verifies loose-ball fallback, stopped opening clocks and one real pickup. This models expiry without waiting a full 18 seconds.
- Release-PASS: real held ball, one whistle/drop/contact, opening jaws, negligible lateral drift, collection only after water, visible recovery after impact, exact pause freeze, no duplicate ball or collider.
- Rules-PASS: actual ordinary foul, stopped clocks, physical free-throw release, off-ball exclusion/personal foul, live re-entry, no extra sprint. Receiver placement follows the referee's actual restart facing in the fixture.
- Quality-Layout-PASS: actual light/shadow/bloom budgets, water supported; actual 1920×1080, 2340×1080, 2400×1080 meter bounds, text and touch-target separation. Final materials checked across presets.
- Fresh M17 Competition/TouchAndGameplay/PeriodsAndRestarts: human and AI physical wins, immediate losing-team activity, actual touch/keyboard, passes/reception, charged shot, steal/lob, controlled-chance keeper catch; all four starts, one halftime swap, Blue/White physical goals and ordinary center passes, draw/fulltime and clean restart.
- Fresh LoggedHubLoop: actual Boot/lobby pointer Division1 launch, restart/abandon, four physical period acquisitions, one victory receipt, retry/replay deduplication, JSON reload/results/lobby, **zero new runtime errors**, real profile hash unchanged. Isolated memory repository.
- Final-Audit-PASS: exact matching opening camera, single match/sprint/ball authorities, saved Edit Mode, Boot entry, emission retained after reload. Temporary Game-view/input-focus and background/auto-tick overrides restored/disabled. Current Console **0 errors / 0 warnings**, compilationFailed false. Historical buffer retains earlier warnings and resolved development/probe errors; it was not manually cleared.

No unresolved focused acceptance failure. One real pause-animation defect was found/fixed; meter/foul fixture sampling/facing assumptions were corrected before reruns. Full-time tests accelerated period ends through the existing API, not match settings; no full-duration match claimed. AI temporarily disabled/restored for isolated contact/action fixtures, keeper probability controlled/restored only for the catch. No physical phone, build/profiling/thermal, real-thumb, exhaustive penalty/roster-depletion or OS interruption test. Old broad historical suites were not repeatedly run. UI no-notch-inset sweep in M18; bounds cover the three simulated landscape resolutions.

## Files and review

Exact inventory: **docs/MILESTONE_18_FILES.txt**. Core new files: SprintPolishSetup, SprintEffortHUD, native recovery and Assets/Arena/SprintPolish assets/metas. Existing PeriodSprintOff, PlayerSelection, PrototypeMatch, SwimmerInput, MobileMatchControls, MatchHUD, MatchNotificationBanner, SprintBallReleaseVisual, ChampionshipPresentation and AuthenticFieldSetup mesh helper are extended. No old-plan.md, save/economy/catalog, character/clip, water shader or original material was edited. Prior local M16/M17/unrelated changes remain.

Already applied. **Tools > Water Polo > Arena > Refine Sprint and Championship Lighting** on the saved arena outside Play Mode preserves repeat tuning. Normal review: **Play Hub Entry → PLAY → Division1 → Launch**. Isolate: **Tools > Water Polo > Play Olympic Match Arena**. Restore Boot: **Recovery > Use Boot as Editor Play Entry**.

Manual checks: compare tap/hold/steer; C/right-shoulder/SWITCH blocked until real pickup; pause in setup/drop/race, resume without stored input; all four quarters and halftime; goals remain normal center passes; meter fades; inspect platform colors/feet and quality tiers; restart/exit confirmations, results/lobby/one reward. Next recommended work was phone profiling/comfort and camera sightlines; M19 was explicitly authorized during handoff.
