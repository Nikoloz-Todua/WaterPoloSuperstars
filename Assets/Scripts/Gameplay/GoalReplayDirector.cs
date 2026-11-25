using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo
{
    public enum GoalPresentationPhase { None, Celebration, Broadcast, BehindGoal }

    public readonly struct RecordedGoal
    {
        public readonly System.Guid MatchId;
        public readonly int Id, Quarter, ScorerCap;
        public readonly PrototypeTeam Team;
        public readonly string ScorerId;
        public readonly GoalAttribution Attribution;
        public readonly float PeriodRemaining, ClipSeconds;
        public RecordedGoal(PrototypeMatch match, GoalMouth mouth, BallController ball, float duration)
        {
            MatchId = match.MatchReplayId;
            Id = ball.ShotSequence;
            Quarter = match.Clock.Quarter;
            Team = mouth.ScoringTeam;
            PeriodRemaining = match.Clock.DisplayRemaining;
            ClipSeconds = duration;
            Attribution = match.LastGoalAttribution;
            bool credited = Attribution.Kind == GoalAttributionKind.AttackingShot ||
                Attribution.Kind == GoalAttributionKind.DefensiveDeflection;
            ScorerId = credited ? Attribution.PlayerId : null;
            ScorerCap = credited ? Attribution.CapNumber : 0;
        }
    }

    [DefaultExecutionOrder(310), DisallowMultipleComponent]
    public sealed class GoalReplayDirector : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private GamePreferences preferences;
        [SerializeField] private GameObject overlay;
        [SerializeField] private TMP_Text headline, detail, score;
        [SerializeField] private Image timeline;
        [SerializeField, Range(12,30)] private int sampleRate=20;
        [SerializeField, Range(5,12)] private float history=10f;
        [SerializeField, Range(1,7)] private float leadIn=4.6f;
        [SerializeField, Range(.5f,5)] private float celebration=2.7f;
        [SerializeField, Range(.5f,1.2f)] private float playbackSpeed=.95f;
        private GoalReplayBuffer buffer;
        private CanvasGroup[] liveHud;
        private float[] hudAlpha;
        private bool[] hudRaycasts;
        private bool hidingHud;
        private GameplayCamera broadcast;
        private Camera lens;
        private WaterInteractionEffects effects;
        private GoalMouth goal;
        private WaterVisuals.ArenaDeveloperCamera inspection;
        private const float NetTailSeconds=.40f;
        private const float SlowBeforeGoal=.32f;
        private const float DecisivePlaybackSpeed=.58f;
        private float nextSample, stageAt, clipStart, goalAt, inputAfter;
        private Vector3 crossingBall;
        private Vector3 focus, eye;
        private Quaternion rotation;
        private int scorer, regret, teamReaction, lastQuarter;
        private bool firstPose, drawScore, animatedSplash;
        private InputAction advance, skip;
        public GoalPresentationPhase Phase { get; private set; }
        public RecordedGoal CurrentGoal { get; private set; }
        public bool Active => Phase != GoalPresentationPhase.None;
        public int RecordingBytes => buffer?.BufferBytes ?? 0;
        public int RecordedFrames => buffer?.Count ?? 0;
        public float GoalTime => goalAt;
        public bool OwnGoalReaction => Active && regret > 0;
        public float ClipEndTime => buffer?.End ?? 0f;
        public float PlaybackTime { get; private set; }
        public Vector3 PlaybackBall => buffer != null && Active ? buffer.BallPosition : Vector3.zero;

        void Awake()
        {
            broadcast = match.GameplayView;
            lens = broadcast.GetComponent<Camera>();
            effects = FindAnyObjectByType<WaterInteractionEffects>();
            inspection = broadcast.GetComponent<WaterVisuals.ArenaDeveloperCamera>();
            match.AttachGoalPresentation(this);
            advance = new InputAction("Advance goal presentation",InputActionType.Button,"<Keyboard>/enter");
            advance.AddBinding("<Gamepad>/rightShoulder");
            skip = new InputAction("Skip goal presentation",InputActionType.Button,"<Keyboard>/backspace");
            skip.AddBinding("<Gamepad>/buttonEast");
        }

        void Start()
        {
            buffer = new GoalReplayBuffer(ball,roster,Mathf.CeilToInt(history*sampleRate)+2,match.transform);
            var siblings=overlay.transform.parent;
            liveHud=new CanvasGroup[siblings.childCount-1];
            hudAlpha=new float[liveHud.Length];
            hudRaycasts=new bool[liveHud.Length];
            int index=0;
            for (int i=0;i<siblings.childCount;i++)
            {
                var child=siblings.GetChild(i);
                if(child==overlay.transform) continue;
                if(!child.TryGetComponent<CanvasGroup>(out var group)) group=child.gameObject.AddComponent<CanvasGroup>();
                liveHud[index++]=group;
            }
        }

        void ShowLiveHud(bool visible)
        {
            if (liveHud==null || hidingHud==!visible) return;
            hidingHud=!visible;
            for (int i=0;i<liveHud.Length;i++)
            {
                var group=liveHud[i];
                if (group==null) continue;
                if (!visible)
                {
                    hudAlpha[i]=group.alpha;
                    hudRaycasts[i]=group.blocksRaycasts;
                    group.alpha=0;
                    group.blocksRaycasts=false;
                }
                else
                {
                    group.alpha=hudAlpha[i];
                    group.blocksRaycasts=hudRaycasts[i];
                }
            }
        }
        void OnEnable()
        {
            match.Changed += MatchChanged;
            match.NewMatch += Clear;
            advance.Enable();
            skip.Enable();
        }
        void OnDisable()
        {
            match.Changed -= MatchChanged;
            match.NewMatch -= Clear;
            advance.Disable();
            skip.Disable();
            Cancel();
        }
        void OnDestroy() { buffer?.Dispose(); advance?.Dispose(); skip?.Dispose(); }

        void Clear()
        {
            Cancel();
            buffer?.Clear();
            CurrentGoal=default;
            lastQuarter=0;
            nextSample=0;
        }

        void MatchChanged()
        {
            if (Active && match.Phase != MatchPhase.GoalRestart) Cancel();
            if (match.Phase==MatchPhase.SprintSetup && match.Clock.Quarter!=lastQuarter)
            {
                buffer?.Clear();
                lastQuarter=match.Clock.Quarter;
                nextSample=0;
            }
        }

        internal void Begin(GoalMouth mouth, BallController actualBall)
        {
            if (buffer == null || Active) return;
            float now = match.PresentationTime;
            buffer.Capture(now,actualBall,true);
            goal=mouth;
            goalAt=buffer.End;
            crossingBall=actualBall.PhysicsPosition;
            clipStart=Mathf.Max(buffer.Start,goalAt-leadIn);
            nextSample=goalAt+1f/sampleRate;
            CurrentGoal=new RecordedGoal(match,mouth,actualBall,goalAt-clipStart);
            var attribution=CurrentGoal.Attribution;
            int slot=buffer.Slot(attribution.Actor);
            var athlete=attribution.Actor != null ? attribution.Actor.GetComponent<SwimmerIdentity>() : null;
            // Keep the credit even when the roster slot has changed. Only pose the same athlete.
            if (athlete == null || athlete.CapNumber != attribution.CapNumber) slot=-1;
            scorer=CurrentGoal.ScorerId!=null ? slot : -1;
            regret=attribution.Kind==GoalAttributionKind.OwnGoal ? slot : -1;
            teamReaction=scorer<=0 ? buffer.ClosestFieldSlot(CurrentGoal.Team,mouth.transform.position) : -1;
            if (inspection != null && inspection.ViewIndex >= 0)
            {
                inspection.SetView(-1);
            }
            ShowLiveHud(false);
            broadcast.CinematicOverride=true;
            focus=mouth.transform.position;
            eye=lens.transform.position;
            rotation=lens.transform.rotation;
            SetStage(GoalPresentationPhase.Celebration);
        }

        public void Advance()
        {
            if (!Active || match.IsPaused || match.PresentationTime < inputAfter) return;
            AdvanceStage();
        }

        public void SkipAll()
        {
            if (Active && !match.IsPaused) Cancel();
        }

        void AdvanceStage()
        {
            if (Phase==GoalPresentationPhase.Celebration && preferences.AutomaticGoalReplays && CurrentGoal.ClipSeconds > .25f)
                SetStage(GoalPresentationPhase.Broadcast);
            else if (Phase==GoalPresentationPhase.Broadcast)
                SetStage(GoalPresentationPhase.BehindGoal);
            else Cancel();
        }

        void SetStage(GoalPresentationPhase value)
        {
            Phase=value;
            stageAt=match.PresentationTime;
            inputAfter=stageAt+.22f;
            firstPose=true;
            animatedSplash=false;
            drawScore=true;
        }

        void Cancel()
        {
            bool restore=Active;
            Phase=GoalPresentationPhase.None;
            ShowLiveHud(true);
            if (overlay != null) overlay.SetActive(false);
            buffer?.Clear();
            if (broadcast != null && restore)
            {
                effects?.EndRecordedContacts();
                broadcast.CinematicOverride=false;
                if (match != null)
                {
                    match.ReleaseGoalPresentation();
                    if (match.isActiveAndEnabled) broadcast.SnapToTarget();
                }
            }
        }

        void LateUpdate()
        {
            if (buffer == null) return;
            if (!Active)
            {
                bool record=match.IsPlaying || match.IsSprintRace || match.Phase==MatchPhase.PenaltyReady || match.Phase==MatchPhase.PenaltyFlight;
                if (record && !match.IsPaused && match.PresentationTime >= nextSample)
                {
                    float sampleTime=match.PresentationTime;
                    buffer.Capture(sampleTime,ball);
                    nextSample=sampleTime+1f/sampleRate;
                }
                return;
            }
            if (match.IsPaused)
            {
                overlay.SetActive(false);
                return;
            }
            if (skip.WasPressedThisFrame()) { SkipAll(); return; }
            if (advance.WasPressedThisFrame()) Advance();

            if (!Active) return;
            // Record the existing bounded net motion during the stoppage. The recorder never moves the ball.
            float now=match.PresentationTime;
            if (now >= nextSample && buffer.End < goalAt+NetTailSeconds)
            {
                buffer.Capture(now,ball,true);
                nextSample=now+1f/sampleRate;
            }
            bool celebrating=Phase==GoalPresentationPhase.Celebration;
            float elapsed=now-stageAt;
            float slowStart=Mathf.Max(clipStart,goalAt-SlowBeforeGoal);
            float buildUp=(slowStart-clipStart)/playbackSpeed;
            float duration=celebrating ? celebration : buildUp+(buffer.End-slowStart)/DecisivePlaybackSpeed+.18f;
            if (elapsed>=duration) { AdvanceStage(); return; }

            if (!overlay.activeSelf) overlay.SetActive(true);
            if (drawScore)
            {
                drawScore=false;
                bool own=CurrentGoal.Attribution.Kind==GoalAttributionKind.OwnGoal;
                headline.text=celebrating ? $"{(own?"OWN GOAL":"GOAL")}  /  {CurrentGoal.Team.ToString().ToUpperInvariant()}" :
                    Phase==GoalPresentationPhase.Broadcast ? "REPLAY  /  SIDELINE" : "REPLAY  /  GOAL LINE";
                detail.text=CurrentGoal.ScorerCap > 0 ? $"#{CurrentGoal.ScorerCap}   •   Q{CurrentGoal.Quarter}" : $"Q{CurrentGoal.Quarter}";
                score.text=$"BLUE  {match.BlueScore}  :  {match.WhiteScore}  WHITE";
            }
            timeline.rectTransform.localScale=new Vector3(Mathf.Clamp01(elapsed/duration),1f,1f);
            PlaybackTime=celebrating ? goalAt : elapsed < buildUp
                ? clipStart+elapsed*playbackSpeed
                : Mathf.Min(buffer.End,slowStart+(elapsed-buildUp)*DecisivePlaybackSpeed);
            buffer.ShowAt(PlaybackTime);
            if (celebrating)
            {
                buffer.Celebrate(scorer>0?scorer:teamReaction,elapsed);
                buffer.Regret(regret,elapsed);
                if (!animatedSplash && elapsed>.48f && scorer>0)
                {
                    animatedSplash=true;

                    effects?.Celebrate(buffer.ActorPosition(scorer),.45f);
                }
            }
            FrameCamera(celebrating,elapsed);
            if (!celebrating) effects?.ShowRecordedContacts(PlaybackTime);
        }

        void FrameCamera(bool celebrating, float elapsed)
        {
            Vector3 point=buffer.BallPosition;
            Vector3 mouth=goal.transform.position;
            Vector3 forward=goal.transform.forward;
            Vector3 target, wantedEye;
            float fov;
            if (celebrating)
            {
                int subject=regret>0?regret:scorer>0?scorer:teamReaction;
                Vector3 head=subject>0 ? buffer.ActorHead(subject) : mouth-forward*3f+Vector3.up*.35f;
                target=head+Vector3.up*.13f;
                wantedEye=head+forward*(2.4f+Mathf.Sin(elapsed*.9f)*.4f)+Vector3.right*2.8f+Vector3.up*1.08f;
                wantedEye.x=Mathf.Clamp(wantedEye.x,-11.6f,11.6f);
                wantedEye.z=Mathf.Clamp(wantedEye.z,-13.7f,13.7f);
                fov=42;
            }
            else if (Phase==GoalPresentationPhase.Broadcast)
            {
                target=Vector3.Lerp(point,mouth,.16f);
                target.y=Mathf.Max(.35f,target.y);
                wantedEye=new Vector3(Mathf.Min(15.7f,target.x+10.5f),7.4f,target.z-forward.z*2.1f);
                fov=51;
            }
            else
            {
                Vector3 right=goal.transform.right;
                // Approach from the opposite side of the ball's entry lane, on the field side of the posts.
                float side=Vector3.Dot(crossingBall-mouth,right)>0f?-1f:1f;
                float approach=Mathf.InverseLerp(goalAt-1.85f,goalAt-.25f,PlaybackTime);
                float close=Mathf.SmoothStep(0f,1f,approach);
                Vector3 wide=mouth-forward*6.3f+right*(side*6.8f)+Vector3.up*4.3f;
                Vector3 near=mouth-forward*3.6f+right*(side*2.75f)+Vector3.up*1.95f;
                wantedEye=Vector3.Lerp(wide,near,close);
                Vector3 opening=mouth+forward*.35f+Vector3.up*.44f;
                target=Vector3.Lerp(Vector3.Lerp(point,opening,.32f),Vector3.Lerp(opening,point,.38f),close);
                fov=Mathf.Lerp(57f,46f,close);
            }
            wantedEye.x=Mathf.Clamp(wantedEye.x,-15.6f,15.6f);
            wantedEye.z=Mathf.Clamp(wantedEye.z,-14f,14f);
            if (firstPose)
            {
                focus=target;
                if (!celebrating)
                {
                eye=wantedEye;
                rotation=Quaternion.LookRotation(focus-eye);
                }
            }
            firstPose=false;
            float blend=1f-Mathf.Exp(-Time.unscaledDeltaTime*(celebrating?6.5f:9f));
            focus=Vector3.Lerp(focus,target,blend);
            eye=Vector3.Lerp(eye,wantedEye,blend);
            rotation=Quaternion.Slerp(rotation,Quaternion.LookRotation(focus-eye),blend);
            lens.transform.SetPositionAndRotation(eye,rotation);
            lens.fieldOfView=Mathf.Lerp(lens.fieldOfView,fov,blend);
        }
    }
}

