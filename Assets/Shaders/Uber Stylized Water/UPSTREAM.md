# Uber Stylized Water — imported subset

Source: https://github.com/MatrixRex/Uber-Stylized-Water

Revision: `1fdd16c6312611fef7fb248dee86cdd3fd456a1e` (2026-10-04).
Downloaded 2026-11-15 via GitHub codeload ZIP; archive comment confirms this revision.
ZIP SHA256: `9473DC7AF3BF8D864384A854A8221D0A0689BB77EFFADF77564866BEC585CD22`.

Copyright (c) 2025 MatrixRex. MIT: see LICENSE.txt.
CustomLighting.hlsl: Copyright (c) 2020 Cyanilux. MIT: see Third Party/URP_ShaderGraphCustomLighting-main/LICENSE.
Retain both notices when redistributing. Copies are in Assets/StreamingAssets/ThirdPartyNotices for player distributions and docs/Licenses for source handoff.

Imported: prebuilt UberStylizedWater.shader, CubeMap.hlsl, CustomLighting.hlsl,
material Inspector, planar reflection scripts, Normal 1/2, Noise 1, Caustic 1,
Guide.txt and original .meta identities. UPSTREAM-CREDITS.md preserves the original
special-thanks document, including the texture source links. Not every asset named
in that document is included here.

The cited Material Maker Caustics node by Oneleven (material?id=913) and Calinou's
OpenGameArt caustic textures are CC0. The other cited OpenGameArt caustic source
(node/12987, leeor_net) is CC-BY-SA 3.0; optional Caustic 2/3 textures are **not imported**.
Do not copy additional upstream art on the assumption that every linked source is MIT.

No demo scene, terrain art, packages, project settings, source Shader Graph,
ShaderGraphVariables editor extension or RexTools dependency is imported.
The prebuilt shader avoids the source graph's 25,000-variant editing requirement.
Existing Unity 6000.6.7f1 / URP 17.6.0 packages are retained.

Local changes to the reflection scripts:

- Unity 6.6: GetInstanceID -> GetEntityId; use unsorted FindObjectsByType overloads.
- URP 17.6: SingleCameraRequest explicitly specifies its destination RenderTexture.
- Restore temporary culling/fog/LOD state in a finally block if reflection rendering fails.

The prebuilt shader and HLSL includes are unchanged. Integration and test-scene code
belongs to Water Polo Superstars; see docs/WATER_VISUALS.md.
