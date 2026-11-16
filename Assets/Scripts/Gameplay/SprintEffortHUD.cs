using UnityEngine;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo
{
    /// <summary>Presentation of PeriodSprintOff's actual capped acceleration effort; owns no input or gameplay.</summary>
    [DisallowMultipleComponent]
    public sealed class SprintEffortHUD : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private MatchMenuController menus;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Graphic fillGraphic,highlight;
        private int lastStroke;
        private float displayed,pulse;
        public float DisplayedEffort=>displayed;
        public float Alpha=>group.alpha;
        private static readonly Color Cyan=new Color(.16f,.8f,1f),Gold=new Color(1f,.79f,.31f);
        private void OnEnable(){match.NewMatch+=Reset;Reset();}
        private void OnDisable(){match.NewMatch-=Reset;group.alpha=0f;}
        private void Reset(){displayed=pulse=0f;lastStroke=0;group.alpha=0f;SetFill();}
        private void SetFill(){var max=fill.anchorMax;if(Mathf.Abs(max.x-displayed)<.0001f)return;max.x=displayed;fill.anchorMax=max;}
        private void Update()
        {
            bool visible=match.IsSprintRace&&!match.IsPaused&&!menus.Visible;
            float actual=visible?match.SprintOff.HumanStrokePower:0f;
            int strokes=match.SprintOff.HumanStrokeCount;
            if(visible&&strokes!=lastStroke)pulse=1f;
            lastStroke=strokes;pulse=Mathf.MoveTowards(pulse,0f,Time.unscaledDeltaTime*5f);
            displayed=Mathf.MoveTowards(displayed,actual,Time.unscaledDeltaTime*6f);SetFill();
            fillGraphic.color=Color.Lerp(Cyan,Gold,Mathf.InverseLerp(.85f,1f,displayed));
            var c=highlight.color;c.a=pulse*.5f;highlight.color=c;
            if(match.IsPaused||menus.Visible)group.alpha=0f;
            else group.alpha=Mathf.MoveTowards(group.alpha,visible?1f:0f,Time.unscaledDeltaTime/(visible?.12f:.25f));
        }
    }
}
