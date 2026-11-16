# Championship poolside arena redevelopment

Implemented in the existing **OlympicMatchArena**, Unity **6000.6.7f1 / URP 17.6**, Windows Editor, 2026-11-17. This is the environment pass explicitly authorized after M19, using the developer's full supplied *Water Polo Arena & Mobile UI — Build Spec* and overriding execution instructions. No local `waterpolo-arena-spec.md` was present. No obsolete setup was run, package installed, player model replaced, menu rebuilt or Git state changed.

The saved scene keeps GUID **5c4d3741df6b7f54e8cfd34d391d78d1**. Native Unity recovery **Assets/Scenes/Recovery/OlympicMatchArena_PreArenaRedevelopment.unity** was copied before geometry changes and verified byte-identical to the original scene. It is outside builds and shares current scripts; it recovers layout, not older source code. Existing local M19 and unrelated changes remain untouched.

## Open, play and inspect

- **Tools > Water Polo > Play Hub Entry**: Boot → MainLobby → PLAY → Division 1 → Start Championship; Training avoids progression rewards.
- **Tools > Water Polo > Play Olympic Match Arena**: current arena directly, without an official hub reward session.
- **Tools > Water Polo > Arena > Open Olympic Match Arena**: open the saved production scene.
- **Tools > Water Polo > Arena > Apply Championship Poolside Redevelopment**: already applied; saved arena outside Play Mode only. A repeat preserves existing authored tuning.
- **Tools > Water Polo > Arena > Refresh Redeveloped Arena Reflection**: rebake the existing 128 px probe after deliberate environment edits. Temporarily hides the water, uses the existing High pipeline, restores the original pipeline/visibility/AA, and saves the scene. Do not use this as a gameplay reset.
- **F6** or **Pause → Settings → Camera View**: Lengthwise / Sideline Broadcast. **F8** cycles the existing inspection views and returns to the selected gameplay view. Normal pause/controls/settings are unchanged.
- **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** restores normal startup after direct inspection.

The delivery state is saved OlympicMatchArena in Edit Mode, Play stopped, Boot entry, original Game-view size/input-focus/preference bytes restored. New assets are under **Assets/Arena/PoolsideRedevelopment/**. Exact paths, including Unity metas: [ARENA_REDEVELOPMENT_FILES.txt](ARENA_REDEVELOPMENT_FILES.txt).

## Dimensions and corrected assumptions

The actual pool already had the intended competitive proportions. **No resize was necessary.** `Assets/Arena/Settings/CompetitionDimensions.asset` remains the authoritative configuration, matched by the existing PoolBounds and goal transforms.

| Quantity | Actual value / world coordinate |
| --- | --- |
| Between goal lines | 25.00 m; Z = −12.50 and +12.50 |
| Sidelines / playable width | 20.00 m; X = −10.00 and +10.00 |
| End ropes | 0.30 m behind goal lines; Z = ±12.80 |
| Rope envelope | 25.60 × 20.00 m; ratio 1.28 |
| Water basin | 28.60 × 22.50 m, including buffers outside the competition ropes |
| Basin depth | 2.05 m |
| Waterline / centre release | Y = 0; release centred at existing PeriodCenter |
| Referee platforms | 1.00 m wide, walking top 0.70 m above water |
| Referee path | X = ±12.65; root Y = 0.62, foot offset 0.08; existing Z travel limit ±12.25 |

The basin is intentionally larger than the playable field; shrinking it to 25.6 × 20 would consume existing outside-rope water. Projection changes apparent proportions. A measured centre-field capture gave projected long/short axis ratios about **1.26 in Lengthwise and 1.92 in Sideline**, while the physical rope envelope stays 1.28. These are perspective measurements for those poses, not replacement dimensions. Camera profiles, FOV, damping, bounds, player/AI limits, formations, goals, detectors, sprint starts, release coordinates and referee paths were preserved. Camera presentation improves through clearer edge structure and removing the old end-hung display from normal sightlines.

Official reference checked: [World Aquatics competition regulations](https://www.worldaquatics.com/rules/competition-regulations), [18 February 2026 PDF](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf), Part Six articles **21.6–21.12**, plus the field definition. The same downloaded regulation/text is retained in ignored `Logs/FieldSprint/`. Rules take precedence over conflicting statements in the supplied design reference:

| Supplied assumption | Applied interpretation |
| --- | --- |
| The squarish screenshot proves a dimension bug | Actual coordinates already measure correctly; no blanket scale/rebuild. |
| Full-width floating 2 m, 5 m and halfway ropes | These would obstruct play and contradict the overriding instructions; retained measured sideline markers/ropes only. |
| Yellow 5 m, no physical 6 m marker; side yellow ends at 5 m | 21.7 specifies **red 5 m, yellow 6 m**; side rope red 0–2, yellow 2–6, green between the two 6 m lines. |
| Remove every red section around the goal ends | End ropes are white **with the official red goal-area/re-entry exceptions**; these existing sections remain. |
| Round tubular posts, goals flush against basin wall | Retained rectangular 80 mm rigid white frames, 3 m clear width / 0.9 m height, front at the goal line, net behind. Basin wall and goal line differ. |
| Qualitative narrow referee strip | Existing 1 m-wide / 0.70 m-high platforms satisfy 21.9 and remain. |
| Benches anywhere behind the goal | 21.11 places them behind the goal lines at the two corners opposite the officials table; new benches use the east end corners. |
| 2 m is an exclusion/re-entry boundary; halfway is offside | These descriptions are inaccurate. This environment pass does not change foul, exclusion, eligibility or restart logic. |
| Change possession reset timings from the visual reference | Existing validated 28-second possession and actual referee-driven resets are preserved; signage only observes them. |

## Implemented environment

`ArenaRedevelopmentSetup` authors native assets and scene objects in Edit Mode. It adds **Championship Poolside Redevelopment** beneath the existing arena root and **Redevelopment Fine Equipment** under the existing quality-controlled fine-prop root. No new runtime environment controller or state machine is required. Geometry is grouped by material/zone into shared meshes, not individual chair slats/buoys as thousands of GameObjects.

**Pool edge and referee lanes.** Sloped navy event-rubber surfaces connect the basin lip to the raised official lanes. White coping, marine-alloy overflow trim, repeated drainage slots, corner cleat/spring details, support panels, access trim and fine grip strips give the edge physical depth. Six local cloned marker meshes widen their colored coverage from 0.82 to **0.96 m** inside the existing one-metre platforms. Their red/yellow/green materials, longitudinal distances, white halfway and red 5 m strips stay intact. Original field marker assets are not overwritten. There is no covering black carpet added. Existing platform geometry and movement scripts remain, and the entire referee lane stays clear.

**Team corners and equipment.** Old padded sideline technical-area renderers are retired, with original objects/assets retained. Two risers centred at **(8,0.72,−18.05)** and **(8,0.72,+18.05)** hold **seven white armless stacking-style chairs each**. Slotted backs, moulded seats, tubular legs, foot braces, riser trim and steps replace the flat/block-like bench reading. Coolers, towels, bottle groups and flight cases occupy equipment space. Separate lower camera/equipment decks are centred near **(8,0.39,±16.32)**. These empty chairs do not introduce substitute athletes or new roster logic.

**Advertising and architectural tiers.** Continuous low navy boards run outside the referee lanes, with low end backdrops outside the goals. They use original *Water Polo Superstars / Championship Series / ToduStudios* text. Higher billboard/LED-style ribbon bands are segmented around a central officials-table sight gap. Existing hall ribbons, section plaques and crowd remain. No real sponsors, country flags or tournament logos are copied; the ribbon graphics are static original event artwork, not a claimed video feed.

**Broadcast platforms and seating.** Two passive equipment platforms sit at **X = ±16.35, Z = ∓9.30, floor Y = 1.45**, behind the ribbon tier. Access stairs, rails, a tripod/camera prop and an empty chair distinguish them from the lower bench-end equipment tier. The camera props contain **zero Camera components**. End-of-grandstand access stairs, metallic nosings, handrails and restrained cobalt soffit strips extend the existing ten six-tier seating blocks. Current crowd models, materials, positions and density presets are preserved.

**Displays.** The existing four-face jumbotron becomes **Four Face Ceiling Championship Display**, centred at **(0,25,0)** below the existing roof. A chamfered 8.8 m-wide housing, thin cyan edges, four suspension cables and a roof spreader truss replace the end-hung structure. Four existing TMP score/clock faces retain their live observer references and correct front-facing orientation. The high placement avoids blocking the unchanged opening and moving gameplay cameras; it is best inspected from the ceiling view and is not forced into every gameplay frame. The two existing low timing boards move to **(−5.2,1.35,±18.0)** beside the end areas. The west-wall primary live board remains at its M19 position. All seven display areas still read **one ArenaEventDisplay / PrototypeMatch**, with no extra scoring authority or RenderTexture.

**Materials and lighting.** Eight local materials use marine enamel, brushed alloy, moulded competition-white polymer, textured non-slip navy rubber, cobalt trim, restrained ice LED, dark LED panel and graphite goal cord. The existing fine-rubber texture and front-face TMP material are reused. Goal net color improves depth without changing its mesh, deformation observer, opening or collider. Existing bench lights are aimed at the new end-corner chairs. The two existing High display washes are repositioned to the ceiling assembly and reduced to intensity 5 / range 6 after an actual render showed a blue hotspot. No light is added. A new **128 px HDR probe bake** incorporates the changed venue; existing water shader/material/profile assets remain byte-identical.

## Preserved gameplay and UI

PrototypeMatch remains the sole match phase/clock/completion authority. BallController remains the sole possession/flight authority. PeriodSprintOff, PlayerSelection, SwimmerMotor, AI, keepers, MatchRules, economy and hub transaction code are not rewritten. Four 80-live-second quarters still display 480 seconds; the shot clock remains independent at its configured duration. One output gameplay Camera retains Lengthwise, Sideline, original wide opening and F8 inspection behavior.

One narrow lifetime fix was necessary: stopping Play with a held ball exposed `SwimmerBallInteraction.OnDisable → BallController.Drop → Release` after PoolBounds had already been destroyed. `Drop` now returns when that field object is gone. It does not change normal possession or ball simulation. A fresh held-ball Stop Play probe and the fresh hub scene-teardown loop both completed without runtime errors.

The current HUD, shot clock, sprint-effort meter, joystick and every action remain. **The proposed four-button interface is deferred.** A later proposal must retain: hold/release shooting plus high/skip modes; flat pass and lob; normal hold sprint and opening tap sprint; context-safe switching; steal; collect; defensive tactic selection; keeper possession behavior; pause/settings and accessibility. Mapping sprint to full joystick deflection or combining switch/steal could change established meanings and is not implemented here. No claim that the existing developer/personal-foul HUD is Editor chrome; it is left intact under the preservation instruction. No country codes or invented team identities replace current data.

## Quality and performance

| Preset | Existing active realtime Lights | New essential geometry | Fine equipment |
| --- | ---: | --- | --- |
| Low | 3 | Chairs, risers, pool edge, referee detail, boards, platform structure, jumbotron | Hidden with existing fine-prop group |
| Medium | 9 | Same | Visible |
| High | 11 | Same | Visible; existing best water/reflection/shadow behavior |

No new realtime light, rendering camera, gameplay collider, package or per-frame environment search was introduced. The established High water reflection path still exists; the passive camera props do not render anything. The new content uses **69 combined geometry meshes, 56,992 referenced geometry vertices / 28,496 triangles and seven geometry materials**, plus **38 separate TMP labels**. Six widened platform meshes and the graphite net material are additional local replacements. There are **75 native mesh assets, eight materials, three visual prefabs and one reflection cubemap**. These are content counts, **not measured draw calls, frame times, GPU memory or device performance**. `LayoutQuality-PASS.json` counts 107 MeshFilters including labels; `ContentMetrics.json` separates actual geometry from TMP.

Old replaced renderers are disabled; their source objects/assets remain for recovery. New static meshes are reflection-probe-static, with selected structural shadows only. Original Low/Medium/High pipelines and water assets are protected. Dynamic audience/athlete/goal rendering costs remain existing project costs. Empty staff positions, static LED bands and simple procedural equipment are deliberate mobile-conscious placeholders. Physical-device profiling remains required before claiming a performance target.

## Fresh verification and evidence

All evidence below was produced in this task in the actual running Editor. Tests live outside Assets to avoid shipping test runners. Scoped copies of the existing focused M17/M19 suites write to **Logs/ArenaRedevelopment/Sprint/** and **Broadcast/**; historical evidence is not overwritten. Period endings use authoritative accelerated-time test calls, not a full-duration playthrough.

| Check | Actual result / report under Logs/ArenaRedevelopment |
| --- | --- |
| Native saved structure / recovery / reload | **PASS**: exact recovery, original scene GUID, 2,858 protected component JSON records and 23,646 serialized object references unchanged; 14 rigs, one HUD/EventSystem/display authority, no missing scripts, repeat setup preserves tuning. `Structure-PASS.json` |
| Dimensions / markings / referee travel | **PASS**: 25 × 20; mirrored goal/2/5/6 m markers, waterline rope bounds, retired football floor graphics, six widened sections; actual referees travel north/south at unchanged X/Y, full lane clear. `Environment-PASS.json` |
| Camera obstruction / playable frame | **PASS**: actor/ball projections and **20 triangle sightline rays** at both goals, centre and near/far sidelines in both views, against **357 venue MeshFilters** (zero unreadable meshes). Existing renderer cutaways respected. This is sampled coverage, not every arbitrary camera pose. `Environment-PASS.json` |
| Human controls / cameras / pause settings | **PASS**: queued keyboard, gamepad, actual simulated touch joystick; screen-relative movement both modes, sprint/release, C/F6, pointer settings, paused blend with unchanged score/clocks/ball, persistence/invalid fallback and F8 return. `Broadcast/controls-FreshConsole-PASS.json` — 0 fresh errors/warnings |
| Passing / shooting / defense / saves | **PASS**: actual B pass/reception/control transfer, screen-relative aim, Space charge/release and physical goal during blend, F steal, L lob, swept keeper catch, shot-clock violation. AI/keeper probabilities temporarily controlled/restored. `Broadcast/Actions-PASS.json` |
| Live displays / rules | **PASS**: primary/HUD/six faces agree; Blue and White physical goals; actual ordinary foul gives no false exclusion, two real exclusions show correct caps/timers, pause stops countdown, legal re-entry clears markers, restart clears state. `Broadcast/display-FreshConsole-PASS.json` — 0 fresh errors/warnings |
| All four quarters / halftime | **PASS**: real physical acquisition in each period, designated-player switching lock/restoration, exact old opening pose, one Q3 end switch, FINAL and restart camera preference. `Broadcast/periods-FreshConsole-PASS.json` — 0 fresh errors/warnings |
| Sprint contest | **PASS**: 14 legal goal-line starters, pre-whistle gates, one whistle/drop/water contact/pickup, capped Shift strokes, human and AI wins, immediate losing-side play, real pointer Sprint hold/release, stamina, pause/resume. `Sprint/Sprint-PASS.json` |
| Goal geometry / ball contacts | **PASS**: retained official opening; visibly outside-post/above-crossbar scoring rejected; actual physical post and crossbar contacts at both ends. `Sprint/GoalContacts-PASS.json` |
| Landscape / quality | **PASS**: actual 1920 / 2340 / 2400 × 1080 Game views in both modes, safe-area bounds, supported original water shader, essential geometry on Low, fine props at Medium/High, 3/9/11 light counts. `LayoutQuality-PASS.json` |
| Advancing live AI views / passive equipment | **PASS**: actual Playing/FreeThrowSetup views, 14 athletes remain in bounds; camera-platform detail capture. `LiveArena-PASS.json` |
| Held-ball teardown | **PASS**: held owner immediately before Stop Play, zero fresh errors/warnings through EnteredEditMode. `HeldBallTeardown.json` |
| Boot → hub → match → result → hub | **PASS**: real production menu commands/pointer Division 1 launch; restart cancel/confirm; abandoned sprint grants no reward/stray holder; four real acquisitions and accelerated quarter ends; one official victory receipt, duplicate rejection, serialized isolated-profile reload, result/return. **Real disk profile SHA-256 unchanged**. `Sprint/HubLoop-PASS.json`, `Sprint/Fresh-Hub-Console-PASS.json` |
| Delivery restoration | **PASS**: Play stopped, Boot entry, original Game-view/input focus/preferences/profile bytes, saved arena. `Restored.json` |

Final current Editor Console: **0 errors / 0 warnings**, compilationFailed false. No manual Console clear was used. The retained Pipeline history includes earlier project warnings and resolved authoring/test probes. The pre-existing `PremiumArenaSetup.cs:123` CS0184 warning may recur on a full rebuild; it is outside this pass. An initial scoped-control capture ran before its output subdirectory existed; the capture-path error was fixed and the full named control run passed fresh. A first sightline probe accidentally filtered the whole arena because PoolWaterVisuals is on the root; it was corrected, guarded for >100 meshes, and rerun against the 357 meshes above. Neither failed probe is counted as acceptance.

No Android/iOS hardware, mobile build, GPU/thermal profiling, full-duration match, arbitrary camera tuning, every penalty-flight/exhausted-roster combination or physical notch/rotation test is claimed. Synthetic touch is Editor testing. Existing simple referee/crowd models remain. New tiers are visual equipment, not navigable spectator areas. A replay system is still not implemented.

## Actual Game-view comparisons

All PNGs are in **Logs/ArenaRedevelopment/**. `Before-CapturePoses.json` / `After-CapturePoses.json` record the matching camera fixtures. Sideline and five inspection transforms/FOVs match exactly; the lengthwise fixture differs by about a centimetre because the real held-ball release pose interpolates. Original opening pose is independently checked exactly. Animation/water states can differ. Close inspections temporarily hide and restore the Canvas; standard gameplay captures retain it. No image generation, post-edited render or real-device screenshot is presented as evidence.

| View | Before | After |
| --- | --- | --- |
| Opening / sprint preparation | [Before](../Logs/ArenaRedevelopment/Before-Opening.png) | [After](../Logs/ArenaRedevelopment/After-Opening.png) |
| Lengthwise gameplay fixture | [Before](../Logs/ArenaRedevelopment/Before-Lengthwise.png) | [After](../Logs/ArenaRedevelopment/After-Lengthwise.png) |
| Sideline gameplay fixture | [Before](../Logs/ArenaRedevelopment/Before-Sideline.png) | [After](../Logs/ArenaRedevelopment/After-Sideline.png) |
| Arena inspection | [Before](../Logs/ArenaRedevelopment/Before-Arena.png) | [After](../Logs/ArenaRedevelopment/After-Arena.png) |
| Bench end | [Before](../Logs/ArenaRedevelopment/Before-Bench.png) | [After](../Logs/ArenaRedevelopment/After-Bench.png) |
| Referee / pool edge | [Before](../Logs/ArenaRedevelopment/Before-PoolEdge.png) | [After](../Logs/ArenaRedevelopment/After-PoolEdge.png) |
| Billboard / stand tier | [Before](../Logs/ArenaRedevelopment/Before-BroadcastTier.png) | [After](../Logs/ArenaRedevelopment/After-BroadcastTier.png) |
| Ceiling installation | [Before](../Logs/ArenaRedevelopment/Before-Ceiling.png) | [After](../Logs/ArenaRedevelopment/After-Ceiling.png) |

Additional captures: **After-CameraPlatform.png**, **Lengthwise/Sideline-SouthGoal.png / -NorthGoal.png**, **Live-AI-0/1.png**, **Landscape-1920/2340/2400-0/1.png**, **Quality-0/1/2.png**, plus fresh exclusion/goal/final and official result/lobby views in the scoped suite subfolders. Logs is ignored and local; retain selected evidence manually if it needs to travel with the repository.

Visual review, scoped to this environment pass (subjective /10, not a device metric):

| Axis | Before | After | Evidence / remaining gap |
| --- | ---: | ---: | --- |
| Sport/venue identity | 7 | 8 | Correct field retained; benches and layered equipment distinguish competition roles. |
| Material/shape quality | 6 | 8 | Slotted moulded chairs, coping, skirt, metal details; procedural props remain simple. |
| Lighting/depth | 8 | 8 | Existing championship rig retained; new probe/retargeted washes integrate equipment. Ceiling blue hotspot corrected in the visual fix pass. |
| Detail/coherence | 6 | 8 | Distinct lower/upper equipment tiers, continuous low boards, access details. Staff positions remain empty. |
| Gameplay readability | 8 | 8 | Both camera profiles/HUD preserved; clearer referee surfaces and no end-hung screen in sideline action. |
| Technical restraint | 7 | 8 | Combined native geometry, shared materials, existing budgets; actual mobile cost still unprofiled. |

## Manual review checklist

1. Use **Play Hub Entry**, PLAY, an unlocked division, Start Championship. Verify the unchanged scoreboard, joystick/actions, 14 starters, central real ball and wide preparation shot.
2. Tap Left Shift / touchscreen SPRINT after the whistle. Try C/SWITCH during the contest (locked), then after acquisition (restored). Pause before and during the race; resume without a second release.
3. Toggle F6 in ordinary play and through Pause → Settings. Move screen-right/up with keyboard/joystick, pass B, lob L, hold/release Space, sprint, steal F and switch C. Approach both goals and each sideline; watch the ball/selected athlete against the new equipment.
4. Inspect the red 2 m/red 5 m/yellow 6 m/white goal and halfway markers and mirrored colors. No full-width internal rope or football circle should appear. Officials should walk on the colored platforms without hitting boards.
5. Use F8 to inspect deck/stands; the end-corner chairs, lower equipment tier, side platform and ceiling suspension should be coherent. The physical board must match HUD scores/time/exclusions. F8 is inspection, not replay.
6. Review Game views at 16:9, 19.5:9 and 20:9 and all three graphics settings. Low retains essential geometry; smaller props follow the fine-prop budget. Check transparent water and swimmer visibility.
7. Score at both ends, inspect keeper contact/center pass, progress through four quarters with the Q3 end switch. The centre device appears only for period starts. Verify pause, restart confirmation, exit confirmation, final result and lobby return. Do not use a real profile to repeatedly farm test rewards.

Next work requires separate authorization: device profiling and touch/notch review; authored staff/referee/crowd/environment detail if desired; future full UI mapping review; replay playback remains a separate milestone. No work beyond this arena pass was started.

## References and tooling notes

The project's existing [Championship Night art direction](PREMIUM_ARENA.md), [field/sprint guide](FIELD_AND_SPRINT.md), [M18 sprint/lighting guide](SPRINT_POLISH.md), [M19 camera/display guide](MATCH_BROADCAST_AND_CAMERAS.md) and [water guide](WATER_VISUALS.md) remain the architectural references. The user-supplied design text is interpreted through the official rule corrections above; no proprietary assets were copied.

Skills consulted: unity-cli, unity-pipeline and craft-game-floors (art-direction, camera-rigs, scene-lighting and visual-scorecard references). Their generic new-game/Cinemachine/global palette recommendations were not applied to this established project. Relevant official Unity references checked: [Mesh.CombineMeshes](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/mesh/combinemeshes), [URP Bloom](https://docs.unity.com/en-us/engine/6000.6/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom) and [URP Volumes](https://docs.unity.com/en-us/engine/6000.6/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes). The implementation uses the existing URP/Lit, TMP and reflection architecture without new dependencies.
