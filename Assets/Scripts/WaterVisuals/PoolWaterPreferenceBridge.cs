using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    // Observes the existing presentation preference; never writes gameplay or persistent economy state.
    [DisallowMultipleComponent]
    public sealed class PoolWaterPreferenceBridge : MonoBehaviour
    {
        public GamePreferences preferences;
        public PoolWaterVisuals water;
        private void OnEnable() { if (preferences != null) preferences.Changed += Apply; }
        private void Start() => Apply();
        private void OnDisable() { if (preferences != null) preferences.Changed -= Apply; }
        private void Apply()
        {
            if (water == null || preferences == null) return;
            int index = (int)preferences.Current.graphics;
            if (water.RuntimeMaterial == null || water.qualityIndex != index) water.ApplyQuality(index);
        }
    }
}
