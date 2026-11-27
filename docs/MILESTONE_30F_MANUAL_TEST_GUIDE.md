# Milestone 30F manual Test Lab guide

Use Unity **6000.6.7f1**. Start each trial from saved Edit Mode. Wait until Blue holds the ball and the controls show **PASS**. Stop Play between trials to restore the starting pose. The Test Lab uses runtime positions in the production arena and an isolated match session.

## Intelligent Team Support

Open **Tools > Water Polo > Test Lab > Team Tactics > Intelligent Team Support**.

1. Hold no keys. Some teammates should swim into better lanes while a nearby free outlet treads and turns toward the carrier.
2. Use **WASD** or the **arrows** to move a few metres toward either side. Use **Left Alt + direction** to turn in place. Look for a new supporting angle from the marked swimmer, width from the wings and a deeper outlet.
3. Press **C** to test manual control. Use an immediate movement input if selection changes. The selected athlete should follow input without also swimming toward its old AI destination. The previous athlete can support or hold possession as the rules allow.

Watch for stationary outlets staring away, players crowding one destination, lane changes that oscillate every stroke, or a Blue pass or shot without input. Defenders can pressure or steal. [Support Game View sequence](Evidence/Milestone30F/Team-Support-Overview.jpg)

## Passing and receiving

Open **Tools > Water Polo > Test Lab > Shooting and Passing**, then choose the named fixture. The lab starts in broadcast. Press **F8** three times from broadcast to reach the staged receiver close-up, then once more to return. **F6** switches the broadcast axis. In close-up, **B** still uses the passer's accepted heading.

| Scenario | Reproduce | Expected result | Defect to watch for |
| --- | --- | --- | --- |
| **Long Pass to Receiver Facing Away** | Leave the aim alone and press **B**. Restart and press **C** during flight. | The receiver starts with their back to the passer, turns through the shoulders and torso, opens a palm before contact and gathers from the physical catch into possession. Blue selection transfers at launch. In the C trial the manually selected athlete stays selected through the catch. | A palm that stays hidden until ownership, a torso still facing away at contact, a ball separated from the hand or a switch that snaps back immediately. |
| **Stationary Dry Pass** | Press **B**. After the catch tap **Space** to try the immediate intentional shot. | Compact preparation, a visible catch and a responsive shot from the new carrier. | A delayed pose that blocks a valid shot or a throw without human input. |
| **Moving Receiver** | Press **T**. The lab gives the receiver a short swim away from the passer and sends the ordinary pass. | The receiver checks the stroke and turns before catching if the arm can meet the pass. | An uninterrupted freestyle stroke with the ball suddenly in the hand. |
| **Lateral Receiving** | Press **T** for a sideways moving outlet. | A shorter reorientation and a lateral reach appropriate to the incoming ball. | A reach that puts the hand behind the body or passes through the chest. |
| **High Receiving** | Press **L** for the existing lob. | Earlier awareness of the arc, tracking upward and a reach when the ball enters the playable range. | Hands following the ball far above actual reach or the flight bending toward the receiver. |
| **Missed Pass and Recovery** | Press **B** along the initial heading into open water. Swim toward the ball after the splash. | The dry pass becomes a loose surface ball and the existing automatic pickup recovers it when eligible. | A ball steering toward the offset teammate or posing an airborne catch after the splash. |

For a manual movement override in the long pass fixture, hold an arrow across the incoming lane immediately after the selection ring transfers. A substantial change in position can leave the physical pass unreachable. Restart the fixture for a clean comparison. [Facing-away Game View sequence](Evidence/Milestone30F/Receiver-Facing-Away-Sequence.jpg)

## Recovery

Stop Play. The lab restores Boot automatically. If a direct Editor play override was interrupted, choose **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**. Reopen the production arena with **Tools > Water Polo > Arena > Open Olympic Match Arena**. Source recovery starts from the user-approved `dcfe2ea` with the exact changed-file list. The developer handles Git operations. [Changed files](MILESTONE_30F_FILES.txt)
