using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using ToduStudios.WaterPolo.WaterVisuals;

namespace ToduStudios.WaterPolo
{
    public enum PreMatchStage
    {
        ArenaIntro, TeamWalkout, FormalLineup, CaptainExchange, PresentationHold, TeamRoster,
        TeamHuddle, MatchStartTransition, Complete
    }

    /// <summary>Pause-aware ceremony timeline. PrototypeMatch grants and completes the stoppage.</summary>
    [DefaultExecutionOrder(315), DisallowMultipleComponent]
    public sealed class PreMatchPresentation : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private VenueIntroductionAnchors anchors;
        [SerializeField] private RefereeVisual[] referees;
        [SerializeField] private Material bluePennant, whitePennant;
        [SerializeField] private GameObject overlay, lineups;
        [SerializeField] private TMP_Text title, subtitle, blueLines, whiteLines;
        [SerializeField] private RectTransform progress;
        [SerializeField, Min(.5f)] private float arenaSeconds=2.3f, walkoutSeconds=5.4f, lineupSeconds=2.1f,
            exchangeSeconds=2.6f, holdSeconds=2.2f, rosterSeconds=3.8f, huddleSeconds=3.3f, transitionSeconds=1.2f;
        private Camera lens;
        private GameplayCamera gameplay;
        private PrematchCast cast;
        private InputAction next, skip;
        private CanvasGroup[] liveHud;
        private float[] previousAlpha;
        private bool[] previousRaycasts;
        private bool hidden;
        private TMP_Text deckCaption;
        private string previousCaption;
        private Renderer[] liveModels;
        private bool[] previousModelHidden;
        private float started, stageStarted, acceptAfter;
        private Vector3 stageCentre, fromEye;
        private Quaternion fromRotation;
        private float fromFov;
        private BroadcastCameraSnapshot opening;

        public PreMatchStage Stage { get; private set; }=PreMatchStage.Complete;
        public bool Active => Stage!=PreMatchStage.Complete;
        public int CastCount => cast?.Count ?? 0;
        public float StageElapsed => Active ? match.PresentationTime-stageStarted : 0;
        internal bool CanBegin => overlay!=null && PrematchCast.Ready(roster,anchors);

        private void Awake()
        {
            gameplay=match.GameplayView;
            lens=gameplay.GetComponent<Camera>();
            match.AttachPreMatch(this);
            next=new InputAction("Next presentation stage",InputActionType.Button,"<Keyboard>/enter");
            next.AddBinding("<Gamepad>/buttonSouth");
            skip=new InputAction("Skip pre-match presentation",InputActionType.Button,"<Keyboard>/backspace");
            skip.AddBinding("<Gamepad>/buttonEast");
        }

        private void OnEnable()
        {
            next.Enable();
            skip.Enable();
            match.Changed+=OnMatchChanged;
            match.PauseChanged+=OnPaused;
        }

        private void Start()
        {
            if (!match.IsPreMatchPresentation) return;
            try
            {
                cast=new PrematchCast(roster,anchors,referees,bluePennant,whitePennant);
                stageCentre=(anchors.blueLineup[6].position+anchors.whiteLineup[6].position)*.5f;
                blueLines.text=RosterText(cast.Blue);
                whiteLines.text=RosterText(cast.White);
                CacheHud();
                HideHud();
                HideLiveModels();
                deckCaption=anchors.crest!=null?anchors.crest.teamCaption:null;
                if(deckCaption!=null)
                {
                    previousCaption=deckCaption.text;
                    deckCaption.text="OPENING CEREMONY";
                }
                gameplay.CinematicOverride=true;
                opening=gameplay.GetOpeningPresentationPose();
                lens.transform.SetPositionAndRotation(opening.position,opening.rotation);
                lens.fieldOfView=opening.fieldOfView;
                started=match.PresentationTime;
                Enter(PreMatchStage.ArenaIntro);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception,this);
                Clear();
                match.FinishPreMatch(this);
            }
        }

        private void OnDisable()
        {
            next.Disable();
            skip.Disable();
            match.Changed-=OnMatchChanged;
            match.PauseChanged-=OnPaused;
            Clear();
        }

        private void OnDestroy() {next?.Dispose();skip?.Dispose();}
        private void OnMatchChanged() {if(Active&&!match.IsPreMatchPresentation) Clear();}
        private void OnPaused(bool paused) {if(Active&&overlay!=null)overlay.SetActive(!paused);}

        private void CacheHud()
        {
            var siblings=overlay.transform.parent;
            liveHud=new CanvasGroup[siblings.childCount-1];
            previousAlpha=new float[liveHud.Length];
            previousRaycasts=new bool[liveHud.Length];
            int index=0;
            for(int i=0;i<siblings.childCount;i++)
            {
                var child=siblings.GetChild(i);
                if(child==overlay.transform)continue;
                if(!child.TryGetComponent<CanvasGroup>(out var group)) group=child.gameObject.AddComponent<CanvasGroup>();
                liveHud[index++]=group;
            }
        }

        private void HideHud()
        {
            if(hidden || liveHud==null)return;
            hidden=true;
            for(int i=0;i<liveHud.Length;i++)
            {
                var group=liveHud[i];
                previousAlpha[i]=group.alpha;
                previousRaycasts[i]=group.blocksRaycasts;
                group.alpha=0;
                group.blocksRaycasts=false;
            }
        }

        private void HideLiveModels()
        {
            var models=new System.Collections.Generic.List<Renderer>();
            foreach(var athlete in roster.Members) models.AddRange(athlete.GetComponentsInChildren<Renderer>(true));
            foreach(var official in referees) if(official!=null)models.AddRange(official.GetComponentsInChildren<Renderer>(true));
            var ball=roster.Members[0].Interaction.Ball;
            models.AddRange(ball.GetComponentsInChildren<Renderer>(true));
            liveModels=models.ToArray();
            previousModelHidden=new bool[liveModels.Length];
            for(int i=0;i<liveModels.Length;i++)
            {
                previousModelHidden[i]=liveModels[i].forceRenderingOff;
                liveModels[i].forceRenderingOff=true;
            }
        }

        private void Clear()
        {
            Stage=PreMatchStage.Complete;
            if(overlay!=null)overlay.SetActive(false);
            if(hidden)
            {
                hidden=false;
                for(int i=0;i<liveHud.Length;i++)
                {
                    var group=liveHud[i];
                    if(group==null)continue;
                    group.alpha=previousAlpha[i];
                    group.blocksRaycasts=previousRaycasts[i];
                }
                if(gameplay!=null)gameplay.CinematicOverride=false;
            }
            if(liveModels!=null)
            {
                for(int i=0;i<liveModels.Length;i++)
                    if(liveModels[i]!=null)liveModels[i].forceRenderingOff=previousModelHidden[i];
                liveModels=null;
            }
            if(deckCaption!=null) deckCaption.text=previousCaption;
            deckCaption=null;
            cast?.Dispose();
            cast=null;
        }

        private void Finish()
        {
            Clear();
            match.FinishPreMatch(this);
        }

        public void Advance()
        {
            if(Active && !match.IsPaused && match.PresentationTime>=acceptAfter) EnterNext();
        }

        public void SkipAll()
        {
            if(Active && !match.IsPaused) Finish();
        }

        private void EnterNext()
        {
            if(Stage==PreMatchStage.MatchStartTransition) Finish();
            else Enter(Stage+1);
        }

        private void Enter(PreMatchStage value)
        {
            Stage=value;
            stageStarted=match.PresentationTime;
            acceptAfter=stageStarted+.22f;
            fromEye=lens.transform.position;
            fromRotation=lens.transform.rotation;
            fromFov=lens.fieldOfView;
            overlay.SetActive(true);
            lineups.SetActive(value==PreMatchStage.TeamRoster);
            switch(value)
            {
                case PreMatchStage.ArenaIntro: SetTitle("CHAMPIONSHIP NIGHT","BLUE  /  WHITE"); break;
                case PreMatchStage.TeamWalkout: SetTitle("TEAM ENTRANCE","BLUE  /  WHITE"); break;
                case PreMatchStage.FormalLineup: SetTitle("FORMAL LINEUP","TEAMS AND MATCH OFFICIALS"); break;
                case PreMatchStage.CaptainExchange:
                    SetTitle("PENNANT EXCHANGE",$"BLUE #{cast.BlueCaptain.Identity.CapNumber}     WHITE #{cast.WhiteCaptain.Identity.CapNumber}");
                    break;
                case PreMatchStage.PresentationHold: SetTitle("MATCH CEREMONY",""); break;
                case PreMatchStage.TeamRoster: SetTitle("STARTING LINEUPS",""); break;
                case PreMatchStage.TeamHuddle: SetTitle("FINAL PREPARATION","TEAM HUDDLES"); break;
                default: SetTitle("OPENING SPRINT",""); break;
            }
        }

        private void SetTitle(string heading,string detail)
        {
            title.text=heading;
            subtitle.text=detail;
            subtitle.enabled=detail.Length>0;
        }

        private static string RosterText(System.Collections.Generic.IReadOnlyList<PrematchCast.Athlete> players)
        {
            var text=new System.Text.StringBuilder();
            foreach(var athlete in players)
            {
                if(text.Length>0)text.AppendLine();
                var p=athlete.Identity;
                string role=p.IsFieldPlayer?RoleName(p.FieldPosition):"Goalkeeper";
                text.Append("<color=#91D5EB>#").Append(p.CapNumber.ToString("00")).Append("</color>   ");
                text.Append(string.IsNullOrWhiteSpace(athlete.DisplayName)?role:athlete.DisplayName);
                if(athlete.Captain)text.Append("  <color=#F3CD80>C</color>");
                else if(!p.IsFieldPlayer && !string.IsNullOrWhiteSpace(athlete.DisplayName))text.Append("  <color=#F3CD80>GK</color>");
            }
            return text.ToString();
        }

        private static string RoleName(FieldPositionRole role)=>role switch
        {
            FieldPositionRole.CentreForward=>"Centre forward",
            FieldPositionRole.CentreBack=>"Centre back",
            FieldPositionRole.LeftWing=>"Left wing",
            FieldPositionRole.RightWing=>"Right wing",
            FieldPositionRole.LeftFlat=>"Left flat",
            _=>"Right flat"
        };

        private float Duration=>Stage switch
        {
            PreMatchStage.ArenaIntro=>arenaSeconds,
            PreMatchStage.TeamWalkout=>walkoutSeconds,
            PreMatchStage.FormalLineup=>lineupSeconds,
            PreMatchStage.CaptainExchange=>exchangeSeconds,
            PreMatchStage.PresentationHold=>holdSeconds,
            PreMatchStage.TeamRoster=>rosterSeconds,
            PreMatchStage.TeamHuddle=>huddleSeconds,
            _=>transitionSeconds
        };

        private void LateUpdate()
        {
            if (!Active || match.IsPaused) return;
            if(skip.WasPressedThisFrame()){SkipAll();return;}
            if(next.WasPressedThisFrame() && match.PresentationTime>=acceptAfter)EnterNext();
            if (!Active) return;
            if(StageElapsed>=Duration)EnterNext();
            if (!Active) return;
            float t=Mathf.Clamp01(StageElapsed/Duration);
            progress.localScale=new Vector3(((int)Stage+t)/8f,1f,1f);
            cast.Show(Stage,t,match.PresentationTime-started);
            FrameCamera(t);
        }

        private void FrameCamera(float t)
        {
            Vector3 eye, look;
            float fov;
            if(Stage==PreMatchStage.ArenaIntro)
            {
                eye=opening.position+(opening.rotation*Vector3.forward)*t*2.2f;
                lens.transform.SetPositionAndRotation(eye,opening.rotation);
                lens.fieldOfView=opening.fieldOfView;
                return;
            }
            if(Stage==PreMatchStage.MatchStartTransition)
            {
                float blend=Mathf.SmoothStep(0,1,t);
                lens.transform.SetPositionAndRotation(Vector3.Lerp(fromEye,opening.position,blend),
                    Quaternion.Slerp(fromRotation,opening.rotation,blend));
                lens.fieldOfView=Mathf.Lerp(fromFov,opening.fieldOfView,blend);
                return;
            }

            switch(Stage)
            {
                case PreMatchStage.TeamWalkout:
                    eye=stageCentre+new Vector3(Mathf.Lerp(-1.8f,1.3f,t),3.9f,Mathf.Lerp(9.1f,7.3f,t));
                    look=stageCentre+new Vector3(0,1.25f,Mathf.Lerp(-5.9f,-.6f,t)); fov=55; break;
                case PreMatchStage.CaptainExchange:
                    eye=stageCentre+new Vector3(.78f-1.55f*t,1.8f,5.0f);
                    look=stageCentre+new Vector3(0,1.03f,.8f); fov=42; break;
                case PreMatchStage.TeamRoster:
                    eye=stageCentre+new Vector3(.9f-1.8f*t,3.12f,11.6f);
                    look=stageCentre+Vector3.up*.80f; fov=49; break;
                case PreMatchStage.TeamHuddle:
                    eye=stageCentre+new Vector3(3.0f-3.4f*t,6.1f,9.8f);
                    look=stageCentre+new Vector3(0,.52f,.7f); fov=47; break;
                default:
                    eye=stageCentre+new Vector3(-1f+2f*t,2.84f,10.7f);
                    look=stageCentre+Vector3.up*.90f; fov=49; break;
            }
            Quaternion direction=Quaternion.LookRotation(look-eye);
            float blendIn=Mathf.SmoothStep(0,1,Mathf.Clamp01(StageElapsed/(Stage==PreMatchStage.TeamWalkout?.85f:.55f)));
            lens.transform.SetPositionAndRotation(Vector3.Lerp(fromEye,eye,blendIn),
                Quaternion.Slerp(fromRotation,direction,blendIn));
            lens.fieldOfView=Mathf.Lerp(fromFov,fov,blendIn);
        }
    }
}
