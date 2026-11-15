using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    // Presentation only: no references to the ball, swimmer motor, rules or match.
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    public sealed class PoolWaterVisuals : MonoBehaviour
    {
        public Renderer waterSurface;
        public Camera viewCamera;
        public PlanarReflectionManager reflectionManager;
        public PlanarReflectionVolume reflectionVolume;
        public PoolWaterQualityProfile[] profiles;
        [Range(0, 2)] public int qualityIndex = 2;
        [Tooltip("Enable only in WaterVisualTest. Restores the previous pipeline when disabled/unloaded.")]
        public bool useIsolatedTestPipeline = true;
        public bool keyboardShortcuts = true;

        private Material originalMaterial, runtimeMaterial;
        private RenderPipelineAsset originalPipeline, appliedPipeline;
        private CameraOverrideOption originalDepth, originalColor;
        private int appliedIndex = -1;
        public Material RuntimeMaterial => runtimeMaterial;

        private void OnEnable()
        {
            if (waterSurface == null || viewCamera == null) return;
            originalMaterial = waterSurface.sharedMaterial;
            originalPipeline = QualitySettings.renderPipeline;
            var data = viewCamera.GetUniversalAdditionalCameraData();
            originalDepth = data.requiresDepthOption;
            originalColor = data.requiresColorOption;
            ApplyQuality(qualityIndex);
        }

        private void Update()
        {
            if (appliedIndex != qualityIndex) ApplyQuality(qualityIndex);
            var keys = Keyboard.current;
            if (!keyboardShortcuts || keys == null) return;
            if (keys.digit1Key.wasPressedThisFrame) ApplyQuality(0);
            if (keys.digit2Key.wasPressedThisFrame) ApplyQuality(1);
            if (keys.digit3Key.wasPressedThisFrame) ApplyQuality(2);
        }

        public void ApplyQuality(int index)
        {
            if (profiles == null || index < 0 || index >= profiles.Length || profiles[index] == null
                || profiles[index].waterMaterial == null || waterSurface == null || viewCamera == null) return;
            var profile = profiles[index];
            qualityIndex = appliedIndex = index;
            if (reflectionVolume != null) reflectionVolume.enabled = false;
            if (reflectionManager != null) reflectionManager.enabled = false;
            var old = runtimeMaterial;
            runtimeMaterial = new Material(profile.waterMaterial) { name = profile.waterMaterial.name + " (runtime)", hideFlags = HideFlags.DontSave };
            if (!profile.planarReflections || reflectionManager == null || reflectionVolume == null)
            {
                runtimeMaterial.DisableKeyword("_ENABLEPLANERREFLECTION");
                runtimeMaterial.SetFloat("_ENABLEPLANERREFLECTION", 0f);
            }
            waterSurface.sharedMaterial = runtimeMaterial;
            if (old != null) Destroy(old);
            if (useIsolatedTestPipeline && profile.testPipeline != null)
            {
                appliedPipeline = profile.testPipeline;
                QualitySettings.renderPipeline = appliedPipeline;
            }
            var data = viewCamera.GetUniversalAdditionalCameraData();
            data.requiresDepthOption = CameraOverrideOption.On;
            data.requiresColorOption = runtimeMaterial.IsKeywordEnabled("_ENABLEREFRACTION") ? CameraOverrideOption.On : CameraOverrideOption.Off;
            if (reflectionVolume != null)
            {
                reflectionVolume.renderScale = profile.reflectionScale;
                reflectionVolume.UpdateTargetMaterials();
                reflectionVolume.enabled = profile.planarReflections;
                // The upstream OnEnable sets its mask; explicitly keep the Water layer excluded.
                reflectionVolume.reflectionLayer = ~(1 << 4);
            }
            if (reflectionManager != null) reflectionManager.enabled = profile.planarReflections;
        }

        private void OnDisable()
        {
            if (reflectionVolume != null) reflectionVolume.enabled = false;
            if (reflectionManager != null) reflectionManager.enabled = false;
            if (waterSurface != null && waterSurface.sharedMaterial == runtimeMaterial) waterSurface.sharedMaterial = originalMaterial;
            if (useIsolatedTestPipeline && QualitySettings.renderPipeline == appliedPipeline) QualitySettings.renderPipeline = originalPipeline;
            if (viewCamera != null)
            {
                var data = viewCamera.GetUniversalAdditionalCameraData();
                data.requiresDepthOption = originalDepth;
                data.requiresColorOption = originalColor;
            }
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
            runtimeMaterial = null;
            appliedIndex = -1;
        }
    }
}
