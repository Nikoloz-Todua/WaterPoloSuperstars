# Milestone 30H final report

Water Polo Superstars · ToduStudios · Recovery closed 2026-11-28 · Unity 6000.6.7f1

**Goalkeeper approved by the developer for now. Swimwear visually rejected. The recovery and documentation are complete.**

## Interruption and recovery

The implementation started at the approved 30G checkpoint `a77e1bb`. The prior session baked both kits, changed the professional goalkeeper pose and palm contact, ran dry and underwater fabric checks, and performed an initial isolated keeper run. That run found late reaction on a fast near-post shot and a save blend held during goal presentation. The fixes, revised Test Lab fixtures and verifier were already on disk when the terminal started its final recompile and fixture polling. The usage limit arrived just after Neutral launched and before the terminal returned the final fixture summaries. Timestamped images show that the captures progressed. The human checkpoint `30a5041` includes the production code, revised verifier, short diagnostic and first manual guide. The final report and file inventory were placeholders, the live handoff described the run as in progress, and `plan.md` still opened at 30G.

Recovery reviewed the checkpoint diff and source, validated the saved scene and live bindings, recovered earlier terminal observations and images, and checked the keeper cases with unresolved final outcomes. It measured the exact Blender garment in rest and eggbeater, confirmed live materials on both teams, and completed the handoff. The verifier now reports actual material and rig bindings and the score after each keeper fixture. Capture can be disabled to preserve earlier images. Production gameplay, assets and scenes required no recovery edit. [Exact file inventory](MILESTONE_30H_FILES.txt)

## Goalkeeper implementation and evidence

The keeper reuses the production eggbeater with low arms, tracked head and goal-local positioning. A threatening carrier can raise readiness. Predicted arrival time, height and side drive the reach, lean and water press. The pose presents one hand laterally or both hands to a central high ball. The fixed order remains BallController at −140, GoalkeeperInterception at −120, and PrototypeMatch at −100. The observer sweeps the moving ball against evaluated palms. BallController owns right-hand catch/hold and deflection, and a physical miss can score. The keeper lowers into recovery after contact or a missed attempt, including during goal presentation. Recovery left this approved behavior and difficulty tuning as committed in `30a5041`.

| Isolated fixture | Evidence | Result |
| --- | --- | --- |
| Neutral and ball tracking | Fresh final-code run | Zero attempts. After a 2.5 m carrier shift the keeper moved 0.97 m toward the carrier. Wrists were +0.07/+0.06 m relative to the chest bone in the sampled neutral frame. |
| Ordinary catch | Prior terminal, before the final reaction and recovery adjustments | One catch, no save attempt or parry, White goalkeeper ownership and 0.03 m peak lift. Save blend 1.00 → 0.00. |
| Goal-local left | Prior terminal, before the final reaction and recovery adjustments | One attempt and one catch with White goalkeeper ownership. Peak lift 0.23 m and blend 1.00 → 0.00. The later left-side capture provides pose evidence. |
| Goal-local right | Fresh final-code run | One attempt, one parry, score 0–0, peak rise 0.21 m, blend 1.00 → 0.00. The left arm reached on this goal-local delivery. |
| Central high | Fresh final-code run | One attempt, one parry, score 0–0, peak rise 0.32 m. Both wrists reached +0.75 m relative to the chest, blend 1.00 → 0.00. |
| Fast near-post miss | Fresh final-code run | One attempt, zero catches/parries, Blue led 1–0 at GoalRestart. Peak rise 0.18 m, blend 1.00 → 0.00. |

The earlier successful catch and left fixtures were reused. The raw post-goal capture named `Keeper-Recovered.jpg` shows the scoring presentation camera on the scorer. The fresh blend measurement is the recovery evidence. The short fixtures leave broader save balance, both keeper roles in all contexts, penalties and other shot types open. Independent left-hand possession remains deferred. [Selected recovered Game View frames](Evidence/Milestone30H/) · [Test Lab paths](MILESTONE_30H_MANUAL_TEST_GUIDE.md)

## Swimwear

The 2,688-triangle reconstructed `AtlasCompetitionBriefs.fbx` is active on all 14 athletes. The suit and cut body share the same root and 66 ordered bones, and all fallbacks are disabled. The live Blue and White URP Lit materials bind the correct fabric maps. Earlier matched standing and advancing underwater evidence establishes the material improvement and Blue visibility. The developer's personal review establishes that the fit remains unacceptable.

The rest mesh retains the steep front taper and the old cut at all three openings. The rebuild mostly subdivided the original silhouette and added millimeter-scale trim. The source audit found pronounced local leg-rim stretch during eggbeater. Seam registration is sound, with localized overlap candidates and penetration depth still unresolved. Rest geometry and pose deformation are the priorities. White-specific underwater framing and device rendering remain open. The garment stays visually rejected until the developer accepts a later correction. [Geometry, skinning and fitting plan](MILESTONE_30H_DIAGNOSTIC_REPORT.md)

## Saved state and limits

- Unity finished in saved Edit Mode with `OlympicMatchArena.unity` open and clean. Normal Play entry is `Assets/Scenes/Boot.unity`. Project compilation is up to date and the verifier compiled without diagnostics. Final Console ground truth was zero errors and zero warnings. Retained Pipeline history shows one pre-existing Windows Smart App Control / WDAC warning for a cached Burst JIT DLL. The Console was left intact.
- No verification or batch run remained active. The interactive Blender window was an unsaved default scene and stayed untouched. The production source was read in a short background Blender process that exited.
- Arena scene and .meta SHA-256 matched before and after recovery. The arena retains GUID `5c4d3741df6b7f54e8cfd34d391d78d1`. The real `profile-v1.json` matched its pre-run SHA-256 and was kept out of the lab flows.
- Verification covered short Windows Editor fixtures and Blender source measurements. No full-duration match, broader regression suite, phone test or Android performance test was run. Existing approved gameplay and walking remain outside this change.

Next, reconstruct the swimwear, fit it to the anatomy and correct its deformation in an isolated candidate. The following major task is natural legal movement during ordinary fouls. Neither implementation started here. The remaining roadmap is in `plan.md`.
