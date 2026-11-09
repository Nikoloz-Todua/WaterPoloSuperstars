# Water Polo Superstars

Water Polo Superstars is a 3D mobile water polo game developed by **ToduStudios**, targeting Android first and iOS later. The long-term goal is responsive sports gameplay with professional mobile presentation; the current build is a functional prototype with placeholder art.

## Technology and architecture

- Unity 6.6, Editor **6000.6.7f1**.
- Universal Render Pipeline (URP), C#, 3D physics and the Unity Input System.
- XZ pool-plane tactics with separate 3D movement/presentation components.
- Shared tactical reasoning for both teams, one authoritative ball-possession system, and independent human selection.
- Useful concepts from the previous Water Polo Manager game are references; its 2D physics implementation is not imported.

## Current stage — Milestone 6

`Assets/Scenes/PrototypePool.unity` contains a playable 2v2 prototype: two Blue field players and two White AI field players. It includes swimming/bounds, pickup/carry/shooting, physical directional passing and reception, manual/automatic friendly switching, tactical support/defense, AI attacks, human/AI stealing, goals, scoreboard and automatic restarts.

The developer manually confirmed movement, camera following, passing, shooting, F stealing, friendly support, opponent activity, scoring by both teams and scoreboard updates. This does not establish manual verification of every edge case. The recorded Milestone 6 automated results cover ten Play Mode and two Edit Mode verification suites; see the canonical handoff for their scope and limitations. Mobile builds and device performance remain unverified.

Milestone 7 is planned next: two AI goalkeepers, positioning, shot interception, saves/deflections, goalkeeper possession and distribution. **None of these goalkeeper systems is implemented yet.** Development starts only after the developer completes the backup and explicitly authorizes it.

## Open and play

Open the project in the pinned Unity Editor. In the Project panel, double-click **Assets > Scenes > PrototypePool**, press Play, then click the Game view. The saved scene is already wired; optional setup is **Tools > Water Polo > Set Up 2v2 Prototype**, outside Play Mode after saving intended scene edits.

| Control | Action |
| --- | --- |
| WASD / arrows | Swim |
| E | Collect a nearby loose ball |
| B | Pass in the facing/assisted direction |
| Space | Shoot |
| C | Switch Blue player |
| F | Attempt a close-range steal |

Blue attacks +Z and White attacks -Z. Selection switches when a friendly actually gains possession. Touch controls are not implemented. Goals reset all four field players while retaining the score; this is free play without official clocks, match end, goalkeepers or full rules.

## Documentation and source control

- [plan.md](plan.md): canonical architecture, milestone history, verification evidence, limitations and roadmap.
- [AGENTS.md](AGENTS.md): persistent development rules, including human-only Git write operations.
- `Tools/Verification/`: Editor-driven verification scripts kept outside player builds.

Only the human developer stages, commits or pushes. Unity assets must retain their `.meta` files. Generated folders (`Library`, `Temp`, `Logs`, `UserSettings`), credentials, local legacy references and `old-plan.md` are excluded from the proposed backup. Existing ignore rules cover Unity caches; `old-plan.md` must be left out explicitly.
