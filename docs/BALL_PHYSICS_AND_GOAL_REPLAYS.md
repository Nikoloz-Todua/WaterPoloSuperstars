# Ball physics and goal replays

Milestone 23 runs in OlympicMatchArena on Unity 6000.6.7f1. The recovery scene OlympicMatchArena_PreMilestone23 matched the production scene byte-for-byte before authoring and sits outside the build list. Both scenes share gameplay scripts.

## Audit and design references

The Milestone 22 commit at entry was 7e5a2aa. The arena already contained the saved penalty aim, substitution UI and closer sideline camera. Its milestone document described the physical referee and penalty path while plan.md still opened at Milestone 20. This handoff records both. The new work keeps the five-metre origin, physical keeper, player status authority and existing camera profiles.

The previous ordinary pass called Release while the body was constrained to the surface with gravity disabled. Targeting discarded the Y component and reception tested a planar circle around the swimmer. In the captured 7 m baseline the ball was at the 0.07 m surface at 5.74 m/s. This explains the skim directly. Lob already used a separate air flight path.

Dry passes, wet placements and raised receiving hands follow the fundamentals described by [SportSG](https://www.activesgcircle.gov.sg/learn/aquatics/basic-ball-handling-in-water-polo) and [Monte Nitzkowski](https://www.waterpoloplanet.com/html_monte_pages2mn06_learning_the_basics1-html/). Presentation uses original arena assets and takes pacing and easy skipping as general cues from [EA's celebration guide](https://help.ea.com/en/articles/ea-sports-fc/how-to-celebrate/) and [First Touch Games' replay support](https://support.ftgames.com/hc/en-us/articles/214385405-How-do-I-watch-my-saved-replays).

## Play controls

| Action | Keyboard and controller | Touch |
| --- | --- | --- |
| Dry pass | B or North | Tap PASS |
| Intentional wet pass | Q+B or LB+North | Hold PASS for at least 0.32 s and release |
| Higher lob pass | L | LOB |
| Charged shot | Hold and release Space or South | Hold and release SHOOT |
| Skip shot | Q+Space | SKIP shot mode |
| High, slower lob-style shot | R+Space | HIGH shot mode |
| Advance a goal presentation stage | Enter or right shoulder | Tap the replay view |
| Finish the presentation | Backspace or East | SKIP ALL |

The centre restart always uses a dry centre pass. Live auto-selection moves to the intended human receiver while the ball is still in flight. The receiver can reposition or switch players. A catch may also trigger the ordinary possession-based selection.

## Physical ball and receiving hand

BallController owns Held, Released and Free. Airborne, pass delivery, shot type and existing support or stoppage flags describe the current movement without creating another physics authority. On release the ball starts from a raised, direction-aligned socket. A dry target is the receiver's raised hand region plus bounded motor-velocity lead. The normal travel time is based on distance and pass speed, limited to 0.30–1.12 s. Launch velocity is displacement divided by time minus half the gravity vector times time. Unity's rigidbody advances flight with gravity and light air damping.

The intended athlete and eligible opposing athletes can catch a dry pass. The ball sweeps its actual travelled segment against a 0.31 m hand region, with speed, arrival and eligibility gates. The earliest valid contact wins once. The visual rig raises the right hand through the existing Animator IK layer. After catching, the ball settles into a raised grip socket on the swimmer. Throws drive the existing pass/shoot triggers and a brief visual turn toward the release. Player models and the water material remain the authored assets.

Wet passes target water just ahead of a moving receiver or slightly short of a stationary one. Higher lob passes use the same release and catch path with a longer flight. A failed catch can hit the water and remain available for ordinary collection. An impact emits the existing splash, caps post-pass speed to 2.35 m/s, and applies stronger surface damping. Only an explicit skip shot receives the first-impact bounce response.

Normal and powerful shots launch above water along the swimmer's aim and use charge-dependent speed. The vertical component targets a playable height at the goal plane. The high shot trades horizontal speed for a higher arc. The keeper still uses swept contact and the match still requires the full ball to cross within the goal frame. The five-metre penalty plane and aimed vertical target remain explicit. Release listeners receive the established flight rather than a transitional surface state.

## Recorded goal presentation

GoalReplayDirector observes the authoritative scored goal. A 20 Hz recorder samples a bounded ten-second ring with 202 slots. The arena records 1,024 world or local poses per sample, including the ball, each existing rig's placement and animated transform hierarchy. The frame header holds time, ball owner slot, state, delivery, release type and shot sequence. Cap IDs allow a replacement boundary to be played as a cut. The final sample records the actual physics position at the accepted goal crossing.

The numeric buffer is 5,802,652 bytes (5.53 MiB) with the production roster. This excludes Transform, GameObject, Renderer and collection overhead. The visual actors are pooled once per arena load, share the existing meshes and materials, and have no colliders, rigidbodies, animators or gameplay components. Rendering interpolates positions and rotations. Playback uses the existing render camera and temporarily hides original ball and athlete renderers.

The sequence starts with a roughly 2.7 s scorer view and a procedural arm pump on the visual copy. Each angle then plays the same clip with up to 4.6 s of recorded build-up at 0.95×, followed by a brief hold. The elevated sideline camera tracks the attack. A diagonal goal-line camera frames the shooter, keeper and mouth from the opposite end of the action. Short clips automatically use the available history. If a clip is too short, celebration proceeds directly to restart.

The presentation uses the match's pause-aware presentation clock. GoalRestart stays locked until the presentation ends. Skip clears the visual copies and returns to the configured live camera before the conceding team takes the existing centre pass. A penalty awarded at the period horn completes the period after its goal presentation. Pause and restart use the existing menus, and replay Pause opens that same Pause menu. Pause → Settings → Auto Replay controls the two angles. It defaults to On using the existing presentation preferences key and leaves the short celebration available when Off.

RecordedGoal carries an in-memory match GUID, scoring event ID, attacking team, validated scorer identity and cap, period, clock and clip duration. Scorer attribution follows the same-team final shot across frame or keeper deflection. Ambiguous possession or non-shot goals get a team presentation. A future Save Goal can version and copy the recorded time window before the ring is reused. This milestone exposes no save control and writes no replay media or progression data.

## Editor workflow

Open **Tools → Water Polo → Arena → Open Olympic Match Arena**. The saved scene already has the overlay. **Tools → Water Polo → Match UX → Set Up Milestone 23 Goal Replays** is a repeat-safe Edit Mode command. For manual gameplay use **Tools → Water Polo → Play Olympic Match Arena**, take a pass and score. Let both angles finish or step through with a tap and Skip All. Use **Tools → Water Polo → Play Hub Entry** for the regular offline flow.

Run each Task-returning verifier with the arena in advancing Play Mode.

```powershell
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.Passes
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.WetMissAndLob
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.Contest
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.TouchPass
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.WhiteAI
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.Distribution
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.Shots
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.Replay
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.Automatic
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.ReplayControls
unity command script run --file Tools/Verification/BallReplayVerification.cs --entry BallReplayVerification.SafeAreaViews
```

## Scope limits

Keeper catches, physical frame interactions and slot swaps are game decisions. The replay records the rig pose and ball, and uses the live arena backdrop. Water particle history, net deformation, skinned blend shapes and bench-only visual proxies are outside the snapshot format. Catch animation uses a tuned raised region and one-handed IK with the current models. Dedicated catch clips, grip animation and celebration clips can replace these poses later. Windows Editor evidence covers simulated touch and pointer interaction, with native mobile performance, physical notch layout and thermal testing outstanding.

## Windows Editor results

| Check | Observed result |
| --- | --- |
| Old 7 m pass | Gravity-off trajectory stayed at y=0.07 m |
| B pass, 3.5 m / 7 m / 11.2 m | Actual peak y=0.666 / 0.972 / 1.726 m. One transfer per catch. Selection changed before the ball arrived |
| Close-up catch | Physical approach and catch captured. After settling, ball centre at y=0.440 m coincided with the rig's right-hand IK target to within 1 mm |
| Team and defensive cases | Moving receiver, opposing swept interception, White AI dry delivery and AI goalkeeper distribution passed |
| Alternate deliveries | Simulated touch tap/hold selected dry/wet. Wet placement landed loose. A displaced lob receiver missed and left a loose ball |
| Shots | Physical regular and powerful goals, high goal, skip bounce and goal, five-metre penalty goal and forced-probability keeper catch passed |
| Goal replay | Blue and White goals, actual pass and shot samples, two angles with the same ID, both automated transitions, mouse/touch/keyboard/gamepad advance, rapid-tap debounce, pause, Skip All, Auto Replay Off and preference reload passed |
| Exceptional goals | Period-ending penalty scored once and advanced to the quarter break after playback. A staged keeper deflection close to the horn retained the actual shooter and scored through the normal detector |
| Existing rules and cameras | M22 timed and early re-entry and third-personal replacement fixture passed. F6 switched both views, F8 remained available, and the selected view returned after replay |
| Canvas | 1920×1080, 2340×1080 and 2400×1080 Game views passed the overlay panel bounds and CanvasRenderer checks |
| Hub | Real pointer flow restarted and abandoned during playback under a memory repository. Four physical sprint acquisitions, accelerated periods, one receipt, duplicate refusal and lobby return passed. Real progression hash matched |

Fixture positions and selected keeper probabilities were controlled for repeatability. The late deflection test calls the existing keeper deflection entry point, then lets physics, scoring and recording resolve the shot. Windows Editor footage does not give device performance numbers or a live-play balance survey.

The initial test runs caught a wrong effects namespace and Unity's deprecated development-build directive. Both were corrected. A carry-over fixture left AI disabled when a penalty regression restarted, and the older end-period check expected the break before playback. Fresh isolated penalty tests passed after adapting the fixture. Final sampled Unity Console reported **0 errors and 0 warnings**, with earlier compiler diagnostics retained in history and zero runtime errors in the hub loop.

Game-view captures and raw results are in `Logs/BallAndGoalReplays/`. `Pass-Comparison.png` shows the old surface pass, three dry distances and the hand approach and catch. `Replay-Contact-Sheet.png` covers goal confirmation, scorer, both cameras, centre setup and live return. The folder includes individual wet pass, airborne shot, skip shot, keeper save and wide-screen overlay frames.

Additional individual Editor checks use the same verification file and the entry names `HandView`, `HighGoal`, `LateDeflection`, `ReplayOff` and `CameraReturn`. The hub-specific Editor capture runner is `Logs/BallAndGoalReplays/HubReplayVerification.cs` with `PremiumSprintChecks.BootEntry` followed by `PremiumSprintChecks.LoggedHubLoop` while Play Mode advances. `Tools/Verification/PenaltyVerification.cs` retains the Goal, Save and EndPeriod checks.

## Exact project file inventory

- `Assets/Editor/MatchUxSetup.cs`
- `Assets/Scenes/OlympicMatchArena.unity`
- `Assets/Scripts/Gameplay/BallController.cs`
- `Assets/Scripts/Gameplay/BallFlightProfile.cs`
- `Assets/Scripts/Gameplay/BallInteractionInput.cs`
- `Assets/Scripts/Gameplay/GamePreferences.cs`
- `Assets/Scripts/Gameplay/GameplayActionButton.cs`
- `Assets/Scripts/Gameplay/GameplayCamera.cs`
- `Assets/Scripts/Gameplay/MatchNotificationBanner.cs`
- `Assets/Scripts/Gameplay/MatchRules.Substitutions.cs`
- `Assets/Scripts/Gameplay/MatchRules.cs`
- `Assets/Scripts/Gameplay/MatchSettingsView.cs`
- `Assets/Scripts/Gameplay/MobileMatchControls.cs`
- `Assets/Scripts/Gameplay/PlayerSelection.cs`
- `Assets/Scripts/Gameplay/PrototypeMatch.cs`
- `Assets/Scripts/Gameplay/SwimmerBallInteraction.cs`
- `Assets/Scripts/Gameplay/SwimmerPassing.cs`
- `Assets/Scripts/Gameplay/WaterPoloDecision.cs`
- `Assets/Scripts/WaterVisuals/ArenaDeveloperCamera.cs`
- `Assets/Scripts/WaterVisuals/RiggedSwimmerVisual.cs`
- `README.md`
- `Tools/Verification/PenaltyVerification.cs`
- `docs/MATCH_UX.md`
- `plan.md`
- `Assets/Scenes/Recovery/OlympicMatchArena_PreMilestone23.unity`
- `Assets/Scenes/Recovery/OlympicMatchArena_PreMilestone23.unity.meta`
- `Assets/Scripts/Gameplay/GoalReplayBuffer.cs`
- `Assets/Scripts/Gameplay/GoalReplayBuffer.cs.meta`
- `Assets/Scripts/Gameplay/GoalReplayDirector.cs`
- `Assets/Scripts/Gameplay/GoalReplayDirector.cs.meta`
- `Tools/Verification/BallReplayVerification.cs`
- `docs/BALL_PHYSICS_AND_GOAL_REPLAYS.md`
