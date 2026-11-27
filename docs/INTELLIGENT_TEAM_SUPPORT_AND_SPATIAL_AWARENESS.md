# Intelligent team support and spatial awareness

## Live responsibility

`PrototypeTeamTactics` keeps the formation, goals and assignments for each team. Previously an attacking athlete mostly travelled to a shifted formation slot. The slot did not account for a blocked lane, a nearby marker or another outlet's destination. When the athlete arrived, the AI cleared its facing command and the rig only occasionally glanced toward a nearby carrier.

In live team possession, the tactics component takes one field-position sample every 0.20 s. It shares the cached carrier, eligible athlete positions and allocated destinations with its field players. Carrier changes and manual selection changes invalidate support reservations on the next decision. Ball position, velocity, pool bounds, match phase and selection come from the existing ball, roster, match and selection systems.

Each outlet considers its role anchor, a nearby hold position, its current destination and a small set of lateral and depth offsets. Projecting opposing field players onto the carrier-to-outlet segment estimates lane clearance. The score also accounts for pressure at the destination, physical and reserved teammate separation, distance from the carrier, distance from the role anchor and travel cost. Small changes retain the previous pocket. A committed relocation lasts at least 0.65 s unless the carrier moves more than 1.5 m, possession changes, selection changes or the phase resets.

Centre forward stays within a tighter neighbourhood of the formation slot. Wide roles keep their side and width, flats offer angled lanes and centre back provides the deeper outlet. The candidate guard leaves 2.15 m to the goal line for off-ball support. The existing match remains responsible for fouls and eligibility. The existing counterattack shift, Press and Zone marks, centre defense and goalkeeper responsibilities still supply their respective decisions.

The existing motor steers and resolves physical athlete contact. On arrival a free teammate treads and turns toward the carrier with the weighted motor yaw. In motion the rig weights a brief glance by the direction of the ball relative to the stroke. The head stays within the body’s available viewing arc. A defending athlete shades its gaze between the ball and its assigned opponent and continues to follow its defensive movement assignment. Selection clears support commands before human movement. Human-team AI may hold a ball while manually deselected. Its autonomous throw gate remains disabled, and opposing AI still uses its pass and shot decisions.

## Design references

The useful public FC concepts were spatial evaluation around the carrier, flexible role positioning and contextual supporting runs. Water polo uses a smaller set of role-constrained supporting swims and a direct geometric lane test. The code is specific to this project. [EA FC 27 Gameplay Deep Dive](https://www.ea.com/games/ea-sports-fc/fc-27/news/pitch-notes-fc27-gameplay-deep-dive) · [EA FC 26 Gameplay Deep Dive](https://www.ea.com/games/ea-sports-fc/fc-26/news/pitch-notes-fc26-gameplay-deep-dive) · [World Aquatics competition regulations](https://www.worldaquatics.com/rules/competition-regulations)

## Windows Editor check

In **Tools > Water Polo > Test Lab > Team Tactics > Intelligent Team Support**, the carrier moved 2.96 m via a queued keyboard event. Two stationary outlets faced the carrier. The marked athlete's original lane was blocked and the new destination 3.09 m away had a clear sampled lane. Four of five support targets changed. Closest teammate separation was 3.03 m before the move and 2.90 m after it. The carrier kept possession and selection. The run counted zero human-team throws. The shared support marker averaged 0.059 ms for five samples in the Windows Editor. This covers the sampled tactical work. [Game View evidence](Evidence/Milestone30F/Team-Support-Overview.jpg)

The first run ended with a legitimate defensive steal during the observation window. The menu fixture now starts the pressure farther from the carrier to leave time for the assessment. The verification covers one attacking pattern in the production arena. Lane evaluation uses field-player positions and geometric clearance, so it can degrade as defenders move. Continuous match balance, visual acceptance on the developer's setup and physical-device performance remain open.
