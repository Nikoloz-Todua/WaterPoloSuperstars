# Milestone 16 — match interface and mobile UX

Implemented 2026-11-16 in Unity **6000.6.7f1 / URP 17.6**, Windows Editor. This pass updates the existing OlympicMatchArena Canvas. Championship Night scenery, transparent water, all fourteen rigged athletes, animation assets, AI, physics, match rules, frontend and economy remain intact.

## Open, play and configure

- Normal game: **Tools > Water Polo > Play Hub Entry**, then **PLAY > Division 1 > Start Championship** (or Training).
- Open the saved match: **Tools > Water Polo > Arena > Open Olympic Match Arena**.
- Isolated match: **Tools > Water Polo > Arena > Play Olympic Match Arena Directly**.
- Restore normal startup: **Tools > Water Polo > Use Boot as Editor Play Entry**.
- Configure the interface: **Tools > Water Polo > Match UX > Set Up Milestone 16 Match Interface**, on the saved OlympicMatchArena outside Play Mode. Existing tuning is preserved on repeat. This is already applied; no Inspector work is required.

Production scene keeps its original GUID. **Assets/Scenes/Recovery/OlympicMatchArena_PreMatchUX.unity** is the exact native pre-pass scene copy, created with AssetDatabase.CopyAsset and excluded from builds. It shares scripts with production; it is a layout recovery, not a source-code rollback. No Git write was performed.

## Authoritative data audit

| Information | Existing owner used by this interface |
| --- | --- |
| Score, phase, period transitions, completion, side switch | PrototypeMatch; Changed / NewMatch |
| Period time and display mapping | PrototypeMatch.Clock / MatchClock |
| Shot time and team possession | PossessionShotClock; advanced only by PrototypeMatch |
| Actual owner, pass/shot/reception, ball flight | BallController; existing possession/release events |
| Human control, eligibility and keeper handoff | PlayerSelection / SwimmerIdentity |
| Movement / input | SwimmerInput writes the original SwimmerMotor; existing Input System actions plus MobileMatchControls |
| Charge / shot power | ShotCharge; existing source-owned hold and release |
| Stamina | SwimmerStamina; ActiveStaminaHUD |
| Fouls, exclusions and restart eligibility | MatchRules / RefereeEvent / SwimmerIdentity |
| Pause | PrototypeMatch.IsPaused, PauseChanged and PresentationTime |
| Official result and rewards | HubMatchBridge -> HubProgression -> existing repository |
| Preferences | Existing GamePreferences PlayerPrefs record, separate from the player profile |

No timeout authority exists in this prototype; no timeout button or simulated timeout event was added. Defaults remain four **80-live-second** periods displaying **480 seconds** each (6×), an independent **28-second** possession clock and **18-live-second** exclusions. These data, execution orders, competitive deadlines and pause velocity preservation were not changed.

Existing in-match UI scripts audited: PrototypeScoreboard, MatchHUD, ActiveStaminaHUD, ShotPowerBar, MatchAtmosphere, MatchMenuController, MatchSettingsView, GamePreferences, MobileMatchControls, GameplayActionButton, VirtualSwimJoystick, SafeAreaLayout and RoundedPanelGraphic. ArenaHudReadability supplied legacy contrast plates; those plates/component are retired only in the upgraded Canvas. BallHeightCue remains the existing world ball cue; ArenaEventDisplay continues observing match state on the six venue faces.

## Delivered interface

**ScoreCard:** compact navy top-center panel with cyan BLUE, white WHITE, score, dedicated quarter and period time, separately labeled shot clock, LIVE / STOPPED / PAUSED / FINAL state and stable possession text. Score changes receive a short 9% scale pulse. Timer text changes only when its displayed integer changes. Shot-clock warnings use amber at ten seconds and red at five, without flashing or another countdown.

**MatchNotification:** one reusable title/context/accent banner observes real score, phase and referee events. GOAL identifies the scoring team and score. Free throw, exclusion, penalty, shot-clock violation, center restart, period end, halftime and full time use the same presentation vocabulary. Strong decisions replace weaker ones; weaker events cannot overwrite an unexpired important decision. There is no growing queue or object stack. Center instructions persist while waiting for the legal pass. Menu overlays suppress the banner immediately; notification expiry follows the pause-aware PresentationTime, while fades use unscaled time. Period/halftime/final menus supply the visible presentation during those stopped phases.

Legacy EventCard, GoalLabel, RestartInstruction, QuickControls and duplicate strategy/status plates remain serialized but inactive in production. MatchAtmosphere still owns existing audio presentation and compact foul/exclusion status; the new banner suppresses only its old visual feed. No referee decision or sound timing was moved into UI.

**Controls:** left thumb has a larger 240-reference-pixel joystick, no drag threshold, the existing analog dead zone and pointer ownership, and reliable recentering on release/cancel/pause/focus loss/disable. SwimmerInput still gives nonzero keyboard/gamepad movement priority over the joystick.

The right thumb has a 176-pixel SHOOT / STEAL primary position and separated PASS, LOB, SPRINT, SWITCH, shot-mode / loose-ball COLLECT and Press/Zone controls. Attacking possession keeps shooting/passing/lob positions visible, with ineligible actions dimmed. Defense shows STEAL in the primary position and retains SWITCH/SPRINT. Eligibility always comes from the existing rules and selected athlete; a visible button does not bypass a restart or keeper restriction. Controls do not move between arbitrary positions during a possession.

SHOOT starts the existing charge on pointer down and releases once on pointer up. Leaving a held action cancels; no Button.onClick shooting path was introduced. Press feedback occurs only after the command accepts the press. Independent pointer IDs support movement plus sprint/shoot. Selection changes, pause, phase changes and application suspension clear holds. The existing shot power bar now sits above SHOOT. The original green ring receives one 0.35-second pulse on control transfer and returns to its authored scale; the compact athlete card identifies field/goalkeeper and shows existing stamina. No new world meshes/materials or selection authority were added.

**Pause:** a dimmed pool, navy menu, prominent Resume, Settings, separate Controls, Restart and Exit to Main Menu. Pause summary reads the actual score/quarter/time. X and Escape provide the corresponding Back action. Restart and abandonment keep the existing confirmation paths. Period/halftime X/Escape continue the authoritative transition once. Full-time X/Escape use the existing reward/lobby path; an isolated match returns to its standalone match menu. The standalone menu also has X/Escape. Completed-match exit wording distinguishes a saved result from abandonment.

Menus request PrototypeMatch actions; they never own timeScale, body velocities, phases or rewards. Existing pause-aware rule deadlines, canceled charge, cleared pointer holds, audio pause and restored timeScale remain. FullTime still submits through HubMatchBridge, including retry/deduplication; restarting binds a fresh session without awarding the abandoned one.

**Settings:** existing master, effects, crowd where supported, camera distance/smoothing and Low/Medium/High graphics remain real controls. Added Small / Normal / Large thumb groups (**0.85 / 1 / 1.12×**) and **40–100%** opacity. These are additive fields in the existing version-1 preference record. They affect presentation only; collection, profile schema, balances and catalog were not changed. No unsupported music slider, fake haptics or HUD-scale framework was added.

CanvasScaler remains height-scaled at 1920×1080. HUD and menu roots use the existing safe-area component. Decorative graphics do not receive raycasts; one Canvas, one EventSystem and InputSystemUIInputModule remain. Panels/labels and meshes are reused during play. No image assets, packages, shaders, characters or stadium assets were generated for this milestone.

## Exact file manifest

Modified:

- Assets/Scenes/OlympicMatchArena.unity
- Assets/Scripts/Gameplay/GamePreferences.cs
- Assets/Scripts/Gameplay/GameplayActionButton.cs
- Assets/Scripts/Gameplay/MatchAtmosphere.cs
- Assets/Scripts/Gameplay/MatchHUD.cs
- Assets/Scripts/Gameplay/MatchMenuController.cs
- Assets/Scripts/Gameplay/MatchSettingsView.cs
- Assets/Scripts/Gameplay/MobileMatchControls.cs
- Assets/Scripts/Gameplay/PrototypeScoreboard.cs
- Assets/Scripts/Gameplay/VirtualSwimJoystick.cs
- plan.md
- README.md

Added:

- Assets/Editor/MatchUxSetup.cs and .meta
- Assets/Scripts/Gameplay/MatchNotificationBanner.cs and .meta
- Assets/Scripts/Gameplay/MatchControlPresentation.cs and .meta
- Assets/Scenes/Recovery/OlympicMatchArena_PreMatchUX.unity and .meta
- Tools/Verification/MatchUxVerification.cs
- docs/MATCH_UX.md

The LiberationSans fallback asset was already modified before this session. Untracked Pipeline settings, SourceAssets, old-plan.md and tatus were pre-existing and were retained. old-plan.md was not edited. All milestone work remains unstaged/uncommitted.

## Fresh verification and evidence

Actual advancing Windows Editor Play Mode, async **unity command script run** entries, real Game-view screenshots, Input System injected devices and focused rule fixtures. No Task objects were returned through synchronous eval. Reports/captures are in ignored **Logs/MatchUX/**. Main source: Tools/Verification/MatchUxVerification.cs; reused checks: Milestone11Verification.MobileButtons and LiveAIPause.

| Check | Result and practical scope |
| --- | --- |
| Compilation | Fresh runtime/editor recompile, no compile errors; verification source compiled without diagnostics. |
| Structure | 3,317 original non-Canvas component records identical; original scene GUID and exact recovery hash; original Boot, MainLobby, PrototypePool, build settings, destination and catalogs unchanged. One Canvas/EventSystem, CanvasRenderer on custom graphics, no missing scripts; setup repeat and scene reload passed. |
| Mobile APIs | Joystick, keyboard priority, sprint stamina/hold/release, charge source ownership, physical pass/reception/control transfer, lob, switch without possession, legal steal, shot mode, pause cancellation and temporary human Blue keeper possession passed. |
| Actual device simulation | Synthetic Touchscreen sent two simultaneous joystick/sprint pointers through InputSystemUIInputModule; both released cleanly. Synthetic gamepad left stick and South charge/release passed. Actual synthetic touch SHOOT showed the power bar, fired once, rejected duplicate release and canceled safely through pause. Keyboard C and original ring pulse/restore plus goalkeeper identity passed. |
| Pause / settings | Actual injected mouse/keyboard exercised Pause, Resume, Controls/X, Settings/X, supported settings application and persistence, restart confirm/cancel, exit cancel and repeated Escape. Clock/shot time stopped; no duplicate UI. Live AI decisions and all swimmer/ball poses stayed fixed during pause and resumed. |
| Events / time | Real physical normal goals for Blue and White, conceding center setup/pass, manual Blue restart, stopped/restarted clocks, real shot-clock expiration, ordinary foul, exclusion priority, penalty setup and paused/resumed physical flight passed. Period ratio and low shot warning checked against the actual authority. |
| Periods / completion | Actual phase APIs advanced all four periods to QuarterBreak / HalfTime / QuarterBreak / FullTime. Correct next quarter, one halftime swap, draw/victory/defeat presentation and X/Escape transitions checked. |
| Normal game loop | Boot -> MainLobby -> actual pointer Division 1 launch -> current arena; restart session replacement/cancel; abandonment without rewards; official win and loss FullTime receipts/results/lobby; bridge replay deduplication and JSON profile reload; Training without rewards. Isolated in-memory profile used the real HubProgression/bridge; real disk profile hash unchanged. |
| Responsive Game views | 18 captures at **1920×1080, 2340×1080 and 2400×1080**: attack, defense, Pause, Settings, Controls and confirmation at each ratio. Largest controls plus simulated **70 px side / 24 px vertical** safe insets. Interactive bounds, nonoverlap and active text overflow checks passed; representative actual images inspected. Additional event, power, result and keeper captures inspected. |

Representative evidence names: Structure-PASS, MobileButtons-Final, LiveAIPause-Run, Menus-Final, Events-PASS, Layouts-PASS, TouchGamepad-PASS, Feedback-PASS, TransitionBack-PASS, HubLoop-PASS and Console-Final. Screens include Layout-1920-Attack, Layout-2400-Defense, Layout-2340-Pause, Blue-Goal, Blue-Center-Restart, Free-Throw, Exclusion, Penalty, Charged-Touch-Shot, Official-Victory, Official-Defeat, Official-Reward-Result and Returned-MainLobby.

Final current Console: **0 errors / 0 warnings**, compilationFailed false. Play is stopped, Boot and the original Game-view selection are restored, and verification focus/background/auto-tick overrides are removed or disabled. Results are recorded in Logs/MatchUX/Console-Final.json and Restored.json. Historical Pipeline buffers contain older Burst/Windows code-integrity and pre-existing analyzer warnings; they are distinct from the current Console. No security settings, caches or packages were changed to suppress them.

Read-only Git inspection found no staged files. Exact final status is in Logs/MatchUX/Git-Final.txt. The whitespace audit reports Unity's native empty serialized YAML values with trailing spaces; native scene serialization was retained. The tracked C# and Markdown portion has no whitespace errors.

## Limits and manual review

This is focused Editor acceptance, not physical phone or final production certification. Touchscreen contacts are synthetic; real thumb comfort, screen cutouts, OS interruption behavior, thermal/GPU/CPU budgets, iOS and Android builds require hardware. No long-duration 320-live-second match was played end to end: period/result checks accelerate the existing authoritative AdvanceLiveTime API without changing saved timing, and action/rule fixtures temporarily disable and restore AI to isolate commands. Live-AI pause and normal-entry integration are separate checks. The old M11 broad PauseAndRules probe did not pass its guaranteed low-speed penalty-goal expectation in the current arena; the final focused penalty checks establish setup/flight suspension and physical resume, not a guaranteed penalty score. Ordinary goals are independently verified with real shots.

No new timeout system, touch aim stick, controller mapping expansion, music system, haptics, automatic period continuation, substitutions or economy implementation was added. Quarter/halftime continue remains an explicit user action, now reachable via Continue/X/Escape. Legacy scenes retain their layout and shared scripts; all verification above targets OlympicMatchArena. Screenshots and assertions are sampled checks, not exhaustive collision/notch coverage or performance measurements.

Manual checklist:

1. Run **Play Hub Entry**, PLAY, Division 1 or Training, Launch. Check Blue/White score, quarter, clock, possession and all fourteen athletes.
2. Swim with WASD and joystick; combine movement with sprint. Collect/pass/lob; hold/release SHOOT and move off it to cancel. Cycle shot modes. Switch with C/SWITCH and check ring, player identity and keeper possession.
3. Defend with STEAL, switch and sprint; inspect foul/exclusion status and the single event banner. Score both ways and manually PASS for a Blue center restart.
4. Pause while swimming, charging, in flight and during a restart. Try Resume, X/Escape, Controls, Settings, restart/exit Cancel and Confirm. Check no held action replays and clocks/AI remain stopped.
5. Review all three landscape sizes and Small/Normal/Large plus opacity. Inspect the power bar, safe-area clearance and all modal Close targets.
6. Continue through halftime and full time; verify final outcome, one result/reward, return to lobby and an abandoned/training session without official rewards. Check Console.

Recommended next milestone, only after separate authorization: hands-on mobile comfort testing and Android profiling, then targeted control sizing/contrast adjustments driven by measured device behavior. Stop after M16.
