# Milestone 30F final report

Water Polo Superstars · ToduStudios · Unity 6000.6.7f1  
Starting human checkpoint `dcfe2ea`, Milestone 30E.

## Task A — Team intelligence

The previous attack mostly shifted each formation slot by carrier progress. Markers, passing lanes and teammate destinations did not enter that decision. On arrival, the motor usually cleared facing and the rig supplied an intermittent glance inside a short range.

The team component now samples field positions once per 0.20 s and shares a support plan. Candidate scores combine lane obstruction, defender pressure, outlet distance, teammate positions and reserved pockets, role fit and travel cost. Centre forward has a tighter movement area. Wings maintain width. Flats can open lateral or diagonal routes. Centre back stays available as a deeper outlet. A short commitment and incumbent preference reduce changes while an athlete is swimming toward a useful spot. An ownership, selection or phase change interrupts obsolete support.

Arrived outlets turn their whole body toward the ball carrier through the existing weighted motor. Swimming outlets glance where the stroke and angle allow. Defenders track the play in relation to their marks, retain their Press or Zone movement and still contest the ball. The existing contact solver handles crowding and momentum. Human selection takes priority and the human-team autonomous throw gate remains in force.

| Windows Editor check | Result |
| --- | --- |
| Stationary and moving support | Two stationary outlets faced the ball carrier. The Game View showed other outlets swimming to spread the attack. |
| Defender-aware move | The marked athlete's sampled starting lane was blocked. Its new pocket was 3.09 m away with a clear sampled lane. |
| Carrier change and spacing | Keyboard moved the carrier 2.96 m. Four of five support targets changed. Closest teammate separation was 3.03 m before and 2.90 m after. |
| Human authority | The carrier retained selection and possession in the final run, and Blue recorded zero uncommanded throws. An earlier run ended with a legal defensive steal. The preview starting positions now allow a longer observation window. |
| Shared planning cost | Mean 0.059 ms across five sampled planning calls in the Windows Editor. |

The support test exercises one controlled production attack. It is possible for the selected lane to close again as its defender moves. A broader manual review of off-ball motion, both attacking ends and opposition balance is open. [Support implementation](INTELLIGENT_TEAM_SUPPORT_AND_SPATIAL_AWARENESS.md) · [Rendered support sequence](Evidence/Milestone30F/Team-Support-Overview.jpg)

## Task B — Receiving

The previous motor could turn during a pending pass, but the rig waited for a close airborne approach before preparing the hand. The separate AI flight pursuit could also disagree with the preparation intent. Selection at release reset the receiver's velocity and turning rate.

The accepted pass now supplies the motor, AI and rig with the same reception read. During preparation, the receiver sees the passer, checks its stroke and turns. After release, the approach comes from the physical position and velocity with a small launch-pocket correction for the swimmer. Automatic selection transfers at physical release. Its handoff clears movement commands while retaining the receive momentum. **C** preserves the manual window and an active movement command takes priority over receive movement. The receiver left under AI control can still attempt the original catch.

The rig opens the right hand once the shoulder can present it. The pose reflects height, side and time to arrival. Swept contact tests the wrist reach, facing, arrival speed, head clearance and play boundaries. The first rendered pose is evaluated at the authoritative contact before the absorption starts. The bent arm then brings the ball into the existing 30E gather and possession path. A valid pass or shot can queue immediately using the existing throw authority. Higher interception preparation was tuned using the same defender starting positions after earlier runs showed the shoulder stayed too low for the approach.

| Windows Editor check | Result |
| --- | --- |
| 8.2 m pass to a swimmer facing away | Began at 180.0° from the pass and caught at 2.9°. The keyboard command entered preparation in 0.020 s. The hand was visibly prepared 0.555 s before the catch. Selection transferred in flight. |
| First rendered palm contact | Palm landmark error was below 0.1 mm at displayed precision. The closest sampled, baked palm skin vertex was 0.3 mm from the collider sphere. The ball shell's three measured bounds radii were 139.6–139.9 mm against a 140.0 mm collider. Its bounds centre was 0.34 mm from the sphere centre in the saved arena. These measure the fixture's first rendered catch and local geometry. |
| Short dry pass and immediate shot | Physical catch passed. A shot queued immediately on ownership physically released 0.280 s later with HumanInput provenance. |
| Away swimming and lateral swimming | The receiver started each trial at 2.60 m/s. Both turned and caught. |
| Lob and higher interception | The lob caught 0.35 m above the surface-rest ball centre. The same defender case that initially failed passed after height-aware preparation reached the physical lane. |
| Manual switch and miss | **C** held control on the manually chosen athlete through the original receiver's catch. Holding deliberate sideways movement made the separate pass miss. Its horizontal bearing changed 0.00° before the miss and it became recoverable at the water surface. |
| Swept contact cost | Mean 0.042 ms across 28 samples during the long approach in the Windows Editor. This timing covers the contact search. |

The final short receiving matrix passed. The competition catch still uses the dominant right grip. Independent left-hand contact and transfer are unimplemented, and other awkward-side passes can legitimately miss. Personal visual approval, physical touch and Android timing remain open. The evidence shows the rendered away-to-catch sequence. The moving, lobbing, interception and interruption results come from controlled runtime probes. [Receiving implementation](INTELLIGENT_RECEIVING_AND_PHYSICAL_CATCHING.md) · [Rendered receiving sequence](Evidence/Milestone30F/Receiver-Facing-Away-Sequence.jpg)

## Saved production condition

- The Editor is stopped and the saved active scene is `Assets/Scenes/OlympicMatchArena.unity`. Boot is the normal Editor Play entry. The production arena has 14 roster members and professional throws enabled. Its file SHA-256 and GUID `5c4d3741df6b7f54e8cfd34d391d78d1` matched before and after.
- Compilation completed and the current Unity Console showed **0 errors and 0 warnings**. The retained Pipeline log history includes an early command typo and two corrected references to the retired receiving flag. The final isolated support and receiving commands finished and Play Mode stopped.
- The isolated Test Lab used the production arena without a hub match session. The real `profile-v1.json` SHA-256 matched after verification. The existing TMP asset change and untracked local files are outside the 30F inventory. Human Git operations were left to the developer.
- No full match ran. The existing 30G–30L roadmap remains deferred. The inverted freestyle lower body is an active backlog defect. Device checks and a match-wide balance pass remain open.

For inspection, start with **Tools > Water Polo > Test Lab > Team Tactics > Intelligent Team Support**, then **Tools > Water Polo > Test Lab > Shooting and Passing > Long Pass to Receiver Facing Away**. The guide lists the exact controls, variants, defect signs and recovery path. [Manual Test Lab guide](MILESTONE_30F_MANUAL_TEST_GUIDE.md) · [Exact changed files](MILESTONE_30F_FILES.txt) · [Live handoff](MILESTONE_30F_LIVE_HANDOFF.md)
