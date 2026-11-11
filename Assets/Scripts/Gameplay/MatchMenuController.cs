using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo
{
    public enum MatchMenuPage { None, Pause, Settings, RestartConfirmation, ExitConfirmation, Landing, Transition }
    /// <summary>UI navigation only. PrototypeMatch owns suspension, resets and all competitive phases.</summary>
    [DefaultExecutionOrder(-110), DisallowMultipleComponent]
    public sealed class MatchMenuController : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private GamePreferences preferences;
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private GameObject pausePanel,settingsPanel,confirmationPanel,landingPanel,transitionPanel,pauseButton;
        [SerializeField] private TMP_Text confirmationTitle,confirmationBody,transitionTitle,transitionBody;
        [SerializeField] private GameObject continueButton,resultRestartButton;
        private InputAction back;
        private MatchMenuPage returnPage;
        public MatchMenuPage Page {get;private set;}
        public bool Visible=>Page!=MatchMenuPage.None;
        private void Awake()
        {
            back=new InputAction("PauseOrBack",InputActionType.Button,"<Keyboard>/escape");
            back.AddBinding("<Gamepad>/start");
        }
        private void OnEnable(){back.Enable();match.Changed+=MatchChanged;match.PauseChanged+=Paused;}
        private void Start()=>MatchChanged();
        private void OnDisable(){back.Disable();match.Changed-=MatchChanged;match.PauseChanged-=Paused;preferences.Save();match.SetPaused(false);}
        private void OnDestroy()=>back?.Dispose();
        private void OnApplicationPause(bool paused){if(paused && match.Phase!=MatchPhase.PreMatch && match.Phase!=MatchPhase.FullTime)OpenPause();}
        private void OnApplicationFocus(bool focused){if(!focused && Application.platform!=RuntimePlatform.WindowsEditor && Application.platform!=RuntimePlatform.OSXEditor)OnApplicationPause(true);}
        private void Update()
        {
            if(back.WasPressedThisFrame())Back();
            float target=Visible?1f:0f;
            overlay.alpha=Mathf.MoveTowards(overlay.alpha,target,Time.unscaledDeltaTime/0.12f);
            // Raycasts block immediately, before the fade; hidden panels never accept input.
        }
        private void Paused(bool paused){if(paused)Show(MatchMenuPage.Pause);else MatchChanged();}
        private void MatchChanged()
        {
            if(match.IsPaused){if(Page==MatchMenuPage.None)Show(MatchMenuPage.Pause);return;}
            if(match.Phase==MatchPhase.PreMatch)Show(MatchMenuPage.Landing);
            else if(match.Phase==MatchPhase.QuarterBreak || match.Phase==MatchPhase.HalfTime || match.Phase==MatchPhase.FullTime)
            {
                transitionTitle.text=match.Phase==MatchPhase.FullTime?match.Result=="WIN"?"VICTORY":match.Result=="LOSE"?"DEFEAT":"DRAW":
                    match.Phase==MatchPhase.HalfTime?"HALFTIME":$"QUARTER {match.Clock.Quarter} COMPLETE";
                transitionBody.text=$"BLUE  {match.BlueScore} : {match.WhiteScore}  WHITE\n"+
                    (match.Phase==MatchPhase.FullTime?"FULL TIME  |  THANK YOU FOR PLAYING":match.Phase==MatchPhase.HalfTime?"Teams change ends when you continue.":$"Next up: Quarter {match.Clock.Quarter+1}");
                bool hub=Hub.HubMatchBridge.Active;
                continueButton.SetActive(match.Phase!=MatchPhase.FullTime || hub);resultRestartButton.SetActive(match.Phase==MatchPhase.FullTime && !hub);
                var nextLabel=continueButton.GetComponentInChildren<TMP_Text>();
                if(nextLabel!=null)nextLabel.text=hub && match.Phase==MatchPhase.FullTime?"REWARDS & LOBBY":"CONTINUE MATCH";
                Show(MatchMenuPage.Transition);
            }
            else Show(MatchMenuPage.None);
        }
        private void Show(MatchMenuPage page)
        {
            Page=page;pausePanel.SetActive(page==MatchMenuPage.Pause);settingsPanel.SetActive(page==MatchMenuPage.Settings);
            confirmationPanel.SetActive(page==MatchMenuPage.RestartConfirmation || page==MatchMenuPage.ExitConfirmation);
            landingPanel.SetActive(page==MatchMenuPage.Landing);transitionPanel.SetActive(page==MatchMenuPage.Transition);
            overlay.blocksRaycasts=overlay.interactable=Visible;
            pauseButton.SetActive(page==MatchMenuPage.None && match.Phase!=MatchPhase.PreMatch && match.Phase!=MatchPhase.FullTime);
            // Avoid a focused Resume button consuming the same Space used to shoot after resuming.
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        }
        public void OpenPause(){if(Page==MatchMenuPage.Settings || Page==MatchMenuPage.RestartConfirmation || Page==MatchMenuPage.ExitConfirmation)return;match.SetPaused(true);}
        public void Resume(){preferences.Save();match.SetPaused(false);}
        public void OpenSettings(){returnPage=Page;Show(MatchMenuPage.Settings);}
        public void AskRestart(){returnPage=Page;confirmationTitle.text="RESTART MATCH?";confirmationBody.text="Score, clocks and player states will reset.\nYour settings will be kept.";Show(MatchMenuPage.RestartConfirmation);}
        public void AskExit(){returnPage=Page;confirmationTitle.text="EXIT MATCH?";confirmationBody.text=Hub.HubMatchBridge.Active?"Return to the lobby.\nAbandoned matches earn no rewards.":"Return to the prototype menu.\nCurrent match progress will be lost.";Show(MatchMenuPage.ExitConfirmation);}
        public void Cancel(){preferences.Save();Show(returnPage);}
        public void Confirm()
        {
            var request=Page;preferences.Save();
            if(request==MatchMenuPage.RestartConfirmation)match.RestartMatch();
            else if(request==MatchMenuPage.ExitConfirmation)
            {
                if(Hub.HubMatchBridge.Active){if(!SaveHubResult())return;match.SetPaused(false);Hub.HubApplication.Instance.ReturnToLobby(true);}
                else match.ReturnToPrototype();
            }
        }
        public void StartMatch(){preferences.Save();match.RestartMatch();}
        private bool SaveHubResult()
        {
            if(match.Phase!=MatchPhase.FullTime)return true;
            var bridge=match.GetComponent<Hub.HubMatchBridge>();
            if(bridge!=null && bridge.TrySaveResult())return true;
            transitionBody.text="Rewards could not be saved. Restore storage access and retry REWARDS & LOBBY.";Show(MatchMenuPage.Transition);return false;
        }
        public void Continue(){preferences.Save();if(Hub.HubMatchBridge.Active && match.Phase==MatchPhase.FullTime){if(SaveHubResult())Hub.HubApplication.Instance.ReturnToLobby(false);}else match.ContinueMatch();}
        public void Back()
        {
            if(Page==MatchMenuPage.Settings || Page==MatchMenuPage.RestartConfirmation || Page==MatchMenuPage.ExitConfirmation)Cancel();
            else if(match.IsPaused)Resume();
            else if(Page==MatchMenuPage.None)OpenPause();
        }
    }
}
