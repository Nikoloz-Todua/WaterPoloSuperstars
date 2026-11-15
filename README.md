# Water Polo Superstars

Water Polo Superstars is a 3D mobile water polo game developed by **ToduStudios**, targeting Android first and iOS later. The current build uses placeholder art and a functional fourteen-athlete match.

## Technology and architecture

- Unity 6.6, pinned Editor **6000.6.7f1**, Universal Render Pipeline, C# and 3D physics.
- Unity Input System; shared human/AI action APIs and stamina.
- PrototypeMatch owns transitions; BallController alone owns possession and flight.
- PlayerSelection owns control independently of possession; PrototypeTeamTactics shares team reasoning.
- MatchRules decides fouls and eligibility; MatchAtmosphere observes decisions for HUD/audio.

## Current stage — Milestone 12 offline game hub

Boot -> MainLobby -> Game Modes -> OlympicMatchArena -> results -> lobby. The unified uGUI hub includes a twelve-athlete collection, upgrades, a legal seven-player lineup editor, live Press/Zone selection, fixed-price local shop, four timed free reward slots, twenty free season tiers, profile/color editing and achievements. Functional pages share a navy/cyan shell with X/Back and Escape navigation. Everything implemented uses local progress; no Firebase or monetization SDK is installed.

**MainLobby now uses the developer's 21 PNG assets**: profile/currency top bar, left Ranking/Shop/Team, right Friends/Clubs/World Tournament, bottom Battle Pass/Missions, four actual reward-pack slots and a prominent PLAY button. The supplied pool background remains visible through a clear centre. PNG proportions and source pixels are preserved; pack timers/claims use the existing saved inventory. Missions opens existing achievements; unavailable social/messages/tournament features have Coming Soon dialogs. Open **Tools > Water Polo > Main Lobby > Open MainLobby**; repeat-safe setup is **Set Up Asset Home Screen** in the same submenu. Actual Game views, pointer routes, pack state/claim persistence and match/reward return passed focused Windows Editor verification at 16:9, 19.5:9 and 20:9 with simulated safe areas. Phone touch/rotation/performance still need hardware testing. Details and screenshots: [latest handoff](plan.md).

The production match now runs in **OlympicMatchArena**, built around WaterVisualTest's clear water, deep tiled shell, daylight and underwater body visibility. It adds a configurable 25 × 20 m field, wider tiled deck, team benches, spectator stands and an indoor hall. Fresh Windows Editor checks passed for gameplay integration, real pointer launch/results/lobby return, save/reward deduplication and three match screen sizes. Read the newest [handoff](plan.md) for exact evidence and remaining limits. Collection ratings and cosmetics remain collection/profile presentation data; live stat/cosmetic integration remains deferred.

Use **Tools > Water Polo > Play Hub Entry** or normal Play: Boot → MainLobby → PLAY → Game Modes → Division 1 / Training → OlympicMatchArena → rewards → lobby. Open the saved arena with **Tools > Water Polo > Arena > Open Olympic Match Arena**; use **Play Olympic Match Arena Directly** in the same submenu for isolated testing. **F8** cycles developer inspection views and returns to broadcast. Restore normal Play with **Tools > Water Polo > Use Boot as Editor Play Entry**. Repeat-safe M12 hub setup respects the new destination. PrototypePool and WaterVisualTest remain independent recovery/test scenes outside the production build list. See [arena and water documentation](docs/WATER_VISUALS.md). Android/iOS performance and final athlete art remain unverified.

The six Game Mode cards use the supplied artwork and background. **Division 1 → Premium League → Continental Cup → World Champions League** unlock in order by winning the preceding championship (default: two wins within three official matches). Configuration is in `Assets/Resources/DivisionCatalog.asset`. Training is playable without progression rewards; Lucky Shot, Friends, Clubs, Messages and the separate country-based World Tournament show Coming Soon. Battle Pass uses its purple background, real free claims and a locked premium preview. Setup: **Tools > Water Polo > Main Lobby > Set Up Unified Frontend**. Recovery scene: `Assets/Scenes/Recovery/MainLobby_PreUnifiedFrontend.unity`. Focused Editor checks cover navigation, three landscape ratios, real transactions, schema migration and two controlled arena wins through the result bridge. Evidence and file inventory: [plan.md](plan.md).

Local schema-2 progress lives at the existing `Application.persistentDataPath/profile-v1.json` path, with explicit schema-1 migration, atomic writes/recovery backups and compact IDs. Existing balances, inventory and timers are preserved; loading alone does not rewrite the old file. Preferences remain separate. See [future backend and economy architecture](docs/FUTURE_BACKEND_AND_ECONOMY.md). No internet is required. Local saves/timers are not cheat-proof.

The existing **OlympicMatchArena** now has cobalt/white event decking, neutral ceramic concourses, protected officials' lanes, dressed team areas, stronger grandstand/hall framing, overhead lighting trusses and original championship signage. An end-hung jumbotron and two poolside boards display the real match score/clocks. Current players/fans, clear water, gameplay/HUD/camera and menu flow are preserved. Use **Tools > Water Polo > Arena > Polish Existing Championship Venue** for repeat-safe setup, or **Refresh Venue Reflection** after lighting edits. Focused Game-view, controls/scoring and existing entry/return checks passed; detailed art and device performance remain future work. See [current arena handoff and screenshots](plan.md).

## Milestone 14 — rigged athlete and interactive water prototype

All 14 existing OlympicMatchArena athletes now use a reusable **66-bone Humanoid mannequin** with treading, alternating head-up swimming, sprinting, possession and event-driven pass/shoot/block/save presentation. The Quaternius/KinemIQ CC0 sources, reduced FBX model, cap/swimwear, eight prototype clips and licenses are included. Original visual objects remain as fallback. Gameplay controllers, physics, ball releases, arena, transparent-water shader, HUD and hub flow are preserved.

Gentle idle rings, animated hand-entry splashes, sprint wakes, short pooled bubbles and concentric ball-impact ripples use bounded quality/camera-aware effects. **Tools > Water Polo > Animated Athletes > Play Isolated Swimmer Lab** previews states with keys 1–8. Normal matches use the saved production arena. Focused actual Game-view/input/pass/scoring checks passed first on one athlete and then on all 14; phone performance and final biomechanical animation remain unverified. See [animation guide, licenses, exact commands and limitations](docs/SWIMMER_ANIMATION.md) and [current handoff](plan.md).

## Existing match foundation

`Assets/Scenes/PrototypePool.unity` retains **six field players and one goalkeeper per team: 14 athletes**. Milestone 10 adds contextual C switching, stable defensive marking, attacking improvements, contact-driven fouls, protected free throws, temporary exclusions, personal foul limits, qualifying penalties, referee messages and original procedural audio cues.

The developer reports functional manual Milestone 9 gameplay. Milestone 10 requires manual review; automated evidence, the file inventory and limitations are in [plan.md](plan.md). Device performance and final game feel remain unverified.

The current uncommitted expansion adds genuine normal/power/high/skip trajectories, water impact and rebound handling, bounded net motion, moving-receiver prediction and height-aware keeper contact. The existing camera gains adaptive broadcast profiles. Two primitive referees walk along observation decks and signal authoritative rule events; pooled ripples, splashes, wakes and shot trails provide lightweight feedback. That original expansion was verified in the Windows Editor without imported characters; Milestone 14 adds the separate rigged visual pipeline described above.

Milestone 11 adds a uGUI pause/settings interface, compact match HUD, confirmation dialogs and an in-scene prototype menu. Its corrective update applies the scene setup, adds functional on-screen swimming/action controls, and introduces protected physical centre-pass restarts after goals. Pause preserves the underlying phase; local audio, camera and graphics preferences survive match resets. See the current [handoff and verification status](plan.md) before testing.

## Open and play

For the full game, use **Tools > Water Polo > Play Hub Entry**. To isolate the production match, use **Tools > Water Polo > Arena > Play Olympic Match Arena Directly**. The older **Play PrototypePool Directly** and Milestone 11 interface setup remain for legacy prototype inspection, not primary match entry.

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
| Hold Left Shift | Sprint while swimming; consumes shared stamina |
| L | Physical aerial lob pass |
| Z | Toggle Blue Press / Zone defense |

Shot modifiers latch when charging starts; Q takes priority if both modifiers are held. High/skip shots use actual Y height and physical collisions. Ordinary B passes skim the water; L has a configurable real arc. A fast shot cannot be collected remotely or above a swimmer's receiving height.

On screen, drag the left joystick to swim. Keyboard movement takes priority while a movement key is held. Hold **SPRINT** while moving; hold/release **SHOOT** to charge/fire. **PASS**, **LOB**, **SWITCH**, **STEAL** and **COLLECT** call the same gameplay commands as the keyboard. The compact **NORMAL / SKIP / HIGH** button cycles shot modes; **DEFENSE: PRESS / ZONE** changes strategy. Actions adapt to possession and restart eligibility. Releasing outside a held action cancels it. Pause clears pointer holds so they cannot replay on resume.

After a goal, both teams reset to their own halves. The conceding team's designated field taker receives the ball at centre. After the referee signal, **Blue waits indefinitely for B or PASS**; there is no timed human fallback. White makes a physical centre pass automatically after a short preparation delay. Movement, shooting, switching and stealing remain protected until release. **PASS TO RESTART** identifies the human action. Match/shot clocks begin when the real pass is released. Period starts retain their separate neutral-ball procedure, and this flow uses each team's current goals after halftime.

On the camera's **Gameplay Camera** component, Mode selects Normal Broadcast or Wide Tactical; keeper/set-piece profiles activate contextually. The **GameplayRealism** object contains the deck referees and Water Interaction Effects quality settings (**Off / Low / Standard**). Existing score, clocks, defense, stamina and shot-power HUD remain.

Legitimate friendly possession still selects its carrier, including a Blue goalkeeper catch. C never transfers possession. The selection component's **Contextual Switching** option preserves roster-order switching as a configurable fallback. Goalkeeper selection retains its possession-only policy.

Free throws have a short protected setup, then **B/L** puts the ball back into play. Direct restart shots are disabled in this simplified profile. AI takes its own restarts; an idle human restart also has a bounded automatic fallback. Penalties use aiming and charged Space shooting from 5m, with a physical goalkeeper save opportunity.

Defaults remain four periods displaying **8:00 over 80 live seconds (6×)** and an independent **28-second possession clock**. Exclusions default to **18 live seconds**, paced independently of the accelerated period display. Three personal fouls permanently remove the athlete; there is no substitute bench. The HUD shows selected-player fouls, reduced field counts, excluded cap numbers/time and referee messages. These mechanics are a documented prototype profile, not a complete current-rule implementation.

Click **Continue Match** at period breaks. Continuing after Q2 swaps team goals, goalkeepers and formations once; new matches restore the original ends. All clocks freeze during stopped free-throw/penalty and centre-setup phases; ordinary free throws retain shot time. Pause cancels a pending human charge safely. Restart and Exit ask for confirmation; Exit returns to the prototype menu. Settings includes the controls reference. See the handoff for exclusion/penalty reset details.

## Documentation and source control

- [plan.md](plan.md): canonical architecture, rules sources, verification, manual steps, Burst investigation and limitations.
- [AGENTS.md](AGENTS.md): permanent development rules, including human-only Git writes.
- `Tools/Verification/`: Editor-driven checks outside player builds.

Only the human stages, commits or pushes. Preserve Unity `.meta` files. Generated folders, credentials, local legacy references, `old-plan.md` and unrelated files are excluded from proposed backups. Stop after Milestone 12; later work needs separate authorization.
