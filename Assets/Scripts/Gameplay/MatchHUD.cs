using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Observes match/selection state and reuses existing score and shot-power UI.</summary>
    [DisallowMultipleComponent]
    public sealed class MatchHUD : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private PrototypeTeamTactics humanTeam;
        [SerializeField] private TMP_Text clockLabel, shotClockLabel, strategyLabel, panelTitle;
        [SerializeField] private GameObject panel, continueButton, restartButton, returnButton;
        private int displayedTime=-1,displayedShot=-1,displayedQuarter=-1;
        private DefenseStrategy? displayedDefense;
        private MatchPhase? displayedPhase;
        private void Update()
        {
            int seconds=Mathf.CeilToInt(match.Clock.DisplayRemaining);
            if(seconds!=displayedTime || match.Clock.Quarter!=displayedQuarter)
            {displayedTime=seconds;displayedQuarter=match.Clock.Quarter;clockLabel.text=$"Q{displayedQuarter} — {seconds/60:0}:{seconds%60:00}";}
            int shot=match.ShotClock!=null?Mathf.CeilToInt(match.ShotClock.Remaining):28;
            if(shot!=displayedShot){displayedShot=shot;shotClockLabel.text=$"SHOT {shot:00}";shotClockLabel.color=shot<=5?new Color(1f,0.3f,0.12f):Color.white;}
            if(displayedDefense!=humanTeam.DefenseMode){displayedDefense=humanTeam.DefenseMode;strategyLabel.text=$"DEFENSE: {humanTeam.DefenseMode.ToString().ToUpperInvariant()} [Z]";}
            if(displayedPhase==match.Phase)return;
            displayedPhase=match.Phase;
            bool modal=match.Phase==MatchPhase.QuarterBreak || match.Phase==MatchPhase.HalfTime || match.Phase==MatchPhase.FullTime || match.Phase==MatchPhase.PreMatch;
            panel.SetActive(modal || match.Phase==MatchPhase.TurnoverRestart);
            continueButton.SetActive(match.Phase==MatchPhase.QuarterBreak || match.Phase==MatchPhase.HalfTime || match.Phase==MatchPhase.PreMatch);
            restartButton.SetActive(match.Phase==MatchPhase.FullTime);returnButton.SetActive(match.Phase==MatchPhase.FullTime);
            switch(match.Phase)
            {
                case MatchPhase.QuarterBreak:panelTitle.text=$"QUARTER {match.Clock.Quarter} COMPLETE";break;
                case MatchPhase.HalfTime:panelTitle.text=$"HALFTIME — SWITCHING ENDS\nBLUE {match.BlueScore} - {match.WhiteScore} WHITE";break;
                case MatchPhase.FullTime:panelTitle.text=$"FULL TIME — {match.Result}\nBLUE {match.BlueScore} - {match.WhiteScore} WHITE";break;
                case MatchPhase.TurnoverRestart:panelTitle.text="SHOT CLOCK — TURNOVER";break;
                case MatchPhase.PreMatch:panelTitle.text="WATER POLO PROTOTYPE";break;
            }
        }
    }
}
