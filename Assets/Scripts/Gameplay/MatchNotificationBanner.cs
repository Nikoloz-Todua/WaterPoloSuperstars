using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>One presentation slot for authoritative events. No rule decisions, clocks or queued stale events.</summary>
    [DisallowMultipleComponent]
    public sealed class MatchNotificationBanner : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private MatchMenuController menus;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text title, context;
        [SerializeField] private UnityEngine.UI.Graphic accent;
        private MatchPhase phase;
        private int blue,white,quarter,priority;
        private float until;
        private bool centre;
        private static readonly Color Cyan=new Color(0.22f,0.85f,1f),Gold=new Color(1f,0.78f,0.3f);
        public string Title=>title.text;
        private void OnEnable()
        {
            blue=match.BlueScore;white=match.WhiteScore;quarter=match.Clock.Quarter;phase=match.Phase;
            match.Changed+=Changed;match.NewMatch+=Clear;match.Rules.Decided+=Decision;
        }
        private void OnDisable(){match.Changed-=Changed;match.NewMatch-=Clear;match.Rules.Decided-=Decision;group.alpha=0f;}
        private void Clear(){until=0f;priority=0;centre=false;blue=white=0;quarter=1;group.alpha=0f;title.text=context.text="";}
        private void Show(string heading,string detail,Color color,int importance,float duration,bool force=false)
        {
            if(!force && match.PresentationTime<until && importance<priority)return;
            title.text=heading;context.text=detail;accent.color=title.color=color;
            priority=importance;until=match.PresentationTime+duration;
        }
        private void Changed()
        {
            var next=match.Phase;
            if(match.BlueScore>blue || match.WhiteScore>white)
            {
                bool scoredBlue=match.BlueScore>blue;
                Show("GOAL",$"{(scoredBlue?"BLUE":"WHITE")}  •  {match.BlueScore} : {match.WhiteScore}",scoredBlue?Cyan:Gold,100,2.5f,true);
            }
            else if(next!=phase)
            {
                if(next==MatchPhase.SprintSetup)Show($"QUARTER {match.Clock.Quarter}","Preparing sprint  •  Hold your goal line",Cyan,75,30f,true);
                else if(next==MatchPhase.SprintRace)Show("SPRINT FOR THE BALL","Tap / hold SPRINT  •  Steer with movement",Gold,75,30f,true);
                else if(match.IsCentreRestart)
                {
                    centre=true;bool human=match.CentreTaker!=null && match.CentreTaker.Team==PrototypeTeam.Blue;
                    Show("CENTER RESTART",next==MatchPhase.CentreRestartSetup?"Wait for the referee":human?"BLUE  •  Tap PASS to restart":"WHITE  •  Center pass",Cyan,75,3f,true);
                }
                else if(next==MatchPhase.TurnoverRestart)Show("SHOT CLOCK VIOLATION","Possession awarded by the referee",Gold,85,2.8f,true);
                else if(next==MatchPhase.QuarterBreak)Show("PERIOD END",$"Quarter {quarter} complete",Gold,100,3f,true);
                else if(next==MatchPhase.HalfTime)Show("HALFTIME","Teams change ends when you continue",Gold,100,3f,true);
                else if(next==MatchPhase.FullTime)Show("FULL TIME",$"BLUE {match.BlueScore} : {match.WhiteScore} WHITE",Gold,100,4f,true);
                else if(next==MatchPhase.Playing)
                {
                    if(phase==MatchPhase.SprintRace)Show("PLAY ON",match.AwaitingOpeningPossession?"Loose ball  •  Swim for possession":"Possession won  •  Pass, shoot or defend",Cyan,35,.9f,true);
                    if(centre){until=match.PresentationTime+0.2f;centre=false;}
                    if(match.Clock.Quarter!=quarter || phase==MatchPhase.PreMatch)
                        Show($"QUARTER {match.Clock.Quarter}","Neutral ball  •  Swim for possession",Cyan,30,1.8f,true);
                }
            }
            blue=match.BlueScore;white=match.WhiteScore;quarter=match.Clock.Quarter;phase=next;
        }
        private void Decision(RefereeEvent e)
        {
            string player=e.Player!=null?$"{e.Player.Team.ToString().ToUpperInvariant()} #{e.Player.CapNumber}":"";
            switch(e.Decision)
            {
                case RefereeDecision.Penalty:Show("PENALTY",$"{e.AwardedTeam.ToString().ToUpperInvariant()}  •  Aim with stick or arrows  ?  Hold / release SHOOT",Gold,90,3.8f);break;
                case RefereeDecision.Exclusion:Show("EXCLUSION",player+(e.Player.PermanentlyRemoved?"  •  Out for the match":$"  •  {Mathf.CeilToInt(e.Player.ExclusionRemaining)} live seconds"),Gold,80,3.4f);break;
                case RefereeDecision.Ordinary:
                case RefereeDecision.FreeThrow:Show("FREE THROW",$"{e.AwardedTeam.ToString().ToUpperInvariant()}  •  PASS or LOB after the whistle",Cyan,60,2.8f);break;
                case RefereeDecision.Reentry:Show("RE-ENTRY",player+"  •  Back in play",Cyan,20,1.7f);break;
            }
        }
        private void Update()
        {
            if(menus.Visible || match.GoalPresentationActive){group.alpha=0f;return;}
            bool show=!menus.Visible && (match.PresentationTime<until || centre && match.IsCentreRestart);
            group.alpha=Mathf.MoveTowards(group.alpha,show?1f:0f,Time.unscaledDeltaTime/(show?0.12f:0.2f));
        }
    }
}
