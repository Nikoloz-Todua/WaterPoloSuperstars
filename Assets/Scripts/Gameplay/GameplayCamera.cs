using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum MatchCameraView { Lengthwise, SidelineBroadcast }
    // Replay values are reserved camera ownership states, not an implemented playback feature.
    public enum MatchCameraPresentation { Live, PeriodOpening, Inspection, GoalCelebration, ReplayPlayback, ReplayOrbit, ReturnToLive }
    [System.Serializable] public struct BroadcastCameraSnapshot
    {public Vector3 position;public Quaternion rotation;public float fieldOfView;public MatchCameraView view;public MatchCameraPresentation presentation;}
    [System.Serializable] public sealed class SidelineCameraProfile
    {
        [Min(12f)] public float distance=22f,height=17f;
        [Range(40f,65f)] public float fieldOfView=54f;
        [Range(1f,1.6f)] public float maximumZoom=1.45f;
        [Min(.1f)] public float damping=.48f;
        [Range(.2f,.8f)] public float ballWeight=.65f;
        public Vector3 lookAtOffset=Vector3.zero;
        public Vector3 positionOffset=Vector3.zero;
    }
    public enum BroadcastCameraMode { NormalBroadcast, WideTactical, GoalkeeperPossession, SetPiece, PeriodSprint }
    [System.Serializable]
    public sealed class BroadcastCameraProfile
    {
        [Range(0.7f,1.6f)] public float size=1f;
        [Range(0f,0.8f)] public float ballWeight=0.4f;
        [Range(1f,2f)] public float maximumZoom=1.5f;
        [Min(0.1f)] public float damping=0.45f;
    }
    /// <summary>Fixed-orientation broadcast camera with contextual composition, prediction and bounded zoom.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(200)]
    public sealed class GameplayCamera : MonoBehaviour
    {
        [SerializeField] private SwimmerMotor target;
        [SerializeField] private PoolBounds poolBounds;
        [SerializeField] private BallController ball;
        [SerializeField] private GoalMouth attackingGoal;
        [SerializeField, Min(0f)] private float goalLookAhead = 1.2f;
        [SerializeField, Min(1f)] private float keeperDistance = 8.5f;
        [SerializeField, Min(1f)] private float keeperHeight = 10.5f;
        [SerializeField, Min(0.05f)] private float framingSmoothTime = 0.4f;
        [Tooltip("Small bounded framing bias toward the ball only while it is nearby.")]
        [SerializeField, Range(0f, 0.4f)] private float ballInfluence = 0.15f;
        [SerializeField, Min(1f)] private float ballFramingRange = 10f;
        [SerializeField, Min(0f)] private float maxBallOffset = 1.5f;
        [SerializeField, Min(1f)] private float distance = 6.5f;
        [SerializeField, Min(1f)] private float height = 7.5f;
        [SerializeField, Range(20f, 80f)] private float pitch = 50f;
        [SerializeField, Range(-180f, 180f)] private float yaw;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.22f;
        [SerializeField, Min(0f)] private float lookAhead = 1.8f;
        [Tooltip("Keep the follow focus this far inside the pool; the deck may remain visible at edges.")]
        [SerializeField, Min(0f)] private float boundsInset = 1.5f;
        [Tooltip("Allowed deck beyond the pool when bounding the inner 70% of the camera view.")]
        [SerializeField, Min(0f)] private float outsideMargin = 2f;

        private Vector3 focus;
        private Vector3 focusVelocity;
        private bool initialized;
        private bool snapping;
        private Camera lens;
        private bool keeperTarget;
        private float currentDistance, currentHeight, distanceVelocity, heightVelocity;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private PrototypeTeamTactics[] teams=new PrototypeTeamTactics[0];
        [SerializeField] private BroadcastCameraMode mode;
        [SerializeField] private bool adaptiveBroadcast=true;
        [SerializeField] private BroadcastCameraProfile normalBroadcast=new BroadcastCameraProfile();
        [SerializeField] private BroadcastCameraProfile wideTactical=new BroadcastCameraProfile {size=1.18f,ballWeight=0.45f,maximumZoom=1.65f};
        [SerializeField] private BroadcastCameraProfile goalkeeperPossession=new BroadcastCameraProfile {size=1.1f,ballWeight=0.2f,maximumZoom=1.5f};
        [SerializeField] private BroadcastCameraProfile setPiece=new BroadcastCameraProfile {size=1.1f,ballWeight=0.5f,maximumZoom=1.5f};
        [SerializeField] private BroadcastCameraProfile periodSprint=new BroadcastCameraProfile {size=1.35f,ballWeight=.5f,maximumZoom=2.2f,damping=.5f};
        [SerializeField,Range(0f,1f)] private float predictionSeconds=0.28f;
        [SerializeField,Range(0f,0.3f)] private float teammateInfluence=0.12f;
        [SerializeField,Min(0.1f)] private float minimumZoom=1f;
        private float requestedZoom=1f, zoomHoldUntil;
        private readonly Vector3[] framingPoints=new Vector3[18];
        private int framingCount;
        [SerializeField] private SidelineCameraProfile sideline=new SidelineCameraProfile();
        [SerializeField,Range(.2f,1.2f)] private float viewBlendSeconds=.6f;
        [Tooltip("Optional architectural overview during sprint preparation only. Live framing stays field-relative.")]
        [SerializeField] private Transform openingOverview;
        [SerializeField,Range(40f,70f)] private float openingFieldOfView=60f;
        [SerializeField,Range(.2f,1.5f)] private float openingBlendSeconds=.85f;
        private float openingWeight;
        private MatchCameraView view;
        private float viewBlend,legacyFieldOfView,sideHeight,sideHeightVelocity;
        private bool capturedLens,sideInitialized;
        private Vector3 sideFocus,sideFocusVelocity;
        private readonly Vector3[] sidePoints=new Vector3[4];
        public MatchCameraView View=>view;
        public bool CinematicOverride { get; internal set; }
        public MatchCameraPresentation Presentation=>CinematicOverride?MatchCameraPresentation.ReplayPlayback:!enabled?MatchCameraPresentation.Inspection:match!=null&&match.Phase==MatchPhase.SprintSetup?MatchCameraPresentation.PeriodOpening:MatchCameraPresentation.Live;
        public float ViewBlend=>viewBlend;
        public void SetView(MatchCameraView value)=>view=value==MatchCameraView.SidelineBroadcast?value:MatchCameraView.Lengthwise;
        public BroadcastCameraSnapshot CaptureCameraPose()=>new BroadcastCameraSnapshot{position=transform.position,rotation=transform.rotation,fieldOfView=GetComponent<Camera>().fieldOfView,view=view,presentation=Presentation};
        public Vector2 ScreenToWorldMovement(Vector2 input)
        {
            Vector3 right=Vector3.ProjectOnPlane(transform.right,Vector3.up).normalized;
            Vector3 up=Vector3.ProjectOnPlane(transform.up,Vector3.up).normalized;
            Vector3 value=right*input.x+up*input.y;return new Vector2(value.x,value.z);
        }
        public BroadcastCameraMode ActiveMode {get;private set;}
        public BroadcastCameraMode Mode {get=>mode;set=>mode=value;}
        public float Zoom => currentHeight/Mathf.Max(1f,keeperTarget?keeperHeight:height);
        private BroadcastCameraProfile Profile
        {
            get
            {
                ActiveMode=match!=null&&match.IsPeriodSprint?BroadcastCameraMode.PeriodSprint:match!=null && (match.IsCentreRestart || match.Rules!=null && match.Rules.IsRulePhase)?BroadcastCameraMode.SetPiece:
                    keeperTarget?BroadcastCameraMode.GoalkeeperPossession:mode==BroadcastCameraMode.WideTactical?mode:BroadcastCameraMode.NormalBroadcast;
                return ActiveMode==BroadcastCameraMode.PeriodSprint?periodSprint:ActiveMode==BroadcastCameraMode.SetPiece?setPiece:ActiveMode==BroadcastCameraMode.GoalkeeperPossession?goalkeeperPossession:
                    ActiveMode==BroadcastCameraMode.WideTactical?wideTactical:normalBroadcast;
            }
        }

        public float SidelineElevation { get; set; } = 1f;
        public float UserZoom { get; private set; } = 1f;
        public float UserSmoothing { get; private set; } = 1f;
        public void SetUserPreferences(float zoom, float smoothing)
        { UserZoom=Mathf.Clamp(zoom,0.85f,1.25f); UserSmoothing=Mathf.Clamp(smoothing,0.65f,1.8f); requestedZoom=Profile.size*UserZoom; zoomHoldUntil=0f; }
        public Vector3 Focus => focus;
        public SwimmerMotor Target => target;
        public void SetAttackingGoal(GoalMouth goal) => attackingGoal=goal;

        public void SetTarget(SwimmerMotor followTarget)
        {
            // Keep the current focus/velocity: selecting another swimmer must not snap the camera.
            target = followTarget;
            keeperTarget = target != null && target.TryGetComponent<SwimmerIdentity>(out var identity) && !identity.IsFieldPlayer;
        }

        private Vector3 DesiredFocus()
        {
            if(!adaptiveBroadcast)return LegacyFocus();
            var profile=Profile;
            framingCount=0;
            if(match!=null&&match.IsPeriodSprint)
            {
                framingPoints[framingCount++]=ball.transform.position;
                if(match.Phase==MatchPhase.SprintSetup)
                {foreach(var p in roster.Members)if(p.EligibleForPlay&&framingCount<framingPoints.Length)framingPoints[framingCount++]=p.Motor.Position;return poolBounds.transform.position;}
                for(int i=0;i<2;i++){var p=match.SprintOff.Sprinter((PrototypeTeam)i);if(p!=null)framingPoints[framingCount++]=p.Motor.Position;}
                return Vector3.Lerp(target.Position,ball.transform.position,.5f);
            }
            Vector3 player=snapping?target.Position:target.transform.position;
            framingPoints[framingCount++]=player;
            Vector3 desired=player+Vector3.ClampMagnitude(target.Velocity*predictionSeconds,lookAhead);
            GoalMouth goal=attackingGoal;
            if(ball!=null)
            {
                Vector3 point=snapping?ball.PlayPosition:ball.Owner!=null?ball.Owner.transform.position:ball.transform.position;
                framingPoints[framingCount++]=point;
                Vector3 prediction=ball.PredictPosition(predictionSeconds);
                desired=Vector3.Lerp(desired,point,profile.ballWeight)+Vector3.ClampMagnitude(prediction-point,2f)*0.3f;
                if(ball.Owner!=null)
                {
                    var carrier=ball.Owner.GetComponent<SwimmerIdentity>();
                    for(int i=0;i<teams.Length;i++)if(teams[i].Team==carrier.Team)goal=teams[i].AttackingGoal;
                }
                if(roster!=null)
                {
                    Vector3 support=Vector3.zero;int count=0;
                    for(int i=0;i<roster.Members.Count;i++)
                    {
                        var p=roster.Members[i];if(!p.EligibleForPlay || !p.IsFieldPlayer || p.Motor==target || (p.Motor.Position-point).sqrMagnitude>64f)continue;
                        support+=p.Motor.Position;count++;
                        if(framingCount<framingPoints.Length)framingPoints[framingCount++]=p.Motor.Position;
                    }
                    if(count>0)desired=Vector3.Lerp(desired,support/count,teammateInfluence);
                }
                if(goal!=null && !keeperTarget && Vector3.Distance(point,goal.transform.position)<11f)
                {desired+=Vector3.ClampMagnitude(goal.transform.position-point,goalLookAhead)*0.5f;framingPoints[framingCount++]=goal.transform.position+Vector3.up*goal.Height*0.5f;}
            }
            desired=poolBounds.ClampPosition(desired,0.5f);
            return desired;
        }
        private Vector3 LegacyFocus()
        {
            // Velocity-based lead fades away at rest and doesn't jump when facing reverses.
            Vector3 lead = target.Velocity / Mathf.Max(0.1f, target.MaxSpeed) * lookAhead;
            Vector3 ballOffset = Vector3.zero;
            if (ball != null)
            {
                Vector3 gap = ball.transform.position - target.transform.position;
                gap.y = 0f;
                // Fade the influence to zero at the range limit, avoiding a focus discontinuity.
                float weight = Mathf.Clamp01(1f - gap.magnitude / Mathf.Max(1f, ballFramingRange));
                ballOffset = Vector3.ClampMagnitude(gap * ballInfluence * weight, maxBallOffset);
            }
            Vector3 goalBias = attackingGoal != null && !keeperTarget ? attackingGoal.transform.position - target.transform.position : Vector3.zero;
            goalBias.y = 0f;
            goalBias = Vector3.ClampMagnitude(goalBias, goalLookAhead);
            return ClampFocus(target.transform.position + lead + ballOffset + goalBias);
        }

        private float FramingZoom(Vector3 desired,BroadcastCameraProfile profile)
        {
            float zoom=Mathf.Max(minimumZoom,profile.size)*UserZoom;
            float baseHeight=keeperTarget?keeperHeight:height,baseDistance=keeperTarget?keeperDistance:distance;
            Quaternion inverse=Quaternion.Inverse(Quaternion.Euler(pitch,yaw,0f));
            float tan=Mathf.Tan((capturedLens?legacyFieldOfView:lens.fieldOfView)*0.5f*Mathf.Deg2Rad);
            for(int pass=0;pass<4;pass++)
            {
                Vector3 position=desired+Quaternion.Euler(0f,yaw,0f)*Vector3.back*(baseDistance*zoom)+Vector3.up*(baseHeight*zoom);
                float requirement=1f;
                for(int i=0;i<framingCount;i++)
                {
                    Vector3 local=inverse*(framingPoints[i]-position);
                    if(local.z<=0.1f){requirement=1.3f;continue;}
                    requirement=Mathf.Max(requirement,Mathf.Max(Mathf.Abs(local.x)/(local.z*tan*lens.aspect*0.8f),Mathf.Abs(local.y)/(local.z*tan*0.74f)));
                }
                if(requirement<=1.01f)break;
                zoom=Mathf.Min(profile.maximumZoom*UserZoom,zoom*Mathf.Min(1.35f,requirement));
            }
            if(zoom>requestedZoom+0.04f){requestedZoom=zoom;zoomHoldUntil=(match != null ? match.PresentationTime : Time.unscaledTime)+0.6f;}
            else if((match != null ? match.PresentationTime : Time.unscaledTime)>=zoomHoldUntil && zoom<requestedZoom-0.06f)requestedZoom=zoom;
            return Mathf.Clamp(requestedZoom,minimumZoom*UserZoom,profile.maximumZoom*UserZoom);
        }

        private Vector3 ClampFocus(Vector3 desired)
        {
            desired = poolBounds.ClampPosition(desired, boundsInset);
            if (lens == null) lens = GetComponent<Camera>();
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * currentDistance + Vector3.up * currentHeight;
            float spread = Mathf.Tan(lens.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.7f;
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            // Approximate the useful ground footprint, allowing some deck at the outer screen edges.
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector3 ray = rotation * new Vector3(x * spread * lens.aspect, y * spread, 1f);
                if (ray.y >= -0.01f)
                    return desired; // Near-horizon Inspector settings cannot define a finite footprint.
                Vector3 ground = offset + ray * (-currentHeight / ray.y);
                Vector2 point = new Vector2(ground.x, ground.z);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            Vector3 center = poolBounds.transform.position;
            float minX = center.x - poolBounds.Width * 0.5f - outsideMargin - min.x;
            float maxX = center.x + poolBounds.Width * 0.5f + outsideMargin - max.x;
            float minZ = center.z - poolBounds.Length * 0.5f - outsideMargin - min.y;
            float maxZ = center.z + poolBounds.Length * 0.5f + outsideMargin - max.y;
            desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : (minX + maxX) * 0.5f;
            desired.z = minZ <= maxZ ? Mathf.Clamp(desired.z, minZ, maxZ) : (minZ + maxZ) * 0.5f;
            return desired;
        }

        public void SnapToTarget()
        {
            if (target == null || poolBounds == null)
                return;
            SetTarget(target);
            if(lens==null)lens=GetComponent<Camera>();
            requestedZoom=Profile.size;
            currentDistance = keeperTarget ? keeperDistance : distance;
            currentHeight = keeperTarget ? keeperHeight : height;
            distanceVelocity = heightVelocity = 0f;
            snapping=true;focus=DesiredFocus();snapping=false;
            float zoom=FramingZoom(focus,Profile);currentDistance*=zoom;currentHeight*=zoom;
            focusVelocity = Vector3.zero;
            initialized = true;
            viewBlend=Opening?0f:view==MatchCameraView.SidelineBroadcast?1f:0f;sideInitialized=false;
            openingWeight=Opening&&openingOverview!=null?1f:0f;
            ApplyPose();
        }

        private void OnEnable()
        {
            lens=GetComponent<Camera>();if(!capturedLens){legacyFieldOfView=lens.fieldOfView;capturedLens=true;}
            initialized = false;
        }

        private void LateUpdate()
        {
            if (CinematicOverride) return;
            if((target==null||!target.gameObject.activeInHierarchy)&&roster!=null)foreach(var p in roster.Members)if(p!=null&&p.isActiveAndEnabled&&p.EligibleForPlay){SetTarget(p.Motor);break;}
            if (target == null || poolBounds == null)
                return;
            if (!initialized)
                SnapToTarget();
            if(match!=null&&match.IsPaused){ApplyPose();return;}
            var profile=Profile;Vector3 desired=DesiredFocus();float zoom=FramingZoom(desired,profile);
            float framingDamping=(adaptiveBroadcast?profile.damping:framingSmoothTime)*UserSmoothing;
            currentDistance = Mathf.SmoothDamp(currentDistance, (keeperTarget ? keeperDistance : distance)*zoom,
                ref distanceVelocity, framingDamping, Mathf.Infinity, Time.deltaTime);
            currentHeight = Mathf.SmoothDamp(currentHeight, (keeperTarget ? keeperHeight : height)*zoom,
                ref heightVelocity, framingDamping, Mathf.Infinity, Time.deltaTime);
            focus = Vector3.SmoothDamp(focus, desired, ref focusVelocity,
                Mathf.Max(0.01f, smoothTime*UserSmoothing), Mathf.Infinity, Time.deltaTime);
            ApplyPose();
        }

        private void ApplyPose()
        {
            if (CinematicOverride) return;
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * currentDistance;
            Vector3 position=focus+offset+Vector3.up*currentHeight;Quaternion rotation=Quaternion.Euler(pitch,yaw,0f);
            float desired=Opening?0f:view==MatchCameraView.SidelineBroadcast?1f:0f;
            viewBlend=Mathf.MoveTowards(viewBlend,desired,Time.unscaledDeltaTime/Mathf.Max(.2f,viewBlendSeconds));
            if(viewBlend>0f)
            {
                SidePose(out var sidePosition,out var sideRotation);float t=Mathf.SmoothStep(0,1,viewBlend);
                position=Vector3.Lerp(position,sidePosition,t);position.y+=Mathf.Sin(t*Mathf.PI)*3f;
                rotation=Quaternion.Slerp(rotation,sideRotation,t);lens.fieldOfView=Mathf.Lerp(legacyFieldOfView,sideline.fieldOfView,t);
            }
            else lens.fieldOfView=legacyFieldOfView;
            openingWeight=Mathf.MoveTowards(openingWeight,Opening&&openingOverview!=null?1f:0f,Time.unscaledDeltaTime/Mathf.Max(.2f,openingBlendSeconds));
            if(openingOverview!=null&&openingWeight>0)
            {
                float t=Mathf.SmoothStep(0,1,openingWeight);position=Vector3.Lerp(position,openingOverview.position,t);
                rotation=Quaternion.Slerp(rotation,openingOverview.rotation,t);lens.fieldOfView=Mathf.Lerp(lens.fieldOfView,openingFieldOfView,t);
            }
            var rules = match != null ? match.Rules : null;
            bool penaltyView = rules != null && rules.IsPenalty && rules.Taker != null;
            penaltyBlend = Mathf.MoveTowards(penaltyBlend, penaltyView ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            if (penaltyView)
            {
                var direction = rules.PenaltyGoal.transform.forward;
                var shooter = rules.Taker.Motor.Position;
                penaltyEye = shooter - direction * 3.2f + Vector3.Cross(Vector3.up, direction) * .7f + Vector3.up * 1.9f;
                penaltyLook = rules.PenaltyGoal.transform.position + Vector3.up * .45f;
            }
            if (penaltyBlend > 0f)
            {
                float weight = Mathf.SmoothStep(0f, 1f, penaltyBlend);
                position = Vector3.Lerp(position, penaltyEye, weight);
                rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(penaltyLook - penaltyEye), weight);
                lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, 49f, weight);
            }
            transform.SetPositionAndRotation(position,rotation);
        }
        private float penaltyBlend;
        private Vector3 penaltyEye, penaltyLook;
        private bool Opening=>match!=null&&match.Phase==MatchPhase.SprintSetup;
        private void SidePose(out Vector3 position,out Quaternion rotation)
        {
            Vector3 player=target!=null?target.Position:poolBounds.transform.position;
            Vector3 point=ball!=null?ball.PlayPosition:player;
            Vector3 wanted=Vector3.Lerp(player,point,sideline.ballWeight);
            wanted.x+=Mathf.Max(0f,Mathf.Max(player.x,point.x)-wanted.x)*.35f;
            if(ball!=null)wanted.z+=Mathf.Clamp(ball.Velocity.z*.10f,-1.2f,1.2f);
            Vector3 center=poolBounds.transform.position;
            wanted.x=Mathf.Clamp(wanted.x,center.x-8f,center.x+8f);wanted.z=Mathf.Clamp(wanted.z,center.z-11f,center.z+11f);wanted.y=poolBounds.Waterline;
            GoalMouth goal = attackingGoal;
            if (ball != null && ball.Owner != null)
            {
                var carrier = ball.Owner.GetComponent<SwimmerIdentity>();
                foreach (var team in teams) if (team.Team == carrier.Team) goal = team.AttackingGoal;
                if (goal != null)
                {
                    Vector3 toGoal = goal.transform.position - point;
                    wanted.z += Mathf.Clamp(toGoal.z * .13f, -1f, 1f);
                    if (toGoal.sqrMagnitude < 90f) wanted = Vector3.Lerp(wanted, goal.transform.position, .13f);
                }
            }
            wanted+=sideline.lookAtOffset;
            float zoom = Mathf.InverseLerp(.85f, 1.25f, UserZoom);
            float setback = sideline.distance * Mathf.Lerp(.43f, .98f, zoom);
            float baseHeight = sideline.height * Mathf.Lerp(.48f, 1f, zoom) * Mathf.Clamp(SidelineElevation,.8f,1.25f);
            if(!sideInitialized){sideFocus=wanted;sideHeight=baseHeight;sideInitialized=true;}
            sideFocus=Vector3.SmoothDamp(sideFocus,wanted,ref sideFocusVelocity,sideline.damping*UserSmoothing,Mathf.Infinity,Time.unscaledDeltaTime);
            int count=0;sidePoints[count++]=player+Vector3.up*.45f;sidePoints[count++]=point;
            if(goal!=null&&(goal.transform.position-point).sqrMagnitude<110f)sidePoints[count++]=goal.transform.position+Vector3.up*.5f;
            float desiredHeight=baseHeight;
            for(int pass=0;pass<5;pass++)
            {
                position=new Vector3(Mathf.Min(center.x+22.5f,sideFocus.x+setback),poolBounds.Waterline+desiredHeight,sideFocus.z)+sideline.positionOffset;
                rotation=Quaternion.LookRotation(sideFocus-position);Quaternion inverse=Quaternion.Inverse(rotation);float tan=Mathf.Tan(sideline.fieldOfView*.5f*Mathf.Deg2Rad);bool fits=true;
                for(int i=0;i<count;i++){Vector3 p=inverse*(sidePoints[i]-position);if(p.z<.1f){fits=false;continue;}float x=.5f+p.x/(2*p.z*tan*lens.aspect),y=.5f+p.y/(2*p.z*tan);if(x<.13f||x>.87f||y<.30f||y>.78f)fits=false;}
                if(fits)break;desiredHeight=Mathf.Min(26f,Mathf.Min(baseHeight*Mathf.Min(1.22f,sideline.maximumZoom),desiredHeight*1.12f));
            }
            if (Mathf.Abs(desiredHeight - sideHeight) < .16f) desiredHeight = sideHeight;
            sideHeight=Mathf.SmoothDamp(sideHeight,desiredHeight,ref sideHeightVelocity,sideline.damping*.65f,Mathf.Infinity,Time.unscaledDeltaTime);
            position=new Vector3(Mathf.Min(center.x+22.5f,sideFocus.x+setback),poolBounds.Waterline+sideHeight,sideFocus.z)+sideline.positionOffset;
            rotation=Quaternion.LookRotation(sideFocus-position);
        }
    }
}
