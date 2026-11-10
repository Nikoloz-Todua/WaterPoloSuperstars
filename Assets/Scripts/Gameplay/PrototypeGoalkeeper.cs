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
        public float SaveReach => saveReach;
        public bool Recovering => Time.time < recoverUntil;
        public int Catches { get; private set; }
        public int Deflections { get; private set; }
        public int SaveAttempts { get; private set; }
        public bool ThreateningShot => ball.Owner == null && ball.ReleaseKind == BallReleaseKind.Shot && ball.IsScoringShot && defendedGoal.transform.InverseTransformDirection(ball.Velocity).z > 0.1f;
        public SwimmerIdentity Identity => identity != null ? identity : identity = GetComponent<SwimmerIdentity>();
        public GoalMouth DefendedGoal => defendedGoal;
        public void SetDefendedGoal(GoalMouth goal)
        {defendedGoal=goal;ResetDecisions();}
        public BallController Ball => ball;
        public bool Stopped => !match.IsPlaying || selection.InputLocked || !Identity.Motor.enabled;
        public bool HumanControlled => selection.ActivePlayer == Identity && !selection.InputLocked;
        public bool AtStation => Vector3.Distance(Identity.Motor.Position, StationPosition(Identity.Motor.Position)) <= 0.05f;
        public bool PossessionProtected => Vector3.Distance(Identity.Motor.Position, StationPosition(Identity.Motor.Position)) <= possessionProtectionDistance;
        public float LateralLimit => Mathf.Max(0f, defendedGoal.Width * 0.5f - lateralMargin);
        public bool Ready => Time.time >= reactAt;
        public Vector3 HandPosition => Identity.Motor.Position + (Identity.Interaction.CarryAnchor.position - transform.position);
        public Vector3 ClampMovement(Vector3 position)
        {
            // Outside the station, return by swimming. Applying station bounds immediately would teleport.
            if (HumanControlled || !AtStation) return poolBounds.ClampPosition(position, 1.1f);
            return StationPosition(position);
        }
        private Vector3 StationPosition(Vector3 position)
        {
            Vector3 local = defendedGoal.transform.InverseTransformPoint(position);
            local.x = Mathf.Clamp(local.x, -LateralLimit, LateralLimit); local.z = -stationDepth;
            Vector3 result = defendedGoal.transform.TransformPoint(local); result.y = position.y; return result;
        }
        private void Awake()
        {
            identity = GetComponent<SwimmerIdentity>();
            if (defendedGoal == null || ball == null || match == null || selection == null || poolBounds == null || Identity.Role != AthleteRole.Goalkeeper || defendedGoal.ScoringTeam == Identity.Team)
            { Debug.LogError("PrototypeGoalkeeper requires its explicit role, opposing scoring goal and match references.", this); enabled = false; return; }
            Identity.Motor.SetMovementBounds(this);
            ballBody = ball.GetComponent<Rigidbody>();
            random = new System.Random(randomSeed);
        }
        private void OnEnable()
        {
            if (match != null) match.Changed += ResetDecisions;
            if (defendedGoal != null) Identity.Motor.SetMovementBounds(this);
        }
        private void ResetDecisions()
        {
            targetX = targetVelocity = 0f; sawThreat = false; reactAt = Time.time + reactionDelay;
            recoverUntil = 0f; random = new System.Random(randomSeed);
            Identity.Motor.StopMotion();
        }
        private void Update()
        {
            if (Stopped) return;
            if (HumanControlled) { Identity.Motor.OverrideSpeed(carryingSpeed); targetVelocity = 0f; return; }
            Identity.Motor.ClearSpeedOverride();
            if (Identity.Interaction.HasBall) { Identity.Motor.SetMoveInput(Vector2.zero); return; }
            Vector3 local = defendedGoal.transform.InverseTransformPoint(ball.PhysicsPosition);
            Vector3 velocity = ballBody.linearVelocity;
            bool incoming = ball.Owner == null && defendedGoal.transform.InverseTransformDirection(velocity).z > 0.1f && local.z < 0f && local.z > -trackingRange;
            if (incoming && !sawThreat) reactAt = Time.time + reactionDelay;
            sawThreat = incoming;
            float desired = 0f;
            var carrier = ball.Owner != null ? ball.Owner.GetComponent<SwimmerIdentity>() : null;
            if (Ready && !Recovering && (incoming || (carrier != null && carrier.Team != Identity.Team && local.z > -trackingRange && local.z < 0f)))
                desired = GoalkeeperPositioning.TrackingX(defendedGoal, ball.PhysicsPosition, velocity, stationDepth, 2f);
            targetX = Mathf.SmoothDamp(targetX, Mathf.Clamp(desired, -LateralLimit, LateralLimit), ref targetVelocity, trackingSmoothTime);
            Vector3 target = defendedGoal.transform.TransformPoint(new Vector3(targetX, 0f, -stationDepth)); target.y = Identity.Motor.Position.y;
            Vector3 delta = target - Identity.Motor.Position;
            Identity.Motor.SetMoveInput(Vector2.ClampMagnitude(new Vector2(delta.x, delta.z) * 2f, 1f));
            Identity.Motor.SetFacingDirection(-defendedGoal.transform.forward);
        }
        public bool ResolveContact(Vector3 contact, Vector3 handAtContact, Vector3 incomingVelocity)
        {
            if (!isActiveAndEnabled || Stopped || Recovering || !AtStation || ball.Owner != null) return false;
            bool slowLoose = ball.CanCollect && incomingVelocity.sqrMagnitude < 6.25f;
            bool approaching = defendedGoal.transform.InverseTransformDirection(incomingVelocity).z > 0.1f && ball.IsScoringShot;
            if (!slowLoose && (!approaching || !Ready || Time.time < ball.ReleaseStartedAt + reactionDelay)) return false;
            if (!slowLoose) { SaveAttempts++; if (random.NextDouble() >= saveChance) return false; }
            bool catchBall = slowLoose || random.NextDouble() < catchChance;
            bool applied;
            if (catchBall)
            {
                applied = ball.TryCatchContact(Identity.Interaction, contact);
                if (applied) Catches++;
            }
            else
            {
                Vector3 normal = contact - handAtContact;
                normal.y = 0f;
                if (normal.sqrMagnitude < 0.0001f) normal = -incomingVelocity;
                Vector3 reflected = Vector3.Reflect(incomingVelocity, normal.normalized) * deflectionRestitution;
                applied = ball.TryDeflectContact(Identity.Interaction, contact, reflected);
                if (applied) Deflections++;
            }
            if (applied) recoverUntil = Time.time + recoveryTime;
            return applied;
        }
        private void OnDrawGizmosSelected()
        {
            if (defendedGoal == null || GetComponent<SwimmerBallInteraction>()?.CarryAnchor == null) return;
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(HandPosition, saveReach);
        }
        private void OnDisable()
        {
            if (match != null) match.Changed -= ResetDecisions;
            Identity.Motor.SetMovementBounds(null); Identity.Motor.StopMotion();
        }
    }
}
