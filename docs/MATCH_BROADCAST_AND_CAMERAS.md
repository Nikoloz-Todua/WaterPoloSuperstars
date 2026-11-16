# Milestone 19 — live arena display and two gameplay views

> Subsequent [arena redevelopment](ARENA_REDEVELOPMENT.md) preserves these camera profiles, input mappings and display bindings. It moves the existing four-face jumbotron to the roof centre and the two low timing boards beside end-corner equipment, with new poolside geometry checked from both views. Use that guide for current placement screenshots and acceptance; this document retains M19 architecture/history.

Production is the existing `Assets/Scenes/OlympicMatchArena.unity`, with its original GUID. Unity **6000.6.7f1 / URP 17.6**, Windows Editor. The new native recovery is `Assets/Scenes/Recovery/OlympicMatchArena_PreMatchBroadcast.unity`; it is an exact pre-M19 scene copy, outside builds. Recovery scenes share current scripts and therefore recover layout, not source code. M19 authorization supersedes the older milestone stop instructions; stop after M19.

## Open and compare

- **Tools > Water Polo > Play Hub Entry** starts Boot → MainLobby. Choose **PLAY → Division 1 → Start Championship**, or Training for an isolated economy-free session.
- **Tools > Water Polo > Play Olympic Match Arena** launches the current match directly without a hub reward session.
- **Tools > Water Polo > Arena > Open Olympic Match Arena** opens the saved production scene.
- **Tools > Water Polo > Arena > Set Up Live Scoreboard and Broadcast Cameras** is already applied. It requires saved OlympicMatchArena outside Play Mode; repeat invocation preserves authored tuning. Do not run old prototype setup tools over production.
- **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** restores normal Editor startup.
- **F6** toggles **Lengthwise / Sideline Broadcast**. **Pause → Settings → Camera View** has two real choices and highlights the selected one. **Save & Back**, Resume and scene teardown save through the existing preferences system.
- **F8** retains the five existing developer inspection viewpoints and then returns to the chosen gameplay view. These are inspection views, not goal replays.

The initial preparation overview intentionally takes priority over either preference. Changing the preference during that preparation takes effect when the referee starts the contest. The existing opening position `(0,28.926,-21.6945)`, rotation `(53,0,0)` and FOV `52` are preserved.

## One live match, one display observer

`PrototypeMatch` still owns phases, scores, pause, transitions and completion. Its `MatchClock` is the only period clock; `PossessionShotClock` is the only shot clock. `BallController` owns the real ball, possession and flight. `MatchRules` owns eligibility, fouls, exclusions and legal re-entry. `PlayerSelection` owns human control. No display or camera changes these authorities.

The existing **one `ArenaEventDisplay`** now feeds the new primary panel plus all **six existing score/time faces**: four jumbotron faces and two end-deck timing screens. `Match.Changed`, `PauseChanged`, `NewMatch` and actual referee events trigger refreshes. Cached per-render-frame reads of the actual clocks keep the arena faces on the same displayed second as the HUD, without a separate countdown or slower polling delay. Text is assigned only when its displayed value changes; the normal refresh path allocates no strings when values are unchanged. Scores update immediately on the existing goal event path. No independent score, exclusion duration, countdown coroutine or reward handler was added.

The board shows:

- Actual **BLUE / WHITE score**, period, formatted period time and shot clock.
- **PERIOD OPENING**, **SPRINT FOR THE BALL**, **LOOSE BALL**, **LIVE**, **PLAY STOPPED**, **PAUSED**, **PERIOD END**, **HALFTIME**, or **FINAL**, derived from current state.
- A restrained **GOAL / BLUE** or **GOAL / WHITE** emphasis for 1.5 pause-aware presentation seconds. Pause and major transitions take priority.
- Seven actual cap numbers per team, including the existing goalkeeper. Current authored caps are **1, 2, 3, 4, 5, 7, 13** on each team; no cap 6 or invented player names were inserted.

The game still uses four **80-live-second** periods displaying **480 seconds** (6×), a separate **28-second** possession clock and **18-live-second** temporary exclusions. The board reads those configured values rather than imposing replacement rules.

## Primary LED installation

**`Olympic Arena — WaterVisualTest Foundation / Primary Championship Scoreboard`** is mounted on the **west spectator wall**, center **`(-23.15,11.8,0)`**, front toward **+X**, above seating and clear of the official lanes, both goals and the playable field. Its housing is **15.8 × 7.6 × 0.48 m**. Four combined geometry meshes form the navy housing, open metallic bezel and wall mounts, dark panel and thin cyan edge traces. A local shared front-face TMP material drives live 3D text. Amber dot renderers are reused for exclusions.

This is a physical venue object, with depth and supports, not a second phone overlay. There is no world-space Canvas, extra display Camera, RenderTexture, additional Light, or gameplay collider. The six older faces mirror essential information and do not create another observer. The primary board is naturally partly or wholly outside some gameplay frames; the screen-space match HUD remains the readable information source during play. F8 and the saved close-ups demonstrate the full installation.

The visual prefab is `Assets/Arena/MatchBroadcast/Prefabs/ChampionshipScoreboard.prefab`. It contains reusable visual children, not duplicate match controllers. Scene-only bindings remain on the existing observer. All generated materials/meshes are native assets under `Assets/Arena/MatchBroadcast/`; there are no imported third-party assets or new packages.

## Actual exclusions

Each roster identity maps once to its cap label and dot. The display reads **`SwimmerIdentity.ExclusionRemaining`** and **`PermanentlyRemoved`**, as maintained by `MatchRules`:

- Temporary exclusion: amber dot and cap, with a smaller second-line countdown using **CeilToInt of the actual remaining timer**.
- Legal re-entry: clears when the authoritative timer/state clears, including the current game profile's possession-regain and goal release conditions.
- Third personal removal: muted **OUT**, with **no temporary-exclusion dot**. A removal is not represented as an endlessly running exclusion countdown.
- Ordinary fouls: no active indicator unless the rules actually excluded that player.
- Restart: existing rule reset clears temporary and permanent states; the display refreshes from them.

Multiple active exclusions are supported without changing layout or stacking banners. The board never enables a collider, moves a waiting athlete or decides eligibility. References were checked against the current [World Aquatics regulations page](https://www.worldaquatics.com/rules/competition-regulations) and its [18 February 2026 regulations](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf). Existing validated simulation rules remain unchanged; this is not a claim of complete regulatory compliance.

## Camera architecture and tuning

No Cinemachine package was present, and none was installed. **`GameplayCamera` remains the sole controller of one actual gameplay output Camera.** Its original lengthwise tuning and lens serialization are unchanged. The existing High-quality water reflection camera/texture is separate pre-existing water rendering, not an additional gameplay or scoreboard view.

`MatchCameraView` selects two live profiles:

| View | Behavior |
| --- | --- |
| Lengthwise | Existing orientation, predictive focus, smoothing, contextual keeper/set-piece/sprint profiles, goal look-ahead and bounded adaptive framing. Captured original lens is restored after sideline use. |
| Sideline Broadcast | East long-side camera looking west, with goals along screen left/right. Smooth longitudinal pan follows controlled athlete and real ball, bounded velocity prediction and current attacking-goal context. Limited lateral look-at and adaptive height keep important action inside the usable viewport. |

Sideline Inspector defaults are **22 m lateral distance**, **17 m height**, **54° FOV**, **0.48 s damping**, **0.65 ball weight**, **1.45 maximum zoom factor**, and zero authored offsets. Position stays inside the arena wall at X≤22.5; adaptive height is bounded to 12–26 m and the focus stays within X −3…+4.5 / Z −4…+4 m around field center. A small five-pass projection check widens height for the controlled athlete, ball and nearby goal, using a central safe region rather than simply chasing one target. User distance/smoothing preferences remain effective. Fields are exposed for future visual tuning; arbitrary extreme offsets can invalidate authored sightlines.

Live styles blend over **0.6 unscaled seconds**, with smooth rotation/FOV and a small upward arc. Switching while paused changes presentation while clocks, bodies, ball, possession and sprint state stay stopped. Targets remain live during ordinary blends; a disappeared target falls back to an eligible roster motor. Returning from F8 snaps to a valid chosen profile, as the existing inspection workflow did. Restart preserves the preference and resets only the existing match. Full time, set pieces, loose balls and goalkeepers use the same camera ownership.

Preparation uses the exact original full-arena opening. Once `SprintSetup` changes to `SprintRace`, the chosen gameplay view blends in; designated sprinter lock, real effort meter, cradle, pickup and subsequent ordinary controls remain under the M18/M17 authorities. Q3 uses reversed goal references, not an assumption that Blue attacks +Z. The sideline camera stays on the same physical side after halftime; Blue then attacks the opposite screen direction.

## Camera-relative input

`SwimmerInput` remains the only human movement writer. It maps the selected keyboard/gamepad/touch vector once through the output Camera's projected screen-right and screen-up axes before supplying `SwimmerMotor.SetMoveInput`. AI commands remain world-space and unchanged. The motor still controls acceleration, speed, stamina, bounds and physics.

- Lengthwise: right → +X, up → +Z, matching existing controls.
- Sideline: right → +Z, up → −X. Moving toward either goal is visually horizontal.
- During a blend, input follows the actual visible orientation. No separate joystick implementation was added.
- Sprint-only zero-direction auto-swim is already a world-space command and is applied after this mapping; it is not rotated a second time. Shift edges, mobile SPRINT, normal hold behavior and pause releases remain intact.
- Pass, shot, steal, switch and keeper APIs retain their meanings. Steering determines the same swimmer facing used by existing pass/shot aim.

The existing settings panel was compacted within its current two columns so camera choices, control opacity and touch-size controls do not overlap. No new page or fullscreen tutorial was created.

## Sightlines, signs and lighting

The new sideline camera is inside the east wall and above the seating/deck/benches. Bounds prevent ordinary tracking from moving through walls or stands. `BroadcastSightlines` observes only the **two existing M18 suspended light-gallery renderer groups**. Cached aggregate bounds and cheap rays to the visible controlled athlete, ball or primary display cut away only a blocking gallery, including when a blend/inspection camera is inside its bounds. A 0.2-second unscaled hold prevents chatter. Original renderer flags restore on disable/scene exit. No field, swimmer, goal, referee, stands or physics collider is hidden or changed.

This is a deliberate bounded architectural cutaway, not a general physics camera-collision solver. Unusual manual Inspector poses or future architecture need new sightline review. Hanging structures can remain visible elsewhere in an inspection image when they do not obstruct those key subjects.

The north deck's two wordmarks had a 180° in-plane rotation relative to the established lengthwise view; they now use `(90,0,0)`. Coordinates, field axes, goals and artwork are untouched. Fixed deck branding naturally reads sideways/opposite from another spectator side. Live digital text uses a local **back-face-culling, depth-tested TMP material**, positive scale and explicit front transforms; its reverse side does not show mirrored text through the housing. Existing jumbotron face transforms were already mounted toward their corresponding viewing sides and were retained.

Lighting changes are limited to the new board's metallic/navy finish, restrained emissive cyan traces and amber indicators, integrated with the existing cobalt wall reveals. **No new realtime lights, postprocessing overrides, beams, reflection probes or lighting redesign.** M18 Low/Medium/High lighting, transparent water, swimmers and ceiling arrays remain. Essential scoreboard content and both camera modes work at all three quality levels.

## Replay foundation — implemented boundary and future design

Implemented now: `MatchCameraView`, explicit read-only `Presentation` reporting **Live / PeriodOpening / Inspection**, reserved presentation enum values for **GoalCelebration / ReplayPlayback / ReplayOrbit / ReturnToLive**, and `CaptureCameraPose()` returning position, rotation, FOV, view and presentation. The last four states have no active playback behavior. The snapshot does not pause, resimulate, record or alter the live match. F8 is an inspection tool only. There is **no working goal replay, orbit UI, recorder, scrubber or replay button** in this milestone.

A later replay milestone should:

1. Keep a bounded **6–10 second, 20–30 Hz in-memory rolling buffer**, sampling post-physics presentation state. Store actual ball transform/visible mode and the fourteen athlete root transforms plus the animation states/normalized times, blend weights and action metadata required by `RiggedSwimmerVisual`. Use fixed arrays/ring indices, stable IDs and interpolation; do not write every frame to JSON.
2. Freeze a completed goal window on the existing authoritative goal event. Attach scoring team/time, recorded shooter/keeper IDs and an assist only if a reliable authoritative pass chain exists. The current match does not expose a complete assist/statistics ledger; do not invent those values.
3. Play the recorded visual poses on presentation-only ghost rigs with gameplay colliders, AI, selection, possession, clocks and rewards disabled. A visual transform/animation buffer reproduces recorded motion approximately; it cannot perfectly recreate all contacts, water particles, animation transitions or shader time. **Do not promise deterministic PhysX resimulation from transforms.**
4. Add an explicit camera-pose owner alongside the existing live controller. Only the chosen owner writes the one output Camera each frame: live, celebration, playback or orbit. Enter/exit restores a captured live pose, then re-resolves live targets safely. Never let a replay call goal scoring, pickup, match reset or economy transactions. Decide the authoritative live-pause policy explicitly before wiring replay UI.
5. Add bounded orbit/look-at/distance profiles over the recorded target, then future replay/play-pause/timeline/camera/drag/pinch/exit controls. Keep separate replay input scopes so controls cannot charge a live shot or move a swimmer.

A raw 8-second/25 Hz buffer for 15 position+rotation pairs is about **84 kB** of float payload before animation, camera and metadata; actual memory depends on representation and rig playback overhead. Budget and profile those costs on phones before committing to a ghost-rig implementation. Exact animation/water reproduction remains a later engineering problem, not a solved feature.

## Performance and limits

Display text and geometry are reused. No ordinary score update instantiates or destroys objects. Four combined housing meshes, shared local materials, 23 world text labels and fourteen normally disabled tiny dot renderers avoid hundreds of UI objects and extra display rendering cameras. The recorded close-up audit counts **41 MeshRenderers / 8,594 referenced mesh vertices** including text and repeated built-in dot meshes; this is not a unique vertex or draw-call measurement. Only active dots render. Camera framing uses cached small arrays and five bounded passes; sightlines use cached bounds and at most six short ray/bounds checks.

World text still has draw/atlas costs; visibility at long distance is intentionally secondary to the HUD. Existing High-quality water reflection and the detailed venue remain mobile GPU concerns. No phone, build, thermal, battery or profiler measurements are claimed. A physical notched-screen safe-area test, extreme Inspector camera tuning, exhaustive occluder coverage and long full-duration match testing remain later checks.

## Verification and evidence

Fresh focused Windows Editor acceptance completed **2026-11-16/17**. Logs/screenshots are under ignored `Logs/MatchBroadcast/`; source is `Tools/Verification/MatchBroadcastVerification.cs`. The focused existing sprint/hub fixtures were reused from `Tools/Verification/FieldSprintVerification.cs`. Exact M19 scope including native assets/metas: [MILESTONE_19_FILES.txt](MILESTONE_19_FILES.txt).

| Fresh run | Confirmed result |
| --- | --- |
| `Structure` | PASS: exact recovery, original GUID/lens/legacy camera tuning; **140 protected gameplay/rig/sprint/ball/referee records unchanged**; 14 roster bindings, one observer/six reused faces, one Canvas/EventSystem, no missing scripts, repeat-safe setup and saved reload. |
| `Controls` | PASS: actual queued keyboard/gamepad/touch events in both views; right/up, sprint, release/deceleration and C switching; actual pointer camera choices, F6 edges, unscaled paused blend without changing bodies/ball/clocks/sprint; Save/Reload/invalid fallback and F8 return. **0 new runtime errors/warnings.** |
| `Displays` | PASS: initial/live data agrees with HUD and six faces; real ordinary foul without dot, two concurrent actual exclusions with correct caps/countdown, pause, timed re-entry, physical Blue/White goals and team emphasis, clean restart. **0 new runtime errors/warnings.** |
| `Periods` | PASS: actor/ball projection at both goals, center, near/far sidelines, distant loose ball and selected keeper; four real sprint pickups, exact opening each quarter, selected view afterward, one halftime reversal, FINAL and restart preference. **0 new runtime errors/warnings.** Period ends accelerated. |
| `Actions` | PASS: actual B pass, physical reception/selection transfer, side D aim, Space charge/release/goal during blend, F steal, L lob, swept goalkeeper catch and shot-clock violation/restart. AI isolated and keeper probabilities temporarily controlled/restored. **0 new runtime errors.** |
| `LayoutAndOrientation` | PASS: advancing **1920×1080, 2340×1080, 2400×1080** Game views; pointer choices at each; non-overlapping controls/settings, all three quality tiers, digital front culling/positive scales, north wordmarks, five F8 views and no display RenderTexture. |
| `ExclusionEdgesAndArena` | PASS: actual regain/goal re-entry, third-personal OUT separated from temporary exclusion, restart clearing removal, inside-gallery cutaway, missing-target fallback and read-only snapshot. **0 new runtime errors.** |
| Existing `SprintCompetition`, rerun with Sideline selected | PASS: 14 legal starts, no pre-whistle collection/taps, one whistle/water contact/pickup, actual human and AI wins, losing defense live immediately, bounded effort/stamina, actual pointer sprint hold/release and pause/resume. |
| Existing `BootEntry` → `LoggedHubLoop`, fresh run | PASS: production menus/pointer unlocked launch; restart cancel/confirm; abandoned sprint exit without reward/holder leak; new launch; four real pickups with accelerated period ends, physical goal, official victory, **one receipt**, duplicate retry/replay rejection, serialized in-memory verification-profile reload, result and lobby return. Real disk profile SHA-256 unchanged. **0 new runtime errors.** Evidence under `Logs/FieldSprint/`. |
| `Restore` / `Delivery` | PASS: saved arena, Boot entry, Play stopped, original preference bytes/Game-view size/input-focus settings restored, real profile unchanged. Temporary background/auto-tick overrides removed at delivery. |

Compilation passed; final current Console **0 errors / 0 warnings**, `compilationFailed=false`. Pipeline retains earlier milestone warnings and resolved development/probe errors separately from current Console; no manual clear was used. Read-only whitespace checks report Unity's native empty-value YAML fields in the scene; source/docs checks are clean. Git LF/CRLF notices are not Unity runtime warnings; Git configuration was not changed.

Initial probes corrected an overly short motor-stop assertion, nested Editor JSON comparison, Unity-vector JSON report serialization and a fixture restart with AI still disabled. Those initial probes failed; the final named runs above passed. This was not an uninterrupted full-duration match, exhaustive penalty/roster-depletion check, phone test, build or performance profile.

## Actual Game-view screenshots

Real Editor renders, not mockups/device captures. Some action/framing fixtures park AI. Close-ups temporarily hide the overlay Canvas and restore it afterward. Match values differ across captures.

| View | Local evidence |
| --- | --- |
| Original opening, matched pose/lens | [Before](../Logs/MatchBroadcast/Before-Opening.png) / [After](../Logs/MatchBroadcast/After-Opening.png), HUD visible in both. |
| Lengthwise gameplay | [Before](../Logs/MatchBroadcast/Before-Lengthwise.png) / [After](../Logs/MatchBroadcast/Lengthwise-Gameplay.png); [final verified view](../Logs/MatchBroadcast/Lengthwise-Verified.png). |
| Horizontal gameplay | [Sideline with live AI](../Logs/MatchBroadcast/Sideline-Gameplay.png). |
| Both goal areas | [Left](../Logs/MatchBroadcast/Sideline-Left-Goal.png) / [Right](../Logs/MatchBroadcast/Sideline-Right-Goal.png), isolated fixtures. |
| Arena installation | [Before existing F8 pose](../Logs/MatchBroadcast/Before-Arena.png) / [After matched pose/FOV52](../Logs/MatchBroadcast/After-Arena-Matched.png), overlay hidden only in after; [additional wider inspection](../Logs/MatchBroadcast/Full-Arena-Scoreboard.png), temporary FOV64. Gameplay lens unchanged. |
| Physical board | [Close-up](../Logs/MatchBroadcast/Scoreboard-Closeup.png), paused for typography inspection. |
| Exclusions | [Two actual exclusions](../Logs/MatchBroadcast/Scoreboard-Active-Exclusions.png) / [third personal OUT](../Logs/MatchBroadcast/Scoreboard-Match-Removal.png). |
| Goals and final | [Blue goal](../Logs/MatchBroadcast/Scoreboard-After-Blue-Goal.png) / [White goal](../Logs/MatchBroadcast/Scoreboard-After-White-Goal.png) / [Final](../Logs/MatchBroadcast/Scoreboard-Final.png). |
| Corrected signage | [North deck and timing screen](../Logs/MatchBroadcast/Corrected-North-Deck-Signage.png). |
| Landscape HUD | [16:9](../Logs/MatchBroadcast/Match-HUD-1920.png) / [19.5:9](../Logs/MatchBroadcast/Match-HUD-2340.png) / [20:9](../Logs/MatchBroadcast/Match-HUD-2400.png). |
| Real settings | [Camera choices](../Logs/MatchBroadcast/Pause-Camera-Settings.png); additional `Settings-1920/2340/2400.png`. |

Additional `F8-Inspection-1…4.png`, `Sideline-Quality-0…2.png` and `Sideline-Keeper-Save.png` support targeted checks. Fresh hub launch/result/lobby captures are under `Logs/FieldSprint/`.

## Manual acceptance

1. **Tools > Water Polo > Play Hub Entry** → PLAY → unlocked Division 1 → Start Championship. Check the original opening, 14 athletes, holder and HUD.
2. After the whistle tap Shift/SPRINT. C/SWITCH stays locked during the contest, then returns after genuine pickup. Check effort meter and ordinary controls.
3. Press F6 during play. In both views steer right/up using WASD or joystick, navigate both goals, B/PASS, hold/release Space/SHOOT, Shift/SPRINT, L/LOB and F/STEAL when eligible. Check goalkeeper possession.
4. Pause → Settings → Camera View. Try both choices while clocks/ball/athletes stay frozen. Save & Back, Resume and start another match to check persistence. Verification restored your original preferences instead of leaving its test selection.
5. Score at both ends. HUD, main board and secondary faces agree; the conceding side uses its ordinary center pass, with no sprint/holder. Pause/resume during a view blend without firing an action.
6. Inspect a real ordinary foul and off-ball hold against the possessing side. Only an actual exclusion lights the cap; its actual timer stops while paused and clears on legal re-entry. Third personal removal reads OUT.
7. Continue all four quarters; Q3 changes ends once. Check FINAL, results/reward once, lobby return, and restart/exit confirmation. Abandonment must earn no reward.
8. Compare both modes at 1920/2340/2400×1080, both goal areas and sidelines, HUD/buttons, F8/front-facing screens and all quality tiers. Physical notches/touch comfort still require device testing.

Later work requires separate authorization: actual replay playback, mobile profiling/readability and camera tuning based on physical-device play. Do not begin it automatically.
