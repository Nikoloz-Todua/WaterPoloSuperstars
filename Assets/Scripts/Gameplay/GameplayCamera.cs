using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum BroadcastCameraMode { NormalBroadcast, WideTactical, GoalkeeperPossession, SetPiece }
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
        [SerializeField,Range(0f,1f)] private float predictionSeconds=0.28f;
        [SerializeField,Range(0f,0.3f)] private float teammateInfluence=0.12f;
        [SerializeField,Min(0.1f)] private float minimumZoom=1f;
        private float requestedZoom=1f, zoomHoldUntil;
        private readonly Vector3[] framingPoints=new Vector3[18];
        private int framingCount;
        public BroadcastCameraMode ActiveMode {get;private set;}
        public BroadcastCameraMode Mode {get=>mode;set=>mode=value;}
        public float Zoom => currentHeight/Mathf.Max(1f,keeperTarget?keeperHeight:height);
        private BroadcastCameraProfile Profile
        {
            get
            {
                ActiveMode=match!=null && match.Rules!=null && match.Rules.IsRulePhase?BroadcastCameraMode.SetPiece:
                    keeperTarget?BroadcastCameraMode.GoalkeeperPossession:mode==BroadcastCameraMode.WideTactical?mode:BroadcastCameraMode.NormalBroadcast;
                return ActiveMode==BroadcastCameraMode.SetPiece?setPiece:ActiveMode==BroadcastCameraMode.GoalkeeperPossession?goalkeeperPossession:
                    ActiveMode==BroadcastCameraMode.WideTactical?wideTactical:normalBroadcast;
            }
        }

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
            float zoom=Mathf.Max(minimumZoom,profile.size);
            float baseHeight=keeperTarget?keeperHeight:height,baseDistance=keeperTarget?keeperDistance:distance;
            Quaternion inverse=Quaternion.Inverse(Quaternion.Euler(pitch,yaw,0f));
            float tan=Mathf.Tan(lens.fieldOfView*0.5f*Mathf.Deg2Rad);
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
                zoom=Mathf.Min(profile.maximumZoom,zoom*Mathf.Min(1.35f,requirement));
            }
            if(zoom>requestedZoom+0.04f){requestedZoom=zoom;zoomHoldUntil=Time.unscaledTime+0.6f;}
            else if(Time.unscaledTime>=zoomHoldUntil && zoom<requestedZoom-0.06f)requestedZoom=zoom;
            return Mathf.Clamp(requestedZoom,minimumZoom,profile.maximumZoom);
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
            ApplyPose();
        }

        private void OnEnable()
        {
            initialized = false;
        }

        private void LateUpdate()
        {
            if (target == null || poolBounds == null)
                return;
            if (!initialized)
                SnapToTarget();
            var profile=Profile;Vector3 desired=DesiredFocus();float zoom=FramingZoom(desired,profile);
            float framingDamping=adaptiveBroadcast?profile.damping:framingSmoothTime;
            currentDistance = Mathf.SmoothDamp(currentDistance, (keeperTarget ? keeperDistance : distance)*zoom,
                ref distanceVelocity, framingDamping, Mathf.Infinity, Time.deltaTime);
            currentHeight = Mathf.SmoothDamp(currentHeight, (keeperTarget ? keeperHeight : height)*zoom,
                ref heightVelocity, framingDamping, Mathf.Infinity, Time.deltaTime);
            focus = Vector3.SmoothDamp(focus, desired, ref focusVelocity,
                Mathf.Max(0.01f, smoothTime), Mathf.Infinity, Time.deltaTime);
            ApplyPose();
        }

        private void ApplyPose()
        {
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * currentDistance;
            transform.SetPositionAndRotation(focus + offset + Vector3.up * currentHeight,
                Quaternion.Euler(pitch, yaw, 0f));
        }
    }
}
