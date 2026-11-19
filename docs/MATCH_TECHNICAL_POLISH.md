# Match technical polish

Milestone 24 continues the saved Milestone 23 implementation in OlympicMatchArena on Unity 6000.6.7f1. The original production scene was copied byte-for-byte to `Assets/Scenes/Recovery/OlympicMatchArena_PreMilestone24.unity` before changes. The recovery is outside the build and shares the current scripts. Input Actions and global build settings also have original copies in `Logs/MatchTechnicalPolish/`.

## Keyboard movement

The original player action combined letters and arrows in one digital composite. Both used the same clone, camera conversion, motor and stamina path. The concrete asymmetries were the letter controls' physical QWERTY binding semantics and an EventSystem that could still dispatch navigation during play. A physical binding can resolve to a differently labelled key after a layout change. The original code did not support a conclusion that the two methods had different swimming speeds on a QWERTY device. [Unity Input System keyboard reference](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.Keyboard.html)

W, A, S and D now resolve by their displayed letters. SwimmerInput retains the corresponding physical key as a fallback when the active layout lacks that letter. Both layouts use an explicit normalized 2D composite. Keeper penalty lean uses the same letter-first rule. The match menu owns UI navigation only while visible. Pointer input, stick input, movement facing, speed, drag, sprint and the camera-relative transform use their existing implementations.

The Windows Editor test queued every cardinal direction, a carried diagonal and Shift sprint under both cameras, switched with F6, and checked Pause. Equivalent sequences yielded the same vectors within test tolerance. Steady speed was approximately 4 m/s and sprint speed approximately 6 m/s on both sets. The test used a simulated keyboard device. Physical non-QWERTY keycaps, rollover and host layout switching remain manual checks.

## Score and reactions

The whole-ball GoalMouth crossing and PrototypeMatch still determine the scoring team, score increment and conceding team for the centre pass. BallController freezes the controlling release's team, action, cap and stable player ID. A possession change closes the previous run of attribution. Legal releases keep their original scoring sequence through water, frame and athlete deflections. A live drop can also become an attributable scoring action if it later crosses. Direct scoring from the prohibited restart releases remains gated.

| Situation | Attribution and reaction |
| --- | --- |
| Attacking shot enters the defended goal | Credit the identified shooter and celebrate with that actor |
| Attacking shot enters after goalkeeper or field-player contact | Credit the original attacker as a defensive deflection |
| Controlled defending-team release enters its own goal without opposing contact | Score for the opponent. Show OWN GOAL and the responsible cap. The visual proxy lowers its head. A nearby field player from the scoring team can react without receiving shooter credit |
| Ambiguous release or opponent involvement following a defending-team release | Award the goal to the correct team and keep scorer credit empty. Use a team reaction |

Credit survives a roster substitution between launch and scoring, because the identity and cap are captured at release. If the actor slot has changed occupants before the presentation, the shot keeps its original attribution and the camera uses the team reaction. Own-goal disappointment and celebration use the existing replay-only poses. They do not drive live match bodies.

## Decisive replay moment

Angle 1 retains the existing broadcast composition. Angle 2 starts with a wider diagonal and glides to a field-side view of the opening. It approaches from the side opposite the ball entry lane so the near post is less likely to hide the ball. The recorder includes roughly 0.4 seconds of the existing bounded live net motion after the registered crossing. Both views use those same recorded samples. The final 0.32 seconds before crossing and the net tail play at 0.58×. The rest remains at the established playback speed.

Celebration, mouse or touch advance, Skip All and Auto Replay remain available. Playback has no match colliders and never recreates the physics event. The configured live camera returns before the real centre pass. The replay still uses the live arena backdrop. Net mesh deformation and particles are outside the recorded snapshot data.

### Game-view evidence

- [Before, diagonal goal-line angle](../Logs/MatchTechnicalPolish/Before-Replay-Goal-Line.png)
- [After, ball held inside the recorded net tail](../Logs/MatchTechnicalPolish/After-Replay-Net.png)
- [Blue goalkeeper own goal](../Logs/MatchTechnicalPolish/Blue-Own-Goal.png)
- [White own goal](../Logs/MatchTechnicalPolish/White-Own-Goal.png)
- [Blue countdown and gate](../Logs/MatchTechnicalPolish/Exclusion-Blue.png)
- [White countdown and gate](../Logs/MatchTechnicalPolish/Exclusion-White.png)
- [Halftime exclusion](../Logs/MatchTechnicalPolish/Exclusion-Halftime.png)

## One-click exclusion testing

In Unity Edit Mode choose **Tools → Water Polo → Verification → Exclusion Test**, then choose a case. The command enters isolated OlympicMatchArena play, opens the Game view and waits for the real sprint-off. You can watch the foul, free throw, gate approach, HUD countdown and re-entry. **Esc** or the on-screen Pause button freezes the countdown. Resume continues it. Stop Play Mode when finished.

- **Blue - Full Countdown** applies a Blue off-ball hold and waits for a self-return after 18 actual playing seconds.
- **White - Full Countdown** runs the same sequence for White.
- **Blue - Early Return** uses an actual Blue possession acquisition after the excluded player reaches the gate.
- **White - Third Personal and Replacement** commits three real exclusion fouls. The first two waits are accelerated through the live-time API. The third disqualifies the athlete and the existing White reserve procedure fills the slot after legal entry.
- **Blue - After Halftime** accelerates the first two periods, uses normal side reversal and sprint-off, and runs the Blue exclusion on the new defended end.

During the staged portion, the demo holds tactical AI so an unrelated goal or steal does not decide the case. It restores the AI on completion. Rules, gate eligibility and the display still use the match's real state. It refuses a running hub session and restores the prior Editor Play entry when stopped. It creates no production-player command and uses no persistent profile. The 18-second authority is the existing live-time rule. [Milestone 22 rules and references](PENALTIES_EXCLUSIONS_AND_CAMERA.md)

## Editor menu and scene audit

The audit checked registrations under Assets, editor method and string callers, global build settings, scene assets and GUID references, the runtime destination resource, verification runners and historical docs. The three primary commands remain **Play Hub Entry**, **Open Main Lobby** and **Play Olympic Match Arena**.

| Menu | Disposition |
| --- | --- |
| Arena | Keeps Open Olympic Match Arena, Apply Premium Championship Finish and Refresh Milestone 20 Venue Reflection |
| Match UX | Keeps the active substitution and goal replay setup |
| Animated Athletes | Keeps the isolated swimmer lab entry |
| Verification | Keeps the field audit and water lab. Adds the Exclusion Test presets |
| Recovery → Arena → Earlier Arena Passes | Groups seven superseded arena, field, lighting, broadcast and reflection passes |
| Recovery → Match UX | Keeps the earlier Milestone 16 interface builder |
| Recovery and Legacy Prototype Tools | Keep frontend migration, athlete recovery, development prototypes and prior builders available |

`EditorBuildSettings.asset` enables Boot, MainLobby and OlympicMatchArena in that order. The shipped HubMatchDestination selects OlympicMatchArena. SampleScene remains a source scene and template reference, with its build entry disabled. PrototypePool, AnimatedSwimmerLab, WaterVisualTest and the prior recovery scenes were already outside the enabled scenes. Asset metadata and scene GUIDs are retained. No assets, tools or old documentation were deleted.

## Focused verification

The `Tools/Verification/MatchPolishVerification.cs` entries `KeyboardMovement` and `GoalCases` run in advancing Play Mode after direct arena launch. The `ObserveBlue`, `ObserveWhite`, `ObserveEarly`, `ObserveReplacement` and `ObserveHalftime` entries observe the corresponding clickable exclusion command. White, replacement and halftime use the clock API to shorten the final observation in the automated suite once the gate is reached. The Blue full-duration run used natural live time.

The M23 replay and penalty fixtures are reused for tap-to-advance, camera return, goal and capture verification. The isolated hub check uses the existing memory repository and four physical sprint acquisitions with accelerated period endings. Fixture keeper deflection uses the established keeper contact API. Field deflection uses a staged live collision. They verify causality and scoring, with keeper difficulty and uncontrolled match frequency left to normal play testing.

The active Android build profile has Override Global Scene List disabled. It inherits the three enabled global scenes.

### Moved commands

The following entries moved from Arena to **Recovery → Arena → Earlier Arena Passes**.

- Set Up Authentic Field and Period Sprint-Offs
- Apply Championship Poolside Redevelopment
- Refresh Redeveloped Arena Reflection
- Build Milestone 20 Championship Venue
- Set Up Live Scoreboard and Broadcast Cameras
- Refine Sprint and Championship Lighting
- Refresh Championship Night Reflection

**Set Up Milestone 16 Match Interface** moved from Match UX to **Recovery → Match UX**.

### Registered Water Polo commands after cleanup

The following 56 registrations resolve under Tools → Water Polo.

- `Animated Athletes/Play Isolated Swimmer Lab`
- `Arena/Apply Premium Championship Finish`
- `Arena/Open Olympic Match Arena`
- `Arena/Refresh Milestone 20 Venue Reflection`
- `Legacy Prototype Tools/Apply Verified Water to PrototypePool`
- `Legacy Prototype Tools/Arena/Polish Earlier Championship Venue`
- `Legacy Prototype Tools/Arena/Refresh Earlier Venue Reflection`
- `Legacy Prototype Tools/Create Movement Prototype`
- `Legacy Prototype Tools/Play PrototypePool Directly`
- `Legacy Prototype Tools/Set Up 2v2 Prototype`
- `Legacy Prototype Tools/Set Up Automatic Switching and AI Prototype`
- `Legacy Prototype Tools/Set Up Camera and Ball Prototype`
- `Legacy Prototype Tools/Set Up Friendly AI Prototype`
- `Legacy Prototype Tools/Set Up Gameplay Realism Expansion`
- `Legacy Prototype Tools/Set Up Goalkeeper Prototype`
- `Legacy Prototype Tools/Set Up Goals and Scoring Prototype`
- `Legacy Prototype Tools/Set Up Milestone 10 Match`
- `Legacy Prototype Tools/Set Up Milestone 11 Interface`
- `Legacy Prototype Tools/Set Up Milestone 8 Prototype`
- `Legacy Prototype Tools/Set Up Milestone 9 Match`
- `Legacy Prototype Tools/Set Up Two Swimmer Passing Prototype`
- `Match UX/Set Up Milestone 22 Substitutions`
- `Match UX/Set Up Milestone 23 Goal Replays`
- `Open Main Lobby`
- `Play Hub Entry`
- `Play Olympic Match Arena`
- `Recovery/Animated Athletes/Apply to All 14 Match Athletes`
- `Recovery/Animated Athletes/Apply to One Match Athlete`
- `Recovery/Animated Athletes/Reimport Prototype Source FBXs`
- `Recovery/Animated Athletes/Restore Original Match Visuals`
- `Recovery/Animated Athletes/Set Up Prototype and Lab`
- `Recovery/Arena/Activate Verified Arena for Lobby`
- `Recovery/Arena/Apply Championship Night Overhaul`
- `Recovery/Arena/Earlier Arena Passes/Apply Championship Poolside Redevelopment`
- `Recovery/Arena/Earlier Arena Passes/Build Milestone 20 Championship Venue`
- `Recovery/Arena/Earlier Arena Passes/Refine Sprint and Championship Lighting`
- `Recovery/Arena/Earlier Arena Passes/Refresh Championship Night Reflection`
- `Recovery/Arena/Earlier Arena Passes/Refresh Redeveloped Arena Reflection`
- `Recovery/Arena/Earlier Arena Passes/Set Up Authentic Field and Period Sprint-Offs`
- `Recovery/Arena/Earlier Arena Passes/Set Up Live Scoreboard and Broadcast Cameras`
- `Recovery/Arena/Rebuild Base Olympic Match Arena`
- `Recovery/Arena/Rebuild From Configured Field Dimensions`
- `Recovery/Frontend/Set Up Asset Home Screen`
- `Recovery/Frontend/Set Up Milestone 12 Hub`
- `Recovery/Frontend/Set Up Unified Frontend`
- `Recovery/Match UX/Set Up Milestone 16 Match Interface`
- `Recovery/Open Pre-M17 Arena Scene`
- `Recovery/Set Up WaterVisualTest`
- `Recovery/Use Boot as Editor Play Entry`
- `Verification/Audit Current Production Field`
- `Verification/Exclusion Test/Blue - After Halftime`
- `Verification/Exclusion Test/Blue - Early Return`
- `Verification/Exclusion Test/Blue - Full Countdown`
- `Verification/Exclusion Test/White - Full Countdown`
- `Verification/Exclusion Test/White - Third Personal and Replacement`
- `Verification/Play WaterVisualTest`


## Exact files changed in Milestone 24

This inventory excludes the Milestone 23 work already present at entry.

- `Assets/InputSystem_Actions.inputactions`
- `Assets/Scripts/Gameplay/SwimmerInput.cs`
- `Assets/Scripts/Gameplay/MatchMenuController.cs`
- `Assets/Scripts/Gameplay/MatchRules.cs`
- `Assets/Scripts/Gameplay/BallController.cs`
- `Assets/Scripts/Gameplay/PrototypeMatch.cs`
- `Assets/Scripts/Gameplay/GoalAttribution.cs` and its `.meta`
- `Assets/Scripts/Gameplay/GoalReplayBuffer.cs`
- `Assets/Scripts/Gameplay/GoalReplayDirector.cs`
- `Assets/Editor/ExclusionTestMenu.cs` and its `.meta`
- `Assets/Editor/AuthenticFieldSetup.cs`
- `Assets/Editor/ArenaRedevelopmentSetup.cs`
- `Assets/Editor/ChampionshipArenaRebuild.cs`
- `Assets/Editor/MatchBroadcastSetup.cs`
- `Assets/Editor/SprintPolishSetup.cs`
- `Assets/Editor/PremiumArenaSetup.cs`
- `Assets/Editor/MatchUxSetup.cs`
- `Assets/Scenes/Recovery/OlympicMatchArena_PreMilestone24.unity` and its `.meta`
- `ProjectSettings/EditorBuildSettings.asset`
- `Tools/Verification/MatchPolishVerification.cs`
- `docs/MATCH_TECHNICAL_POLISH.md`
- `plan.md`
- `README.md`

Session-only scripts, source backups, results and screenshots are in `Logs/MatchTechnicalPolish/`.

## Verification results

| Case | Final result |
| --- | --- |
| Same keyboard sequences through both cameras | 12 paired checks passed. Four cardinal directions, a carried diagonal and Shift sprint per camera. F6 and Pause passed |
| Standard goals | Blue and White physical shots each scored once and retained the identified attacker |
| Own goals and uncertainty | Blue keeper pass and White field pass scored for the opposing teams. Responsible caps were retained without shooter credit. An attacker-side pass with uncertain shooter was attributed as Unknown |
| Deflections | Staged swept keeper contact and a physical field-player collision preserved normal attacker credit and used DefensiveDeflection |
| Replay continuity | Matched before and after fixture captured. Game view showed the ball inside the net with the diving keeper. Mouse advance, keyboard advance, both angles, pause, Skip All, camera restore and physical centre pass passed |
| Existing gameplay | Three distances of queued-B dry passing and one transfer per catch passed. Penalty goal and penalty save passed |
| Exclusions | All five real menu presets passed. Natural Blue timing, both teams, countdown freeze, five-player period, early possession, permanent removal, reserve cap change and six players restored verified |
| Official hub | Boot and lobby launch, restart and abandon during replay, White own goal credited to Blue, four physical sprint acquisitions, accelerated periods, one official victory receipt, duplicate refusal, result and lobby return passed |
| Safety and restoration | Production arena bytes still match the pre-M24 recovery. Play stopped, Boot restored, real progression hash unchanged and no fresh hub runtime errors |

The first hub run exposed a teardown exception when Unity destroyed the replay actor parent before the replay director. The cleanup now handles either scene-destruction order. The isolated hub run then passed again with zero runtime errors. A first replacement demo attempted the next foul during a simultaneous shot-clock turnover. The staged sequence now waits for live play before the next incident. The verified replacement restored White to six field players while preserving the disqualified cap history.

The final Unity Console sample reported **0 errors and 0 warnings**. Earlier compiler and fixture diagnostics remain visible in the retained history. Android and iOS device performance, full-duration play, physical non-QWERTY layouts and an exhaustive review of deflection decisions remain outside the verification evidence.

- [Replay and own-goal comparison](../Logs/MatchTechnicalPolish/Goal-Comparison.png)
- [Close replay with keeper and recorded net tail](../Logs/MatchTechnicalPolish/After-Replay-Net-Keeper.png)
- [Exclusion scenario comparison](../Logs/MatchTechnicalPolish/Exclusion-Comparison.png)
- [Isolated hub receipt and profile verification](../Logs/MatchTechnicalPolish/HubLoop-PASS.json)

## Read-only Git status

Milestone 23 local edits were already present when Milestone 24 started. Both sets remain in the working tree, with nothing staged. The existing untracked pipeline settings, SourceAssets, old-plan.md and tatus remain. No Git write operation was performed.
