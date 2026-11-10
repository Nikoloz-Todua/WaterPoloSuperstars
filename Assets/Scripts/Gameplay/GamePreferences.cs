using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ToduStudios.WaterPolo
{
    public enum MatchGraphicsQuality { Low, Medium, High }
    /// <summary>Local user preferences; applies presentation controls without changing authored tuning or match rules.</summary>
    [DisallowMultipleComponent]
    public sealed class GamePreferences : MonoBehaviour
    {
        public const string StorageKey="ToduStudios.WaterPolo.UI.v1";
        [Serializable] public sealed class Values
        {
            public int version=1;
            public float master=1f,sfx=1f,crowd=1f,zoom=1f,smoothing=1f;
            public MatchGraphicsQuality graphics=MatchGraphicsQuality.High;
        }
        [SerializeField] private GameplayCamera gameplayCamera;
        [SerializeField] private MatchAtmosphere atmosphere;
        [SerializeField] private WaterInteractionEffects effects;
        [SerializeField] private UniversalAdditionalCameraData cameraData;
        private Values values=new Values();
        private bool dirty,originalShadows,originalPost;
        public Values Current=>JsonUtility.FromJson<Values>(JsonUtility.ToJson(values));
        public bool SupportsCrowd=>atmosphere!=null && atmosphere.SupportsCrowdAudio;
        public event Action Changed;
        private void Awake()
        {
            if(cameraData!=null){originalShadows=cameraData.renderShadows;originalPost=cameraData.renderPostProcessing;}
            Reload();
        }
        private static float Valid(float value,float fallback,float min,float max)=>float.IsNaN(value) || float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
        public void Reload()
        {
            values=new Values();
            try
            {
                string json=PlayerPrefs.GetString(StorageKey,"");
                if(!string.IsNullOrEmpty(json)){var loaded=JsonUtility.FromJson<Values>(json);if(loaded!=null && loaded.version==1)values=loaded;}
            }
            catch(ArgumentException){values=new Values();}
            Normalize();dirty=false;Apply();Changed?.Invoke();
        }
        private void Normalize()
        {
            values.master=Valid(values.master,1f,0f,1f);values.sfx=Valid(values.sfx,1f,0f,1f);values.crowd=Valid(values.crowd,1f,0f,1f);
            values.zoom=Valid(values.zoom,1f,0.85f,1.25f);values.smoothing=Valid(values.smoothing,1f,0.65f,1.8f);
            if(!Enum.IsDefined(typeof(MatchGraphicsQuality),values.graphics))values.graphics=MatchGraphicsQuality.High;
        }
        public void SetMaster(float v){values.master=v;Commit();}
        public void SetSfx(float v){values.sfx=v;Commit();}
        public void SetCrowd(float v){values.crowd=v;Commit();}
        public void SetZoom(float v){values.zoom=v;Commit();}
        public void SetSmoothing(float v){values.smoothing=v;Commit();}
        public void SetGraphics(int v){values.graphics=(MatchGraphicsQuality)v;Commit();}
        private void Commit(){Normalize();dirty=true;Apply();Changed?.Invoke();}
        private void Apply()
        {
            AudioListener.volume=values.master;
            if(atmosphere!=null)atmosphere.SetVolumes(values.sfx,values.crowd);
            if(gameplayCamera!=null)gameplayCamera.SetUserPreferences(values.zoom,values.smoothing);
            if(effects!=null)effects.Quality=values.graphics==MatchGraphicsQuality.Low?WaterEffectsQuality.Off:
                values.graphics==MatchGraphicsQuality.Medium?WaterEffectsQuality.Low:WaterEffectsQuality.Standard;
            if(cameraData!=null)
            {cameraData.renderShadows=values.graphics==MatchGraphicsQuality.High && originalShadows;cameraData.renderPostProcessing=values.graphics==MatchGraphicsQuality.High && originalPost;}
        }
        public void Save(){if(!dirty)return;PlayerPrefs.SetString(StorageKey,JsonUtility.ToJson(values));PlayerPrefs.Save();dirty=false;}
        private void OnApplicationPause(bool paused){if(paused)Save();}
        private void OnDisable()=>Save();
    }
}
