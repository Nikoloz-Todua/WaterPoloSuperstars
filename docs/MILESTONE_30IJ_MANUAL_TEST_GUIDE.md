# Milestones 30I and 30J manual Test Lab guide

Start each fixture from saved Edit Mode. They stage the production arena without the real profile. Stop Play before opening the next fixture. The shots and restarts below are staged, so a few seconds per fixture is sufficient. Use Pause and Step if a contact is difficult to see.

## Boundaries and ball physics

Open **Tools > Water Polo > Test Lab > Boundaries and Ball Physics**.

| Menu entry | Short inspection |
| --- | --- |
| **Sideline Movement Freedom** | Use **WASD** or **arrow keys** toward the near rope. Continue diagonally along its length and then turn back inside. The rope can be crossed into the water buffer. The outside basin edge should reject the outward component while allowing parallel travel. |
| **Goal-Line and Goal-Mouth Movement** | Use the movement keys to enter between the posts. Reverse to leave. Swim diagonally along the goal line and approach a post. The shallow recess ends before the back net. |
| **Goalkeeper Loose Ball Behind Keeper** | Let the fixture place the slow ball. Watch the keeper turn and swim back to take possession. Check that the scoreboard stays 0–0. |
| **Ball Partially Across Goal Line** | The staged ball partly crosses the plane and the score stays 0–0. Press **T** in the focused Game view to apply the remaining motion and score once. |
| **Wide Shot - Visible Trajectory** | Watch the automatic shot pass the side of the goal. Follow the actual ball after the referee's award and observe the goal-throw replacement. |
| **High Miss - Visible Trajectory** | Watch the automatic shot clear the crossbar and continue before goal-throw replacement. |
| **Goalkeeper Deflection Out of Play** | The setup stages a legal keeper contact with the existing ball API. Follow the deflected flight and check that the award is a corner throw. |
| **Post Rebound - Live Ball** | The automatic shot hits the physical post. Check that it rebounds toward the field and no boundary restart is called. |
| **Sideline Exit - Throw In** | Watch the automatic side exit, continued flight, and possession awarded to the team that did not touch the shot last. |
| **Defender Deflection Out of Play** | The fixture stages a field block touch and outgoing trajectory. Check the award after the ball crosses the end rope. |

## Match continuity

Open **Tools > Water Polo > Test Lab > Match Continuity**.

| Menu entry | Short inspection |
| --- | --- |
| **Ordinary Foul - Legal Repositioning** | The fixture awards the foul immediately. Watch the near defender move away and the outlet adjust while the taker holds the ball at the mark. Aim with the movement keys and press **B** for the pass. The quarter and possession clocks only resume on legal release. |
| **Goal Reactions and Centre Restart** | The fixture scores once. Watch the scorer, nearby teammate and field athletes heading toward their own side. Press **Enter** to advance or **Backspace** to skip the goal presentation. The conceding team takes the centre restart. |
| **Goal Celebration Preview** | The same fixture stages a late Q4 go-ahead goal. Watch the higher two-arm surge and settling motion. Use the same advance or skip controls. |
| **Goalkeeper Conceding Reaction** | Watch the fast miss reach the net. Verify the keeper attempts a save and settles. The production celebration camera follows the scorer, so later keeper settling may be near the edge of the frame. |

## Localized water

- **Tools > Water Polo > Test Lab > Swimming and Floating > Eggbeater Comparison** stages treading at a low side angle. Leave movement released briefly and look for the small hand disturbances.
- **Tools > Water Polo > Test Lab > Swimming and Floating > Freestyle Side View** uses the existing camera to show stroke contacts and wake. Swim with **WASD** or **arrows**, then hold **Left Shift** during movement to compare sprint.
- **Tools > Water Polo > Test Lab > Arena and Water > Ordinary Ball Drop** stages a vertical drop. Look for the initial splash and damped settle.
- **Tools > Water Polo > Test Lab > Shooting and Passing > Skip Shot** stages the intentional shallow skip. Compare its surface contact to the ordinary drop.

Stop Play and choose **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**. The optional automated probe is `Tools/Verification/Milestone30IJVerification.cs`, entry `Milestone30IJVerification.Run`. Use `unity command script run` with the fixture's short label as a JSON argument immediately after launch. Result captures go to `Logs/Milestone30IJ/`.
