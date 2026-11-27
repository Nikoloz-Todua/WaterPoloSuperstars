# Milestone 30E final report

## Task one — Protected possession

30D derived its carry pose mainly from speed and retreat. The motor could accelerate to swim speed before the ball lifted, and the short-carry pose still held the ball forward. During active translation the input adapter usually stopped supplying facing intent. Retreat then kept its prior yaw even when a second control had updated the stored aim.

The existing motor now raises the live field player's grip before applying purposeful translation. Standing still retains eggbeater and the palm-over-surface ball pose. A short adjustment, deliberate turn or throw preparation uses the existing rear-loaded pose on the Humanoid rig. The default cap is 58% of ordinary speed for raised movement and 54% for retreat. The same weighted yaw turns the athlete and the shoulder-relative load. The authoritative ball samples the evaluated hand after the rig updates. Retreat keeps the current travel direction while the player aims independently. Changing travel input exits that retreat. When movement stops, it decelerates before the grip lowers. Shot charge can keep its raised preparation.

Aligned forward input must sustain enough speed and dwell to enter the existing ball-leading crawl. Forward sprint uses a lower entry threshold and the existing sprint stroke. The ball transitions from the hand to the leading point, keeps its owner and gathers back into the grip when the athlete slows. Deliberate backward sprint uses the established drop, flight and same-athlete pickup suppression.

| Focused Windows Editor result | Observation |
| --- | --- |
| Idle and brief forward/lateral movement | Palm above the partly floating ball at rest. Translation followed the lift. Forward and lateral samples reached 2.21 and 2.01 m/s with the ball centre 0.35 and 0.33 m above its surface-rest height. |
| Left, right, 180-degree and full-circle turns | The holder stayed in place within the 0.035 m test tolerance and kept possession. Sampled settled ball-to-grip error rounded to 0.0000 m. Nearest sampled ball centre to head joint was 0.513 m. No turn released the ball. |
| Retreat and lateral turn | Backward eggbeater with raised possession. Independent lateral aim turned the body through a quarter turn while preserving travel at 2.16 m/s. A subsequent change of movement direction exited retreat. |
| Sustained forward, sprint and stop | Leading began after 0.55 s at normal input and 0.21 s with sprint. Sustained speeds reached 4.00 and 6.00 m/s. Both gathered and returned to the surface hold on stopping. |
| Voluntary backward-sprint drop | One physical drop. The nearby previous holder could not collect it after it settled within the suppression window. |
| Quick and full Space shots | Each command produced one physical human release from the evaluated grip. Throwing shoulder rise at release measured 0.251 m and 0.638 m. |

[Protected possession contact sheet](Evidence/Milestone30E/Protected-Possession.jpg) · [Held rotation contact sheet](Evidence/Milestone30E/Held-Rotation.jpg)

## Task two — Directional control

30D's five-point line had a shaft and displayed requested aim ahead of body yaw. The targeted pass could subsequently take its bearing from the predicted hand. Its former wind-up delay estimated rotation time without checking actual alignment at physical release.

The chevron consists of three local line vertices, two diagonal segments and one point. It sits just outside the selection ring using the same green material, inheriting the selected athlete's interpolated orientation. Keyboard movement and joystick steer in camera space. Pull opposite the current facing to retreat. Left Alt plus WASD/arrows aims in place. Numpad 4/6/8/2 provides independent left/right/up/down aim while moving. Press B for a dry pass or Q+B for a wet pass. On mobile, drag PASS to turn while retaining joystick travel, then lift the PASS touch to throw. Tapping PASS aims from the accepted heading and holding it stationary for 0.32 s requests a wet pass.

The existing configurable assist cone is 35 degrees in the production arena. Target selection ranks eligible teammates in that cone by angle, range and lane clearance. A live human pass binds the requested heading at input. Its existing wind-up waits for the rendered body to come within 24 degrees. Release requires it to come within 8 degrees. If a predicted or newly ineligible receiver leaves the cone, the physical pass follows the accepted heading into open water, the same fallback as when no teammate qualifies. After launch the normal ballistic flight, interception and water response apply. Shot charge and its modifiers remain in place. The shot keeps the selected horizontal bearing, and goal geometry still informs its existing vertical calculation.

| Focused Windows Editor result | Observation |
| --- | --- |
| Both cameras and both teams | Lengthwise and Sideline Broadcast each passed forward, backward, left, right and diagonal aiming in isolated Blue and White human-control fixtures. Maximum chevron-to-body angle error was 0.020 degrees. |
| Left/right assisted passes | Eight HumanInput dry passes selected the intended outlet, with initial yaw within the test tolerance and physical release origin matching the grip at displayed precision. Keyboard passes pressed at the start of a quarter turn released in 0.35–0.39 s and flew 8.9–9.0 degrees from accepted heading toward the receiving hand. |
| PASS drag during retreat | The existing UI accepted a synthetic mouse drag while independent retreat input remained held. The athlete turned and the pass released 0.16 s after pointer-up. The shifted outlet was 27.1 degrees from accepted launch heading after lead and hand offset, inside the 35-degree cone. |
| Open water | With teammates staged behind the passer, no receiver was selected and initial flight matched the accepted heading to displayed precision. |

[Directional passing contact sheet](Evidence/Milestone30E/Directional-Passing.jpg)

## Final project condition

- Production C# compiled on Unity **6000.6.7f1**. The active saved scene is clean OlympicMatchArena with GUID `5c4d3741df6b7f54e8cfd34d391d78d1`. Its file and the real player profile were byte-identical by before/after SHA-256. Existing arena scene references and assets are used by the shipped paths.
- Unity is in Edit Mode with Boot as the normal Play entry. The final Console had **0 errors and 0 warnings**. Test Lab runs and verification scripts have ended. No full match ran.
- Runtime fixtures and actual Game view frames provide Windows Editor evidence. The simulated White human-control assignment was confined to verification. Physical simultaneous touches and Android performance remain untested. Personal approval of the carry, turning and dribbling transitions remains open. Inverted freestyle legs, receiving preparation and the remaining roadmap stay deferred in `plan.md`.
- The production changes are in `SwimmerMotor`, `SwimmerInput`, `RiggedSwimmerVisual`, `BallController` and its professional throw partial, `PlayerSelection` and `SwimmerPassing`. The Editor change extends `Milestone30BPreview` and the focused script is `Tools/Verification/Milestone30EVerification.cs`. [Exact file inventory](MILESTONE_30E_FILES.txt)
- In Edit Mode, use **Tools > Water Polo > Test Lab > Ball Control and Player Switching > Directional Aim and Held Rotation** or **Tools > Water Polo > Test Lab > Shooting and Passing > Directional Passing**. The manual guide lists inputs and visible defects to watch for. Use **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** if a direct run is interrupted. Source recovery can use the human checkpoint `3974e80` and the exact inventory. Git writes remain a human operation. [Manual guide](MILESTONE_30E_MANUAL_TEST_GUIDE.md) · [Live handoff](MILESTONE_30E_LIVE_HANDOFF.md)

