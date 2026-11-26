# Milestone 30B ball control and player switching

## Possession and throwing

`WaterPoloThrowPose` uses the final evaluated hand on the production Humanoid rig. The relaxed target sets the ball centre at its established floating height and turns the palm down. Its finger joints fan and curl for contact. Slow travel blends toward a compact raised carry, while sustained forward crawl and sprint can continue to lead the ball at the surface. Stopping gathers into the grip. Charge and a protected retreat blend toward rear loading, with chest rotation, an elevated forearm and the head directed toward play.

`BallController` remains the only ball authority. After Animator evaluation, the rig publishes the final palm plus one radius along its normal. Held physics and render transforms use that point with Rigidbody interpolation disabled. The gather interpolates the old surface or catch point into the grip. The existing release phase launches the same ball from the evaluated grip and restores normal flight. Charge release carries a fully loaded upper body into acceleration. The existing one-request gate and pause/reset cancellation stay in the release path.

Dry passes predict a hand point from the receiving athlete's facing, position and velocity, with lead capped at 1.65 m. The pass flies ballistically after release. The established 0.57 m receiving target and swept catch region remain in use. The receiving rig rises, tracks and reaches toward the ball as it approaches that region, then absorbs the incoming motion. The verification passed stationary and moving catches and a facing defender's physical interception. In three close-up cases the first captured frame after the swept catch could still show about 0.17–0.34 m between the visible palm and the ball surface. The shortest sampled approach error was 0.086 m. The visual contact timing needs further character polish. [Rendered pass sequence](Evidence/Milestone30B/Passing-and-Catching.jpg)

## Directional movement and selection

- Keyboard and mobile sprint use the same stamina request. The movement adapter passes camera-relative input through the usual movement mapping. While holding the ball, a backwards dot below −0.66 enters protected retreat. A dot above −0.32 exits it. Upright eggbeater and the protected carry replace crawl in that state. The motor retains the swimmer's facing while retreating.
- Backward sprint releases one physical loose ball at the held position, with no forward launch impulse. The previous holder has a 0.85 s recovery suppression. Other eligible swimmers use ordinary pickup. Rest and unrelated UI controls leave the current ownership alone.
- The intended friendly recipient can be selected at physical pass release. A manual C/SWITCH choice has a 1.05 s protection interval through a field-player catch. The Blue goalkeeper can still become controllable on legitimate keeper possession.
- Open-play loose-ball selection checks every 0.30 s. The current athlete is favored within 2 m. Candidate estimates use projected ball points at 0.30 and 0.80 s, reachable pickup distance, approach speed and the turn needed. A candidate must arrive within 1.60 s and beat the current athlete by at least 0.60 s. Picks exclude barred field players and the voluntary dropper during suppression. Opponent-held balls, protected phases and live fast shots retain the current selection. Keeper selection stays tied to possession.

The thresholds are serialized on `SwimmerMotor`, `BallController` and `PlayerSelection`. [Manual test guide](MILESTONE_30B_MANUAL_TEST_GUIDE.md)
