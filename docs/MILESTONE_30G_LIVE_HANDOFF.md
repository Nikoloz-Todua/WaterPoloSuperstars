# Milestone 30G live handoff

Starting checkpoint `c1b37b6`, approved Milestone 30F. Unity 6000.6.7f1 and Blender 5.2.3 LTS.

## Stage A — Authored and verified in the focused Editor fixtures

The source legs used a fixed reference. The export reproduced the source sample, Humanoid altered the lower-leg axes, and the older procedural override generated a separate kick against the Humanoid pelvis. The new Blender normal and sprint Actions author the complete pelvis-to-toe chain. The runtime uses a shared 60 Hz bake and cached joints. The existing 66-bone models, Avatar and gameplay retain their prior bindings. Live normal, sprint, normal/sprint ball-leading and return to upright/held possession passed. The receiving compatibility test passed after narrowing its receiver selector to the staged outlet.

The prior screenshot probe permitted resampling between applying a pose and rendering. The final Game View phase capture holds the evaluated pose. The developer's visual approval of the original inversion issue remains open.

## Stage B — Authored and verified in focused deck fixtures

The two editable deck Actions and animation-only FBX takes now bind to the production athlete and official. The Blender contact table and actual ankle/toe bind orientations drive the foot solve. A Blend Playable keeps the gait alive through ceremony starts, stops, stance and the captain exchange. The final footage includes the live referee walking and reversing. The first referee preview searched the wrong transform for its visual and was corrected. Direct standing, place transitions, both gait styles and personal deck appearance are available in the Test Lab.

## Stage C — Inspection checkpoint

The project is in Edit Mode with Boot restored. The arena and profile hashes matched. The tests used isolated editor fixtures. The production arena file retains its GUID. Re-run the targeted probes via `Tools/Verification/Milestone30GVerification.cs`. Use the manual guide for the corresponding Test Lab scenario. The source generation scripts are `Tools/Art/AuthorProfessionalFlutter.py` and `Tools/Art/AuthorProfessionalDeck.py`.

Next action is the developer's review of the close live flutter, walkout and captain exchange. Keep any visually rejected defect open. Android and exact floor-slip measurement remain open. Start later milestones only with explicit authority.

[Final report](MILESTONE_30G_FINAL_REPORT.md) · [Manual guide](MILESTONE_30G_MANUAL_TEST_GUIDE.md) · [Exact inventory](MILESTONE_30G_FILES.txt)
