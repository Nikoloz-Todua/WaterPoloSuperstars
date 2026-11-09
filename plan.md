# Water Polo Superstars — Project Plan and Handoff

Last updated: 2026-11-09.

This document describes the NEW 3D project. Distinguish confirmed implementation from goals and user-reported setup. Update after meaningful development tasks with changes, tests, issues, and next steps. Do not import the previous game's historical logs.

## Identity and vision

- Game: Water Polo Superstars. Studio: ToduStudios. Unity organization: TODU.
- Studio email: todustudios@gmail.com. Game email: waterpolosuperstars@gmail.com.
- Genre: 3D mobile water polo sports simulation.
- Primary release: Android / Google Play. Secondary: iOS / App Store.
- Solo developer assisted by ChatGPT, Codex, Unity AI, and Blender.
- Presentation target: attractive normal gameplay inspired by Dream League Soccer, within realistic solo-development scope and mobile performance limits.
- Long-term goals: believable athletes, swimming/passing/shooting/blocking/goalkeeper animations, water and splashes, arena, responsive controls, tactical AI, cameras, cinematic goals/replays, career, championships, World Cup, transfers, cards, progression, and monetization. These are ambitions, not completed systems.

Build the new game around its own requirements. The old game is an optional reference, not the foundation or a mandatory migration target. Pool-plane tactical calculations with 3D presentation are an option to evaluate, not a binding architecture decision.

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
- Do not initialize another repository inside this one. Preserve Unity `.meta` files, avoid unrelated edits, and create Git checkpoints before large/risky work.
- Normal workflow: inspect status/diff, stage intended changes, commit stable work, push when authorized. Never upload generated caches.

Setup-time versions reported: Git `2.56.0.windows.4`, Node.js `v26.13.0`, npm `11.20.0`, Codex CLI `0.166.0`, Unity CLI `1.0.0-beta.15`. These are historical values, not guaranteed current installations.

Codex was authenticated with the developer's ChatGPT account. `unity setup codex` installed the Unity plugin, reporting `0.1.8-beta` at setup; installed plugin/skills may change later. Launch Codex from this project folder with `codex`, or `codex.cmd` if PowerShell blocks the `.ps1` launcher. External Codex and Unity AI services are separate.

## Current implementation and verification

Milestones 1-4 are implemented in `Assets/Scenes/PrototypePool.unity` and manually verified by the developer. Milestone 5 is implemented and automatically verified in Unity; developer manual testing is pending. The separate SampleScene remains unchanged. No old gameplay source was imported.

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

Developer manual Milestone 5 playtesting is pending. Camera feel, AI positioning quality, keyboard/gamepad feel, different aspect ratios, mobile touch UI and Android/iOS builds/device performance are not verified by these automated checks. Simple spacing is not a full collision/avoidance system; kinematic swimmers can overlap. No tactical brain/formations, full teams, keeper, clock, steals, fouls, advanced animations/water or economy were added.

Manual test: open `Assets/Scenes/PrototypePool.unity`, Play and click the Game view. E collects with the yellow cap; face the white-cap teammate and B to pass. Selection/ring/camera must remain with the passer during travel, then transfer on reception; WASD must move the receiver. Face/pass back and verify automatic return selection. Try C while carrying: ownership must stay with the previous carrier. Watch the inactive friendly support the carrier and the red-cap opponent defend. Shoot/miss and let an eligible inactive friendly approach a loose ball; acquisition should select that friendly. The red cap may collect but must never become selected. Score at either goal and verify all three reset and another pickup/pass/shot works. Check `Window > General > Console` for errors/warnings.

No manual Inspector wiring is required. The final setup menu is optional because the scene is already saved. To run it, exit Play Mode, save any intended scene edits, then choose `Tools > Water Polo > Set Up Automatic Switching and AI Prototype`. Inspector tuning: each actor's Prototype Swimmer AI for support/pressure/range/speed; `Gameplay/PlayerSelection` for Human Team; `CameraRig/Main Camera > Gameplay Camera` for framing and `Camera` for FOV. Keep Human Team Blue for this two-friendly prototype.

Milestone 5 remains uncommitted for developer review. Recommended next task: manually validate automatic switching, support/pressure and camera framing; record feedback and tune this milestone before authorizing more gameplay. Android input/device validation remains an important later milestone.

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
- After meaningful work, document behavior/files changed, confirmed decisions, actual tests, incomplete work, bugs, and the next step before committing.
- Documentation instructions do not guarantee updates after a session ends; include updates explicitly in task completion.

## Roadmap

1. Foundation inspection: completed; saved configuration examined, with live/device verification limits stated above.
2. Milestone 1 movement sandbox: implemented, exercised in automated Editor Play Mode checks, and manually verified by the developer; camera needs closer framing.
3. Milestone 2: following gameplay camera, possession, carrying and shooting one ball. Implemented, automatically verified and manually confirmed by the developer.
4. Milestone 3 goals/scoring/restart: implemented, automatically verified and manually confirmed by the developer.
5. Milestone 4 two swimmers/selection/passing/wider framing: implemented, automatically verified and manually confirmed by the developer.
6. Milestone 5 possession switching, basic friendly/opponent AI and wider camera: implemented and automatically verified; awaiting manual testing and feedback.
7. Proposed follow-ups after approval: tune controls/camera/AI from feedback, landscape touch controls and Android device validation, then small AI/rules increments.
8. Improve animation, water, lighting, feedback and performance through measured iterations.
9. Request old card/pack/roster examples when useful and choose the new data/UI architecture.
10. Expand competitions, progression, backend and monetization after the playable foundation is validated.

Milestone 5 is implemented and must remain uncommitted pending developer review. Local checkpoints: Milestone 1 `a8312f6`, Milestone 2 `2ca3909`, Milestone 3 `0e43094`, Milestone 4 `4babe88`. No push is authorized. `old-plan.md` is an untracked legacy reference preserved and excluded from commits; it is not the governing plan for this game.
