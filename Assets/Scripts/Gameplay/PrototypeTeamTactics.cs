using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Explicit team geometry and shared roster context; never owns the ball.</summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeTeamTactics : MonoBehaviour
    {
        [SerializeField] private PrototypeTeam team;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private BallController ball;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private GoalMouth attackingGoal;
        [SerializeField] private GoalMouth defendedGoal;
        [SerializeField] private PrototypeMatch match;
        [SerializeField, Min(0.1f)] private float assignmentInterval = 0.5f;
        [SerializeField, Min(0f)] private float reassignmentAdvantage = 1f;
        [SerializeField, Min(1f)] private float supportForward = 4f;
        [SerializeField, Min(1f)] private float supportWidth = 3f;
        [SerializeField, Min(0.5f)] private float pressureDistance = 1.5f;
        [SerializeField, Min(1f)] private float shootingRange = 9f;
        [SerializeField, Range(0f, 1f)] private float shotQualityThreshold = 0.55f;
        [SerializeField, Min(0.1f)] private float minimumHoldTime = 0.8f;
        [SerializeField, Min(1f)] private float maximumHoldTime = 8f;
        [SerializeField, Min(0.5f)] private float teamPassCooldown = 3f;
        private SwimmerIdentity presser, loosePursuer;
        private SwimmerBallInteraction assignedOwner;
        private float nextAssignment, nextPass;
        public PrototypeTeam Team => team;
        public PrototypeRoster Roster => roster;
        public BallController Ball => ball;
        public PoolBounds Pool => pool;
        public GoalMouth AttackingGoal => attackingGoal;
        public GoalMouth DefendedGoal => defendedGoal;
        public Vector3 AttackDirection => attackingGoal.transform.forward;
        public float ShootingRange => shootingRange;
        public float ShotQualityThreshold => shotQualityThreshold;
        public float MinimumHoldTime => minimumHoldTime;
        public float MaximumHoldTime => maximumHoldTime;
        public bool CanPass => Time.time >= nextPass;
        public SwimmerIdentity Carrier => ball.Owner != null ? ball.Owner.GetComponent<SwimmerIdentity>() : null;
        public SwimmerIdentity Presser { get { RefreshAssignments(); return presser; } }
        public SwimmerIdentity LoosePursuer { get { RefreshAssignments(); return loosePursuer; } }
        public void RecordPass() => nextPass = Time.time + teamPassCooldown;
        public void ResetDecisions() { nextAssignment = nextPass = 0f; presser = loosePursuer = null; assignedOwner = null; }
        private void OnEnable() { if (match != null) match.Changed += ResetDecisions; }
        private void OnDisable() { if (match != null) match.Changed -= ResetDecisions; }
        public static Vector3 Planar(Vector3 delta) { delta.y = 0f; return delta; }
        public static bool Eligible(SwimmerIdentity player) => player != null && player.IsFieldPlayer && player.enabled && player.gameObject.activeInHierarchy && player.Motor.enabled && player.Interaction.isActiveAndEnabled;
        public Vector3 Clamp(Vector3 position) => pool.ClampPosition(position, 1.1f);

        public int Slot(SwimmerIdentity player)
        {
            int rank = 0;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var member = roster.Members[i]; if (member == player) return rank;
                if (Eligible(member) && member.Team == team) rank++;
            }
            return rank;
        }
        public Vector3 Formation(SwimmerIdentity player)
        {
            int slot = Slot(player);
            Vector3 right = Vector3.Cross(Vector3.up, AttackDirection);
            return Clamp(pool.transform.position - AttackDirection * (3f + slot / 2 * 2f) + right * (slot % 2 == 0 ? -3f : 3f));
        }
        public Vector3 Support(SwimmerIdentity player, SwimmerIdentity carrier)
        {
            int rank = 0;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (p == player) break;
                if (Eligible(p) && p.Team == team && p != carrier) rank++;
            }
            return Clamp(SwimmerTactics.Support(carrier.Motor.Position, AttackDirection, rank, carrier.IsFieldPlayer ? supportForward : 6f, supportWidth));
        }
        public Vector3 Defense(SwimmerIdentity player, SwimmerIdentity carrier)
        {
            if (carrier.IsFieldPlayer && Presser == player) return Clamp(SwimmerTactics.Defend(carrier.Motor.Position, defendedGoal.transform.position, pressureDistance));
            SwimmerIdentity mark = null;
            int count = 0;
            for (int i = 0; i < roster.Members.Count; i++)
                if (Eligible(roster.Members[i]) && roster.Members[i].Team != team && roster.Members[i] != carrier) count++;
            int wanted = Slot(player) % Mathf.Max(1, count), index = 0;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (!Eligible(p) || p.Team == team || p == carrier) continue;
                if (index++ == wanted) { mark = p; break; }
            }
            Vector3 threat = mark != null ? mark.Motor.Position : carrier.Motor.Position + Vector3.Cross(Vector3.up, AttackDirection) * (Slot(player) % 2 == 0 ? -3f : 3f);
            return Clamp(Vector3.Lerp(threat, defendedGoal.transform.position, 0.28f));
        }
        public float Pressure(Vector3 position)
        {
            float nearest = 100f;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (Eligible(p) && p.Team != team) nearest = Mathf.Min(nearest, Planar(p.Motor.Position - position).magnitude);
            }
            return Mathf.Clamp01(1f - nearest / 3f);
        }
        public bool LaneClear(Vector3 from, Vector3 to, float clearance = 0.9f)
        {
            Vector3 line = Planar(to - from); float length = line.sqrMagnitude;
            if (length < 0.01f) return false;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (!Eligible(p) || p.Team == team) continue;
                float t = Vector3.Dot(Planar(p.Motor.Position - from), line) / length;
                if (t > 0.08f && t < 0.95f && Planar(p.Motor.Position - (from + line * t)).sqrMagnitude < clearance * clearance) return false;
            }
            return true;
        }
        public float ShotQuality(Vector3 position)
        {
            Vector3 local = attackingGoal.transform.InverseTransformPoint(position);
            float distance = Planar(attackingGoal.transform.position - position).magnitude;
            if (local.z >= -1f || distance > shootingRange) return 0f;
            float angle = Mathf.Clamp01(1f - Mathf.Abs(local.x) / 10f);
            return Mathf.Clamp01(0.5f * (1f - distance / (shootingRange * 1.5f)) + 0.3f * angle + (LaneClear(position, attackingGoal.transform.position) ? 0.2f : 0f) - Pressure(position) * 0.2f);
        }
        public SwimmerIdentity BetterPass(SwimmerIdentity carrier)
        {
            SwimmerIdentity best = null; float bestScore = 0.12f;
            Vector3 origin = carrier.Motor.Position;
            float currentDistance = Planar(attackingGoal.transform.position - origin).magnitude;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (!Eligible(p) || p.Team != team || p == carrier) continue;
                Vector3 delta = Planar(p.Motor.Position - origin); float distance = delta.magnitude;
                if (distance < 2.5f || distance > 11f || !LaneClear(origin, p.Motor.Position)) continue;
                float progress = currentDistance - Planar(attackingGoal.transform.position - p.Motor.Position).magnitude;
                float score = progress * 0.08f + (Pressure(origin) - Pressure(p.Motor.Position)) * 0.4f;
                if (score > bestScore) { best = p; bestScore = score; }
            }
            return best;
        }
        private void RefreshAssignments()
        {
            if (assignedOwner == ball.Owner && Time.time < nextAssignment) return;
            bool changed = assignedOwner != ball.Owner; assignedOwner = ball.Owner;
            nextAssignment = Time.time + assignmentInterval;
            Vector3 point = Carrier != null ? Carrier.Motor.Position : ball.PhysicsPosition;
            var previous = ball.Owner != null ? presser : loosePursuer;
            SwimmerIdentity nearest = null; float best = float.PositiveInfinity;
            for (int i = 0; i < roster.Members.Count; i++)
            {
                var p = roster.Members[i]; if (!Eligible(p) || p.Team != team) continue;
                float distance = Planar(p.Motor.Position - point).magnitude;
                if (!changed && p == previous) distance -= reassignmentAdvantage;
                if (distance < best) { nearest = p; best = distance; }
            }
            if (ball.Owner != null) presser = nearest; else loosePursuer = nearest;
        }

        private void Awake()
        {
            if (roster == null || ball == null || pool == null || match == null || attackingGoal == null || defendedGoal == null || attackingGoal.ScoringTeam != team || defendedGoal.ScoringTeam == team)
            {
                Debug.LogError("PrototypeTeamTactics requires a roster, ball, pool and correctly assigned opposing goals.", this);
                enabled = false;
            }
        }
    }
}
