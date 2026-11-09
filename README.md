# Water Polo Superstars

Water Polo Superstars is a 3D mobile water polo game developed by **ToduStudios**, targeting Android first and iOS later. The long-term goal is responsive sports gameplay with professional mobile presentation; the current build is a functional prototype with placeholder art.

## Technology and architecture

- Unity 6.6, Editor **6000.6.7f1**.
- Universal Render Pipeline (URP), C#, 3D physics and the Unity Input System.
- XZ pool-plane tactics with separate 3D movement/presentation components.
- Shared tactical reasoning for both teams, one authoritative ball-possession system, and independent human selection.
- Useful concepts from the previous Water Polo Manager game are references; its 2D physics implementation is not imported.

## Current stage — Milestone 7

`Assets/Scenes/PrototypePool.unity` contains two Blue field players, two White AI field players, and one AI goalkeeper per team. The existing swimming/bounds, pickup/carry/shooting, physical passing/reception, friendly switching, tactical AI, stealing, goals, scoreboard and restarts remain. Goalkeepers add bounded goal-mouth movement, reaction-limited swept-contact catches/deflections, secure possession and timed physical distribution to field players.

The developer manually confirmed Milestone 6 movement, camera following, passing, shooting, F stealing, friendly support, opponent activity, scoring by both teams and scoreboard updates. This does not establish manual verification of every edge case or the new goalkeeper behavior. See the canonical handoff for fresh automated verification results and limitations. Goalkeeper game feel, mobile builds and device performance remain unverified.

Goalkeepers share the existing ball ownership and passing APIs. Saves require actual swept contact within a limited hand sphere, and missed/out-of-reach shots can score. The keepers remain AI-controlled: catches do not select them; receiving Blue field players use the normal automatic selection rule. Their upright orange bodies and team-colored caps are placeholders.

## Open and play

Open the project in the pinned Unity Editor. In the Project panel, double-click **Assets > Scenes > PrototypePool**, press Play, then click the Game view. The saved scene is already wired; optional setup is **Tools > Water Polo > Set Up Goalkeeper Prototype**, outside Play Mode after saving intended scene edits. Repeated setup preserves the existing pool/goals/camera/HUD and tuning.

| Control | Action |
| --- | --- |
| WASD / arrows | Swim |
| E | Collect a nearby loose ball |
| B | Pass in the facing/assisted direction |
| Space | Shoot |
| C | Switch Blue player |
| F | Attempt a close-range steal |

Blue attacks +Z and White attacks -Z. Selection switches when a friendly field player actually gains possession; C cycles Blue field players only. Touch controls and manual keeper control are not implemented. Goals reset all six athletes while retaining the score; this remains free play without official clocks, match end or full rules. Diving animations, shot-height/arcing mechanics and realistic water are future work.

## Documentation and source control

- [plan.md](plan.md): canonical architecture, milestone history, verification evidence, limitations and roadmap.
- [AGENTS.md](AGENTS.md): persistent development rules, including human-only Git write operations.
- `Tools/Verification/`: Editor-driven verification scripts kept outside player builds.

Only the human developer stages, commits or pushes. Unity assets must retain their `.meta` files. Generated folders (`Library`, `Temp`, `Logs`, `UserSettings`), credentials, local legacy references and `old-plan.md` are excluded from the proposed backup. Existing ignore rules cover Unity caches; `old-plan.md` must be left out explicitly.
