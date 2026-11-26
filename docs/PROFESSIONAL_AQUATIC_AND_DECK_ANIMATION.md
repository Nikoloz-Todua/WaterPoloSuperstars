# Professional aquatic and deck animation

## Milestone 30D prone chain revision

The developer rejected the 30C appearance in live play. The new audit sampled the Blender Action and reimported motion-only FBX at 0%, 25%, 50%, 75% and 100%. The FBX joint geometry tracked the Action, including its approximately 0.40 m hip-relative toe excursion. The source aimed thighs and calves against a fixed world-space reference while the torso rolled. The 30C ankle/calf correction restored axial shin orientation but kept the source excursion, femur orientation and mismatch to pelvis roll.

`ProneFlutterPose` now uses cached anatomical joint frames and the sampled pelvis orientation. It keeps the original Humanoid upper body and stroke phase. The narrower hips-led kick establishes the femur, carries its anterior frame through the knee hinge into the shin, and resolves the ankle and toe together. Prone weighting and the pelvis gate keep the chain out of upright treading, deck gait and retreat. The Blender source, Avatar, skin weights and 66-bone production skeleton remain available. The matched Unity crawl shows approximately −0.194 m to +0.061 m toe excursion relative to the hip. The rendered correction is in production and awaits the developer's visual acceptance. [Matched phase views](Evidence/Milestone30D/Freestyle-Matched-Phases.jpg)

## Milestone 30C comparison history

## Prone lower body

The editable `WaterPoloFreestyle` and `WaterPoloSprint` Blender Actions contain prone hips and thighs, rearward toes and a shallow, alternating knee bend. Blender source samples measured freestyle calf front-down between 0.970 and 0.998. The live Unity athlete retained the hip and thigh orientation and 10–23° knee bend, but its final calf front-down dots averaged −0.018 and −0.016 in ordinary crawl. A value near zero indicates a sideways anatomical face. The Humanoid evaluation loses the authored axial rotation at the calf, and the 30B correction addressed the ankle only.

`RiggedSwimmerVisual` now carries each thigh's anatomical frame through the actual knee hinge into the shin. It aligns the ankle to that shared frame while retaining the sampled sagittal extension and body roll. This changes the final pose without shifting the measured hip, knee or ankle joint positions or remapping the Avatar. Prone weighting takes the adjustment out of upright floating, eggbeater, retreat and deck acting. Existing replay recording samples final bone transforms.

With the same saved arena, live crawl calves averaged 0.984 and 0.985 front-down after repair. Sprint averaged 0.980 on both and ball-leading crawl averaged 0.983 and 0.981. Ordinary crawl knee flexion was 10.37–22.46°. Before/after side, rear, overhead and underwater captures include phases of the alternating kick. Personal visual approval and transitions under uncontrolled match conditions remain open.

The before/after tiles show corresponding view and movement categories sampled during advancing play. Frame phase varies between the two sessions. The reusable Editor probe is `Tools/Verification/Milestone30CAquaticVerification.cs`, entry `Milestone30CAquaticVerification.After`. [Prone comparison](Evidence/Milestone30C/Prone-Comparison.jpg)

## Walking and standing

The original Meshy Walking take contains steep toe-joint angles. The prior deck solver clamped ankle-to-toe pitch after Humanoid sampling and the stationary scene pointed that segment horizontally. The foot and toe bones sit at different heights in a flat sole, so a horizontal joint-to-joint axis raises the sole toward the toes. The shared deck solve now reconstructs heel approach, foot-flat support, toe-off and swing. Deck distance advances the phases, and a bounded knee solve targets the heel/toe-calibrated contact. The imported walk still supplies the walkout's hips, arms and torso. Captain steps use the same legs with the pennant pose. The professional referee path uses the same floor contact under its existing observation and arm signals.

Fresh Windows Editor coverage advanced through all eight ceremony stages with fourteen visual actors and a held match clock. The standing ankle-to-toe axis was −17.94°. Walking and captain step samples ranged from −44.94° at toe-off to −3.94° near forward contact. These values describe the axis between joints at different heights. The close exchange frame and walkout sequence support a visual review of sole placement. The live officials fixture measured a 1.000 travel-facing dot, 1.000 pool-gaze dot, a 0.212 m minimum ankle lane and a 0.696 m estimated stance-floor height. It also exercised both foul award directions, the exclusion sweep and the penalty ready cue. [Ceremony sequence](Evidence/Milestone30C/Deck-Sequence.jpg)

## Editable assets and scope

- Aquatic Actions and original rig: `SourceAssets/Athletes/WPS_Animations_001/Blender/WPS_AquaticMotion_001.blend` and `SourceAssets/Athletes/WPS_Athlete_001/Blender/WPS_Athlete_001.blend`.
- Walking reference: `SourceAssets/Athletes/WPS_Animations_001/Original/Meshy_AI_Atlas_Form_20261124_animated_unity_humanoid_without_skin_60fps_All_animation.fbx`. Production imported walk: `Assets/ProductionAthletes/AuthenticMotion/MeshyDeckSource.fbx`.
- Official and garment source: `SourceAssets/Officials/WPS_Referee_001/Blender/PoolsideOfficial.blend` and `SourceAssets/Athletes/WPS_Swimwear_001/Blender/AtlasCompetitionBriefs.blend`.

The Blender source files were inspected in the existing installation and were preserved. No new shooting elevation, lateral shooting variation, goalkeeper pose repair or splash behavior is part of this checkpoint. Use the current isolated previews to inspect the inherited poses.
