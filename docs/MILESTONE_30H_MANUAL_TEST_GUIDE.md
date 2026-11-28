# Milestone 30H manual Test Lab guide

Start each fixture from saved Edit Mode. Stop Play before choosing the next one. The fixtures use the existing preview or Olympic arena without connecting the production profile. Use the Game view and Unity's Pause and Step toolbar buttons for a closer look at a delivery. A few seconds per fixture is sufficient.

## Competition briefs

The current garment is visually rejected. Use these views to reproduce the existing fit defects and compare a later candidate.

| Unity menu | Controls | What to inspect |
| --- | --- | --- |
| **Tools > Water Polo > Test Lab > Athlete and Equipment > Competition Swimwear Inspection** | Press **F3** for the hips, use **Left/Right Arrow** to orbit, and **Tab** to switch between dry and animated preview. In animated preview, **1** selects eggbeater, **2** selects normal crawl, **3** selects sprint and **U** tilts the camera underneath. | Compare front taper, side depth and rear coverage. In eggbeater, step through wide knee separation and watch the leg openings for stretching, pinching or clipping. |
| **Tools > Water Polo > Test Lab > Athlete and Equipment > Underwater Swimwear Inspection** | Hold **WASD** or the **arrows**. Add **Left Shift** briefly, then stop and tread. **F8** returns to broadcast from the side view. | Compare the submerged material in motion and at rest. Check that the waistband and leg edges stay visible through water. |
| **Tools > Water Polo > Test Lab > Swimming and Floating > Eggbeater Comparison** | Leave movement released for a few cycles. Pause or step the Game view at wide knee separation. | Use the production underwater side view to inspect the leg seam near the upper thigh and compare its opening as the legs cycle. |
| **Tools > Water Polo > Test Lab > Shooting and Passing > Full Power Shooting Elevation** | Hold **Space** until the charge fills, then release. | Inspect the brief's hem and waistband as the body rises and rotates. |

## Goalkeeper

The developer approved the current keeper improvements for now. Use these short fixtures if a later change needs a direct comparison.

All entries start at **Tools > Water Polo > Test Lab > Goalkeeper and Penalties**. **F8** returns to broadcast. From broadcast, press **F8** three times to return to the staged close-up. Except for Neutral, the setup launches one pass or shot automatically shortly after staging. Restart the entry to replay its initial trajectory.

| Entry | Reproduction and visible acceptance |
| --- | --- |
| **Goalkeeper Neutral and Ball Tracking** | Observe the keeper while holding the ball. Use **WASD** or the **arrows** to swim a few metres sideways, stop, then tap **Space** for an ordinary shot if desired. The keeper should track the carrier and slide along the goal with low hands. A close carrier can elicit a partial ready stance. Flag raised arms while no ball is coming. |
| **Ordinary Goalkeeper Catch** | Keep the shooter still and let the pass run. Look for a presented hand, small lift, and withdrawal into right-hand possession. Flag a visible hand gap or a full-height dive. |
| **Keeper Save Left** and **Keeper Save Right** | Compare lean and reach from the same camera. Labels refer to the goal-local shot side. Check the visible hand at contact and follow a deflection continuously into recovery. |
| **Keeper Save High** | Inspect the central two-handed reach above the normal waterline. Watch the leg drive, lift and timing. The recovery run produced a parry. |
| **Goalkeeper Miss and Recovery** | A fast near-post trajectory should elicit a directional attempt and can score beyond reach. Watch the arms and root settle. Flag repeated leaps, an arm stuck up or a save with a large visible gap. |

[Focused Game View evidence](Evidence/Milestone30H/)

## Recovery

Exit Play. Choose **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**. For a short scripted keeper measurement, use the existing `Tools/Verification/Milestone30HVerification.cs` via `unity command script run` with entry `Milestone30HVerification.Keeper` and JSON arguments such as `["High",false]`. Run it just after opening the fixture and use `false` to keep the prior captures. [Final results](MILESTONE_30H_FINAL_REPORT.md)
