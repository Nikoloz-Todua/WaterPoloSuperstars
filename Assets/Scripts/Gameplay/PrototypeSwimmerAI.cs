using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum SwimmerAIBehaviour { Human, Idle, Support, PursueLooseBall, Defend, HoldBall, AwaitPass, Stopped }

    /// <summary>Small shared decision adapter. All movement and pickup still use existing authorities.</summary>
    [DefaultExecutionOrder(-20), DisallowMultipleComponent, RequireComponent(typeof(SwimmerIdentity))]
    public sealed class PrototypeSwimmerAI : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private BallController ball;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private Transform defendedGoal;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.15f;
        [SerializeField, Min(1f)] private float supportDistance = 4.5f;
        [SerializeField, Min(1f)] private float supportWidth = 2.5f;
        [SerializeField, Min(0.5f)] private float pressureDistance = 2.8f;
        [SerializeField, Min(1f)] private float looseBallRange = 9f;
        [SerializeField, Range(0.1f, 1f)] private float movementFraction = 0.7f;
        [SerializeField, Min(0.1f)] private float arrivalRadius = 0.45f;
        [SerializeField, Min(0.5f)] private float slowingRadius = 1.5f;
        [SerializeField, Min(0.5f)] private float spacing = 2.2f;

        private SwimmerIdentity identity;
        private float nextDecision;
        private SwimmerBallInteraction previousOwner;
        public SwimmerAIBehaviour Behaviour { get; private set; }
        public Vector3 Target { get; private set; }

        private void Awake()
        {
            identity = GetComponent<SwimmerIdentity>();
            if (roster == null || selection == null || ball == null || pool == null || match == null || defendedGoal == null)
            {
                Debug.LogError("PrototypeSwimmerAI needs roster, selection, ball, pool, match and defended-goal references.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (selection.ActivePlayer == identity)
            {
                Behaviour = SwimmerAIBehaviour.Human;
                nextDecision = 0f; // A handoff must recompute tactics before applying any old AI target.
                return; // Never clear or overwrite the human's movement intent.
            }
            if (match.IsRestarting || selection.InputLocked || !identity.Motor.enabled)
            {
                Behaviour = SwimmerAIBehaviour.Stopped;
                nextDecision = 0f;
                return;
            }
            if (Time.time >= nextDecision || previousOwner != ball.Owner)
            {
                previousOwner = ball.Owner;
                nextDecision = Time.time + decisionInterval;
                Think();
            }
            if (Behaviour == SwimmerAIBehaviour.HoldBall || Behaviour == SwimmerAIBehaviour.AwaitPass || Behaviour == SwimmerAIBehaviour.Idle)
            {
                identity.Motor.SetMoveInput(Vector2.zero);
                return;
            }
            Vector3 delta = Target - identity.Motor.Position;
            delta.y = 0f;
            float distance = delta.magnitude;
            Vector3 steering = distance > arrivalRadius ? delta.normalized * Mathf.Clamp01(distance / slowingRadius) : Vector3.zero;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                SwimmerIdentity other = roster.Members[i];
                if (other == null || other == identity || !other.gameObject.activeInHierarchy) continue;
                Vector3 away = identity.Motor.Position - other.Motor.Position;
                away.y = 0f;
                float gap = away.magnitude;
                if (gap > 0.01f && gap < spacing) steering += away / gap * (1f - gap / spacing);
            }
            Vector2 input = Vector2.ClampMagnitude(new Vector2(steering.x, steering.z), 1f) * movementFraction;
            identity.Motor.SetMoveInput(input);
            if (Behaviour == SwimmerAIBehaviour.PursueLooseBall && ball.CanCollect)
                identity.Interaction.TryCollect(); // Existing range/cooldown/exclusive-claim rules decide success.
        }

        private void Think()
        {
            Target = identity.Motor.Position;
            if (identity.Interaction.HasBall) { Behaviour = SwimmerAIBehaviour.HoldBall; return; }
            if (ball.Owner == null)
            {
                if (ball.IntendedReceiver != null) { Behaviour = SwimmerAIBehaviour.AwaitPass; return; }
                if (!ball.CanCollect || !IsNearestTeammateToBall()) { Behaviour = SwimmerAIBehaviour.Idle; return; }
                Vector3 loose = ball.PhysicsPosition;
                if (PlanarDistance(identity.Motor.Position, loose) > looseBallRange) { Behaviour = SwimmerAIBehaviour.Idle; return; }
                Behaviour = SwimmerAIBehaviour.PursueLooseBall;
                Target = pool.ClampPosition(loose, 1.1f);
                return;
            }
            SwimmerIdentity carrier = ball.Owner.GetComponent<SwimmerIdentity>();
            if (carrier == null) { Behaviour = SwimmerAIBehaviour.Idle; return; }
            if (carrier.Team == identity.Team)
            {
                int rank = 0;
                for (int i = 0; i < roster.Members.Count; i++)
                {
                    SwimmerIdentity member = roster.Members[i];
                    if (member == identity) break;
                    if (member != null && member != carrier && member.Team == identity.Team) rank++;
                }
                Target = SwimmerTactics.Support(carrier.Motor.Position, carrier.transform.forward, rank, supportDistance, supportWidth);
                Behaviour = SwimmerAIBehaviour.Support;
            }
            else
            {
                Target = SwimmerTactics.Defend(carrier.Motor.Position, defendedGoal.position, pressureDistance);
                Behaviour = SwimmerAIBehaviour.Defend;
            }
            Target = pool.ClampPosition(Target, 1.1f);
        }

        private bool IsNearestTeammateToBall()
        {
            SwimmerIdentity nearest = null;
            float best = float.PositiveInfinity;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                SwimmerIdentity member = roster.Members[i];
                if (member == null || member.Team != identity.Team || !member.enabled || !member.gameObject.activeInHierarchy) continue;
                float distance = PlanarDistance(member.Motor.Position, ball.PhysicsPosition);
                if (distance < best) { best = distance; nearest = member; }
            }
            return nearest == identity; // Include the human in arbitration; only one teammate should chase.
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDisable()
        {
            if (identity != null && selection != null && selection.ActivePlayer != identity) identity.Motor.StopMotion();
        }
    }
}
