using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Pointer commands enter existing gameplay APIs. SwimmerInput remains the only human movement writer.</summary>
    [DefaultExecutionOrder(-15),DisallowMultipleComponent]
    public sealed class MobileMatchControls : MonoBehaviour
    {
        [SerializeField,HideInInspector] private int interfaceVersion;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeTeamTactics humanTeam;
        [SerializeField] private MatchMenuController menus;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private VirtualSwimJoystick joystick;
        [SerializeField] private GameplayActionButton[] buttons;
        [SerializeField] private TMP_Text restartInstruction;
        private SwimmerIdentity observed;
        private ShotCharge charge,heldCharge;
        private SwimmerPassing passing;
        private LobPassing lob;
        private SwimmerSteal steal;
        private SwimmerStamina stamina;
        public ShotType ShotMode {get;private set;}
        public bool SprintHeld {get;private set;}
        private bool Usable=>!match.IsPaused && !menus.Visible && !selection.InputLocked && observed!=null && observed==selection.ActivePlayer && observed.EligibleForPlay;
        public bool CanMove=>Usable && match.IsPlaying;
        public Vector2 MovementFor(SwimmerMotor motor)=>CanMove && observed.Motor==motor?joystick.Value:Vector2.zero;
        public bool SprintFor(SwimmerMotor motor)=>CanMove && observed.Motor==motor && SprintHeld;
        private void OnEnable(){match.PauseChanged+=Paused;match.Changed+=Phase;}
        private void OnDisable(){match.PauseChanged-=Paused;match.Changed-=Phase;ResetInputs();}
        private void Paused(bool paused){if(paused)ResetInputs();}
        private void Phase(){ResetInputs();}
        private void OnApplicationFocus(bool focus){if(!focus)ResetInputs();}
        private void ResetInputs()
        {
            SprintHeld=false;joystick?.ResetInput();
            if(heldCharge!=null)heldCharge.CancelCharge();heldCharge=null;
            if(buttons!=null)foreach(var b in buttons)if(b!=null)b.CancelInput();
        }
        private void Update()
        {
            if(observed!=selection.ActivePlayer)
            {
                ResetInputs();observed=selection.ActivePlayer;
                if(observed!=null){charge=observed.GetComponent<ShotCharge>();passing=observed.GetComponent<SwimmerPassing>();lob=observed.GetComponent<LobPassing>();steal=observed.GetComponent<SwimmerSteal>();stamina=observed.GetComponent<SwimmerStamina>();}
            }
            bool visible=!menus.Visible && !match.IsPaused && match.Phase!=MatchPhase.PreMatch && match.Phase!=MatchPhase.FullTime;
            if(gameplayRoot.activeSelf!=visible)gameplayRoot.SetActive(visible);
            if(!CanMove)joystick.ResetInput();
            bool centre=match.IsCentreRestart;
            bool hasBall=observed!=null && observed.Interaction.HasBall;
            foreach(var b in buttons)
            {
                bool show=b.Action==MobileMatchAction.Shoot?hasBall:
                    b.Action==MobileMatchAction.Steal?!hasBall:
                    b.Action==MobileMatchAction.Pass?hasBall:
                    b.Action==MobileMatchAction.Lob || b.Action==MobileMatchAction.ShotMode?hasBall && !centre:
                    b.Action==MobileMatchAction.Collect?ball.Owner==null && match.IsPlaying:true;
                if(b.gameObject.activeSelf!=show)b.gameObject.SetActive(show);
                b.SetAvailable(CanUse(b.Action));
                if(b.Action==MobileMatchAction.ShotMode)b.SetCaption(ShotMode==ShotType.Skip?"SKIP":ShotMode==ShotType.High?"HIGH":"NORMAL");
                if(b.Action==MobileMatchAction.Strategy)b.SetCaption(humanTeam.DefenseMode==DefenseStrategy.Press?"DEFENSE: PRESS":"DEFENSE: ZONE");
            }
            string instruction=centre?(match.Phase==MatchPhase.CentreRestartSetup?"CENTRE RESTART  |  WAIT FOR WHISTLE":
                match.CentreTaker.Team==selection.HumanTeam?"PASS TO RESTART  |  B OR PASS":"WHITE CENTRE PASS"):
                match.Phase==MatchPhase.FreeThrowReady?"FREE THROW  |  B / PASS OR L / LOB":
                match.Phase==MatchPhase.PenaltyReady?"PENALTY  |  HOLD / RELEASE SHOOT":"";
            if(restartInstruction.text!=instruction)restartInstruction.text=instruction;
        }
        public bool CanUse(MobileMatchAction action)
        {
            if(!Usable)return false;
            bool hasBall=observed.Interaction.HasBall;
            if(action==MobileMatchAction.Shoot || action==MobileMatchAction.ShotMode)return hasBall && match.Rules.CanRelease(observed.Interaction,BallReleaseKind.Shot);
            if(action==MobileMatchAction.Pass)return hasBall && match.Rules.CanRelease(observed.Interaction,BallReleaseKind.Pass);
            if(action==MobileMatchAction.Lob)return hasBall && match.Rules.CanRelease(observed.Interaction,BallReleaseKind.Lob);
            if(!match.IsPlaying)return false;
            if(action==MobileMatchAction.Sprint)return stamina!=null && stamina.SprintAllowed;
            if(action==MobileMatchAction.Steal)return !hasBall && ball.Owner!=null && ball.Owner.GetComponent<SwimmerIdentity>().Team!=selection.HumanTeam;
            if(action==MobileMatchAction.Collect)return ball.CanCollect;
            return true;
        }
        public bool Begin(MobileMatchAction action)
        {
            if(!CanUse(action))return false;
            switch(action)
            {
                case MobileMatchAction.Shoot:if(charge.BeginCharge(ShotMode,this)){heldCharge=charge;return true;}return false;
                case MobileMatchAction.Sprint:SprintHeld=true;return true;
                case MobileMatchAction.Pass:charge.CancelCharge();return passing.TryPass();
                case MobileMatchAction.Lob:charge.CancelCharge();return lob.TryPass();
                case MobileMatchAction.Switch:selection.SelectNext();return true;
                case MobileMatchAction.Steal:steal.TrySteal();return true;
                case MobileMatchAction.Collect:return observed.Interaction.TryCollect();
                case MobileMatchAction.Strategy:humanTeam.ToggleDefense();return true;
                case MobileMatchAction.ShotMode:ShotMode=ShotMode==ShotType.Normal?ShotType.Skip:ShotMode==ShotType.Skip?ShotType.High:ShotType.Normal;return true;
                default:return false;
            }
        }
        public void End(MobileMatchAction action,bool cancel)
        {
            if(action==MobileMatchAction.Sprint)SprintHeld=false;
            if(action!=MobileMatchAction.Shoot || heldCharge==null)return;
            var pending=heldCharge;heldCharge=null;
            if(cancel || !CanUse(action))pending.CancelCharge();else pending.ReleaseCharge(this);
        }
    }
}
