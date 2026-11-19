# Pre-match presentation

The first pre-match ceremony is saved in **OlympicMatchArena**. It uses the existing entrance, deck marks, athlete rigs, lighting and single gameplay camera. The sequence runs once on a fresh arena load and lasts about 23 seconds at the default pace.

## Play it in Unity

1. Choose **Tools > Water Polo > Play Olympic Match Arena** for a direct preview. Choose **Tools > Water Polo > Play Hub Entry** to start from Boot and enter a match through MainLobby.
2. Let the sequence play through, or use the controls below. After the intro, play the existing opening sprint to win the ball.
3. To check restart and exit, open **Pause** during the walkout. Cancel a pending restart to stay in the paused ceremony. Confirm it to go straight to the new Q1 sprint. In a match launched from MainLobby, **Exit > Confirm** returns to the lobby.

| Action | Touch or mouse | Keyboard | Gamepad |
|---|---|---|---|
| Advance one stage | Tap or click the presentation | Enter | South |
| Skip the remaining intro | **Skip Intro** | Backspace | East |
| Pause or resume | **Pause** / **Resume** | Escape | Start |

The scene is already configured. If the production scene is restored to a revision without the intro, open that saved production scene in Edit Mode and run **Tools > Water Polo > Match UX > Set Up Pre-Match Presentation**. Repeating setup on the configured scene keeps its saved data. Intro timing is exposed on the scene's **Pre Match Presentation** component. To restore regular Editor startup after direct preview, use **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**.

## Presentation flow

| Stage | Default time | What appears |
|---|---:|---|
| Arena intro | 2.3 s | Wide Championship Night overview with a short push toward the pool. |
| Team walkout | 5.4 s | Blue and White arrive from separate lanes behind the entrance and fan out to their marks. |
| Formal lineup | 2.1 s | Seven athletes per side with two match officials at centre. Goalkeepers retain their red caps. |
| Captain exchange | 2.6 s | Captains step forward and exchange two team pennants in a medium shot. |
| Presentation hold | 2.2 s | Both teams return to their marks for a short ceremony hold. |
| Starting lineups | 3.8 s | Two roster cards show team names, cap numbers, captains and goalkeepers. Existing collection names appear when assigned. Other entries show their field role. |
| Final preparation | 3.3 s | Both teams gather in separate deck huddles and start toward the pool. |
| Match-start transition | 1.2 s | The camera returns to the original quarter-opening view. The existing Q1 sprint begins. |

## How it is put together

- **PrototypeMatch** owns the new stopped phase and authorizes the handoff to Q1. Live swimmer simulation, player input, possession, the period clock and the shot clock wait during the intro. Finishing or skipping calls the same guarded sprint transition. Restart runs the existing fresh-match path and later periods retain their existing openings.
- **PreMatchPresentation** advances the stage sequence on the match's pause-aware presentation clock. It owns the overlay and temporary camera override. The existing menu still handles Pause, Resume, Settings, Restart and Exit. Cleanup returns the camera, live renderers, HUD raycasts and deck display to their match state.
- **PrematchCast** reads the existing venue anchors and the live roster. It uses the assigned collection name when available and selects the centre forward as captain, with the first field athlete as a fallback. The cast uses visual copies of the fourteen current rigs and the two existing official models. The props and cast have no gameplay colliders, bodies, selection or AI. They share meshes and materials and are released at handoff or teardown.
- **PresentationVisualCopy** is shared with the existing goal replay buffer. Replay still captures and plays the same live transform history. The intro borrows the current render camera and pauses live rig posing while its own small cast moves. The live camera preference survives the transition.

## Windows Editor verification

Unity **6000.6.7f1**, with the saved production scene and advancing Game View. The verification fixture is [Tools/Verification/PreMatchPresentationVerification.cs](../Tools/Verification/PreMatchPresentationVerification.cs).

| Check | Result |
|---|---|
| Full sequence and screenshots | All eight stages ran in order. Across them the match clock, live athlete positions and loose opening ball held. Fourteen render-only athletes had zero colliders. |
| Pause and resume | Pointer opened the normal Pause menu during walkout. Timeline and camera held, and Resume continued the same stage. |
| Advance and skip | Simulated touch, Enter and gamepad South each advanced one stage. Holding confirm did not advance twice. The pointer button, Backspace and gamepad East each handed off to Q1. |
| Camera and first possession | Lengthwise and Sideline Broadcast were restored in separate runs. The F6 and F8 bindings did not take over during the intro. One sprint and one physical pickup followed each verified handoff. Fourteen unique live IDs and ordinary selection were present afterward. |
| Restart, exit and Play stop | Restart cancellation kept the paused ceremony. Confirmed restart disposed its cast and entered Q1. Exit during the walkout returned to MainLobby with no reward. Stopping Play in Arena Intro returned cleanly to Edit Mode. |
| Boot and official match | Actual lobby buttons launched the production arena with catalog names. Four physical opening sprints and the halftime end switch passed with accelerated period endings. One official result and reward returned to MainLobby. Reward retry did not duplicate it, and the real profile hash matched. |
| Recorded goal regression | A physical Blue shot registered once with the shooter. Celebration and both recorded replay angles ran with stopped clock and fixed score. Skip All returned to the normal centre pass. |
| Setup, build routes and Console | Repeat setup left the saved scene byte-identical. Enabled build routes remain Boot, MainLobby and OlympicMatchArena. Boot Editor entry and input settings were restored. Final current Console reported **0 errors and 0 warnings**, with no new warning or error entries in the session since the baseline cursor. |

Fixture retries accounted for the first canvas layout after scene loads and the collection ID used by official-match scorer attribution. The final focused runs passed. Tests used the Windows Editor and simulated mobile input. Full-duration matches, physical devices, performance and a full penalty or exclusion regression suite remain outside this pass.

### Evidence

The following links point to local Game View captures and machine-readable results under **Logs/PreMatchPresentation/**.

- [Eight-stage contact sheet](../Logs/PreMatchPresentation/ContactSheet.png) and [full run](../Logs/PreMatchPresentation/Full-PASS.json)
- [Walkout](../Logs/PreMatchPresentation/02-TeamWalkout.png), [exchange](../Logs/PreMatchPresentation/04-CaptainExchange.png), [starting lineups](../Logs/PreMatchPresentation/06-TeamRoster.png) and [team huddles](../Logs/PreMatchPresentation/07-TeamHuddle.png)
- [Pause](../Logs/PreMatchPresentation/09-IntroPaused.png), [opening sprint](../Logs/PreMatchPresentation/10-OpeningSprint.png) and [live match](../Logs/PreMatchPresentation/11-LiveMatch.png)
- [Input run](../Logs/PreMatchPresentation/Controls-PASS.json), [gamepad full skip](../Logs/PreMatchPresentation/GamepadSkip-PASS.json), [recorded goal](../Logs/PreMatchPresentation/15-RecordedGoalRegression.png) and [official lobby loop](../Logs/PreMatchPresentation/Hub-PASS.json)

## Placeholders and future work

The current rig uses a procedural deck gait, planted standing poses and a simple arm reach. The existing officials are static placeholders. The two small pennants interpolate between hands, and huddles stay on deck. The hold is a short visual ceremony. A future version can plug authored locomotion, robes, walking officials, hand choreography, anthem audio and water entry animations into these stages. Club labels and authentic captains can use future team metadata. The current cast and overlay use the existing Blue and White team data. The optional dead-ball water tuning was deferred.

## Exact project file inventory

| Status | Path |
|---|---|
| New | `Assets/Editor/PreMatchPresentationSetup.cs` and `.cs.meta` |
| New | `Assets/Scripts/Gameplay/PreMatchPresentation.cs` and `.cs.meta` |
| New | `Assets/Scripts/Gameplay/PrematchCast.cs` and `.cs.meta` |
| New | `Assets/Scripts/Gameplay/PresentationVisualCopy.cs` and `.cs.meta` |
| New recovery | `Assets/Scenes/Recovery/OlympicMatchArena_PreMatchPresentation.unity` and `.unity.meta` |
| New verification | `Tools/Verification/PreMatchPresentationVerification.cs` |
| New report | `docs/PRE_MATCH_PRESENTATION.md` |
| Modified | `Assets/Editor/MatchUxSetup.cs` |
| Modified | `Assets/Scenes/OlympicMatchArena.unity` |
| Modified | `Assets/Scripts/Gameplay/GameplayCamera.cs` |
| Modified | `Assets/Scripts/Gameplay/GoalReplayBuffer.cs` |
| Modified | `Assets/Scripts/Gameplay/MatchNotificationBanner.cs` |
| Modified | `Assets/Scripts/Gameplay/MobileMatchControls.cs` |
| Modified | `Assets/Scripts/Gameplay/PrototypeMatch.cs` |
| Modified | `Assets/Scripts/WaterVisuals/RiggedSwimmerVisual.cs` |
| Modified | `plan.md` and `README.md` |

The recovery scene was copied before editing and verified byte-identical to the original at that point. It sits outside the build. [Read-only Git status](../Logs/PreMatchPresentation/Final-Git.txt) and [final Console output](../Logs/PreMatchPresentation/Final-Console.txt) are in the local evidence folder. All milestone files are unstaged. Read-only Git status also shows the pre-existing edit to `ProjectSettings/EditorBuildSettings.asset` and untracked `ProjectSettings/Packages/com.unity.pipeline/`, `SourceAssets/`, `old-plan.md` and `tatus`.
