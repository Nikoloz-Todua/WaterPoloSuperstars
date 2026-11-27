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
        private float committedUntil;
        private BallReleaseKind awaitingRelease;
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
        private void OnEnable()
        {
            if (match != null) match.Changed += ResetDecisions;
            if (ball != null) ball.Released += CountRelease;
            nextDecision = 0f;
        }
        private void ResetDecisions()
        {
            nextDecision = committedUntil = 0f; previousOwner = null; decision = default;
            awaitingRelease = BallReleaseKind.None;
            if (identity != null && selection.ActivePlayer != identity) identity.Motor.StopMotion();
        }

        private void CountRelease(SwimmerBallInteraction player, BallReleaseKind kind)
        {
            if (player != identity.Interaction || kind != awaitingRelease) return;
            awaitingRelease = BallReleaseKind.None;
            if (kind == BallReleaseKind.Shot) ShotsExecuted++;
            else { PassesExecuted++; tacticalTeam.RecordPass(); }
            nextDecision = 0f;
        }

        private void Update()
        {
            if (match.IsPaused) return;
            if (selection.ActivePlayer == identity) { Behaviour = SwimmerAIBehaviour.Human; nextDecision = 0f; return; }
            if(match.IsSprintRace&&identity.EligibleForPlay&&identity.Motor.enabled)
            {Behaviour=SwimmerAIBehaviour.PursueLooseBall;match.SprintOff.DriveAI(identity);return;}
            if (identity.EligibleForPlay && identity.Motor.enabled && match.Rules != null && match.Rules.OrdinaryFreeThrow)
            {
                RepositionForFreeThrow(match.Rules.Taker);
                return;
            }
            if (!identity.EligibleForPlay || !match.IsPlaying || selection.InputLocked || !identity.Motor.enabled)
            { Behaviour = SwimmerAIBehaviour.Stopped; nextDecision = 0f; return; }
            if (ball.TryReadReception(identity, out _))
            {
                Behaviour = SwimmerAIBehaviour.AwaitPass;
                nextDecision = 0f;
                stamina?.RequestSprint(false);
                identity.Motor.SetMoveInput(Vector2.zero);
                identity.Motor.ClearFacingDirection();
                return;
            }
            if (identity.Team == selection.HumanTeam && identity.Interaction.HasBall)
            {
                Behaviour = SwimmerAIBehaviour.Carry;
                awaitingRelease = BallReleaseKind.None;
                nextDecision = 0f;
                identity.Motor.SetMoveInput(Vector2.zero);
                identity.Motor.ClearFacingDirection();
                stamina?.RequestSprint(false);
                return;
            }
            if (identity.Interaction.HasBall && ball.HasPendingThrow) return;
            awaitingRelease = BallReleaseKind.None;
            if (Time.time >= nextDecision || previousOwner != ball.Owner)
            {
                bool ownershipChanged=previousOwner!=ball.Owner;
                previousOwner = ball.Owner; nextDecision = Time.time + decisionInterval;
                var next = WaterPoloDecision.Evaluate(identity, tacticalTeam, selection.ActivePlayer);
                bool committed=decision.Behaviour==SwimmerAIBehaviour.Pass || decision.Behaviour==SwimmerAIBehaviour.Shoot;
                bool receiverValid=decision.Receiver==null || PrototypeTeamTactics.Eligible(decision.Receiver);
                if(ownershipChanged || !committed || Time.time>=committedUntil || !receiverValid)
                {
                    if(next.Behaviour!=decision.Behaviour)committedUntil=Time.time+0.45f;
                    decision=next;
                }
            }
            Behaviour = decision.Behaviour;
            if(stamina!=null)stamina.RequestSprint(false);
            if (Behaviour == SwimmerAIBehaviour.Pass || Behaviour == SwimmerAIBehaviour.Shoot)
            {
                Vector3 point = decision.Receiver != null ? passing.AimPoint(decision.Receiver) : decision.Target;
                Vector3 aim = PrototypeTeamTactics.Planar(point - identity.Motor.Position).normalized;
                identity.Motor.SetMoveInput(Vector2.zero); identity.Motor.SetFacingDirection(aim);
                if (Vector3.Dot(identity.Motor.Facing, aim) > 0.9995f && identity.Motor.Velocity.sqrMagnitude < 0.04f && identity.Interaction.HasBall)
                {
                    ShotType shot=tacticalTeam.AIShotSpeed>=18f?ShotType.Powerful:ShotType.Normal;
                    float goalDistance=Vector3.Distance(identity.Motor.Position,tacticalTeam.AttackingGoal.transform.position);
                    if(goalDistance>6f && goalDistance<9f && tacticalTeam.Pressure(identity.Motor.Position)>0.3f)shot=ShotType.High;
                    else if(goalDistance>5f && goalDistance<8f && identity.FieldPosition==FieldPositionRole.CentreForward && tacticalTeam.LaneClear(identity.Motor.Position,point))shot=ShotType.Skip;
                    bool shooting = Behaviour == SwimmerAIBehaviour.Shoot;
                    awaitingRelease = shooting ? BallReleaseKind.Shot : BallReleaseKind.Pass;
                    bool accepted = shooting ? identity.Interaction.TryShoot(tacticalTeam.AIShotSpeed, shot, BallActionSource.TacticalAI) :
                        passing.TryPassTo(decision.Receiver, BallActionSource.TacticalAI);
                    if (!accepted) awaitingRelease = BallReleaseKind.None;
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
            bool holdPocket = Behaviour == SwimmerAIBehaviour.Support && distance <= stoppingRadius &&
                tacticalTeam.Carrier != null && tacticalTeam.Carrier.Team == identity.Team;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                if (holdPocket) break;
                var other = roster.Members[i]; if (other == identity || !PrototypeTeamTactics.Eligible(other)) continue;
                // Close pressure intentionally enters steal reach; ordinary spacing must not repel it forever.
                if (Behaviour == SwimmerAIBehaviour.Press && other == tacticalTeam.Carrier) continue;
                Vector3 away = PrototypeTeamTactics.Planar(identity.Motor.Position - other.Motor.Position); float gap = away.magnitude;
                if (gap > 0.01f && gap < spacing) steering += away / gap * (1f - gap / spacing);
            }
            float blockMobility = tacticalTeam.BlockMobility(identity);
            identity.Motor.SetMoveInput(Vector2.ClampMagnitude(new Vector2(steering.x, steering.z), 1f) *
                (burst ? 1f : movementFraction) * blockMobility);
            if (holdPocket)
                identity.Motor.SetFacingDirection(tacticalTeam.Carrier.Motor.Position - identity.Motor.Position);
            if (Behaviour == SwimmerAIBehaviour.PursueLooseBall && ball.CanCollect && !ball.UsesAutomaticPickup)
                identity.Interaction.TryCollect();
            if (Behaviour == SwimmerAIBehaviour.Press && ball.Owner != null && stealing != null)
            {
                Vector3 toCarrier = PrototypeTeamTactics.Planar(tacticalTeam.Carrier.Motor.Position - identity.Motor.Position);
                identity.Motor.SetFacingDirection(toCarrier);
                if (toCarrier.sqrMagnitude <= stealing.Reach * stealing.Reach && stealing.CanAttempt) stealing.TrySteal();
            }
        }

        internal bool TryObservePlay(out Vector3 position, out bool friendlySupport)
        {
            position = default;
            friendlySupport = false;
            if (!isActiveAndEnabled || !match.IsPlaying || !identity.EligibleForPlay ||
                selection.ActivePlayer == identity || selection.InputLocked || identity.Interaction.HasBall)
                return false;
            var carrier = tacticalTeam.Carrier;
            if (carrier == null) return false;
            position = carrier.Motor.Position + Vector3.up * .38f;
            friendlySupport = carrier.Team == identity.Team && Behaviour == SwimmerAIBehaviour.Support;
            if (friendlySupport || Behaviour == SwimmerAIBehaviour.Press) return true;
            if (Behaviour != SwimmerAIBehaviour.Defend) return false;
            var mark = tacticalTeam.MarkFor(identity);
            if (mark != null) position = Vector3.Lerp(mark.Motor.Position + Vector3.up * .35f, position, .46f);
            return true;
        }

        private void RepositionForFreeThrow(SwimmerIdentity taker)
        {
            stamina?.RequestSprint(false);
            if (identity == taker)
            {
                identity.Motor.SetMoveInput(Vector2.zero);
                return;
            }
            bool supporting = identity.Team == taker.Team;
            Behaviour = supporting ? SwimmerAIBehaviour.Support : SwimmerAIBehaviour.Defend;
            Vector3 target = supporting ? tacticalTeam.Support(identity, taker) : tacticalTeam.Defense(identity, taker);
            Vector3 delta = PrototypeTeamTactics.Planar(target - identity.Motor.Position);
            Vector3 move = delta.sqrMagnitude > .15f ? Vector3.ClampMagnitude(delta * .44f, .53f) : Vector3.zero;
            Vector3 face = move.sqrMagnitude > .12f ? delta : taker.Motor.Position - identity.Motor.Position;
            identity.Motor.SetMoveInput(new Vector2(move.x, move.z));
            identity.Motor.SetFacingDirection(face);
        }
        private void OnDisable()
        {
            if (match != null) match.Changed -= ResetDecisions;
            if (ball != null) ball.Released -= CountRelease;
            awaitingRelease = BallReleaseKind.None;
            if (identity != null && selection != null && selection.ActivePlayer != identity) identity.Motor.StopMotion();
        }
    }
}
