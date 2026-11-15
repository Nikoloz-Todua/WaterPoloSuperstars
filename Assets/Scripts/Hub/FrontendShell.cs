using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Presentation only. HubView owns the single navigation history.</summary>
    public sealed class FrontendShell : MonoBehaviour
    {
        public HubView hub;
        public FrontendArt art;
        public GameObject backdrop;
        public UnityEngine.UI.Image background,tint;
        public UnityEngine.UI.AspectRatioFitter backgroundFit;
        public UnityEngine.UI.Button close;
        public TMP_Text title,profile,coins,diamonds;
        void Awake()=>close.onClick.AddListener(hub.Back);
        public void Present(string page,LocalProfile p)
        {
            bool visible=page!="Lobby";gameObject.SetActive(visible);backdrop.SetActive(visible);if(!visible)return;
            title.text=page=="Play"?"GAME MODE":page=="Mode Details"?"MODE DETAILS":page=="Tactics"||page=="Collection"||page=="Details"?"TEAM":page=="Trophies"?"RANKING":page=="Season Pass"?"BATTLE PASS":page.ToUpperInvariant();
            profile.text=p.displayName+"\nLEVEL "+p.level;coins.text=p.coins.ToString("N0");diamonds.text=p.gems.ToString("N0");
            background.sprite=page=="Season Pass"?art.battlePassBackground:page=="Play"||page=="Mode Details"?art.gameModesBackground:hub.home.art.background;
            backgroundFit.aspectRatio=background.sprite.rect.width/background.sprite.rect.height;
            tint.color=new Color(.008f,.02f,.06f,page=="Play"?.24f:page=="Season Pass"?.30f:.80f);
        }
    }
}
