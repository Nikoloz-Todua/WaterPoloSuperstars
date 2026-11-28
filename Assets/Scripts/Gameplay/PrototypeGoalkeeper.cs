using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Role-specific keeper tracking and goal-local movement envelope, using the shared motor.</summary>
    [DefaultExecutionOrder(-30), DisallowMultipleComponent, RequireComponent(typeof(SwimmerIdentity))]
    public sealed class PrototypeGoalkeeper : MonoBehaviour, ISwimmerMovementBounds
    {
        [SerializeField] private GoalMouth defendedGoal;
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private PoolBounds poolBounds;
        [SerializeField, Min(0.1f)] private float carryingSpeed = 3.2f;
        [SerializeField, Min(0f)] private float possessionProtectionDistance = 1.5f;
        [SerializeField, Min(0.5f)] private float stationDepth = 1.4f;
        [SerializeField, Min(0f)] private float lateralMargin = 0.45f;
        [SerializeField, Min(0f)] private float trackingRange = 12f;
        [SerializeField, Min(0f)] private float reactionDelay = 0.16f;
        [SerializeField, Min(0.05f)] private float trackingSmoothTime = 0.15f;
        [SerializeField, Min(0.1f)] private float saveReach = 0.5f;
        [SerializeField, Range(0f, 1f)] private float saveChance = 0.8f;
        [SerializeField, Range(0f, 1f)] private float catchChance = 0.65f;
        [SerializeField, Range(0f, 1f)] private float deflectionRestitution = 0.65f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.5f;
        [SerializeField] private int randomSeed = 173;
        private SwimmerIdentity identity;
        private Rigidbody ballBody;
        private float targetX, targetVelocity, reactAt;
        private bool sawThreat;
        private System.Random random;
        private float recoverUntil;
        private float handLift;
        [SerializeField,Min(0f)] private float maximumHandLift=0.65f;
        [SerializeField,Min(0.1f)] private float handLiftSpeed=2.5f;
        [SerializeField] private Transform leftArmVisual,rightArmVisual;
        private Vector3 leftArmRest,rightArmRest;
        private bool armRestCaptured;
        private Vector3 presentedSavePoint;
        private float presentedSaveUntil;
        private int attemptedShot = -1;
        private bool presentedShot;
        private bool pursuingLooseBall;
        internal bool PursuingLooseBall => pursuingLooseBall;
        public float SaveReach => saveReach;
        public bool Recovering => Time.time < recoverUntil;
        public int Catches { get; private set; }
        public int Deflections { get; private set; }
        public int SaveAttempts { get; private set; }
        internal bool UsesAnimatedHands => ball.ProfessionalThrows && Identity.Interaction.Visual != null &&
            Identity.Interaction.Visual.SupportsEvaluatedGrip;
        public bool ThreateningShot => UsesAnimatedHands ? TryReadApproach(out _, out _, out bool shot) && shot :
            ball.Owner == null && ball.ReleaseKind == BallReleaseKind.Shot && ball.IsScoringShot && defendedGoal.transform.InverseTransformDirection(ball.Velocity).z > 0.1f;
        public SwimmerIdentity Identity => identity != null ? identity : identity = GetComponent<SwimmerIdentity>();
        public GoalMouth DefendedGoal => defendedGoal;
        public void SetDefendedGoal(GoalMouth goal)
        {defendedGoal=goal;ResetDecisions();}
        public BallController Ball => ball;
        public bool Stopped => !Identity.EligibleForPlay || (!match.IsPlaying && (match.Rules==null || !match.Rules.KeeperMayAct(this))) ||
            (selection.InputLocked && match.Phase!=MatchPhase.PenaltyFlight) || !Identity.Motor.enabled;
        public bool HumanControlled => selection.ActivePlayer == Identity && !selection.InputLocked;
        public bool AtStation => Vector3.Distance(Identity.Motor.Position, StationPosition(Identity.Motor.Position)) <= 0.05f;
        public bool PossessionProtected => Vector3.Distance(Identity.Motor.Position, StationPosition(Identity.Motor.Position)) <= possessionProtectionDistance;
        public float LateralLimit => Mathf.Max(0f, defendedGoal.Width * 0.5f - lateralMargin);
        private bool PenaltyPosition => match.Rules != null && match.Rules.PenaltyStation(this);
        private float ActiveStationDepth => PenaltyPosition ? -0.45f : stationDepth;
        public void ChoosePenaltySide(float input) => penaltyLean = Mathf.Clamp(input, -1f, 1f);
        public void PreparePenalty()
        {
            penaltyLean = 0f;
            Identity.Motor.SetMovementBounds(this);
            Vector3 point=defendedGoal.transform.position+defendedGoal.transform.forward*0.45f;point.y=poolBounds.Waterline;
            Identity.Motor.ResetPose(point,Quaternion.LookRotation(-defendedGoal.transform.forward));
        }
        public bool Ready => Time.time >= reactAt;
        internal float Readiness
        {
            get
            {
                var owner = ball.Owner != null ? ball.Owner.GetComponent<SwimmerIdentity>() : null;
                if (owner == null || owner.Team == Identity.Team || Stopped) return 0f;
                Vector3 local = defendedGoal.transform.InverseTransformPoint(owner.Motor.Position);
                if (local.z >= 0f || local.z < -8f || Mathf.Abs(local.x) > -local.z * .8f + .6f) return 0f;
                return Mathf.InverseLerp(8f, 2.6f, -local.z);
            }
        }

        private bool TryReadApproach(out Vector3 point, out float seconds, out bool shot)
        {
            point = default;
            seconds = 0f;
            shot = ball.ReleaseKind == BallReleaseKind.Shot && ball.IsScoringShot;
            if (ball.Owner != null || !match.BallInMotion || Stopped || !AtStation || Recovering) return false;
            Vector3 local = defendedGoal.transform.InverseTransformPoint(ball.PhysicsPosition);
            Vector3 velocity = defendedGoal.transform.InverseTransformDirection(ball.Velocity);
            if (velocity.z < 1.1f || local.z >= 0f || local.z < -trackingRange) return false;

            // Meet the ball in front of the swimmer. The penalty root may be behind the line.
            float depth = Mathf.Min(-.13f, -ActiveStationDepth - .29f);
            seconds = (depth - local.z) / velocity.z;
            if (seconds < -.12f || seconds > .72f) return false;
            point = ball.PredictPosition(Mathf.Max(0f, seconds));
            if (!shot)
            {
                Vector3 reach = point - Identity.Motor.Position;
                return ball.IsAirborne && ball.Velocity.sqrMagnitude <= 144f &&
                    point.y < poolBounds.Waterline + .74f &&
                    Mathf.Abs(defendedGoal.transform.InverseTransformDirection(reach).x) < .68f;
            }

            Vector3 goalPoint = defendedGoal.transform.InverseTransformPoint(ball.PredictPosition(-local.z / velocity.z));
            return Mathf.Abs(goalPoint.x) <= defendedGoal.Width * .5f + ball.Radius * .45f &&
                goalPoint.y < defendedGoal.Height + ball.Radius * .4f;
        }

        internal bool TryGetSavePresentation(out Vector3 point, out float weight, out bool shot)
        {
            point = presentedSavePoint;
            weight = 0f;
            shot = presentedShot;
            if (!isActiveAndEnabled || Stopped || pursuingLooseBall) return false;
            if (Time.time < presentedSaveUntil)
            {
                weight = Mathf.Clamp01((presentedSaveUntil - Time.time) / .16f);
                return true;
            }
            if (!TryReadApproach(out point, out float seconds, out shot) || seconds > .46f) return false;
            float urgency = Mathf.InverseLerp(12f, 25f, ball.Velocity.magnitude);
            float reaction = shot ? Mathf.Lerp(1.4f, 1f, saveChance) * Mathf.Lerp(1f, .54f, urgency) : .52f;
            if (Time.time < ball.ReleaseStartedAt + reactionDelay * reaction) return false;
            weight = Mathf.Lerp(.50f, 1f, 1f - Mathf.InverseLerp(.07f, .46f, seconds));
            return true;
        }

        internal bool TryReadPalms(out Vector3 left, out Vector3 right)
        {
            left = right = default;
            return UsesAnimatedHands && Identity.Interaction.Visual.TryReadKeeperPalms(out left, out right);
        }

        internal bool CanContactWithPalm(out bool shot) => TryGetSavePresentation(out _, out _, out shot) &&
            !Recovering && ball.Owner == null;
        public Vector3 HandPosition => Identity.Motor.Position + (Identity.Interaction.CarryAnchor.position - transform.position)+Vector3.up*handLift;
        public Vector3 ClampMovement(Vector3 position)
        {
            if (PenaltyPosition) return StationPosition(position);
            if (poolBounds.HasPhysicalBasin && (!isActiveAndEnabled || HumanControlled || pursuingLooseBall || !AtStation))
                return position;
            // A saved penalty leaves the body on the goal line. Swim through the mouth back to the
            // ordinary station; a generic pool clamp would jump it forward on the first live step.
            Vector3 currentLocal=defendedGoal.transform.InverseTransformPoint(Identity.Motor.Position);
            if(!AtStation && currentLocal.z>-stationDepth && currentLocal.z<=0.5f && Mathf.Abs(currentLocal.x)<=LateralLimit)
            {
                Vector3 local=defendedGoal.transform.InverseTransformPoint(position);
                local.x=Mathf.Clamp(local.x,-LateralLimit,LateralLimit);local.z=Mathf.Clamp(local.z,-stationDepth,0.45f);
                Vector3 result=defendedGoal.transform.TransformPoint(local);result.y=position.y;return result;
            }
            // Outside the station, return by swimming. Applying station bounds immediately would teleport.
            if (HumanControlled || !AtStation) return poolBounds.ClampPosition(position, 1.1f);
            return StationPosition(position);
        }
        private Vector3 StationPosition(Vector3 position)
        {
            Vector3 local = defendedGoal.transform.InverseTransformPoint(position);
            local.x = Mathf.Clamp(local.x, -LateralLimit, LateralLimit); local.z = -ActiveStationDepth;
            Vector3 result = defendedGoal.transform.TransformPoint(local); result.y = position.y; return result;
        }
        private float penaltyLean;
        private void Awake()
        {
            identity = GetComponent<SwimmerIdentity>();
            if (defendedGoal == null || ball == null || match == null || selection == null || poolBounds == null || Identity.Role != AthleteRole.Goalkeeper || defendedGoal.ScoringTeam == Identity.Team)
            { Debug.LogError("PrototypeGoalkeeper requires its explicit role, opposing scoring goal and match references.", this); enabled = false; return; }
            Identity.Motor.SetMovementBounds(this);
            ballBody = ball.GetComponent<Rigidbody>();
            random = new System.Random(randomSeed);
            if(leftArmVisual!=null)leftArmRest=leftArmVisual.localPosition;
            if(rightArmVisual!=null)rightArmRest=rightArmVisual.localPosition;
            armRestCaptured=true;
        }
        private void OnEnable()
        {
            if (match != null) match.Changed += ResetDecisions;
            if (defendedGoal != null) Identity.Motor.SetMovementBounds(this);
        }
        private void ResetDecisions()
        {
            if (match.Phase == MatchPhase.PenaltyFlight) { sawThreat = true; return; }
            targetX = targetVelocity = 0f; sawThreat = false; reactAt = Time.time + reactionDelay;
            handLift=0f;
            presentedSaveUntil = 0f;
            pursuingLooseBall = false;
            attemptedShot = -1;
            ApplyArmLift();
            recoverUntil = 0f; random = new System.Random(randomSeed);
            Identity.Motor.StopMotion();
        }
        private void Update()
        {
            if (match.IsPaused) return;
            if (Stopped) return;
            if (HumanControlled)
            {
                pursuingLooseBall = false;
                Identity.Motor.OverrideSpeed(carryingSpeed);
                targetVelocity = 0f;
                return;
            }
            Identity.Motor.ClearSpeedOverride();
            if (Identity.Interaction.HasBall)
            {
                pursuingLooseBall = false;
                Identity.Motor.SetMoveInput(Vector2.zero);
                return;
            }
            Vector3 ballPoint=ball.PlayPosition;
            Vector3 local = defendedGoal.transform.InverseTransformPoint(ballPoint);
            Vector3 velocity = ballBody.linearVelocity;
            Vector3 recovery = PrototypeTeamTactics.Planar(ballPoint - Identity.Motor.Position);
            pursuingLooseBall = poolBounds.HasPhysicalBasin && !PenaltyPosition && local.z > -3.3f &&
                Mathf.Abs(local.x) < defendedGoal.Width * .5f + 1.35f && recovery.sqrMagnitude < 13f &&
                ball.CanRecoverKeeperBall && ball.IsRecoveryCandidate(Identity.Interaction);
            if (pursuingLooseBall)
            {
                presentedSaveUntil = 0f;
                sawThreat = false;
                handLift = Mathf.MoveTowards(handLift, 0f, handLiftSpeed * Time.deltaTime);
                ApplyArmLift();
                Identity.Motor.SetFacingDirection(recovery);
                Identity.Motor.SetMoveInput(Vector2.ClampMagnitude(new Vector2(recovery.x, recovery.z) * 1.6f, .85f));
                return;
            }
            bool animated = UsesAnimatedHands;
            Vector3 predicted = default;
            bool incoming = animated ? TryReadApproach(out predicted, out _, out _) :
                ball.Owner == null && defendedGoal.transform.InverseTransformDirection(velocity).z > 0.1f && local.z < 0f && local.z > -trackingRange;
            if (incoming && !sawThreat) reactAt = Time.time + reactionDelay;
            sawThreat = incoming;
            if (animated && attemptedShot != ball.ShotSequence &&
                TryGetSavePresentation(out _, out _, out bool shot) && shot)
            {
                attemptedShot = ball.ShotSequence;
                SaveAttempts++;
            }
            float flightTime=velocity.sqrMagnitude>0.01f?Vector3.Distance(ball.PhysicsPosition,HandPosition)/velocity.magnitude:0f;
            float desiredLift=incoming && Ready?Mathf.Clamp(ball.PredictPosition(Mathf.Min(0.7f,flightTime)).y-Identity.Motor.Position.y-0.07f,0f,maximumHandLift):0f;
            handLift=Mathf.MoveTowards(handLift,desiredLift,handLiftSpeed*Time.deltaTime);
            ApplyArmLift();
            float desired = 0f;
            var carrier = ball.Owner != null ? ball.Owner.GetComponent<SwimmerIdentity>() : null;
            if (animated && incoming)
                desired = Mathf.Lerp(targetX, defendedGoal.transform.InverseTransformPoint(predicted).x, .78f);
            else if (Ready && !Recovering && (incoming || (carrier != null && carrier.Team != Identity.Team && local.z > -trackingRange && local.z < 0f)))
                desired = GoalkeeperPositioning.TrackingX(defendedGoal, ballPoint, velocity, ActiveStationDepth, 2f);
            if (PenaltyPosition && Identity.Team == selection.HumanTeam)
                desired = Mathf.Lerp(desired, penaltyLean * LateralLimit, Mathf.Abs(penaltyLean) * .85f);
            targetX = Mathf.SmoothDamp(targetX, Mathf.Clamp(desired, -LateralLimit, LateralLimit), ref targetVelocity, trackingSmoothTime);
            Vector3 target = defendedGoal.transform.TransformPoint(new Vector3(targetX, 0f, -ActiveStationDepth)); target.y = Identity.Motor.Position.y;
            Vector3 delta = target - Identity.Motor.Position;
            Identity.Motor.SetMoveInput(delta.sqrMagnitude > .0025f
                ? Vector2.ClampMagnitude(new Vector2(delta.x, delta.z) * 2f, 1f) : Vector2.zero);
            Vector3 poolFacing = -defendedGoal.transform.forward;
            Vector3 towardPlay = PrototypeTeamTactics.Planar(ballPoint - Identity.Motor.Position).normalized;
            Identity.Motor.SetFacingDirection(animated && Vector3.Dot(towardPlay, poolFacing) > .35f
                ? Vector3.RotateTowards(poolFacing, towardPlay, .50f, 0f) : poolFacing);
        }
        public bool ResolveContact(Vector3 contact, Vector3 handAtContact, Vector3 incomingVelocity)
        {
            if (!isActiveAndEnabled || Stopped || Recovering || !AtStation || ball.Owner != null) return false;
            bool slowLoose = ball.CanCollect && incomingVelocity.sqrMagnitude < 6.25f;
            bool approaching = defendedGoal.transform.InverseTransformDirection(incomingVelocity).z > 0.1f && ball.IsScoringShot;
            if (!slowLoose && (!approaching || !Ready || Time.time < ball.ReleaseStartedAt + reactionDelay)) return false;
            if (!slowLoose) { SaveAttempts++; if (random.NextDouble() >= saveChance) return false; }
            float effectiveCatch=catchChance>=0.999f?1f:catchChance*Mathf.Lerp(1f,0.4f,Mathf.InverseLerp(16f,30f,incomingVelocity.magnitude));
            bool catchBall = slowLoose || random.NextDouble() < effectiveCatch;
            bool applied;
            if (catchBall)
            {
                applied = ball.TryCatchContact(Identity.Interaction, contact);
                if (applied) Catches++;
            }
            else
            {
                Vector3 normal = contact - handAtContact;
                if (normal.sqrMagnitude < 0.0001f) normal = -incomingVelocity;
                Vector3 reflected = Vector3.Reflect(incomingVelocity, normal.normalized) * deflectionRestitution;
                applied = ball.TryDeflectContact(Identity.Interaction, contact, reflected);
                if (applied) Deflections++;
            }
            if (applied)
            {
                recoverUntil = Time.time + recoveryTime;
                if (!slowLoose)
                {
                    presentedSavePoint = contact;
                    presentedSaveUntil = Time.time + .31f;
                    presentedShot = true;
                }
            }
            return applied;
        }

        internal bool ResolvePalmContact(Vector3 contact, Vector3 palm, Vector3 incoming, bool right, bool shot)
        {
            if (!CanContactWithPalm(out _)) return false;
            // The right hand can proceed straight into the existing possession grip.
            bool catchable = right && incoming.magnitude <= 19f &&
                (!shot || random.NextDouble() < catchChance * Mathf.Lerp(1f, .38f, Mathf.InverseLerp(12f, 19f, incoming.magnitude)));
            if (catchable && ball.TryCatchContact(Identity.Interaction, contact))
                Catches++;
            else
            {
                Vector3 normal = (contact - palm).normalized;
                if (Vector3.Dot(normal, incoming) >= -.1f) normal = -incoming.normalized;
                Vector3 outgoing = Vector3.Reflect(incoming, normal) * deflectionRestitution;
                if (!ball.TryDeflectContact(Identity.Interaction, contact, outgoing)) return false;
                Deflections++;
            }
            recoverUntil = Time.time + recoveryTime;
            presentedSavePoint = contact;
            presentedSaveUntil = Time.time + (shot ? .16f : .10f);
            presentedShot = shot;
            return true;
        }
        private void OnDrawGizmosSelected()
        {
            if (defendedGoal == null || GetComponent<SwimmerBallInteraction>()?.CarryAnchor == null) return;
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(HandPosition, saveReach);
        }
        private void OnDisable()
        {
            handLift=0f;ApplyArmLift();
            if (match != null) match.Changed -= ResetDecisions;
            Identity.Motor.SetMovementBounds(null); Identity.Motor.StopMotion();
        }
        private void ApplyArmLift()
        {
            if(!armRestCaptured)return;
            if(leftArmVisual!=null)leftArmVisual.localPosition=leftArmRest+Vector3.up*(handLift*0.75f);
            if(rightArmVisual!=null)rightArmVisual.localPosition=rightArmRest+Vector3.up*(handLift*0.75f);
        }
    }
}
