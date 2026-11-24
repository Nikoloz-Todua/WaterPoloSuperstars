# Milestone 29 — Competition ball, pickup and dribbling

Milestone 29A installed the branded ball and staged pickup and dribbling behind disabled switches. Milestone 29B passed the Play Mode gate and enabled both switches in the saved OlympicMatchArena. Normal matches launched through Boot use the saved settings.

## Competition ball

The Meshy FBX and four 2K PNGs were copied from `C:/Users/todua/OneDrive/Desktop/WPS_Ball_001` to `SourceAssets/Equipment/WPS_Ball_001/Original/`. Their common prefix is `Meshy_AI_WP_Superstars_Basketball_20261124_texture`. The original color PNG and imported albedo have identical SHA-256 hashes.

`Tools/Art/PrepareCompetitionBall.py` centers the shell and projects it onto a unit-diameter sphere while retaining the source UVs, black seams and WP SUPERSTARS artwork. Blender measured 3,014 triangles, 1,509 welded vertices and zero degenerate faces. The material uses URP Lit, sRGB albedo, a normal map at 0.55 strength and zero metallic. Linear smoothness is packed as `clamp(1 − roughness, 0.15, 0.39)`. Three maps use mipmaps and 1024-pixel Android ASTC overrides. Close-up Game View confirms the seams and branding. Their visibility at replay distance varies with screen size and rotation. The live and replay copies use the same mesh and material, so M29B retained this PBR configuration.

The same authoritative Ball object has a 0.45 kg Rigidbody and a 0.14 m SphereCollider. Its visual diameter is 0.28 m and aligned with that collider. The 22 cm supplier size is a reference for any future physics-scale review. The serialized collider, Rigidbody and ball root transform compare byte-for-byte with the pre-29B records. Surface travel rolls the visual child at 55% of geometric roll. Airborne flight applies a smaller cosmetic spin while Rigidbody contacts still determine physical motion. The replay records both the root and visual rotation.

## Automatic recovery

The initial 1.05 m radius let the athlete capsule push the ball away at approximately 1.188 m separation. Milestone 29B uses the closest planar separation over the next physics step based on the motor and ball’s relative velocity. The projected displacement is capped at 0.14 m and the athlete’s existing interaction reach still limits acquisition. This resolves approach before contact can accelerate the ball away. Physics collision settings stay intact.

| Pickup setting | Saved default |
| --- | --- |
| Nominal planar reach | 1.20 m |
| Maximum one-step projection | 0.14 m, subject to the existing manual reach |
| Maximum ball speed | 2.35 m/s |
| Ball center height tolerance around surface rest height | ±0.16 m |
| Forward reach cone | Dot product ≥ 0.20 outside 0.55 m |
| Existing release protection | 0.60 s |

`BallController` ranks eligible human and AI candidates by current separation, with scene roster order breaking exact ties. It calls the existing `TryCollect` API once. Automatic recovery is confined to ordinary loose balls in live play and respects eligibility, protected opening possession, restarts, airborne releases and existing catch and keeper handling. A shot settling on the surface still has to be outside the goal pocket and moving at 0.50 m/s or less. Manual pickup and existing legal challenges are available.

## Holding and ball-leading crawl

- At rest, the existing right-hand IK and Milestone 28 eggbeater present a raised ball. Brief repositioning can retain the grip.
- Regular dribbling starts after at least 2.10 m/s of forward movement for 0.15 s. Active sprint can enter at 1.10 m/s, and slowing to 1.10 m/s or below gathers the ball. The ball keeps the same logical owner while the free-arm crawl or sprint plays.
- The leading point uses the existing skinned head, movement direction and waterline. The default head clearance is 0.36 m, compensated for actual radius and increased by 0.022 m per m/s above 4 m/s. The root-to-target distance is bounded to 0.68–1.09 m and clamped inside the pool. The water-height variation is ±0.01 m with a ±0.035 m lateral sweep.
- The existing smoothed controlled movement and grip path handle slowing and gathering. A charged shot gathers into the grip as charge starts. A tap shot or immediate pass can release from the existing socket through the normal API. No extra gameplay release timer is used. A legal steal clears the previous dribble immediately.
- During an immediate release the ball can be in flight while the upper body is still blending out of crawl. This is visible in the first moments of the pass capture. [Pass released directly from dribbling](Evidence/Milestone29/Pass-From-Dribble.jpg)

## Verification

- The saved switches passed a Play Mode run on Unity 6000.6.7f1. It covered selected Blue pickup through movement without E, AI White pursuit and pickup, nearest-candidate contest, standing hold, crawl, sprint, turning, stop/gather, a legal steal, physical pass and catch, and a direct shot from dribbling. Sustained crawl lead was 0.311 m at the sampled frame. A Blue goal passed both replay angles and returned to the real White centre restart.
- The match fixtures separately passed both cameras, keyboard swim and sprint, boundary throws on both sides, goal throws, a keeper corner, a physical penalty catch, and Blue, White and own-goal scoring attribution. The penalty fixture fixed the keeper’s catch probability in memory to exercise the actual catch contact. It did not change the saved probabilities.
- The replay fixture verified the shared branded mesh. The actual goal-line replay and close-up were inspected. The automated possession fixture drives the same motor and release APIs used by the live controls. The separate keyboard fixture sent Input System events. Phone controls and Android performance remain unverified.
- The live stroke probe captured four frames per cycle from the moving camera. The closest sampled ball center was 0.255 m from the elbow-to-wrist paths at crawl and 0.235 m at sprint, against the 0.14 m radius. The minimum lead from the head joint was 0.198 m in crawl and 0.161 m in sprint. These are pose samples rather than continuous finger-mesh collision checks. [Crawl sequence](Evidence/Milestone29/Crawl-Sequence.jpg) · [Sprint sequence](Evidence/Milestone29/Sprint-Sequence.jpg)
- Repeatable scripts are in `Tools/Verification/CompetitionBallVerification.cs`. Raw results include `Logs/Milestone29/Regression/BallFinal.txt`, `CompetitionBallVerification.Presentation.txt`, `BallPhysicsPolishVerification.Boundaries.txt`, `PenaltyVerification.Save.txt`, and `MatchPolishVerification.GoalCases.txt` in the same directory. [Ball close-up and goal-line replay](Evidence/Milestone29/Ball-and-Replay.jpg) · [Branded ball inside the net](Evidence/Milestone29/Branded-Ball-In-Goal.png) · [Hold, crawl and sprint](Evidence/Milestone29/Ball-Possession.jpg)

## Inspect and recover

- Play from Boot and launch a match in the lobby, or use **Tools > Water Polo > Play Olympic Match Arena**. Skip or watch the ceremony, then let the opening sprint-off finish. Swim to a slow loose ball without E. Use W/A/S/D to move, Left Shift to sprint, B to pass and Space to shoot. Release movement to gather the ball.
- In saved Edit Mode use **Tools > Water Polo > Arena > Restore Previous Ball and Carry** to restore the previous sphere and material and disable pickup and dribbling. **Tools > Water Polo > Arena > Apply Competition Ball and Dribbling** restores the shipped setup. It migrates the exact experimental 1.32 m reach and 0.32 m clearance while preserving later Inspector tuning.
- `Logs/Milestone29/Arena-Pre29B.unity` is the local pre-29B arena copy. **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** restores the normal Play entry after a direct arena test.
