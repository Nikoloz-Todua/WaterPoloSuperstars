# Milestone 30D live handoff

## Stage A recovery

- Starting Git HEAD is `d44f116`. The approved 30C checkpoint is present as tracked and untracked working-tree changes. Preserve those edits, `old-plan.md`, and `tatus`. File inventory for this turn is relative to the starting working tree.
- Unity is pinned to 6000.6.7f1. The TMP fallback asset is clean in the starting Git status. The original local diff mentioned in 30C is unavailable.
- The developer approved contact, human input authority, pickup, walking and Test Lab. Freestyle was rejected in live play and remains an active visual defect. The prior phase-mismatched calf measurements do not establish acceptance.
- Fresh tests so far: read-only status, history and Editor version. No 30D runtime or visual result yet.
- Next action: record workspace file hashes and saved arena/Editor state, read the 30C sources and verification, then capture the existing rendered freestyle at repeatable normalized phases.
- Recovery: use the saved production arena and the existing contact and Test Lab setup. Stop Play and use **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** after isolated tests.

## Stage A before the first capture

The Editor is in clean Edit Mode, production arena open, Boot entry set. Blender MCP answered on 127.0.0.1:9876. The saved arena SHA-256 and pre-existing work are recorded in `Logs/Milestone30D/Starting-Workspace.json`. The starting primary-profile hash is `xPO3U/DaVx2NNbBMkrQUL5STt2MCJvLbD6GTsiT6b+4=`; this is the comparison baseline for 30D only. The TMP fallback hash is `E6B7A3B5FC2478E622E17FCC60AFFFD8B1A854E1CCDB81FD6B110295BFEA4ED1`.

Next action: run the baseline pose comparison through the Underwater Freestyle Test Lab before changing production animation code. Collect the original Blender and exported FBX phase values separately.

## Stage A before verification

The old production pose was sampled at 0, 25, 50, 75 and 100 percent from five viewpoints, with matching sprint and tread views. The source Action samples show that the foot travels from 0.27 m below to 0.13 m above the hip, and the old leg animation uses a global roll reference. The previous Unity calf correction leaves that envelope and pelvis-to-thigh behavior intact. The new prone solve carries anatomical fronts through a hip-relative kick, with smaller hip travel and coordinated knee, shin, ankle and toe placement. It reads the existing stroke timing and leaves the Humanoid skeleton and asset clips in place. Verification and visual acceptance are pending.

The same phases rendered after the new pose was applied. The exported FBX preserves source joint geometry at these points. The new Unity crawl samples kept the toes approximately 0.194 m below to 0.061 m above the hip. The correction is active on the saved-authored rigs, and rendered underwater and overhead samples were inspected. User visual acceptance, advancing ball-leading proof and replay coverage remain open.

## Stage B before verification

The selected receiver turns toward the flight from the wind-up onward, brakes or makes a short adjustment toward the predicted reception centre, and yields this assistance to explicit movement. A forward-facing and arm-reach gate protects catches, and the arm absorbs toward the physical contact. The ballistic trajectory is determined once, on release. Next run is the long facing-away Test Lab scenario.

The 8.2 m Test Lab fixture passed. The receiver faced directly away initially (dot −1.000), turned during the flight, caught at dot 0.997, and was selected before the catch. First rendered palm-to-sphere error was below 0.1 mm in this case. The four actual Game view frames are in `Logs/Milestone30D/Receiving/`. Moving, intercepted, manually protected and missed cases remain separate coverage.

## Stage C before verification

The selected carrier now stores camera-relative aim separately from motor travel. Hold Left Alt plus movement keys to aim without translating. On touch, drag PASS to aim and release it for a directional dry pass. A stationary tap uses the current aim and a stationary hold can still deliver wet. The green selection arrow shows the chosen heading. Pass assistance keeps its cone, and open-water passes use the indicated heading. Aim also supplies the normal shot's horizontal heading while existing shot height/charge remain authoritative. Short turns and backward travel preserve the evaluated grip, stable forward travel transitions into leading, and sprint rules remain in the motor. Next action: compile and verify the arrow and real keyboard/drag release before tuning shooting motion.

The Stage C keyboard and real UI drag fixture passed. Both cameras showed the correct arrow, holding Alt turned in place while retaining one-hand grip, and the drag release launched a dry open-water pass exactly along the indicated heading. Maximum measured orientation, palm gap and translate errors were below the displayed precision. Tested using a synthetic mouse through the existing UI event system. Physical touch and assisted pass variations remain open.

## Stage D before verification

The shot boost is driven by the accepted pending request and sampled in the same evaluation that places the grip. It raises the visual pelvis and shoulders, presses down with the two leg chains, adds limited lateral lean and emits one bounded rise and recovery splash through the existing water observer. The quick category uses a shorter motion and lower rise. The motor and gain in release velocity remain separate. Next action: run the quick/full keyboard shot fixture, inspect rise and follow-through, and read physical release and garment heights.

Quick/full keyboard fixture passed. Pelvis rise was 0.256 m and 0.645 m, key-up to release 185 ms and 271 ms, and absolute ball release height 0.608 m and 0.987 m above water. Upper garment surface at release was −0.209 m and +0.173 m. Each action released once with measured origin error under 0.1 mm. Captures are in `Logs/Milestone30D/Shooting/`.

## Stage E before verification

The awarded ordinary FreeThrowReady phase lets eligible off-ball field players seek support or retain their guard assignments. The taker stays on the mark, opponents stay outside the existing configured distance, and teammates stay clear of body contact near the mark. Contact still resolves through the roster. The allowed taker uses the evaluated palm-over-ball pose during readiness. Penalty, boundary, centre and exclusion phases keep their prior movement permissions. Next action: verify the staged ordinary stop, held clocks, physical restart command and the separate penalty stage.

The ordinary/penalty fixture passed. Five attacking teammates and five defenders repositioned. The minimum defensive root clearance was 2.200 m, both clocks were held, the taker stayed on the mark and the presented palm touched the sphere. The deliberate pass resumed live play with human provenance. The penalty subcase kept the outlet stationary.

## Stage F in progress

All six implementation paths are present. Continue only with short integration checks and evidence assembly. The first shot fixture measured the release correctly, but the Game view framed a goalkeeper instead of the shooter. The close camera now explicitly frames the shooter and the fixture is repeating both shots. Next: verify the corrected view, run a focused control and contact regression, confirm Boot, the saved scene, current Console and the profile/TMP hashes, then complete the report.

The follow-camera check identified the cause of the wrong shot view. The preview staged the Rigidbody at centre while its interpolated Transform still represented the opening position. The follow callback added that stale displacement to its waypoint. It now follows the motor's physics position. One second attempt also missed the synthetic Space edge during startup. The fixture focuses the Game view and waits for the genuine charge edge before its hold interval.

The corrected shots rendered the shooter. The final quick/full measurements were 0.265/0.647 m pelvis rise, 182/270 ms from submitted key-up to release, 0.611/0.990 m ball height, and −0.206/+0.176 m upper garment height at release. Actual release origin matched the evaluated grip within the test precision. Underwater Sprint Freestyle and Directional Aim and Held Rotation have distinct Test Lab entries.
