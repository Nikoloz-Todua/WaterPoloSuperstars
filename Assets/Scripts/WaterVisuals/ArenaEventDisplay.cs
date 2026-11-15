using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>World-space venue displays observe the match. Never owns scores, time, phases or input.</summary>
    [DisallowMultipleComponent]
    public sealed class ArenaEventDisplay : MonoBehaviour
    {
        public PrototypeMatch match;
        public TMP_Text[] scores,clocks;
        float nextUpdate;
        int blue=-1,white=-1,seconds=-1,shot=-1,quarter=-1;
        MatchPhase phase;
        void OnEnable(){if(match!=null)match.Changed+=Refresh;}
        void Start()=>Refresh();
        void OnDisable(){if(match!=null)match.Changed-=Refresh;}
        void Update(){if(Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+.15f;Refresh();}
        public void Refresh()
        {
            if(match==null)return;
            if(blue!=match.BlueScore||white!=match.WhiteScore)
            {
                blue=match.BlueScore;white=match.WhiteScore;
                string value=$"BLUE   {blue} : {white}   WHITE";
                foreach(var label in scores)if(label!=null)label.text=value;
            }
            int time=Mathf.CeilToInt(match.Clock.DisplayRemaining),possession=Mathf.CeilToInt(match.ShotClock.Remaining);
            if(time==seconds&&possession==shot&&quarter==match.Clock.Quarter&&phase==match.Phase)return;
            seconds=time;shot=possession;quarter=match.Clock.Quarter;phase=match.Phase;
            string state=phase==MatchPhase.FullTime?"FULL TIME":phase==MatchPhase.HalfTime?"HALF TIME":phase==MatchPhase.QuarterBreak?"INTERVAL":$"Q{quarter}  {seconds/60:0}:{seconds%60:00}   |   SHOT {shot:00}";
            foreach(var label in clocks)if(label!=null)label.text=state;
        }
    }
}
