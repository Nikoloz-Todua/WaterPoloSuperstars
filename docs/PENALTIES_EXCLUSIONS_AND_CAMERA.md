# Penalties, exclusions and broadcast camera

## Production scene

Milestone 22 uses OlympicMatchArena in Unity 6000.6.7f1. The native recovery scene is OlympicMatchArena_PreMilestone22, outside the build. Its scene bytes matched production before editing. It still references the original render meshes. As with other scene recoveries, gameplay scripts are shared.

## Competition markings

The premium arena batch contained 2.55 m flat transverse strips at X ±11.3. Each side had one white halfway strip, two yellow 6 m strips and four red strips at 2 m and 5 m. The saved scene migration checked the plane, triangle span, position, mesh count and triangle count. Fourteen quads (28 triangles) were removed across three copied meshes. The builder omits these faces on future builds. The coping indicators at every original distance, goal-line strips, halfway float, coloured ropes and cores, anchorage, re-entry rectangles, flying areas, floor lanes and T ends remain. Pool bounds, goals, scoring volumes and water materials retain their original assets.

## Rule decisions

The source is [World Aquatics Competition Regulations, February 2026, Part Six](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf). See Articles 2.5, 10, 11, 16, 17, 20.6 and 20.7.

MatchRules accepts a contact action from the existing defensive commands. A shielded close reach through the carrier can report a push. An intentional hold of an off-ball opponent produces an exclusion. Successful ball-only steals and swimmer overlap do not enter this decision path. A reported misconduct exclusion permanently removes the offender. The prototype has no wrestling simulation or separate player misconduct command.

A probable-goal check uses possession, current attacking goal, distance and depth, attacker facing, the offender behind the carrier, and other eligible defenders between attacker and goal. A central carrier driving toward goal inside 2 m can qualify without an active shot charge. A defender impeding a shooting action inside 6 m starts one pending decision. The attacker gets a short opportunity to finish that action. A goal clears the pending foul. An unsuccessful attempt or loss of the opportunity awards one penalty. These are deterministic gameplay approximations of a referee's judgement.

Ordinary fouls grant the existing free throw. Exclusions and penalty fouls each add one personal. A penalty alone creates no exclusion countdown. Third personal on an exclusion disqualifies the athlete and holds the replacement for re-entry eligibility. Third personal on a penalty allows immediate replacement. Match records follow the athlete through bench changes. Restart clears the records.

## Five-metre throw

PrototypeMatch controls preparation, ready, flight, goal and period transitions. The taker and release origin are on the 5 m line. The opposing keeper starts on the line. Other swimmers are staged outside the area with clearance. During a period-ending penalty they are staged outside the field. The gameplay camera blends behind the taker without changing the saved gameplay view.

Use arrows, WASD, left stick or the touch joystick to adjust the goal target. Releasing the stick retains the target. Hold and release Space, controller South or SHOOT. The HUD shows the aim mark and the existing charge meter. ShotCharge sets release speed, and BallController applies the physical launch trajectory. The existing swept keeper contact, post and net geometry and whole-ball scoring decision resolve the outcome. A rebound or miss can return to play. On a period-ending throw the outcome closes the period. Pause preserves the ready phase and cancels an interrupted charge.

White takes its own AI throw. When defending White's penalty, Blue can hold left/right or the joystick before release to bias the keeper's assisted reaction. The keeper moves in response to the flight. No separate goalkeeper possession authority is installed.

## Exclusion and substitution

The existing live-time step advances the 18-second exclusion timer. Pauses, intervals, setup and other stopped phases freeze it. The prototype's 80-second live period still displays 480 seconds. Exclusion time uses the live clock, as does the existing 28-second possession system. A goal authorizes return from any place. A team regaining possession, or receiving a free throw or penalty, also releases its exclusion wait. The existing game has no independent goal-throw award command.

The athlete loses selection, collision and ball eligibility immediately. The swimmer travels toward the east corner behind the currently defended goal. During play, eligibility and arrival at the proper gate are separate conditions. The return movement crosses back into the field before normal duties resume. The HUD and LED cap rows read the same player status.

Use **Pause → Substitutions**. Tap the OUT and IN rows to cycle players, confirm, and resume. The carrier must release the ball before a normal exchange. The available reserves come from the owned collection outside the lineup. Direct arena play and the AI use existing catalog definitions as match-local reserves. Goalkeeper and field eligibility are distinct. The tactical scene slot, its original PlayerId, AI bindings and controller references stay intact. Its current athlete, collection identity, cap and personal record change at legal entry. No substitution writes to a club profile.

For a live exchange the departing swimmer goes to the bench-side flying area in the defended half. A presentation-only copy of the existing animated skin travels from behind the goal. Both must reach the zone and complete the provisional above-water hand-contact pose before transfer. Exclusion replacements wait at the separate re-entry corner. The slot then swims into play. The departing visual follows the existing team facility route toward a bench anchor. MatchRules exposes departure, arrival, contact, entry and bench notifications. Bench sitting and deck locomotion use interim presentation. AI chooses the first eligible reserve after permanent disqualification.

## Sideline camera

The old 22 m horizontal offset stayed fixed at minimum zoom. Vertical clamping and a midfield tracking limit also kept shots wide. The new profile moves the camera with the controlled swimmer and ball. Its ordinary horizontal setback spans approximately 9.46–21.56 m, with a 14.0 m default. Base height spans 8.16–17 m with an 11.48 m default before the independent height multiplier and adaptive framing. Zoom can add limited height for the active players and goal. A small dead band and damped tracking reduce changes during ordinary possession.

Use **Pause → Settings → Camera View → Sideline Broadcast**. Camera Distance uses 0.85–1.25 with default 1.00. Sideline Height uses 0.80–1.25 with default 1.00. The existing preferences store persists both independently of the club save. F6 switches the live preference. F8 retains inspection views. The wide opening and lengthwise profile remain available.

## Verification commands

From the project root, open **Tools → Water Polo → Arena → Open Olympic Match Arena**. The saved production scene already contains the panel and reticle. **Tools → Water Polo → Match UX → Set Up Milestone 22 Substitutions** reapplies missing M22 interface wiring in Edit Mode. Use **Tools → Water Polo → Play Olympic Match Arena** for isolated play. Run each asynchronous check through script run with the arena in advancing Play Mode.

```powershell
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.Goal
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.Save
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.Post
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.Run
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.Touch
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.AdvantageGoal
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.EndPeriod
unity command script run --file Tools/Verification/PenaltyVerification.cs --entry PenaltyVerification.ThirdPenalty
unity command script run --file Tools/Verification/MatchExchangeVerification.cs --entry MatchExchangeVerification.WhitePenalty
unity command script run --file Tools/Verification/MatchExchangeVerification.cs --entry MatchExchangeVerification.Exchange
unity command script run --file Tools/Verification/MatchExchangeVerification.cs --entry MatchExchangeVerification.Discipline
unity command script run --file Tools/Verification/MatchExchangeVerification.cs --entry MatchExchangeVerification.Height
```

The tests stage contact, possession and player positions for repeatability. The keeper-catch fixture forces the save and catch probability. The goal fixture disables keeper decision-making. Neither measures save balance. The discipline fixture advances the live clock directly for timed exits and stages a real ownership change for early release. The substitution and height tests use real pointer events. The end-period fixture advances the clock to the horn. The separate official integration run uses an isolated memory profile and accelerates period endings.

## Remaining scope

The authored re-entry geometry is narrower in depth than the 1.0–1.1 m specification in Article 20.7. The requested field cleanup preserved it. Existing starter cap assignments and character skins are inherited. This is a focused simulation of the requested offences and restart procedures. Full disciplinary-card administration, timeouts, last-minute coach options and the broader competition regulations are outside the implemented rules. Hardware performance, native mobile inputs and every camera occlusion have not been measured.

Future A is a passing rework after the developer describes the specific problems. Future B covers cinematic multi-angle goal replay, playback and possible saving of important goals. Future C replaces interim substitution movement, deck travel and bench presentation with custom Blender/Meshy athlete animations.
