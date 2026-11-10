using UnityEngine;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent, RequireComponent(typeof(SwimmerIdentity))]
    public sealed class SwimmerPassing : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField, Min(0.1f)] private float passSpeed = 8f;
        [SerializeField, Range(0f, 60f)] private float assistCone = 35f;
        [SerializeField, Range(0f, 1f)] private float assistStrength = 0.45f;
        [SerializeField, Range(0f, 25f)] private float maximumCorrection = 12f;
        [SerializeField, Min(1f)] private float maximumTargetDistance = 12f;
        [SerializeField, Min(0.1f)] private float receivingDistance = 1.35f;
        [SerializeField, Min(0.02f)] private float minimumFlightTime = 0.08f;
        [SerializeField, Min(0.1f)] private float receivingWindow = 2.5f;
        private SwimmerIdentity identity;
        public SwimmerIdentity Identity => identity != null ? identity : identity = GetComponent<SwimmerIdentity>();
        public SwimmerBallInteraction Interaction => Identity.Interaction;
        public float ReceivingDistance => receivingDistance;
        public float MaximumTargetDistance => maximumTargetDistance;

        public bool CanReceiveFrom(SwimmerBallInteraction passer)
        {
            return isActiveAndEnabled && Interaction.isActiveAndEnabled && roster != null &&
                passer != null && roster.AreTeammates(Identity, passer.GetComponent<SwimmerIdentity>());
        }

        public bool TryPass()
        {
            if (!Interaction.HasBall || roster == null) return false;
            Vector3 forward = Vector3.ProjectOnPlane(Identity.Motor.Facing, Vector3.up).normalized;
            Vector3 origin = Identity.Motor.Position;
            SwimmerPassing receiver = null;
            float best = float.NegativeInfinity;
            foreach (SwimmerIdentity teammate in roster.Members)
            {
                if (!teammate.IsFieldPlayer || !roster.AreTeammates(Identity, teammate)) continue;
                Vector3 delta = teammate.Motor.Position - origin;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance < 2.5f || distance > maximumTargetDistance) continue;
                float alignment = Vector3.Dot(forward, delta / distance);
                if (alignment < Mathf.Cos(assistCone * Mathf.Deg2Rad)) continue;
                SwimmerPassing candidate = teammate.GetComponent<SwimmerPassing>();
                if (candidate == null || !candidate.CanReceiveFrom(Interaction)) continue;
                float score = alignment - distance * 0.01f;
                if (score > best) { best = score; receiver = candidate; }
            }

            return Launch(forward, receiver);
        }

        public bool TryPassTo(SwimmerIdentity teammate)
        {
            if (!Interaction.HasBall || roster == null || teammate == null || !teammate.IsFieldPlayer || !roster.AreTeammates(Identity, teammate)) return false;
            Vector3 delta = Vector3.ProjectOnPlane(teammate.Motor.Position - Identity.Motor.Position, Vector3.up);
            float distance = delta.magnitude;
            var receiver = teammate.GetComponent<SwimmerPassing>();
            Vector3 forward = Vector3.ProjectOnPlane(Identity.Motor.Facing, Vector3.up).normalized;
            if (distance < 2.5f || distance > maximumTargetDistance || Vector3.Dot(forward, delta / distance) < Mathf.Cos(assistCone * Mathf.Deg2Rad) || receiver == null || !receiver.CanReceiveFrom(Interaction)) return false;
            return Launch(forward, receiver);
        }

        private bool Launch(Vector3 forward, SwimmerPassing receiver)
        {
            Vector3 origin = Identity.Motor.Position;
            Vector3 direction = forward;
            if (receiver != null)
            {
                Vector3 release = origin + forward * Interaction.ReleaseDistance;
                Vector3 desired = receiver.Identity.Motor.Position - release;
                desired.y = 0f;
                Vector3 assisted = Vector3.Slerp(forward, desired.normalized, assistStrength);
                direction = Vector3.RotateTowards(forward, assisted, maximumCorrection * Mathf.Deg2Rad, 0f).normalized;
            }
            return Interaction.Ball.TryPass(Interaction, direction, passSpeed, receiver, minimumFlightTime, receivingWindow);
        }
    }
}
