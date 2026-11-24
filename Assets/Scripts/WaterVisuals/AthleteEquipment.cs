using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Applies the selected kit to a clothed skin on the existing swimmer rig.</summary>
    [DefaultExecutionOrder(-20)]
    public sealed class AthleteEquipment : MonoBehaviour
    {
        [SerializeField] SwimmerIdentity identity;
        [SerializeField] SkinnedMeshRenderer body, briefs, cap, number;
        [SerializeField] Renderer[] fallback;
        [SerializeField] Material skin;
        [SerializeField] AthleteKitStyle blue, white;
        [Header("Preview without a match identity")]
        [SerializeField] PrototypeTeam previewTeam;
        [SerializeField] bool previewGoalkeeper;
        [SerializeField, Range(1, 63)] int previewNumber = 7;

        static readonly int BaseMapTransform = Shader.PropertyToID("_BaseMap_ST");
        MaterialPropertyBlock properties;
        AthleteKitStyle currentStyle;
        Material currentSkin;
        bool ready, currentGoalkeeper, detached;
        int printedNumber = -1;
        public Renderer NumberRenderer => number;

        void Awake() => Refresh();

        void LateUpdate()
        {
            if (!detached) Refresh();
        }

        public void SetDetachedNumber(int capNumber)
        {
            Refresh();
            detached = true;
            if (ready) UpdateNumber(capNumber);
        }

        void Refresh()
        {
            bool goalkeeper = identity != null ? !identity.IsFieldPlayer : previewGoalkeeper;
            var style = (identity != null ? identity.Team : previewTeam) == PrototypeTeam.Blue ? blue : white;
            if (style != currentStyle || skin != currentSkin || goalkeeper != currentGoalkeeper || !ready)
            {
                var capMaterials = style != null ? style.Cap(goalkeeper) : null;
                ready = IsRenderable(body) && IsRenderable(briefs) && IsRenderable(cap) && IsRenderable(number) &&
                    skin != null && style != null && style.Briefs != null && style.Number(goalkeeper) != null &&
                    capMaterials != null && capMaterials.Length == cap.sharedMesh.subMeshCount;
                if (ready)
                    foreach (var material in capMaterials)
                        if (material == null) { ready = false; break; }

                if (ready)
                {
                    body.sharedMaterial = skin;
                    briefs.sharedMaterial = style.Briefs;
                    cap.sharedMaterials = capMaterials;
                    number.sharedMaterial = style.Number(goalkeeper);
                }
                SetVisible(ready);
                currentStyle = style;
                currentSkin = skin;
                currentGoalkeeper = goalkeeper;
            }

            if (ready) UpdateNumber(identity != null ? identity.CapNumber : previewNumber);
        }

        static bool IsRenderable(SkinnedMeshRenderer part) =>
            part != null && part.sharedMesh != null && part.rootBone != null;

        void SetVisible(bool visible)
        {
            if (body != null) body.enabled = visible;
            if (briefs != null) briefs.enabled = visible;
            if (cap != null) cap.enabled = visible;
            if (number != null) number.enabled = visible;
            if (fallback == null) return;
            foreach (var renderer in fallback)
                if (renderer != null) renderer.enabled = !visible;
        }

        void UpdateNumber(int capNumber)
        {
            if (printedNumber == capNumber) return;
            properties ??= new MaterialPropertyBlock();
            SetNumber(number, capNumber, properties);
            printedNumber = capNumber;
        }

        internal static void SetNumber(Renderer renderer, int capNumber, MaterialPropertyBlock block)
        {
            int cell = capNumber is >= 1 and <= 63 ? capNumber : 0;
            const float step = 1f / 8f;
            renderer.GetPropertyBlock(block);
            block.SetVector(BaseMapTransform, new Vector4(step, step, (cell % 8) * step, (7 - cell / 8) * step));
            renderer.SetPropertyBlock(block);
        }
    }
}

