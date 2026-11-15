using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    [CreateAssetMenu(menuName = "Water Polo/Pool Water Quality")]
    public sealed class PoolWaterQualityProfile : ScriptableObject
    {
        public Material waterMaterial;
        [Tooltip("Used only by the isolated visual test. Never assign this globally to gameplay without profiling.")]
        public UniversalRenderPipelineAsset testPipeline;
        public bool planarReflections;
        [Range(0.1f, 0.75f)] public float reflectionScale = 0.4f;
        [TextArea] public string costNotes;
    }
}
