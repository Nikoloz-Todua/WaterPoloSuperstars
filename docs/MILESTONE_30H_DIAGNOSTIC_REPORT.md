# Milestone 30H production diagnostic

Baseline is `a77e1bb` and Unity 6000.6.7f1. The goalkeeper technique reference at the supplied Desktop path was available and inspected.

## Competition briefs

The saved arena uses `CompetitionBriefs` on all fourteen athletes, including both goalkeepers. Each renderer points to the 2,688-triangle `Assets/ProductionAthletes/Candidate/AtlasCompetitionBriefs.fbx` with 66 bones and bind poses. The fallback renderers are disabled. The shipped body is the existing cut Atlas athlete. Blender's `AtlasCompetitionBriefs.blend` has the registered openings, normalized weights and a FabricMask for the waistband, leg binding and side. The Blue shared URP Lit material samples only the generic fabric, so the mask is missing from the visible Blue kit. White samples an earlier band bake. White's raised base tint and both kits' existing highlight settings can flatten the separation between skin, panel and trim under strong reflections.

The source and saved bindings supported a fabric pass with the registered geometry. `Tools/Art/BakeWhiteBriefsFabric.py` baked both kits using the actual UV and FabricMask. The matched dry views show that the Blue waistband now reads separately from the fabric panel. The advancing Blue crawl confirms that the kit remains visible through the production water. Any residual fit defect needs a pose-specific skin penetration check before changing the seam.

## Goalkeeper

`PrototypeGoalkeeper` drives station motion. Its broad `ThreateningShot` getter only checks shot status and local forward velocity. `RiggedSwimmerVisual` drives a generic Humanoid Block flag from it. Even with zero save weight, `WaterPoloThrowPose.Keeper` applies a 57% reach toward two forward hands. Separately the keeper interception observer sweeps a lifted CarryAnchor sphere, so rendered arm and ball contact can disagree. Once that sphere contacts, a random save rejection can allow the ball to pass. The existing aim and keeper presentation use the root station plane, and a Save trigger can play after contact.

The production keeper now uses one pose path with low hands and a timed approach read. The existing sweep samples the evaluated palm landmarks and uses BallController's catch or deflection methods. The unrigged compatibility path remains. The first fixture round established that the production keeper could catch an ordinary pass and a directional shot. It also exposed two timing defects. The fast near-post miss reached the station before the full reaction delay, and the goal-presentation freeze could preserve the failed dive blend. The implementation now derives the reaction time from incoming speed and lets a keeper finish a save recovery during goal presentation. The final report records the reruns.

Preserve the 30G swim and deck clips, goalie selection, match rules and ball authority. No source models, match scene or profile have been changed at this checkpoint.
