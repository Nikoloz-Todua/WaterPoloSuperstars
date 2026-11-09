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

Milestone 1 (Movement Sandbox) is implemented in `Assets/Scenes/PrototypePool.unity`. The separate SampleScene remains unchanged. No old gameplay source was imported.

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

The fixed elevated perspective camera frames the full pool. Water is an opaque blue unlit placeholder, with a separate floor beneath it; no fluid simulation or water effects. Temporary prototype palette: blue pool, pale edges, coral swimmer and yellow cap. These are readability choices, not final art direction. Detailed animation, Cinemachine, audio, menus, online services and visual polish are outside this milestone.

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

### Use and current limitations

Open `Assets/Scenes/PrototypePool.unity` from the Project panel, press Play, click the Game view and use WASD/arrows or a gamepad left stick. Existing scene creation is already complete; the menu command is optional and preserves existing work. Save any dirty scenes first.

Inspector tuning: `Gameplay/PoolBounds` controls Width, Length and Waterline; `Actors/Swimmer > Swimmer Motor` controls speed, acceleration, deceleration, turn speed and clearance. Changing playable bounds does not resize the decorative pool or reframe the fixed camera. Keep clearance appropriate to any replacement collider/visual size. The builder deliberately does not regenerate edited scenes.

Known limitations: placeholder presentation; no touch controls, ball, goals/scoring, AI, sprint/stamina, or animations. Project orientation still allows portrait rotation; landscape configuration belongs to a later approved mobile-control task.

Milestone 1 has passed the developer's manual movement review. Milestone 2 is authorized: closer following gameplay camera and basic ball pickup/carry/flat shooting. First create a local Milestone 1 Git checkpoint excluding legacy references and generated files; do not push.

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
3. Milestone 2: following gameplay camera, possession, carrying and shooting one ball. Authorized; implementation follows the local Milestone 1 checkpoint.
4. Proposed follow-ups: goal detection/reset, landscape touch controls and Android device validation, then small AI/rules increments.
5. Improve animation, water, lighting, feedback and performance through measured iterations.
6. Request old card/pack/roster examples when useful and choose the new data/UI architecture.
7. Expand competitions, progression, backend and monetization after the playable foundation is validated.

Milestone 2 is the current authorized scope. A local Milestone 1 checkpoint is authorized; no push is authorized. `old-plan.md` is an untracked legacy reference to preserve and exclude from commits; it is not the governing plan for this game.
