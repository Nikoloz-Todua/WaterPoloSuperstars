# Water Polo Test Lab

Start these commands in Unity 6000.6.7f1 with the arena saved and Play stopped. The Test Lab opens the production `OlympicMatchArena` in direct Editor Play, applies scenario positions in memory, and restores Boot on exit. The athlete preview reuses its existing standalone scene. The Test Lab entries are Editor scripts, outside player builds. Direct arena inspection starts without the hub or an official reward session.

## Menu map

### Milestone 30F additions

Open **Tools > Water Polo > Test Lab > Team Tactics > Intelligent Team Support** to compare outlets and marking before and after moving the carrier. Under **Shooting and Passing**, use **Long Pass to Receiver Facing Away** or **Stationary Dry Pass** with **B**, **Moving Receiver** or **Lateral Receiving** with **T**, **High Receiving** with **L** and **Missed Pass and Recovery** with **B** followed by movement toward the loose ball. Press **F8** three times from broadcast in the receiving fixtures for the receiver close-up and once more to return. [30F manual tests](MILESTONE_30F_MANUAL_TEST_GUIDE.md)

### Milestone 30D additions

All paths start at **Tools > Water Polo > Test Lab**. Use **Swimming and Floating > Underwater Sprint Freestyle** with forward input and **Shift**, **Ball Control and Player Switching > Directional Aim and Held Rotation** with **Left Alt + direction**, and **Shooting and Passing > Long Pass to Receiver Facing Away** with **B/PASS**. Under **Shooting and Passing**, **Full Power Shooting Elevation** takes a held **Space/SHOOT** and **Quick Shooting Elevation** takes a tap. **Referees and Match Rules > Ordinary Foul Repositioning** stages a protected ordinary restart. Watches, moves and commands are exercised in the same saved production arena. [30D manual reproduction and acceptance signs](MILESTONE_30D_MANUAL_TEST_GUIDE.md)

### Existing previews

| Path under Tools > Water Polo | What starts |
| --- | --- |
| **Play > Play Full Game** | Boot, then the normal MainLobby. |
| **Play > Play Match** | An isolated, full arena match with the standard ceremony. Stop Play to return the entry to Boot. |
| **Test Lab > Arena and Water > Ordinary Ball Drop** | A physical drop into the existing pool water after the staged entry. **F8** cycles arena cameras. |
| **Test Lab > Athlete and Equipment > Play Athlete Preview** | Existing athlete sandbox. **F3** frames the equipment, **Left/Right** orbits, **1/2/3/6** selects tread/crawl/sprint/shot and **F4** compares the imported deck clip. Inspect the corrected live walking in the Ceremony entry. |
| **Test Lab > Swimming and Floating > Freestyle and Floating** | Side view of the selected, unladen swimmer. **WASD/arrows** steer, **Shift** sprints, release movement to tread. |
| **Test Lab > Swimming and Floating > Underwater Freestyle** | The same production pose and controls from below the waterline. |
| **Test Lab > Swimming and Floating > Underwater Ball Leading** | The selected athlete starts with possession. Sustain forward movement until the ball leads the crawl. |
| **Test Lab > Ball Control and Player Switching > Possession and Throwing** | Close carry, rotation, charge, drop and human control. **B** passes, **Space** charges and releases on key-up, **L** lobs, **C** switches, and **Esc** pauses. |
| **Test Lab > Shooting and Passing > Passing and Switching** | A short Blue pass target and normal replayable manual switching. Press **B**, then **C** while the ball is airborne to test manual protection. |
| **Test Lab > Shooting and Passing > Skip Shot** | A timed physical skip launch. Live equivalent **Q + Space**. |
| **Test Lab > Player Contact and Defense > Head-On Centre Players** | Selected Blue centre forward and an opposing centre back. Hold **W/Up Arrow** after setup. |
| **Test Lab > Player Contact and Defense > Shoulder Slide** | Offset stationary defender. Hold **W/Up Arrow** through the offset or add slight lateral input to peel off. |
| **Test Lab > Player Contact and Defense > Sprint into Stationary Defender** | Stationary obstacle directly ahead. Hold **W/Up Arrow + Shift** and then move sideways. |
| **Test Lab > Player Contact and Defense > Contested Loose Ball** | Loose ball between two approaching centre players. Hold **W/Up Arrow** and watch exclusive pickup. |
| **Test Lab > Player Contact and Defense > Centre Defense and Shot Block** | Earlier centre marking and one-arm block preview, now accessible by feature. Use **Space** to shoot. |
| **Test Lab > Goalkeeper and Penalties > Keeper Save Left / Right / High** | Earlier directional shot and save previews using the production ball and goalkeeper. Watch the automatic shot, then stop and select another side. |
| **Test Lab > Ceremony and Walking > Arena Walkout and Pennants** | Production ceremony in the isolated arena. **Enter** or tap advances a stage, **Backspace** or **Skip Intro** advances to the sprint. Stop Play after the ceremony. |
| **Verification > Exclusion Test** | Existing isolated rule scenarios and compatibility paths. **White - Third Personal and Replacement** is used by the existing verifier. |

On a touch layout, use the existing joystick and **SPRINT**, **PASS**, **SHOOT**, **LOB**, and **SWITCH**. **F6** switches live broadcast camera and **F8** visits existing inspection points in Editor. Staging cameras configure the intended screen movement for their particular view.

The focused entries formerly grouped by milestone now appear under Test Lab by feature. The original **Play Hub Entry** and **Play Olympic Match Arena** paths remain for existing verification and documentation. Existing tools in **Arena**, **Athletes**, **Verification**, **Recovery** and **Legacy Prototype Tools** remain available. Wider menu reorganization, standalone Test Lab scenes, performance tooling and additional match-rules scenarios are deferred.

## Exit and recover

Stop Play before launching a second scenario. **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** restores normal startup if a direct legacy command or interrupted session left an arena override. A dirty scene blocks the lab launch so a production scene edit cannot be discarded. Save intended edits before using a preview.
