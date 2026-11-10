using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum SwimmerAIBehaviour { Human, Idle, Support, PursueLooseBall, Defend, HoldBall, AwaitPass, Stopped, Carry, Pass, Shoot, Press, Formation }

    /// <summary>Movement/action adapter for the shared decision model; never writes ball ownership.</summary>
    [DefaultExecutionOrder(-20), DisallowMultipleComponent, RequireComponent(typeof(SwimmerIdentity))]
    public sealed class PrototypeSwimmerAI : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private BallController ball;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PrototypeTeamTactics tacticalTeam;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.15f;
        [SerializeField, Range(0.1f, 1f)] private float movementFraction = 0.7f;
        [SerializeField, Min(0.1f)] private float arrivalRadius = 0.45f;
        [SerializeField, Min(0.5f)] private float slowingRadius = 1.5f;
        [SerializeField, Min(0.5f)] private float spacing = 2.2f;
        private SwimmerIdentity identity;
        private SwimmerPassing passing;
        private SwimmerSteal stealing;
        private SwimmerDecision decision;
        private SwimmerBallInteraction previousOwner;
        private float nextDecision;
        private SwimmerStamina stamina;
        public SwimmerAIBehaviour Behaviour { get; private set; }
        public Vector3 Target => decision.Target;
        public PrototypeTeamTactics TacticalTeam => tacticalTeam;
        public int PassesExecuted { get; private set; }
        public int ShotsExecuted { get; private set; }

        private void Awake()
        {
            identity = GetComponent<SwimmerIdentity>(); passing = GetComponent<SwimmerPassing>();
            stamina = GetComponent<SwimmerStamina>();
            stealing = GetComponent<SwimmerSteal>();
            if (roster == null || selection == null || ball == null || pool == null || match == null || tacticalTeam == null || tacticalTeam.Team != identity.Team)
            { Debug.LogError("PrototypeSwimmerAI requires the matching tactical team and gameplay references.", this); enabled = false; }
        }
        private void OnEnable() { if (match != null) match.Changed += ResetDecisions; nextDecision = 0f; }
        private void ResetDecisions()
        {
            nextDecision = 0f; previousOwner = null; decision = default;
            if (identity != null && selection.ActivePlayer != identity) identity.Motor.StopMotion();
        }
        private void Update()
        {
            if (selection.ActivePlayer == identity) { Behaviour = SwimmerAIBehaviour.Human; nextDecision = 0f; return; }
            if (!match.IsPlaying || selection.InputLocked || !identity.Motor.enabled)
            { Behaviour = SwimmerAIBehaviour.Stopped; nextDecision = 0f; return; }
            if (Time.time >= nextDecision || previousOwner != ball.Owner)
            {
                previousOwner = ball.Owner; nextDecision = Time.time + decisionInterval;
                decision = WaterPoloDecision.Evaluate(identity, tacticalTeam);
            }
            Behaviour = decision.Behaviour;
            if(stamina!=null)stamina.RequestSprint(false);
            if (Behaviour == SwimmerAIBehaviour.Pass || Behaviour == SwimmerAIBehaviour.Shoot)
            {
                Vector3 point = decision.Receiver != null ? decision.Receiver.Motor.Position : decision.Target;
                Vector3 aim = PrototypeTeamTactics.Planar(point - identity.Motor.Position).normalized;
                identity.Motor.SetMoveInput(Vector2.zero); identity.Motor.SetFacingDirection(aim);
                if (Vector3.Dot(transform.forward, aim) > 0.9995f && identity.Motor.Velocity.sqrMagnitude < 0.04f && identity.Interaction.HasBall)
                {
                    bool released = Behaviour == SwimmerAIBehaviour.Shoot ? identity.Interaction.TryShoot(tacticalTeam.AIShotSpeed) : passing.TryPassTo(decision.Receiver);
                    if (released)
                    {
                        if (Behaviour == SwimmerAIBehaviour.Shoot) ShotsExecuted++;
                        else { PassesExecuted++; tacticalTeam.RecordPass(); }
                        nextDecision = 0f;
                    }
                }
                return;
            }
            identity.Motor.ClearFacingDirection();
            if (Behaviour == SwimmerAIBehaviour.AwaitPass || Behaviour == SwimmerAIBehaviour.Idle)
            { identity.Motor.SetMoveInput(Vector2.zero); return; }
            Vector3 delta = PrototypeTeamTactics.Planar(Target - identity.Motor.Position); float distance = delta.magnitude;
            bool burst=(Behaviour==SwimmerAIBehaviour.PursueLooseBall && distance>4f) || (Behaviour==SwimmerAIBehaviour.Press && distance>3f) || (Behaviour==SwimmerAIBehaviour.Carry && distance>5f && tacticalTeam.Pressure(identity.Motor.Position)<0.3f);
            if(stamina!=null)stamina.RequestSprint(burst);
            float stoppingRadius = Behaviour == SwimmerAIBehaviour.Press ? 0.1f : arrivalRadius;
            Vector3 steering = distance > stoppingRadius ? delta.normalized * Mathf.Clamp01(distance / slowingRadius) : Vector3.zero;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var other = roster.Members[i]; if (other == identity || !PrototypeTeamTactics.Eligible(other)) continue;
                // Close pressure intentionally enters steal reach; ordinary spacing must not repel it forever.
                if (Behaviour == SwimmerAIBehaviour.Press && other == tacticalTeam.Carrier) continue;
                Vector3 away = PrototypeTeamTactics.Planar(identity.Motor.Position - other.Motor.Position); float gap = away.magnitude;
                if (gap > 0.01f && gap < spacing) steering += away / gap * (1f - gap / spacing);
            }
            identity.Motor.SetMoveInput(Vector2.ClampMagnitude(new Vector2(steering.x, steering.z), 1f) * (burst?1f:movementFraction));
            if (Behaviour == SwimmerAIBehaviour.PursueLooseBall && ball.CanCollect) identity.Interaction.TryCollect();
            if (Behaviour == SwimmerAIBehaviour.Press && ball.Owner != null && stealing != null)
            {
                Vector3 toCarrier = PrototypeTeamTactics.Planar(tacticalTeam.Carrier.Motor.Position - identity.Motor.Position);
                identity.Motor.SetFacingDirection(toCarrier);
                if (toCarrier.sqrMagnitude <= stealing.Reach * stealing.Reach && stealing.CanAttempt) stealing.TrySteal();
            }
        }
        private void OnDisable()
        {
            if (match != null) match.Changed -= ResetDecisions;
            if (identity != null && selection != null && selection.ActivePlayer != identity) identity.Motor.StopMotion();
        }
    }
}
