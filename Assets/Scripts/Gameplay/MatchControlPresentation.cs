using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Applies local thumb-control preferences and pulses the existing selected ring. Never selects a player.</summary>
    [DisallowMultipleComponent]
    public sealed class MatchControlPresentation : MonoBehaviour
    {
        [SerializeField] private GamePreferences preferences;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private MatchMenuController menus;
        [SerializeField] private CanvasGroup leftControls,rightControls;
        [SerializeField] private SwimmerIdentity[] athletes;
        [SerializeField] private Transform[] rings;
        [SerializeField] private GameObject[] gameplayInformation;
        private Vector3[] scales;
        private int active=-1;
        private float switchedAt;
        private void Awake(){scales=new Vector3[rings.Length];for(int i=0;i<rings.Length;i++)if(rings[i]!=null)scales[i]=rings[i].localScale;}
        private void OnEnable(){preferences.Changed+=Apply;Apply();}
        private void OnDisable(){preferences.Changed-=Apply;Restore();}
        private void Apply()
        {
            var value=preferences.Current;float size=value.controlSize==0?0.85f:value.controlSize==2?1.12f:1f;
            leftControls.transform.localScale=rightControls.transform.localScale=Vector3.one*size;
            leftControls.alpha=rightControls.alpha=value.controlOpacity;
        }
        private void Restore(){if(active>=0 && scales!=null && rings[active]!=null)rings[active].localScale=scales[active];}
        private void LateUpdate()
        {
            bool visible=!menus.Visible && match.Phase!=MatchPhase.PreMatch && match.Phase!=MatchPhase.FullTime;
            foreach(var info in gameplayInformation)if(info.activeSelf!=visible)info.SetActive(visible);
            if(active<0 || athletes[active]!=selection.ActivePlayer)
            {
                Restore();active=-1;for(int i=0;i<athletes.Length;i++)if(athletes[i]==selection.ActivePlayer){active=i;break;}
                switchedAt=match.PresentationTime;
            }
            if(active<0 || rings[active]==null)return;
            float t=Mathf.Clamp01((match.PresentationTime-switchedAt)/0.35f);
            rings[active].localScale=scales[active]*(1f+0.22f*Mathf.Sin(t*Mathf.PI));
        }
    }
}
