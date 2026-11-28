# Milestones 30I and 30J final report

Water Polo Superstars · ToduStudios · Unity 6000.6.7f1 Windows Editor

Started from human checkpoint `ce1b946`. The current swimwear is accepted for now. Its fitting and deformation, further walking work and goalkeeper balance are deferred. This session changes competitive continuity around the existing production arena. [Diagnostics and implementation details](MILESTONE_30IJ_DIAGNOSTIC_REPORT.md)

## Workstream B · Movement and ball continuity

| Improvement | Status | Implementation and verification |
| --- | --- | --- |
| Sideline and goal-line swimming | Implemented and verified in staged motion | The 1.10 m inset from the rule rectangle was replaced in the production arena by a 0.40 m capsule clearance from the physical basin. Both side limits measured 10.85 m from centre. Diagonal input at a wall retained 1.59 m of tangential travel. |
| Goal mouth, posts and net | Implemented and verified in staged motion. Visual fit awaits personal approval. | Both mouths admit and release an athlete. The maximum root depth was 0.50 m on both ends and the post stopped the root 0.40 m before the plane. The rear clearance accounts for stroke reach. Individual limb poses can still visually overlap cords. |
| Keeper loose ball behind the station | Implemented and verified in one staged situation | The existing keeper turned and swam 0.74 m toward a live ball partly beyond the goal plane, then took ordinary possession with no save or score. The pickup and scored-ball gates remain on BallController. |
| Goal volume, live rebounds and scored-ball protection | Implemented and verified in short fixtures | A 0.14 m radius ball remained live with its centre 0.045 m past the line. The fixture received a T press through the Input System and scored once after the complete crossing. Pickup and keeper catch of the scored ball were rejected. A physical post rebound reversed at 4.72 m/s and was collected in play. |
| Visible boundary flight and restart | Implemented and verified in staged outcomes | The same ball remained visible and physically moved with acquisition and scoring closed. Sampled post-award travel was 0.90 m wide, 1.94 m high, 2.24 m goalkeeper deflection, 1.60 m field deflection and 1.52 m sideline. The outcomes were two goal throws, two corners, and a throw in. The two clocks held and each restart was supplied. |
| Outside physical response | Implemented. Full visual coverage remains partial. | The live ball uses the existing frame and water physics. During the dead phase it can splash into surrounding water or use a low rebound against the authored deck plane. Rope collision and every last-touch/out-of-field combination remain untested or outside the equipment scope. |

The score plane uses the front of the posts. The November 2026 end-rope award uses the full ball crossing 0.30 m beyond that plane. The existing match phase and last-touch facts still choose the team, short/full shot-clock policy and restart. The fixture stages goalkeeper and field touches where deterministic attribution is the focus. The project still uses its existing simplified restart marks, direct-shot coverage and held-ball rule model. [World Aquatics Part Six](https://resources.fina.org/fina/document/2026/11/26/a63658dd-6078-4ef8-ab2a-2da081347f6b/_2026-11-26_World-Aquatics_CR.pdf)

## Workstream A · Fouls, celebrations and restart

| Improvement | Status | Implementation and verification |
| --- | --- | --- |
| Ordinary foul movement and quick release | Implemented and verified in the staged human fixture | Previously the match's roster pause, selection stop and generic setup delay suspended all translation. Now the ready transition happens once the marked taker is supplied. In the fixture the outlet moved 1.15 m and the near defender moved 0.22 m while the taker held the mark. Both clocks stayed stopped. An unauthorized shot and a teammate's unauthorized pass were rejected. The taker's human pass resumed play. |
| Contextual scorer gestures | Implemented, awaiting personal visual approval | The original procedural poses are Quick Pump, Power Raise, Acknowledge, Composed and Late Surge. The director uses score margin, decisive late score context, a nearby teammate or recent pass provider, and the last gesture. The routine and late poses were visually inspected in advancing Game View. Power Raise was selected in the keeper-conceding run. Quick Pump and Composed are implemented with limited visual coverage. |
| Teammates and conceding side | Implemented, awaiting personal visual approval | One nearby teammate may turn and acknowledge with a delayed arm gesture. A selected conceding player dips the head and upper torso. Most players tread or use bounded swimming toward their own half. The normal and late fixtures measured about 1.30 m of recovery movement while both clocks stayed stopped. A synchronized contact high-five is deferred. |
| Keeper conceding and centre restart | Implemented and verified in the staged goals | The fast miss scored once. The existing save blend rose to 1.00 and finished at zero. One centre signal and opposing-team possession followed. In the routine and late checks the opposing restart pass resumed live play. Already-legal field athletes retain position. The taker, receiver and athletes still on the wrong side use essential restart staging. |
| Live versus replay presentation | Implemented and verified in one routine goal | The old celebration hid the roster behind a single recorded pose. Only replay views now use that presentation copy. The routine check advanced the running broadcast replay, entered the behind-goal view, then skipped to the restart. Both clocks stayed out of live play. |

No paid assets, new Blender animations or replacement gameplay rigs were used. The contextual approach was inspired by EA's public discussion of player reactions and goal moments. The animation and technical implementation are original to Water Polo Superstars. [EA Sports gameplay notes](https://www.ea.com/ea-play/news/pitch-notes-fifa-22-gameplay-deep-dive)

## Local water

Implemented and sampled. Stationary hand-speed sculls, live hand crossings, speed-sensitive wake and bow contacts, and an occasional subtle float ring share the existing contact sampler and effect budgets. The final isolated lane sampled 62 idle scull events across the roster, two normal hand entries, and 8 normal versus 10 sprint wakes in equal windows. The ordinary ball drop had no bounce and 0.00 m/s residual vertical speed, while the intentional skip registered one bounce and 2.32 m/s vertical speed. The two effects measured strength 0.63 and 1.00 respectively. Independent idle-foot turbulence, separate bow/float fixtures, detailed close-range approval and Android performance remain open.

## Verification and handoff

Only isolated deterministic Windows Editor fixtures ran. All 17 distinct cases completed. Refinements were rechecked with the affected cases. No full match, broader regression suite, real-profile flow, device run or later milestone was started. Scripted motion used the same motor as human input and the ball used live production fixed steps. Nine representative Game View files are saved. Raw observations stay in the local `Logs/Milestone30IJ/Fixture-Results.txt`.

Unity is in saved Edit Mode with OlympicMatchArena open and `Assets/Scenes/Boot.unity` restored as normal Play entry. The project compile completed and the current Console had zero warnings and errors. Retained history includes a Windows Burst-cache warning and a Test Lab launch guard message. No verifier remained active. Arena GUID `5c4d3741df6b7f54e8cfd34d391d78d1`, arena .meta hash and real profile hash were preserved. Unity serialized ongoing arena configuration and regenerated unrelated material/font data while testing. The incidental material/font data were removed from the delivered changes. Git was inspected read-only.

Personal inspection should prioritize the shallow goal fit, the contrast between routine and late celebrations, and water readability. No full match is needed. [File inventory](MILESTONE_30IJ_FILES.txt) · [Test Lab guide](MILESTONE_30IJ_MANUAL_TEST_GUIDE.md) · [Game View evidence](Evidence/Milestone30IJ/)
