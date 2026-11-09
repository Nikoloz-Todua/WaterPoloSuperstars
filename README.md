# Water Polo Superstars

Water Polo Superstars is a 3D mobile water polo game developed by **ToduStudios**, targeting Android first and iOS later. The long-term goal is responsive sports gameplay with professional mobile presentation; the current build is a functional prototype with placeholder art.

## Technology and architecture

- Unity 6.6, Editor **6000.6.7f1**.
- Universal Render Pipeline (URP), C#, 3D physics and the Unity Input System.
- XZ pool-plane tactics with separate 3D movement/presentation components.
- Shared tactical reasoning for both teams, one authoritative ball-possession system, and independent human selection.
- Useful concepts from the previous Water Polo Manager game are references; its 2D physics implementation is not imported.

## Current stage — Milestone 8

`Assets/Scenes/PrototypePool.unity` contains two Blue field players, two White AI field players, and one goalkeeper per team. Swimming, physical passing/reception, tactical AI, stealing, goal detection, score HUD and six-athlete restarts remain. Milestone 8 adds temporary human Blue goalkeeper control on confirmed possession, charged human shots with a power bar, and modest camera/goal-frame readability improvements.

The developer manually tested and backed up Milestone 7 as `a1374a8`. Earlier Milestone 6 confirmations cover movement, passing, shooting, stealing, AI activity and both teams scoring; they do not establish exhaustive edge-case coverage. Milestone 8 awaits developer manual testing. See the canonical handoff for actual automated results and limitations. Mobile builds and device performance remain unverified.

Goalkeepers share the existing ball ownership and passing APIs. Saves require actual swept contact within a limited hand sphere, and missed/out-of-reach shots can score. Blue keeper possession selects the keeper and suspends its positioning/distribution AI; releasing the ball returns field control, and the keeper swims home. White's keeper stays autonomous. Upright orange bodies and team-colored caps are placeholders. Physical goal mouths remain 3m by 0.9m; brighter white frames and smoothly blended keeper/field camera framing improve presentation.

## Open and play

Open the project in the pinned Unity Editor. In the Project panel, double-click **Assets > Scenes > PrototypePool**, press Play, then click the Game view. The saved scene is already wired; optional setup is **Tools > Water Polo > Set Up Milestone 8 Prototype**, outside Play Mode after saving intended scene edits. Repeated setup preserves the existing pool/goals/camera/HUD and tuning.

| Control | Action |
| --- | --- |
| WASD / arrows | Swim |
| E | Collect a nearby loose ball |
| B | Pass in the facing/assisted direction |
| Hold / release Space | Charge / shoot in the facing direction (quick tap also shoots) |
| C | Switch Blue field player |
| F | Attempt a close-range steal |

Blue attacks +Z and White attacks -Z. Friendly acquisition selects the actual carrier, including Blue's keeper. The keeper is not included in routine C switching. A single bottom-centre bar appears only while charging and progresses green/yellow/red over the default 0.7s charge. AI uses immediate shots. Touch controls are not implemented. Goals reset all six athletes while retaining the score; this remains free play without official clocks, match end or full rules. Diving animations, shot-height/arcing mechanics and realistic water are future work.

## Documentation and source control

- [plan.md](plan.md): canonical architecture, milestone history, verification evidence, limitations and roadmap.
- [AGENTS.md](AGENTS.md): persistent development rules, including human-only Git write operations.
- `Tools/Verification/`: Editor-driven verification scripts kept outside player builds.

Only the human developer stages, commits or pushes. Unity assets must retain their `.meta` files. Generated folders (`Library`, `Temp`, `Logs`, `UserSettings`), credentials, local legacy references and `old-plan.md` are excluded from the proposed backup. Existing ignore rules cover Unity caches; `old-plan.md` must be left out explicitly.
