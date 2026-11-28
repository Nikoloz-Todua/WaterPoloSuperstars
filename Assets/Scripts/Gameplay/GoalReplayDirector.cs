using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo
{
    public enum GoalPresentationPhase { None, Celebration, Broadcast, BehindGoal }

    internal enum GoalGesture { None, QuickPump, PowerRaise, Acknowledge, Composed, LateSurge, Teammate, Regret, KeeperGlance }

    internal readonly struct GoalReaction
    {
        internal readonly GoalGesture Gesture;
        internal readonly Vector3 LookAt;
        internal readonly float Weight, Age;
        internal bool HoldPosition => Gesture != GoalGesture.KeeperGlance && Weight > .16f;
        internal float Lift => Weight * (Gesture == GoalGesture.PowerRaise ? .25f :
            Gesture == GoalGesture.LateSurge ? .33f : Gesture == GoalGesture.QuickPump ? .09f : 0f);

        internal GoalReaction(GoalGesture gesture, Vector3 lookAt, float age, float duration)
        {
            Gesture = gesture; LookAt = lookAt; Age = age;
            Weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .27f, age)) *
                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(duration * .60f, duration, age)));
        }
    }

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
        private SwimmerIdentity celebrant, nearbyTeammate, disappointed;
        private GoalGesture scorerGesture, lastCelebration;
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
        public Vector3 PlaybackBall => Phase == GoalPresentationPhase.Celebration ? ball.PhysicsPosition :
            buffer != null && Active ? buffer.BallPosition : Vector3.zero;

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
            lastCelebration = GoalGesture.None;
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
            celebrant = buffer.AthleteAt(scorer > 0 ? scorer : teamReaction);
            PrototypeTeam conceding = CurrentGoal.Team == PrototypeTeam.Blue ? PrototypeTeam.White : PrototypeTeam.Blue;
            disappointed = buffer.AthleteAt(regret > 0 ? regret : buffer.ClosestFieldSlot(conceding, mouth.transform.position));
            nearbyTeammate = null;
            if (celebrant != null)
            {
                var provider = ball.RecentPassProvider(celebrant.Interaction);
                var teammate = provider != null ? provider.GetComponent<SwimmerIdentity>() : null;
                if (teammate != null && teammate.EligibleForPlay &&
                    (teammate.Motor.Position - celebrant.Motor.Position).sqrMagnitude <= 25f)
                    nearbyTeammate = teammate;
                float nearest = 15f;
                foreach (var candidate in roster.FieldPlayers)
                {
                    if (nearbyTeammate == teammate && teammate != null) break;
                    if (candidate == celebrant || candidate.Team != CurrentGoal.Team || !candidate.EligibleForPlay) continue;
                    float distance = (candidate.Motor.Position - celebrant.Motor.Position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    nearbyTeammate = candidate;
                }
            }
            int margin = CurrentGoal.Team == PrototypeTeam.Blue ? match.BlueScore - match.WhiteScore : match.WhiteScore - match.BlueScore;
            bool late = CurrentGoal.Quarter == match.Clock.Quarters && CurrentGoal.PeriodRemaining < 96f;
            scorerGesture = GoalGestureFor(margin, late, nearbyTeammate != null, lastCelebration);
            lastCelebration = scorerGesture;
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

        internal static GoalGesture GoalGestureFor(int margin, bool late, bool teammate, GoalGesture previous)
        {
            if (margin <= -2 || margin >= 4) return previous == GoalGesture.Composed ? GoalGesture.QuickPump : GoalGesture.Composed;
            if (late && (margin == 0 || margin == 1))
                return previous == GoalGesture.LateSurge ? GoalGesture.PowerRaise : GoalGesture.LateSurge;
            if (teammate && previous != GoalGesture.Acknowledge) return GoalGesture.Acknowledge;
            if (margin == 1 && previous != GoalGesture.PowerRaise) return GoalGesture.PowerRaise;
            return previous == GoalGesture.QuickPump ? GoalGesture.Composed : GoalGesture.QuickPump;
        }

        internal bool TryReadReaction(SwimmerIdentity player, out GoalReaction reaction)
        {
            reaction = default;
            if (Phase != GoalPresentationPhase.Celebration || player == null || !player.EligibleForPlay)
                return false;
            float age = match.PresentationTime - stageAt;
            Vector3 forward = player.Motor.Position + player.Motor.Facing * 2f + Vector3.up * .40f;
            if (player == celebrant)
            {
                Vector3 partner = nearbyTeammate != null ? nearbyTeammate.Motor.Position + Vector3.up * .45f : forward;
                reaction = new GoalReaction(scorerGesture, scorerGesture == GoalGesture.Acknowledge ? partner : forward,
                    age, scorerGesture == GoalGesture.Composed ? .85f : 1.55f);
            }
            else if (player == nearbyTeammate && celebrant != null)
                reaction = new GoalReaction(GoalGesture.Teammate, celebrant.Motor.Position + Vector3.up * .45f,
                    age - .24f, .98f);
            else if (player == disappointed)
                reaction = new GoalReaction(GoalGesture.Regret, goal.transform.position + Vector3.up * .08f,
                    age - .31f, 1.15f);
            else if (!player.IsFieldPlayer && player.Team != CurrentGoal.Team)
                reaction = new GoalReaction(GoalGesture.KeeperGlance, ball.PhysicsPosition, age - .42f, 1.30f);
            return reaction.Weight > .001f;
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
            if (!celebrating) buffer.ShowAt(PlaybackTime);
            if (celebrating)
            {
                if (!animatedSplash && elapsed>.48f && celebrant != null)
                {
                    animatedSplash=true;

                    float magnitude = scorerGesture is GoalGesture.PowerRaise or GoalGesture.LateSurge ? .62f : .24f;
                    effects?.Celebrate(celebrant.Motor.Position, magnitude);
                }
            }
            FrameCamera(celebrating,elapsed);
            if (!celebrating) effects?.ShowRecordedContacts(PlaybackTime);
        }

        void FrameCamera(bool celebrating, float elapsed)
        {
            Vector3 point=PlaybackBall;
            Vector3 mouth=goal.transform.position;
            Vector3 forward=goal.transform.forward;
            Vector3 target, wantedEye;
            float fov;
            if (celebrating)
            {
                int subject=regret>0?regret:scorer>0?scorer:teamReaction;
                Vector3 head=subject>0 ? buffer.LiveHead(subject) : mouth-forward*3f+Vector3.up*.35f;
                target=head+Vector3.up*.13f;
                if (regret <= 0 && nearbyTeammate != null)
                    target = Vector3.Lerp(target, nearbyTeammate.Motor.Position + Vector3.up * .56f, .28f);
                wantedEye=head+forward*(2.4f+Mathf.Sin(elapsed*.9f)*.4f)+Vector3.right*2.8f+Vector3.up*1.08f;
                wantedEye.x=Mathf.Clamp(wantedEye.x,-11.6f,11.6f);
                wantedEye.z=Mathf.Clamp(wantedEye.z,-13.7f,13.7f);
                fov=nearbyTeammate != null && regret <= 0 ? 49f : 42f;
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

