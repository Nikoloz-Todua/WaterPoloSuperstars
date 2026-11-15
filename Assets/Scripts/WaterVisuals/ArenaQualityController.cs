using UnityEngine;
using UnityEngine.Rendering;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Scene-owned presentation budgets. Never changes asset tuning, physics or progression.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class ArenaQualityController : MonoBehaviour
    {
        public ArenaDimensions dimensions;
        public GamePreferences preferences;
        public PoolWaterVisuals water;
        public RenderPipelineAsset[] pipelines;
        public GameObject mediumAudience, highAudience, fineProps;
        RenderPipelineAsset original, applied;
        int last = -1;
        void OnEnable()
        {
            original = QualitySettings.renderPipeline;
            if (preferences != null) preferences.Changed += Apply;
        }
        void Start() => Apply();
        void Apply()
        {
            int level = preferences == null ? 2 : (int)preferences.Current.graphics;
            if (level == last) return;
            last = level;
            if (pipelines != null && level < pipelines.Length && pipelines[level] != null)
            { applied = pipelines[level]; QualitySettings.renderPipeline = applied; }
            water.ApplyQuality(level);
            if (mediumAudience != null) mediumAudience.SetActive(level >= 1);
            if (highAudience != null) highAudience.SetActive(level >= 2);
            if (fineProps != null) fineProps.SetActive(level >= 1);
        }
        void OnDisable()
        {
            if (preferences != null) preferences.Changed -= Apply;
            if (QualitySettings.renderPipeline == applied) QualitySettings.renderPipeline = original;
            last = -1;
        }
    }
}
