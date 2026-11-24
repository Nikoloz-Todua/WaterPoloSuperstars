# Production athletes and modular customization

Milestone 27 installs one Meshy-derived athlete body and modular kit on the fourteen existing OlympicMatchArena swimmers. The saved arena keeps six field players and a goalkeeper per side. The preview scene and native recovery are outside the build scene list.

## Source and editable art

The developer supplied Meshy Pro's Atlas Form FBX and four PNG maps. The files were found at the expected OneDrive Desktop location. The five project copies in **SourceAssets/Athletes/WPS_Athlete_001/Original/** match the Desktop SHA-256 hashes. The originals were read in place for verification.

Blender 5.2.3 LTS loaded one unrigged mesh with 5,228 unique geometric vertices, 10,452 triangles and one UV layer in approximately 0–1 on both axes. Each of 15,678 topological edges was used twice. The body is 1.900 m tall, 1.149 m wide in A-pose and 0.325 m deep, with feet near world zero and the face towards Blender −Y. The FBX imports four packed image entries and the four supplied external PNGs are each 2048 × 2048. The geometry count here is the Blender mesh count. Unity uses separate vertices where normals or UVs split.

Open **SourceAssets/Athletes/WPS_Athlete_001/Blender/WPS_Athlete_001.blend** for the editable, clothed rig. The hidden authoring collection contains the untouched import. It is outside Unity's import tree. The runtime FBX contains only the fitted skin, briefs, cap and number surfaces with their shared reference skeleton. It excludes the source body and its embedded maps.

The batch workflow uses the installed Blender executable and **Tools/Art/PrepareProductionAthlete.py**. It reads the exact Quaternius/KinemIQ source skeleton in **SourceAssets/SwimmerAnimation/character_ual.glb**. The live Blender MCP service was queried and localhost:9876 was unavailable. Background Blender worked independently of any user document. **Tools/Art/BuildAthleteCapNumberAtlas.ps1** builds the Windows authoring atlas. Neither workflow requires a paid add-on.

## Meshes, clothing and materials

| Part | Triangles | Runtime use |
| --- | ---: | --- |
| Skin with garment coverage removed | 10,536 | One textured skinned renderer |
| High-cut competition briefs | 820 | One skinned renderer, independent fabric material |
| Cap fabric, bindings, ear guards and chin strap | 1,300 | One head-weighted renderer, two materials |
| Two curved number print regions | 192 | One head-weighted renderer, one alpha-clipped material |
| **Total per athlete** | **12,848** | **Four enabled skinned renderers, five material slots** |

- The source mesh was conformed to the reference rest rig with shoulder, elbow, wrist, knee and ankle landmarks and a local shoulder relaxation pass. The blend file retains the skin UVs. The final authoring report found zero degenerate faces and one to three normalized weights per vertex. Hands currently deform as single weighted shapes.
- The briefs are cut along the actual retargeted body surface with high-cut legs, the upper hip band and front/back coverage. Body polygons in the covered region are omitted. The garment and adjoining body follow the same deformation at their boundary. The dark default fabric is plain. The waistband silhouette is modeled by its cut. Extra cloth seams and piping are art polish.
- The cap follows the head and has a fitted shell, face binding, molded guards with six narrow ports and a centre opening on each side, and a chin ribbon fitted to the jaw. The close-up shows the procedural edge faceting. The headgear can be edited separately in Blender.
- Briefs U wraps around the body and V tracks height. Use horizontal Repeat for new garment designs across the wrap seam. The cap shell has circumferential U and crown V. Guard and trim UVs are a separate material region. Both cap number regions have outward-facing UVs and can share one atlas. There is room for future textures, panels and logos on the fabric materials.
- URP Lit reads sRGB albedo and a normal map with strength 0.65. Smoothness is authored by inverting the linear roughness map, clamping it to 0.18–0.52 and storing it in alpha on the linear metallic/smoothness texture. The exported metallic map ranged 0–0.329. The packed metallic channel is zero for dielectric skin. The material multiplies packed smoothness by 0.82.
- The default platform limit for the three skin maps is 2048. Android overrides them to 1024 with ASTC 6×6 for albedo and normal and ASTC 8×8 for smoothness. The 512 atlas uses ASTC 4×4 on Android. All four use mipmaps. The Windows Editor was set to Android and reported the three skin maps at 1024 and 19.37 MiB total runtime texture-object memory for all four maps. The ASTC build payload estimate with mipmaps is about 1.86 MiB, excluding engine overhead and platform fallbacks.

## Rig and animation ownership

The arena retains the existing 66-bone Humanoid rig, valid Avatar and AnimatedSwimmer Animator Controller. The Editor resolves every exported bone path onto the existing hierarchy and compares local rest matrices before it attaches renderers. The added interchange FBX supplies mesh bind data. The original Avatar and Animator continue to drive the bones. Gameplay actors, world anchors, cameras, colliders, Rigidbody instances and waterline settings remain with the existing systems.

| Presentation | Milestone 27 behaviour |
| --- | --- |
| Head-up swim, sprint, stop and turn | Existing in-place blend tree follows the motor and stamina |
| Tread and eggbeater approximation | Existing TreadWater clip, with the same visual alignment |
| Receive and hold | Existing right-hand Humanoid IK tracks the authoritative target |
| Dry/wet passes, lob and shot variants | Existing upper-body Pass or Shoot follows real ball release |
| Block and save | Existing Block state and Save trigger on real keeper events |
| Deck standing, walking, pennant and huddle | Existing procedural deck joints and positions drive the copied rig |
| Celebration and own-goal reaction | Existing pose playback with the scorer or regret joint offsets |

Root motion remains off. No new motion clips were authored. Shoulder extremes, ankle flex, rigid fingers, isolated leg motion, deck foot planting and the existing eggbeater remain candidates for specialist rig and animation work. There are no new run, sit or dive clips.

AthleteEquipment binds the four clothing/skin renderers. It derives role and number from SwimmerIdentity, including substitute cap overrides. A missing required part/material selects the retained clothed mannequin. The skin surface physically omits covered geometry. There is no naked runtime body or separate equipment Animator.

The renderer-only copy preserves shared meshes, materials, resolved bones and material property blocks. The intro cast inherits kit and numbers from the live actors. Goal playback reads each recorded cap number from the existing capture buffer and applies that atlas cell to the replay actor. The temporary reserve clone uses the incoming cap, and the return-to-bench visual switches to the departing cap. The tactical slot continues to own the actual match identity. Slot occupation, exclusions, timing, scoring and rewards remain under their prior authorities.

## Preview and customization

- **Tools > Water Polo > Athletes > Open Athlete Preview** opens the saved development scene. Select **WaterPoloAthlete** and inspect **Athlete Equipment**. Choose Preview Team, Preview Goalkeeper and Preview Number. Edit Skin or the Blue/White kit reference to use another compatible material setup.
- **Tools > Water Polo > Athletes > Play Athlete Preview** opens the same scene in Play Mode. Use Left/Right Arrow to orbit, F1 for the full body, F2 for the cap, F3 for the waist, and Tab to switch between dry standing and animated poses. In animated mode, keys 1–8 select tread, swim, sprint, hold, pass, shoot, block and save. Stop Play before keeping Inspector changes.
- Under **Assets/ProductionAthletes/Materials/**, the Blue Team Kit and White Team Kit assets point to separate field cap, keeper cap, briefs and number materials. Edit or duplicate the kit to set cap colour, contrast, patterns or briefs textures. The keeper shell is red with the appropriate guard/binding colour. Live numbers always follow the match roster. The preview range is 1–63.
- A revised cap shape or brief design can be exported as independently named skinned geometry. A new body shape needs a matching bind fit and garment coverage before it can replace the current visuals. Hairstyles, logos and extra accessories are extension points in the art and material setup. Runtime body swapping and a player-facing editor are future work.
- **Tools > Water Polo > Play Hub Entry** starts the production flow. **Tools > Water Polo > Play Olympic Match Arena** enters the match directly. **Tools > Water Polo > Recovery > Use Boot as Editor Play Entry** restores normal Play after the direct arena command.

## Recovery

**Tools > Water Polo > Athletes > Restore Previous Athlete Visuals** enables the four mannequin renderers and removes the Milestone 27 visual bindings on the saved arena. **Tools > Water Polo > Athletes > Apply Production Athlete Visuals** installs the four authored parts and kit binding once per athlete. Both commands require saved Edit Mode. They operate on the arena; later gameplay edits remain in that scene. The exact pre-edit native scene is **Assets/Scenes/Recovery/OlympicMatchArena_PreProductionAthletes.unity**. Its SHA-256 matches the original arena bytes. The production GUID remains **5c4d3741df6b7f54e8cfd34d391d78d1**. The former prefab, model, Avatar, controller and clips remain available. The one-athlete arena used for verification was temporary and was removed after the gate.

## Verification and limits

- Unity 6000.6.7f1 compiled the changes. The saved-scene record comparison preserved all 7,469 existing records. Fourteen existing prefab instance records gained renderer override and added-part entries. The new records are visual objects, skinned renderers, kit scripts and stripped references to existing bones.
- An isolated copy of the arena ran Blue #1 with the new kit and thirteen existing mannequins first. Actual keyboard swim and sprint advanced the original motor. The target head joint remained 0.26 m above the configured surface. The held ball centre was 0.044 m from the animated right wrist in the measured hold. An actual dry pass reached its receiver. A physical shot triggered one release presentation and one goal, followed by both replay angles.
- The full roster passed all eight ceremony stages, pointer pause/resume, cleanup and the first physical sprint acquisition. The scene continued into the live broadcast. The existing deck lighting is dim in the lineup and during the exchange.
- The preview measured 1.9003 m from the lowest baked point to the cap in standing pose. Queued Tab, F2, Right Arrow and 2 exercised dry/animated switching, close framing, orbit and animation state selection.
- A fresh full-roster goal regression passed Blue and White shots, both own-goal cases, defensive deflections, unknown credit, net-tail playback and live centre restarts. Camera-relative movement matched WASD and arrows in both cameras. Fresh boundary tests passed both sidelines, both ends, a keeper corner and the 18-second restart. The wide penalty, field/keeper deflections and halftime reversal also passed.
- An isolated physical penalty catch passed with deterministic keeper save and catch chances. The keeper's regular driver logged a Save animation trigger. The roster checks verified all fourteen atlas values, field and keeper materials, and that an instanced athlete with a missing skin material falls back to the four clothed mannequin renderers.
- A fresh White third-foul scenario returned the team to six field players and changed the printed cap from 3 to 6. Contextual C switched to another eligible field player. Accelerating the real shot clock reached the normal turnover and White possession.
- Boot → Lobby → Match → Results → Lobby passed with a memory-backed verification profile. Pointer tests covered restart and exit during the intro, the goal replay skip, four physical sprint acquisitions, the result and one reward receipt. Period endings were accelerated. The real progression file hash was unchanged.
- In the parked 983 × 494 Windows Game View with D3D12 on RTX 5060 Ti, thirty samples per configuration reported 1,104 draws and 2,159,115 rendered triangles with the new kit versus 1,134 and 2,013,323 for mannequins. These totals include the stadium, water and render passes. The two sets used the same paused actors and camera. The raw roster geometry is 179,872 versus 139,048 triangles. Per-character animation/skinning CPU time and GPU time were not isolated.
- There is one current gameplay detail level. Native Android hardware was absent from adb; actual phone FPS, memory, thermal performance and iOS remain unverified. The final art still needs a broader shoulder/garment pose review and authored aquatic/deck motion, including stronger treading biomechanics.
- Retained history includes an exclusion demonstration timeout after another fixture had parked the AI and an obsolete keeper fixture that failed during possession setup. The fresh arena replacement and physical penalty catch passed. Final Editor state reported zero current errors or warnings and compilation failure false. The saved arena was open in Edit Mode with Boot restored as Play entry.

The focused verification script is **Tools/Verification/ProductionAthleteVerification.cs**. The full logs and Game views are in **Logs/ProductionAthletes/**. Selected durable views are in **docs/Evidence/ProductionAthletes/**. See **docs/MILESTONE_27_FILES.txt** for the exact project paths and recovery files.

