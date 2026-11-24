# Retained CC0 source files

See `provenance.json` for source URLs, retrieval date and SHA256 hashes, and `CC0-1.0.txt` for the legal text. Quaternius is the original author; KinemIQ distributes the fitted mannequin and selected unified-rig clips. These three GLBs are kept outside Assets so Unity does not need a glTF package or import the dense original model.

Regenerate the FBXs with Blender in background mode:

`blender --background --python Tools/Art/PrepareAnimatedSwimmer.py`

Then run **Tools > Water Polo > Animated Athletes > Set Up Prototype and Lab** in Unity for first setup. After intentional source regeneration use **Reimport Prototype Source FBXs** in the same submenu, preserving the existing `.meta` files. Reimport explicitly reapplies Humanoid and clip import settings. Existing prefab/controller tuning is preserved by ordinary setup; it is not regenerated on repeated calls.

The eight custom clips are a water polo animation prototype. The two original reference clips remain separately named and are not substituted for head-up freestyle.
