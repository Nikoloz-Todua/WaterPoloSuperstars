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
        public TMP_Text primaryScore,primaryClock,primaryState;
        public SwimmerIdentity[] athletes;
        public TMP_Text[] capLabels;
        public Renderer[] exclusionDots;
        int blue=-1,white=-1,seconds=-1,shot=-1,quarter=-1;
        int[] shownExclusions;
        float goalUntil;
        string goalNotice;
        bool paused,waiting;
        MatchPhase phase;
        static readonly Color Cyan=new Color(.22f,.85f,1f),Amber=new Color(1f,.69f,.23f);
        public int ActiveExclusions {get;private set;}
        void Awake(){shownExclusions=new int[athletes==null?0:athletes.Length];for(int i=0;i<shownExclusions.Length;i++)shownExclusions[i]=int.MinValue;}
        void OnEnable(){if(match!=null){match.Changed+=Refresh;match.PauseChanged+=Paused;match.NewMatch+=ResetDisplay;if(match.Rules!=null)match.Rules.Decided+=Decision;}}
        void Start()=>Refresh();
        void OnDisable(){if(match!=null){match.Changed-=Refresh;match.PauseChanged-=Paused;match.NewMatch-=ResetDisplay;if(match.Rules!=null)match.Rules.Decided-=Decision;}}
        void Paused(bool value)=>Refresh();
        void Decision(RefereeEvent value)=>Refresh();
        void ResetDisplay(){blue=white=seconds=shot=quarter=-1;goalUntil=0;Refresh();}
        // The HUD observes the same fixed-step clock each rendered frame. Cached values keep
        // text rebuilds at displayed-second changes without a visible inter-screen polling lag.
        void Update()=>Refresh();
        public void Refresh()
        {
            if(match==null)return;
            if(blue!=match.BlueScore||white!=match.WhiteScore)
            {
                if(blue>=0&&(match.BlueScore>blue||match.WhiteScore>white)){goalNotice=match.BlueScore>blue?"GOAL / BLUE":"GOAL / WHITE";goalUntil=match.PresentationTime+1.5f;}
                blue=match.BlueScore;white=match.WhiteScore;
                string value=$"BLUE   {blue} : {white}   WHITE";
                foreach(var label in scores)if(label!=null)label.text=value;
                if(primaryScore!=null)primaryScore.SetText("{0} : {1}",(float)blue,(float)white);
            }
            RefreshExclusions();
            string status=match.IsPaused?"PAUSED":match.Phase==MatchPhase.FullTime?"FINAL":match.Phase==MatchPhase.HalfTime?"HALFTIME":match.Phase==MatchPhase.QuarterBreak?"PERIOD END":
                match.IsPeriodSprint?match.IsSprintRace?"SPRINT FOR THE BALL":"PERIOD OPENING":match.AwaitingOpeningPossession?"LOOSE BALL":match.PresentationTime<goalUntil?goalNotice:match.IsPlaying?"LIVE":"PLAY STOPPED";
            if(primaryState!=null){if(primaryState.text!=status)primaryState.text=status;Color accent=match.PresentationTime<goalUntil||ActiveExclusions>0?Amber:Cyan;if(primaryState.color!=accent)primaryState.color=accent;}
            int time=Mathf.CeilToInt(match.Clock.DisplayRemaining),possession=Mathf.CeilToInt(match.ShotClock.Remaining);
            if(time==seconds&&possession==shot&&quarter==match.Clock.Quarter&&phase==match.Phase&&paused==match.IsPaused&&waiting==match.AwaitingOpeningPossession)return;
            seconds=time;shot=possession;quarter=match.Clock.Quarter;phase=match.Phase;paused=match.IsPaused;waiting=match.AwaitingOpeningPossession;
            string state=$"Q{quarter}  {seconds/60:00}:{seconds%60:00}   |   SHOT {shot:00}"+(paused?"  /  PAUSED":phase==MatchPhase.FullTime?"  /  FINAL":"");
            foreach(var label in clocks)if(label!=null)label.text=state;
            if(primaryClock!=null)primaryClock.text=$"Q{quarter}     {seconds/60:00}:{seconds%60:00}     SHOT {shot:00}";
        }
        private void RefreshExclusions()
        {
            ActiveExclusions=0;if(athletes==null||shownExclusions==null)return;
            for(int i=0;i<athletes.Length;i++)
            {
                var p=athletes[i];if(p==null)continue;int state=p.PermanentlyRemoved?-1:Mathf.CeilToInt(p.ExclusionRemaining);if(state>0)ActiveExclusions++;
                int displayKey = p.LastDisqualifiedCap * 10000 + p.CapNumber * 100 + state;
                if(shownExclusions[i]==displayKey)continue;shownExclusions[i]=displayKey;
                if(i<capLabels.Length&&capLabels[i]!=null){capLabels[i].text=state<0?$"#{p.CapNumber}\n<size=65%>OUT</size>":state>0?$"#{p.CapNumber}\n<size=65%>{state}s</size>":p.LastDisqualifiedCap>0?$"#{p.CapNumber}\n<size=65%>#{p.LastDisqualifiedCap} OUT</size>":$"#{p.CapNumber}";capLabels[i].color=state<0?new Color(.72f,.5f,.5f):state>0?Amber:p.Team==PrototypeTeam.Blue?Cyan:Color.white;}
                if(i<exclusionDots.Length&&exclusionDots[i]!=null)exclusionDots[i].enabled=state>0;
            }
        }
    }
}
