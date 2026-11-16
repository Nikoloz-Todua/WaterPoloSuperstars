using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Existing quality preference budgets visual detail and a shared low-frequency ribbon phase.</summary>
    [DefaultExecutionOrder(-100),DisallowMultipleComponent]
    public sealed class ChampionshipVenuePresentation : MonoBehaviour
    {
        public ChampionshipVenueLayout layout;
        public GamePreferences preferences;
        public GameObject lowUpperSeats,fullUpperSeats,mediumDetails,highDetails;
        public Renderer[] ribbonPanels=new Renderer[0];
        MaterialPropertyBlock block;
        float nextUpdate;
        int quality=-1;
        static readonly int Phase=Shader.PropertyToID("_Phase"),Artwork=Shader.PropertyToID("_BaseMap"),Weight=Shader.PropertyToID("_ArtworkWeight"),Base=Shader.PropertyToID("_BaseColor"),Accent=Shader.PropertyToID("_AccentColor");
        public float RibbonPhase {get;private set;}
        void OnEnable(){block=new MaterialPropertyBlock();if(preferences!=null)preferences.Changed+=Apply;Apply();}
        void OnDisable(){if(preferences!=null)preferences.Changed-=Apply;}
        void Apply()
        {
            quality=preferences!=null?(int)preferences.Current.graphics:2;
            if(lowUpperSeats!=null)lowUpperSeats.SetActive(quality==0);
            if(fullUpperSeats!=null)fullUpperSeats.SetActive(quality>=1);
            if(mediumDetails!=null)mediumDetails.SetActive(quality>=1);
            if(highDetails!=null)highDetails.SetActive(quality>=2);
            nextUpdate=0;RefreshPanels();
        }
        void Update()
        {
            if(quality<=0||layout==null||Time.unscaledTime<nextUpdate)return;
            nextUpdate=Time.unscaledTime+1f/Mathf.Max(2,layout.ribbonUpdateRate);
            RibbonPhase=Mathf.Repeat(Time.unscaledTime*layout.ribbonSpeed,100f);RefreshPanels();
        }
        void RefreshPanels()
        {
            if(block==null||layout==null)return;
            block.Clear();block.SetFloat(Phase,quality<=0?0:RibbonPhase);block.SetColor(Base,layout.ribbonBase);block.SetColor(Accent,layout.ribbonAccent);
            block.SetFloat(Weight,layout.ribbonArtwork!=null?layout.artworkWeight:0);if(layout.ribbonArtwork!=null)block.SetTexture(Artwork,layout.ribbonArtwork);
            for(int i=0;i<ribbonPanels.Length;i++)if(ribbonPanels[i]!=null)ribbonPanels[i].SetPropertyBlock(block);
        }
    }
}
