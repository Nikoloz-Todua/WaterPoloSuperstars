using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum IllegalContact { Push, Hold }
    public enum RefereeDecision { Ordinary, Exclusion, Penalty, Reentry, FreeThrow }
    public readonly struct RefereeEvent
    {
        public readonly RefereeDecision Decision;
        public readonly SwimmerIdentity Player;
        public readonly PrototypeTeam AwardedTeam;
        public RefereeEvent(RefereeDecision decision, SwimmerIdentity player, PrototypeTeam team)
        { Decision = decision; Player = player; AwardedTeam = team; }
    }

    /// <summary>Prototype rule profile. Decisions/eligibility here; phases in PrototypeMatch; ownership in BallController.</summary>
    [DefaultExecutionOrder(-90), DisallowMultipleComponent]
    public sealed class MatchRules : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private PrototypeTeamTactics[] teams;
        [SerializeField] private bool ordinaryFouls = true;
        [SerializeField, Min(1f)] private float exclusionSeconds = 18f;
        [SerializeField, Min(1)] private int personalFoulLimit = 3;
        [SerializeField, Min(0.1f)] private float setupSeconds = 0.8f;
        [SerializeField, Min(1f)] private float maximumThrowWait = 6f;
        [SerializeField, Min(1f)] private float clearance = 2.2f;
        private Collider[] colliders;
        private bool[] colliderEnabled;
        private float readyAt, throwAt, flightUntil;
        private bool penalty;
        private SwimmerIdentity taker;
        private PrototypeGoalkeeper penaltyKeeper;
        private float nextHold;
        public SwimmerIdentity Taker => taker;
        public int PersonalFoulLimit => personalFoulLimit;
        public bool IsPenalty => penalty && IsRulePhase;
        public bool IsRulePhase => match.Phase == MatchPhase.FreeThrowSetup || match.Phase == MatchPhase.FreeThrowReady ||
            match.Phase == MatchPhase.PenaltySetup || match.Phase == MatchPhase.PenaltyReady || match.Phase == MatchPhase.PenaltyFlight;
        public event Action<RefereeEvent> Decided;
        public int DecisionCount { get; private set; }

        private void Awake()
        {
            colliders = new Collider[roster.Members.Count]; colliderEnabled = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                var p = roster.Members[i]; p.Rules = this;
                colliders[i] = p.GetComponent<Collider>(); colliderEnabled[i] = colliders[i] != null && colliders[i].enabled;
            }
        }
        private void OnEnable()
        {
            match.NewMatch += ResetMatch; match.Changed += PhaseChanged; ball.PossessionChanged += PossessionChanged;
            ball.Released += Released;
        }
        private void OnDisable()
        {
            match.NewMatch -= ResetMatch; match.Changed -= PhaseChanged; ball.PossessionChanged -= PossessionChanged;
            ball.Released -= Released;
        }
        private PrototypeTeamTactics Team(PrototypeTeam team)
        { for (int i = 0; i < teams.Length; i++) if (teams[i].Team == team) return teams[i]; return null; }
        public int ActiveFields(PrototypeTeam team)
        { int count = 0; foreach (var p in roster.FieldPlayers) if (p.Team == team && p.EligibleForPlay) count++; return count; }
        public bool CanSelect(SwimmerIdentity player) => !IsRulePhase || taker == null || taker.Team != selection.HumanTeam || player == taker;
        public bool CanRelease(SwimmerBallInteraction player, BallReleaseKind kind)
        {
            var identity = player != null ? player.GetComponent<SwimmerIdentity>() : null;
            if (identity == null || !identity.EligibleForPlay) return false;
            if (match.IsPlaying) return true;
            return identity == taker && (match.Phase == MatchPhase.FreeThrowReady &&
                (kind == BallReleaseKind.Pass || kind == BallReleaseKind.Lob) ||
                match.Phase == MatchPhase.PenaltyReady && kind == BallReleaseKind.Shot);
        }
        public bool KeeperMayAct(PrototypeGoalkeeper keeper) => keeper != null && match.Phase == MatchPhase.PenaltyFlight && keeper == penaltyKeeper;
        public bool PenaltyStation(PrototypeGoalkeeper keeper) => IsPenalty && keeper == penaltyKeeper;
        public bool CanMove(SwimmerIdentity player) => match.IsPlaying || (!player.IsFieldPlayer && KeeperMayAct(player.GetComponent<PrototypeGoalkeeper>()));

        /// <summary>A real close-contact command is required. Failed/out-of-range steals alone never award fouls.</summary>
        public bool ReportContact(SwimmerIdentity offender, SwimmerIdentity victim, IllegalContact contact)
        {
            if (!match.IsPlaying || offender == null || victim == null || !offender.IsFieldPlayer ||
                !offender.isActiveAndEnabled || !victim.isActiveAndEnabled || !offender.EligibleForPlay || !victim.EligibleForPlay || offender.Team == victim.Team ||
                !roster.Contains(offender) || !roster.Contains(victim) ||
                PrototypeTeamTactics.Planar(offender.Motor.Position - victim.Motor.Position).sqrMagnitude > 1.96f) return false;
            var carrier = ball.Owner != null ? ball.Owner.GetComponent<SwimmerIdentity>() : null;
            if (carrier == null || carrier.Team != victim.Team) return false;
            bool isPenalty = carrier == victim && ProbableGoal(victim, offender);
            bool exclusion = contact == IllegalContact.Hold && carrier != victim;
            if (!isPenalty && !exclusion && !ordinaryFouls) return false;
            penalty = isPenalty; taker = carrier; penaltyKeeper = null;
            // Capture the restart location before stopping clears the live ball and cancels shot charging.
            Vector3 location = ball.PhysicsPosition;
            match.BeginRuleRestart(penalty);
            if (penalty || exclusion)
            {
                offender.PersonalFouls++;
                offender.PermanentlyRemoved = offender.PersonalFouls >= personalFoulLimit;
                if (exclusion) offender.ExclusionRemaining = exclusionSeconds;
                if (!offender.EligibleForPlay) Park(offender);
            }
            ReleaseTeam(victim.Team);
            if (!taker.EligibleForPlay) taker = FindTaker(victim.Team, location);
            if (taker == null) { match.EndRuleRestart(); return false; }
            if (penalty)
            {
                var goal = Team(victim.Team).AttackingGoal;
                location = goal.transform.position - goal.transform.forward * 5f;
                foreach (var p in roster.Members)
                    if (!p.IsFieldPlayer && p.Team != victim.Team && p.EligibleForPlay) penaltyKeeper = p.GetComponent<PrototypeGoalkeeper>();
                if (penaltyKeeper != null)
                {
                    penaltyKeeper.PreparePenalty();
                }
            }
            else
            {
                var goal = Team(victim.Team).AttackingGoal;
                Vector3 local = goal.transform.InverseTransformPoint(location);
                if (local.z > -2f && Mathf.Abs(local.x) < goal.Width * 0.5f + 2f)
                    location = goal.transform.TransformPoint(new Vector3(local.x, 0f, -2.2f));
            }
            location = pool.ClampPosition(location, 1.1f);
            taker.Motor.ResetPose(location, Quaternion.LookRotation(Team(taker.Team).AttackDirection));
            ArrangeRestart(); ball.TryAwardRestart(taker.Interaction);
            if (match.ShotClock != null)
                match.ShotClock.AwardFoul(victim.Team, exclusion);
            readyAt = Time.unscaledTime + setupSeconds;
            Emit(penalty ? RefereeDecision.Penalty : exclusion ? RefereeDecision.Exclusion : RefereeDecision.Ordinary, offender, victim.Team);
            selection.RefreshEligibility();
            return true;
        }
        private SwimmerIdentity FindTaker(PrototypeTeam team, Vector3 point)
        {
            SwimmerIdentity best = null; float distance = float.PositiveInfinity;
            foreach (var p in roster.FieldPlayers)
            { if (p.Team != team || !p.EligibleForPlay) continue; float d = (p.Motor.Position - point).sqrMagnitude; if (d < distance) { best = p; distance = d; } }
            return best;
        }
        private bool ProbableGoal(SwimmerIdentity victim, SwimmerIdentity offender)
        {
            var team = Team(victim.Team); Vector3 toGoal = PrototypeTeamTactics.Planar(team.AttackingGoal.transform.position - victim.Motor.Position);
            float depth = -team.AttackingGoal.transform.InverseTransformPoint(victim.Motor.Position).z;
            if (depth <= 0f || depth > 6f || toGoal.magnitude > 7f || Vector3.Dot(victim.Motor.Facing, toGoal.normalized) < 0.8f ||
                Vector3.Dot(offender.Motor.Position - victim.Motor.Position, toGoal.normalized) > -0.15f) return false;
            bool shooting = victim.TryGetComponent<ShotCharge>(out var charge) && charge.IsCharging;
            if (!shooting && victim.TryGetComponent<PrototypeSwimmerAI>(out var ai)) shooting = ai.Behaviour == SwimmerAIBehaviour.Shoot;
            if (!shooting) return false;
            foreach (var p in roster.FieldPlayers)
            {
                if (p.Team == victim.Team || p == offender || !p.EligibleForPlay) continue;
                Vector3 d = PrototypeTeamTactics.Planar(p.Motor.Position - victim.Motor.Position);
                float along = Vector3.Dot(d, toGoal.normalized);
                if (along > 0f && along < toGoal.magnitude && (d - toGoal.normalized * along).sqrMagnitude < 2.25f) return false;
            }
            return true;
        }
        // Intentional hold is a gameplay command available to humans and AI; it is never a random referee roll.
        public bool TryHold(SwimmerIdentity offender)
        {
            if (!match.IsPlaying || Time.time < nextHold || offender == null || !offender.EligibleForPlay) return false;
            SwimmerIdentity nearest = null; float distance = 1.96f;
            foreach (var p in roster.Members)
            { if (p.Team == offender.Team || !p.EligibleForPlay) continue; float d = (p.Motor.Position - offender.Motor.Position).sqrMagnitude; if (d < distance) { nearest = p; distance = d; } }
            if (nearest == null) return false;
            nextHold = Time.time + 1.6f;
            return ReportContact(offender, nearest, IllegalContact.Hold);
        }
        private void ArrangeRestart()
        {
            var goal = Team(taker.Team).AttackingGoal;
            int rank = 0;
            foreach (var p in roster.Members)
            {
                if (p == taker || !p.EligibleForPlay || p.GetComponent<PrototypeGoalkeeper>() == penaltyKeeper) continue;
                Vector3 point = p.Motor.Position;
                if (penalty)
                {
                    // All non-participants outside 6m and >=3m from taker, separated into lanes.
                    float x = ((rank % 6) - 2.5f) * 2.7f;
                    point = goal.transform.position - goal.transform.forward * (8f + (rank / 6) * 3f) + goal.transform.right * x;
                    rank++;
                }
                else if (p.Team != taker.Team)
                {
                    Vector3 away = PrototypeTeamTactics.Planar(point - taker.Motor.Position);
                    if (away.sqrMagnitude >= clearance * clearance) continue;
                    if (away.sqrMagnitude < 0.01f) away = -Team(taker.Team).AttackDirection;
                    point = ClearRestartPoint(point, away);
                }
                p.Motor.ResetPose(pool.ClampPosition(point, 1.1f), Quaternion.LookRotation(Team(p.Team).AttackDirection));
            }
        }
        private Vector3 ClearRestartPoint(Vector3 original, Vector3 away)
        {
            Vector3 point=pool.ClampPosition(taker.Motor.Position+away.normalized*clearance,1.1f);
            if(PrototypeTeamTactics.Planar(point-taker.Motor.Position).sqrMagnitude>=clearance*clearance-0.01f)return point;
            // At a wall/corner, choose the nearest legal inward route rather than clamping back onto the taker.
            float best=float.PositiveInfinity;
            for(int i=0;i<12;i++)
            {
                float angle=i*Mathf.PI/6f;
                Vector3 candidate=pool.ClampPosition(taker.Motor.Position+new Vector3(Mathf.Cos(angle),0f,Mathf.Sin(angle))*(clearance+0.05f),1.1f);
                if(PrototypeTeamTactics.Planar(candidate-taker.Motor.Position).sqrMagnitude<clearance*clearance)continue;
                float distance=(candidate-original).sqrMagnitude;
                if(distance<best){best=distance;point=candidate;}
            }
            return point;
        }
        private void Update()
        {
            if (!IsRulePhase) return;
            if (match.Phase == MatchPhase.FreeThrowSetup || match.Phase == MatchPhase.PenaltySetup)
            {
                if (Time.unscaledTime < readyAt) return;
                match.ReadyRuleRestart(penalty); throwAt = Time.unscaledTime + maximumThrowWait;
                if (!penalty) Emit(RefereeDecision.FreeThrow, taker, taker.Team);
                return;
            }
            if (match.Phase == MatchPhase.PenaltyFlight)
            {
                if (ball.Owner != null || Time.unscaledTime >= flightUntil)
                {
                    if (match.ShotClock != null) match.ShotClock.FinishPenalty(taker.Team, ball.Owner);
                    match.EndRuleRestart();
                }
                return;
            }
            if (taker == null || !taker.EligibleForPlay) { match.EndRuleRestart(); return; }
            bool human = taker.Team == selection.HumanTeam;
            if (human && Time.unscaledTime < throwAt) return;
            var team = Team(taker.Team);
            var receiver = penalty ? null : FindOutlet();
            Vector3 target = penalty ? team.ShotTarget() : receiver != null ? receiver.Motor.Position : taker.Motor.Position + team.AttackDirection * 5f;
            Vector3 aim = PrototypeTeamTactics.Planar(target - taker.Motor.Position).normalized;
            taker.Motor.SetFacingDirection(aim);
            if (Vector3.Dot(taker.Motor.Facing, aim) < 0.998f) return;
            if (penalty) taker.Interaction.TryShoot(team.AIShotSpeed);
            else if (receiver == null || !taker.GetComponent<SwimmerPassing>().TryPassTo(receiver))
                ball.TryPass(taker.Interaction, aim, 8f, null, 0.08f, 2.5f);
        }
        private SwimmerIdentity FindOutlet()
        {
            SwimmerIdentity best = null; float score = float.NegativeInfinity;
            foreach (var p in roster.FieldPlayers)
            {
                if (p == taker || p.Team != taker.Team || !p.EligibleForPlay) continue;
                float d = PrototypeTeamTactics.Planar(p.Motor.Position - taker.Motor.Position).magnitude;
                if (d < 2.5f || d > 11f) continue;
                float s = -d - Team(taker.Team).Pressure(p.Motor.Position) * 4f;
                if (s > score) { best = p; score = s; }
            }
            return best;
        }
        private void Released(SwimmerBallInteraction player, BallReleaseKind kind)
        {
            if (player != taker?.Interaction) return;
            if (match.Phase == MatchPhase.FreeThrowReady) match.EndRuleRestart();
            else if (match.Phase == MatchPhase.PenaltyReady) { flightUntil = Time.unscaledTime + 3f; match.LaunchPenalty(); }
        }
        public void TickLive(float seconds)
        {
            if(!match.IsPlaying || seconds<=0f)return;
            foreach (var p in roster.Members)
            {
                if (p.ExclusionRemaining <= 0f || p.PermanentlyRemoved) continue;
                p.ExclusionRemaining = Mathf.Max(0f, p.ExclusionRemaining - seconds);
                if (p.ExclusionRemaining <= 0f) Reenter(p);
            }
        }
        private void PossessionChanged(SwimmerBallInteraction previous, SwimmerBallInteraction next)
        { if (match.IsPlaying && next != null) ReleaseTeam(next.GetComponent<SwimmerIdentity>().Team); }
        public void GoalScored() { ReleaseTeam(PrototypeTeam.Blue); ReleaseTeam(PrototypeTeam.White); }
        private void ReleaseTeam(PrototypeTeam team)
        { foreach (var p in roster.Members) if (p.Team == team && p.ExclusionRemaining > 0f && !p.PermanentlyRemoved) { p.ExclusionRemaining = 0f; Reenter(p); } }
        private void Reenter(SwimmerIdentity player)
        {
            player.Motor.ResetPose(Team(player.Team).DefendedGoal.transform.position - Team(player.Team).DefendedGoal.transform.forward * 4f + Vector3.right * (pool.Width * 0.5f - 1.2f),
                Quaternion.LookRotation(Team(player.Team).AttackDirection));
            RestoreCollider(player); selection.RefreshEligibility();
            Emit(RefereeDecision.Reentry, player, player.Team);
        }
        private void Park(SwimmerIdentity player)
        {
            player.Motor.StopMotion(); player.GetComponent<ShotCharge>()?.CancelCharge();
            Vector3 point = Team(player.Team).DefendedGoal.transform.position;
            point.x = pool.transform.position.x + pool.Width * 0.5f + 1.5f + player.CapNumber * 0.45f;
            point.y = pool.Waterline;
            player.GetComponent<Rigidbody>().position = point; player.transform.position = point;
            var collider = player.GetComponent<Collider>(); if (collider != null) collider.enabled = false;
        }
        private void RestoreCollider(SwimmerIdentity player)
        { for (int i = 0; i < colliders.Length; i++) if (roster.Members[i] == player && colliders[i] != null) colliders[i].enabled = colliderEnabled[i]; }
        private void PhaseChanged()
        { foreach (var p in roster.Members) if (!p.EligibleForPlay) Park(p); }
        private void ResetMatch()
        {
            taker = null; penaltyKeeper = null; penalty = false; nextHold = 0f; DecisionCount = 0;
            foreach (var p in roster.Members) { p.PersonalFouls = 0; p.ExclusionRemaining = 0f; p.PermanentlyRemoved = false; RestoreCollider(p); }
        }
        private void Emit(RefereeDecision decision, SwimmerIdentity player, PrototypeTeam team)
        { DecisionCount++; Decided?.Invoke(new RefereeEvent(decision, player, team)); }
    }
}
