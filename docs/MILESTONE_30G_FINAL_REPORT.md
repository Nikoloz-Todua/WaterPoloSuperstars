# Milestone 30G final report

Water Polo Superstars · ToduStudios  
Starting checkpoint `c1b37b6` · Unity 6000.6.7f1 · Blender 5.2.3 LTS

## Task A — Freestyle

The original Blender kicks used a fixed-world reference while the torso rolled. The old animation-only FBX matched the source joint geometry at nine phases. Unity Humanoid changes the shin's local orientation. The 30D override reconstructed the legs independently of the authored source and kept the Humanoid pelvis. Close baseline views reproduced visible lower-body alignment problems at particular phases. They did not isolate a universal 180° turn as the sole cause.

Blender now has editable normal and sprint Actions with a common pelvis and leg frame, hip-led alternating kicks, delayed knee flexion and foot articulation. The production Animator still supplies the upper body and stroke phase. After its evaluation, one runtime layer blends the authored bind-relative pelvis, thigh, shin, foot and toe rotations on the same 66-bone rig. The tread, protected carry and shooting leg paths retain their existing roles.

| Focused production check | Observed result |
| --- | --- |
| Advancing crawl and sprint | Both cycles advanced through the real visual and motor. The phase probe rendered ordinary crawl every 12.5% plus the loop endpoint, with side, rear and overhead views. It also sampled sprint and upright tread. |
| Normal and sprint ball-leading | The carrier reached leading at both speeds and gathered back into stationary possession. |
| Eggbeater | The probe returned to upright movement. The sampled upright pose matched the existing tread output when the overlay was inactive. |
| 30F compatibility | The 8.2 m facing-away pass reached the intended receiver, selected them and caught. First rendered palm landmark separation rounded to 0.0000 m. |
| Processing | The authored flutter marker averaged 0.0056 ms per sample over 125 Windows Editor samples in the side-view fixture. It covers the pose application on the advancing athlete. |

The screenshot harness was corrected to stop the Animator resampling after applying its final pose. The retained comparisons use the final skinned render. The original inversion defect stays open until the developer reviews the moving athlete. [Technical diagnosis](PROFESSIONAL_FREESTYLE_ANIMATION_REPAIR.md) · [Matched phases](Evidence/Milestone30G/Freestyle-Before-After-Matched-Phases.jpg) · [Production views](Evidence/Milestone30G/Freestyle-Production-Views.jpg)

## Task B — Walk, stand and foot planting

The existing Meshy clip produced a visible raised-toe moment when sampled on the production Humanoid. The runtime also imposed a nominal foot slope, and standing and captain stepping used a different pose path from the entrance. The original rig's bind ankle and toe heights differ from those nominal depths.

Blender now supplies a 1.10 s walk and a 2.50 s stand, authored on the production skeleton. Heel contact, flat stance, toe-off and the swing curve use 1.28 m per same-foot cycle. Two new animation-only FBX takes supply the body without driving the root. In the arena, the cast's route distance supplies phase, the shared solver uses heel and forefoot pivots from the skinned rig, and the blend settles into the authored stand. Captains use the same approach and turn back toward their marks. The officials use their existing Humanoid model with a scaled stride and a short pivot as they brake and reverse. The existing direction and signal authority still decide where an official moves and looks.

| Focused visual check | Observed result |
| --- | --- |
| Standalone source comparison | Captured the original Meshy sample, the new walk and the new stand on the same skinned production athlete. Compared the authored motion in Blender and Unity. |
| Team walkout and lineup | Five advancing walkout frames captured, plus the live lineup. There are 14 cast athletes. |
| Captain approach, exchange and return | Captured the approach, standing exchange and return step under the existing ceremony. |
| Official movement | The live professional official travelled 1.50 m during the captured run, then pivoted and reversed with the ball location. A first fixture only searched the referee root for its child visual. The corrected fixture targeted the saved production hierarchy. |

The deck solver uses two calibrated support points. These approximate a curved sole. Exact surface penetration and support-foot slip were outside the captured measurements, and extreme turns and personal appearance remain open. The production swimwear is still the existing asset and its separate appearance work remains in 30H. [Deck implementation](PROFESSIONAL_DECK_WALKING_AND_FOOT_PLANTING.md) · [Standalone comparison](Evidence/Milestone30G/Walking-Clip-Comparison.jpg) · [Walkout](Evidence/Milestone30G/Walking-Gait-Sequence.jpg) · [Captains](Evidence/Milestone30G/Natural-Standing-and-Captain.jpg) · [Officials](Evidence/Milestone30G/Referee-Walking.jpg)

## Saved project and recovery

- Unity is in Edit Mode with Boot as the normal Play scene. The saved OlympicMatchArena still has 14 athletes and the existing GUID `5c4d3741df6b7f54e8cfd34d391d78d1`. Its SHA-256 and the real profile SHA-256 matched the values at the start of 30G.
- The current Console showed zero errors and warnings following successful compilation. Short isolated probes ended and Play Mode stopped. The retained Pipeline history includes earlier fixture failures described in the handoff. No full match ran.
- The editable athletic source is `SourceAssets/Athletes/WPS_Animations_001/Blender/WPS_AquaticMotion_001.blend`. The original aquatic Actions and Meshy walk remain available. Start source recovery from `c1b37b6` with the exact inventory. Git operations remain with the developer.
- Personal approval, Android checks and the previously deferred roadmap remain open. Follow the 30G manual guide for the exact menu paths, playback controls and signs to look for. [Manual guide](MILESTONE_30G_MANUAL_TEST_GUIDE.md) · [Live handoff](MILESTONE_30G_LIVE_HANDOFF.md) · [Changed files](MILESTONE_30G_FILES.txt)
