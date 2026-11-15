# Rigged swimmer prototype sources

The mannequin and two reference swimming clips are CC0 1.0 assets by **Quaternius**, distributed by **KinemIQ**. KinemIQ identifies its default mannequin as the Universal Animation Library 2 mannequin, fitted to its unified skeleton; the selected clips are from Universal Animation Library 1.

- Model: https://kinemiq.app/studio/assets/character_ual.glb
- Swim reference: https://kinemiq.app/animations/move-swimming/
- Treading reference: https://kinemiq.app/animations/idle-in-water/
- Source/license credits: https://kinemiq.app/sources/
- Original library: https://quaternius.com/packs/universalanimationlibrary.html
- License: https://creativecommons.org/publicdomain/zero/1.0/

Retrieved 2026-11-15. Original GLBs, exact download URLs, SHA256 hashes and the CC0 legal text are retained in `SourceAssets/SwimmerAnimation/` outside Unity's runtime imports. This notice and the legal text also ship in `Assets/StreamingAssets/ThirdPartyNotices/`.

Project modifications: reduced mannequin geometry, reassigned skin/swimwear regions, fitted weighted cap/ear guards, FBX conversion, explicit Humanoid mapping and in-place clip import. `Tools/Art/PrepareAnimatedSwimmer.py` reproducibly prepares these from the retained source GLBs using headless Blender 5.2.3. No KinemIQ application/engine code is redistributed.

`Source_swim_fwd_loop` and `Source_swim_idle_loop` are converted references, not the controller's final strokes. The reference swim has paired arm motion. The eight project clips (`TreadWater`, `HeadUpSwim`, `Sprint`, `Possession`, `Pass`, `Shoot`, `Block`, `Save`) are prototype keyframes authored/adapted for this project; the treading foundation retains CC0 source pose motion. These are stylized prototypes, not professional water polo motion capture.

The separately investigated Asset Store **Basic Character Animations FREE** package 409552 was **not downloaded or imported**. Its Standard Asset Store EULA is distinct from CC0. No Asset Store purchase, login, entitlement or package license is implied by this prototype.
