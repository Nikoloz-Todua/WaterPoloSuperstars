# Milestone 30G ankle and deck polish

This pass starts from the reviewed 30G working tree over `c1b37b6`. Blender 5.2.3 LTS and Unity 6000.6.7f1 were used. No full match ran.

## Freestyle feet

The Blender audit reproduced the foot rotation in the authored crawl and sprint. The former ankle aim used the **back** of the shin as the destination for the top of the foot. The Unity raw Humanoid pose had different roll, but the existing production overlay applied the baked ankle pose and reproduced the Blender inversion. This located the defect in the authored roll. The fix is in `AuthorProfessionalFlutter.py`, the editable Actions in `WPS_AquaticMotion_001.blend`, and `WPSFlutterMotion.json`. The Avatar, existing Humanoid clips and `ProneFlutterPose` code are reused.

The corrected foot top follows the front of the shin. The toes trail the kick with less pitch variation and a small delayed toe bend. All **755** pelvis, thigh and shin quaternion samples are component-identical across both bakes. The anatomical alignment check moved from −1.000 to +1.000 in Blender and the final Unity pose at the sampled crawl and sprint phases. The check compares the top of the foot to the front of the shin around the toe direction. It validates the roll on those frames alongside the rendered mesh. [Before and after side/rear views](Evidence/Milestone30GPolish/Freestyle-Ankle-Roll.jpg)

## Deck gait

The shared walk now flattens the foot sooner after heel strike, extends the toes as the heel lifts and keeps the swing closer to the deck. Blender still supplies the in-place body rhythm. The updated walk and stand Actions and existing FBX bindings carry the change. The shared foot solver holds stance position and heading into braking and rolls from a heel pivot to a forefoot pivot. Body height releases more slowly after a loaded step. Referees blend stride length and gait into the pivot when reversing. The rehearsal routes and control over actor movement stay with the existing ceremony and referee systems. [Rendered deck checks](Evidence/Milestone30GPolish/Deck-Gait-and-Officials.jpg)

## Focused verification

| Fixture | Result |
| --- | --- |
| Freestyle Side View and Freestyle Rear View | The Game View rendered the matched crawl and sprint poses on the production athlete. The live probe reached both movement speeds and returned to eggbeater. |
| Underwater Ball Leading | Maintained ownership during normal and sprint leading. The production Animator reached full crawl and a sustained sprint blend before the check ended. The ball then gathered into stationary possession. |
| Natural Athlete Walk | The standalone skinned preview advanced the new walk and stand. |
| Arena Walkout and Pennants | Captured the live entrance, formal lineup, captain approach, exchange and return step. The cast remained at 14 athletes. |
| Referee Walking | The official travelled 1.51 m along the deck, braked, pivoted and resumed travel in the other direction under the existing ball tracking. |

Personal visual review and exact sole clearance and slip measurements remain open. Android was outside the Editor fixtures. The work deliberately covers only the freestyle feet and deck gait.

## Inspect manually

1. **Tools > Water Polo > Test Lab > Swimming and Floating > Freestyle Side View** and **Freestyle Rear View**. Swim with **WASD** or arrows, then hold **Left Shift**. Compare the soles and tops of both feet across beats. The toes should point away from the hips and the feet should follow the shin plane. Release movement to check the upright transition.
2. **Tools > Water Polo > Test Lab > Swimming and Floating > Underwater Sprint Freestyle** and **Underwater Ball Leading**. Hold forward long enough for the rapid stroke to settle. Use **Left Shift** with the ball and release movement to inspect the gather.
3. **Tools > Water Polo > Test Lab > Ceremony and Walking > Natural Athlete Walk**. Use **P** for quarter speed, **]** to step a frame and **Left/Right** to orbit. Look for heel contact, a flat support foot, and toe flex when the heel lifts. Open **Standing and Foot Placement** in the same submenu to review the settled stance.
4. **Tools > Water Polo > Test Lab > Ceremony and Walking > Arena Walkout and Pennants**. Use **Enter** to reach the entrance and exchange. Let each play briefly and watch how the last steps settle. Also open **Referee Walking**, move along the pool with **WASD/arrows** and reverse direction. Check the official's shoes through the brake and pivot.

## Saved state and recovery

Unity is in saved Edit Mode. Boot is the normal Play entry and the active scene is OlympicMatchArena. The arena GUID and SHA-256 and the real profile SHA-256 matched. Compilation succeeded and the current Console showed zero errors or warnings. No Git writes were performed.

To recover the normal Play entry, stop Play and use **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry**. To reopen the production scene use **Tools > Water Polo > Arena > Open Olympic Match Arena**.

The pre-polish implementation was copied locally to `Logs/Milestone30GPolish/Before` because 30G was still local work. Keep Play stopped. From the project root, use the following only to roll back this polish on the listed files.

```powershell
$backup = 'Logs/Milestone30GPolish/Before'
Get-Content -LiteralPath 'Logs/Milestone30GPolish/restore-files.txt' |
    ForEach-Object {
        Copy-Item -LiteralPath (Join-Path $backup $_) -Destination $_ -Force
    }
unity command script recompile
```

The white-box swim fixture is `Tools/Verification/Milestone30GPolishVerification.cs`. The step and official probes reuse the existing `Milestone30GVerification.cs` through a local copy that redirects evidence output.
