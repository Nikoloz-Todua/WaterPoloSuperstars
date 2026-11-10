# Water Polo Superstars — Project Plan and Handoff

Last updated: 2026-11-10.

## Current handoff — M10 gameplay realism expansion

This is the explicitly authorized follow-up to the existing **uncommitted M10 phase**, not a separately committed/pushed milestone. It preserves the previous M10 work and all **14 athletes**. Verification uses the pinned Windows **Unity 6000.6.7f1** Editor; no Android hardware, Blender, paid assets, imported rigs or external services are needed. The earlier M10 handoff below remains technical history, with its rule-profile limits still applicable.

### Architecture and actual behavior

**One BallController remains authoritative.** `BallFlightProfile` contains serializable release/impact tuning plus the shared ShotType and impact-event models. `BallTrajectory` provides pure equations, not another simulation/possession component. BallController runs its flight/water preparation at execution order **-140**, before existing keeper swept contact (-120) and match scoring (-100). PhysX still advances real 3D flight with gravity/CCD; the existing water-plane model handles floating drift. PrototypeMatch alone awards scores/transitions, using the unchanged directional whole-sphere GoalMouth geometry.

| Input/API | Implemented release |
| --- | --- |
| Space tap | Normal direct shot, limited genuine height; default release 0.22m above the floating centre. |
| Hold/release Space | Existing 0.7s charge and 10–22m/s range; charge >=0.65 selects Powerful, default release height 0.35m. |
| Hold Q, then hold/release Space | Skip. Default 0.55m release height and a downward launch chosen for a water strike before the goal. |
| Hold R, then hold/release Space | High. Real gravity trajectory with flight time based on current goal distance/speed and a bounded goal-height target; default release height 0.45m. |
| B | Ordinary water-skimming pass with distance-scaled speed, bounded motion lead and existing angle assistance. |
| L | Real lob; existing duration remains, with configurable minimum arc height (default 1.8m). |

The modifier is latched when charging begins; Q wins if both Q and R are held. Releasing a modifier mid-charge does not change the chosen type. Direct `TryShoot(speed, ShotType)` and `ShotCharge.BeginCharge(ShotType)` expose the same actions to AI/future touch controls. Existing C/F/G/Z/Shift/E/gamepad actions, power bar, keeper shooting and possession selection remain. AI can choose High under nearby pressure and Skip for a clear centre-forward chance at suitable range, through the same APIs and configured AI speed; it has no speed or possession bypass.

**Water impact** uses the actual crossing between successive physics poses and the incoming 3D velocity. A qualifying shallow, fast Skip impact retains 90% horizontal speed and uses 0.42 vertical restitution, capped to a single intentional bounce and 0.6–2.7m/s upward speed. Subsequent water contact settles; it cannot bounce indefinitely. Normal/power shots retain 94% horizontal speed at the transition to existing water drag; soft lob landings retain 32%. These are configurable game values, not claims of full hydrodynamics. The effective water plane is the existing `SurfaceHeight` floating-centre level. Frame/athlete colliders stay enabled in flight. Side/end bounds preserve height, permit valid goal-pocket travel and reject above-bar/outside-post goals.

`PredictPosition` uses bounded measured velocity, gravity, water response (including one predicted skip), and drag. It does not know future input. Pass lead is capped (1.4m ordinary/1.5m lob) and retains angle/cone restrictions; nearby lane blockers reduce assisted target preference. Field reception still requires an actual travelled segment/current reachable position, low descending height, available ownership and match eligibility. Default maximum field reception speed is **14m/s**; keeper swept-contact saves remain separate. Receiver/loose-ball AI swims toward predicted travel/landing positions rather than acquiring remotely. Predictions approximate drag and unobserved collisions and can be wrong.

**Keeper contact** remains physical 3D swept hand-sphere contact with reaction, skill, station and recovery checks. Hand height moves at a bounded rate up to 0.65m and the primitive arms rise with it. Tracking reads the actual held-ball carrier position, avoiding the frozen Rigidbody pose while held. Powerful shots lower ordinary catch probability, while explicit 0/1 tuning endpoints remain useful for verification. Deflections retain 3D motion when above water and can subsequently land or skip. Manual Blue keeper possession/distribution policy is unchanged.

**Posts and net:** the ball/posts/crossbar share a new 0.7-restitution, low-friction PhysicsMaterial asset. Actual colliders rebound shots; scoring dimensions remain 3m × 0.9m. On a validated crossing, PrototypeMatch asks BallController to contain the scored ball with reduced momentum inside the goal pocket during the existing goal pause. It remains visible moving into the net instead of freezing at the plane. `GoalNetReaction` deforms existing non-colliding strands briefly, then restores them; it cannot award a goal. New restarts clear capture/flight state. No replay, cloth solver or invisible scoring shortcut was added.

### Broadcast camera

GameplayCamera remains the only camera controller. It preserves the broadcast yaw through selection/halftime and combines controlled athlete, actual ball/carrier, bounded velocity prediction, current possessing team's goal, and nearby eligible teammates/defenders. A bounded point/frustum calculation widens distance/height when needed at the current aspect ratio. Zoom uses hysteresis and damping rather than tracking every small change. Selection retains the existing smooth target handoff; explicit restart snaps read physics poses to avoid stale rendered restart positions.

Inspector profiles: **Normal Broadcast**, **Wide Tactical**, **Goalkeeper Possession**, **Set Piece**, each with size, ball weighting, maximum zoom and damping. Normal/Wide are the ordinary manual mode choices; keeper/set-piece profiles activate from gameplay context. Existing distance/height/FOV/lead tuning is retained; adaptive framing also has a legacy follow fallback. Minimum/maximum zoom are bounded, so extreme off-ball separation cannot guarantee the whole pool/all athletes remain visible. No uncontrolled halftime rotation or camera shake was introduced.

### Referees and event presentation

Two visible primitive referees stand on observation decks outside the pool at approximately X=±11.4m. PoolsideReferee observes carrier/ball position, rule phases, goals and period changes. It chooses central/advanced/end observation sectors at **0.65s** intervals rather than copying ball Z every frame. Movement uses **3.5m** start hysteresis, **0.5m** stopping tolerance, **2.2m/s** speed, **3m/s²** acceleration/deceleration and bounded end positions. It smoothly looks toward play, pauses for decisions/goals and returns toward midfield at breaks. The current scene's straight deck corridors are clear of goals/walls; this is not dynamic obstacle navigation for future arena furniture.

RefereeVisual implements `IRefereePresentation`; movement holds a replaceable MonoBehaviour presentation adapter. Primitive torso/head/arms/legs provide restrained walking, idle, turning, whistle-hand, foul, exclusion, penalty, goal and period poses. There is no IK or imported rig. Signals subscribe to MatchRules.Decided and PrototypeMatch.Changed; proximity never creates a foul or possession award. All referee/deck visuals have no gameplay colliders. MatchAtmosphere supplies short and stronger original procedural whistles, period/final cues and throttled water-impact audio; it never controls timing. Sound quality/listening remain manual review.

### Water and impact effects

WaterInteractionEffects creates **16 reusable ripple/splash slots** (8 available in Low), two shared procedural meshes, and one reusable wake line per athlete at startup. Events size effects by impact intensity. Skip/save/frame contacts can show small splash spokes; landings ripple. Movement produces small V wakes, with sprint/keeper movement increasing visible disturbance. A short reusable shot trail distinguishes fast releases, while the existing BallHeightCue supplies the airborne shadow. Effects have no colliders, ownership logic or match timers. They expire in 0.9s and clear on restart; no per-frame GameObject/mesh spawning is used. **Off / Low / Standard** quality is configurable; both runtime API and Inspector changes clear active effects/trails. Low keeps only faster wakes and half the impact pool. No fluid simulation, tessellation or large particle overdraw was added.

### Setup and file inventory for this expansion

Saved scene is already configured. Optional command, after stopping Play Mode and saving intended edits: **Tools > Water Polo > Set Up Gameplay Realism Expansion**. It extends the current M10 scene, rejects unexpected/dirty scenes, preserves all identities and useful tuning, wires references, creates the deck/referees/materials/effects/net observers, and avoids duplicate objects on repeat. It does not rerun M1–M10 builders or touch SampleScene.

Created under `Assets/Scripts/Gameplay/`, each with `.meta`: `BallFlightProfile.cs`, `GoalNetReaction.cs`, `PoolsideReferee.cs`, `RefereeVisual.cs`, `WaterInteractionEffects.cs`. Created Editor utility `Assets/Editor/GameplayRealismSetup.cs` with `.meta`; created `Tools/Verification/GameplayRealismVerification.cs` outside player builds. New materials under `Assets/Materials/Prototype/` with `.meta`: `ObservationDeck.mat`, `RefereeWhite.mat`, `RefereeDark.mat`, `RefereeSkin.mat`, `WaterFeedback.mat`, `RealismBall.asset` (PhysicsMaterial).

Modified existing runtime files: `BallController.cs`, `BallInteractionInput.cs`, `GameplayCamera.cs`, `LobPassing.cs`, `MatchAtmosphere.cs`, `PrototypeGoalkeeper.cs`, `PrototypeMatch.cs`, `PrototypeSwimmerAI.cs`, `PrototypeTeamTactics.cs`, `ShotCharge.cs`, `SwimmerBallInteraction.cs`, `SwimmerPassing.cs`, `WaterPoloDecision.cs`. Saved additive wiring/objects in `Assets/Scenes/PrototypePool.unity`; updated README, AGENTS and this handoff. The earlier M10 uncommitted files remain part of the shared review set. No package/security/Git write is authorized or needed.

### Verification and review limits

Fresh compilation passed. **Eight distinct focused entries** have passed in the Windows Editor: five new entries and three reused regressions. One consolidated pass covered the representative systems; only changed/failed checks were repeated afterward. No claim is made that all historical suites or device tests ran.

| Entry | Fresh automated coverage |
| --- | --- |
| GameplayRealismVerification.Setup | Repeat-safe setup, all 14 IDs, two non-colliding referees, one ball/effects authority, two net observers, material wiring and retained tuning. Edit scene has 276 transforms. |
| GameplayRealismVerification.BallPhysics | Normal water transition, real injected Space/Q/R charge controls, high and skip goals, bounded skip/net motion and one score, physical post/crossbar collisions, above-bar rejection, soft lob height/landing, contested low reception and exclusive acquisition. |
| GameplayRealismVerification.CameraRefereesEffects | Controlled player/ball framing at 16:9, 19.5:9, 21:9; manual API/actual-acquisition handoffs; keeper/set-piece/wide profiles; deck bounds, speed, turning and stationary dead zone; foul/high-charge cancellation and penalty signal while repositioning; real pooled impacts, expiry/stable object count, no effect colliders and Inspector quality. |
| GameplayRealismVerification.Integration | Moving-carrier keeper tracking, real high-shot catch and powerful post-skip deflection, airborne shot-clock cleanup/opposing restart, halftime orientation/end mapping, full time/new match and 10s of AI play with exclusive possession/eligible Blue control. |
| GameplayRealismVerification.EffectsQualitySwitch | Active impact exists before Inspector Off; active ripple/splash objects and shot trail clear. |
| Milestone10Verification.FoulsAndExclusions | Actual F-contact foul, charge cancellation, protected free throws, frozen clocks, expiry after release, personal fouls, exclusions/re-entry, permanent removal, new match and halftime history. |
| Milestone10Verification.PenaltiesAndIntegration | Human/AI penalty qualification, physical save/AI goal, man-up goal re-entry, shot/period clock handling, no keeper return teleport and 12s integrated play. |
| FullTeamMilestone9Verification.GoalsAndKeepers | Five physical goals including post-halftime own goal, two catches/distributions at new ends, conceding ownership and all-athlete starts. |

Evidence: entry-specific `Logs/Realism-Final-*.json`. These checks include deliberate isolated physics poses and forced keeper skill endpoints where necessary; they do not imply manual gameplay confirmation. The effect-expiry fixture stops later ball motion after its first actual impact so another legitimate wall ripple cannot obscure expiry. Real frame progression was confirmed with the CLI frame-count wait, not assumed from Play Mode entry. Actual Game-view captures are `Logs/Realism-Broadcast.png`, `Realism-Skip.png` and `Realism-Referee.png`. Final Console/settings/Git audit is recorded below after cleanup.

Known limitations: hybrid water surface and one intentional skip rather than full buoyancy/spin/waves; approximate prediction and bounded pass help; no guarantee every high shot beats or is saved by a keeper; limited hand/arm lift without diving; simple sector-based referee walking on the current clear decks rather than navigation around future furniture; symbolic primitive signals; tonal procedural audio; a fixed-orientation broadcast view with bounded zoom that cannot show arbitrarily separated action. Earlier M10 rule simplifications still apply. Long match balance, every collision angle, simultaneous period/foul edges, audible mix, every screen layout and mobile CPU/GPU/thermal behavior remain unverified. No Android device is required for this Editor handoff.

### Exact manual Unity review

1. Open **Assets > Scenes > PrototypePool**, press Play, click Game. The scene is wired; optional rebuild is **Tools > Water Polo > Set Up Gameplay Realism Expansion** outside Play after saving intended edits.
2. Use E/B/L/C/F/G/Z/Shift/WASD as before. Tap Space, fully charge Space, then hold Q before Space for Skip and R before Space for High. Watch actual height/shadow, water strike, retained skip travel, post/bar rebounds, reachable pickups and single goals/net motion. Charge cancellation on possession loss/foul must not release a stale shot.
3. Pass to moving receivers and contest lob landings. Test actual Blue keeper catch/control/distribution and keeper responses to high/skip shots. Physics and prediction do not guarantee reception/save success.
4. Select **Hierarchy > Main Camera > Gameplay Camera** (the object with GameplayCamera). Compare Mode Normal Broadcast/Wide Tactical; tune the four profiles in Play for review only. Test player handoffs, keeper possession, midfield/goal approaches and long passes at Game-view 16:9 and wider presets. Verify the attacking end remains correct after halftime without camera rotation.
5. Watch **Hierarchy > GameplayRealism > RefereeEast / RefereeWest**: purposeful deck movement, natural starts/stops, facing, and rule-driven signals. Move play between midfield/ends, then provoke F/G fouls and a qualifying penalty; referees must not collide with players/ball or independently decide outcomes.
6. Select **Hierarchy > GameplayRealism > Water Interaction Effects**. Compare Quality Standard/Low/Off while splashes/wakes are active. Confirm effects clear/expire and goals, pickup and restarts remain unaffected. Listen to ordinary/stronger/period whistles and impact volume.
7. Continue through halftime and full time, then Restart Match. Check ends, scores, clocks, exclusions, pending flight/net state and old effects. These are manual Windows Editor review steps; no manual playtest or Android/iOS performance claim is made.

Final audit: compilation completed with **compilationFailed=false**. The last live gameplay/physics checks observed **0 errors / 0 warnings**. After the final Editor domain reload, the current native Console contains **0 errors / 1 warning**: the previously investigated **Burst JIT DLL blocked by Windows Smart App Control / WDAC** warning recurred. This is not a remaining kinematic-body or C# compile warning. Native Console inspection confirmed its exact source (`Logs/Realism-Native-Console.json`); no Console clearing or security changes were used. See the prior Burst investigation below for safe manual diagnostics. Current snapshots: `Logs/Realism-Final-Console.json` and `Logs/Realism-Final-Editor.json`.

The final compatibility check also confirms water landing does not bypass release protection. The final camera change removed a temporary params-array allocation from the framing loop using equivalent binary maximum operations; compilation was rechecked without repeating unchanged historical suites. Broadcast, skip/splash, and close-up foul-signal captures were inspected; explicit camera snaps now frame the new physics pose correctly and the goal stays clear of the HUD in the reviewed goal-area capture.

Editor is left **stopped**, Run In Background restored **off**, auto-tick disabled, and generated Pipeline configuration moved under ignored Logs. Incidental Run In Background and URP importer settings were restored; **no package, ProjectSettings or SampleScene diff remains**. SampleScene SHA1 remains `E1AF35BE9552429AF329973CC3C3FB1C9478A32B`. C#/Markdown whitespace checks passed; normal Unity-generated scene/material YAML whitespace is retained. Source/meta pairs and original serialized references were preserved.

Read-only Git audit: HEAD remains **`07f46d5`**, staging index empty. Combined earlier M10 plus this expansion: **26 modified tracked files and 34 new scoped files**, all unstaged/uncommitted. Of the new files, **25 belong to this expansion**, including source/meta/material/verification files; 9 were already present from M10. Existing unrelated untracked `old-plan.md` and `tatus` remain untouched. The temporary unused material created while setting up restitution was removed only after checking it had no scene references. No Git write, checkpoint or push occurred.

Next step: developer reviews this expanded uncommitted M10 build, then personally chooses/stages/commits/pushes the review set. Stop after this expansion; no subsequent milestone or service integration is authorized.

## Previous handoff — initial Milestone 10

The developer explicitly authorized M10 in the current session and reported functional manual M9 gameplay. Earlier statements below that M10 was not authorized or M9 manual review was pending are historical. This session extends the existing **14-athlete** scene in place; it does not rebuild M9 or start a later milestone. Only the human performs Git writes.

### Architecture and implementation

- **PrototypeMatch** remains the only phase/transition authority. Added `FreeThrowSetup`, `FreeThrowReady`, `PenaltySetup`, `PenaltyReady`, and `PenaltyFlight`. `IsPlaying` still means only ordinary live play. `BallInMotion` additionally permits physical penalty flight/scoring/keeper contact; it does not advance clocks. Rule transitions synchronize score-observer samples, cancel input/charges and notify existing listeners.
- **MatchRules** classifies explicit illegal contact, tracks per-athlete foul history/eligibility and orchestrates protected restarts through PrototypeMatch. It caches the roster's collision state once. SwimmerIdentity exposes runtime history, while MatchRules alone mutates it. This is a prototype rule profile, not a complete officiating simulator.
- **BallController** remains the sole possession/flight authority. `TryAwardRestart` validates the rules taker and awards through its existing exclusive-owner path. Releases validate eligibility and restart action type; initial free-throw passes cannot score directly. Existing flat/lob flights, physical saves and goal detection are reused. Ball release/save/impact events feed presentation only.
- **PlayerSelection** retains acquisition-driven carrier selection, temporary human Blue keeper possession, ring/input exclusivity and camera retargeting. It never transfers ownership. Excluded/permanently removed athletes fail selection and action eligibility. During a friendly restart, control stays with the taker; opponent restarts permit a valid Blue field fallback.
- **PrototypeTeamTactics / WaterPoloDecision / PrototypeSwimmerAI** remain the shared team brain, intent model and physical action adapter. No additional AI or ball manager was introduced. Keepers remain outside field formations, marking and loose-ball pursuit.

### Contextual switching policy

`ContextualSelection.Score` is a separate, bounded scoring policy evaluated immediately on C. Defense scores estimated arrival using actual motor speed, movement toward the action, goal-side position and the team's primary presser. When another player already presses, off-ball candidates get a small lane-related adjustment. Loose-ball scoring uses predicted aerial landing/current ball position, arrival, momentum, designated pursuer and intended receiver. Attacking scoring favors open reachable progressive support and the player's role support position. The current athlete is excluded when another candidate exists.

Ties resolve by ordinal PlayerId. The most recently departed manual player receives a **1.5-point return penalty for 0.8s**, rather than an input delay or selection lock; urgent situations may still justify a return. `PlayerSelection.contextualSwitching=false` restores roster order; missing tactical context also falls back. Neither mode selects White or cycles goalkeepers. Legitimate Blue acquisition still selects the actual field recipient/keeper.

### Tactical improvements

- Shared assignments refresh at the existing **0.5s** interval or ownership change. One field presser is selected with incumbent advantage and recovery-side cost; other defenders get unique nearest-suitable marks, prioritizing threats nearer goal and favouring centre-back protection of the centre-forward. Incumbent marks receive hysteresis. Zone uses role stations with bounded passing-lane bias; man-down stations compact toward the defended goal. Unmarked threats are handled by danger priority rather than duplicate markers.
- Support lanes shift modestly with ball lateral position. Wings/other advanced roles progress during a **3s recovery transition**, and man-up width expands. The centre-back retains its role support offset. Carrier routes continue toward goal once clear; contested left/right alternatives need a pressure advantage before changing sides.
- Passing evaluates progress, openness and lane obstruction; immediate return passes to the last passer are penalized for **6s**. Counterattack passes receive a progress bonus. Existing goalkeeper-aware corner targeting, legal shot range/drag budget and pressure scoring remain in use.
- Shots/pass intentions commit for **0.45s** while valid, reducing aim/intent conflict. Maximum-hold and urgent-shot fallback respect physical range instead of forcing distant speculative shots. Movement still uses acceleration, arrival slowing, spacing, stamina and the same release APIs for both teams. These are geometric heuristics; sophisticated opponent prediction, deliberate AI off-ball holds and measured mobile balance remain future work.

### Rule sources and prototype adaptations

The [official World Aquatics regulations page](https://www.worldaquatics.com/rules/competition-regulations) currently links the **18 February 2026** edition. Its roughly 40MB PDF exceeded the browsing tool's fetch limit; local download was also unavailable. Do **not** claim a full 2026 rule audit. The readable reference was the World Aquatics June 2025 Part Six extract [hosted by the British Water Polo League](https://www.bwpl.org/uploads/2/6/5/9/26598283/wa_competition-regulations_june-2025_updated-25-06-2025_water_polo_extract.pdf), especially its ordinary/exclusion, penalty, throw and personal-foul articles. Reconcile the profile against the current full text before claiming current competition compliance.

That reference supports an 18-live-second exclusion, a third-personal-foul limit and a 5m penalty throw. These are reference values; the prototype intentionally compresses the period display while keeping exclusions and possession seconds independently paced. Reset/eligibility choices below are implemented mechanics, with full regulations and replacement procedures outside scope.

| Gameplay event | Implemented decision and behavior |
| --- | --- |
| F attempt rejected outside legal reach/facing/protection | No random foul. Normal failed attempts retain their previous result/cooldown. |
| Shielded rear F reach through the carrier's body within 1.1m | Explicit illegal-push event in this game profile; ordinary foul unless the narrow penalty test qualifies. Ordinary fouls are configurable and do not add personal fouls. |
| G intentional hold within 1.4m of an opponent, with that team in possession | Same command API available to human/AI. Off-ball hold awards exclusion; a hold of the carrier uses ordinary/qualifying penalty handling. AI currently uses F, not deliberate G holds. |
| Qualifying rear foul | Requires the actual carrier inside 6m, facing goal, a current human charge or AI Shoot intent, offender behind, and no eligible defending field player in the shooting corridor. Merely contacting a player near goal is insufficient. |
| Free throw | Captures ball location before clearing play; valid carrier/eligible field taker, goal-area relocation, 0.8s stopped setup, opposing clearance and frozen translation. B/L releases; direct shot/charge is blocked. AI aims/passes through gameplay APIs. After 6s an idle human also gets the same bounded automatic release. This pass-only profile and fixed protection/clearance are game simplifications. |
| Exclusion | Personal foul recorded, 18 independent live seconds by default, collider disabled, swimmer parked outside the pool near its current defended end. No ball actions, human selection, ordinary movement/tactics or stamina advancement. Live expiry, own-team recovery/awarded throw, or a goal permits temporary re-entry; permanent removal never expires. |
| Third personal foul | Athlete permanently ineligible for that match; history remains separate from temporary time. No substitute is invented because this roster has no replacement bench. |
| Penalty | Personal foul, short stopped setup, shooter at 5m, other field athletes outside 6m and at least 3m clear. Keeper uses a bounded goal-line station, then real hand contact/save chance. Human turns with movement input and uses charged Space; AI aims/shoots through normal APIs. Pass/lob and field motion are blocked until resolution. |

Exclusion removal/re-entry and legal restart placement are explicit administrative relocations, not live AI teleporting or unearned possession. The opponent clearance search includes an inward-route fallback at walls. Ordinary fouls do not refill the possession clock; exclusions raise it to at least the configurable 18-second restart value. Penalty catches give the defending team a fresh full possession; retained/loose attacking resolution uses the restart value. A goal uses the existing conceding-team restart. New match restores foul history/colliders; halftime retains history/time and moves waiting positions to the new defended end.

All three clocks freeze outside Playing, including **the entire penalty sequence**. During penalty flight only ball physics, keeper contact and the existing goal observer run; a catch or a **3s** flight limit returns to ordinary play without inventing possession. A miss/rebound becomes a live ball after that window. This isolated penalty flight, immediate foul whistle rather than delayed advantage, fixed automatic throw deadline, absence of substitutions, and lack of a comprehensive rebound/out-of-play/6m direct-free-shot model are deliberate prototype limitations. No claim is made that every official foul category is implemented.

### Presentation and audio

MatchAtmosphere listens to authoritative rules/match/ball events. The existing uGUI Canvas gains a two-message referee feed plus selected cap/personal fouls, active field counts/man-up, and excluded cap numbers/remaining time or OUT. Score, period, possession clock, Z defense, stamina and power bar remain. Labels update on events or at 5Hz; geometry/objects are not created during gameplay.

An AudioSource uses event-driven whistle, goal, save, impact, period and final cues. Four small original mono procedural clips are synthesized once and destroyed with the component; optional licensed clips and an optional AudioMixerGroup can override/reroute them. Optional crowd audio plays only on goals at reduced gain. Missing clips are safe; impact audio is throttled. Sound never schedules or changes gameplay. No downloaded audio, crowd rendering or rigged referee was added. Audible quality, overlapping-cue balance and Android audio latency need manual/device review.

### Burst / Windows investigation

Pinned Editor **6000.6.7f1** resolves built-in **com.unity.burst 2.0.0** (`type: shim`), confirmed in packages-lock, PackageCache package.json and the Editor's built-in package directory. Old dependency minimums referencing 1.8.x do not establish that 1.8.x is the resolved version. No package or Editor upgrade was made.

The live Pipeline Console journal records Burst JIT DLLs under Library/BurstCache blocked by **Windows code integrity policy (Smart App Control / WDAC)**, explicitly reported by Unity. This establishes a compatibility/enforcement symptom, not evidence of malware or proof of which policy is responsible. Compilation and Play Mode verification succeed despite those recorded blocks. [Microsoft's Smart App Control explanation](https://learn.microsoft.com/windows/apps/develop/smart-app-control/overview) describes blocking unknown unsigned binaries; this is consistent with the observed JIT behavior. No verified package-specific fix was found, and changing to arbitrary package versions is unjustified for this built-in shim.

Safe manual follow-up if the warning recurs: retain the exact warning and inspect **Event Viewer > Applications and Services Logs > Microsoft > Windows > CodeIntegrity > Operational** around that timestamp, plus Windows Security's block details. Report Editor/Burst versions and policy/event details to Unity. As a diagnostic, the documented **Jobs > Burst > Enable Compilation** toggle can disable Editor Burst compilation without disabling Windows protection; confirm that menu in this Editor and expect different Editor performance. The available [Unity menu reference](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/editor-burst-menu.html) describes the 1.8 menu and distinguishes Editor from Player settings; applicability to built-in 2.0 should be confirmed manually. Do not disable Smart App Control, whitelist unsigned DLLs, alter policy/registry, delete Library or sign changing cache DLLs as a guessed fix. No security settings were changed. The CLI version-update check could not reach its CDN; the installed CLI connected normally.

### Files changed

Created, each Unity script with its `.meta`: `Assets/Scripts/Gameplay/ContextualSelection.cs`, `MatchRules.cs`, `MatchAtmosphere.cs`; `Assets/Editor/Milestone10PrototypeSetup.cs`. Created verification source: `Tools/Verification/Milestone10Verification.cs` (outside Assets/player builds).

Modified runtime files under `Assets/Scripts/Gameplay/`: `BallController.cs`, `BallInteractionInput.cs`, `GoalkeeperInterception.cs`, `PlayerSelection.cs`, `PossessionShotClock.cs`, `PrototypeGoalkeeper.cs`, `PrototypeMatch.cs`, `PrototypeRoster.cs`, `PrototypeSwimmerAI.cs`, `PrototypeTeamTactics.cs`, `ShotCharge.cs`, `SwimmerIdentity.cs`, `SwimmerMotor.cs`, `SwimmerPassing.cs`, `SwimmerSteal.cs`, `WaterPoloDecision.cs`. Modified setup helper: `Assets/Editor/MatchHUDSetup.cs`. Saved scene: `Assets/Scenes/PrototypePool.unity` (additive wiring/HUD/components; identities and existing tuning retained). Modified `Tools/Verification/Milestone8Verification.cs` to isolate extra full-team outlets in its keeper passing fixture; physical reception assertions remain. Modified `GoalScoringPrototypeVerification.cs` to assert the already-established M9 conceding-team owner/current roster starts rather than historical M3 neutral-ball/zero-position restarts; goal geometry/exact-once assertions remain. Updated `AGENTS.md`, `README.md` and this handoff. No package, SampleScene or intended project-setting change belongs to M10.

### Verification and remaining manual review

Fresh compilation and **12 distinct verification entries passed** in the running **6000.6.7f1** Editor. Canonical results are the twelve entry-specific `Logs/M10-Final-*.json` files named below; earlier shorter-name duplicates are intermediate records, not additional coverage. One consolidated regression was run near completion, pausing only to adapt the two historical fixtures described above. The final bounded keeper-return correction was then checked by PenaltiesAndIntegration and KeeperControl; the full regression was not needlessly repeated. Historical results elsewhere in this file are not fresh M10 evidence.

| Entry | Fresh evidence |
| --- | --- |
| `Milestone10Verification.Setup` | Fourteen identities preserved, one rules/presentation authority, contextual wiring, repeat setup adds no objects. |
| `Milestone10Verification.SelectionAndTactics` | Injected actual C selects useful goal-side athlete over remote roster successor; loose-ball route, ownership preservation, auto-acquisition, off-ball control, camera, White rejection, roster fallback, five unique stable marks plus presser and distinct Press/Zone geometry. |
| `Milestone10Verification.FoulsAndExclusions` | Real close rear F command, charge cancellation, no duplicate stopped decision, protected pass-only free throw, both clocks frozen, preserved near-expiry clock turning over once after release, autonomous White free throw, three personal fouls, disabled excluded collider/actions/selection, timed re-entry, permanent third removal, new-match reset, active exclusion/history/waiting end across halftime. |
| `Milestone10Verification.PenaltiesAndIntegration` | Near-goal contact alone rejected as penalty, genuine human and AI shooting-action qualification, legal other-field clearance, real physical keeper save/exclusive catch, clock reset, no keeper pool-clamp teleport, autonomous AI physical penalty goal/exact-once score, goal-triggered temporary re-entry, 12s integrated fourteen-athlete play with bounded stamina/eligible actors and exclusive possession. Two live ordinary-foul decisions occurred in the final integrated run. |
| `Milestone8Verification.ChargedShooting` | Real hold/release/tap input, capped physical shot, no duplicate release, charge bar, possession/switch/stoppage cancellation and immediate AI API. |
| `Milestone8Verification.KeeperControl` | Blue catch/ring/camera/input, human swimming/charged/lateral shots, physical outlet/reception, field-only C, lock/loss recovery, steal cancellation and autonomous White keeper distribution. |
| `Milestone9Verification.Aerial` | Physical height, high pickup rejection, legitimate reception, landing, above-bar rejection and transition cleanup. |
| `GoalkeeperSaveVerification.Run` | Both high-speed catches, exclusive ownership, reachable/missed shots, physical deflection, glancing goal, seeded skill/reaction endpoints and no duplicate contact reroll. |
| `GoalScoringPrototypeVerification.Run` | Both whole-ball planes, held/reverse/post/crossbar rejection, physical misses, fast/long goals, score/HUD, repeated exact-once conceding restarts and current roster starts. |
| `TacticalOffenseVerification.Run` | Actual White pickup/turn/shot/goal, carrying advance, improved-option physical pass/reception and configured anti-stall release. |
| `FullTeamMilestone9Verification.GoalsAndKeepers` | Five physical goals including post-halftime own goal, two catches/distributions at new ends, conceding ownership, fourteen reset positions. |
| `FullTeamMilestone9Verification.TimingAndEnds` | Four 80/480 periods, stopped clocks, one Q2 switch, all fourteen physical relocations and new-match restoration. |

Live frame advancement was proved with the Pipeline frame-count wait (not inferred from Play Mode entry). The Game-view referee capture is `Logs/M10-FreeThrow.png`; visual review caught and corrected a feed/clock overlap in the actual saved HUD, whose clock row had custom centred anchors. Feed placement now sits below that row. Audio hooks/procedural clips are source/compile verified; audible quality was not manually tested. C#/Markdown whitespace checks pass. Unity's serializer adds ordinary blank-value trailing spaces in scene YAML. No historical fixture assertion was removed merely to make a run pass.

Fresh Console ground truth after gameplay verification: **0 errors, 0 warnings, compilationFailed=false**. Burst/code-integrity messages remain in the earlier Pipeline journal; these are not the current Console count. No Console clearing was used to hide failures. Final Editor settings/status and read-only Git inventory are recorded at the end of this handoff after cleanup. The live Editor menu listing confirmed **Jobs > Burst > Enable Compilation** exists in built-in 2.0.0; it was inspected, never toggled.

Manual steps after opening **Assets > Scenes > PrototypePool**:

1. Press Play and focus Game. Use WASD/arrows, Shift, E, B/L, charged Space, C, F and Z. Verify ordinary possession, shots, saves, stamina and camera. With an opponent carrying or a loose ball, tap C repeatedly and judge the tactical choices; friendly receptions must still select the real carrier.
2. While defending, approach just behind a carrier and use F at close body reach. Check FOUL, whistle, cancelled charge where applicable, stopped clocks and protected free throw. Misses at ordinary reach must not randomly become fouls. As a Blue taker, aim in place and press B/L; Space should not fire a direct free-throw shot. Wait through a White free throw to verify autonomous resumption.
3. With White in possession, move beside an off-ball White athlete and press G. Check Blue cap identity, PF increment, reduced field count, non-blocking waiting position, unavailable C selection, man-down shape and countdown only during live play. Verify re-entry after expiry, recovery and goals. Repeat three personal fouls on the same cap: OUT must survive time and later goals.
4. For a qualifying penalty, use a clear shooting chance inside 6m: a carrier facing goal, charge/AI shooting action, and an illegal rear reach within body range with no intervening field defender. This requires positioning; mere nearby contact should produce an ordinary foul. On the award verify 5m shooter, clear other fields, stationary aiming/Space shooting, physical keeper save or goal, single scoring and reliable resumption. The consolidated verification source supplies reproducible automated fixtures for both teams if manual positioning is difficult.
5. Continue through Q1/Q2, verify the halftime goal/keeper/formation swap happens once and preserves foul history. Finish all four periods and restart the match; scores, ends, exclusions, OUT status, colliders and personal fouls must reset. Check a goal during man-up and a nearly expired shot clock during a free-throw setup.
6. Review referee feed/HUD readability and sound volume at the intended landscape aspect ratio. Device-test Android CPU/GPU/allocations/audio/thermal behavior separately; no device/build performance claim is implied. Later milestones remain unauthorized.

Known limits: narrow, command-based foul recognition rather than full physical grappling; no automatic sanctions for ordinary swimmer overlap, deliberate AI off-ball holding, substitute bench, official advantage delay, bonus/team-foul/violent-conduct categories, neutral referee disputes or complete out-of-play handling. Re-entry uses an administrative placement rather than an animation. Penalty rebounds wait for the bounded isolated flight to end; human goalie movement during an opponent's penalty is AI-managed. The HUD is a prototype landscape layout and can become crowded with many simultaneous exclusions. Tactical quality/balance, long matches, every simultaneous period/foul edge and Android/iOS builds remain manual/device work. Do not describe this as full official 2026 rules compliance.

Next step: developer manually reviews M10, then personally chooses/stages/commits/pushes the relevant files. No later milestone begins automatically.

Final audit: Editor left **stopped**, Run In Background restored **off**, auto-tick disabled, generated Pipeline config moved to ignored `Logs/M10-EditorPipelineConfig.json`, and the temporary persisted Run In Background override restored in place. No package/ProjectSettings/SampleScene diff remains. Final Console snapshot: `Logs/M10-Final-Console.json`, 0 errors/0 warnings, compilationFailed=false; final Editor snapshot: `Logs/M10-Final-Editor.json`. The post-layout capture was inspected again and the feed no longer overlaps the clock row. No full mobile aspect-ratio matrix or audible/device test was performed.

Read-only Git audit: HEAD **`07f46d5` — Complete full 6v6 water polo match prototype - Milestone 9**. Staging index is empty. M10 leaves **23 modified tracked files and 9 new source/meta/verification files**, all unstaged/uncommitted. Pre-existing untracked `old-plan.md` and `tatus` remain untouched and excluded from the M10 file inventory. No Git write operation, checkpoint, security-setting modification or later-milestone work occurred. The script/meta inventory above is the scoped review set; generated Logs and unrelated local files are not backup candidates.

This document describes the NEW 3D project. Distinguish confirmed implementation from goals and user-reported setup. Update after meaningful development tasks with changes, tests, issues, and next steps. Do not import the previous game's historical logs.

## Identity and vision

- Game: Water Polo Superstars. Studio: ToduStudios. Unity organization: TODU.
- Studio email: todustudios@gmail.com. Game email: waterpolosuperstars@gmail.com.
- Genre: 3D mobile water polo sports simulation.
- Primary release: Android / Google Play. Secondary: iOS / App Store.
- Solo developer assisted by ChatGPT, Codex, Unity AI, and Blender.
- Presentation target: attractive normal gameplay inspired by Dream League Soccer, within realistic solo-development scope and mobile performance limits.
- Long-term goals: believable athletes, swimming/passing/shooting/blocking/goalkeeper animations, water and splashes, arena, responsive controls, tactical AI, cameras, cinematic goals/replays, career, championships, World Cup, transfers, cards, progression, and monetization. These are ambitions, not completed systems.

Build the new game around its own requirements. The old game is an optional reference, not the foundation or a mandatory migration target. The implemented prototype uses XZ pool-plane tactical calculations with separate 3D movement/presentation; useful legacy tactical concepts inform this separation without importing Rigidbody2D/Collider2D code.

## Project location and creation

- Main project: `C:\Users\todua\UnityProjects\WaterPoloSuperstars`.
- Created on the desktop through Unity Hub as a fresh Unity 6.6 project, using the Universal 3D template and URP.
- Editor: `6000.6.7f1`, verified in `ProjectSettings/ProjectVersion.txt` during this update.
- Successfully opened in Unity and VS Code, according to the developer.
- Normal project directories: `Assets`, `Packages`, `ProjectSettings`. Generated folders such as `Library`, `Temp`, `Logs`, and `UserSettings` must remain excluded from Git.
- Root documentation: `AGENTS.md`, `README.md`, and this `plan.md`; setup also includes `.gitignore` and `.vsconfig`.

## Hardware and test devices

Primary desktop:

| Component | Hardware |
| --- | --- |
| CPU | Intel Core i5-12600K |
| GPU | NVIDIA GeForce RTX 5060 Ti, 8GB VRAM |
| RAM | 32GB DDR4, 3000 MT/s |
| SSD | PNY CS2340 1TB NVMe |
| OS | Windows 11 Pro |

Installed tools reported: Unity Hub/Editor, VS Code, Blender, Git for Windows, Node.js/npm, Codex CLI, and Unity CLI.

Travel laptop: HP EliteBook, Intel Core i7-7500U, Intel HD Graphics 620, 8GB DDR4 at 2133 MHz, Samsung MZVLW256HEHP-000H1 256GB SSD. Use for documentation, Git, lightweight coding, and limited Unity tasks; do not rely on it for heavy 3D work.

Available phone: iPhone 16. Google Pixel 10a is a planned Android test device; acquisition is unconfirmed. Android device testing, including weaker hardware, remains necessary. Desktop performance does not establish mobile performance.

## Reported Unity and Android setup

The foundation inspection confirmed the saved identity, Android API/backend/architecture/input settings, Visible Meta Files and Force Text values below. Android SDK/NDK/OpenJDK directories exist and the Editor log records the Android profile as active; no Android build has been tested. Other setup history remains developer-reported:

- Company: `ToduStudios`; product: `Water Polo Superstars`; version: `0.1.0`.
- Android Build Support, SDK/NDK tools, and OpenJDK installed; Android build profile active.
- Application ID: `com.todustudios.waterpolosuperstars`; bundle version code: `1`.
- Minimum API 26 / Android 8.0. Target API: automatic, highest installed. Verify store requirements near release.
- IL2CPP, ARM64 enabled, Input System Package (New).
- Visible Meta Files and Force Text serialization configured.
- Unity AI Assistant and Unity CLI enabled during creation. A previous Unity AI `NoSubscription` error concerns that service, not proof of a broken project.

## Git and tools

- GitHub account: `Nikoloz-Todua`.
- Public repository: https://github.com/Nikoloz-Todua/WaterPoloSuperstars
- Remote: `https://github.com/Nikoloz-Todua/WaterPoloSuperstars.git`, verified locally during this update.
- Default branch reported: `main`.
- Initial pushed commits reported: `9c3204b` (Unity setup), `0eab526` (documentation/instructions). Always check current status; a previously clean tree is not a current guarantee.
- Do not initialize another repository inside this one. Preserve Unity `.meta` files and avoid unrelated edits. Existing checkpoint entries below are historical records, not authorization for an AI to create another checkpoint.
- **Only the human developer may perform Git write operations.** AI may inspect read-only status/diffs/history/remote state and edit authorized project files. It must NEVER stage, commit, push, fetch, reset, rebase, merge, stash, alter branches/refs/index/configuration, or use equivalent APIs/Unity integrations to change Git state. Previous command approvals do not supersede this rule. Give the human exact commands for backups/checkpoints; never execute them.
- Human workflow: inspect status/diff, stage only reviewed files, inspect the staged contents, commit using the existing identity, push to the reviewed remote/branch, then compare local and remote commit IDs. Never upload caches, secrets or local legacy references. Do not automatically change `.gitignore`.

Setup-time versions reported: Git `2.56.0.windows.4`, Node.js `v26.13.0`, npm `11.20.0`, Codex CLI `0.166.0`, Unity CLI `1.0.0-beta.15`. These are historical values, not guaranteed current installations.

Codex was authenticated with the developer's ChatGPT account. `unity setup codex` installed the Unity plugin, reporting `0.1.8-beta` at setup; installed plugin/skills may change later. Launch Codex from this project folder with `codex`, or `codex.cmd` if PowerShell blocks the `.ps1` launcher. External Codex and Unity AI services are separate.

## Current implementation and verification

Milestones 1-8 are implemented and have the developer's manual confirmations below. Milestone 8 was manually tested successfully and backed up as `734d8da` before Milestone 9. **Milestone 9 — complete full-team prototype expansion** now contains six fields and one goalkeeper per team, six tactical roles, four compressed periods, halftime ends switching, possession clocks, shared sprint/stamina and real aerial lobs. Its fresh automated evidence is below; developer manual gameplay/device testing remains pending. Earlier results are historical and do not establish exhaustive manual edge-case coverage. SampleScene and the old 2D project are preserved.

Current build: **six Blue fields, six White fields, one keeper per team (14 athletes)**. All six Blue fields are selectable; Blue keeper acquisition grants temporary human control, and White stays AI-controlled. X is width, Z is pool length/tactics, Y is height including real lob flight. Blue initially attacks +Z; Continue after Q2 reverses both teams' ends for Q3/Q4. New matches restore the originals. BallController owns possession/flight, PlayerSelection human control, ShotCharge human shooting, PrototypeTeamTactics/WaterPoloDecision field tactics, SwimmerMotor physics/stamina, and PrototypeMatch/GoalMouth phases/scoring/restarts. MatchClock defaults to four periods of **80 live seconds / 480 displayed seconds (6×)**; PossessionShotClock independently defaults to **28 live seconds at rate 1**. Both advance exclusively through live PrototypeMatch steps. Keeper behavior and HUD remain separate components.

**Current manual confirmation (developer-reported):** swimming, camera follow, passing, shooting, F stealing, friendly attacking support, active opposing swimmers, scoring by both teams and scoreboard updates. The developer reports a gameplay screenshot showing `BLUE 1 - 2 WHITE`; this is their reported playtest evidence, distinct from the earlier automated preview capture. Do not infer manual validation of every selection, cooldown, missed-shot, restart, duplication or other edge case from this list.

**Automated evidence:** Milestones 6/7/8 recorded their passing suites below. Milestone 9 uses one reusable focused verification script and a selected consolidated regression; see its section for actual fresh results. Earlier two-field/six-athlete counts, free-play clocks and AI-only Blue keeper expectations are historical rules, superseded where explicitly described by later milestones. Milestone 8 manual gameplay is confirmed. Milestone 9 manual game feel, exhaustive device/aspect-ratio checks and mobile performance remain pending.

### Milestone 1 files

- `Assets/Scripts/Gameplay/PoolBounds.cs`: configurable axis-aligned rectangle (default width 20m, length 30m), waterline and position clamping; independent of decorative walls.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: kinematic 3D Rigidbody movement in FixedUpdate with interpolation, acceleration/deceleration, limited turning, normalized direction and boundary clearance. Public `SetMoveInput(Vector2)` accepts world X/Z direction from a human adapter or future AI. Actual planar `Velocity` is available for future animation. No future gameplay systems were added.
- `Assets/Scripts/Gameplay/SwimmerInput.cs`: clones and owns only the existing `Player/Move` action; WASD/arrows and analogue gamepad input. Input lifetime is separate from shared assets and the movement motor.
- `Assets/Editor/MovementPrototypeBuilder.cs`: explicit `Tools > Water Polo > Create Movement Prototype` command. Creates and saves the separate scene and references. Repeated runs open the existing scene without rebuilding/duplicating it. Refuses Play Mode or unsaved scene changes. Opens the prototype alone to avoid duplicate cameras/listeners; never saves or overwrites SampleScene.
- `Assets/Materials/Prototype/{Water,Floor,PoolEdge,Swimmer,Cap,Skin,Markings}.mat`: seven lightweight URP materials.
- `Assets/Scenes/PrototypePool.unity`: Environment (floor/water/boundaries/markings), Actors/Swimmer with separate Visual child, Gameplay/PoolBounds, CameraRig/Main Camera, Lighting/Directional Light.
- `Tools/Verification/MovementSandboxVerification.cs`: reproducible Editor Play Mode integration checks invoked through Unity Pipeline; outside Assets so it is not included in player builds.
- Unity-generated `.meta` files accompany the new Assets files and folders. No packages or build settings were changed.

### Confirmed architecture and scope

Pool gameplay uses world X for width, Z for length, Y for height; origin at pool centre, one unit approximately one metre. Future goals will sit at opposite Z ends. Swimming root height is fixed by PoolBounds; visual geometry is replaceable independently of the physics/input root.

The swimmer uses a horizontal CapsuleCollider and conservative 1.1m boundary clearance, so turning does not take its body over the edge. Rectangular clamping, not decorative wall colliders, enforces the bounds. This milestone does not implement swimmer-to-swimmer obstacle avoidance or collision resolution.

Milestone 1 used a fixed full-pool overview camera; Milestone 2 supersedes that camera as described below. Water is an opaque blue unlit placeholder, with a separate floor beneath it; no fluid simulation or water effects. Temporary prototype palette: blue pool, pale edges, coral swimmer and yellow cap. These are readability choices, not final art direction. Detailed animation, Cinemachine, audio, menus, online services and visual polish were outside Milestone 1.

### Verification performed in Unity 6000.6.7f1

- Direct Editor connection verified for this project; scripts compiled with `compilationFailed=false` and no compiler errors.
- Builder ran successfully and saved the scene/materials; repeat invocation retained one swimmer and existing scene content.
- Actual Play Mode with advancing frames/physics: injected Input System keyboard and gamepad devices exercised WASD movement, arrow input, analogue strength, acceleration, release/deceleration, stopped drift, facing, all four boundaries under sustained input, and waterline correction.
- Straight speed: approximately 3.999998m/s; diagonal speed: approximately 4.000003m/s, both matching the configured 4m/s maximum.
- Camera viewport checks contained all four pool corners at the current Game view aspect; a 1280x720 camera capture was visually inspected.
- Console ground truth after runtime checks: zero errors, zero warnings, compilation not failed. Verification helper also compiled without diagnostics after replacing deprecated object lookup calls.
- `SampleScene.unity` content hash stayed `1c63aa8ded8c325623e162675b13bfcd0b50aa5a` (Git blob hash).
- Play Mode stopped after checks; temporary Editor background-run/auto-tick overrides disabled. Capture and transient automation helpers are kept only under ignored Logs/Library paths.

Developer manual Play Mode verification after Milestone 1: WASD, turning, stopping, pool containment and opening/running PrototypePool all work. The full-pool camera makes the swimmer far too small; a closer gameplay camera is the next approved improvement. Physical gamepad feel, other screen aspects, Android builds/device performance and iOS remain unverified. No claim of mobile readiness is made.

### Milestone 1 usage and original limitations

Open `Assets/Scenes/PrototypePool.unity` from the Project panel, press Play, click the Game view and use WASD/arrows or a gamepad left stick. Existing scene creation is already complete; the menu command is optional and preserves existing work. Save any dirty scenes first.

Inspector tuning: `Gameplay/PoolBounds` controls Width, Length and Waterline; `Actors/Swimmer > Swimmer Motor` controls speed, acceleration, deceleration, turn speed and clearance. Changing playable bounds does not resize the decorative pool or reframe the fixed camera. Keep clearance appropriate to any replacement collider/visual size. The builder deliberately does not regenerate edited scenes.

Original Milestone 1 limitations: placeholder presentation; no touch controls, ball, goals/scoring, AI, sprint/stamina, or animations. Milestone 2 adds the ball below. Project orientation still allows portrait rotation; landscape configuration belongs to a later approved mobile-control task.

Milestone 1 passed the developer's manual movement review and is recorded in local checkpoint `a8312f6` (`Checkpoint manually verified Milestone 1 movement sandbox`). It includes the implementation, materials/scene/metas, verification tool and relevant documentation. It excludes `old-plan.md`, raw legacy references, logs and generated folders. No push was made.

### Milestone 2 — Gameplay camera and basic ball interaction

Implemented and saved in the existing PrototypePool scene; no duplicate swimmer or environment was created. The original `SwimmerMotor.cs`, `SwimmerInput.cs` and `PoolBounds.cs` are unchanged from Milestone 1.

Files added:

- `Assets/Scripts/Gameplay/GameplayCamera.cs`: fixed-angle perspective camera following the interpolated controlled swimmer in LateUpdate with SmoothDamp and velocity-based forward lead. Inspector settings: Distance, Height, Pitch, Yaw, Smooth Time, Look Ahead, Bounds Inset and Outside Margin. Defaults are distance 6.5m, height 7.5m, pitch 50 degrees; camera FOV is 50 degrees. The camera is deliberately one small component for this bounded prototype; no Cinemachine dependency, replays or effects were added.
- `Assets/Scripts/Gameplay/BallController.cs`: sole ownership authority and Free/Held/Released state machine. Atomic range-checked pickup uses the physics position. Held ball is kinematic with collider disabled and follows the hand anchor smoothly in LateUpdate. Release clears ownership and applies configurable flat velocity; 0.6s protection precedes return to Free. The previous owner's collision is ignored until protection has expired and the colliders are separated. Dropping/disabling a carrier clears ownership.
- `Assets/Scripts/Gameplay/SwimmerBallInteraction.cs`: movement-independent commands (`TryCollect`, `TryShoot`, `HasBall`), assigned ball and carry anchor, configurable 2m pickup range, 12m/s shot speed and 1.5m release distance. These commands can later be called by touch or AI. One assigned ball is sufficient for this prototype; no global manager or singleton.
- `Assets/Scripts/Gameplay/BallInteractionInput.cs`: private new-Input-System actions; E collects, Space shoots. Optional gamepad buttons: West collects, South shoots. Shoot takes priority over simultaneous pickup. The existing movement action asset is untouched.
- `Assets/Editor/CameraBallPrototypeSetup.cs`: `Tools > Water Polo > Set Up Camera and Ball Prototype`. Reuses the existing scene and components, creates only missing ball/anchor/components, assigns references and saves. Repeated runs preserve tunable values and retain exactly one ball. Refuses Play Mode or unsaved scene changes. Never modifies SampleScene.
- `Assets/Materials/Prototype/Ball.mat`: yellow URP Lit ball material, plus all new asset `.meta` files.
- `Tools/Verification/CameraBallPrototypeVerification.cs`: repeatable injected-input Play Mode checks, outside Assets/player builds.

Files modified: `Assets/Scenes/PrototypePool.unity`, `Tools/Verification/MovementSandboxVerification.cs` (the obsolete full-pool camera assertion now checks swimmer visibility), and this `plan.md`.

Scene additions: `Actors/Ball` (unit-scale physics root, SphereCollider radius 0.14m, 0.28m yellow sphere Visual child), `Actors/Swimmer/BallCarryAnchor` (local 0.45, 0.07, 0.85), swimmer interaction/input components, and GameplayCamera on `CameraRig/Main Camera`.

The free/released ball uses a dynamic 3D Rigidbody with no gravity, frozen vertical position, water drag and continuous collision detection. Its centre is 0.07m above PoolBounds.Waterline, so the sphere meets the placeholder surface. Rectangular bounds constrain the ball and damp/reflect its outward velocity; decorative pool edges remain nonphysical. The flat-water model is explicitly a prototype, to be extended or replaced before lob/skip/airborne shots.

Camera bounds constrain the follow focus and an estimated ground footprint of the inner 70% of the viewport, allowing a configurable deck margin. This limits excess outside views while keeping the swimmer visible at corners. It is not a promise that every screen pixel remains over water. Extreme Inspector settings/aspects need manual review; near-horizon pitch falls back to bounded focus.

### Milestone 2 verification and handoff

Verified in Unity 6000.6.7f1 with direct Unity CLI/Pipeline access:

- Runtime/editor scripts compiled successfully; no compilation failure. New verification helper compiled without diagnostics.
- Scene setup executed and was repeated: exactly one ball, one swimmer, valid assigned references; no duplicate environment.
- Movement regression passed: WASD/arrow input, analogue gamepad strength, acceleration/deceleration, diagonal speed approximately 4m/s, facing, waterline and all four swimmer boundaries.
- Actual Play Mode with advancing frames and physics: Space without possession does nothing; out-of-range E is rejected; nearby E collects; ball reaches hand anchor; a second valid claimant is rejected; held collider is disabled.
- Swimming while carrying works; ball follows within the expected smoothing lag. Space releases the ball in the swimmer's facing direction with dynamic physics, rejects immediate re-collection and holds the waterline.
- Released ball becomes Free after cooldown and can be collected again with E. Disabling the carrier drops ownership. Ball containment/floating passed at all four edges with injected 80m/s velocities.
- Camera follows movement with forward lead, responds partially rather than snapping to a 3m target displacement (about 0.23m in the early sample), settles, and keeps the swimmer within the viewport at every pool corner.
- A 1280x720 centre-pool camera capture was visually inspected: the swimmer is substantially larger than in Milestone 1 and the yellow ball is readable. Capture is stored under ignored `Logs/Milestone2CameraBall.png`.
- Console ground truth after checks: zero errors, zero warnings, compilation not failed. SampleScene retains blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.
- Play Mode stopped after checks; temporary Editor auto-tick/background-run overrides disabled. Temporary capture/config artifacts moved out of Assets/ProjectSettings into ignored Logs. No packages, persistent project settings or build scene list were changed.

Developer manual Milestone 2 results: WASD/turning, smooth camera follow, E pickup, carrying while moving, Space shooting and re-collection all work correctly. Camera is somewhat too close; a wider FIFA/Dream League Soccer-style broadcast view is a future task, not the priority for Milestone 3. Gamepad interaction buttons, different screen aspects and mobile hardware remain unverified. Android builds, touch UI, device performance and iOS were not tested.

To test: open `Assets/Scenes/PrototypePool.unity`, press Play, click the Game view, use WASD/arrows, press E near the initial ball, swim/turn while carrying, press Space to shoot, then approach the loose ball and press E again after the short cooldown. Camera tuning is on `CameraRig/Main Camera > Gameplay Camera`; pickup/shot tuning is on `Actors/Swimmer > Swimmer Ball Interaction`; drag/cooldown tuning is on `Actors/Ball > Ball Controller`.

Milestone 2 scope limits: flat shots only; conservative release distance and capsule clearance are tuned to this placeholder swimmer; changing collider/model size requires retuning. One ball/controlled swimmer, no goalkeeper, teams of actors, shot clock, shot charge, lob/skip shots, touch UI or replays. Milestone 3 adds goals/scoring below. Pool visuals do not auto-resize when playable dimensions change.

Milestone 2 passed manual review and is recorded in local checkpoint `2ca3909` (`Checkpoint manually verified Milestone 2 camera and ball interaction`). It includes its scene, scripts, material, verification and manual-results documentation. It excludes legacy references and generated files. No push was made.

### Milestone 3 — Goals, scoring and repeatable restart

Implemented in the same PrototypePool scene. The camera script/settings and movement/input behavior are preserved. The user's request for a wider FIFA/Dream League Soccer-style broadcast camera remains a future task; this milestone does not redesign the camera.

New code:

- `Assets/Scripts/Gameplay/GoalMouth.cs`: independent goal geometry and `PrototypeTeam` (Blue/White). Unit-scale root uses local +Z into the net; configurable clear width (3m), height (0.9m), depth (1m) and post thickness (0.1m). The scoring plane is local Z=0, with a named ScoringPlane child and selected-object gizmo. Swept sphere tests count only outward crossings where the whole sphere passes the plane and clears posts/crossbar; reverse crossings and tangent post contact are rejected.
- `Assets/Scripts/Gameplay/PrototypeGoalView.cs`: separate replaceable placeholder view. Updates the frame, back/side net colliders, lattice net strands, pocket water and split end-boundary sections when dimensions change. Goal dimensions are on GoalMouth, independent of a future detailed goal model. Goal roots must retain unit scale and the documented axis convention.
- `Assets/Scripts/Gameplay/PrototypeMatch.cs`: one score authority with two score values, per-shot deduplication, physics-path observation and restart coordination. Samples actual Rigidbody positions before ball containment; a new shot starts its path at the recorded release point, so pickup/release/reset position changes cannot fabricate a crossing. Positive-Z net credits Blue; negative-Z net credits White, including an own-goal-equivalent shot into that net. No shooter/team roster attribution is implemented.
- `Assets/Scripts/Gameplay/PrototypeScoreboard.cs`: event-driven TextMeshPro scoreboard and GOAL notification; no per-frame text rebuilding.
- `Assets/Editor/GoalScoringPrototypeSetup.cs`: `Tools > Water Polo > Set Up Goals and Scoring Prototype`. Adds/reuses goals, references, restart markers, score authority, HUD and materials; preserves existing swimmer/pool/camera objects. Refuses Play Mode or unsaved scenes; saves only PrototypePool. No duplicate objects on repeat invocation.
- `Tools/Verification/GoalScoringPrototypeVerification.cs`: repeatable Play Mode scoring/restart checks outside Assets/player builds.

Modified code/assets:

- `Assets/Scripts/Gameplay/BallController.cs`: retains ownership/state authority; adds configured goal pockets, release-shot eligibility/sequence/origin, restart pause and reset API. The Released cooldown may transition to Free while a long shot remains scoring-eligible; a pickup, drop or reset clears eligibility. Release spawning stays clamped to the playing side, so only actual travel into the goal scores. Goal pockets permit valid paths beyond the pool end; all other containment remains. Fast valid shots reach the bounded pocket rather than reflecting prematurely before the line.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: adds `ResetPose` to clear movement intent/velocity and reset a clamped pose. Ordinary movement/turning is unchanged.
- `Assets/Scenes/PrototypePool.unity`: new goals, split end geometry, restart markers, score authority and HUD. Original NearEdge/FarEdge objects are retained with their uninterrupted slab renderers disabled; split solid sections close the rest of each end. Existing side bounds remain.
- `Tools/Verification/CameraBallPrototypeVerification.cs`: closed-end containment checks now target X=4m, outside the intentional goal opening.
- `Assets/Materials/Prototype/GoalFrame.mat`, `GoalNet.mat`, and new asset `.meta` files. White solid frames, back/side colliders and simple lattice nets; no external goal models.
- `Assets/TextMesh Pro/` and its `.meta`: standard TMP Essential Resources (fonts, materials, shaders/settings and accompanying resources) imported through the installed package's non-interactive importer. No TMP examples, new packages or external model/art imports.

Scene locations:

- `Environment/Goals/BlueScoringGoal`: goal plane at world Z=+15m, root Y=-0.08m; local +Z points outward. WhiteScoringGoal is at Z=-15m with Y rotation 180 degrees. Both are centred at X=0.
- `Gameplay/PrototypeMatch`: authoritative scores and configurable 1.5-second restart delay.
- `Gameplay/RestartPositions/{SwimmerStart,BallStart}`: saved centre swimmer pose and initial nearby collection position.
- `PrototypeHUD`: screen-space overlay Canvas with responsive scaler, dark score panel (`BLUE 0 - 0 WHITE`) and temporary GOAL text. It is passive UI; no EventSystem is required for these non-interactive labels.

Restart locks further scoring, pauses ball simulation, and temporarily disables swimmer movement/input and ball input. After the unscaled configurable delay it clears possession, shot eligibility, velocities and input intent, resets both actors to the start markers, restores previously enabled controls, hides GOAL and preserves the score. Scores reset only when a new Play Mode session begins. No match clock/quarters were added.

### Milestone 3 verification and handoff

Verified in Unity 6000.6.7f1:

- Runtime/editor compilation completed with no errors. All three verification scripts compiled without diagnostics and ran in actual Play Mode with advancing physics.
- Movement regression passed (WASD/arrows, analogue input, normalized diagonal speed, turning, stopping, fixed height and swimmer containment).
- Milestone 2 camera/pickup/carry/shoot/cooldown regression passed. Camera configuration was not redesigned.
- Directional sphere geometry tested at both goal planes: high-speed swept crossing, whole-radius threshold, wrong direction, post tangency and crossbar miss.
- Held ball moved across the line did not score. Real outside-post and post-centred shots did not score or escape through the closed end wall. A released ball travelling from net toward pool did not score.
- Integration test scored three Blue goals and one White goal: high-speed 80m/s shot, repeat pickup/Space shot after restart, opposite goal, and a long shot after the cooldown changed Released to Free. Every event scored exactly once.
- Scoreboard/GOAL updates, restart scoring lock, restored controls, cleared ownership/shot state, correct start positions and retained scores all passed after repeated restarts.
- Repeated Editor setup retained 90 scene objects, two goals, one score authority and one scoreboard. An Inspector-width change moved the placeholder post correctly; original width was restored and saved.
- Near-goal and GOAL-notification screenshots were captured and visually inspected under ignored `Logs/Milestone3GoalsNear.png` and `Logs/Milestone3GoalNotification.png`. Scoreboard and net/frame are visible. The preview shot added one further temporary Blue score; runtime scores are not saved into the scene.
- Final Console ground truth after Play Mode exit: zero errors, zero warnings, no compilation failure. SampleScene retains blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.
- Play Mode stopped; temporary auto-tick/background overrides disabled. Temporary capture/config files are kept outside Assets/ProjectSettings in ignored Logs/Library paths. No persistent project settings, package versions or build scene list changed.

Manual test: open PrototypePool, Play, click Game, E to collect, W toward the positive-Z goal, release movement while facing the goal and Space to shoot. Confirm Blue increments once, GOAL appears, then the centre restart permits E/Space again. After restart, swim/face toward negative Z with S and shoot to test White. Miss outside the posts to confirm the score stays unchanged.

Developer manual Milestone 3 verification: movement/turning, pickup/carry/shoot, both goals, rejection of missed shots, scoreboard, GOAL notification and restart all work; no problems were observed. Other aspect ratios and Android/iOS builds/device performance remain pending. Milestone 3 scope: flat waterline shots, single swimmer/ball, geometric whole-ball crossing, simple rigid nets and pocket containment. Pool dimension changes do not reposition goal roots automatically.

Milestone 3 passed manual review and is recorded in local checkpoint `0e43094` (`Checkpoint manually verified Milestone 3 goals scoring and restart`). This includes the verified implementation, necessary TMP resources and manual-results documentation; legacy references and generated files are excluded. No push was made.

### Milestone 4 — Two swimmers, selection, passing and wider camera

Implemented in the existing PrototypePool. Both swimmers belong to the prototype Blue team and use the existing kinematic SwimmerMotor. No tactical AI or opposing team was added. Legacy architecture notes were reviewed for the separation of TeamManager selection, PlayerMovement/body execution, TeammateAI adapters and MatchContext ownership; those are conceptual references only. No legacy source implementation was inspected or copied for this milestone, and no 2D physics dependency was introduced.

New runtime files under `Assets/Scripts/Gameplay/`:

- `SwimmerIdentity.cs`: stable prototype player ID, team, cap number, start marker and selection visual; cached access to its own motor/interaction. No possession state is stored here.
- `PrototypeRoster.cs`: one shared member list, unique-ID/start validation, per-swimmer simulation pause and restart positions. Human selection and passing consult the same roster; membership is separate from movement or tactical decisions.
- `PlayerSelection.cs`: one authoritative active human-controlled swimmer. Enables only that swimmer's SwimmerInput and BallInteractionInput, stops the previous swimmer's movement intent/velocity, updates the selection ring and camera target. It never transfers/drops the ball or disables SwimmerBallInteraction. Inactive motors remain available to future AI but currently hold their positions.
- `PlayerSwitchInput.cs`: C (optional gamepad right shoulder) calls SelectNext. Processes before actor input; restart input lock rejects switching. Future touch controls can call the same selection API.
- `SwimmerPassing.cs`: movement/input-independent pass command, same-team target choice, modest launch assistance and configurable receiving parameters. Defaults: speed 8m/s, 35-degree candidate cone, 45% aim blend capped at 12 degrees, targets 2.5-12m away, receiving radius 1.35m, minimum flight 0.08s and receiving window 2.5s. An unaligned pass remains unassisted. No homing, interception logic or tactical movement.

Other new files/assets:

- `Assets/Editor/TeamPlayPrototypeSetup.cs`: `Tools > Water Polo > Set Up Two Swimmer Passing Prototype`. Extends the previous setup without rebuilding the environment/goals. Clones only the swimmer body when Swimmer2 is missing, assigns distinct identity/child references, roster, selection/input, passing, markers and restart wiring. Existing actor tuning and subsequent camera tuning are preserved; newly cloned actors get their own ID. Refuses Play Mode/unsaved scenes and saves only PrototypePool.
- `Assets/Materials/Prototype/TeammateCap.mat` (white), `SelectionRing.mat` (green unlit), with new asset `.meta` files. Selection rings use a 48-point LineRenderer each; no per-frame mesh creation.
- `Tools/Verification/TeamPlayPrototypeVerification.cs`: injected-input Play Mode checks outside Assets/player builds.

Modified files:

- `BallController.cs`: remains the sole ownership/state authority. Adds directional TryPass and one expiring intended receiver; manual pickup and automatic reception share the same private ownership assignment. Receiving checks the actual travelled segment against the intended teammate's current position. It begins after minimum flight time, may accept the intended teammate during Released cooldown, and never grants the passer automatic re-collection. Manual loose pickup retains the existing cooldown. Missing/disabled receivers or expiry leave ordinary free-ball physics. Reception clears goal eligibility; positive-speed pass releases otherwise use the existing release sequence/goal path.
- `BallInteractionInput.cs`: adds B (optional gamepad North) to call the actor's passing component. Only the selected actor's input adapter is enabled. Priority is shoot, pass, then collect for simultaneous actions.
- `SwimmerBallInteraction.cs`: exposes the assigned authoritative Ball reference; HasBall still derives from Ball.Owner.
- `SwimmerMotor.cs`: adds Position and StopMotion for physics-position queries and clean handoff; movement integration, acceleration, turning and bounds are unchanged.
- `GameplayCamera.cs`: adds Target/SetTarget without resetting focus or smoothing velocity. Existing camera system is retained. The scene's first Milestone 4 upgrade sets distance 9.5m, height 11.5m, smoothing 0.3s and FOV 55; pitch stays 50 degrees. Further setup runs preserve Inspector tuning. This is wider active-player framing, not a ball-aware broadcast rig.
- `PrototypeMatch.cs`: leaves goal detection/deduplication/scoreboard intact; delegates multi-swimmer freeze/reset to roster and selection when assigned, with a single-swimmer fallback for older scenes. Both start poses reset, ball ownership/pass intent clears, C is locked during GOAL, and the previously selected swimmer remains selected after restart.
- `Assets/Scenes/PrototypePool.unity`, `plan.md`, and the three older verification scripts. Their swimmer fixture now explicitly selects the first roster member; expected actor count follows the roster. Existing goal/ball/movement assertions remain.

Scene/identity details:

- `Actors/Swimmer`: `prototype-blue-1`, cap 1 (yellow), start at (0,0,0), faces +Z; initially controlled.
- `Actors/Swimmer2`: `prototype-blue-2`, cap 2 (white), start at (2,0,5), faces -Z. Separate Rigidbody, visuals, carry anchor, motor and interaction, referencing the same ball.
- `Gameplay/PrototypeRoster` and `Gameplay/PlayerSelection` hold roster and control; `Gameplay/RestartPositions/Swimmer2Start` adds the second saved pose.
- Exactly one green selection ring marks human control; it is not a ball-ownership indicator. Receiving does not auto-select the receiver. Switching away from a carrier leaves that swimmer carrying the ball.

### Milestone 4 verification and handoff

Verified in Unity 6000.6.7f1 through the existing CLI/Pipeline integration:

- Runtime/editor compilation completed without errors. All four Play Mode verification scripts compiled without diagnostics and passed with advancing physics.
- Team checks: unique same-team IDs, one enabled pair of human input adapters, one selection ring, independent W/D movement, inactive position holding, C switching and smooth camera retargeting. Initial wider framing includes both swimmers.
- Switching preserved possession; selected non-owners could not pass or shoot the other swimmer's ball.
- B created a visible moving, assisted pass with no instantaneous ownership transfer. Passer re-collection was blocked, an inactive teammate received, ownership stayed exclusive, and the second swimmer passed back successfully without automatic selection changes.
- Moving the intended receiver away caused a miss: trajectory did not home, receiving intent expired, and the free ball remained collectible.
- The second swimmer picked up and shot a valid goal. Restart locked both motors/input and C, reset both saved positions, cleared ball ownership/receiving intent and restored one active controller. A new pickup/pass/shot and second restart succeeded. The team test added two Blue scores.
- All movement, camera/pickup/carry/shoot and scoring/restart regressions passed. The goal test added three Blue and one White score, including miss/held/reverse rejection and a long shot; original both-goal behavior is preserved.
- Repeated Editor setup retained 103 scene objects, two distinct swimmers, one ball, one selector, two goals and one score authority; camera tuning was preserved.
- Screenshots under ignored `Logs/Milestone4Team.png`, `Milestone4Pass.png` and `Milestone4Receiver.png` capture wider framing, travelling pass and selected receiver. Initial and receiver views were visually inspected. Preview scores are temporary runtime values; a new Play session starts at 0-0.
- Final Console ground truth: zero errors, zero warnings, no compilation failure. SampleScene retains blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`. New Assets have companion `.meta` files.
- Play Mode stopped, auto-tick/background overrides disabled, temporary generated settings kept in ignored Logs. No package versions, persistent project settings or build scene list changed.

Manual test: open PrototypePool and Play, click Game, E to collect with the selected yellow-cap swimmer, B to pass toward the white-cap teammate, C to select that receiver, B to pass back, and C to return control to the yellow cap. WASD/arrows move only the ring-selected swimmer; Space shoots only if that swimmer owns the ball. Aim/facing matters. Test switching while carrying separately: the ball must stay with its existing owner. After a goal both swimmers reset; selection is retained, so C back to the swimmer near the starting ball if needed.

Inspector tuning: per-actor SwimmerPassing controls speed/assistance/receiving; CameraRig/Main Camera controls wider framing; identity Start Position and roster members define reset membership. Existing shot speed, pickup distance and goal/restart tuning remain available.

Remaining limits: manual keyboard/gamepad feel, other screen aspects, Android touch controls/builds and device performance await testing. Only the intended teammate auto-receives a pass, with a coarse placeholder receiving radius; missed/unaligned loose balls require E. Assistance does not guarantee a catch. No interceptions, AI positioning, swimmer overlap resolution, lob/charged passes, opposing teams, 6v6 match, goalkeeper, clock, fouls, economy, advanced animation or realistic water were added. Camera follows selection, so a distant unselected carrier/ball can leave view; ball-aware broadcast framing is deferred.

Developer manual Milestone 4 verification: two visible swimmers, C switching, single-player input isolation, B passing/receiving, pickup/carry/shoot, goals and restarts all work. The missing automatic selection of a receiving teammate is the highest-priority Milestone 5 task. Gamepad/device testing remains pending.

Milestone 4 is recorded in local checkpoint `4babe88` (`Checkpoint manually verified Milestone 4 team selection and passing`), including its manual-results documentation. Legacy references, old-plan.md and generated files were excluded. No push was made.

### Milestone 5 — Possession switching, basic teammate/opponent AI and wider framing

Implemented in order: possession switching verified first, friendly AI verified next, one opponent verified next, then camera framing and final regression checks. Historical Milestone 4 descriptions of stationary inactive swimmers and reception without control switching describe the earlier milestone; the behavior below is current.

New runtime/editor files:

- `Assets/Scripts/Gameplay/SwimmerTactics.cs`: small pure XZ target calculations for support and goal-side defense. No input, physics or ownership mutation.
- `Assets/Scripts/Gameplay/PrototypeSwimmerAI.cs`: one shared controller for any inactive friendly or opponent. Recomputes decisions every 0.15 seconds and on owner changes; supplies direction to the existing SwimmerMotor. Support defaults to 4.5m forward / 2.5m lateral relative to the carrier, with simple spacing and arrival slowing. One nearest eligible member per team pursues a collectible loose ball within a configurable range, including the human in nearest-player arbitration. AI requests pickup through SwimmerBallInteraction.TryCollect; it cannot bypass distance, release cooldown or exclusive ownership. An opposing carrier causes goal-side defensive pressure at 2.8m. The selected human's motor input is never written by AI. Handoff and restart force a fresh decision. Indexed cached-roster loops avoid per-frame searches/collection allocations.
- `Assets/Editor/BasicAIPrototypeSetup.cs`: final menu `Tools > Water Polo > Set Up Automatic Switching and AI Prototype`. Reuses the previous setup chain, creates only a missing opponent/start marker/materials, configures all AI references and selection's ball reference, registers all three participants, applies the first wider-camera upgrade and saves PrototypePool. Repeated setup preserves existing camera/actor tuning and does not duplicate objects. Refuses Play Mode and unsaved scenes. The intermediate `Set Up Friendly AI Prototype` command remains available; use the final command for the complete milestone.

New assets:

- `Assets/Materials/Prototype/OpponentCap.mat` (red), `OpponentBody.mat` (pale), and companion `.meta` files for these materials and the three new scripts.
- `Actors/Opponent`: `prototype-white-1`, White team, cap 3, starts at (4,0,8) facing -Z, with its own visual/carry anchor/body/start marker. Its loose-ball pursuit range is 6m; friendly default is 9m. White defends the +Z goal. It never has human input or an active selection ring.
- `Gameplay/RestartPositions/OpponentStart`: third restart pose. PrototypeRoster now contains all three participants, across both teams. Team membership and human-selection eligibility are separate from overall restart membership.

Modified implementation:

- `Assets/Scripts/Gameplay/BallController.cs`: still the only ball ownership authority. Publishes `PossessionChanged(previous, next)` once when ownership actually changes, after pickup/reception/release/drop/reset state is applied. A rejected claim or unchanged null owner does not publish another change. Adds read-only PhysicsPosition for loose-ball decisions. No alternate ownership model or 2D physics was introduced.
- `Assets/Scripts/Gameplay/PlayerSelection.cs`: subscribes/unsubscribes to that event. A newly acquired eligible Blue carrier is selected immediately, updating the existing input adapters, ring and smoothly retargeted camera. Null/loose-ball events, opponents and locked selection are ignored; there is no polling or queued switch to replay after a restart. C cycles only eligible friendlies. Manual switching preserves ownership and is not undone while the same carrier continues holding the ball. Selection never grants possession.
- `Assets/Scripts/Gameplay/GameplayCamera.cs`: preserves fixed orientation, bounded focus and smooth target changes. Adds a small nearby-ball bias (influence 0.15, range 10m, maximum offset 1.5m, smoothly faded with distance), not a separate camera mode. Scene upgrade uses distance 11m, height 13m, pitch 50 degrees, FOV 55 and smoothing 0.3 seconds; all tuning remains in the Inspector. Wider framing includes all three starting swimmers. A distant ball can still leave view.
- `Assets/Editor/TeamPlayPrototypeSetup.cs`: recognizes the known opponent and preserves it in roster/restart membership on repeated lower-level setup. Unknown extra actors are still rejected. Opponent inputs/selection remain disabled.
- `Assets/Scenes/PrototypePool.unity`: saved/wired three-swimmer scene, explicit possession-event reference, three AI components, opponent start/materials, and wider camera settings. Existing goals, ball mechanics, pool and scoreboard remain. The existing PrototypeMatch roster pause/reset path handles all participants without rewriting scoring.

Control and AI behavior:

- WASD/arrows move the ring-selected Blue swimmer; E requests pickup; Space shoots; B passes in the facing/assisted direction; C manually selects the next Blue swimmer. Future touch/AI commands can use the same movement, interaction and selection APIs.
- Pass travel does not switch selection. Successful friendly reception or legitimate inactive-friendly loose pickup switches once. A return pass works the same way. The old carrier becomes eligible for AI; only the new selection receives human input.
- During an intended pass, inactive AI waits in position to preserve the existing receiving contract. It does not intercept or home toward a travelling pass. A missed pass becomes ordinary loose-ball pursuit after its receiving intent expires. After shooting, the active swimmer remains selected unless a friendly genuinely acquires the loose ball.
- Opponent pressure is positional only: no steals/contact possession. If the opponent collects a loose ball it stops and holds it; no autonomous pass/shoot/attack behavior is implemented. Blue inactive swimmers can then position defensively. Stop and restart Play Mode to begin a fresh possession if the opponent is holding the ball; do not treat missing recovery rules as a complete competitive match.
- During GOAL, all three motors and human interaction are locked. The existing restart resets every participant and clears ball owner/pass/shot state while preserving scores and a valid friendly selection. Null ownership notifications cannot trigger stale camera changes.

### Milestone 5 verification and handoff

Verified in the running Unity 6000.6.7f1 Editor through Unity CLI/Pipeline, with advancing Play Mode physics:

- Runtime/editor compilation completed without errors. Seven Play Mode verification suites compiled without diagnostics and passed. Existing fixture suites temporarily disable/restore AI where they require stationary actors; live AI behavior is tested separately.
- `Tools/Verification/MovementSandboxVerification.cs`: existing movement/keyboard/arrows/analog gamepad, acceleration/stopping/turning, waterline, bounds and visibility. Straight/diagonal speed both approximately 4m/s.
- `Tools/Verification/CameraBallPrototypeVerification.cs`: existing pickup range/exclusive ownership, carrying, facing-direction shots, cooldown/recollection, disable cleanup, 80m/s containment, smooth camera follow and visibility at all corners at the current Game view aspect.
- `Tools/Verification/GoalScoringPrototypeVerification.cs`: both scoring planes, held/reverse/outside-post/post-hit rejection, high-speed and long shots, exactly-once scores, HUD/GOAL and repeat restarts. Added three Blue and one White score during this regression run.
- `Tools/Verification/TeamPlayPrototypeVerification.cs`: updated intentional reception expectations for automatic switching and roster-sized actor counts; original selection isolation, physical passes/return passes, missed passes, shots and repeated restarts passed. Added two Blue scores during this regression run. This fixture isolates AI.
- New `Tools/Verification/PossessionSelectionVerification.cs`: acquisition-time switch on pass and return, camera/ring/input synchronization, failed pass/no premature switching, manual C independence, inactive-friendly pickup and lock/no stale replay. Counted 12 genuine ownership notifications in the fixture; no per-frame repeated switching.
- New `Tools/Verification/FriendlyAIVerification.cs`: live friendly support movement, human movement unaffected, handoff to AI while retaining ball ownership, nearest-friendly pursuit and automatic selection only after legitimate pickup. Human closer to ball suppresses redundant teammate chase. Opponent AI is isolated for this fixture.
- New `Tools/Verification/OpponentAIVerification.cs`: all three AI adapters enabled. Defense toward the defended goal, opponent input/selection exclusion, real loose-ball approach/acquisition, exclusive enemy ownership without selection transfer, pool/waterline containment, all-three goal freeze/reset, then new pickup/pass/return-pass with automatic switching and camera retargeting all passed.
- New `Tools/Verification/AIPrototypeSetupVerification.cs` (Edit Mode): repeated final setup retained 113 objects, three unique IDs/motors/AI adapters, one ball/selection/match authority and two goals. All required AI/start/ball/ring references were assigned; temporary custom camera distance survived repeated setup and was restored afterward. No duplicate actor components.
- A 1280x720 camera capture at ignored `Logs/Milestone5Broadcast.png` was visually inspected: three swimmers, selected ring and ball are readable. This camera-render capture excludes screen-space overlay UI; the HUD was verified by the scoring suite.
- Console ground truth at final Play verification: zero errors, zero warnings, compilation not failed. SampleScene remains unchanged. All new Unity assets have companion `.meta` files. No packages, build-scene list or persistent project settings were intentionally changed. Play Mode and temporary auto-tick/background overrides are stopped/disabled at handoff.

At the Milestone 5 checkpoint, isolated manual playtesting was pending. The later developer Milestone 6 manual results are recorded in the current snapshot and Milestone 6 section; do not reinterpret them as exhaustive testing of this earlier checkpoint. Different aspect ratios, gamepad feel, mobile touch UI and Android/iOS builds/device performance are not verified by the automated checks. Simple spacing is not full collision/avoidance; kinematic swimmers can overlap. Tactical formations and stealing were not part of Milestone 5; Milestone 6 adds them below, and Milestone 7 adds keepers. Full teams, clock, fouls, advanced animations/water and economy remain unimplemented.

Manual test: open `Assets/Scenes/PrototypePool.unity`, Play and click the Game view. E collects with the yellow cap; face the white-cap teammate and B to pass. Selection/ring/camera must remain with the passer during travel, then transfer on reception; WASD must move the receiver. Face/pass back and verify automatic return selection. Try C while carrying: ownership must stay with the previous carrier. Watch the inactive friendly support the carrier and the red-cap opponent defend. Shoot/miss and let an eligible inactive friendly approach a loose ball; acquisition should select that friendly. The red cap may collect but must never become selected. Score at either goal and verify all three reset and another pickup/pass/shot works. Check `Window > General > Console` for errors/warnings.

No manual Inspector wiring is required. The final setup menu is optional because the scene is already saved. To run it, exit Play Mode, save any intended scene edits, then choose `Tools > Water Polo > Set Up Automatic Switching and AI Prototype`. Inspector tuning: each actor's Prototype Swimmer AI for support/pressure/range/speed; `Gameplay/PlayerSelection` for Human Team; `CameraRig/Main Camera > Gameplay Camera` for framing and `Camera` for FOV. Keep Human Team Blue for this two-friendly prototype.

Milestone 5 is recorded in local checkpoint `d647882` (`Checkpoint Milestone 5 automated-tested; manual verification pending`). Before extending it, all seven existing Play Mode suites and its Edit Mode repeated-setup check were rerun successfully with zero current Console errors/warnings. This checkpoint includes only Milestone 5 assets, scripts, verification and documentation; legacy/generated files were excluded. It is NOT a manual-playtest approval. No push was made.

### Milestone 6 — First playable 2v2, tactical offense/defense and stealing

The current PrototypePool contains two Blue and two White field swimmers. Blue attacks +Z, White attacks -Z. Blue is human-selectable; inactive Blue swimmers and all White swimmers use the same shared reasoning/physical action architecture. All four are registered in the existing PrototypeRoster for pause/reset. Gameplay remains a score-based free-play prototype: no keeper, official clock, quarters, match end or competition rules.

Foundation review included actual current gameplay/editor/verification scripts, saved scene, AGENTS.md, README and this plan. No `docs/ARCHITECTURE.md`, `docs/MIGRATION_AUDIT.md` or docs directory was present. The available old-plan architectural summaries of WaterPoloBrain/IAgentBody, TeamSide, TeamManager, MatchContext, TeammateAI and BotMovement were reviewed as concepts. No legacy C# source archive/local reference clone was available or imported; no 2D physics code was copied.

New implementation files (with companion Unity `.meta` files):

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Gameplay/PrototypeTeamTactics.cs` | One component per team. Explicit team/roster/ball/pool/match/attacking-goal/defended-goal context; formation/support targets, presser/loose-pursuer coordination, lane/pressure/shot/pass evaluation and team pass cooldown. No ownership assignment or physical movement. |
| `Assets/Scripts/Gameplay/WaterPoloDecision.cs` | Shared deterministic decision model returning a small value-type SwimmerDecision: formation, support, pressure/defense, loose pursuit, carry, pass, shoot or pass wait. No scene searches, keyboard simulation or body/ownership mutation. |
| `Assets/Scripts/Gameplay/SwimmerSteal.cs` | Reusable human/touch/AI steal command, configurable cooldown/range/facing/protection/exposure; calls the authoritative ball resolver and reports a StealResult. Match stoppages block attempts and reset attempt timers. |
| `Assets/Editor/TwoVTwoPrototypeSetup.cs` | Explicit repeat-safe `Tools > Water Polo > Set Up 2v2 Prototype`; creates only missing White Player 2/start marker and team context objects, wires every AI/steal/team/goal reference and saves PrototypePool. |
| `Assets/Materials/Prototype/BlueTeamBody.mat` | Blue team body color; White retains its pale body and red cap. Yellow/white Blue caps distinguish the two friendly prototypes. Colors are presentation only; explicit identity drives rules. |

Modified implementation files:

- `Assets/Scripts/Gameplay/PrototypeSwimmerAI.cs`: the existing shared adapter now consumes WaterPoloDecision and PrototypeTeamTactics instead of embedding the old support/hold logic. Retains movement speed/arrival/spacing/decision Inspector tuning. Executes commands through SwimmerMotor, SwimmerPassing, SwimmerBallInteraction and SwimmerSteal; never assigns Owner. Default decision interval is 0.15s. AI actions are canceled/recomputed on match changes, control handoff and owner changes. Selected human input is never overwritten. Pass/shot execution counters are read-only runtime diagnostics, not progression stats.
- `Assets/Scripts/Gameplay/BallController.cs`: adds read-only OwnershipStartedAt and atomic TrySteal validation/assignment using its existing AssignOwner/PossessionChanged path. Pickup compares both Rigidbody positions so interpolation cannot reject a valid post-reset/moving claim; non-swimmer diagnostic claimants retain the transform fallback. Avoids setting velocities on an already kinematic held body during transfers. Existing physical pass/reception, flat shots, release protection, bounds and scoring eligibility remain authoritative.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: adds optional independent facing direction for stationary AI aiming/pressure, using the same limited Rigidbody rotation in FixedUpdate. Stop/reset/disable clears aim and velocity. Existing movement integration is retained.
- `Assets/Scripts/Gameplay/SwimmerInput.cs`: clears AI aim before applying human movement so handoff cannot leave stale aim control.
- `Assets/Scripts/Gameplay/BallInteractionInput.cs`: F issues the shared steal command through the new Input System; no conflicting existing F binding was found. Existing E/B/Space remain, with action priority shoot, pass, steal, collect. C remains in PlayerSwitchInput. No touch controls were added.
- `Assets/Editor/TeamPlayPrototypeSetup.cs`, `BasicAIPrototypeSetup.cs`: lower-level setup preserves registered extra White swimmers instead of truncating/rejecting the four-member roster. Basic AI setup retains existing matching tactical-team references. Use the final 2v2 menu to ensure all Milestone 6 references are configured.
- `Assets/Scenes/PrototypePool.unity`: fourth swimmer, fourth start, Blue/White team contexts, four steal components, matching AI team references and Blue team materials. Existing goal geometry, HUD, original swimmers, ball and camera remain. No SampleScene changes.
- `plan.md` and the eight existing verification scripts listed below. No PlayerSelection, PrototypeRoster, PrototypeMatch, GoalMouth or scoreboard rewrite was needed.

Scene details:

- Blue: `Actors/Swimmer` (`prototype-blue-1`, initially selected) and `Actors/Swimmer2` (`prototype-blue-2`), preserving their existing starts/anchors/motors and cap colors.
- White: `Actors/Opponent` (`prototype-white-1`, existing start (4,0,8)) and `Actors/Opponent2` (`prototype-white-2`, new start (-4,0,8), yaw 180). New `Gameplay/RestartPositions/Opponent2Start` is independent of every other saved start.
- `Gameplay/BlueTeam`, `Gameplay/WhiteTeam`: PrototypeTeamTactics with opposite goal references. Goal.ScoringTeam identifies the attacking team credited at that net; Blue defends the -Z net, White the +Z net.
- Runtime decisions iterate roster membership/team/slot data, independent of GameObject names or a fixed team count. The explicit Editor setup verifies a 2+2 scene; it does not create full teams. There are 125 scene objects after repeated setup.

Tactical behavior and tunables:

- Each team coordinates one nearest loose-ball pursuer and one defensive presser, including the human in nearest-member selection. Assignments update every 0.5s and on owner change, with a 1m retention advantage to avoid rapid swaps. Other swimmers hold formation or protect a goal-side passing lane/mark. Loose-ball pursuit covers the pool; the earlier 6/9m initial-AI search limits no longer govern play.
- Support uses the team's explicit attacking direction, default 4m forward/3m lateral, and stable roster slots. Formation and all targets are pool-clamped. The adapter uses soft spacing and arrival slowdown; pressure can enter the 1.8m steal radius without being repelled by ordinary carrier-spacing rules. This is lightweight steering, not physical swimmer overlap resolution or full pathfinding.
- AI carriers advance toward a central attacking position. After a minimum 0.8s hold they assess a 9m shot range, lateral angle, clear lane, defender pressure and default quality threshold 0.55. They stop, turn using the motor, and shoot through the same API as the human. The actual ball must cross the existing goal geometry to score.
- If a shot is unattractive, an unobstructed teammate 2.5-11m away with better progress/pressure can be selected for a directional physical pass. The adapter aims and calls the existing passing API; no homing/instant ownership transfer. Team pass cooldown is 3s, limiting repeated exchanges. Aiming and ordinary motor deceleration occur before release.
- A carrier exceeding the configurable 8s maximum hold selects a physical fallback shot when no better pass is chosen. Rotation/settling adds a short aim allowance; this prevents indefinite holding without altering the score directly. AI may miss long/pressured attempts.
- During an intended pass, inactive AI waits, preserving the existing receiving contract. Misses expire to normal loose-ball play. No new interception system, lobs or charged shots were added.

Prototype stealing:

- F and AI pressure call exactly the same SwimmerSteal.TrySteal path. Valid attempts consume a default 1.6s cooldown, including range/facing/protection failures. The core ball authority verifies an opposing held carrier, range <=1.8m, attacker facing within 65 degrees, >=1s since the carrier acquired possession, and carrier-forward dot toward attacker >=-0.25 (rear approaches are shielded). Exact overlapping centres are rejected because they have no meaningful facing direction.
- This is a deterministic prototype rule: every valid exposed-front/side attempt succeeds; there is no hidden random roll. Missed conditions preserve the existing owner. Same-team attempts, free-ball attempts and stoppages cannot transfer possession. The ball has one Owner before and after the atomic transfer and emits the existing genuine ownership-change event.
- Friendly pickup/reception/steal therefore automatically selects the actual new Blue carrier, updates the ring/input/camera once, and never selects White. C still changes only control and does not transfer possession; the previous human becomes eligible for tactical AI immediately. Future touch controls can call the same movement/pass/shoot/steal/selection APIs.
- No foul probability, referee, exclusion or penalty is implemented. The protection/cooldown and shielding rules are prototype fairness constraints, not official water polo rules.

Goals, presentation and restart:

- Existing goal detection/scoreboard/GOAL/deduplication remains unchanged. Both teams release real balls and can score at their correct attacking net; own-goal-equivalent crossings still credit that net's configured team.
- Existing roster pause/reset includes all four. Team assignment/pass timers, per-actor pending decisions/aim and steal cooldowns reset from match Changed events; ball reset clears possession, shot and receiving intent. Scores and a valid Blue selection survive restart. No deferred action/event queue exists to fire a stale pass after reset.
- The Milestone 5 elevated camera is retained (distance 11m, height 13m, pitch 50, FOV 55, smoothing 0.3s and bounded nearby-ball bias). It already frames all four initial swimmers and follows selection smoothly; no camera rewrite or additional HUD was needed. A distant ball can leave the selected-player view. Screen-space HUD remains score/GOAL only.

### Milestone 6 automated verification

Verification tools remain outside Assets/player builds. New files:

- `Tools/Verification/TwoVTwoSetupVerification.cs`: Edit Mode scene repeatability, 2+2 identities, unique starts, shared authorities, correct goal/team references and preserved camera tuning.
- `Tools/Verification/TacticalOffenseVerification.cs`: real White pickup/advance/turn/shot/goal, physical pass and valid White reception, and configured anti-stall release. Isolates other AI for deterministic decision fixtures.
- `Tools/Verification/StealDefenseVerification.cs`: injected F input, protection/range/facing/shielding failures, cooldown, same-team/lock rejection, successful atomic steal/auto-selection, coordinated targets/hysteresis and an actual AI pressure/steal sequence.
- `Tools/Verification/TwoVTwoMatchVerification.cs`: all AI enabled; human E/B/return/C interactions, camera retargeting, two White AI and two Blue human goals, exact-once locks and reset snapshots for every actor, followed by 20 simulated seconds of ordinary autonomous play with bounds/ownership/no-stall assertions.

Existing fixtures updated: `MovementSandboxVerification.cs`, `CameraBallPrototypeVerification.cs`, `GoalScoringPrototypeVerification.cs`, `TeamPlayPrototypeVerification.cs`, `PossessionSelectionVerification.cs`, `FriendlyAIVerification.cs`, `OpponentAIVerification.cs`, `AIPrototypeSetupVerification.cs`. Changes isolate deterministic starting poses from ongoing live AI, preserve original gameplay assertions and update intentionally superseded expectations (four actors, passive holding replaced by offense, idle replaced by formation, tactical-team references). The follow-only camera settling measurement parks the ball outside its framing influence after containment checks, so legitimate moving-ball bias does not alter that measurement.

Stage checks completed: pre-change Milestone 5 regression (eight suites); four-actor scene/configuration plus passing regression; White offense; defense/stealing; full-AI integrated match. No developer manual test or mobile performance claim is implied.

Recorded final verification from the Milestone 6 implementation session in Unity 6000.6.7f1 (not rerun during this documentation review):

- Runtime/editor compilation completed successfully. All 12 verification scripts compiled with zero diagnostics and passed: ten Play Mode suites (`MovementSandboxVerification`, `CameraBallPrototypeVerification`, `GoalScoringPrototypeVerification`, `TeamPlayPrototypeVerification`, `PossessionSelectionVerification`, `FriendlyAIVerification`, `OpponentAIVerification`, `TacticalOffenseVerification`, `StealDefenseVerification`, `TwoVTwoMatchVerification`) and two Edit Mode suites (`AIPrototypeSetupVerification`, `TwoVTwoSetupVerification`).
- The final integrated match run added two Blue and four White goals: the four explicitly arranged scoring/restart cycles passed, and additional White attacks scored during the 20-second uninterrupted observation. Five completed restart snapshots had been verified when observation ended; the final goal was still in its restart delay. The preview capture then waited for that restart to finish. No false duplicate or incorrect-team score was observed.
- Both repeated-setup suites retained 125 objects, four unique players/start references, exactly two members per team, one ball/selection/score authority, correctly assigned tactical/steal references and preserved custom camera tuning. Original SampleScene retains Git blob hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.
- At that session's final Console check: zero errors, zero warnings and `compilationFailed=false`. Historical Pipeline journal counts are not the current Console status. No Console clearing was used to mask errors. This review did not query the live Editor Console.
- `Logs/Milestone6Final-*.json` contains the actual final suite results, and `Logs/Milestone6Baseline-*.json` contains the pre-change checks. These are ignored local artifacts. `Logs/Milestone6Gameplay.png` is an actual Game-view capture with score HUD, four swimmers, ball and selected ring; it was visually inspected for prototype readability. Preview scores were transient and reset on the next Play session.
- The implementation session ended with Play Mode stopped and temporary auto-tick/background overrides disabled. Automation-generated configuration/captures/helpers remain in ignored Logs/Library paths. Player/URP settings were restored; no packages, Android configuration or build scene list changed. New Unity assets have `.meta` files. At the subsequent documentation review Milestone 6 was still uncommitted and the index empty; the developer later completed its backup as `48ab2f6`. old-plan.md remained untracked/excluded.

### Milestone 6 developer manual verification

The developer personally tested the current 2v2 build and confirmed swimming movement, camera following, passing, shooting, stealing with F, friendly attacking support, opposing swimmers moving/playing, both teams scoring and scoreboard updates. Their reported gameplay screenshot showed `BLUE 1 - 2 WHITE`. The screenshot was described by the developer in the documentation request; it was not separately inspected during this review.

These confirmations establish playable core 2v2 behavior, not exhaustive edge-case coverage. Automated-only assertions, including detailed directional/duplicate scoring checks, cooldown/shielding boundaries, full restart snapshots and repeat-setup invariants, retain their separate recorded evidence. Gamepad, Android/iOS builds, touch input, device performance and broader aspect-ratio coverage remain unverified.

Milestone 6 limitations: deterministic close-contact stealing needs gameplay balancing; soft steering can still allow kinematic bodies to overlap; formations/pass-risk/shot quality are simple heuristics without player attributes. All shots/passes stay at the waterline. Goal restarts return to the same prototype start positions and centre ball rather than official possession/kickoff rules. Milestone 6 added no keeper; Milestone 7 below adds that role. Fouls, exclusions, clocks, match end, full teams, touch UI, network/backend, economy, advanced water/athletes/animation and audio polish remain absent. Core 2v2 manual results are confirmed above; exhaustive manual edge cases, device builds, mobile performance, touch controls, aspect-ratio coverage and final camera/AI tuning remain pending.

Manual handoff: open `Assets > Scenes > PrototypePool` in the Project panel, Play, and click the Game view. WASD/arrows swim; E picks up; B passes; Space shoots; C selects the other Blue swimmer; F attempts a steal. Face the nearby white-cap Blue teammate to pass and observe automatic selection on receipt. Leave a loose ball for White to collect and attack; defend from the carrier's front/side, face the carrier and use F within reach after acquisition protection expires. White must never become selected. Score at +Z for Blue; allow White to attack -Z. Confirm scoreboard/GOAL, all four resets and repeated pickup/pass/shot sequences. Open `Window > General > Console` to inspect errors/warnings. No Inspector wiring is required.

Milestone 6 setup remains available at `Tools > Water Polo > Set Up 2v2 Prototype`, outside Play Mode after saving intended scene edits. Tune team attack/support/shot/hold settings at `Gameplay/BlueTeam` or `WhiteTeam > Prototype Team Tactics`; per-actor speed/decision/spacing at Prototype Swimmer AI, steal conditions at Swimmer Steal; camera remains at `CameraRig/Main Camera`. Milestone 7 extends this setup through the final keeper menu below. Only the developer may stage/commit/push; they completed the Milestone 6 backup and explicitly authorized Milestone 7 afterward.

### Milestone 7 — AI goalkeepers, saves and distribution

Authorized scope: add one AI-only goalkeeper to each existing two-field-player team. The human developer completed/pushed the Milestone 6 backup as `48ab2f6` (`Complete 2v2 water polo prototype through Milestone 6`) before this work. Initial read-only status showed main matching origin/main, with only old-plan.md untracked. No AI checkpoint, staging, commit, push, fetch or other Git write was performed. The absolute human-only Git rule takes precedence over checkpoint wording in milestone templates.

Foundation review covered AGENTS.md, this plan, actual runtime/editor/verification scripts, saved PrototypePool hierarchy/configuration and the available old-plan goalkeeper summary. No old Goalkeeper.cs source archive was available. Ball tracking, physical contact, skill/catch decisions, protected possession and distribution were useful concepts; no Rigidbody2D code, old coordinate constants, halftime/shot-height/animation/manual-keeper logic was imported.

New runtime files under `Assets/Scripts/Gameplay/`:

| File | Responsibility |
| --- | --- |
| `GoalkeeperPositioning.cs` | Pure goal-local tracking/projection and relative swept-sphere contact geometry. It reads no global scene state and never assigns ownership or moves actors. |
| `PrototypeGoalkeeper.cs` | Dedicated role-specific tracking, bounded target generation, reaction/recovery and seeded save/catch decisions. Sends movement/facing commands to the existing motor, and confirmed contact results to the existing ball authority. |
| `GoalkeeperInterception.cs` | One scene observer at execution order -120, before PrototypeMatch (-100). Samples actual ball/keeper hand travel, finds limited physical contact, prevents post-line rescue and latches contacts to avoid repeated save rolls. No whole-goal wall or full-scene per-frame searches. |
| `GoalkeeperDistribution.cs` | Secures a hold, chooses an eligible field-player outlet, aims with the shared motor and releases through SwimmerPassing; timed fallback prevents indefinite possession. Match changes cancel cached outlet/timing. |

Other new files/assets:

- `Assets/Editor/GoalkeeperPrototypeSetup.cs`: explicit `Tools > Water Polo > Set Up Goalkeeper Prototype` menu. Extends the previous repeat-safe setup, creates only missing keeper objects/start markers/components/materials, registers all six athletes and assigns keeper/interception/distribution references. Refuses Play Mode or unsaved scenes, preserves existing field actors/pool/goals/camera/HUD and subsequent tuning, and saves only PrototypePool.
- `Assets/Materials/Prototype/GoalkeeperBody.mat`, `BlueGoalkeeperCap.mat`, `WhiteGoalkeeperCap.mat`: upright orange placeholder bodies with blue/white caps. These colors do not determine identity. Companion `.meta` files accompany all new Unity scripts/materials.
- New verification files outside Assets: `GoalkeeperSetupVerification.cs`, `GoalkeeperPositionVerification.cs`, `GoalkeeperSaveVerification.cs`, `GoalkeeperDistributionVerification.cs`, `GoalkeeperMatchVerification.cs` under `Tools/Verification/`.

Modified implementation:

- `SwimmerIdentity.cs`: adds explicit AthleteRole (FieldPlayer=0 default, Goalkeeper=1), Role and IsFieldPlayer. Existing four actors retain the default field role and IDs. The same identity, motor and SwimmerBallInteraction can represent either role; no competing keeper ownership model/interface was necessary.
- `PrototypeRoster.cs`: Members contains every athlete for pause/reset; cached FieldPlayers contains only the four field roles for selection/tactics/test fixtures. Cache rebuilds when serialized roster data changes. No per-frame filtered list allocation.
- `PlayerSelection.cs`: eligibility additionally requires IsFieldPlayer. A Blue keeper catch does not change human selection, ring or camera target; C continues cycling only the two Blue field players. The existing possession event selects a receiving Blue field player after a keeper pass.
- `SwimmerMotor.cs`: introduces generic ISwimmerMovementBounds and an optional bounds provider. Keepers supply a goal-local envelope; the same acceleration/deceleration/MovePosition/facing integration remains authoritative. Field behavior has no additional constraint. Pose reset respects the keeper envelope.
- `PrototypeTeamTactics.cs`, `WaterPoloDecision.cs`: field eligibility/ranks/pressure/pursuit exclude keepers. A friendly keeper owner produces outlet support roughly 6m into the pool; opponents hold defensive lanes rather than trying to press the keeper. Overall possession still derives solely from Ball.Owner. Normal field support/defense is retained.
- `SwimmerPassing.cs`: adds MaximumTargetDistance and directed TryPassTo with the same range/aim/receiver validation and shared assisted launch path as ordinary passes. Current automatic target selection/outlets prefer field players. Existing physical flight, receiving window/cooldown and exclusivity remain unchanged.
- `BallController.cs`: adds release kind/time/releaser metadata and MotionRevision/MotionOrigin for release/reset/catch/deflection discontinuities. TryCatchContact and TryDeflectContact consume contacts confirmed by the interception observer; BallController alone applies ownership or velocity/state changes. A bounded keeper's secured hold is protected from the prototype field-steal command. Catch clears scoring eligibility; deflection retains its original shot sequence/eligibility, clears pending pass intent and applies a short 0.15s loose-release protection.
- `PrototypeMatch.cs`: retains directional whole-ball geometry, score authority and per-shot deduplication. Path sampling now starts a changed physical trajectory at Ball.MotionOrigin, so a reflected ball cannot reuse its old incoming segment and release/reset spawning cannot fabricate a goal.
- `Assets/Editor/TeamPlayPrototypeSetup.cs`, `BasicAIPrototypeSetup.cs`, `TwoVTwoPrototypeSetup.cs`: role-aware field setup preserves existing keepers in overall roster membership without assigning field AI/human inputs or truncating them. The final keeper menu ensures the complete current wiring.
- `Assets/Scenes/PrototypePool.unity`: six-athlete roster, two new keepers/starts, one observer and two distributors, explicit goal/team/ball links and placeholder keeper materials. SampleScene, camera script/tuning, score HUD, goal geometry, field input bindings and field AI controller implementation are preserved.
- All 12 previous verification scripts now distinguish field fixtures from the complete athlete roster. The ten original Play Mode suites disable/restore keeper behavior while testing the established field systems; five new keeper suites test role inclusion, interception and six-athlete integration separately. Startup waits for any current restart before capturing the controlled swimmer reference. AGENTS.md, README.md and this plan describe the current baseline.

Scene and movement:

- `Actors/BlueGoalkeeper`: role Goalkeeper, team Blue, ID `prototype-blue-goalkeeper`, defends the White-scoring -Z goal. `Actors/WhiteGoalkeeper`: role Goalkeeper, team White, ID `prototype-white-goalkeeper`, defends the Blue-scoring +Z goal. Field IDs/start poses remain unchanged. Each has its own `Gameplay/RestartPositions/{BlueGoalkeeperStart,WhiteGoalkeeperStart}` marker.
- At current geometry the roots start at world Z=-13.6/+13.6, face into the pool and remain at PoolBounds.Waterline. Positioning uses each assigned goal's transform/width, not names/colors or hardcoded world ends.
- Default station is 1.4m in front of the goal line. Lateral extent is clear half-width minus 0.45m margin (1.05m for a 3m mouth). The motor clamps to this envelope every physics integration, speed 2.5m/s, acceleration 8m/s², deceleration 10m/s². Target smoothing is 0.15s; a nearby opponent carrier or approaching ball supplies lateral tracking/projected crossing. A distant harmless ball causes a smooth central return. No teleporting/dive animation or pool-wide chase.
- The upright keeper's CapsuleCollider is a trigger, not a solid invisible save wall. Actual save geometry is the limited hand sphere below. Held ball uses the existing carry anchor, collider-disabled state and exclusive owner; human controls remain available on the selected Blue field player.

Physical saves:

- The observer tests the actual travelled ball segment against the hand's travelled segment, with full 3D relative swept-sphere geometry. Effective radius is configurable hand reach (default 0.5m) plus actual ball radius (0.14m); hand centre is 0.55m in front of the keeper root at the waterline holding anchor. Shots must really enter that area. The observer runs before score evaluation and rejects contacts after the scoring plane has already been crossed.
- Default reaction delay is 0.16s, enforced both on threat acquisition and against the authoritative release timestamp, including shots arriving between Update calls. Keeper tracking distinguishes ownership/team and approach velocity; shot/pass/drop metadata remains in BallController. A harmless distant pass causes no remote save.
- At an eligible fast contact, default save probability is 0.8; conditional catch probability is 0.65. Each keeper has a configurable seeded System.Random (default seed 173), used only at new contact attempts. The contact latch prevents rerolling every frame; recovery defaults to 0.5s. Deterministic tests force probability endpoints and known geometry.
- Catch moves the ball to its actual contact point and uses the existing ownership assignment/holding behavior. Deflection reflects incoming velocity around the physical contact normal with default restitution 0.65 and remains unowned/collectible after 0.15s. Glancing reflections can continue into the net and still score legitimately. A keeper cannot cancel a shot outside its hand reach or rescue a goal retroactively.
- A slow collectible loose ball actually within the hand sphere can be collected without a probabilistic fast-shot roll, including after it settles inside a prior contact area. No remote proximity ownership assignment is added. Keeper holds cannot score, and another athlete cannot simultaneously acquire them.

Distribution and restart:

- Each keeper secures possession for 1s by default, considers outlets every 0.2s, prefers forward field teammates with clearer lanes/less pressure and valid range, and aims before release. Current keeper pass speed is 11m/s, maximum target range 22m and receiving window 5s; these are independently tunable from field passes. The ball physically travels and can miss; only the existing legitimate reception/pickup changes owner.
- If no suitable outlet becomes available by the default 3s maximum hold, the keeper performs a physical clearance into the pool. A short aiming/settling allowance may add to that timer. No goalkeeper may stay human-selected or hold the ball indefinitely.
- All six motors pause/reset through the existing roster. Match Changed events reset keeper reaction/recovery, contact latches and distributor target/timers. Ball reset clears owner/reception/shot state. Scores and valid field selection persist. There are no delayed distribution coroutines/actions that survive a goal.

### Milestone 7 verification and handoff

Fresh pre-change baseline: both existing Edit Mode setup suites and ten existing Play Mode suites passed before adding keeper behavior. Background keyboard routing required temporary Editor-only Input System overrides (AllDeviceInputAlwaysGoesToGameView / IgnoreFocus) for injected-device tests while the Editor was unfocused; restore the original PointersAndKeyboardsRespectGameViewFocus / ResetAndDisableNonBackgroundDevices at handoff. Gameplay input settings/controls are not changed for this test environment.

Final automated verification in Unity 6000.6.7f1:

- Runtime/editor compilation completed without errors. All **17 final verification suites** compiled with zero diagnostics and passed: 14 Play Mode suites and three Edit Mode setup suites. Actual structured outputs are in ignored `Logs/Milestone7Final-*.json`; the 12 pre-change baseline results are in `Logs/Milestone7Baseline-*.json`.
- Original field regressions passed: MovementSandboxVerification, CameraBallPrototypeVerification, GoalScoringPrototypeVerification, TeamPlayPrototypeVerification, PossessionSelectionVerification, FriendlyAIVerification, OpponentAIVerification, TacticalOffenseVerification, StealDefenseVerification and TwoVTwoMatchVerification. Keepers are explicitly isolated in these field fixtures, preserving the previous movement/input, physical pass/shot, selection, scoring, stealing and restart assertions. AIPrototypeSetupVerification and TwoVTwoSetupVerification also passed on the full scene while checking field-only role counts and total physical actor counts correctly.
- GoalkeeperSetupVerification passed in Edit Mode: repeated final setup retained 143 objects, six distinct IDs/start markers, exactly one keeper per team, four field players, one observer and two distributors; keeper/camera tuning and shared references were preserved. No duplicate objects/components were created.
- GoalkeeperPositionVerification passed: both assigned goals/teams, keeper selection rejection, smooth lateral tracking to both sides, fixed waterline, enforced goal envelope and central return.
- GoalkeeperSaveVerification passed: 80m/s swept catches at both goals, one authoritative owner, no keeper selection/camera transfer, no duplicate catch, keeper-held ball beyond the plane rejected, valid inside-post shot outside reach scored, central reflection became collectible, glancing deflection entering the net scored, saveChance=0 allowed a physical goal without rerolls, and a 2s reaction delay prevented a point-blank high-speed catch. Probability endpoints and stationary-keeper fixtures made the outcomes deterministic.
- GoalkeeperDistributionVerification passed four physical collect/secure/pass/receive cycles (two per team), legitimate field reception/exclusive ownership, Blue field auto-selection/camera and White control isolation, outlet support decisions, plus a no-eligible-outlet timed clearance. Pass velocity/travel was checked before reception.
- GoalkeeperMatchVerification passed both teams' physical goals, two full six-athlete freeze/reset snapshots, cleared keeper recovery/ownership/outlet state and no stale post-goal distribution. A further 20 simulated seconds of live six-athlete play preserved field-only Blue selection/camera, exclusive possession, bounded keepers, non-stalling keeper holds, field-only pressing/pursuit and active offensive shooting.
- Final Console ground truth showed zero errors, zero warnings and compilationFailed=false; historical Pipeline journal counts were not treated as the current Console status. No Console clearing was used to hide failures.
- Actual Game-view captures `Logs/Milestone7Goalkeepers.png` and `Logs/Milestone7Catch.png` were visually inspected for the placeholder keeper and caught ball near the goal. Preview probabilities were restored, and runtime preview scores are not saved. This is not developer manual game-feel or mobile-performance verification.
- At the Milestone 7 implementation handoff Play Mode was stopped and temporary test overrides were restored/disabled. Original Player/URP settings, packages/build scene list and SampleScene were unchanged; new assets had `.meta` files and no AI Git write occurred. The developer subsequently manually tested and backed up Milestone 7 as `a1374a8` before authorizing Milestone 8.

Historical Milestone 7 limits (see Milestone 8 for the later control/charge changes): shots/passes stayed flat at the waterline; the hand volume was spherical with seeded skill decisions. That milestone added no charged shooting, manual keeper control, advanced trajectories/dives, official rules/clocks, halftime, fouls, stamina, full teams, final art, backend, multiplayer or economy. Keeper distribution/save balancing, kinematic field overlap and a selected-field camera leaving distant saves outside view remained limitations. Both keepers stayed in their station envelopes. No SAVE HUD, replay or cinematic effect was required.

Historical Milestone 7 manual instructions used immediate Space shots and AI-only keepers. Its no-keeper-selection rule is intentionally superseded for Blue possession by Milestone 8 below; do not apply the historical rule when testing the current scene.

To examine outcomes deliberately, during Play select `Actors/WhiteGoalkeeper > Prototype Goalkeeper` in the Inspector: temporarily set Save Chance=1 and Catch Chance=1 for catches, Catch Chance=0 for deflections, or Save Chance=0 for missed save attempts. Reaction Delay and Save Reach remain physical limits, so chance 1 does not guarantee an unreachable/late save. Play Mode tuning reverts on exit; do not save test overrides as production tuning accidentally. Keeper Hold Duration/Maximum Hold are on Goalkeeper Distribution. Moving/turning speed is on Swimmer Motor. Inspect `Window > General > Console` after play. No manual wiring is required.

Historical Milestone 7 setup: `Tools > Water Polo > Set Up Goalkeeper Prototype` retained 143 objects. The developer subsequently manually tested, committed and pushed it as `a1374a8` (`Implement AI goalkeepers and ball distribution - Milestone 7`). The current final setup command and changed selection/shooting policy are recorded in Milestone 8 below. No Git write operations are performed by AI or scene setup.

### Milestone 8 — Manual keeper possession, charged shots and readability

Historical Milestone 8 starting point: main at `a1374a8`, matching stored origin/main, with only old-plan.md untracked. No AI Git writes occurred. The developer subsequently manually tested this milestone successfully and backed it up as `734d8da` (`Implement manual goalkeeper control and charged shooting - Milestone 8`) before authorizing Milestone 9. Its original automated evidence below remains valid as historical coverage; device performance remains unverified.

#### Files and responsibilities

New files (each new Assets file has its Unity-generated `.meta`):

- `Assets/Scripts/Gameplay/ShotCharge.cs`: per-Blue-athlete human preparation state; reusable `BeginCharge`, `ReleaseCharge`, `CancelCharge` commands, independent of keyboard/UI and ball ownership.
- `Assets/Scripts/Gameplay/ShotPowerBar.cs`: one reusable uGUI presentation component on the existing score Canvas. Caches the selected actor's charge component only on target changes, updates an existing fill rectangle/color, and hides on release/cancellation/lock.
- `Assets/Editor/Milestone8PrototypeSetup.cs`: final repeat-safe setup menu, Blue keeper human adapters/ring, three charge components, one bar, explicit attacking-goal camera reference and readable goal material. New camera defaults apply only when the bar controller is first created; repeated setup preserves camera/charge/keeper tuning.
- `Assets/Materials/Prototype/ReadableGoalFrame.mat`: white URP Unlit frame presentation; no scoring/collision geometry change.
- `Tools/Verification/Milestone8Verification.cs`: focused `KeeperControl`, `ChargedShooting`, `Presentation` Play Mode entries and `Setup` Edit Mode entry. Runtime entries guard against being run in Edit Mode before mutating fixtures.

Modified files:

- `Assets/Scripts/Gameplay/PlayerSelection.cs`: possession-based temporary Blue keeper eligibility, remembered field fallback, field-only manual cycle and unlock cleanup. BallController ownership events remain authoritative.
- `Assets/Scripts/Gameplay/PrototypeGoalkeeper.cs`: suspend positioning while human-controlled; 3.2m/s configurable carrying speed; ordinary pool bounds with 1.1m clearance; swim back to the goal station after release/loss instead of snapping. Interception remains available only at the goal station. Configurable protection distance is 1.5m from the station envelope.
- `Assets/Scripts/Gameplay/GoalkeeperDistribution.cs`: reset pending AI distribution decisions while human control is active; White keeper and explicitly deselected Blue keeper retain AI distribution.
- `Assets/Scripts/Gameplay/SwimmerMotor.cs`: transient speed override for human keeper carrying; cleared on stop/disable/reset. Existing field physics/acceleration/waterline movement remains shared.
- `Assets/Scripts/Gameplay/BallController.cs`: near-goal keeper steal protection now depends on its configured station distance; a keeper carrying into the pool uses the existing opposing steal rules. Ownership/release, swept saves and scoring remain single authorities.
- `Assets/Scripts/Gameplay/SwimmerBallInteraction.cs`: speed-taking `TryShoot(float)` overload shares the existing physical release; parameterless `TryShoot()` retains direct AI shooting.
- `Assets/Scripts/Gameplay/BallInteractionInput.cs`: press begins charge, release executes it; passing or disabling the adapter cancels charge. Handoffs suppress same-frame button replay and wait for an already-held shoot button to be released before accepting a new charge.
- `Assets/Scripts/Gameplay/GameplayCamera.cs`: smooth keeper/field height/distance blending and a small explicit attacking-goal framing bias for field play. Existing elevated angle, ball influence, focus smoothing and pool limits remain.
- `Assets/Editor/GoalkeeperPrototypeSetup.cs`: wires the shared PoolBounds reference required by keeper carrying/return.
- `Assets/Scenes/PrototypePool.unity`: all components/references, Blue keeper marker, bar hierarchy, camera tuning and visible frame material saved. SampleScene is unchanged.
- `Tools/Verification/{GoalkeeperSaveVerification,GoalkeeperMatchVerification,GoalkeeperDistributionVerification,GoalkeeperSetupVerification}.cs`: intentional Milestone 8 expectations replace the old Blue-no-control rule. Physical contact, ownership, pass travel, goal/restart and uniqueness assertions remain. Blue AI distribution tests explicitly return control to a field player before expecting automatic release. Live holding timeouts apply to AI keepers, not a deliberately controlled human keeper.
- Root `plan.md`, `README.md`, `AGENTS.md`: synchronized current behavior, controls, actual evidence and manual-test status. No old migration reference was removed.

#### Control, shooting and presentation policy

- A genuine friendly ownership event selects the Blue carrier, including its goalkeeper. Approaching a shot or attempting a pass does not select the keeper. Selection enables exactly one human movement/action pair, moves the existing-style green ring and retargets the camera. White possession never selects White.
- C cycles only registered friendly **field** players, including when used during a keeper hold; possession is unchanged. Deselecting the keeper intentionally resumes its AI. Keeper release/loss falls back to the last valid selected field player (or another eligible field); actual friendly pass reception then selects the receiver. Lock/reset cleanup cannot replay stale keeper possession.
- The controlled keeper moves/turns through SwimmerInput/SwimmerMotor at the fixed waterline, constrained inside the normal playable pool. Distribution and AI movement do not fight input. After release/loss it swims home and resumes goal-local tracking. Its trigger collider and conservative outer clearance prevent crossing goal geometry; this milestone does not add hard athlete-to-athlete collision resolution.
- Hold Space (gamepad south button likewise) while possessing to begin charge; release to shoot using facing at release. Duration defaults to **0.7s**, speed **10–22m/s**, with clamped maximum and a usable minimum for quick taps. Holding does not fire or alter possession; releases are one-shot. Loss, C handoff, pass, lock or input disable cancels. AI calls the existing immediate **12m/s** default shot API (or its existing chosen speed), without keyboard simulation. Future touch buttons can call the same charge commands.
- One bottom-centre bar uses the existing 1920x1080 CanvasScaler. It appears only while charging, fills green → yellow → orange/red and hides on cancel/release. No UI objects, scene searches or materials are created per frame. It does not control ball physics.
- The pool remains **20m X × 30m Z**. Both physical goal mouths remain **3m × 0.9m**, with their existing whole-ball directional planes, posts and keeper reach. Only frame material and camera presentation changed. Field camera: distance **10m**, height **11.8m**, pitch **50°**, FOV **48°**, focus smoothing **0.3s**, attacking-goal bias **1.2m**. Keeper framing: distance **8.5m**, height **10.5m**, profile smoothing **0.4s**. These are Inspector settings, with smooth retargeting and no new camera mode/cinematic system.
- Existing match handling freezes/resets all six athletes, clears possession and temporary charge/keeper actions, preserves scores and restores valid field input. A held keeper ball remains ineligible for scoring; valid physical deflections into the net still count.

#### Fresh verification in Unity 6000.6.7f1

Runtime/editor compilation passed. Executed checks compiled with **zero script diagnostics**. Structured final outputs are local ignored `Logs/Milestone8-*.json`:

| Executed entry/suite | Result / important coverage |
| --- | --- |
| `Milestone8Verification.KeeperControl` | PASS: actual Blue catch; exactly one input/ring/camera; AI hold/distribution suppression beyond timeout; WASD movement; fully charged keeper shot; lateral facing tap shot; swimming return without teleport; immediate physical B pass and receiver selection; C field-only; locked/unexpected loss; opposing steal cancels keeper charge; White keeper remains AI/distributes. |
| `Milestone8Verification.ChargedShooting` | PASS: injected Space hold/release, max clamp, usable quick tap, physical speed/direction, one shot per release, ownership preservation; cancellation on loss/switch/lock and no held-button replay; bar visibility; direct AI shooting and touch-ready API. |
| `Milestone8Verification.Presentation` | PASS: keeper viewport visibility, gradual field return, attacking field/held ball/goal in view; unchanged 3m × 0.9m opening. At the current 861x373 Game view the goal projects to approximately 65px wide. Actual `Logs/Milestone8PowerBar.png` was inspected for brighter frame, actor/ball readability and the visible bar. This is a prototype presentation check, not final game-feel validation. |
| `Milestone8Verification.Setup` | PASS: repeated final setup retains **147 objects**, six athletes/four field roles, three charge components, one bar and exactly one human input pair per Blue athlete; camera tuning survives YAML save/reload; saved field/keeper AI remains enabled. |
| `TeamPlayPrototypeVerification` | PASS: WASD isolation, C selection/ring/camera, exclusive possession, physical B/return passes, misses/expiry, human Space shots, two goals/restarts and subsequent pickup/pass/shot. |
| `StealDefenseVerification` | PASS: F input, range/facing/protection/shielding/cooldown, atomic successful steal/selection, team/lock rejection, coordinated defense/hysteresis and real AI pressure/steal. |
| `GoalScoringPrototypeVerification` | PASS: both swept whole-ball planes, posts/crossbar, held/reverse/missed shots, high-speed and long-shot goals, exact-once score/HUD and repeated restarts. |
| `TacticalOffenseVerification` | PASS: White pickup/advance/turn/physical shot/goal, physical passing/reception and anti-stall release. |
| `GoalkeeperSaveVerification` | PASS: **80m/s** catches at both goals with the new Blue/White selection policy, exclusive ownership, held-ball goal rejection, reach-limited goals, physical reflection, glancing-deflection goal, deterministic skill/reaction endpoints and no duplicate rerolls. |
| `GoalkeeperMatchVerification` | PASS: both teams' physical goals, two complete six-athlete freeze/reset snapshots, no stale distributions; **20 simulated seconds** of live six-athlete play with valid selection/camera, ownership, field-only tactics, bounded keepers and active offensive shots. |
| `GoalkeeperDistributionVerification` | PASS: four physical collect/secure/pass/receive cycles across both teams, Blue field opt-out restoring keeper AI, legitimate receiver selection and no-outlet timed clearance. |
| `GoalkeeperSetupVerification` | PASS: repeated keeper setup preserves **147 objects**, teams/roles/start markers/shared references, optional Blue human adapters, keeper reach and camera tuning. |

The selected consolidated regression was run after feature checks; all 17 historical suites were **not** rerun after minor edits. Other historical movement/camera/possession/friendly/opponent/2v2 setup suites and the old standalone keeper-position suite were not freshly run in this milestone; their previous results remain historical. Their critical selection/physics/tactics interactions were covered by the focused and selected regression above. Early verification exposed an Edit Mode test invocation, YAML float precision in a strict tuning comparison, and insufficient projected goal size at the first camera tuning; these were investigated, corrected and the affected assertions passed in final runs. No assertion about physical saves, goals or exclusive ownership was removed to force success.

Final current Unity Console ground truth: **0 errors, 0 warnings, compilationFailed=false**. Historical Pipeline journal counters are not the current Console; no Console clear was used to conceal failures. Play Mode is stopped; temporary Input System focus/background, Pipeline auto-tick and background-run overrides are restored/disabled. Incidental Player/URP settings were restored and generated Pipeline configuration moved to ignored Logs; packages/build settings remain unchanged. New assets have `.meta` companions. SampleScene hash remains `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`.

#### Manual handoff, limits and next step

The saved scene is ready: Project panel **Assets > Scenes > PrototypePool**, double-click, press **Play**, click **Game**. WASD/arrows swim, E collects, B passes, **hold/release Space** shoots, C cycles Blue fields, F steals. Optional setup: stop Play Mode, save intended edits with **File > Save**, then **Tools > Water Polo > Set Up Milestone 8 Prototype**; no manual reference wiring is needed.

Test a quick tap and a full charge on a Blue field carrier; confirm the bar disappears on release, pass, loss and C switching. Let White attack the Blue goal: an actual catch should select the keeper, move its ring/camera, and suspend AI distribution even while holding beyond three seconds. Swim/turn and B-pass to a visible field outlet, or charge/release a facing shot; field control must return and the keeper must swim home. White keeper catches should remain autonomous. During a Blue keeper hold C may return to a field player deliberately, resuming keeper AI distribution. Repeat at both goals and check scores, all-six restarts and the next pickup/pass/shot.

Inspector tuning: **Actors > [Blue goalkeeper] > Prototype Goalkeeper** for carrying speed/protection; the three Blue actors' **Shot Charge** for duration/min/max speeds; **CameraRig > Main Camera > Gameplay Camera** for field/keeper framing, smoothing and goal bias, and **Camera** for FOV. Goal Mouth dimensions and keeper save/reaction values are unchanged.

Limitations: manual gameplay feel, goal/camera preference across aspect ratios, gamepad feel and mobile performance/builds remain unverified. Camera still follows the selected actor; a distant ball/save may leave frame. Human keeper possession has no automatic timeout; C explicitly gives control back to fields. Interception remains near the keeper station, rather than a general field catch mechanic. Ordinary kinematic overlap/soft-spacing limits remain. No touch HUD, lob/skip/charged AI shots, dives, clocks, fouls, full teams, rigged art, realistic water, backend, monetization or later milestone was implemented.

The developer completed Milestone 8 manual review and backup as `734d8da`, then explicitly authorized Milestone 9. The current milestone below supersedes this historical handoff's team count, clock-free state and setup command. Only the developer may perform Git writes; old-plan.md stays excluded.

### Milestone 9 initial implementation — historical 3v3 stage

**Historical record:** the following initial-stage counts (eight), 90/480 timing, 30-second possession clock, fixed ends and manual instructions are superseded by **MILESTONE 9 — COMPLETE FULL-TEAM PROTOTYPE EXPANSION** below. Preserve the original evidence without treating old fixture expectations as current defaults. This is one milestone, not a new Milestone 10.

Authorized scope: three field swimmers plus one keeper per team, coordinated tactics, match/shot clocks, shared sprint/stamina, real aerial lobs and an extended prototype HUD. Initial read-only inspection found `main` at **734d8da**, matching stored origin/main, with only old-plan.md untracked. Milestone 8 is developer-manually verified. No Git checkpoint/write was performed. Milestone 9 manual playtesting and device builds/performance remain pending.

#### Team structure and tactical model

- Eight athletes: three Blue human-eligible fields, three White AI fields and the two existing keepers. Existing four field identities and both keepers are retained. Added IDs are `prototype-blue-3` and `prototype-white-3`; every actor has its own start marker. A `FieldPositionRole` (Central/Left/Right) is separate from AthleteRole, so keepers are never counted as field formation slots. Loops use roster membership rather than a fixed three-player runtime array.
- Prototype starts: Blue centre `(0,0,0)`, wings `(-5,0,-2)` / `(5,0,-2)`; White centre `(0,0,6)`, wings mirrored at Z=8. Keeper starts and goal geometry remain unchanged. First expansion initializes these; repeat setup preserves subsequent start/role tuning. Future full formations still need additional role/slot definitions and balancing.
- PrototypeTeamTactics remains the shared team context. Role lanes provide central/wide support, clamp forward progress near goal and maintain separation from the carrier. One presser is selected with assignment intervals and hysteresis; other defenders receive distinct marking ranks or zone lanes. Press closes space aggressively; Zone protects central/wide stations nearer the defended goal and only presses very close danger. Z toggles Blue strategy; White chooses with a three-second interval and 7m/11m danger hysteresis.
- Pass scoring includes range, progress, receiver pressure/openness, lane obstruction, direction and receiver shot quality; pass tuning remains serialized. Selected AI receivers use the shared directed physical pass API. Defenders continue defending/contesting during flat passes; only the intended receiver waits for its normal pass. During lobs eligible pursuers/receivers move toward the estimated landing point; no high-ball ownership is fabricated.
- Shot quality includes range/alignment, blockers/pressure and a keeper-position penalty. Effective shooting range is capped by 85% of BallController's unobstructed `speed / waterDrag` travel estimate, so low configured shot power cannot receive an optimistic out-of-range quality score. Aim chooses a mouth-width/radius-safe corner opposite the opposing keeper, with configurable AI shot speed (default 16m/s). The existing minimum/maximum hold timers remain; under five shot-clock seconds the carrier prioritizes a shot without waiting for a fresh minimum hold. This is a deterministic prototype quality heuristic, not a calibrated real-world scoring probability.
- The available local old-plan descriptions of TeamSide/WaterPoloBrain/MatchContext, compressed MatchTimer, ShotClock keeper-hold behavior and StaminaSystem were reviewed conceptually. Actual legacy source groups were not present; no 2D source/physics or old coordinates were imported.

#### One authoritative match phase and transition contract

PrototypeMatch retains whole-ball directional goal detection and score authority and now owns `PreMatch`, `Playing`, `GoalRestart`, `TurnoverRestart`, `QuarterBreak`, `FullTime`. `IsPlaying` is the shared competitive action guard. `IsRestarting` remains a compatibility query for goal/turnover restarts, not a second phase flag. Keeper/field/steal/interception adapters and BallController reject stopped play. PlayerSelection's input lock is applied by the central transition; charge/input disable cancels pending shots.

- MatchClock is a serialized timing model inside PrototypeMatch, not another independent ticking manager. Defaults: **four quarters, 90 live seconds per quarter, 480 displayed seconds**. Display ratio is 480/90. Physics-step time advances only while Playing; goal/turnover pauses, quarter breaks, ready state and full time do not consume clock time. Default time scale is unchanged.
- PossessionShotClock is one event observer advanced by PrototypeMatch after goal and quarter checks. Default **30 live seconds** per team possession, independent of displayed quarter compression. A real opposing acquisition resets once; same-team passing, human switching, failed steals and keeper collection/distribution preserve remaining time. Release/loose flight retains the last possession's ticking clock; before first acquisition the shot clock waits. Goal and quarter restarts establish a fresh clock.
- A completed goal crossing observed in a physics step takes priority over that step's clock deadline. Scores increment once and the existing GOAL/restart delay pauses both clocks. If little quarter time remains, the quarter can finish shortly after the goal restart; no extra goal is awarded during that transition.
- Shot-clock expiration requests one controlled turnover: lock inputs, pause all eight motors, cancel charge, clear owner/pass/flight, show turnover notification, then reset legal positions. The awarded team's first registered field player receives a range-checked BallController pickup at its start. Blue selection is restored only to eligible Blue athletes. Clock/score authorities are not duplicated.
- Quarter end locks all eight, clears pending ball movement and shows Continue/Start. Continue resets starts/ball and begins the next quarter, preserving score and stamina. Last-quarter end shows BLUE/WHITE final score plus WIN/LOSE/DRAW from Blue's perspective. Restart Match resets scores, quarter/shot clocks and stamina. Return to Prototype opens the **same scene's paused ready state**; Continue/Start starts a fresh match. No career scene or external menu was added.
- Every central transition notifies the existing team/keeper/match listeners to clear obsolete decisions, reaction/recovery and distribution targets. Restarts restore field selection if an old keeper hold is no longer valid. No delayed throw coroutine or arbitrary action-delay workaround was introduced.
- **Halftime ends swapping is deferred**: goal score attribution, goal-local keeper stations, tactical goals, camera attack reference and restart positions currently use fixed ends. They must change atomically in a separately verified feature. Blue continues attacking +Z and White -Z through all quarters.

#### Sprint and stamina

SwimmerStamina is shared by every field motor, with the same rules for humans and AI. Hold Left Shift with movement to request sprint. Normal tuned speed remains 4m/s; default multiplier is **1.5** (6m/s at full input) and sprint acceleration multiplier **1.25**. Default normalized drain is **0.16/s**; recovery **0.12/s** at rest and **0.06/s** during ordinary swimming. Values clamp to [0,1]. Exhaustion blocks further sprint until **0.25** recovery, preventing rapid zero-stamina oscillation. Field AI requests bursts for distant loose balls, defensive pressure and useful unpressured carries; it has no unlimited White-team stamina exception.

The motor advances fatigue once per enabled live physics step. Stamina freezes during stoppages, persists through goals/turnovers/quarters and resets on the authoritative NewMatch event. Selection/stop clears sprint requests without refilling stamina. Keepers have an explicit non-sprinting profile; controlled carrying remains 3.2m/s. One ActiveStaminaHUD bar follows selected Blue fields or labels the keeper profile.

#### Real aerial flight

- B remains the flat pass; **L** calls LobPassing. It chooses a modest assisted facing direction and eligible field recipient, never teleports possession. Default unassisted range is **8m from release**, max assisted range **16m**, flight duration **1.3s**, assist cone **30°**, correction cap **12°**.
- BallController remains the sole authority. `BallReleaseKind.Lob` / `IsAirborne` enable actual Rigidbody gravity, unlock Y and remove water drag for flight. Horizontal velocity reaches the bounded target over the requested duration; initial vertical velocity is `-Physics.gravity.y * duration / 2`, approximately a 2.07m rise for 1.3s at default gravity. LandingPoint predicts from the current physical velocity/height, so collision changes affect pursuit.
- Rising/high balls cannot be collected. A descending ball within **0.65m above SurfaceHeight**, after release protection, can be legitimately received/collected within the existing horizontal reach. This prevents unrealistic high pickups and permits low reception before a ball rests on a swimmer collider. Opponents can contest eligible low/landed balls. Keeper interception still requires actual swept 3D hand contact and its existing reach/reaction rules.
- On reception, landing, deflection or reset, the shared controller restores flat water physics and exclusive possession rules. Landing synchronizes the water pose before restoring the Y lock/interpolation, avoiding a stale PhysX underwater constraint anchor. Releases use SwimmerMotor physics position/facing, avoiding stale interpolated render poses immediately after resets. Flat charged shots and normal passes keep the same release authority.
- Existing GoalMouth whole-sphere checks include vertical clearance; above-crossbar crossings remain invalid and there is no new scoring authority. Airborne XZ containment reflects at pool limits, while PhysX handles posts/athletes. Low keeper deflections return to the prototype's flat-water model. A pooled inexpensive ground disk cues height only while airborne; no per-frame object/material creation or water particles.

#### Camera, HUD and setup

The existing camera remains the only camera behavior. Initial M9 field framing is distance **10.5m**, height **12.2m**, pitch **50°**, FOV **50°**, goal bias **1.5m**; nearby-ball influence/range/max offset become **0.2 / 12m / 2m**. Existing keeper profile and smooth retargeting remain. These initial changes apply only on first M9 presentation setup; repeat setup retains custom tuning. The camera is not a full-pool overview; a distant ball or athlete can leave view.

The existing Canvas/scoreboard/charge bar remain. MatchHUD adds quarter/display time, shot time, defense label, turnover message and quarter/full-time/ready panel. ActiveStaminaHUD adds the selected-athlete bar. UI objects and button listeners are created/wired once; no UI object or scene search is created every gameplay frame. uGUI uses the new Input System UI module/EventSystem and existing resolution scaler. This is a prototype landscape layout, not a finished touch interface.

Saved scene command: stop Play Mode, save intended edits (**File > Save**), then **Tools > Water Polo > Set Up Milestone 9 Match**. No manual Inspector wiring is needed. Repeated setup retains eight identities/start markers, one ball/match/shot-clock authority and current tuning; old lower-level builders accept/preserve extra registered athletes. SampleScene is never saved or overwritten.

#### Files created or modified

New runtime files under `Assets/Scripts/Gameplay/`: `MatchClock.cs`, `PossessionShotClock.cs`, `SwimmerStamina.cs`, `LobPassing.cs`, `TeamStrategyInput.cs`, `MatchHUD.cs`, `ActiveStaminaHUD.cs`, `BallHeightCue.cs`. Each has its Unity-generated `.meta`.

New Editor files: `Assets/Editor/Milestone9PrototypeSetup.cs` and `MatchHUDSetup.cs`, both with `.meta`. New material: `Assets/Materials/Prototype/BallShadow.mat` and `.meta`. New verification file: `Tools/Verification/Milestone9Verification.cs` (outside player builds).

Modified runtime files under `Assets/Scripts/Gameplay/`: `SwimmerIdentity.cs` (field roles), `PrototypeTeamTactics.cs`, `WaterPoloDecision.cs`, `PrototypeSwimmerAI.cs` (coordinated tactics, action/urgency/sprint), `PrototypeMatch.cs` (phases/clocks/transitions), `PrototypeScoreboard.cs` (GOAL only for goal restarts), `BallController.cs` (live action guards, real flight/low pickup/restoration, physics release pose), `BallInteractionInput.cs` (L action), `SwimmerMotor.cs` (shared stamina and authoritative Facing), `SwimmerInput.cs` (Shift), `SwimmerPassing.cs` (physics facing), `SwimmerSteal.cs`, `PrototypeGoalkeeper.cs`, `GoalkeeperInterception.cs` (central live-phase guards).

Modified Editor files: `Assets/Editor/TeamPlayPrototypeSetup.cs`, `BasicAIPrototypeSetup.cs`, `TwoVTwoPrototypeSetup.cs` (preserve expanded teams; expose the shared setup helper internally). Modified scene: `Assets/Scenes/PrototypePool.unity`. Updated existing verification: `Tools/Verification/GoalScoringPrototypeVerification.cs` parks all non-shooters away from its clear-lane fixture, including the added central defender; `StealDefenseVerification.cs` explicitly selects Press and suspends/restores White automatic mode for its pressure-specific assertions. No physical goal/steal assertions were removed. Updated root docs: `AGENTS.md`, `README.md`, `plan.md`. No packages, SampleScene or unrelated project settings changed. Existing `.meta` files and old-game references remain intact.

#### Verification and manual handoff

Fresh verification in the running **Unity 6000.6.7f1** Editor: runtime/editor compilation passed. All **13 distinct executed verification entry points** passed (seven new focused/integrated entries plus six reused entries). There are 12 passing final structured result files in ignored `Logs/Milestone9-Final-*.json`, plus the initial passing expansion result in `Logs/Milestone9-A-Expansion.json`. All final verification scripts compiled with zero diagnostics.

| Executed entry | Actual coverage / result |
| --- | --- |
| `Milestone9Verification.Setup` | PASS: **183 objects**, eight unique athletes/start markers, 3+1 per team, distinct Central/Left/Right roles; repeat setup preserves identity, camera/stamina Inspector tuning, one match/ball/shot clock, valid Input System UI references and single persistent button listeners. |
| `Milestone9Verification.Expansion` | PASS: C cycles three Blue fields, White excluded, exactly one movement input, eight athletes bounded at the waterline. |
| `Milestone9Verification.Tactics` | PASS: one presser, distinct Press/Zone positioning, three zone lanes, spaced role support, obstructed/open pass lanes, deterministic stable decisions, low-power range rejection and shooting urgency under five possession seconds. |
| `Milestone9Verification.Clocks` | PASS: live countdown; real same-team pass/reception and keeper hold preserve time; C preserves time; opposing acquisition resets once; charged-shot cancellation on timeout; stopped clocks during turnover; legitimate opposing restart; compressed quarter display, all-eight freeze/reset, quarter continuation, full time DRAW/panel, ready state and new match. Shortened runtime-only fixture tuning was restored. |
| `Milestone9Verification.Stamina` | PASS: injected W+Shift reaches sprint speed and drains, release restores tuned speed, rest recovers, held Shift survives control switching with only the new actor sprinting; exhaustion/recovery threshold/clamps, persistence through turnover, new-match reset and real AI pursuit using the same drain. Keeper non-sprint profiles verified. |
| `Milestone9Verification.Aerial` | PASS: injected L, real gravity/height above 1m, high pickup rejected, low physical receiver acquisition/auto-selection, immediate subsequent flat shot, missed-lob collectible waterline landing, above-crossbar geometry rejection and in-flight turnover cleanup. |
| `Milestone9Verification.Integration` | PASS: actual last-seconds goal before quarter deadline, retained score/goal pause then quarter break; keeper catch preserving same-team countdown and human selection, controlled physical keeper lob/field reception, turnover cleanup, high lob near keeper outside real reach, quarter transition canceling flight, **six eight-athlete reset snapshots**, then **16 simulated seconds** of all-AI-enabled play with exclusive possession, bounded athletes/fatigue, valid selection/camera and an actual White offensive shot. Z input toggling verified. |
| `Milestone8Verification.KeeperControl` | PASS: Blue catch/control/ring/camera, AI distribution suspension, manual swim/fully charged and lateral keeper shots, swimming return, physical B outlet/reception, field-only C, locked/unexpected loss, steal canceling keeper charge and unchanged White AI distribution. |
| `Milestone8Verification.ChargedShooting` | PASS: actual hold/release, quick tap, max clamp, no early/duplicate shot, facing direction/speed, loss/switch/lock cancellation, no held-button replay, reusable charge API/bar and direct AI shot compatibility. |
| `GoalkeeperSaveVerification.Run` | PASS: both **80m/s** catches/exclusive owner, correct Blue/White control policy, held-ball goal rejection, unreachable shot scores, real reflection, glancing deflection goal, seeded skill/reaction endpoints and no duplicate save rerolls. |
| `GoalScoringPrototypeVerification.Run` | PASS: both whole-ball planes, partial/held/reverse/post/crossbar/miss rejection, high-speed/long/cooldown-crossing goals, scoreboard/GOAL and repeated exact-once restarts. Clear-lane fixture now isolates the complete roster. |
| `StealDefenseVerification.Run` | PASS: F input/range/facing/protection/shielding/cooldown, atomic steal/auto-selection, same-team/lock rejection, explicit Press coordination/hysteresis and actual AI pressure/steal. Automatic strategy tuning restored after fixture. |
| `TacticalOffenseVerification.Run` | PASS: White real pickup/advance/turn/physical shot/goal, improved-option physical pass/legitimate White reception and configurable anti-stall release. |

The consolidated pass reused relevant earlier suites; it did not run every historical suite after each edit. Scoped fixture corrections were rerun, and focused stamina/integration checks followed the held-Shift handoff and allocation-free tactical-loop refinements. The final pure shot-range assessment addition was checked by Tactics and physical TacticalOffense; default shot tuning is unchanged. Other historical fixed-count setup/team suites were not freshly run; their 2v2/six-athlete assertions are historical until adapted for the current scene. No manual game-feel, long-run match balance, every aspect ratio or Android/iOS performance claim is implied.

Final Unity Console ground truth: **0 errors, 0 warnings, compilationFailed=false**. Historical Pipeline journal counters include earlier sessions and are not the current Console. No Console clearing was used to hide failures. Play Mode is stopped; temporary focus/background routing, auto-tick and Run In Background test overrides are restored/disabled. Incidental Player/URP settings were restored and generated Pipeline configuration moved under ignored Logs. SampleScene hash remains `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`. New assets have all companion `.meta` files; code/Markdown whitespace checks passed. Current source contains no gameplay scene-wide searches.

Actual Game-view captures `Logs/Milestone9Match.png`, `Milestone9QuarterBreak.png`, `Milestone9FullTime.png` were inspected for prototype state/HUD readability. Full-time capture uses the two-quarter shortened test fixture, not saved default match tuning. Default scene configuration remains four 90-second quarters and a 30-second shot clock. UI wiring/state was verified; developer mouse/controller feel and final presentation remain manual review tasks.

Known prototype limits: fixed ends/no halftime swap; simple geometric roles/spacing and deterministic pass/shot heuristics need balancing; kinematic athlete overlap is still possible; no referee/out-of-play process (an aerial ball may physically lodge on goal geometry until the possession-clock restart); AI currently chooses flat passes while the shared lob API is available to both teams and the controlled keeper. Keeper deflections flatten to water after contact; no advanced lob/skip shots or dives. Stamina recovery during breaks is frozen, not an official fatigue/substitution model. No official fouls/exclusions, touch HUD, mobile builds, full teams, final art, backend, multiplayer, economy or later milestone was added.

Open **Assets > Scenes > PrototypePool**, Play, click Game. WASD/arrows swim; E collects, B passes, Space hold/release charges/shoots, C cycles three Blue fields, F steals, **Shift sprints, L lobs, Z toggles defense**. Test keeper catches/control, B/L distribution and charged keeper shots. Watch stamina across possessions and quarters. Quarter breaks require clicking Continue/Start; full time offers Restart Match and Return to Prototype. The default match has six minutes of live play plus breaks/restarts. For an optional faster flow-only review: stop Play, select **Hierarchy > Gameplay > PrototypeMatch**, expand **Inspector > Prototype Match > Clock**, set Real Seconds Per Quarter temporarily to 10, then Play; restore 90 afterward. The displayed quarter remains eight minutes. Keep the default quarter length when manually testing a full 30-second shot-clock possession.

Next task: developer manually reviews Milestone 9 (controls, AI pacing/spacing, fatigue, air pickups, clock transitions and HUD/camera). Only the developer stages/commits/pushes reviewed changes. Milestone 10 is not authorized. No Git write operation was performed; old-plan.md remains untouched and excluded.

### MILESTONE 9 — COMPLETE FULL-TEAM PROTOTYPE EXPANSION

Implemented on 2026-11-10 in Unity 6000.6.7f1, extending the initial M9 implementation in place. Read-only inspection found `main` at `734d8da`, matching stored `origin/main`, with the earlier M9 implementation already unstaged/uncommitted. No Git write operation or checkpoint was performed. The previous user-confirmed manual gameplay evidence belongs to Milestone 8 and earlier; **this full-team build requires developer manual review**. Available old-plan descriptions informed role/team/clock separation; no old source clone or Rigidbody2D implementation was imported.

#### Team composition and shared tactics

- Exactly **14 athletes: six fields plus one existing goalkeeper per team**. Initial eight athlete identities and existing GameObjects remain. New fields use `prototype-blue-4/5/6` and `prototype-white-4/5/6`; setup assigns unused cap numbers without changing the earlier identities. All twelve fields share the same physical motor, ball interaction, AI decision/action adapter and stamina components.
- `SwimmerIdentity.FieldPosition` gives six stable roles independently of team, cap, object name and goalkeeper AthleteRole. Previously serialized Central/Left/Right values 0/1/2 retain their indices as CentreForward/LeftWing/RightWing; CentreBack/LeftFlat/RightFlat add slots 3/4/5. Keepers remain outside field assignment, pursuit, human C cycling and role counts.
- New serialized `FieldFormation` entries store period-start, attack and zone offsets in team coordinates: lateral X and forward progress relative to pool centre/current AttackDirection. Runtime roster loops do not require exactly six; missing roles have a generic layout, and repeated roles receive extra spacing offsets. The default six slots are:

| Role | Period start (lateral, forward) | Attack | Zone |
| --- | --- | --- | --- |
| Centre Forward | (0, -3.5) | (0, 11) | (0, -6) |
| Centre Back | (0, -10) | (0, 3.5) | (0, -10) |
| Left Wing | (-7, -5.8) | (-7, 9) | (-6, -9) |
| Right Wing | (7, -5.8) | (7, 9) | (6, -9) |
| Left Flat | (-3.8, -7.6) | (-4, 6.5) | (-3.5, -6.5) |
| Right Flat | (3.8, -7.6) | (4, 6.5) | (3.5, -6.5) |

`PrototypeTeamTactics` converts these through current team direction and pool bounds. Attack slots translate with carrier progress, retain a minimum carrier separation and a goal-side centre back. This creates five off-ball options instead of five followers. Zone maintains six lanes with limited threat bias. Press chooses one suitable field-carrier presser with hysteresis; remaining defenders get distinct marking ranks blended toward their role zones. Keeper possession does not allocate a bogus field presser. Shared existing pass-risk/open-lane, shot-range/keeper/pressure, shot-clock urgency and sprint/stamina decisions remain. Goalkeeper distribution evaluates registered field teammates, now six candidates. No duplicate tactical brain or possession manager was added.

All six Blue fields cycle with C. Every legitimate friendly field acquisition still selects that recipient; a confirmed Blue keeper catch temporarily grants keeper control. Releasing keeper possession returns valid field control. White is never human-selectable. Existing ring, charge cancellation, input exclusivity and camera targeting remain centralized in PlayerSelection.

#### Four periods and possession clock

- MatchClock remains the single serialized period timer inside PrototypeMatch. Defaults: **4 periods, 80 real live seconds, 480 displayed seconds, ratio 6**. Display starts Q1 — 8:00; after 10 live seconds it is 7:00, after 40 it is 4:00, and after 80 the period expires once. Each period starts a fresh double-precision live accumulator; derived remaining time clamps to zero, including fixed-step floating-point boundary tolerance.
- PrototypeMatch.AdvanceLiveTime is the shared clock path for normal FixedUpdate and deterministic verification. It advances only in Playing, clamps to the earliest clock deadline, and ignores invalid/non-positive deltas. Goals, turnover restarts, quarter breaks, HalfTime, PreMatch and FullTime stop both clocks. A valid completed crossing already observed during the physics step takes precedence over that step's deadline.
- Q1 -> QuarterBreak -> Q2 -> **HalfTime** -> Q3 -> QuarterBreak -> Q4 -> FullTime. Continue resumes; full time retains scores and reports WIN/LOSE/DRAW. No extra time or shootout. Total default live time is **320 seconds (5 minutes 20 seconds)** plus stoppages.
- PossessionShotClock defaults to **28 actual live seconds**, independently configurable through duration and clockRate (default 1). It does not use the period factor 6. True opposing-team acquisition resets it once; loose flight, same-team passing, manual selection and same-team keeper possession preserve the current possession. Expiration enters the existing controlled opposing turnover restart, canceling charge/ownership/actions and pausing the quarter until resumption.
- The 28-second baseline follows [World Aquatics competition regulations, Part Six §9.2 (18 February 2026)](https://resources.fina.org/fina/document/2026/02/18/e6815ecc-06d9-4f0b-98e9-4c441cf5e6a3/2026-02-18_World-Aquatics_CR-Final.pdf). This prototype does not implement the full foul/exclusion/rebound reset exceptions; its independent real-time tuning is deliberate.

#### Halftime, score attribution and restart contract

Halftime enters a stopped phase after Q2 and displays **HALFTIME — SWITCHING ENDS** with the current score. No end change occurs until Continue. PrototypeMatch then atomically remaps both teams' attack/defend GoalMouth references, each physical goal's current ScoringTeam, both keepers' DefendedGoal and reaction state, the human camera's attacking-goal reference, and every roster start marker. PrototypeRoster mirrors base poses 180 degrees around pool centre; it does not repeatedly rotate already-mirrored positions. The same athletes reset into their new defensive halves with current facing. Exactly one reversal occurs; a second Continue is ignored once Playing. Q4 retains Q3 ends. Restart Match restores the captured Q1 goals/base poses, clears the switch count, score and clocks.

Physical goal geometry/directional whole-ball crossing is unchanged (3m opening, 0.9m high). Crediting follows the team currently attacking that physical goal, independent of last shooter; this also handles own goals after reversal. Goalkeepers' station/tracking/save/distribution logic reads their new defended goals. Held balls cannot score. No visual-only camera rotation or permanent Blue/+Z scoring rule was introduced.

Period/new-match starts use a **neutral centre ball and separated own-half positions**, not an official sprint-duel/swim-off subsystem. After a goal, the **conceding team** is awarded a valid range-checked pickup by its first eligible field player after reset. Scores survive goals, turnovers and periods. Central stop/reset logic locks human actions, clears charge and obsolete flight/ownership, stops AI/keeper decisions, resets all fourteen athletes and restores valid selection. No queued keeper outlet survives a break. Halftime preserves the instantaneous camera pose and smoothly follows the changed target; it does not SnapToTarget during the ends swap.

#### Camera, HUD, setup and changed files

The existing camera keeps its consistent world orientation. Initial full-team setup widens field distance/height to 13/15m, FOV 52, goal bias 1.8m, keeper distance/height 10.5/12.3m and limited ball-aware focus. Existing smoothing, pool clamps and role blending remain; repeat setup preserves subsequent Inspector tuning. HUD reuses score, Q/time, shot clock, stamina, defense, charge bar and modal buttons, adding the halftime state rather than another Canvas/controller.

Open the already saved `Assets/Scenes/PrototypePool.unity`. Optional repeat-safe setup outside Play Mode: **Tools > Water Polo > Set Up Milestone 9 Match**. No manual wiring is required. Exactly six fields and one keeper per team are registered; existing actors, goals, UI and camera are preserved. Unexpected excess fields cause a clear setup failure rather than deletion. The verified scene has **243 GameObjects**, fourteen unique athlete identities and one existing match/ball/shot-clock authority. SampleScene is unchanged.

Files added by this final expansion: `Assets/Scripts/Gameplay/FieldFormation.cs` and its `.meta`; `Tools/Verification/FullTeamMilestone9Verification.cs` (outside player builds).

Files changed by this final expansion: `Assets/Editor/Milestone9PrototypeSetup.cs`; `Assets/Scenes/PrototypePool.unity`; runtime `SwimmerIdentity.cs`, `PrototypeTeamTactics.cs`, `PrototypeRoster.cs`, `MatchClock.cs`, `PossessionShotClock.cs`, `GoalMouth.cs`, `PrototypeGoalkeeper.cs`, `GameplayCamera.cs`, `PrototypeMatch.cs`, `MatchHUD.cs`; `Tools/Verification/GoalkeeperSaveVerification.cs`; `AGENTS.md`, `README.md`, `plan.md`. The keeper verification now parks all non-shooters outside the shot lane so the new centre back does not legitimately block its isolated keeper fixtures. No save/geometry assertion was removed. Other initial-M9 source/material/meta changes listed in its historical section remain part of the combined uncommitted milestone.

#### Fresh verification for the full-team expansion

Actual Unity Editor compilation passed. All **six executed entry points** below passed with zero script compilation diagnostics. Results are preserved in ignored `Logs/Milestone9Full-*.json`; these are fresh results, separate from initial-M9 historical evidence.

| Executed entry | Actual result and coverage |
| --- | --- |
| `FullTeamMilestone9Verification.Setup` | PASS: 14 distinct actors, 6+1 per team, six roles, separated own-half starts, original identities retained, 4/80/480/6 and independent 28 defaults, repeat setup stays at 243 objects and preserves camera tuning. |
| `FullTeamMilestone9Verification.TimingAndEnds` | PASS: 10/40/80 live-second conversion, 4000 fixed steps reaching the 80-second deadline, stopped clocks, Q1/Q2 sequence, halftime panel and single guarded Continue swap, both goal/keeper mappings, all fourteen physical mirrored poses, unchanged instantaneous camera pose, Q3/Q4/full-time DRAW, new-match original ends restored. |
| `FullTeamMilestone9Verification.GoalsAndKeepers` | PASS: five physical goals including Blue/White goals after halftime and a Blue own goal credited White; exact-once/conceding restarts; keeper-held/charging state cleared at Q2; both keepers catch at their new goals and execute two physical distributions; fourteen-actor resets and valid field selection. |
| `FullTeamMilestone9Verification.RolesAndPossession` | PASS: injected C selects all six fields; real pickup auto-selects each with one input/camera; five spaced support options, one Press presser and six Zone slots, keeper-possession assignments, independent 28-second clock, same-team keeper/C preserving time, opposing reset once and charged-turnover cleanup/awarded restart. |
| `FullTeamMilestone9Verification.Gameplay` | PASS consolidated regression: injected W/Shift movement and stamina drain; physical B pass/reception and preserved possession clock; max-charge/release shot; true-height L lob/high pickup rejection/reception; F protection/cooldown/success; actual White AI physical goal before and after halftime; **20 simulated seconds** of live all-AI-enabled full-team play across the two halves with bounds/waterline/fatigue, exclusive ownership and valid selection/camera assertions. |
| `GoalkeeperSaveVerification.Run` | PASS reused regression: both 80m/s catches/exclusive owner/Blue-keeper control, held-ball scoring rejection, out-of-reach shot scoring, physical deflection/loose-ball continuation, glancing deflection goal, deterministic save skill endpoints/reaction delay and no repeated contact reroll. |

Clock tests use deterministic accelerated simulation instead of waiting through repeated complete real-time matches. The consolidated gameplay test uses actual physics/input updates. Historical fixed-count setup and initial-M9 clock suites were not redundantly rerun; their old eight-athlete/fixed-end defaults are superseded. No assertion was weakened to force success. Actual Game-view captures `Logs/Milestone9Full-Gameplay.png` and `Milestone9Full-Halftime.png` were inspected for state/readability; a separate capture-only check confirmed Q2 HalfTime and the Continue panel. Screenshots do not establish game feel or mobile performance.

Final Unity Console ground truth: **0 errors, 0 warnings, compilationFailed=false**. Pipeline historical journal counters are separate from current Console counts; no Console clearing was used to hide failures. Play Mode is stopped; temporary Input System focus/background routing, run-in-background override and auto-tick are restored/disabled. Generated Pipeline configuration was moved to ignored Logs. No ProjectSettings/package differences remain. SampleScene retains hash `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`. New Assets have all companion `.meta` files. Source/Markdown whitespace checks pass with Windows CRLF recognized; Unity's serializer emits trailing spaces for empty YAML fields in the scene, which were left as generated.

Final read-only Git state: `main`, HEAD `734d8da`, zero ahead/behind stored `origin/main`, **empty index**, 27 modified tracked files and 27 untracked files. Untracked files comprise 26 milestone implementation/meta/verification files plus the pre-existing untouched `old-plan.md`. This includes the initial M9 work and this expansion. No remote fetch was run, so stored upstream parity is not a fresh remote-network verification. No Git staging, commit, push, checkpoint or other write operation occurred.

#### Remaining limits and manual handoff

Full-team spacing, pass/shot heuristics, keeper balance and camera framing require manual tuning; kinematic athletes can still overlap. This is geometric tactical coordination, not final collision-avoidance or official referee logic. Period starts are neutral restarts, not a complete swim-off. No out-of-play/referee process, official foul/exclusion/rebound shot-clock exceptions, substitutions, final animation/art, touch HUD, device build/performance validation, backend, multiplayer or economy is included. AI still primarily chooses flat passes; the shared actual-height lob API works for human fields/keeper. Stamina is frozen during breaks rather than using an official rest/substitution model. Very long unattended sessions and every screen aspect ratio were not tested.

Manual review: open **Assets > Scenes > PrototypePool**, Play and click the Game view. C cycles six Blue fields; WASD/arrows swim, Shift sprints, E picks up, B passes, Space hold/release charges/shoots, F steals, L lobs, Z toggles Press/Zone. Check Blue keeper catch/control, passing out and returning to field control. Observe Q1 8:00 -> 7:00 after ten *live* seconds (goal/turnover pauses add wall time). Continue after Q1; after Q2 verify halftime, Continue once and check both keepers/teams at opposite ends, reversed attacks and correct scores. Q4 must retain Q3 ends. Full time offers Restart Match (restores original ends) and Return to Prototype. A full default match is 5:20 of live play plus stoppages. For a temporary transition-only review, outside Play select **Hierarchy > Gameplay > PrototypeMatch > Inspector > Prototype Match > Clock > Real Seconds Per Quarter**, set 10, and restore **80** afterward; do not use shortened periods to evaluate a 28-second possession.

Next step: developer manually tests this complete **Milestone 9** build, then personally reviews/stages/commits/pushes. Milestone 10 is not authorized. All changes remain unstaged/uncommitted; `old-plan.md` stays untouched and excluded. No AI Git checkpoint or write operation was performed.

## Optional old-game references

Previous game: Water Polo Manager, Unity `6000.4.7f1`, C#, 2D physics, URP 2D. Reference repository supplied by the developer: https://github.com/Nikoloz-Todua/WaterPoloManager . Remote contents were not verified in this session. A local reference clone has not been confirmed.

The developer is happy to share old files, screenshots, videos, and logic examples at any time. Codex can and should request the specific material useful to the task. Keep the old project and detailed old documentation outside this active project unless deliberately selecting something for reuse. Preserve the original project/repository. Do not upgrade it just to read files.

### Useful card and UI/UX ideas

- Mystery packs and collectible player cards are important references. The old plan describes Common, Rare, Epic, and Legendary PACK tiers shared by shop packs and match rewards.
- Confirm PLAYER rarities separately: an earlier old-plan description lists Common/Rare/Legendary only. Do not assume pack tiers and player rarities are identical; ask about Epic when designing the new system.
- Card information: identity, portrait, overall rating, position, and rarity styling.
- Old positions: GK (goalkeeper), CB (centre-back), LW/RW (wings), CF (centre-forward), LF/RF (flats). Preserve useful role concepts, not old enum ordering or starter-slot indices.
- Roster reference: starters, bench, position filtering, lineup changes, buy/sell/upgrade, team overall, club identity/customization.
- Opening reference: shared reveal screen, staggered reveals, rarity borders, NEW/duplicate feedback, duplicate-to-currency conversion, and a shared odds popup driven by the opening definitions.
- Reward reference: four post-match slots, timed unlock, one active unlock, ready/collect states, persistence across closing the app.
- Hub reference: club/profile, currencies, team/shop/play navigation, reward slots, missions, and season progression.
- Shop reference: scrollable sections and navigation tabs for offers, packs, deals, and reward offers.
- Review screenshots/video before choosing the new presentation. Do not inherit old coordinates, layout constants, scene wiring, asset-loader workarounds, prices, odds, unlock times, or balancing automatically.
- Old local JSON persistence and development purchase stubs are not production billing. Backend integration was incomplete. Plan trusted purchase/entitlement validation before releasing real-money features.

### Files to request when useful

Names below are references from the handoff/old plan, not inspected implementations. Request small dependency groups and relevant examples rather than importing everything.

| Task | Useful old references |
| --- | --- |
| Player cards, stats, positions, collection/save model | `PlayerData.cs`, `PlayerDatabase.cs`, `Roster.cs`, `RosterManager.cs`, sample player assets/save |
| Mystery packs and reveal | `CardPack.cs`, `PackRevealUI.cs`, `PackCardFX.cs`, `PostMatchRewardManager.cs`, screenshots/video |
| Hub/shop/team/club UX | `NavigationManager.cs`, `ShopUI.cs`, `TeamScreenUI.cs`, `ClubCustomizationUI.cs`, navigation examples |
| Competitions/progression | `LeagueSeason.cs`, `WorldCupSeason.cs` if present, `MatchPresentationContext.cs`, `MissionManager.cs`, `SeasonPassManager.cs`, `LeaderboardManager.cs` |
| Tactical logic for a specific milestone | `WaterPoloAI.cs` / `WaterPoloBrain`, `IAgentBody`, `TeamSide.cs`, `MatchContext.cs`, then relevant movement adapters |
| Specific rules | Relevant scoring, timer, shot-clock, exclusion, penalty, timeout, substitution, stamina or replay scripts plus dependencies |

Inspect dependencies and asset licensing before reuse. Adapt independent data/domain logic where helpful; rebuild 2D physics, sprite presentation, and camera assumptions as needed for 3D. Old architecture instructions do not govern this new project.

For new-project decisions, inspect relevant existing scripts/scenes, `Packages/manifest.json`, and `ProjectSettings` files before relying on their contents.

## Working process

- Read `plan.md` and `AGENTS.md` before significant changes.
- Explain substantial architecture changes and implement approved scope in small milestones. Do not automatically migrate the old game.
- Prefer simple, maintainable C# and account for mobile CPU/GPU/memory limits.
- Give clear Unity instructions with exact menu/panel paths. During interactive setup, provide a few steps and wait for requested screenshots/output.
- Do not call file edits tested without evidence. Report compilation, runtime and device checks separately.
- After meaningful work, document behavior/files changed, confirmed decisions, actual tests, incomplete work, bugs, and the next step. The human alone reviews/stages/commits/pushes; AI must not perform Git write operations or automatic checkpoints.
- Documentation instructions do not guarantee updates after a session ends; include updates explicitly in task completion.

## Roadmap

1. Foundation inspection: completed; saved configuration examined, with live/device verification limits stated above.
2. Milestone 1 movement sandbox: implemented, exercised in automated Editor Play Mode checks, and manually verified by the developer; camera needs closer framing.
3. Milestone 2: following gameplay camera, possession, carrying and shooting one ball. Implemented, automatically verified and manually confirmed by the developer.
4. Milestone 3 goals/scoring/restart: implemented, automatically verified and manually confirmed by the developer.
5. Milestone 4 two swimmers/selection/passing/wider framing: implemented, automatically verified and manually confirmed by the developer.
6. Milestone 5 possession switching, basic friendly/opponent AI and wider camera: implemented and automatically verified at its historical checkpoint; later core gameplay confirmation belongs to the combined Milestone 6 build.
7. Milestone 6 first playable 2v2: shared tactical offense/defense, stealing, four-player scene and scoring/restarts; 12 recorded automated suites passed, and the developer confirmed the specific core gameplay listed above.
8. The human completed the Milestone 6 backup as `48ab2f6` before Milestone 7. AI performs no Git writes; new milestone changes remain uncommitted until the developer tests and backs them up.
9. Milestone 7 adds two AI goalkeepers, physical saves and distribution. The developer manually tested and backed it up as `a1374a8` before Milestone 8; this does not imply exhaustive manual edge-case/device coverage.
10. Milestone 8 adds temporary manual Blue keeper possession, charged shots/power bar and presentation adjustments; developer manual testing and backup completed as `734d8da`.
11. Milestone 9 — complete full-team prototype expansion: six fields plus one keeper per team, six-role tactics, four 80-live-second/8:00 periods, halftime physical ends swap, independent 28-second possession clock, sprint/stamina and aerial passes. Developer reports functional manual gameplay; current human-created HEAD is `07f46d5`.
12. Milestone 10 is implemented in place with contextual selection, shared tactical improvements, prototype referee/free-throw/exclusion/penalty handling and event presentation/audio. See the current handoff at the top for fresh evidence and limits. Developer manual review is next; later milestones require separate authorization.
13. Improve animation, water, lighting, feedback and performance through measured iterations.
14. Request old card/pack/roster examples when useful and choose the new data/UI architecture.
15. Expand competitions, progression, backend and monetization after the playable foundation is validated.

Local checkpoint history: Milestone 1 `a8312f6`, Milestone 2 `2ca3909`, Milestone 3 `0e43094`, Milestone 4 `4babe88`, Milestone 5 `d647882` (its original label records automated testing with manual verification pending at that time). These predate the absolute human-only Git rule and grant no AI Git-write permission. The developer's backups are Milestone 6 `48ab2f6`, Milestone 7 `a1374a8` and Milestone 8 `734d8da`. Milestone 9 awaits developer manual testing/commit/push; no AI checkpoint was created. `old-plan.md` remains untracked, untouched and excluded.

## Documentation and manual backup review — Milestone 6 history

Documentation updated: AGENTS.md, README.md and this canonical plan only. Current source, saved Unity configuration and all 12 stored Milestone 6 result files were inspected; no gameplay code, assets, scenes or settings were changed, and no new Unity tests were run for this documentation-only task. There is no docs/ directory to synchronize; do not create duplicate architecture/roadmap sources unnecessarily.

Read-only Git inspection found branch `main`, local HEAD `d647882`, an empty staging index and five commits ahead / zero behind the stored `origin/main`. A read-only remote query confirmed GitHub `refs/heads/main` at `0eab526e611ef15ac811f0f9d1830651f9780d3b` at review time. The configured fetch/push remote is `https://github.com/Nikoloz-Todua/WaterPoloSuperstars.git`. These are pre-backup observations; recheck before and after the human's operations. A normal push will also publish the existing five local milestone commits; no history rewrite or force push is needed.

Proposed backup comprises the Milestone 6 implementation/scene/material, associated `.meta` files, verification scripts and the three updated root Markdown files. Stage only the explicit reviewed paths supplied to the developer, then inspect staged names/diffs before committing. Preserve `old-plan.md` locally and leave it untracked: it is not covered by the current .gitignore. Existing ignore rules cover Library, Temp, Logs and UserSettings. No .gitignore change is necessary for this explicit-path backup.

The repository-content review found no credential-signature matches or suspicious credential/archive paths in the proposed files, no missing asset/meta pairs and no tracked generated cache folders. A read-only scan of each of the five unpushed commit snapshots also found no credential-signature matches or generated/legacy path matches. The largest proposed file is the existing TMP LiberationSans SDF asset (2,256,862 bytes); no oversized generated content was found. The manual staging list contains 33 reviewed paths: 19 modified files (including these three Markdown files) and 14 new implementation/meta/verification files. old-plan.md is the only additional untracked file and stays excluded. The existing Git name/email are configured; use them without replacing the identity. This is a targeted review, not a guarantee that arbitrary future changes contain no secrets. Canonical handoff details such as the requested local project path and hardware are intentionally retained in plan.md; README omits personal setup details. Do not publish local tokens, private keys, service-account credentials or raw legacy archives. No staging, commit, push, fetch or other Git write operation was performed during this review.
