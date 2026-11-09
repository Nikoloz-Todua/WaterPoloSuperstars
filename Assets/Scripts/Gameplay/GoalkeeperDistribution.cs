using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Secures a keeper hold, aims, and uses the same physical pass/reception mechanics as field players.</summary>
    [DefaultExecutionOrder(-10), DisallowMultipleComponent, RequireComponent(typeof(PrototypeGoalkeeper), typeof(SwimmerPassing))]
    public sealed class GoalkeeperDistribution : MonoBehaviour
    {
        [SerializeField] private PrototypeTeamTactics tacticalTeam;
        [SerializeField] private PrototypeMatch match;
        [SerializeField, Min(0.1f)] private float holdDuration = 1f;
        [SerializeField, Min(0.5f)] private float maximumHold = 3f;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.2f;
        private PrototypeGoalkeeper keeper;
        private SwimmerPassing passing;
        private SwimmerIdentity receiver;
        private float nextDecision;
        public int Distributions { get; private set; }
        public SwimmerIdentity IntendedOutlet => receiver;

        private void Awake()
        {
            keeper = GetComponent<PrototypeGoalkeeper>(); passing = GetComponent<SwimmerPassing>();
            if (tacticalTeam == null || match == null || tacticalTeam.Team != keeper.Identity.Team)
            { Debug.LogError("GoalkeeperDistribution requires matching team and match references.", this); enabled = false; }
        }
        private void OnEnable() { if (match != null) match.Changed += ResetDecision; }
        private void OnDisable() { if (match != null) match.Changed -= ResetDecision; receiver = null; }
        private void ResetDecision() { receiver = null; nextDecision = 0f; }
        private void Update()
        {
            if (!keeper.isActiveAndEnabled || keeper.Stopped) { ResetDecision(); return; }
            if (!keeper.Identity.Interaction.HasBall) { ResetDecision(); return; }
            float held = Time.time - keeper.Ball.OwnershipStartedAt;
            keeper.Identity.Motor.SetMoveInput(Vector2.zero);
            if (held < holdDuration) { keeper.Identity.Motor.SetFacingDirection(-keeper.DefendedGoal.transform.forward); return; }
            if (Time.time >= nextDecision)
            {
                nextDecision = Time.time + decisionInterval;
                receiver = ChooseOutlet();
            }
            bool fallback = held >= Mathf.Max(holdDuration, maximumHold);
            if (receiver != null && !PrototypeTeamTactics.Eligible(receiver)) receiver = null;
            Vector3 aim = receiver != null ? PrototypeTeamTactics.Planar(receiver.Motor.Position - keeper.Identity.Motor.Position).normalized : -keeper.DefendedGoal.transform.forward;
            keeper.Identity.Motor.SetFacingDirection(aim);
            if (Vector3.Dot(transform.forward, aim) < 0.9995f || keeper.Identity.Motor.Velocity.sqrMagnitude > 0.04f) return;
            bool released = false;
            if (receiver != null && (fallback || tacticalTeam.LaneClear(keeper.Identity.Motor.Position, receiver.Motor.Position))) released = passing.TryPassTo(receiver);
            if (!released && fallback) released = passing.TryPass();
            if (released) { Distributions++; ResetDecision(); }
        }
        private SwimmerIdentity ChooseOutlet()
        {
            SwimmerIdentity best = null; float bestScore = float.NegativeInfinity;
            var fields = tacticalTeam.Roster.FieldPlayers;
            for (int i = 0; i < fields.Count; i++)
            {
                var player = fields[i];
                if (!PrototypeTeamTactics.Eligible(player) || player.Team != keeper.Identity.Team) continue;
                float distance = PrototypeTeamTactics.Planar(player.Motor.Position - keeper.Identity.Motor.Position).magnitude;
                if (distance < 2.5f || distance > passing.MaximumTargetDistance) continue;
                if (Vector3.Dot(player.Motor.Position - keeper.Identity.Motor.Position, tacticalTeam.AttackDirection) < 1f) continue;
                float score = (tacticalTeam.LaneClear(keeper.Identity.Motor.Position, player.Motor.Position) ? 2f : 0f) - distance * 0.1f - tacticalTeam.Pressure(player.Motor.Position) * 0.5f;
                if (score > bestScore) { bestScore = score; best = player; }
            }
            return best;
        }
    }
}
