using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum BallState { Free, Held, Released }
    public enum PassDelivery { Dry, Wet, Lob }
    public enum BallReleaseKind { None, Shot, Pass, Drop, Lob }
    public enum BallActionSource { None, HumanInput, TacticalAI, RuleRestart, Verification, VoluntaryRelease, PossessionLoss }

    internal readonly struct BallTouch
    {
        internal readonly SwimmerIdentity Player;
        internal readonly bool ControlledRelease, Intentional, BlockedShot;
        internal BallTouch(SwimmerIdentity player, bool controlledRelease, bool intentional, bool blockedShot)
        { Player = player; ControlledRelease = controlledRelease; Intentional = intentional; BlockedShot = blockedShot; }
    }

    /// <summary>Exclusive ownership and hybrid 3D flight/water simulation for the single match ball.</summary>
    [DefaultExecutionOrder(-140), DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed partial class BallController : MonoBehaviour
    {
        [SerializeField] private PoolBounds poolBounds;
        [SerializeField] private PrototypeMatch match;
        private bool Live => match==null || match.CanSwim;
        private bool periodSupported,periodDropping;
        public bool PeriodSupported=>periodSupported;
        public bool PeriodDropping=>periodDropping;
        public PrototypeMatch Match => match;
        public event Action<SwimmerBallInteraction, BallReleaseKind> Released;
        public event Action Impact;
        public event Action Saved;
        public event Action<BallImpactEvent> Contact;
        public event Action<GoalMouth, Vector3, float> Netted;
        [SerializeField] private BallFlightProfile flight = new BallFlightProfile();
        public PassDelivery Delivery { get; private set; }
        public event Action<SwimmerBallInteraction> DryCaught;
        private PrototypeRoster roster;
        public ShotType CurrentShot { get; private set; }
        public int WaterBounces { get; private set; }
        private Vector3 previousAirPosition;
        private float bobStarted, bobDepth;
        private const float MinimumWaterDrag = 2.65f;
        private bool BallCanLeaveField => poolBounds != null && poolBounds.OfficiatesBallExits && match != null && match.Rules != null;
        internal BallTouch LastTouch { get; private set; }
        public bool AwaitingReplacement { get; private set; }
        private Renderer[] renderers;
        private bool[] renderMask;
        private float FloatDrag => Mathf.Max(waterDrag, MinimumWaterDrag);

        internal bool TryGetFieldExit(Vector3 from, Vector3 to, out FieldExit exit)
        {
            exit = default;
            return BallCanLeaveField && poolBounds.TryGetBallExit(from, to, Radius, out exit);
        }
        private GoalMouth nettedGoal;
        private Vector3 netVelocity;
        public Vector3 PlayPosition => Owner!=null?Owner.GetComponent<SwimmerMotor>().Position:PhysicsPosition;
        private bool Eligible(SwimmerBallInteraction player) => player != null && player.isActiveAndEnabled &&
            (player.GetComponent<SwimmerIdentity>() == null || player.GetComponent<SwimmerIdentity>().EligibleForPlay);
        private bool CanRelease(SwimmerBallInteraction player, BallReleaseKind kind) =>
            match != null && match.Rules != null ? match.Rules.CanRelease(player, kind) : Live;
        [SerializeField] private GoalMouth[] goalMouths = new GoalMouth[0];
        [Tooltip("Ball centre above the swimmer-root waterline; radius is 0.14m in the prototype.")]
        [SerializeField] private float surfaceOffset = 0.07f;
        [SerializeField, Min(0f)] private float releaseProtection = 0.6f;
        [SerializeField, Min(0f)] private float waterDrag = 0.65f;
        [SerializeField, Range(0f, 1f)] private float edgeRestitution = 0.45f;
        [SerializeField, Min(1f)] private float carrySharpness = 24f;
        [SerializeField,Min(0.1f)] private float receivingHeight = 0.65f;

        [Header("Live possession")]
        [SerializeField] private bool automaticPickup;
        [SerializeField, Min(.1f)] private float automaticPickupDistance = 1.20f;
        [SerializeField, Min(.1f)] private float automaticPickupSpeed = 2.35f;
        [SerializeField] private bool swimDribbling;
        [SerializeField, Min(.1f)] private float dribbleStartSpeed = 2.1f;
        [SerializeField, Min(.0f)] private float dribbleStopSpeed = 1.1f;
        [SerializeField, Min(0f)] private float dribbleStartDelay = .15f;
        [SerializeField, Min(.1f)] private float dribbleHeadGap = .36f;
        [SerializeField, Min(0f)] private float retreatPickupDelay = .85f;
        [SerializeField] private Transform rollingVisual;

        private Rigidbody body;
        private SphereCollider ballCollider;
        private Collider ignoredOwnerCollider;
        private float collectAfter;
        private bool simulationPaused;
        private SwimmerBallInteraction retreatDropper;
        private float retreatPickupAfter;
        private SwimmerPassing intendedReceiver;
        private float receiveAfter, receiveUntil;
        private Vector3 receivingCentre;
        private Vector3 previousPassPosition;
        private SwimmerMotor carrierMotor;
        private ShotCharge carrierCharge;
        private SwimmerStamina carrierStamina;
        private WaterVisuals.RiggedSwimmerVisual carrierVisual;
        private float swimmingTime;
        private Vector3 lastCarrierPosition, lastVisualPosition;
        private int lastVisualRevision = -1;

        public BallState State { get; private set; } = BallState.Free;
        public SwimmerBallInteraction Owner { get; private set; }
        public bool IsDribbling { get; private set; }
        internal bool UsesAutomaticPickup => automaticPickup && roster != null && match != null && match.IsPlaying;
        public float OwnershipStartedAt { get; private set; }
        public int MotionRevision { get; private set; }
        public Vector3 MotionOrigin { get; private set; }
        public BallReleaseKind ReleaseKind { get; private set; }
        public BallActionSource LastReleaseSource { get; private set; }
        public float ReleaseStartedAt { get; private set; }
        public SwimmerBallInteraction LastReleaser { get; private set; }
        public bool IsAirborne { get; private set; }
        public Vector3 LandingPoint
        {
            get
            {
                if(!IsAirborne)return PhysicsPosition;
                float gravity=Mathf.Max(0.1f,-Physics.gravity.y);Vector3 velocity=Velocity;
                float flight=(velocity.y+Mathf.Sqrt(velocity.y*velocity.y+2f*gravity*Mathf.Max(0f,PhysicsPosition.y-SurfaceHeight)))/gravity;
                Vector3 point=poolBounds.ClampPosition(PhysicsPosition+velocity*flight,Radius);point.y=SurfaceHeight;return point;
            }
        }
        public Vector3 Velocity => body != null && !body.isKinematic ? body.linearVelocity : Vector3.zero;
        public event Action<SwimmerBallInteraction, SwimmerBallInteraction> PossessionChanged;
        private bool InPlay(Vector3 point) => !BallCanLeaveField || poolBounds.ContainsPartOfBall(point, Radius);
        public bool CanCollect => InPlay(PhysicsPosition) && Live && !periodSupported && !periodDropping && isActiveAndEnabled && !simulationPaused && Owner == null && Time.time>=collectAfter && Velocity.magnitude<=flight.maximumReceivingSpeed &&
            ((!IsAirborne && State==BallState.Free) || (IsAirborne && !IsPassFlight && PhysicsPosition.y<=SurfaceHeight+receivingHeight && Velocity.y<=0f));
        private bool IsPassFlight => ReleaseKind == BallReleaseKind.Pass || ReleaseKind == BallReleaseKind.Lob;
        public int ShotSequence { get; private set; }
        public Vector3 ShotOrigin { get; private set; }
        // Eligibility outlives the short Released cooldown state, allowing long shots to score.
        public bool IsScoringShot { get; private set; }
        public bool CanScoreGoal { get; private set; }
        private GoalAction goalAction;
        private bool opposingTouch;

        public GoalAttribution AttributeGoal(PrototypeTeam team) => goalAction.Attribute(team,opposingTouch);

        private void NotePlayerContact(SwimmerIdentity player, bool intentional = false)
        {
            if (player == null || !player.EligibleForPlay) return;
            if (LastReleaser != null && LastReleaser == player.Interaction && ignoredOwnerCollider != null) return;
            bool opponent = LastReleaser != null && LastReleaser.TryGetComponent<SwimmerIdentity>(out var releaser)
                && player.Team != releaser.Team;
            if (CanScoreGoal && goalAction.Actor != null && player.Team != goalAction.Team) opposingTouch = true;
            LastTouch = new BallTouch(player, false, intentional, opponent && ReleaseKind == BallReleaseKind.Shot);
        }
        public SwimmerBallInteraction IntendedReceiver => intendedReceiver != null ? intendedReceiver.Interaction : null;
        public float SurfaceHeight => poolBounds.Waterline + surfaceOffset;
        // Legacy tactical range budget for airborne shot selection. Floating motion uses FloatDrag.
        public float EstimateFlatTravelRange(float speed) => waterDrag>0f?Mathf.Max(0f,speed)/waterDrag:Mathf.Infinity;
        public float Radius => ballCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        public Vector3 PhysicsPosition => body != null ? body.position : transform.position;

        private void Awake()
        {
            roster = match != null ? match.GetComponent<PrototypeRoster>() : null;
            if (roster == null && match != null) roster = FindAnyObjectByType<PrototypeRoster>();
            body = GetComponent<Rigidbody>();
            ballCollider = GetComponent<SphereCollider>();
            if (poolBounds == null)
            {
                Debug.LogError("BallController requires an assigned PoolBounds.", this);
                enabled = false;
                return;
            }
            body.useGravity = false;
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = FloatDrag;
            body.position = SurfacePosition(body.position);
            renderers = GetComponentsInChildren<Renderer>(true);
            renderMask = new bool[renderers.Length];
            EnableBallCollider();
            if (match != null) match.PauseChanged += CancelThrowOnPause;
        }

        public bool TryCollect(SwimmerBallInteraction claimant)
        {
            if (!CanCollect || !Eligible(claimant) || PickupSuppressed(claimant) || claimant.CarryAnchor == null)
                return false;
            // Compare both physics poses; an interpolated render Transform can lag after movement/reset.
            claimant.TryGetComponent<SwimmerMotor>(out var motor);
            Vector3 separation = body.position - (motor != null ? motor.Position : claimant.transform.position);
            separation.y = 0f;
            if (separation.sqrMagnitude > claimant.PickupDistance * claimant.PickupDistance)
                return false;
            AssignOwner(claimant);
            return true;
        }

        private void AssignOwner(SwimmerBallInteraction claimant, Vector3? contact = null)
        {
            Vector3 pickupPoint = contact ?? transform.position;
            Vector3 arrivingVelocity = contact.HasValue ? Velocity : Vector3.zero;
            RestoreSurfacePhysics();
            SwimmerBallInteraction previousOwner = Owner;
            // The ball is the only ownership authority: a second claim sees Held immediately.
            intendedReceiver = null;
            retreatDropper = null;
            RestoreOwnerCollision();
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            ballCollider.enabled = false;
            if (contact.HasValue) body.position = transform.position = contact.Value;
            Owner = claimant;
            ClearDribble();
            if (swimDribbling || professionalThrows)
            {
                carrierMotor = claimant.GetComponent<SwimmerMotor>();
                carrierCharge = claimant.GetComponent<ShotCharge>();
                carrierStamina = claimant.GetComponent<SwimmerStamina>();
                carrierVisual = claimant.GetComponentInChildren<WaterVisuals.RiggedSwimmerVisual>();
                lastCarrierPosition = claimant.transform.position;
            }
            LastTouch = new BallTouch(claimant.GetComponent<SwimmerIdentity>(), false, true, false);
            OwnershipStartedAt = Time.time;
            State = BallState.Held;
            IsScoringShot = CanScoreGoal = false;
            if (UsesEvaluatedGrip && Live && (match == null || match.IsPlaying))
                BeginGather(pickupPoint, arrivingVelocity);
            NoteMotionOrigin(body.position);
            NotifyPossessionChanged(previousOwner);
        }

        public bool TryAwardRestart(SwimmerBallInteraction claimant, Vector3? mark = null)
        {
            if(match==null || match.IsPaused || !Eligible(claimant))return false;
            bool centre=match.IsCentreRestart && match.CentreTaker?.Interaction==claimant;
            bool foul=match.Rules!=null && match.Rules.IsRulePhase && match.Rules.Taker?.Interaction==claimant;
            if(!centre && !foul)return false;
            ResetForRestart(mark ?? claimant.GetComponent<SwimmerMotor>().Position);
            AssignOwner(claimant, mark.HasValue ? PhysicsPosition : null);
            return true;
        }

        public bool TryShoot(SwimmerBallInteraction claimant, float speed, BallActionSource source = BallActionSource.HumanInput)
            => TryShoot(claimant,speed,speed>=18f?ShotType.Powerful:ShotType.Normal,source);

        public bool TryShoot(SwimmerBallInteraction claimant, float speed, ShotType shot, BallActionSource source = BallActionSource.HumanInput)
        {
            if (!CanRequestThrow(claimant, BallReleaseKind.Shot, source))
                return false;
            var motor = claimant.GetComponent<SwimmerMotor>();
            Vector3 direction = source == BallActionSource.HumanInput && match != null && match.IsPlaying
                ? motor.ControlAim : motor.Facing;
            if (CanScheduleThrow)
            {
                QueueThrow(new PendingThrow(BallReleaseKind.Shot, source, direction, speed, shot: shot));
                return true;
            }
            return LaunchShot(claimant, direction, speed, shot, ThrowOrigin(claimant, direction), source);
        }

        private bool LaunchShot(SwimmerBallInteraction claimant, Vector3 direction, float speed, ShotType shot, Vector3 origin, BallActionSource source)
        {
            bool penaltyThrow = match != null && match.Phase == MatchPhase.PenaltyReady;
            GoalMouth goal = null;
            var identity = claimant.GetComponent<SwimmerIdentity>();
            foreach (var candidate in goalMouths)
                if (candidate != null && identity != null && candidate.ScoringTeam == identity.Team) { goal = candidate; break; }
            if (penaltyThrow)
            {
                goal = match.Rules.PenaltyGoal;
                Vector3 local = goal.transform.InverseTransformPoint(origin);
                local.z = -5f;
                // Keep the five-metre trajectory aligned with the taker's aim.
                local.x = goal.transform.InverseTransformPoint(identity.Motor.Position).x;
                origin = goal.transform.TransformPoint(local);
            }
            CurrentShot = shot;
            Vector3 horizontal = direction * Mathf.Max(.1f, speed);
            float approach = goal != null ? Vector3.Dot(horizontal, goal.transform.forward) : 0f;
            float depth = goal != null ? -goal.transform.InverseTransformPoint(origin).z : 0f;
            float time = Mathf.Clamp(depth / Mathf.Max(1f, approach), .1f, 1.35f);
            float top = goal != null ? goal.transform.position.y : poolBounds.Waterline;
            float targetHeight = penaltyThrow ? top + match.Rules.PenaltyTargetHeight : top + .43f;
            float vertical = approach > 2f && depth > 0 ? BallTrajectory.Velocity(origin,new Vector3(origin.x,targetHeight,origin.z),time).y : .55f;
            if (shot == ShotType.Skip && !penaltyThrow)
            {
                float landing = Mathf.Clamp(time * .47f, .13f, .5f);
                vertical = BallTrajectory.Velocity(origin,new Vector3(origin.x,SurfaceHeight,origin.z),landing).y;
            }
            else if (shot == ShotType.High && !penaltyThrow)
            {
                float highTime = Mathf.Clamp(time * 1.32f + .07f, .28f, 1.4f);
                horizontal *= time / highTime;
                vertical = BallTrajectory.Velocity(origin,new Vector3(origin.x,targetHeight,origin.z),highTime).y;
            }
            Release(claimant, horizontal.magnitude, direction, BallReleaseKind.Shot, origin);
            BeginFlight(origin, horizontal + Vector3.up * vertical);
            CompleteRelease(claimant,BallReleaseKind.Shot,source);
            return true;
        }

        private Vector3 ThrowOrigin(SwimmerBallInteraction player, Vector3 forward)
        {
            var motor = player.GetComponent<SwimmerMotor>();
            if (match != null && match.Rules != null && match.Rules.BoundaryRestart is BoundaryRestartAward restart)
            {
                Vector3 mark = poolBounds.ClampPosition(restart.Mark, Radius);
                mark.y = poolBounds.Waterline + .57f;
                return mark;
            }
            Vector3 socket = motor != null ? motor.Position : player.transform.position;
            socket += forward * .36f + Vector3.Cross(Vector3.up,forward) * .26f;
            socket = poolBounds.ClampPosition(socket, Radius);
            socket.y = poolBounds.Waterline + .57f;
            return socket;
        }

        public Vector3 RaisedHand(SwimmerBallInteraction player, Vector3 from)
        {
            var motor = player.GetComponent<SwimmerMotor>();
            return PassHand(motor.Position, motor.Facing, from);
        }

        private Vector3 PassHand(Vector3 root, Vector3 facing, Vector3 from)
        {
            Vector3 toward = Vector3.ProjectOnPlane(from-root,Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up,facing);
            Vector3 hand = root + toward * .42f + right * .19f;
            hand.y = ReceivingHandHeight;
            return hand;
        }

        private float ReceivingHandHeight => poolBounds.Waterline + .57f;

        internal bool TryReceiveIntent(SwimmerBallInteraction player, out Vector3 facing, out Vector3 centre)
        {
            facing = Vector3.zero;
            centre = player.transform.position;
            if (preparation.Receiver != null && preparation.Receiver.Interaction == player &&
                preparation.Delivery != PassDelivery.Wet && Owner != null)
            {
                facing = Vector3.ProjectOnPlane(Owner.transform.position - centre, Vector3.up).normalized;
                return facing.sqrMagnitude > .5f;
            }
            if (!IsAirborne || Owner != null || player != IntendedReceiver || Delivery == PassDelivery.Wet ||
                Velocity.sqrMagnitude < .3f) return false;
            facing = Vector3.ProjectOnPlane(-Velocity, Vector3.up).normalized;
            if (Vector3.Dot(centre - PhysicsPosition, Velocity) < -.25f) return false;
            centre = receivingCentre;
            return facing.sqrMagnitude > .5f;
        }

        public bool ShowReceivingHand(SwimmerBallInteraction player)
        {
            if (!IsAirborne || Owner != null || !IsPassFlight || Delivery == PassDelivery.Wet ||
                player == LastReleaser || !Eligible(player)) return false;
            Vector3 toward = player.transform.position - PhysicsPosition;
            return toward.sqrMagnitude < 8f && Vector3.Dot(toward,Velocity) > 0f &&
                (player == IntendedReceiver || LastReleaser != null &&
                 player.GetComponent<SwimmerIdentity>().Team != LastReleaser.GetComponent<SwimmerIdentity>().Team);
        }

        private void BeginFlight(Vector3 origin, Vector3 velocity)
        {
            IsAirborne=true;body.constraints=RigidbodyConstraints.None;body.useGravity=true;body.linearDamping=flight.airDrag;
            body.position=origin;body.linearVelocity=velocity;previousAirPosition=origin;NoteMotionOrigin(origin);
        }
        public Vector3 PredictPosition(float seconds)
        {
            seconds=Mathf.Clamp(seconds,0f,flight.maximumPrediction);
            if(Owner!=null)return PlayPosition+Owner.GetComponent<SwimmerMotor>().Velocity*seconds;
            if(IsAirborne)
            {
                float gravity = Mathf.Max(.1f, -Physics.gravity.y);
                Vector3 point = PhysicsPosition;
                Vector3 velocity = Velocity;
                for (int contacts = WaterBounces; contacts <= flight.maximumSkips; contacts++)
                {
                    float landing = BallTrajectory.TimeToHeight(point.y - SurfaceHeight, velocity.y, gravity);
                    if (seconds < landing) return ClampBallPoint(BallTrajectory.Position(point, velocity, seconds, true));
                    point = BallTrajectory.Position(point, velocity, landing, true);
                    point.y = SurfaceHeight;
                    seconds -= landing;
                    velocity = WaterResponse(velocity + Physics.gravity * landing, point, contacts, out bool skip);
                    if (!skip) return ClampBallPoint(point + FlatDisplacement(velocity, seconds));
                }
                return ClampBallPoint(point);
            }
            return ClampBallPoint(PhysicsPosition+FlatDisplacement(Velocity,seconds));
        }
        private Vector3 FlatDisplacement(Vector3 velocity,float seconds)
        {float drag=FloatDrag;return velocity*((1f-Mathf.Exp(-drag*seconds))/drag);}
        private Vector3 ClampBallPoint(Vector3 point)
        {Vector3 clamped=poolBounds.ClampPosition(point,Radius);clamped.y=Mathf.Max(SurfaceHeight,point.y);var pocket=FindPocket(point);return pocket!=null?pocket.ClampPocket(point,Radius):clamped;}

        public StealResult TrySteal(SwimmerBallInteraction claimant, float reach, float facingDot, float protection, float minimumExposure)
        {
            if (!Live || !isActiveAndEnabled || simulationPaused || State != BallState.Held || Owner == null || claimant == null || !claimant.isActiveAndEnabled || claimant.CarryAnchor == null)
                return StealResult.Unavailable;
            var attacker = claimant.GetComponent<SwimmerIdentity>();
            var carrier = Owner.GetComponent<SwimmerIdentity>();
            if (attacker == null || carrier == null || !attacker.EligibleForPlay || !carrier.EligibleForPlay || attacker.Team == carrier.Team) return StealResult.NoOpponent;
            // Keep the near-goal keeper protection; a keeper carrying into the pool uses normal steal rules.
            if (!carrier.IsFieldPlayer && carrier.TryGetComponent<PrototypeGoalkeeper>(out var keeper) && keeper.PossessionProtected) return StealResult.Protected;
            Vector3 delta = carrier.Motor.Position - attacker.Motor.Position;
            delta.y = 0f;
            if (delta.sqrMagnitude > reach * reach) return StealResult.OutOfRange;
            if (delta.sqrMagnitude < 0.01f || Vector3.Dot(claimant.transform.forward, delta.normalized) < facingDot) return StealResult.WrongFacing;
            if (Time.time - OwnershipStartedAt < protection) return StealResult.Protected;
            if (Vector3.Dot(Owner.transform.forward, -delta.normalized) < minimumExposure) return StealResult.Shielded;
            AssignOwner(claimant);
            return StealResult.Success;
        }

        public bool TryPass(SwimmerBallInteraction claimant, Vector3 direction, float speed,
            SwimmerPassing receiver, float minimumFlightTime, float receivingWindow, PassDelivery delivery = PassDelivery.Dry,
            BallActionSource source = BallActionSource.HumanInput)
        {
            if (!CanRequestThrow(claimant, BallReleaseKind.Pass, source))
                return false;
            if (match != null && match.IsCentreRestart && (receiver == null || !match.IsCentreReceiver(receiver.Identity))) return false;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (direction.sqrMagnitude < .5f || speed <= 0f || (receiver != null && !receiver.CanReceiveFrom(claimant))) return false;
            if (CanScheduleThrow)
            {
                QueueThrow(new PendingThrow(BallReleaseKind.Pass, source, direction, speed, receiver,
                    minimumFlightTime, receivingWindow, delivery));
                return true;
            }
            LaunchDirectedPass(claimant, direction, speed, receiver, minimumFlightTime, receivingWindow,
                delivery, ThrowOrigin(claimant, direction), source);
            return true;
        }

        private void LaunchDirectedPass(SwimmerBallInteraction claimant, Vector3 direction, float speed,
            SwimmerPassing receiver, float minimumFlightTime, float receivingWindow, PassDelivery delivery, Vector3 origin, BallActionSource source)
        {
            bool evaluatedDryTarget = professionalThrows && delivery == PassDelivery.Dry && (match == null || match.IsPlaying);
            Vector3 target = receiver != null
                ? PassHand(receiver.Identity.Motor.Position, receiver.Identity.Motor.Facing, origin)
                : origin + direction * 5f;
            float range = Vector3.ProjectOnPlane(target-origin, Vector3.up).magnitude;
            float time = Mathf.Clamp(range / Mathf.Max(speed,10.5f), .30f, 1.12f);
            if (receiver != null)
            {
                var motor = receiver.Identity.Motor;
                Vector3 movement = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up);
                Vector3 lead = Vector3.ClampMagnitude(movement * time * .82f, 1.65f);
                if (evaluatedDryTarget)
                {
                    Vector3 toward = Vector3.ProjectOnPlane(origin - motor.Position, Vector3.up).normalized;
                    // The recipient turns and checks momentum. Lead the attainable root, then
                    // put the target in front of the anticipated throwing-side shoulder.
                    float carryMomentum = Mathf.Lerp(.08f, .22f, Mathf.InverseLerp(-.4f, .65f, Vector3.Dot(motor.Facing, toward)));
                    lead = Vector3.ClampMagnitude(movement * carryMomentum, .58f);
                    receivingCentre = motor.Position + lead;
                    target = PassHand(receivingCentre, toward, origin);
                    range = Vector3.ProjectOnPlane(target - origin, Vector3.up).magnitude;
                    time = Mathf.Clamp(range / Mathf.Max(speed, 10.5f), .30f, 1.12f);
                }
                else
                {
                    target += lead;
                    receivingCentre = motor.Position + lead;
                }
                if (delivery == PassDelivery.Wet)
                    target += movement.sqrMagnitude > .25f ? movement.normalized * .64f : -direction * .38f;
            }
            target = poolBounds.ClampPosition(target, Radius);
            // The receiving hand keeps its established height even when the throw leaves a lower real palm.
            target.y = delivery == PassDelivery.Wet ? SurfaceHeight : receiver == null ? origin.y : ReceivingHandHeight;
            LaunchPass(claimant,origin,target,time,receiver,delivery,minimumFlightTime,receivingWindow,source);
        }

        public bool TryLob(SwimmerBallInteraction claimant, Vector3 direction, float distance, float flightDuration, SwimmerPassing receiver,
            BallActionSource source = BallActionSource.HumanInput)
        {
            if (!CanRequestThrow(claimant, BallReleaseKind.Lob, source) || flightDuration < .2f || distance <= 0f ||
                (receiver != null && !receiver.CanReceiveFrom(claimant))) return false;
            direction = Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            if (direction.sqrMagnitude < .5f) return false;
            if (CanScheduleThrow)
            {
                QueueThrow(new PendingThrow(BallReleaseKind.Lob, source, direction, distance, receiver, flightDuration));
                return true;
            }
            LaunchLob(claimant, direction, distance, flightDuration, receiver, ThrowOrigin(claimant, direction), source);
            return true;
        }

        private void LaunchLob(SwimmerBallInteraction claimant, Vector3 direction, float distance,
            float flightDuration, SwimmerPassing receiver, Vector3 origin, BallActionSource source)
        {
            if (receiver != null) receivingCentre = receiver.Identity.Motor.Position;
            Vector3 target = receiver != null ? PassHand(receiver.Identity.Motor.Position, receiver.Identity.Motor.Facing, origin) +
                Vector3.ClampMagnitude(receiver.Identity.Motor.Velocity * flightDuration * .7f,1.65f) : origin + direction * distance;
            target = poolBounds.ClampPosition(target,Radius);
            target.y = receiver != null ? ReceivingHandHeight : SurfaceHeight;
            LaunchPass(claimant,origin,target,flightDuration,receiver,PassDelivery.Lob,.1f,flightDuration + .4f,source);
        }

        private void LaunchPass(SwimmerBallInteraction player, Vector3 origin, Vector3 target, float duration,
            SwimmerPassing receiver, PassDelivery delivery, float minimumFlight, float window, BallActionSource source)
        {
            var kind = delivery == PassDelivery.Lob ? BallReleaseKind.Lob : BallReleaseKind.Pass;
            Vector3 velocity = BallTrajectory.Velocity(origin,target,duration);
            var planar = Vector3.ProjectOnPlane(velocity,Vector3.up);
            Release(player,planar.magnitude,planar.normalized,kind,origin);
            Delivery = delivery;
            intendedReceiver = receiver;
            receiveAfter = Time.time + Mathf.Max(.08f, minimumFlight);
            receiveUntil = Time.time + Mathf.Max(duration+.16f, window);
            previousPassPosition = origin;
            BeginFlight(origin,velocity);
            CompleteRelease(player,kind,source);
        }

        private void RestoreSurfacePhysics(Vector3? at = null)
        {
            IsAirborne=false;
            bobDepth = 0f;
            if(body==null)return;
            // Set the water pose before locking Y: PhysX otherwise retains the underwater landing pose.
            var interpolation=body.interpolation;body.interpolation=RigidbodyInterpolation.None;
            body.useGravity=false;body.constraints=RigidbodyConstraints.None;
            if (poolBounds != null)
            {
                Vector3 position = at ?? body.position;
                position.y = SurfaceHeight;
                body.position = transform.position = position;
            }
            if(!body.isKinematic){Vector3 velocity=body.linearVelocity;velocity.y=0f;body.linearVelocity=velocity;}
            Physics.SyncTransforms();
            body.constraints=RigidbodyConstraints.FreezePositionY;body.linearDamping=FloatDrag;
            body.interpolation=interpolation;
        }

        public void Drop(SwimmerBallInteraction claimant)
        {
            // Scene teardown can remove the pool before a held swimmer's OnDisable callback.
            // There is no remaining field in which to spawn a physical drop in that case.
            if (poolBounds == null) return;
            if ((match == null || !match.IsPaused) && State == BallState.Held && Owner == claimant && claimant != null)
            {
                Vector3 held = IsDribbling || UsesEvaluatedGrip ? transform.position : claimant.HeldBallPoint;
                Vector3 origin = poolBounds.ClampPosition(held, Radius);
                origin.y = Mathf.Max(held.y, SurfaceHeight);
                Vector3 drift = Vector3.ClampMagnitude(claimant.GetComponent<SwimmerMotor>().Velocity, .55f);
                Release(claimant, 0f, claimant.transform.forward, BallReleaseKind.Drop, origin);
                if (origin.y > SurfaceHeight + .02f) BeginFlight(origin, drift);
                CompleteRelease(claimant,BallReleaseKind.Drop,BallActionSource.PossessionLoss);
            }
        }

        internal void TryReleaseForRetreat(SwimmerBallInteraction claimant)
        {
            if (match == null || !match.IsPlaying || match.AwaitingOpeningPossession ||
                simulationPaused || HasPendingThrow ||
                !CanPerformThrow(claimant, BallReleaseKind.Drop, BallActionSource.VoluntaryRelease))
                return;

            Vector3 origin = poolBounds.ClampPosition(transform.position, Radius);
            origin.y = Mathf.Max(transform.position.y, SurfaceHeight);
            Vector3 forward = claimant.GetComponent<SwimmerMotor>().Facing;
            Release(claimant, 0f, forward, BallReleaseKind.Drop, origin);
            transform.position = origin;
            retreatDropper = claimant;
            retreatPickupAfter = Time.time + retreatPickupDelay;
            collectAfter = Time.time;
            State = BallState.Free;
            if (origin.y > SurfaceHeight + .002f) BeginFlight(origin, Vector3.zero);
            CompleteRelease(claimant, BallReleaseKind.Drop, BallActionSource.VoluntaryRelease);
        }

        private bool PickupSuppressed(SwimmerBallInteraction player) =>
            player == retreatDropper && Time.time < retreatPickupAfter;

        internal bool CanRecoverLooseBall => CanCollect && !IsAirborne && nettedGoal == null &&
            Velocity.sqrMagnitude <= automaticPickupSpeed * automaticPickupSpeed &&
            (!IsScoringShot || ReleaseKind != BallReleaseKind.Shot || Velocity.sqrMagnitude <= .25f && FindPocket(PhysicsPosition) == null);

        internal bool IsRecoveryCandidate(SwimmerBallInteraction player) =>
            Eligible(player) && !PickupSuppressed(player);

        private void Release(SwimmerBallInteraction claimant, float speed, Vector3 launchDirection, BallReleaseKind kind, Vector3? actualOrigin = null)
        {
            RestoreSurfacePhysics();
            WaterBounces=0;nettedGoal=null;
            intendedReceiver = null;
            Vector3 direction = Vector3.ProjectOnPlane(launchDirection, Vector3.up).normalized;
            // Start beyond the capsule; at pool edges the position is clamped and collision stays
            // ignored until separation, so the shot cannot bounce off its own swimmer.
            Vector3 releasePoint = speed > 0f
                ? claimant.GetComponent<SwimmerMotor>().Position + direction * claimant.ReleaseDistance
                : transform.position;
            Vector3 spawn = actualOrigin ?? poolBounds.ClampPosition(releasePoint, Radius);
            if (!actualOrigin.HasValue) spawn.y = SurfaceHeight;
            body.position = spawn;
            if (IsEvaluatedRelease) transform.position = spawn;
            ShotOrigin = spawn;
            NoteMotionOrigin(spawn);
            ReleaseKind = kind;
            ReleaseStartedAt = Time.time;
            LastReleaser = claimant;
            LastTouch = new BallTouch(claimant.GetComponent<SwimmerIdentity>(), kind != BallReleaseKind.Drop, true, false);
            bool legalRelease = match == null || !match.IsCentreRestart && match.Phase != MatchPhase.FreeThrowReady ||
                kind == BallReleaseKind.Shot && match.Rules != null && match.Rules.BoundaryRestart.HasValue;
            IsScoringShot = speed > 0f && legalRelease;
            CanScoreGoal = IsScoringShot || kind == BallReleaseKind.Drop && match != null && match.IsPlaying;
            goalAction = CanScoreGoal ? new GoalAction(claimant,kind) : default;
            opposingTouch = false;
            if (CanScoreGoal) ShotSequence++;
            Owner = null;
            ClearDribble();
            State = BallState.Released;
            collectAfter = Time.time + releaseProtection;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            EnableBallCollider();
            ignoredOwnerCollider = claimant.GetComponent<Collider>();
            if (ignoredOwnerCollider != null)
                Physics.IgnoreCollision(ballCollider, ignoredOwnerCollider, true);
            body.linearVelocity = direction * speed;
            body.angularVelocity = Vector3.zero;
        }

        private void CompleteRelease(SwimmerBallInteraction claimant, BallReleaseKind kind, BallActionSource source)
        {
            LastReleaseSource = source;
            NotifyPossessionChanged(claimant);
            Released?.Invoke(claimant,kind);
        }

        private Vector3 SurfacePosition(Vector3 position)
        {
            Vector3 clamped = poolBounds.ClampPosition(position, Radius);
            GoalMouth pocket = FindPocket(position);
            if (pocket != null) clamped = pocket.ClampPocket(position, Radius);
            clamped.y = SurfaceHeight;
            return clamped;
        }

        private GoalMouth FindPocket(Vector3 position)
        {
            if (goalMouths == null) return null;
            foreach (GoalMouth mouth in goalMouths)
                if (mouth != null && mouth.isActiveAndEnabled && mouth.AllowsPocket(position, Radius)) return mouth;
            return null;
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;
            if (paused)
            {
                CancelPendingThrow();
                if (!body.isKinematic) {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
                body.isKinematic = true;
            }
        }

        private void EnableBallCollider()
        {
            ballCollider.enabled = true;
            if (!BallCanLeaveField) return;
            foreach (var goal in goalMouths)
                if (goal != null && goal.TryGetComponent<PrototypeGoalView>(out var view)) view.IgnoreBallBarrier(ballCollider);
        }

        internal void RetireForReplacement()
        {
            if (AwaitingReplacement) return;
            RestoreOwnerCollision();
            SetSimulationPaused(true);
            ballCollider.enabled = false;
            IsAirborne = IsScoringShot = CanScoreGoal = false;
            intendedReceiver = null;
            AwaitingReplacement = true;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderMask[i] = renderers[i].forceRenderingOff;
                renderers[i].forceRenderingOff = true;
            }
        }

        private void RestoreBallRenderers()
        {
            if (!AwaitingReplacement) return;
            AwaitingReplacement = false;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].forceRenderingOff = renderMask[i];
        }

        public void ResetForRestart(Vector3 position)
        {
            ResetFree();
            collectAfter = 0f;
            body.angularVelocity = Vector3.zero;
            Vector3 spawn = poolBounds.ClampPosition(position, Radius);
            spawn.y = SurfaceHeight;
            body.position = spawn;
            ShotOrigin = spawn;
            NoteMotionOrigin(spawn);
        }
        public void PreparePeriodBall(Vector3 position)
        {
            if(match==null||match.Phase!=MatchPhase.SprintSetup)return;
            ResetForRestart(position);periodSupported=true;simulationPaused=true;
            body.isKinematic=true;body.interpolation=RigidbodyInterpolation.None;
            body.constraints=RigidbodyConstraints.None;body.useGravity=false;ballCollider.enabled=false;
            body.position=position;transform.position=position;Physics.SyncTransforms();NoteMotionOrigin(position);
        }
        public void ReleasePeriodBall()
        {
            if(match==null||!match.IsSprintRace||match.IsPaused||!periodSupported)return;
            periodSupported=false;periodDropping=true;simulationPaused=false;
            body.isKinematic=false;body.interpolation=RigidbodyInterpolation.Interpolate;EnableBallCollider();
            ReleaseKind=BallReleaseKind.Drop;IsScoringShot=false;CurrentShot=ShotType.Normal;
            BeginFlight(body.position,Vector3.zero);
        }

        /// <summary>Receives a swept contact already validated by the interception observer, including released-shot cooldown.</summary>
        public bool TryCatchContact(SwimmerBallInteraction claimant, Vector3 contact)
        {
            var identity = claimant != null ? claimant.GetComponent<SwimmerIdentity>() : null;
            if (!(Live || match!=null && !match.IsPaused && match.Phase==MatchPhase.PenaltyFlight) || !isActiveAndEnabled || simulationPaused || Owner != null || State == BallState.Held || identity == null || !identity.EligibleForPlay || identity.IsFieldPlayer || !claimant.isActiveAndEnabled || claimant.CarryAnchor == null)
                return false;
            Vector3 incoming=Velocity;
            AssignOwner(claimant,contact);
            Saved?.Invoke();
            Contact?.Invoke(new BallImpactEvent(BallImpactKind.Save,contact,incoming,Vector3.zero));
            return true;
        }

        public bool TryDeflectContact(SwimmerBallInteraction claimant, Vector3 contact, Vector3 outgoingVelocity)
        {
            var identity = claimant != null ? claimant.GetComponent<SwimmerIdentity>() : null;
            if (!(Live || match!=null && !match.IsPaused && match.Phase==MatchPhase.PenaltyFlight) || !isActiveAndEnabled || simulationPaused || Owner != null || State == BallState.Held || identity == null || !identity.EligibleForPlay || identity.IsFieldPlayer || !claimant.isActiveAndEnabled) return false;
            if (!InPlay(contact)) return false;
            NotePlayerContact(identity, true);
            intendedReceiver = null; RestoreOwnerCollision();
            Vector3 incoming=Velocity;
            if(contact.y>SurfaceHeight+0.04f || outgoingVelocity.y>0.1f)BeginFlight(contact,outgoingVelocity);
            else
            {
                contact.y=SurfaceHeight;
                RestoreSurfacePhysics(contact);
                outgoingVelocity=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(outgoingVelocity,Vector3.up),3f);
                body.linearVelocity=outgoingVelocity;
            }
            body.angularVelocity = Vector3.zero;
            State = BallState.Released; collectAfter = Time.time + 0.15f;
            // Keep the original shot sequence/eligibility. A real deflection may still enter the net.
            NoteMotionOrigin(contact);
            Saved?.Invoke();
            Contact?.Invoke(new BallImpactEvent(BallImpactKind.Save,contact,incoming,outgoingVelocity));
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(match!=null && !match.BallInMotion)return;
            var athlete=collision.rigidbody != null ? collision.rigidbody.GetComponent<SwimmerIdentity>() : null;
            NotePlayerContact(athlete);
            Impact?.Invoke();
            if(collision.contactCount>0)Contact?.Invoke(new BallImpactEvent(BallImpactKind.Frame,collision.GetContact(0).point,collision.relativeVelocity,Velocity));
        }

        private void NoteMotionOrigin(Vector3 position) { MotionRevision++; MotionOrigin = position; }

        private Vector3 WaterContact(Vector3 end, out float fraction)
        {
            fraction = previousAirPosition.y > end.y
                ? Mathf.Clamp01((previousAirPosition.y - SurfaceHeight) / (previousAirPosition.y - end.y)) : 1f;
            var contact = Vector3.Lerp(previousAirPosition, end, fraction);
            contact.y = SurfaceHeight;
            return contact;
        }

        private void ResolveWaterImpact(Vector3 contact, float fraction)
        {
            periodDropping=false;
            Vector3 incoming=Velocity;
            Vector3 outgoing=WaterResponse(incoming,contact,WaterBounces,out bool skip);
            if(skip)
            {
                WaterBounces++;
                // Integrate only the remainder after the actual surface crossing, preserving the travelled segment.
                float rest=Time.fixedDeltaTime*(1f-fraction);
                Vector3 next=BallTrajectory.Position(contact,outgoing,rest,true);next.y=Mathf.Max(next.y,SurfaceHeight+0.002f);
                BeginFlight(next,outgoing+Physics.gravity*rest);NoteMotionOrigin(contact);previousPassPosition=next;
                Vector3 along = Vector3.ProjectOnPlane(outgoing, Vector3.up).normalized;
                body.angularVelocity = Vector3.Cross(Vector3.up, along) * Mathf.Min(24f, outgoing.magnitude * 1.2f);
            }
            else
            {
                RestoreSurfacePhysics(contact);
                body.linearVelocity=outgoing;
                bobStarted = Time.time;
                bobDepth = Mathf.Lerp(.008f, .033f, Mathf.InverseLerp(.8f, 8f, -incoming.y));
                State=BallState.Free;
                previousPassPosition=contact;
            }
            Contact?.Invoke(new BallImpactEvent(skip?BallImpactKind.Skip:BallImpactKind.Water,contact,incoming,outgoing));
        }
        private Vector3 WaterResponse(Vector3 incoming, Vector3 contact, int bounces, out bool skip)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(incoming, Vector3.up);
            float speed = horizontal.magnitude;
            float down = Mathf.Max(0f, -incoming.y);
            float slope = down / Mathf.Max(.01f, speed);
            skip = ReleaseKind == BallReleaseKind.Shot && CurrentShot == ShotType.Skip && bounces < flight.maximumSkips &&
                speed > flight.skipMinimumSpeed && down > (bounces == 0 ? .64f : 1.15f) &&
                slope < flight.skipMaximumSlope && FindPocket(contact) == null;
            if (!skip) return SettledVelocity(horizontal);
            float steepness = Mathf.InverseLerp(.13f, flight.skipMaximumSlope, slope);
            Vector3 outgoing = horizontal * (flight.skipRetention * Mathf.Lerp(1f, .77f, steepness));
            outgoing.y = Mathf.Min(down * flight.skipRestitution * Mathf.Lerp(1.17f, .80f, steepness), 2.55f);
            skip = outgoing.y >= .27f;
            if (!skip) return SettledVelocity(horizontal);
            return outgoing;
        }

        private Vector3 SettledVelocity(Vector3 incoming)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(incoming, Vector3.up);
            if (IsPassFlight)
            {
                bool wet = Delivery == PassDelivery.Wet;
                return Vector3.ClampMagnitude(horizontal * flight.lobWaterRetention, wet ? 1.9f : 2.35f);
            }
            // An ordinary shot sheds most of its planar speed. Vertical motion is fully absorbed.
            return Vector3.ClampMagnitude(horizontal * flight.shotWaterRetention, 3f);
        }

        public void CaptureGoal(GoalMouth goal)
        {
            if(goal==null || nettedGoal!=null || Owner!=null)return;
            float speed=Velocity.magnitude;netVelocity=Velocity*0.28f;nettedGoal=goal;
            IsScoringShot=CanScoreGoal=false;intendedReceiver=null;IsAirborne=false;body.isKinematic=true;
            Netted?.Invoke(goal,body.position,speed);
            Contact?.Invoke(new BallImpactEvent(BallImpactKind.Net,body.position,netVelocity/0.28f,netVelocity));
        }
        private void AdvanceNet()
        {
            Vector3 next=body.position+netVelocity*Time.fixedDeltaTime;
            Vector3 local=nettedGoal.transform.InverseTransformPoint(next);
            local.x=Mathf.Clamp(local.x,-nettedGoal.Width*0.5f+Radius,nettedGoal.Width*0.5f-Radius);
            local.y=Mathf.Clamp(local.y,SurfaceHeight-nettedGoal.transform.position.y,nettedGoal.Height-Radius);
            local.z=Mathf.Clamp(local.z,Radius,nettedGoal.Depth-Radius);
            body.MovePosition(nettedGoal.transform.TransformPoint(local));
            netVelocity*=Mathf.Exp(-6f*Time.fixedDeltaTime);
            netVelocity.y-=5f*Time.fixedDeltaTime;
        }

        private void FixedUpdate()
        {
            if(nettedGoal!=null){AdvanceNet();return;}
            if (simulationPaused) return;
            if (State == BallState.Held)
            {
                if (!Eligible(Owner))
                    ResetFree();
                else if (swimDribbling)
                    UpdateDribble();
                return;
            }

            if (IsAirborne && TryReceivePass()) return;
            if(IsAirborne)
            {
                if (ignoredOwnerCollider != null && !ignoredOwnerCollider.bounds.Intersects(ballCollider.bounds))
                    RestoreOwnerCollision();
                if(body.position.y<=SurfaceHeight && body.linearVelocity.y<=0f)
                {
                    Vector3 contact = WaterContact(body.position, out float fraction);
                    // An exit before the splash remains a swept airborne event for the match.
                    if (TryGetFieldExit(previousAirPosition, contact, out _)) return;
                    ResolveWaterImpact(contact, fraction);
                    if(IsAirborne)return;
                }
                else
                {
                    Vector3 position = body.position;
                    previousAirPosition = position;
                    if (BallCanLeaveField) return;
                    // Containment remains available in older practice scenes.
                    Vector3 clamped=ClampBallPoint(position);
                    Vector3 airVelocity=body.linearVelocity;
                    if(Mathf.Abs(clamped.x-position.x)>0.001f)airVelocity.x*=-edgeRestitution;
                    if(Mathf.Abs(clamped.z-position.z)>0.001f)airVelocity.z*=-edgeRestitution;
                    body.position=clamped;body.linearVelocity=airVelocity;previousAirPosition=clamped;
                    return;
                }
            }

            if (TryReceivePass()) return;

            if (State == BallState.Released && Time.time >= collectAfter)
                State = BallState.Free;
            if (Time.time >= collectAfter && ignoredOwnerCollider != null &&
                !ignoredOwnerCollider.bounds.Intersects(ballCollider.bounds))
                RestoreOwnerCollision();

            if (TryAutomaticPickup()) return;
            Vector3 surface = body.position;
            float bobAge = Time.time - bobStarted;
            if (bobAge > .60f) bobDepth = 0f;
            surface.y = SurfaceHeight - bobDepth * Mathf.Exp(-7f * bobAge) * Mathf.Sin(16f * bobAge);
            if (Mathf.Abs(body.position.y - surface.y) > .0001f) body.position = surface;
            if (BallCanLeaveField) return;
            Vector3 bounded = SurfacePosition(surface);
            bounded.y = surface.y;
            if (bounded != surface) body.position = bounded;
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            Vector3 next = body.position + velocity * Time.fixedDeltaTime;
            Vector3 constrained = SurfacePosition(next);
            bool inGoalLane = FindPocket(next) != null;
            if (Mathf.Abs(next.x - constrained.x) > 0.0001f)
                velocity.x *= -edgeRestitution;
            if (Mathf.Abs(next.z - constrained.z) > 0.0001f)
            {
                // Reach the back-net limit rather than reflecting a fast valid shot before the line.
                if (inGoalLane) velocity.z = (constrained.z - body.position.z) / Time.fixedDeltaTime;
                else velocity.z *= -edgeRestitution;
            }
            body.linearVelocity = velocity;
        }

        private bool TryReceivePass()
        {
            if (!IsPassFlight || Owner != null) return false;
            if (intendedReceiver != null && (!Eligible(intendedReceiver.Interaction) ||
                !intendedReceiver.isActiveAndEnabled || Time.time > receiveUntil)) intendedReceiver = null;

            Vector3 from = previousPassPosition;
            Vector3 current = body.position;
            previousPassPosition = current;
            if (!IsAirborne || Delivery == PassDelivery.Wet || Time.time < receiveAfter || Time.time > receiveUntil ||
                current.y > SurfaceHeight + 1.2f || Velocity.magnitude > flight.maximumReceivingSpeed) return false;

            SwimmerBallInteraction catcher = null;
            Vector3 catchPoint = Vector3.zero;
            float first = float.PositiveInfinity;
            if (intendedReceiver != null) TryHandContact(intendedReceiver.Identity,from,current,ref catcher,ref catchPoint,ref first);
            if (roster != null && LastReleaser != null)
            {
                var passer = LastReleaser.GetComponent<SwimmerIdentity>();
                for (int i = 0; i < roster.Members.Count; i++)
                {
                    var p = roster.Members[i];
                    if (p.Team == passer.Team || !p.EligibleForPlay) continue;
                    TryHandContact(p,from,current,ref catcher,ref catchPoint,ref first);
                }
            }
            if (catcher == null) return false;
            AssignOwner(catcher,catchPoint);
            DryCaught?.Invoke(catcher);
            return true;
        }

        private void TryHandContact(SwimmerIdentity player, Vector3 from, Vector3 to,
            ref SwimmerBallInteraction catcher, ref Vector3 point, ref float first)
        {
            if (!Eligible(player.Interaction)) return;
            Vector3 motion = to-from;
            if (motion.sqrMagnitude < .000001f || (player.Motor.Position - to).sqrMagnitude > 2.5f) return;
            Vector3 arrival = Vector3.ProjectOnPlane(motion,Vector3.up).normalized;
            bool evaluated = professionalThrows && player.Interaction.Visual != null && player.Interaction.Visual.SupportsEvaluatedGrip;
            float faceDot = Vector3.Dot(player.Motor.Facing, -arrival);
            if ((evaluated || player.Interaction != IntendedReceiver) && faceDot < .24f) return;
            if ((Velocity-player.Motor.Velocity).magnitude > flight.maximumReceivingSpeed) return;
            Vector3 hand = RaisedHand(player.Interaction,from-arrival);
            // Solve first segment/sphere contact so a defender and receiver cannot both collect.
            float reach = evaluated ? .22f : .31f;
            Vector3 delta = from-hand;
            float a = motion.sqrMagnitude;
            float b = Vector3.Dot(delta,motion);
            float c = delta.sqrMagnitude - reach*reach;
            float d = b*b-a*c;
            if (d < 0f) return;
            float t = c <= 0f ? 0f : (-b-Mathf.Sqrt(d))/a;
            if (t < 0f || t > 1f || t >= first) return;
            var contact = Vector3.Lerp(from,to,t);
            if (contact.y < SurfaceHeight+.1f || TryGetFieldExit(from, contact, out _)) return;
            if (evaluated && !player.Interaction.Visual.CanReachDryCatch(contact, Velocity)) return;
            first = t;
            point = contact;
            catcher = player.Interaction;
        }

        private bool TryAutomaticPickup()
        {
            if (!UsesAutomaticPickup || match.AwaitingOpeningPossession || !CanRecoverLooseBall)
                return false;

            Vector3 point = body.position;
            Vector3 inside = poolBounds.ClampPosition(point, Radius);
            if (Mathf.Abs(point.x - inside.x) > .01f || Mathf.Abs(point.z - inside.z) > .01f ||
                Mathf.Abs(point.y - SurfaceHeight) > .16f) return false;

            SwimmerBallInteraction nearest = null;
            float best = float.PositiveInfinity;
            Vector3 ballVelocity = Velocity;
            var members = roster.Members;
            for (int i = 0; i < members.Count; i++)
            {
                var player = members[i];
                if (player == null || !player.EligibleForPlay) continue;
                var interaction = player.Interaction;
                if (!interaction.isActiveAndEnabled || PickupSuppressed(interaction) || interaction.CarryAnchor == null) continue;

                var motor = player.Motor;
                Vector3 gap = point - motor.Position;
                gap.y = 0f;
                float reach = Mathf.Min(automaticPickupDistance, interaction.PickupDistance);
                float distance = gap.sqrMagnitude;
                if (distance >= best || distance > interaction.PickupDistance * interaction.PickupDistance ||
                    (distance > .55f * .55f && Vector3.Dot(gap.normalized, motor.Facing) < .2f))
                    continue;
                // Reserve the surface recovery before the swimmer's next kinematic step
                // can push the ball away. Rank the candidates by current separation.
                if (distance > reach * reach)
                {
                    Vector3 relative = ballVelocity - motor.Velocity;
                    relative.y = 0f;
                    Vector3 step = Vector3.ClampMagnitude(relative * Time.fixedDeltaTime, .14f);
                    float closest = Mathf.Clamp01(-Vector3.Dot(gap, step) / Mathf.Max(step.sqrMagnitude, .000001f));
                    if ((gap + step * closest).sqrMagnitude > reach * reach) continue;
                }

                best = distance;
                nearest = interaction;
            }
            if (nearest == null) return false;
            AssignOwner(nearest, point);
            return true;
        }

        private void UpdateDribble()
        {
            if (HasPendingThrow || IsGathering)
                return;
            if (match == null || !match.IsPlaying || carrierMotor == null || carrierVisual == null ||
                carrierMotor.IsRetreating || (carrierCharge != null && carrierCharge.IsCharging))
            {
                EndDribble();
                return;
            }

            Vector3 velocity = carrierMotor.Velocity;
            velocity.y = 0f;
            float forward = Vector3.Dot(velocity, carrierMotor.Facing);
            bool sprint = carrierStamina != null && carrierStamina.IsSprinting;
            if (IsDribbling)
            {
                if (velocity.sqrMagnitude <= dribbleStopSpeed * dribbleStopSpeed || !sprint && carrierMotor.IsSteeringCarry)
                {
                    EndDribble();
                }
                return;
            }

            if (forward < (sprint ? dribbleStopSpeed : dribbleStartSpeed) || !sprint && carrierMotor.IsSteeringCarry)
            {
                swimmingTime = 0f;
                return;
            }
            swimmingTime += Time.fixedDeltaTime;
            IsDribbling = sprint || swimmingTime >= Mathf.Max(.30f, dribbleStartDelay);
        }

        private Vector3 LeadingPoint(Vector3 root)
        {
            Vector3 direction = Vector3.ProjectOnPlane(carrierMotor.Velocity, Vector3.up).normalized;
            if (direction.sqrMagnitude < .01f) direction = carrierMotor.Facing;
            float speed = carrierMotor.Velocity.magnitude;
            float clearance = dribbleHeadGap + (Radius - .14f) + Mathf.Max(0f, speed - 4f) * .022f;
            float lead = Mathf.Clamp(Vector3.Dot(carrierVisual.HeadPosition - root, direction) + clearance, .68f, 1.09f);
            float wave = match.PresentationTime * 11f;
            Vector3 target = root + direction * lead + Vector3.Cross(Vector3.up, direction) * (.035f * Mathf.Sin(wave));
            target = poolBounds.ClampPosition(target, Radius);
            target.y = SurfaceHeight + .012f + .01f * Mathf.Sin(wave * .83f);
            return target;
        }

        private void ClearDribble()
        {
            CancelPendingThrow();
            gatherUntil = 0f;
            gripSampled = false;
            IsDribbling = false;
            swimmingTime = 0f;
            carrierMotor = null;
            carrierCharge = null;
            carrierStamina = null;
            carrierVisual = null;
        }

        private void LateUpdate()
        {
            // timeScale changes do not retroactively zero this frame's deltaTime. Freeze carry immediately.
            if (match!=null && match.IsPaused)return;
            if (State == BallState.Held && Owner != null)
            {
                if (UsesEvaluatedGrip) return;
                FollowCarryTarget();
            }
            RollVisual();
        }

        private void FollowCarryTarget()
        {
            Vector3 root = Owner.transform.position;
            Vector3 target = IsDribbling ? LeadingPoint(root) : Owner.HeldBallPoint;
            Vector3 origin = transform.position;
            Vector3 travel = root - lastCarrierPosition;
            if (IsDribbling && travel.sqrMagnitude < .36f) origin += travel;
            lastCarrierPosition = root;
            float blend = 1f - Mathf.Exp(-carrySharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(origin, target, blend);
        }

        private void RollVisual()
        {
            if (rollingVisual == null) return;
            Vector3 point = transform.position;
            Vector3 travel = point - lastVisualPosition;
            travel.y = 0f;
            bool roll = lastVisualRevision == MotionRevision && (IsDribbling ||
                (State != BallState.Held && !IsAirborne && !simulationPaused && nettedGoal == null));
            if (roll && travel.sqrMagnitude > .000001f && travel.sqrMagnitude < .25f)
                rollingVisual.Rotate(Vector3.Cross(Vector3.up, travel), travel.magnitude * Mathf.Rad2Deg / Radius * .55f, Space.World);
            else if (IsAirborne && !simulationPaused && travel.sqrMagnitude > .000001f && travel.sqrMagnitude < 1f &&
                     lastVisualRevision == MotionRevision)
                rollingVisual.Rotate(Vector3.Cross(Vector3.up, travel), travel.magnitude * Mathf.Rad2Deg / Radius * .09f, Space.World);
            lastVisualPosition = point;
            lastVisualRevision = MotionRevision;
        }

        private void RestoreOwnerCollision()
        {
            if (ignoredOwnerCollider != null && ballCollider != null)
                Physics.IgnoreCollision(ballCollider, ignoredOwnerCollider, false);
            ignoredOwnerCollider = null;
        }

        private void ResetFree()
        {
            periodSupported=periodDropping=false;
            RestoreSurfacePhysics();
            SwimmerBallInteraction previousOwner = Owner;
            intendedReceiver = null;
            retreatDropper = null;
            nettedGoal=null;WaterBounces=0;
            simulationPaused = false;
            IsScoringShot = CanScoreGoal = false;
            goalAction = default;
            opposingTouch = false;
            ReleaseKind = BallReleaseKind.None; LastReleaseSource = BallActionSource.None; LastReleaser = null; LastTouch = default;
            Owner = null;
            ClearDribble();
            State = BallState.Free;
            if (body == null || ballCollider == null)
            {
                NotifyPossessionChanged(previousOwner);
                return;
            }
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = Vector3.zero;
            RestoreBallRenderers();
            EnableBallCollider();
            RestoreOwnerCollision();
            NoteMotionOrigin(body.position);
            NotifyPossessionChanged(previousOwner);
        }

        private void NotifyPossessionChanged(SwimmerBallInteraction previousOwner)
        {
            if (previousOwner != Owner) PossessionChanged?.Invoke(previousOwner, Owner);
        }

        private void OnDisable()
        {
            ResetFree();
        }

        private void OnDestroy()
        {
            if (match != null) match.PauseChanged -= CancelThrowOnPause;
        }
    }
}
