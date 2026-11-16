using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Scene-local venue budgets only. Does not own cameras, gameplay or persistent preferences.</summary>
    [DefaultExecutionOrder(-120), DisallowMultipleComponent]
    public sealed class ChampionshipPresentation : MonoBehaviour
    {
        public GamePreferences preferences;
        public Volume volume;
        public VolumeProfile[] profiles;
        public UniversalAdditionalCameraData viewCamera;
        public Light[] poolLights;
        public Light[] technicalLights;
        public Light[] accentLights;
        public GameObject mediumAtmosphere,highAtmosphere;
        public bool refinedLighting;
        void OnEnable(){if(preferences!=null)preferences.Changed+=Apply;}
        void Start()=>Apply();
        void OnDisable(){if(preferences!=null)preferences.Changed-=Apply;}
        void Apply()
        {
            int level=preferences==null?2:(int)preferences.Current.graphics;
            // Legacy generic preferences disable all post on Medium/Low. This venue retains the cheap
            // neutral grade on every level; its profile disables bloom on Low instead.
            if(viewCamera!=null)viewCamera.renderPostProcessing=true;
            if(refinedLighting&&viewCamera!=null)viewCamera.renderShadows=level>=1;
            if(volume!=null&&profiles!=null&&level<profiles.Length)volume.sharedProfile=profiles[level];
            for(int i=0;i<poolLights.Length;i++)if(poolLights[i]!=null)poolLights[i].enabled=level>0||i==0||i==3;
            if(technicalLights!=null)foreach(var light in technicalLights)if(light!=null)light.enabled=level>=1;
            if(accentLights!=null)for(int i=0;i<accentLights.Length;i++)if(accentLights[i]!=null)accentLights[i].enabled=level>=1&&(i<2||level>=2);
            if(mediumAtmosphere!=null)mediumAtmosphere.SetActive(level>=1);
            if(highAtmosphere!=null)highAtmosphere.SetActive(level>=2);
        }
    }
}
