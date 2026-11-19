# Milestone 26 — Ball physics and boundary restarts

The saved **OlympicMatchArena** uses one physical match ball, referee-awarded restarts and the existing goal and replay systems. Launch with **Tools > Water Polo > Play Olympic Match Arena**. To preview the full start path, choose **Tools > Water Polo > Play Hub Entry** and launch a match from the lobby.

## Ball and water

The old controller reflected air and surface motion at the field limits. The hidden barriers either side of each goal could also return a ball using the shared 0.70 impact restitution. The earlier settled-water path used different resistance for some types of contact.

- A drop leaves the visible grip and falls under gravity. Ordinary downward energy is absorbed at the first water contact. The ball takes its floating pose at the swept contact and slows from its remaining horizontal velocity. Surface damping is at least 2.65 per second.
- Typical landing limits are 2.35 m/s for a missed dry or lob pass, 1.90 m/s for a wet pass, and 3 m/s for an ordinary shot. Existing splash and ripple observers receive the real water impact once. Dry catches, high shots and in-air drag use their established paths.
- An intentional skip retains its original shallow-angle eligibility, retention and restitution. It gets one powered bounce. The next landing settles like a normal shot.
- The shared solid impact material now has 0.36 restitution. Posts, crossbar, nets and athlete bodies remain physical. The goal-side containment barriers ignore only the match ball in the officiated arena.

## Whole-ball exit and restart

`PrototypeMatch` first checks the existing goal-mouth sweep. A legal score wins over a boundary award. A partially submerged, floating ball can cross into the opening, with whole-ball horizontal and crossbar clearance still required. The separate field sweep uses the configured 20 × 25 m court. Touching the plane or having part of the ball inside leaves play live. Current team tactics map the crossed goal line to the defending team after the halftime reversal.

| Exit and last action | Award |
|---|---|
| Sideline | Free throw for the other team from the exit location. The HUD says **THROW IN**. A field player blocking a shot over the sideline earns the throw for the blocking team. |
| Goal line outside the mouth | **GOAL THROW** to the defending team for a wide attacking release or a defending field player's shot block. The replacement is positioned in the defending two-metre area. |
| Goal line off a defending keeper, or following a defender's controlled release or intentional non-shot touch | **CORNER THROW** to the attacking team at the two-metre sideline mark nearest the exit. Attacking players are cleared from the goal area before the throw. |

The referee records the last eligible player's actual contact separately from goal attribution. Passive body deflections and explicit ball actions retain separate classifications. The interpretation uses Part Six, Articles 7, 9, 12, 13 and 14. [World Aquatics Competition Regulations, February 2026](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf)

A stoppage freezes the live match and shot clocks. A new possession receives 28 seconds, a corner receives 18, and an eligible attacking restart after an opposing intentional touch or a keeper's sideline shot block receives 18. An unchanged possession can keep its remaining time on a passive pass deflection. A shot through the sideline otherwise resets to 28. A wide penalty enters the same exit logic, with the existing end-of-period penalty taking precedence at the horn.

## Replacement and controls

The referee hides, stops and disables the one ball's collider at the exit. Once the existing setup delay expires, the same ball appears at the legal mark and is assigned to the closest eligible taker. The player can use **PASS** or **LOB**, or take a direct shot where allowed. White and the existing delayed fallback use an actual pass. Live time and gameplay resume on release. A future attendant can observe `MatchRules.BallReplacementRequested` with the immutable exit, team and restart mark.

## Windows Editor verification

| Check | Evidence |
|---|---|
| Baseline | An outward shot reached the right boundary and came back at about −9.0 m/s along the same axis while the match stayed live. [Baseline](../Logs/BallPhysicsPolish/Before-Physics.txt) |
| Water | Drop landed once, with vertical speed changing from −2.74 m/s to 0. Missed dry pass, wet pass, lob pass and ordinary shot settled. The skip bounced once with 1.61 m/s upward velocity and 18.9 m/s total exit speed. [Water results](../Logs/BallPhysicsPolish/Water-PASS.json) |
| Boundaries | Both sidelines and both goal lines, no goal on wide shots, locked and hidden ball during setup, one authoritative ball, both cameras, pause safety, real keyboard and AI restart passes. [Boundary results](../Logs/BallPhysicsPolish/Boundaries-PASS.json) |
| Contact attribution | Partial overlap stayed live. Actual field body contacts were followed by controlled outgoing directions for repeatable cases. Side and end awards differed as required. A passive pass deflection preserved 20.82 seconds. Scripted keeper deflections gave an 18-second corner or attacking sideline throw. Q3 checked the reversed defending goal. [Contact and halftime results](../Logs/BallPhysicsPolish/DeflectionsAndHalfTime-PASS.json) |
| Penalty exit | A real awarded five-metre penalty missed wide, gave White a 28-second goal throw, and resumed with no goal. [Penalty results](../Logs/BallPhysicsPolish/PenaltyExit-PASS.json) |
| Goal and save regression | A high shot scored through the existing mouth, and its replay selected the same shot. A separate keyboard-charged penalty was caught by the keeper with the usual intercept logic. [High shot](../Logs/BallPhysicsPolish/HighGoal.txt) · [Keeper save](../Logs/BallPhysicsPolish/PhysicalKeeperSave.txt) |
| Official hub flow | Ceremony, keyboard skip, restart and exit, both goal replay angles and centre restart passed. Four opening sprints and one reward passed with accelerated period endings and an isolated profile. The real progression hash remained unchanged. [Hub results](../Logs/BallPhysicsPolish/Hub-PASS.json) |
| Final Editor state | Unity 6000.6.7f1, saved production scene, one match ball, Play Mode stopped and Boot restored. Current Console had 0 errors and 0 warnings. The capture retains one fixed setup-script compilation error and shows no new runtime errors or warnings. [Editor state](../Logs/BallPhysicsPolish/Final-Editor.txt) · [Console](../Logs/BallPhysicsPolish/Final-Console.txt) |

Initial fixture retries corrected shot contact isolation, sampling the brief AI ready phase and keyboard focus. Final targeted runs passed. Full-duration matches and physical phones were outside the test scope.

For repeatable verification, start a fresh direct arena run for each entry point in [BallPhysicsPolishVerification.cs](../Tools/Verification/BallPhysicsPolishVerification.cs): `Water`, `Boundaries`, `DeflectionsAndHalfTime`, and `PenaltyExit`. These Editor fixtures stage specific shots and contacts. Game View evidence uses the existing render camera, with a closer test angle for the water shots.

[Game View comparison](../Logs/BallPhysicsPolish/ContactSheet.png) · [Normal shot landing](../Logs/BallPhysicsPolish/04b-Normal-Shot-Landing.png) · [Corner throw](../Logs/BallPhysicsPolish/11-Corner-Throw.png) · [Replacement ready](../Logs/BallPhysicsPolish/09-Replacement-Ball-Ready.png)

## Files in this milestone

- `Assets/Editor/GameplayRealismSetup.cs`, `Assets/Editor/OlympicArenaSetup.cs`
- `Assets/Materials/Prototype/RealismBall.asset`
- `Assets/Scenes/OlympicMatchArena.unity` has three targeted serialized value changes. A byte-checked pre-edit copy is stored outside Assets under `Logs/BallPhysicsPolish/`.
- `Assets/Scripts/Gameplay/BallController.cs`, `BallFlightProfile.cs`, `GoalMouth.cs`, `PoolBounds.cs`, `PrototypeGoalView.cs`, `PrototypeMatch.cs`
- `Assets/Scripts/Gameplay/MatchRules.cs`, new `MatchRules.Boundaries.cs` and its `.meta`, `PossessionShotClock.cs`
- `Assets/Scripts/Gameplay/MatchNotificationBanner.cs`, `MatchAtmosphere.cs`, `PoolsideReferee.cs`
- New `Tools/Verification/BallPhysicsPolishVerification.cs`, this report, `plan.md` and `README.md`

The floating model uses a fixed mean height and does not sample render waves. The gameplay has explicit keeper deflections and controlled releases, while field-player body contacts are passive unless gameplay records an explicit action. No attendant animation, new scene or physical-device performance test was part of the milestone.

The working tree remains unstaged and uncommitted, including the earlier pre-match work. [Read-only Git status](../Logs/BallPhysicsPolish/Final-Git.txt)
