# Milestone 30G manual Test Lab guide

Open Unity 6000.6.7f1 in Edit Mode. Start with the named Test Lab command and stop Play between fixtures. The arena tests run in the saved production arena and the solo clips use the existing AthletePreview scene.

## Freestyle and eggbeater

All entries are under **Tools > Water Polo > Test Lab > Swimming and Floating**.

- **Underwater Freestyle**, **Freestyle Side View** and **Freestyle Rear View**. Use **WASD** or arrows for a short forward swim, then let go. The hips should follow the prone torso. The knees should bend toward the back of the leg while the feet trail. Alternate swimming and treading to inspect the transition. Watch for thighs or soles rolling independently, a frog kick or a limb snapping as the athlete stands up in the water.
- **Underwater Sprint Freestyle**. Hold movement and **Left Shift**. Watch a faster alternating kick with the same anatomy. Release Shift and then movement to compare crawl and tread.
- **Underwater Ball Leading**. Sustain forward movement until the ball leads the stroke. Add **Left Shift** briefly and then stop. The ball should gather normally and the legs should return to the upright pose.
- **Eggbeater Comparison**. Start without movement, swim briefly, then release the direction. Compare the settled upright pose against the prone legs.

Use **Tools > Water Polo > Test Lab > Athlete and Equipment > Play Athlete Preview** for slow-motion anatomy. Press **Tab** for water poses, **1/2/3** for tread/crawl/sprint, **Left/Right** to orbit, **O** for overhead, **U** for underwater and **P** for quarter speed. The production comparisons are captured at the same eight stroke phases and loop end. [Matched phases](Evidence/Milestone30G/Freestyle-Before-After-Matched-Phases.jpg) · [Side and rear views](Evidence/Milestone30G/Freestyle-Production-Views.jpg)

## Deck and officials

All entries are under **Tools > Water Polo > Test Lab > Ceremony and Walking**.

| Scenario | Reproduce | Inspect |
| --- | --- | --- |
| **Natural Athlete Walk** | Use **Left/Right** for side, front and rear. **P** sets quarter speed and **]** steps one source frame. **F4** shows the earlier imported walk and **F7** returns to the authored walk. | Heel touches first, the foot rolls flat, the heel rises with the forefoot still down, and the swing clears the floor. Watch for a locked toe-up ankle or both soles sliding together. |
| **Standing and Foot Placement** | Use **F9** for the authored stand if switching from the walk. Orbit and inspect the ankles. | Feet stay neutral through the weight shift, with a relaxed stance. Watch for lifted toes, floating heels or a forced knee bend. |
| **Arena Walkout and Pennants** | **Enter** advances a stage. Let the walkout, lineup and exchange play for a few seconds each. Stop Play after the exchange. | Players ease into the route, alternate their steps, settle into the lineup and plant their feet for the exchange. Watch the returning captains turn back toward their marks. |
| **Referee Walking** | Move the carrier with **WASD** or arrows along the pool. Reverse after the official has started moving. Press **F8** to return to broadcast. | The existing official follows the play along the deck, checks the pool, brakes and pivots. Watch for prolonged toe-up stance or a sideways glide. |

[Walkout evidence](Evidence/Milestone30G/Walking-Gait-Sequence.jpg) · [Lineup and exchange](Evidence/Milestone30G/Natural-Standing-and-Captain.jpg) · [Official evidence](Evidence/Milestone30G/Referee-Walking.jpg)

Stop Play. If an interrupted direct launch leaves a Play override, choose **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**. The production scene opens from **Tools > Water Polo > Arena > Open Olympic Match Arena**. Source recovery starts from the approved `c1b37b6` and the file inventory. The editable Blender file also retains the legacy crawl and sprint Actions. [Exact file inventory](MILESTONE_30G_FILES.txt)
