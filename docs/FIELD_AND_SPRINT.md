# Milestone 17 — authentic field and period sprint-offs

> The subsequent [poolside redevelopment](ARENA_REDEVELOPMENT.md) preserves all field/sprint coordinates. It improves referee surface coverage, end-corner benches, pool-edge architecture and display integration, with fresh four-period/goal/contact/hub checks.

> Current camera/live-display additions and fresh M19 acceptance: [MATCH_BROADCAST_AND_CAMERAS.md](MATCH_BROADCAST_AND_CAMERAS.md). Field geometry and rules below are preserved.

> M18 refinement is documented in [SPRINT_POLISH.md](SPRINT_POLISH.md); the sections below retain the original milestone architecture and evidence. Use the latest plan.md handoff for current authorization.


Implemented and checked in Unity **6000.6.7f1 / URP 17.6**, Windows Editor, 2026-11-16. Production remains the saved `OlympicMatchArena`, with its original GUID, Championship Night venue, transparent water, fourteen rigs, M16 HUD, rules, hub and economy. Stop after this milestone. All changes are unstaged and uncommitted.

## Rules and measured field

Reference: [World Aquatics competition regulations](https://www.worldaquatics.com/rules/competition-regulations), [18 February 2026 PDF](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf), Part Six articles 6, 21.6, 21.7, 21.9 and glossary 2.7.1 (PDF pages 224, 243–245). The downloaded reference/text extraction stays under ignored `Logs/FieldSprint/`. These references guide equipment and starts; this remains the game's simplified rules profile.

The actual `PoolBounds` is **25 m long × 20 m wide**, centered at `(0,0,0)`, waterline **Y=0**, goal fronts **Z=±12.5**. Existing `CompetitionDimensions.asset`, GoalMouth opening/colliders and pool bounds were retained. One Unity unit represents one metre. Distances use the actual goal planes, never the back wall. The side markings are static field coordinates; changing team ends does not move them.

| Mark | North Z | South Z | Color |
| --- | ---: | ---: | --- |
| Goal line | 12.5 | -12.5 | White |
| 2 m | 10.5 | -10.5 | Red |
| Penalty 5 m | 7.5 | -7.5 | Red |
| 6 m | 6.5 | -6.5 | Yellow |
| Halfway | 0 | 0 | White |

`Competition Field Equipment` is a child of the existing arena foundation. `Measured Competition Ropes` combines faceted spool buoy meshes by material, with 0.12 m diameter, approximately 0.22 m spacing and their centers at the waterline. Side sections are red 0–2 m, yellow 2–6 m and green 6 m–halfway. Separate red 5 m ticks avoid confusing the penalty mark with the yellow 6 m line. End ropes sit **0.30 m behind** each goal line, white except red goal-area/re-entry sections, with small cables and anchor plates. Red east-corner entry enclosures keep the existing exclusion/waiting coordinates unchanged. No huge water labels were added.

The old game-authored `Water Polo Field Markers — no race lanes` group is inactive. It contains the football-style center circle, center floor bar and redundant goal-area floor boxes. It has no colliders. The original coordinates, Ball Start marker, AI/formation data, decorative tiled pool and source assets remain intact.

## Goals and officials

Each original goal now has `Competition Goal Visual`: chamfered white rectangular posts/crossbar with **3.0 m clear width, 0.9 m clear height, 0.08 m members**, metal stays and float supports. A shared lightweight cord mesh encloses the back, sides, roof and underwater bottom, approximately 0.12 m grid, with a slight rear bow. Existing GoalNetReaction makes a bounded 0.08 m reaction; no rope simulation. Old visual renderers remain serialized but disabled. **Every original goal collider and GoalMouth detector remains enabled and unchanged**, including post/crossbar, side/back pocket and ball scoring direction. The frame follows those collider dimensions.

`Elevated Referee Platforms` adds two **1.0 m wide** navy lanes, top **Y=0.70**, centered **X=±12.65**, with metal trim, outer safety rails, colored inset section strips, full-width distance ticks and four access steps at each end. The dark top with a colored inset is a deliberate venue/mobile simplification of the fully section-colored regulatory platform. Existing benches remain outside the clear lane. Referee foot bounds were measured; deck/root height is **0.62**, so feet meet the 0.70 m surface. Movement uses the existing controller, deck offset 2.65 and travel fraction 0.49. No foul decision logic changed. These officials use kinematic placement: equipment adds **no colliders** or navigation system.

## Sprint state and authorities

`PrototypeMatch` alone owns phases and transitions. Two enum values are appended without changing old values:

`SprintSetup → SprintRace → Playing`

1. On initial arena start, confirmed match restart, and each `ContinueMatch` into Q2/Q3/Q4, the authority stages the period. Score, foul history and later-period clocks are retained according to existing rules. Q3 uses the already-swapped goals/formations.
2. `PeriodSprintOff` places eligible athletes on their actual defended goal line. Six fields span about ±8.1 m with over 1 m spacing; the keeper is central, no field starts between the posts or ahead of the line. Existing treading presentation remains. The fastest eligible field swimmer (cap-number tie break) is designated on each team. Blue control selects that athlete. Cyan/yellow water-level rings identify both sprinters; the original green human ring remains.
3. A pause-aware **2 s preparation** holds all swimmers. The single existing ball is kinematic in the center cradle, collider disabled and collection forbidden. No stroke can be banked before the whistle.
4. The authority releases once, raises `SprintStarted` once, unlocks swimming and starts the real ball dropping. MatchAtmosphere reuses its existing whistle cue/fallback; no new audio was imported. The small M16 banner reads **SPRINT FOR THE BALL**. Setup/race scoreboard labels are READY/SPRINT.
5. `BallController.ReleasePeriodBall` restores dynamic 3D gravity and zero initial velocity. The ball drops **0.45 m above its existing surface center** (center Y=0.52 with current surface offset). Collection is barred until the normal water-impact path runs. Existing pooled impact/splash effects handle contact. The visible ball pose is synchronized immediately, including an immediate pause before physics advances.
6. Real `TryCollect` ownership ends the race through the existing `PossessionChanged` event. There is no winner assignment, ball teleport, free pass or post-win stoppage. The loser immediately runs ordinary AI/defense. Normal movement, passing, charge/shoot, switching, steals and keeper behavior resume.

Match/shot/exclusion clocks still advance only in **Playing**. World Aquatics starts period timing on first ball contact; this profile uses real acquisition as that contact boundary. Defaults remain **80 live seconds displaying 480 seconds**, four periods, **28 s possession**, **18 live seconds exclusion**. No duration or speed mapping changed. `CanSwim` permits race movement without widening the old normal action/foul gates. BallController still exclusively owns possession, flight and water response. No new match controller or ball exists.

The reusable `Retracting Period Ball Release` contains an open ring, narrow stem and submerged base. `SprintBallReleaseVisual` only observes state: after the whistle it lowers 1.5 m over 0.55 s, then hides. It has no collider, never parents/duplicates the ball, remains hidden after ordinary goals and disappears with the scene on exit. It reappears for the next period. Pause freezes its presentation timeline.

After a **goal**, existing conceding-team center setup/signal/physical pass remains unchanged. Blue still waits for B/PASS; White performs its normal automatic pass. Goal restarts never create a sprint. Period breaks still use the existing Continue action; automatic break continuation was not added. Halftime swaps ends exactly once before Q3; confirmed new matches restore original ends/counters.

## Controls, AI and camera

| During sprint only | Behavior |
| --- | --- |
| Repeated Left Shift | Fresh presses contribute strokes. Presses closer than 0.16 s are ignored. |
| Repeated touchscreen SPRINT | Uses the same stroke API and cadence cap. |
| Hold Shift / SPRINT | Accessible normal sprint fallback with normal stamina cost. |
| WASD / arrows / joystick / existing gamepad | Steer normally. With no directional input, sprint hold or a recent stroke steers toward the center ball. |
| Pause / release | Holds and stroke energy clear. No charge or stored stroke leaks into ordinary play. |

Each accepted stroke adds 0.28 energy (cap 1), decaying 0.85/s, with a short 0.38 s swim request. It multiplies **acceleration up to 1.35×**, never maximum speed; normal sprint speed and stamina remain authoritative. Extremely rapid taps gain no extra benefit. A pre-held Shift must be released before a new press is counted. Ordinary Shift remains unchanged after acquisition. During opening preparation/race, irrelevant attack/defense buttons are hidden; SPRINT and valid loose-ball COLLECT remain. The full M16 context layout returns immediately afterward.

White's designated sprinter uses the same SwimmerMotor, stamina, stroke energy and speed ceiling, with a bounded **0.16–0.30 s reaction** varying by quarter/team. No teleport or scripted winner. Other AI fields enter existing supporting formation targets and may acquire the ball through the same API. Both a controlled human win and idle-human AI win were observed; this is functional fairness evidence, not statistical difficulty balancing. A short goal-line departure corridor prevents the ordinary 1.1 m motor boundary inset from snapping staged swimmers illegally ahead; ordinary bounds resume once inside.

The one existing GameplayCamera gains a period profile: staged framing includes all athletes/ball, racing framing follows the controlled sprinter/center and both designated racers with existing damping. It returns to the ordinary profile on acquisition. F8 developer inspection views remain. Original swimmer assets, animations and main camera architecture were preserved; original swimming wakes now observe `CanSwim` during the race.

## Tools menu

`Tools > Water Polo` now contains three root launch commands and the requested submenus:

- **Play Hub Entry** — Boot → MainLobby; PLAY → Division 1 (or another unlocked mode) → Launch.
- **Open Main Lobby** — opens the existing saved lobby in Edit Mode.
- **Play Olympic Match Arena** — isolated current arena, including period sprint.
- **Arena** — Open Olympic Match Arena; Set Up Authentic Field and Period Sprint-Offs; Refresh Championship Night Reflection.
- **Match UX** — existing M16 setup.
- **Animated Athletes** — isolated swimmer lab.
- **Verification** — Audit Current Production Field; Play WaterVisualTest.
- **Recovery** — Open Pre-M17 Arena Scene; Use Boot as Editor Play Entry; deliberate arena rebuild/dimension/activation, frontend setup, athlete reimport/apply/fallback and water-test setup commands.
- **Legacy Prototype Tools** — movement, camera/ball, two-swimmer passing, 2v2, AI, goalkeeper, scoring, M8/M9/M10/M11, old realism/venue/water setup and PrototypePool direct play.

All **42** MenuItem registrations were audited, without duplicates. Only menu attributes and one missing-catalog diagnostic string moved; old implementation methods/classes/scenes remain because newer setup/verification code calls several helpers. Legacy setup and recovery rebuilds are deliberate authoring operations, not launch commands. Do not run a base arena rebuild merely to review M17. Setup was already applied and saved; repeat calls preserve authored tuning. Native pre-change recovery: `Assets/Scenes/Recovery/OlympicMatchArena_PreAuthenticField.unity`, outside builds, exact copy of the M16 arena before M17. It shares scripts and is a scene/layout recovery, not a source rollback.

## Fresh verification and evidence

Versioned runner: `Tools/Verification/FieldSprintVerification.cs`. Uses asynchronous Pipeline **script run**, advancing Play Mode, real Input System device events, actual screenshot captures and native saved-scene operations. Reports/images: ignored **Logs/FieldSprint/**. Compilation succeeded without errors or verification-source diagnostics. Final current Console: **0 errors / 0 warnings**, compilationFailed false. Historical Pipeline buffer separately retains old Burst/Windows code-integrity and existing CS0184/UAC0009 warnings; no security/package/cache changes or manual clearing were used to hide them.

| Report | Passed checks |
| --- | --- |
| Structure-PASS | Same scene GUID/exact native recovery; 857 protected athlete, gameplay tuning and M16 Canvas component records unchanged; Boot/MainLobby/PrototypePool/catalog/build-entry/swimmer/water-file hashes unchanged; 14 rigs, one Canvas/EventSystem, no missing scripts, repeat-safe save/reload; all goal-measured mirrored markers and goal colliders; 42 unique menus. |
| Sprint-PASS | All 14 on current goal lines, legal spacing/post separation; no pre-whistle movement/tap bank/pickup; one whistle, real gravity/water contact and one claim; immediate visible cradle pose; capped repeated Shift, real mouse hold/release, stamina, human and AI wins, loser moving immediately, pause in preparation/race without restart. |
| GoalContacts-PASS | Correct inner opening; outside/tangent and crossbar-height scoring rejection; actual ball collision contacts against post and crossbar at **both goals**, no score on those contacts. |
| TouchGameplay-PASS | Repeated synthetic touchscreen SPRINT; clear stroke state; ordinary keyboard swim/sprint, physical pass/reception/control transfer, charged shot release, legal steal and lob; existing swept keeper catch at new frame. |
| Periods-PASS | Four real sprint starts/acquisitions, Q3 after one end swap; physical Blue/Q1 and White/Q3 goals, conceding center passes without sprint/apparatus; retained score, draw FullTime, confirmed clean Q1 restart. |
| Views-PASS | Actual warm Game views at **1920×1080, 2340×1080, 2400×1080** (16:9, 19.5:9, 20:9), sprint control bounds, preparation/race/overview/goal/platform/cradle, Low/Medium/High water supported, measured referee feet/lane alignment. |
| HubLoop-PASS | Actual Boot/lobby pointer Division 1 launch; restart cancel/confirm fresh session; abandoned sprint no rewards/stray holder/frozen lobby; four real official sprint acquisitions, one 1:0 victory receipt, bridge retries and replay deduplication, JSON profile reload, results/lobby; **real user disk profile hash unchanged**. Isolated memory repository. |
| Launch-Menus-PASS | Actual Open Main Lobby, Arena/Open, Play Hub Entry and root Play Olympic Match Arena commands executed; direct opening acquired physically. |
| Fresh-Hub-Console-PASS | After teardown fix, repeated official launch/restart/abandon/four-period victory/result/lobby with an Application.logMessageReceived observer: **zero new runtime errors**, including scene unloads. |

The retained buffer exposed a genuine `MissingReferenceException` during an early abandoned-sprint scene unload: old PrototypeMatch.OnDisable called CompleteRestart after a goal had already been destroyed. Fixed by limiting OnDisable to suspension restoration/event unsubscription; explicit commands own gameplay resets. The full hub loop was rerun afterward with a live error observer and passed. Historical buffer entries remain visible, including this resolved exception and one corrected CLI command spelling error, alongside the older warnings. Current Console counts alone are not the runtime-error evidence.

Period-end tests used the **existing AdvanceLiveTime API to accelerate periods**, after real sprint pickups; no match settings were changed and no full-duration match is claimed. Isolated scoring/action fixtures temporarily disabled/restored AI; keeper save/catch chances were temporarily controlled/restored for the catch probe. Live sprint AI remained active during competition tests. Two early standalone probes waited for Playing after the unattended match had already reached a restart; their fixture now resets before waiting and reruns passed. No unresolved focused acceptance failures remain. The broader old foul/penalty suites were not rerun in M17; their authorities/tuning are preserved and M16 evidence remains historical.

Actual captures (not renders or mockups):

- `Final-Preparing-Broadcast.png`, `Final-Active-Sprint.png`, `Final-Field-Overview.png`.
- `Final-Goal-Detail-NoHUD.png`, `Final-Platform-Detail-NoHUD.png`, `Final-Cradle-Detail-NoHUD.png` — paused F8 inspection close-ups, Canvas temporarily hidden only for these detail captures.
- `Final-Sprint-Layout-1920.png`, `Final-Sprint-Layout-2340.png`, `Final-Sprint-Layout-2400.png`; `Final-Quality-0/1/2.png`.
- `Human-Sprint-Winner.png`, `AI-Sprint-Winner.png`, `Verified-Keeper-Catch.png`, `Q1-Goal.png`, `Q3-Goal.png`, `Four-Period-FullTime.png`.
- `Official-Four-Sprint-Victory.png`, `Official-Reward-Result.png`, `Returned-MainLobby.png`.

The final gameplay/overview, goal/keeper close-up, platform, cradle, wide ratios and result frames were visually inspected. A fix pass removed irrelevant sprint buttons, enabled race wakes and corrected immediate paused ball/cradle pose. Craft reference scope: existing palette/style, placeholder equipment, game-feel and visual-scorecard; no new controller, shader, package or arena redesign. Final scoped board (1–5):

Palette discipline: 4/5 — eight deliberate navy/white/regulatory-color shared URP materials; no default material in equipment.
Style uniformity: 4/5 — metal/white competition equipment follows the retained navy venue and mannequin prototype style.
Lighting depth: 3/5 — retained Championship Night contrast, lit water and readable shaded frame; no lighting overhaul.
Silhouette: 3/5 — existing animated arms/caps remain readable from gameplay and keeper close-up; distant athlete detail remains limited.
UI intentionality: 4/5 — retained M16 styling, READY/SPRINT and one compact banner; irrelevant buttons removed in fix pass (draft 3/5).
Feedback visibility: 4/5 — real ball drop, sprinter cues, whistle, swimming wake and acquisition feedback (draft 3/5; immediate paused cradle edge corrected).

## Assets, safety and limits

Exact M17 file inventory: **MILESTONE_17_FILES.txt** alongside this guide. New `Assets/Arena/AuthenticField/`: **23 mesh assets, 8 materials, 1 visual prefab**, folder/file metas, approximately **4.24 MiB**. Installed scene equipment plus goal replacements: **26 renderers / 23 shared meshes / 86,552 referenced vertices**, eight shared materials; **zero per-buoy GameObjects, added lights, particle systems or colliders**. Existing effects are reused. Counts are content measurements, not draw-call/frame-rate or mobile performance results. Low/Medium/High budgets remain; equipment uses the same bounded geometry on all three. No runtime Instantiate/Destroy loop for sprint or ropes, no net physics, dependencies or character imports.

Only `PrototypeMatch` transitions, BallController's temporary support/drop state, motor/input/AI adapters and presentation were extended. No save/economy/catalog semantics changed. Original swimmer/animation source assets, transparent-water materials/shader, Boot/MainLobby/build routes and old-plan.md stayed intact. Prior M16 edits, initial TMP fallback-font modification and unrelated untracked Pipeline configuration, SourceAssets, old-plan.md and tatus were preserved. The PDF parser was installed only into ignored `Logs/FieldSprint/python` for reference extraction, not Unity or system dependencies. Temporary input-focus and Game-view overrides were restored; Play stopped, Boot entry restored, auto-tick/background command override disabled and serialized PlayerSettings background value retained.

Remaining limits: no physical Android/iOS, real-thumb ergonomics, mobile build/profiling, statistical AI balance, full-duration match, exhaustive penalty/exclusion interaction or OS suspend test. Platform colors use the described inset simplification. Athletes align their authoritative root/head proxy on the line using existing treading poses; no new wall-grip clip. Third-foul removal remains without substitutions; if a team has **no eligible field swimmer**, setup warns/stays stopped rather than inventing a replacement (forfeit rules are not implemented). Whistle uses the existing procedural fallback when no clip is assigned. Net reaction is cosmetic and bounded; cradle retraction has no physical obstruction. The legacy recovery scene has shared updated scripts. No outstanding required feature was deliberately deferred; production-device comfort/performance and extreme roster depletion remain follow-up work.

## Manual review / next separately authorized work

1. Use **Tools > Water Polo > Play Hub Entry**; PLAY → Division 1 → Launch. Confirm seven athletes per team on their own goal lines and a real ball in the cradle.
2. After the whistle, tap Shift/SPRINT repeatedly; compare holding it, steering manually and doing nothing. Verify the AI can win; no pickup before water contact; normal defense/pass/charge works immediately after acquisition.
3. Pause before release, during the drop and during racing. Resume without a second whistle or stored input. Restart cancel/confirm; exit cancel/confirm; lobby remains responsive and abandoned match grants no rewards.
4. Inspect both ends: white goal/halfway, red 2 m and 5 m, yellow 6 m, red/yellow/green ropes, re-entry corners, no floor circle, nets/frame/collisions, referee feet and clear platform travel. F8 cycles views; finish at broadcast.
5. Score in both directions; confirm conceding center pass, hidden cradle and no sprint after goals. Continue all four periods; Q3 swaps ends once, scores survive, Q4 has one result/reward and a working lobby return.
6. Check the three landscape ratios and each quality tier. Next recommended milestone: physical phone input comfort, profiling and balanced sprint difficulty based on measured play sessions; no later milestone is started here.
