# Milestone 30B recovery audit

## Saved production and Editor state

- The session started at commit `6fd5c7d`. The saved production arena is clean against that commit and retains GUID `5c4d3741df6b7f54e8cfd34d391d78d1`. The existing TMP fallback asset change, `old-plan.md` and `tatus` are unrelated.
- The prior combined runner requested Stop after its final presentation case. The previous Editor process had exited by the start of this session, and Unity's last scene setup records OlympicMatchArena. On reopening Unity 6000.6.7f1, the active arena was in Edit Mode and clean. The Editor Play override was empty, so the regular Play button would have started the open arena. Boot remained first in the build scenes.
- The arena serializes the interactive pool finish, sampled contact effects, evaluated throwing and refined walkout. Both poolside officials have match tracking enabled. The arena serializes 14 skinned mesh bindings to `AtlasCompetitionBriefs.fbx`. Its original body cut and separate garment renderer are retained. The previous presentation run measured 220,332 raw roster triangles and 56 skinned renderers.
- The recorded 2,688-triangle geometry gate included a rendered isolated athlete preview and measured 265 matching seams across standing and advancing aquatic/action/deck samples. Its worst measured seam separation was 0.102 mm. The arena rollout check validated bindings, triangle counts and recovery. In the old live tread frame, a heavily tinted submerged waist had much less contrast than in crawl. The live M30B inventory identified a separate materials distinction. Edit Mode references `Blue/White Competition Briefs.mat`, and `AthleteEquipment` applies `AuthenticEquipment/Blue Briefs.mat` and `White Briefs.mat` when the match initializes. These materials are opaque URP Lit with alpha 1, back-face culling and default opaque queue. The full roster binding was genuine at the checkpoint.
- All three supplied photographs were located and inspected in the OneDrive Desktop folder. They show the rear-loaded arm and counterbalance, the diagonal keeper extension with torso rise, and the back of the hand over a partly submerged ball. The fitted briefs, aquatic motion and referee Blender sources are available under `SourceAssets`. Blender MCP responded on loopback port 9876 with Blender 5.2.3 and telemetry off.

## Recovered verification records

| Result | Recorded scope |
| --- | --- |
| Passed focused checks | Water contact sampling and effect budgets, water in both replay angles, evaluated throwing with 132 rendered turn samples and five physical releases, three physical test catches, isolated briefs deformation and material preview, both match officials. |
| Passed combined fixtures | CompetitionBall Run and Presentation, MatchPolish GoalCases and KeyboardMovement, RefereeMilestone30 Run. |
| Fixture output error | SwitchAndClock reached its report write, then failed because its output directory was absent. |
| Failed or incomplete combined fixtures | Ceremony Full missed ArenaIntro, Boundaries failed its controlled keeper deflection assertion, Penalty Save timed out at PenaltyReady, and Replacement timed out waiting for re-entry. These outcomes do not establish the gameplay cause. |
| Missing evidence | A fresh final compile and Console result for the combined run, completed ceremony and rules reruns, and a fresh isolated Boot-to-results-to-lobby run with real profile hash comparison. The Editor log also records a destroyed water renderer during replay cleanup. |

The prior fixture screenshots show the relaxed ball supported from below, matching the authored palm normal in the saved source. The measured grip error established attachment to that pose. It did not validate the requested palm-over-ball orientation.

## Recovery and documentation

Recorded water, throwing/deck and garment restore/reapply checks passed, with byte-identical reapply and repeat in the relevant gates. Preserve the saved arena GUID and the pre-milestone snapshot under `Logs/Milestone30/Recovery/`. The `EditorSceneDiagnostic.unity` copy differs in hash from the shipped arena and is diagnostic only. The prior handoff already included the three topic reports. Its linked exact Milestone 30 file inventory was missing and is reconstructed from the human checkpoint.

The underlying run results are in `Logs/Milestone30/` and `Logs/Milestone30/Regression/`. M30B reran the five incomplete cases successfully. The boundary verification needed a release gate because Milestone 30 queues the physical throw. The water recovery command's first M30B save serialized the new tuning fields. The recovered snapshots for the pre-session and saved M30B scene are under `Logs/Milestone30B/Recovery/`. Unity's recovery save also normalized the pre-existing dynamic TMP fallback diff to the committed state. No exact copy of that local atlas variant was found. [Original rendered evidence](Evidence/Milestone30/) · [Milestone 30 checkpoint inventory](MILESTONE_30_FILES.txt)
