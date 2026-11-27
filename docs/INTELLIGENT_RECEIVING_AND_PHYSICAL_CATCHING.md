# Intelligent receiving and physical catching

## From request to arrival

30D already exposed an intended receiver while the throw was pending and turned the motor toward the passer. The rig entered its receiving pose only during close airborne flight. The separate AI flight decision could still steer toward another estimate, and automatic selection cleared the swimmer's speed and turn momentum.

`BallController` now exposes one read of the accepted dry or lob pass for the field motor, AI and rig. In preparation the receiver tracks the passer and checks the existing stroke. The accepted receiver and human assist cone still belong to the existing throw pipeline. At release the ball keeps its existing ballistic launch, speed, direction, water behavior and physical collisions. The receiver estimates the approach from the current rigidbody position and velocity using `BallTrajectory`. The launch pocket bounds assisted movement. Heading and time-to-station inform the presented hand. The assist ends when the pass leaves the pocket, passes the athlete, wets, expires, is intercepted, becomes invalid or leaves live play.

The existing weighted yaw brings the shoulders and body into the approach. Head tracking, deceleration and an upright blend can start during the turn. The rig opens the fingers and presents the right palm as the arm becomes accessible. The target varies with trajectory height, lateral offset and time to arrival. The lower body and other arm use the established tread or movement animation. High approaches have more rig elevation, while low and lateral approaches vary the reach and shoulder position. Opponents close enough to a reachable lane can present their hand using the same pose path. The current production grip takes the ball in the right hand. A dedicated left-handed catch and transfer would require additional authored or procedural work.

Automatic selection still happens on physical release. A dry-pass receiver keeps current translation and weighted turn momentum through the handoff while the motor clears the superseded command. **C** can switch away during flight under the existing manual protection. Deliberate movement takes precedence over the assist. A still-eligible receiver can continue preparing under AI after the switch.

## Physical contact and recovery

The authoritative ball sweeps the travelled segment against eligible candidates and retains the earliest valid contact. The rendered rig tests whether its wrist can reach the candidate without full extension or passing the ball through the head. The bounded vertical allowance gives field players a chance to meet a high or low approach. Facing, maximum incoming speed, match eligibility and physical obstruction still apply. The existing competition sphere and ball shell stay in use. The ownership transition makes a single claim.

The first rendered gather frame uses the actual swept contact point before the existing 0.13 s absorption advances. The right wrist solution matches the contact with the palm surface. The remainder of the gather bends the arm inward and settles into normal possession. An immediate intentional action may queue through the existing throw pipeline. Neutral, protected short movement, retreat, ball-leading crawl and deliberate release continue to use the 30E systems.

## Design references

EA's public materials describe receiver commitment options, automatic switching, input authority and contextual animation decisions based on motion and ball distance. The implementation here uses the project's existing weighted turn, live ball physics and Humanoid arm solver. [EA FC 26 Gameplay Settings](https://www.ea.com/able/resources/ea-sports-fc/fc-26) · [EA FC 26 Gameplay Responsiveness Update](https://www.ea.com/games/ea-sports-fc/fc-26/news/pitch-notes-fc26-gameplay-responsiveness-update)

## Verification

See the final report for the advancing Windows Editor measurements, the outcome of each controlled variant and the limits of the rendered evidence. [Technical report](MILESTONE_30F_FINAL_REPORT.md) · [Facing-away sequence](Evidence/Milestone30F/Receiver-Facing-Away-Sequence.jpg)
