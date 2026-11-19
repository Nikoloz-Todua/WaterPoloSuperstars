# Water Polo Superstars

Water Polo Superstars is a 3D mobile water polo game developed by **ToduStudios**, targeting Android first and iOS later. The current build uses placeholder art and a functional fourteen-athlete match.

## Current stage — ball physics and boundary restarts

Ordinary drops, missed passes and shots settle into a slower float. Intentional skip shots still bounce. Whole-ball exits now bring up a throw-in, goal throw or corner in OlympicMatchArena, and the replacement is played from the restart spot. [Rules and Editor verification](docs/BALL_PHYSICS_AND_BOUNDARY_RESTARTS.md).

## Previous stage — pre-match presentation

OlympicMatchArena now opens with a short arena overview, two-team walkout, captain pennant exchange, starting lineups and deck huddles. Tap to advance or choose **Skip Intro** to go straight to the opening sprint.

Preview with **Tools > Water Polo > Play Olympic Match Arena**, or launch through **Tools > Water Polo > Play Hub Entry**. [Controls, implementation and Game View evidence](docs/PRE_MATCH_PRESENTATION.md).

## Previous stage — match reliability and goal presentation

WASD and arrow keys share match movement in both cameras. Own goals credit the opposing team and show the responsible athlete's regret. The goal-line replay angle follows the recorded crossing into the net.

For an exclusion demonstration, choose **Tools > Water Polo > Verification > Exclusion Test** and select a scenario. [Milestone 24 controls, game views and Editor verification](docs/MATCH_TECHNICAL_POLISH.md).

## Previous stage — dry passing and goal replays

Pass through the air to a teammate's raised hand, or hold the touch PASS button to place the ball on the water. Charged, high and skip shots share the arena's physical goalkeeper and goal system. Goals trigger a short celebration and two camera angles of the recorded play. Tap to advance or use **Skip All** to return to the centre restart.

Launch with **Tools > Water Polo > Play Hub Entry** or test the arena with **Tools > Water Polo > Play Olympic Match Arena**. [Controls, implementation and Editor verification](docs/BALL_PHYSICS_AND_GOAL_REPLAYS.md).

## Previous stage — championship arena premium finish

The existing **OlympicMatchArena** now reads as one long **50 × 25 m architectural pool** around the preserved **25 × 20 m gameplay field**. Continuous measured competition ropes, underwater black T markings, reinforced white goals and compact re-entry landings replace the broken-line/partitioned-pool appearance. The three-tier bowl, 22 correctly facing team chairs, clear bench access, player-entry portal, blue LED ribbons and live scoreboards remain integrated with the existing match.

Already saved; repeat-safe finish: **Tools > Water Polo > Arena > Apply Premium Championship Finish**, outside Play Mode. Both cameras, broad opening, fourteen athletes, AI, controls/HUD, scoring, sprint-offs, exclusions and Boot/hub/results are preserved. Fresh Windows Editor physical/input/pointer/camera/hub checks passed; full-duration play and mobile performance remain untested. [Report, measured compromise, screenshots, verification and exact files](docs/CHAMPIONSHIP_ARENA_PREMIUM.md). No Git writes were performed.

## Previous stage — championship poolside arena redevelopment

The existing **25 × 20 m** competition field already had the correct dimensions; its 25.6 × 20 m rope envelope and larger basin are preserved. The saved OlympicMatchArena now has sloped pool-edge architecture and drainage, clearer full-width referee colors, white armless chairs on end-corner team risers, equipment areas, layered original event boards, passive broadcast platforms, stand-access detailing and a ceiling-centred live jumbotron. Both gameplay cameras, the wide sprint opening, fourteen athletes, transparent water, controls/HUD and match/economy authorities remain.

Already applied: **Tools > Water Polo > Arena > Apply Championship Poolside Redevelopment**. Launch with **Tools > Water Polo > Play Hub Entry** or **Play Olympic Match Arena**. Field markings follow World Aquatics: red 2 m/red 5 m/yellow 6 m side markers, with no cross-field distance ropes. [Implementation, rule corrections, before/after Game views and verification](docs/ARENA_REDEVELOPMENT.md). Mobile hardware/performance and full-duration play remain untested; the proposed four-button UI is deferred.

## Previous stage — Milestone 19 live scoreboard and broadcast cameras

The existing arena now has a large physical LED scoreboard showing actual scores, clocks, period and temporary exclusions. One observer also keeps six existing jumbotron/deck faces synchronized. Two playable views use one gameplay Camera: preserved **Lengthwise** and elevated **Sideline Broadcast**. Toggle **F6** or use **Pause → Settings → Camera View**; the preference persists. Keyboard, gamepad and joystick movement follow the visible camera axes. The original period-opening overview and F8 inspection remain.

Focused Windows Editor checks passed for both views, goals, exclusions/re-entry, controls, four sprint starts, halftime, pause, restart/exit, three landscape ratios and the Boot/lobby/result/reward loop. Period endings were accelerated; real phones and performance remain untested. Replay pose/state foundations are small and read-only; **goal replay playback is not implemented**. [M19 guide, exact files, screenshots and manual checklist](docs/MATCH_BROADCAST_AND_CAMERAS.md). Launch with **Tools > Water Polo > Play Hub Entry** or **Play Olympic Match Arena**. Setup is already saved.

## Previous stage - Milestone 18 sprint effort and lighting

Deterministic central-lane sprinters, contest switching lock, a real effort meter, smoother opening jaws/cable retraction, clearer colored referee platforms and refined championship lighting are installed in the existing arena. The wide opening camera, swimmers, clear water, normal restarts and rewards remain. Focused Editor/input/quality/period/hub checks passed; physical phones, performance and full-duration play remain untested. [M18 guide and before/after views](docs/SPRINT_POLISH.md). M19 camera/live-display work has subsequently been authorized.

## Previous stage — Milestone 17 field equipment and period sprint-offs

The existing OlympicMatchArena now has measured competition ropes and markers, rectangular white goals with enclosing nets, elevated referee lanes and a genuine race for the center ball at the beginning of each period. Repeated **Left Shift / touchscreen SPRINT** adds capped acceleration; holding remains supported. The AI races using the same motor/stamina. The original ball drops from a retracting cradle, then normal pickup starts play immediately. Goals retain the conceding-team center pass, and Q3 uses the existing halftime end switch. The giant center circle is hidden; Championship Night, fourteen rigs, transparent water, M16 HUD and rewards remain.

Already configured and saved. **Tools > Water Polo > Play Hub Entry** starts Boot → MainLobby; **Tools > Water Polo > Play Olympic Match Arena** isolates the current match. The cleaned Arena, Match UX, Animated Athletes, Verification, Recovery and Legacy Prototype Tools submenus retain old utilities safely. Repeat-safe setup: **Tools > Water Polo > Arena > Set Up Authentic Field and Period Sprint-Offs** outside Play Mode. Native recovery: `Assets/Scenes/Recovery/OlympicMatchArena_PreAuthenticField.unity`.

Focused Windows Editor checks passed for physical sprint pickups in all four periods, human/AI wins, synthetic touch and keyboard, goal contacts/scoring, halftime, pause/restart/exit, three landscape ratios and Boot/lobby/result/reward deduplication. Period endings were accelerated; no full-duration or physical-phone test is claimed. See [M17 architecture, evidence, limitations and checklist](docs/FIELD_AND_SPRINT.md), [exact files](docs/MILESTONE_17_FILES.txt) and [latest handoff](plan.md).

## Milestone 16 match UX

The existing OlympicMatchArena now has a broadcast scoreboard with separate period/shot clocks, one prioritized event banner, larger contextual thumb controls, shot-power and selection feedback, and a polished pause menu with Controls, real settings and restart/exit confirmations. Control size and opacity persist locally. Gameplay, Championship Night art, all fourteen rigged swimmers, clock mappings, progression and rewards are preserved. Focused Windows Editor checks covered real pointer/touch simulation, gameplay commands, pause, three landscape ratios and Boot/lobby/match/result return with reward deduplication. These are Editor and accelerated-transition checks; physical phones, mobile performance and a full-duration playthrough remain untested. See [M16 guide, exact files and manual checklist](docs/MATCH_UX.md) and [latest handoff](plan.md).

Already configured in the saved arena. Repeat-safe command: **Tools > Water Polo > Match UX > Set Up Milestone 16 Match Interface** outside Play Mode. Play normally with **Tools > Water Polo > Play Hub Entry**, or isolate with **Tools > Water Polo > Play Olympic Match Arena**. The native pre-pass scene copy is `Assets/Scenes/Recovery/OlympicMatchArena_PreMatchUX.unity`.

## Technology and architecture

- Unity 6.6, pinned Editor **6000.6.7f1**, Universal Render Pipeline, C# and 3D physics.
- Unity Input System; shared human/AI action APIs and stamina.
- PrototypeMatch owns transitions; BallController alone owns possession and flight.
- PlayerSelection owns control independently of possession; PrototypeTeamTactics shares team reasoning.
- MatchRules decides fouls and eligibility; MatchAtmosphere observes decisions for HUD/audio.

## Offline game hub — Milestone 12 foundation

Boot -> MainLobby -> Game Modes -> OlympicMatchArena -> results -> lobby. The unified uGUI hub includes a twelve-athlete collection, upgrades, a legal seven-player lineup editor, live Press/Zone selection, fixed-price local shop, four timed free reward slots, twenty free season tiers, profile/color editing and achievements. Functional pages share a navy/cyan shell with X/Back and Escape navigation. Everything implemented uses local progress; no Firebase or monetization SDK is installed.

**MainLobby now uses the developer's 21 PNG assets**: profile/currency top bar, left Ranking/Shop/Team, right Friends/Clubs/World Tournament, bottom Battle Pass/Missions, four actual reward-pack slots and a prominent PLAY button. The supplied pool background remains visible through a clear centre. PNG proportions and source pixels are preserved; pack timers/claims use the existing saved inventory. Missions opens existing achievements; unavailable social/messages/tournament features have Coming Soon dialogs. Open **Tools > Water Polo > Open Main Lobby**; repeat-safe setup is **Tools > Water Polo > Recovery > Frontend > Set Up Asset Home Screen**. Actual Game views, pointer routes, pack state/claim persistence and match/reward return passed focused Windows Editor verification at 16:9, 19.5:9 and 20:9 with simulated safe areas. Phone touch/rotation/performance still need hardware testing. Details and screenshots: [latest handoff](plan.md).

The production match now runs in **OlympicMatchArena**, built around WaterVisualTest's clear water, deep tiled shell, daylight and underwater body visibility. It adds a configurable 25 × 20 m field, wider tiled deck, team benches, spectator stands and an indoor hall. Fresh Windows Editor checks passed for gameplay integration, real pointer launch/results/lobby return, save/reward deduplication and three match screen sizes. Read the newest [handoff](plan.md) for exact evidence and remaining limits. Collection ratings and cosmetics remain collection/profile presentation data; live stat/cosmetic integration remains deferred.

Use **Tools > Water Polo > Play Hub Entry** or normal Play: Boot → MainLobby → PLAY → Game Modes → Division 1 / Training → OlympicMatchArena → rewards → lobby. Open the saved arena with **Tools > Water Polo > Arena > Open Olympic Match Arena**; use **Tools > Water Polo > Play Olympic Match Arena** for isolated testing. **F8** cycles developer inspection views and returns to broadcast. Restore normal Play with **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**. Repeat-safe M12 hub setup respects the new destination. PrototypePool and WaterVisualTest remain independent recovery/test scenes outside the production build list. See [arena and water documentation](docs/WATER_VISUALS.md). Android/iOS performance and final athlete art remain unverified.

The six Game Mode cards use the supplied artwork and background. **Division 1 → Premium League → Continental Cup → World Champions League** unlock in order by winning the preceding championship (default: two wins within three official matches). Configuration is in `Assets/Resources/DivisionCatalog.asset`. Training is playable without progression rewards; Lucky Shot, Friends, Clubs, Messages and the separate country-based World Tournament show Coming Soon. Battle Pass uses its purple background, real free claims and a locked premium preview. Setup: **Tools > Water Polo > Recovery > Frontend > Set Up Unified Frontend**. Recovery scene: `Assets/Scenes/Recovery/MainLobby_PreUnifiedFrontend.unity`. Focused Editor checks cover navigation, three landscape ratios, real transactions, schema migration and two controlled arena wins through the result bridge. Evidence and file inventory: [plan.md](plan.md).

Local schema-2 progress lives at the existing `Application.persistentDataPath/profile-v1.json` path, with explicit schema-1 migration, atomic writes/recovery backups and compact IDs. Existing balances, inventory and timers are preserved; loading alone does not rewrite the old file. Preferences remain separate. See [future backend and economy architecture](docs/FUTURE_BACKEND_AND_ECONOMY.md). No internet is required. Local saves/timers are not cheat-proof.

The existing **OlympicMatchArena** now uses a darker **Championship Night** presentation: navy/metal architecture, bright competition water, focused lighting, textured event decking, protected officials' lanes, rebuilt padded team/seating areas, equipment, sideline boards and a larger live jumbotron. The 14 rigged swimmers, current crowd, transparent water, gameplay/HUD/camera flow and hub entry are retained. **Tools > Water Polo > Recovery > Arena > Apply Championship Night Overhaul** is repeat-safe; **Refresh Championship Night Reflection** rebakes after lighting edits. Focused actual Game views, controls/pass/scoring, presets, sightlines and real PLAY/Training/return passed; device performance and final authored art remain future work. See [arena guide and limits](docs/PREMIUM_ARENA.md) and [current handoff](plan.md).

## Milestone 14 — rigged athlete and interactive water prototype

All 14 existing OlympicMatchArena athletes now use a reusable **66-bone Humanoid mannequin** with treading, alternating head-up swimming, sprinting, possession and event-driven pass/shoot/block/save presentation. The Quaternius/KinemIQ CC0 sources, reduced FBX model, cap/swimwear, eight prototype clips and licenses are included. Original visual objects remain as fallback. Gameplay controllers, physics, ball releases, arena, transparent-water shader, HUD and hub flow are preserved.

Gentle idle rings, animated hand-entry splashes, sprint wakes, short pooled bubbles and concentric ball-impact ripples use bounded quality/camera-aware effects. **Tools > Water Polo > Animated Athletes > Play Isolated Swimmer Lab** previews states with keys 1–8. Normal matches use the saved production arena. Focused actual Game-view/input/pass/scoring checks passed first on one athlete and then on all 14; phone performance and final biomechanical animation remain unverified. See [animation guide, licenses, exact commands and limitations](docs/SWIMMER_ANIMATION.md) and [current handoff](plan.md).

## Existing match foundation

`Assets/Scenes/PrototypePool.unity` retains **six field players and one goalkeeper per team: 14 athletes**. Milestone 10 adds contextual C switching, stable defensive marking, attacking improvements, contact-driven fouls, protected free throws, temporary exclusions, personal foul limits, qualifying penalties, referee messages and original procedural audio cues.

The developer reports functional manual Milestone 9 gameplay. Milestone 10 requires manual review; automated evidence, the file inventory and limitations are in [plan.md](plan.md). Device performance and final game feel remain unverified.

The current uncommitted expansion adds genuine normal/power/high/skip trajectories, water impact and rebound handling, bounded net motion, moving-receiver prediction and height-aware keeper contact. The existing camera gains adaptive broadcast profiles. Two primitive referees walk along observation decks and signal authoritative rule events; pooled ripples, splashes, wakes and shot trails provide lightweight feedback. That original expansion was verified in the Windows Editor without imported characters; Milestone 14 adds the separate rigged visual pipeline described above.

Milestone 11 adds a uGUI pause/settings interface, compact match HUD, confirmation dialogs and an in-scene prototype menu. Its corrective update applies the scene setup, adds functional on-screen swimming/action controls, and introduces protected physical centre-pass restarts after goals. Pause preserves the underlying phase; local audio, camera and graphics preferences survive match resets. See the current [handoff and verification status](plan.md) before testing.

## Open and play

For the full game, use **Tools > Water Polo > Play Hub Entry**. To isolate the production match, use **Tools > Water Polo > Play Olympic Match Arena**. The older **Play PrototypePool Directly** and Milestone 11 interface setup remain under **Legacy Prototype Tools** for prototype inspection.

| Control | Action |
| --- | --- |
| Escape / top-right Pause | Pause or resume; Back cancels settings/confirmations |
| WASD / arrows | Swim; turn in place while taking a protected restart |
| E | Collect a nearby loose ball |
| B | Flat pass |
| Tap / hold / release Space | Normal / charged powerful shot; also used for penalties |
| Hold Q before Space | Charge/release a skip shot |
| Hold R before Space | Charge/release a high shot |
| C | Select a tactically useful eligible Blue field player |
| F | Attempt a close-range steal; a rear reach through the carrier's body can be an illegal push |
| G | Intentional close hold: off-ball holds cause exclusions; qualifying shooting fouls cause penalties |
| Hold Left Shift; repeat presses during period sprint | Sprint with shared stamina; capped acceleration strokes during opening race |
| L | Physical aerial lob pass |
| Z | Toggle Blue Press / Zone defense |

Shot modifiers latch when charging starts; Q takes priority if both modifiers are held. High/skip shots use actual Y height and physical collisions. Ordinary B passes skim the water; L has a configurable real arc. A fast shot cannot be collected remotely or above a swimmer's receiving height.

On screen, drag the left joystick to swim. Keyboard movement takes priority while a movement key is held. Hold **SPRINT** while moving; hold/release **SHOOT** to charge/fire. **PASS**, **LOB**, **SWITCH**, **STEAL** and **COLLECT** call the same gameplay commands as the keyboard. The compact **NORMAL / SKIP / HIGH** button cycles shot modes; **DEFENSE: PRESS / ZONE** changes strategy. Actions adapt to possession and restart eligibility. Releasing outside a held action cancels it. Pause clears pointer holds so they cannot replay on resume.

After a goal, both teams reset to their own halves. The conceding team's designated field taker receives the ball at centre. After the referee signal, **Blue waits indefinitely for B or PASS**; there is no timed human fallback. White makes a physical centre pass automatically after a short preparation delay. Movement, shooting, switching and stealing remain protected until release. **PASS TO RESTART** identifies the human action. Match/shot clocks begin when the real pass is released. Period starts use the separate competitive sprint-off; both procedures use each team's current goals after halftime.

On the camera's **Gameplay Camera** component, Mode selects Normal Broadcast or Wide Tactical; keeper/set-piece profiles activate contextually. The **GameplayRealism** object contains the deck referees and Water Interaction Effects quality settings (**Off / Low / Standard**). Existing score, clocks, defense, stamina and shot-power HUD remain.

Legitimate friendly possession still selects its carrier, including a Blue goalkeeper catch. C never transfers possession. The selection component's **Contextual Switching** option preserves roster-order switching as a configurable fallback. Goalkeeper selection retains its possession-only policy.

Free throws have a short protected setup, then **B/L** puts the ball back into play. Direct restart shots are disabled in this simplified profile. AI takes its own restarts; an idle human restart also has a bounded automatic fallback. Penalties use aiming and charged Space shooting from 5m, with a physical goalkeeper save opportunity.

Defaults remain four periods displaying **8:00 over 80 live seconds (6×)** and an independent **28-second possession clock**. Exclusions default to **18 live seconds**, paced independently of the accelerated period display. Three personal fouls permanently remove the athlete; there is no substitute bench. The HUD shows selected-player fouls, reduced field counts, excluded cap numbers/time and referee messages. These mechanics are a documented prototype profile, not a complete current-rule implementation.

Click **Continue Match** at period breaks. Continuing after Q2 swaps team goals, goalkeepers and formations once; new matches restore the original ends. All clocks freeze during stopped free-throw/penalty and centre-setup phases; ordinary free throws retain shot time. Pause cancels a pending human charge safely. Restart and Exit ask for confirmation; hub Exit returns to MainLobby without abandoned-match rewards, while isolated play returns to the prototype menu. Pause has separate Controls and Settings pages. See the handoff for exclusion/penalty reset details.

## Documentation and source control

- [plan.md](plan.md): canonical architecture, rules sources, verification, manual steps, Burst investigation and limitations.
- [AGENTS.md](AGENTS.md): permanent development rules, including human-only Git writes.
- `Tools/Verification/`: Editor-driven checks outside player builds.

Only the human stages, commits or pushes. Preserve Unity `.meta` files. Generated folders, credentials, local legacy references, `old-plan.md` and unrelated files are excluded from proposed backups. Stop after the currently authorized milestone; later work needs separate authorization.
