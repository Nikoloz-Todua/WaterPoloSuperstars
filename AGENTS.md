# Water Polo Superstars — Codex Instructions

## Project

- Game: Water Polo Superstars; studio: ToduStudios.
- Engine: Unity 6.6; read `ProjectSettings/ProjectVersion.txt` for the pinned Editor version.
- Rendering: Universal Render Pipeline (URP).
- Primary platform: Android; secondary: iOS.
- Canonical technical handoff: `plan.md`. `README.md` is the short public introduction.

## Git — human operations only

- NEVER stage, commit, push, or otherwise modify Git state. The human developer performs ALL Git write operations manually.
- Do not run `git add`, `commit`, `push`, `reset`, `rebase`, `merge`, `stash`, `fetch`, or working-state-changing `checkout`/`switch`, or any equivalent command/API that changes the index, refs, branches, commits, working tree, repository configuration, or remotes.
- Do not perform these operations through GitHub APIs, Unity integrations, scripts, plugins, or other indirect tools. Do not automatically create checkpoints.
- Read-only inspection is permitted: status, diff, log, branch inspection, remote inspection, ls-files, and read-only remote queries. Never print credentials while inspecting configuration or files.
- Previous permissions or saved command approvals do not override this restriction. For a backup/checkpoint, provide the developer with explicit commands to execute manually; do not execute them yourself.
- Preserve existing local changes and exclude generated caches, secrets, `old-plan.md`, raw legacy references, archives and unrelated files from proposed backups. Do not recommend indiscriminate staging.

## Development rules

1. Read `plan.md`, this file, and relevant existing documentation before coding. Inspect actual scripts, scene references and configuration rather than assuming documented APIs exist.
2. Preserve working gameplay. Explain necessary structural changes before implementing them; do not replace major systems unnecessarily or modify unrelated files.
3. Prefer clean, modular C#. Keep team identity, tactical decisions, input, physical movement, possession, scoring and presentation separate. Reuse the authoritative ball/selection/match systems.
4. Account for Android CPU, GPU, memory and allocation costs. Desktop performance does not establish mobile performance.
5. Use the existing Unity Input System. Human/touch and AI commands should share gameplay APIs; do not introduce legacy input or 2D physics dependencies into the 3D prototype.
6. Preserve Unity `.meta` files and serialized references. Never overwrite SampleScene during prototype setup. Keep Editor setup repeat-safe and preserve useful Inspector tuning.
7. Run relevant compilation/automated verification where available. Distinguish source inspection, previous recorded results, fresh automated runs, developer manual tests and device tests. Never claim untested functionality or every edge case is verified.
8. Never delete, overwrite or upgrade the old Water Polo Manager project to inspect it. Request small groups of useful legacy files/examples when needed; do not import its full architecture or historical plan into this project.
9. Give exact Unity menu paths and short, clear manual instructions when required. Do not begin a later milestone without explicit authorization.

## Documentation rule

After every meaningful session, review changes and update `plan.md` with the actual implementation, files changed, confirmed architecture, tests/evidence, limitations, known issues and next step. Keep it sufficient for a new session without chat history. Preserve valuable technical history, but avoid temporary discussion and failed experiments.

Current work is Milestone 8: four field players plus two goalkeepers. Milestone 7 was manually tested and backed up by the developer as `a1374a8`. Blue keeper possession now automatically grants temporary human control; release/loss restores a Blue field player. C still cycles only Blue field players. White's keeper remains AI-controlled. Human shooting uses hold/release charge; AI keeps direct shot commands. Keep keepers outside field formation/pursuit assignments. Read the latest actual automated results and manual-test status in `plan.md`; Milestone 8 manual gameplay and device performance are not verified. Do not begin another milestone without explicit authorization. The human-only Git restriction remains permanent; no automatic checkpoints.
