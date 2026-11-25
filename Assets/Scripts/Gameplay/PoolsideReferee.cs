using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum RefereeSignal { None, Foul, Exclusion, Penalty, Goal, Period, Restart, Start }

    public readonly struct RefereePoseCue
    {
        public readonly RefereeSignal Signal;
        public readonly Vector3 Point, Direction, Boundary;
        public readonly IllegalContact? Contact;
        public readonly float Age, Duration;
        public readonly int CapNumber;

        public RefereePoseCue(RefereeSignal signal, Vector3 point, Vector3 direction, Vector3 boundary,
            IllegalContact? contact, float age, float duration, int capNumber)
        {
            Signal = signal;
            Point = point;
            Direction = direction;
            Boundary = boundary;
            Contact = contact;
            Age = age;
            Duration = duration;
            CapNumber = capNumber;
        }
    }

    /// <summary>Follows a deck route and observes the decisions already made by the match.</summary>
    [DisallowMultipleComponent]
    public sealed class PoolsideReferee : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private MatchRules rules;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private MonoBehaviour visual;
        [SerializeField, Min(.5f)] private float walkingSpeed = 2.2f;
        [SerializeField, Min(.1f)] private float acceleration = 3f;
        [SerializeField, Min(1f)] private float moveThreshold = 3.5f;
        [SerializeField, Min(.1f)] private float stopThreshold = .5f;
        [SerializeField, Min(.1f)] private float decisionInterval = .65f;
        [SerializeField] private float deckSide = 1f;
        [SerializeField, Min(1f)] private float deckOffset = 1.4f;
        [SerializeField] private float deckHeight = .18f;
        [SerializeField, Range(.4f, .5f)] private float travelFraction = .43f;
        [SerializeField] private bool useMatchTrackingPresentation = true;
        [Tooltip("Distance above the legacy referee root to the actual surface of the walking deck.")]
        [SerializeField] private float professionalDeckOffset = .08f;

        private IRefereePresentation presentation;
        private RefereeVisual model;
        private float targetZ, speed, nextDecision, signalStarts, signalUntil, holdUntil;
        private int lastScore, signalCap;
        private bool walking;
        private MatchPhase lastPhase;
        private Vector3 signalPoint, signalDirection, signalBoundary;
        private IllegalContact? signalContact;

        public RefereeSignal Signal { get; private set; }
        public float Speed => speed;
        public float TargetZ => targetZ;
        public bool Walking => walking;
        public Vector3 ObservationPoint { get; private set; }
        public int SignalledCapNumber => Signal == RefereeSignal.Exclusion || Signal == RefereeSignal.Penalty ? signalCap : 0;
        public Vector3 SignalledAttackDirection => Signal == RefereeSignal.None ? Vector3.zero : signalDirection;

        private bool UsesMatchTracking => useMatchTrackingPresentation && model != null && model.UsesProfessional;

        private void Awake()
        {
            presentation = visual as IRefereePresentation;
            model = visual as RefereeVisual;
            targetZ = transform.position.z;
            lastScore = match.BlueScore + match.WhiteScore;
            lastPhase = match.Phase;
        }

        private void OnEnable()
        {
            rules.Decided += Decision;
            match.Changed += Phase;
            match.NewMatch += ResetObserver;
            match.CentreRestartSignalled += CentreSignal;
            match.SprintStarted += SprintSignal;
        }

        private void OnDisable()
        {
            rules.Decided -= Decision;
            match.Changed -= Phase;
            match.NewMatch -= ResetObserver;
            match.CentreRestartSignalled -= CentreSignal;
            match.SprintStarted -= SprintSignal;
        }

        private void SprintSignal() => SetSignal(RefereeSignal.Start, pool.transform.position, 1.15f);
        private void CentreSignal(SwimmerIdentity taker) => SetSignal(RefereeSignal.Start, pool.transform.position, 1.15f);

        private void ResetObserver()
        {
            targetZ = pool.transform.position.z;
            walking = false;
            speed = 0f;
            Signal = RefereeSignal.None;
            lastScore = 0;
            lastPhase = match.Phase;
            nextDecision = holdUntil = signalUntil = 0f;
        }

        private void SetSignal(RefereeSignal signal, Vector3 point, float duration, Vector3 direction = default,
            Vector3 boundary = default, IllegalContact? contact = null, int cap = 0)
        {
            Signal = signal;
            signalPoint = point;
            signalDirection = direction;
            signalBoundary = boundary;
            signalContact = contact;
            signalCap = cap;
            signalStarts = match.PresentationTime;
            signalUntil = signalStarts + duration;
            holdUntil = signalUntil;
        }

        private void Decision(RefereeEvent decision)
        {
            if (decision.Decision == RefereeDecision.Reentry || decision.Decision == RefereeDecision.FreeThrow) return;
            Vector3 mark = rules.BoundaryRestart?.Mark ?? decision.RestartPoint ?? ball.PlayPosition;
            Vector3 action = decision.IncidentPoint ?? mark;
            Vector3 direction = rules.AttackDirection(decision.AwardedTeam);
            int cap = decision.Player != null ? decision.Player.CapNumber : 0;
            switch (decision.Decision)
            {
                case RefereeDecision.Exclusion:
                    Vector3 boundary = new(pool.transform.position.x + Mathf.Sign(deckSide) * pool.Width * .5f,
                        pool.Waterline + .35f, action.z);
                    SetSignal(RefereeSignal.Exclusion, action, 1.8f, direction, boundary, decision.Contact, cap);
                    break;
                case RefereeDecision.Penalty:
                    SetSignal(RefereeSignal.Penalty, mark, 1.4f, direction, cap: cap);
                    break;
                case RefereeDecision.BallOut:
                    SetSignal(RefereeSignal.Restart, mark, 1.45f, direction);
                    break;
                default:
                    SetSignal(RefereeSignal.Foul, mark, 1.8f, direction, contact: decision.Contact);
                    break;
            }
        }

        private void Phase()
        {
            int score = match.BlueScore + match.WhiteScore;
            MatchPhase phase = match.Phase;
            if (score != lastScore)
                SetSignal(RefereeSignal.Goal, pool.transform.position, 1.75f);
            else if (phase != lastPhase)
            {
                if (phase == MatchPhase.PenaltyReady && UsesMatchTracking)
                    SetSignal(RefereeSignal.Start, rules.Taker != null ? rules.Taker.Motor.Position : ball.PlayPosition, .85f);
                else if (phase == MatchPhase.QuarterBreak || phase == MatchPhase.HalfTime || phase == MatchPhase.FullTime)
                {
                    SetSignal(RefereeSignal.Period, pool.transform.position, 1.4f);
                    targetZ = pool.transform.position.z;
                }
            }
            lastScore = score;
            lastPhase = phase;
            nextDecision = 0f;
        }

        private void Update()
        {
            if (match.IsPaused) return;
            float now = match.PresentationTime;
            if (now >= signalUntil) Signal = RefereeSignal.None;

            Vector3 action = ball.PlayPosition;
            if (Signal != RefereeSignal.None) action = signalPoint;
            else if (rules.BoundaryRestart is BoundaryRestartAward restart) action = restart.Mark;
            else if (rules.IsRulePhase && rules.Taker != null) action = rules.Taker.Motor.Position;
            ObservationPoint = action;

            if (now >= nextDecision && now >= holdUntil)
            {
                nextDecision = now + decisionInterval;
                float z = action.z - pool.transform.position.z;
                // Observe sectors so that minor ball movement does not cause a route reversal.
                float sector = Mathf.Abs(z) < 4f ? 0f : Mathf.Sign(z) * pool.Length *
                    (Mathf.Abs(z) > 10f ? travelFraction - .01f : .22f);
                if (rules.IsPenalty) sector = Mathf.Sign(z) * pool.Length * .34f;
                if (!match.IsPlaying && !rules.IsRulePhase && match.Phase != MatchPhase.GoalRestart) sector = 0f;
                float desired = pool.transform.position.z + sector;
                if (Mathf.Abs(desired - targetZ) > moveThreshold) targetZ = desired;
            }

            float delta = targetZ - transform.position.z;
            if (!walking && Mathf.Abs(delta) > moveThreshold && now >= holdUntil) walking = true;
            if (walking && (Mathf.Abs(delta) < stopThreshold || now < holdUntil)) walking = false;
            float desiredSpeed = walking ? Mathf.Sign(delta) * Mathf.Min(walkingSpeed,
                Mathf.Sqrt(2f * acceleration * Mathf.Abs(delta))) : 0f;
            if (UsesMatchTracking)
            {
                // Brake through zero before changing travel direction. The visual pivots before taking its next step.
                if (Mathf.Abs(speed) > .025f && speed * desiredSpeed < 0f) desiredSpeed = 0f;
                if (Mathf.Abs(speed) < .12f && Mathf.Abs(desiredSpeed) > .01f && !model.FacesTravel(Mathf.Sign(desiredSpeed)))
                    desiredSpeed = 0f;
            }

            float braking = UsesMatchTracking && Signal != RefereeSignal.None ? acceleration * 2f : acceleration;
            speed = Mathf.MoveTowards(speed, desiredSpeed, braking * Time.deltaTime);
            Vector3 point = transform.position;
            point.x = pool.transform.position.x + Mathf.Sign(deckSide) * (pool.Width * .5f + deckOffset);
            point.y = pool.Waterline + deckHeight;
            float nextZ = Mathf.Clamp(point.z + speed * Time.deltaTime,
                pool.transform.position.z - pool.Length * travelFraction, pool.transform.position.z + pool.Length * travelFraction);
            float travelled = nextZ - point.z;
            point.z = nextZ;
            transform.position = point;
            Vector3 look = Vector3.ProjectOnPlane(action - point, Vector3.up);
            if (look.sqrMagnitude > .1f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 100f * Time.deltaTime);

            if (UsesMatchTracking)
            {
                float travelDirection = Mathf.Abs(speed) >= .12f ? Mathf.Sign(speed) : walking ? Mathf.Sign(delta) : 0f;
                model.AnimateMatchOfficial(travelled, travelDirection, point.y + professionalDeckOffset, action,
                    new RefereePoseCue(Signal, signalPoint, signalDirection, signalBoundary, signalContact,
                        now - signalStarts, signalUntil - signalStarts, signalCap));
            }
            else
            {
                bool start = Signal == RefereeSignal.Start;
                if (model != null) model.SetSignalTargets(signalPoint, signalDirection, start);
                presentation?.Animate(speed, start ? RefereeSignal.Restart : Signal);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.65f, ObservationPoint);
            if (Signal == RefereeSignal.None) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(signalPoint, .18f);
        }
    }
}
