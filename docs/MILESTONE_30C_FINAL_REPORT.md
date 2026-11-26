# Milestone 30C technical report

## Recovery checkpoint

The session started at `d44f116` on Unity 6000.6.7f1. OlympicMatchArena was clean and active in Edit Mode. The Play override was empty. Blender MCP answered on local port 9876. The prior keyboard and missed dry-pass checkpoints failed, the close-up catch gap remained visible, and the extended match run was interrupted. The live handoff contains the active implementation and verification checkpoints.

## Delivered checkpoint

Stages A–C are implemented and have focused Windows Editor evidence. Stage G delivers the feature-based Test Lab and new contact and swimming inspections. The remaining broad work is explicitly deferred.

| Area | Delivery and acceptance status |
| --- | --- |
| Human input authority | Passed idle, rotation, movement, pause input, selection cancellation and manual protection. One explicit keyboard pass/catch and shot released with HumanInput provenance. Actual opposing tactical pass and shot passed. A Blue ordinary restart held for 6.25 s and White's automatic restart pass recorded RuleRestart. Human keeper hold and automated penalty release were not independently exercised beyond the central gate. |
| Physical player contact | Passed six staged cases with actual production motors and rigidbodies. Head-on Blue teammates and centre opponents held 0.900 m between roots. Shoulder and crossing minima were 0.655 and 0.647 m. The wall sprint reached 5.750 m/s and maintained 0.780 m separation with the defender at x=8.900 m. Captures show visible resistance and return to tread. Arm mesh overlap remains possible. Broad live tactical and Android acceptance remain open. |
| Automatic pickup | Passed 0.94 m nearby recovery, one-owner contested recovery and a legitimate dry miss. The miss hit water once and was automatically picked up at about 2.108 m/s. |
| Freestyle lower body | Repaired the actual loss of calf axial orientation in the Unity Humanoid result. The measured calf front-down dots moved from approximately zero to 0.98. Hip-led timing and shallow knee flexion persisted. The final normal, sprint, leading and eggbeater sequences were inspected. User visual approval remains open. |
| Standing and walking | The deck solver now follows route distance for support, toe-off and swing with heel/toe-calibrated standing. Eight ceremony stages passed and Game view sequences include lineup and both captains. The live official moved while tracking the pool, signalled both directions and executed the exclusion and penalty cues. Detailed naturalness, close deck contact and independent stopping/lateral variations still need user review. |
| Receiving | Inherited stationary reception and automatic selection passed. Long-pass recognition, receiving orientation and the first visible palm gap remain pending. |
| Directional aim and carrying | Existing camera-relative W and Up Arrow matched 4.00 m/s in each camera. New aim arrow, independent aim and more controllable held turns are deferred. Existing retreat, dribble and voluntary drop remain available. The broad release fixture was not rerun for every modifier and touch input. |
| Professional shooting and splash | The existing charge, physical release and rear-loaded preparation remain. New explosive torso elevation, leaning options and timed splash upgrades are deferred. |
| Goalkeeper | New goalkeeper neutral state, routine catch, save commitment and recovery work is deferred. This checkpoint exercises the penalty ready rule and official cue. Existing save previews remain. |
| Foul possession and movement | Human restart input authority and the stopped clock passed. The low foul grip, legal teammate repositioning and new goalkeeper observation during stoppages are deferred. |
| Goals and ball exits | New celebrations, conceding reactions and visible adjudicated near-miss/out-of-play interval are deferred. Goal attribution, replay and boundary restart changes were outside the delivered scope. |
| Water | One physical missed-pass impact and normal recovery passed. Existing effects and skip previews remain. Idle sculling, contact and shooting effects are deferred. |
| Swimwear | Existing Atlas briefs appear in live underwater crawl and both team ceremony views. Garment construction and materials continue from 30B. User visual approval, replay and elevated shooting visibility remain pending. |
| Test Lab | Uses the existing match and preview assets with no hub session. New direct entries include head-on, shoulder, sprint, contested pickup, freestyle from side/below, and walkout/captains. The earlier focused previews moved from the milestone folder to feature folders. Existing production and verification paths are kept. The actual head-on menu and keyboard approach passed, reaching a 0.900 m signed gap. The ceremony menu passed all eight stages. Wider development/recovery menu cleanup and additional standalone labs are deferred. |

## Actual verification and limits

- The focused control, contact, swim, eight-stage deck, official and ordinary restart/missed pass checks used advancing Windows Editor Play. The artificial fixture positions and selected disabled brains make each outcome repeatable. Local JSON results are under `Logs/Milestone30C/`.
- Earlier fixture failures were constrained to a wrong camera axis, a too-short opening wait, scaled time used during pause, a third athlete parked in the sprint lane, late attachment to the brief arena overview and a glancing camera axis in the head-on menu. Each is documented in the live handoff. The historical close-up catch gap remains unresolved.
- The roster pair solve consumes one fixed-step pass with 3 iterations and a cached roster array. The pair marker averaged 9.48 μs per invocation, with an 11.2 μs peak recorded frame in the available Windows contact samples. The local viewport snapshot is observational. Android GPU, memory, FPS, thermal behavior and broad touch checks remain unmeasured.
- A separate 894 × 476 High quality Test Lab snapshot reported 352 draw calls, 1.668M rendered triangles, 11.12 ms CPU frame, 4.95 ms CPU main thread and 0.214 ms GPU. It used a close, mostly idle view after the scripted approach, with 0 live water patches/droplets in the second sample. The active roster was 56 skinned renderers and 220,332 raw triangles. The enabled roster renderers referenced about 42 MiB of shared textures, measured by Unity object size. The scene's full-draw totals and the close Test Lab figures use different views.
- Final active-athlete inventory confirmed seven Blue and seven White Atlas briefs, including both goalkeeper roles, with 2,688 triangles and 66 bone slots per garment. The pre-existing 66 bind poses were present in the saved mesh. [Head-on Game view](Evidence/Milestone30C/Contact-Test-Lab.png) · [Officials](Evidence/Milestone30C/Official-Deck-Sequence.jpg)
- No four-period full-duration run was started. Fresh goal/own-goal/replay variants, boundary and penalty outcomes, and the Boot-to-results reward loop remain open as separate integration checks.
- Older fixtures that deliberately call a throw on behalf of an unselected actor must explicitly use the isolated Editor verification source. Wider legacy fixture compatibility was not rerun. The latest keyboard movement fixture passed in both cameras. It did not reproduce the 30B zero-movement result.

## Manual inspection and recovery

1. **Tools > Water Polo > Test Lab > Player Contact and Defense > Head-On Centre Players**. Hold **W/Up Arrow** after setup, disengage, then try **Shoulder Slide** and **Sprint into Stationary Defender**. [Contact motion](Evidence/Milestone30C/Contact-Sequence.jpg)
2. **Tools > Water Polo > Test Lab > Swimming and Floating > Underwater Freestyle**. Alternate normal movement, **Shift** and tread. Run **Underwater Ball Leading** to inspect the carried-object variation. [Before and after](Evidence/Milestone30C/Prone-Comparison.jpg) · [Swim variants](Evidence/Milestone30C/Prone-Variants.jpg)
3. **Tools > Water Polo > Test Lab > Ceremony and Walking > Arena Walkout and Pennants**. **Enter** advances a stage. Let the movement play and inspect the lineup and both captains' standing soles during the exchange. [Deck sequence](Evidence/Milestone30C/Deck-Sequence.jpg)
4. **Tools > Water Polo > Test Lab > Ball Control and Player Switching > Possession and Throwing**. Move and turn before holding **Space** to shoot or pressing **B** to pass. Press **C** during a queued action to inspect cancellation. Stop Play between cases.

The contact switch can be cleared on **PrototypeRoster** in saved Edit Mode. The full pre-contact scene snapshot is `Logs/Milestone30C/OlympicMatchArena-BeforeContact.unity`. Shared source recovery uses the checkpoint version identified in the exact inventory and the existing recovery menus. Use **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** after isolated tests.

The initial TMP fallback font asset change no longer appeared in Git status after Unity import/Play and scene save. The asset received no direct source edit in this task. Its original diff was not captured, so preservation of those local bytes could not be verified. The unrelated untracked entries remain present.

## Final project condition

OlympicMatchArena is saved and clean in Edit Mode, retains its production GUID, and has the contact option enabled on the 14-player roster. The authority and evaluated aquatic/deck changes compile and are active in the production code paths. The Editor's normal Play scene is Boot. Current Console errors and warnings are zero. The isolated direct tests left the real profile byte-identical by SHA-256. There are no active fixtures, experimental gameplay switches or new standalone scene assets. Stage D is the next development checkpoint.

## Deliverables

- [Live handoff](MILESTONE_30C_LIVE_HANDOFF.md)
- [Exact change inventory](MILESTONE_30C_FILES.txt)
- [Manual user test guide](MILESTONE_30C_MANUAL_TEST_GUIDE.md)
- [Contact and authority](PLAYER_CONTACT_AND_CONTROL_AUTHORITY.md)
- [Aquatic and deck animation and editable sources](PROFESSIONAL_AQUATIC_AND_DECK_ANIMATION.md)
- [Receiving and match continuity](RECEIVING_AIMING_AND_MATCH_CONTINUITY.md)
- [Test Lab and recovery paths](WATER_POLO_TEST_LAB_GUIDE.md)

Next development checkpoint should start with a long pass to a swimmer facing away. Measure its physical contact and rendered palm separately, then connect the receiving turn to the aim and selection work before adding presentation changes.
