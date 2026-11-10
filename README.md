# Water Polo Superstars

Water Polo Superstars is a 3D mobile water polo game developed by **ToduStudios**, targeting Android first and iOS later. The long-term goal is responsive sports gameplay with professional mobile presentation; the current build is a functional prototype with placeholder art.

## Technology and architecture

- Unity 6.6, Editor **6000.6.7f1**.
- Universal Render Pipeline (URP), C#, 3D physics and the Unity Input System.
- XZ pool-plane tactics with separate 3D movement/presentation components.
- Shared tactical reasoning for both teams, one authoritative ball-possession system, and independent human selection.
- Useful concepts from the previous Water Polo Manager game are references; its 2D physics implementation is not imported.

## Current stage — Milestone 9

`Assets/Scenes/PrototypePool.unity` contains **six field players and one goalkeeper per team: 14 athletes**. The complete Milestone 9 prototype adds six tactical roles, coordinated Press/Zone defense, four compressed periods, a possession shot clock and turnovers, shared sprint/stamina, aerial lob passes and match-flow HUD/panels. Existing movement, passing, stealing, charged shots, temporary human Blue goalkeeper possession, saves and scoring remain.

The developer manually tested Milestone 8 and backed it up as `734d8da`. Milestone 9 awaits developer manual gameplay testing; see the canonical handoff for actual automated checks and limitations. Mobile builds, device performance and final game feel remain unverified.

Goalkeepers share the existing ball ownership and passing APIs. Saves require actual swept contact within a limited hand sphere, and missed/out-of-reach shots can score. Blue keeper possession selects the keeper and suspends its positioning/distribution AI; releasing the ball returns field control, and the keeper swims home. White's keeper stays autonomous. Upright orange bodies and team-colored caps are placeholders. Physical goal mouths remain 3m by 0.9m; brighter white frames and smoothly blended keeper/field camera framing improve presentation.

## Open and play

Open **Assets > Scenes > PrototypePool**, press Play, then click the Game view. The saved scene is wired. Optional setup is **Tools > Water Polo > Set Up Milestone 9 Match**, outside Play Mode after saving intended edits. Repeated setup preserves identities, existing pool/goals, and gameplay tuning.

| Control | Action |
| --- | --- |
| WASD / arrows | Swim |
| E | Collect a nearby loose ball |
| B | Pass in the facing/assisted direction |
| Hold / release Space | Charge / shoot in the facing direction (quick tap also shoots) |
| C | Switch Blue field player |
| F | Attempt a close-range steal |
| Hold Left Shift | Sprint while swimming (field players; consumes stamina) |
| L | Physical aerial lob pass |
| Z | Toggle Blue Press / Zone defense |

Blue initially attacks +Z and White -Z. Continuing after Period 2 physically switches both teams' ends, formations and goalkeepers; scoring follows the current attacking goal. Periods 3/4 retain those ends, and a new match restores the originals. Friendly acquisition selects the actual carrier, including Blue's keeper; C cycles only the six Blue fields. Human shots use the existing 0.7s charge bar; AI shoots directly. Lob passes have actual Y height, gravity, low descending reception and a ground height cue.

Default match: **four periods, each displaying 8:00 over 80 seconds of live play (6×)**. Clocks stop during restarts and breaks. Click **Continue / Start** at breaks, including **HALFTIME — SWITCHING ENDS** after Q2. Full time shows WIN/LOSE/DRAW, **Restart Match** and **Return to Prototype** (the same scene's paused ready state). The independent **28-second** shot clock survives same-team passes and keeper distribution, resets on opposing acquisition and stops at breaks; it is not accelerated sixfold. Expiration awards a controlled opposing restart. Periods use a neutral centre-ball start; goals award the conceding team possession. Stamina persists across ordinary restarts and resets for a new match. A formal swim-off, official foul/rebound exceptions, touch HUD, diving animations and final art remain future work.

## Documentation and source control

- [plan.md](plan.md): canonical architecture, milestone history, verification evidence, limitations and roadmap.
- [AGENTS.md](AGENTS.md): persistent development rules, including human-only Git write operations.
- `Tools/Verification/`: Editor-driven verification scripts kept outside player builds.

Only the human developer stages, commits or pushes. Unity assets must retain their `.meta` files. Generated folders (`Library`, `Temp`, `Logs`, `UserSettings`), credentials, local legacy references and `old-plan.md` are excluded from the proposed backup. Existing ignore rules cover Unity caches; `old-plan.md` must be left out explicitly.
